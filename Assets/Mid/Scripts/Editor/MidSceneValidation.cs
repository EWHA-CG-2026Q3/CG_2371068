using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace SilentPavilion.Editor
{
    public static class MidSceneValidation
    {
        static void Require(bool condition,string message) {if(!condition) throw new InvalidOperationException("[Mid] "+message);}

        [MenuItem("Tools/Mid/The Silent Pavilion/03 Validate Scene")]
        public static void Validate()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Require(scene.path==MidSceneBuilder.ScenePath,"Mid 장면을 먼저 열어 주세요.");
            var report=new StringBuilder("The Silent Pavilion — Unity validation\n");
            report.AppendLine("Unity: "+Application.unityVersion+" / "+DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            Vector3 transformed=MidMatrixMath.TransformPoint(MidMatrixMath.Multiply(
                MidMatrixMath.Translation(new Vector3(3,0,0)),
                MidMatrixMath.Multiply(MidMatrixMath.RotationX(Mathf.PI*.5f),MidMatrixMath.Scale(new Vector3(2,1,1)))),new Vector3(1,1,0));
            Require(Vector3.Distance(transformed,new Vector3(5,0,1))<.0001f,"직접 TRS 계산 검산 실패");
            report.AppendLine("PASS: direct matrix T*Rx*S maps (1,1,0) to (5,0,1).");
            int meshCount=0,triangleCount=0;
            foreach(var root in scene.GetRootGameObjects())
            {
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root)==0,"루트에 누락 스크립트");
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"누락 스크립트: "+t.name);
                foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
                {
                    Require(mf.sharedMesh!=null,"누락 Mesh: "+mf.name);
                    Mesh m=mf.sharedMesh;
                    if(!AssetDatabase.GetAssetPath(m).StartsWith("Assets/Mid/")) continue;
                    meshCount++;var v=m.vertices;var tr=m.triangles;triangleCount+=tr.Length/3;
                    Require(v.Length>0 && tr.Length>0 && tr.Length%3==0,"빈/잘못된 Mesh: "+m.name);
                    for(int i=0;i<v.Length;i++) Require(float.IsFinite(v[i].x)&&float.IsFinite(v[i].y)&&float.IsFinite(v[i].z),"비정상 좌표: "+m.name);
                    for(int i=0;i<tr.Length;i+=3)
                    {
                        Require(tr[i]>=0&&tr[i]<v.Length&&tr[i+1]>=0&&tr[i+1]<v.Length&&tr[i+2]>=0&&tr[i+2]<v.Length,"인덱스 범위: "+m.name);
                        Require(Vector3.Cross(v[tr[i+1]]-v[tr[i]],v[tr[i+2]]-v[tr[i]]).sqrMagnitude>1e-18f,"면적 0인 삼각형: "+m.name);
                    }
                }
                foreach(var r in root.GetComponentsInChildren<Renderer>())
                    foreach(var material in r.sharedMaterials)
                    {
                        Require(material!=null && material.shader!=null,"누락 Material: "+r.name);
                        Require(!ShaderUtil.ShaderHasError(material.shader),"셰이더 컴파일 오류: "+material.shader.name);
                    }
            }
            report.AppendLine("PASS: "+meshCount+" Mid mesh instances / "+triangleCount+" triangles; finite vertices, valid nonzero faces, materials, scripts.");
            foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if(AssetDatabase.GetAssetPath(mf.sharedMesh).EndsWith("Pavilion.FBX"))
                    report.AppendLine("Pavilion part "+mf.name+": "+mf.GetComponent<Renderer>().bounds);
            var breath=Object.FindFirstObjectByType<MidCreatureBreathing>();Require(breath!=null,"호흡 컴포넌트 누락");
            Require(GameObject.Find("Old wooden chair")==null,"별도 의자가 남아 있습니다.");
            var set=breath.transform.parent;
            var pavilion=GameObject.Find("Pavilion / reused original geometry");
            float floor=MidSceneRevision.MeasureFloor(pavilion);
            Bounds bench=MidSceneRevision.MeasureBench(pavilion,floor);
            float pelvisBottom=set.TransformPoint(new Vector3(0,.68f,.05f)).y;
            Require(Mathf.Abs(pelvisBottom-bench.center.y)<.005f,"골반이 기존 의자 좌판에 닿지 않습니다.");
            Vector3 pelvis=set.TransformPoint(new Vector3(0,.68f,.05f));bool supported=false;
            foreach(var collider in pavilion.GetComponentsInChildren<Collider>())
                if(collider.Raycast(new Ray(pelvis+Vector3.up*.2f,Vector3.down),out RaycastHit contact,.3f)
                    && Mathf.Abs(contact.point.y-pelvis.y)<.005f) supported=true;
            Require(supported,"원본 의자 충돌체가 골반을 받치지 않습니다.");
            Require(Mathf.Abs(set.TransformPoint(new Vector3(0,.02f,0)).y-floor)<.005f,"발 높이가 정자 바닥과 다릅니다.");
            Require(Vector3.Dot(-set.forward,Vector3.right)>.99f,"크리처가 정자 안쪽을 향하지 않습니다.");
            report.AppendLine("PASS: no separate chair; pelvis contacts original bench; creature faces inward.");
            var pivot=breath.pivot;
            Require(Vector3.Distance(MidMatrixMath.TransformPoint(breath.MatrixAt(1.3f),pivot),pivot)<.0001f,"호흡 중 골반 기준점 이동");
            Require(MidAtmosphere.RevealAtDistance(2)>MidAtmosphere.RevealAtDistance(9),"접근 연출 거리 계산");
            var wisps=Object.FindObjectsByType<MidWispMotion>(FindObjectsSortMode.None);
            Require(wisps.Length>=2 && Vector3.Distance(wisps[0].PositionAt(0),wisps[0].PositionAt(3))>.01f,"불빛 운동 누락");
            report.AppendLine("PASS: pelvis pivot fixed; distance reveal; independent wisp motion.");
            var walker=Object.FindFirstObjectByType<MidFirstPersonController>();Require(walker!=null && walker.view!=null,"플레이어 누락");
            var interference=Object.FindFirstObjectByType<MidProximityStatic>();
            Require(interference!=null && interference.overlay!=null && !interference.overlay.raycastTarget,"잡음 오버레이 참조/입력 차단 오류");
            Require(!ShaderUtil.ShaderHasError(interference.overlay.material.shader),"잡음 셰이더 컴파일 오류");
            Require(Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Length==0,"화면 텍스트가 남아 있습니다.");
            report.AppendLine("PASS: branch creature bark shader; text-free static overlay does not intercept input.");
            var camera=walker.view.GetComponent<Camera>();Require(camera!=null && camera.GetUniversalAdditionalCameraData().renderPostProcessing,"카메라 후처리 비활성");
            Physics.SyncTransforms();
            var controller=walker.GetComponent<CharacterController>();Vector3 initial=walker.transform.position;
            // 실제 CharacterController로 통로와 계단 충돌을 검증. Inspector 값은 원위치로 복원한다.
            for(int i=0;i<400;i++) controller.Move(new Vector3(0,-.02f,0));
            for(int i=0;i<1000 && walker.transform.position.z<1.8f;i++) controller.Move(new Vector3(0,-.015f,.025f));
            Vector3 reached=walker.transform.position;
            controller.enabled=false;walker.transform.position=initial;controller.enabled=true;Physics.SyncTransforms();
            report.AppendLine("Walk route endpoint: "+reached.ToString("F3"));
            Require(reached.z>1.6f,"기존 의자 앞까지 진입 경로가 막혔습니다. 도착점: "+reached);
            Require(reached.y>-.2f && reached.y<1.8f,"플레이어 바닥 높이 비정상");
            report.AppendLine("PASS: CharacterController outside -> pavilion interior, restored initial position.");
            File.WriteAllText(MidSceneBuilder.Root+"/Documentation/Validation.txt",report.ToString());
            AssetDatabase.Refresh();Debug.Log(report.ToString());
        }

        [MenuItem("Tools/Mid/The Silent Pavilion/04 Capture Preview Images")]
        public static void CapturePreviews()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null) {Debug.LogWarning("[Mid] nographics에서는 캡처할 수 없습니다.");return;}
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=MidSceneBuilder.ScenePath)
                EditorSceneManager.OpenScene(MidSceneBuilder.ScenePath);
            var player=Object.FindFirstObjectByType<MidFirstPersonController>();
            var source=player.view.GetComponent<Camera>();
            var preview=Object.Instantiate(source.gameObject);preview.name="Mid temporary preview camera";
            var camera=preview.GetComponent<Camera>();camera.enabled=false;
            var listener=preview.GetComponent<AudioListener>();if(listener!=null) Object.DestroyImmediate(listener);
            // PNG 출력은 sRGB 대상에 렌더링한다. 선형 HDR 값을 그대로 저장하면 실제 Game 뷰보다 어둡다.
            var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
            var before=RenderTexture.active;
            try
            {
                Capture(camera,rt,"Preview_Approach",new Vector3(0,1.7f,-9.5f),new Vector3(0,1.75f,.8f));
                var atmosphere=Object.FindFirstObjectByType<MidAtmosphere>();
                float floor=MidSceneRevision.MeasureFloor(GameObject.Find("Pavilion / reused original geometry"));
                float originalIntensity=atmosphere.revealLight.intensity;
                try
                {
                    atmosphere.revealLight.intensity=atmosphere.nearRevealIntensity;
                    Vector3 chest=atmosphere.creature.TransformPoint(new Vector3(0,1.25f,0));
                    Capture(camera,rt,"Preview_Interior",new Vector3(.7f,floor+1.64f,.15f),chest);
                    CaptureMotionPoses(camera,rt,atmosphere.creature.GetComponent<MidCreatureBreathing>(),
                        new Vector3(.7f,floor+1.64f,.15f),chest);
                }
                finally {atmosphere.revealLight.intensity=originalIntensity;}
                Capture(camera,rt,"Preview_Forest",new Vector3(0,1.7f,-12),new Vector3(-5,5.5f,3));
            }
            finally {RenderTexture.active=before;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(preview);}
            AssetDatabase.Refresh();Debug.Log("[Mid] Preview images saved under Assets/Mid/Documentation.");
        }
        static void Capture(Camera camera,RenderTexture rt,string name,Vector3 position,Vector3 target)
        {
            camera.transform.position=position;camera.transform.LookAt(target);camera.aspect=(float)rt.width/rt.height;
            // URP 17의 공식 RenderRequest. Camera.Render / 폐기된 RenderSingleCamera를 쓰지 않는다.
            var request=new UniversalRenderPipeline.SingleCameraRequest {destination=rt};
            RenderPipeline.SubmitRenderRequest(camera,request);
            RenderTexture.active=rt;
            var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();
            File.WriteAllBytes(MidSceneBuilder.Root+"/Documentation/"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
        }
        static void CaptureMotionPoses(Camera camera,RenderTexture rt,MidCreatureBreathing breath,Vector3 position,Vector3 target)
        {
            var upper=breath.upperBody;var eyes=breath.eyes.GetComponent<MeshFilter>();
            Mesh original=upper.sharedMesh,originalEyes=eyes.sharedMesh;
            Mesh preview=Object.Instantiate(original),previewEyes=Object.Instantiate(originalEyes);
            try
            {
                upper.sharedMesh=preview;eyes.sharedMesh=previewEyes;
                for(int frame=0;frame<2;frame++)
                {
                    float t=(frame==0?.25f:.75f)/breath.breathsPerSecond;
                    var matrix=breath.MatrixAt(t);var v=original.vertices;var e=originalEyes.vertices;
                    for(int i=0;i<v.Length;i++) v[i]=MidMatrixMath.TransformPoint(matrix,v[i]);
                    for(int i=0;i<e.Length;i++) e[i]=MidMatrixMath.TransformPoint(matrix,e[i]);
                    preview.vertices=v;preview.RecalculateNormals();preview.RecalculateBounds();
                    previewEyes.vertices=e;previewEyes.RecalculateBounds();
                    Capture(camera,rt,frame==0?"Preview_MotionA":"Preview_MotionB",position,target);
                }
            }
            finally {upper.sharedMesh=original;eyes.sharedMesh=originalEyes;Object.DestroyImmediate(preview);Object.DestroyImmediate(previewEyes);}
        }
        public static void CaptureBatch() {EditorSceneManager.OpenScene(MidSceneBuilder.ScenePath);CapturePreviews();}
    }
}
