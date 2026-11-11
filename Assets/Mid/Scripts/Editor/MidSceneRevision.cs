using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace SilentPavilion.Editor
{
    // 열려 있는 Editor에서 Mid 장면만 검사/수정한다. 원본 실습 장면은 건드리지 않는다.
    public static class MidSceneRevision
    {
        public static void InspectBatch()
        {
            EditorSceneManager.OpenScene(MidSceneBuilder.ScenePath);
            Inspect();
        }
        public static void ApplyBatch()
        {
            EditorSceneManager.OpenScene(MidSceneBuilder.ScenePath);
            Apply();
            MidSceneValidation.Validate();
        }
        static Transform Find(Scene scene,string name)
        {
            foreach(var root in scene.GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true)) if(t.name==name) return t;
            throw new InvalidOperationException("Missing Mid object: "+name);
        }
        public static Bounds MeasureBench(GameObject pavilion,float floor)
        {
            // 원본 메시의 좌측 긴 좌판: 가늘고 긴 수평 판재들을 모아 실측한다.
            // 난간/다리의 윗면은 폭과 높이 조건으로 제외한다. 원본 FBX는 수정하지 않는다.
            bool found=false;Bounds seat=default;
            foreach(var mf in pavilion.GetComponentsInChildren<MeshFilter>())
            {
                if(mf.name!="Pavilion") continue;
                var v=mf.sharedMesh.vertices;var indices=mf.sharedMesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                {
                    Vector3 a=mf.transform.TransformPoint(v[indices[i]]),b=mf.transform.TransformPoint(v[indices[i+1]]),c=mf.transform.TransformPoint(v[indices[i+2]]);
                    Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
                    float minX=Mathf.Min(a.x,b.x,c.x),maxX=Mathf.Max(a.x,b.x,c.x);
                    float minZ=Mathf.Min(a.z,b.z,c.z),maxZ=Mathf.Max(a.z,b.z,c.z);
                    if(normal.y<.99f || a.y<floor+.4f || a.y>floor+.75f || maxX>-.5f
                        || maxX-minX<.14f || maxZ-minZ<1f || minZ<1) continue;
                    if(!found) {seat=new Bounds(a,Vector3.zero);found=true;}
                    seat.Encapsulate(a);seat.Encapsulate(b);seat.Encapsulate(c);
                }
            }
            if(!found) throw new InvalidOperationException("기존 좌측 의자의 좌판을 찾지 못했습니다.");
            return seat;
        }
        public static float MeasureFloor(GameObject pavilion)
        {
            Physics.SyncTransforms();float floor=float.NegativeInfinity;
            foreach(var collider in pavilion.GetComponentsInChildren<Collider>())
                if(collider.Raycast(new Ray(new Vector3(0,1,0),Vector3.down),out RaycastHit hit,2)) floor=Mathf.Max(floor,hit.point.y);
            if(!float.IsFinite(floor)) throw new InvalidOperationException("정자 바닥 측정 실패");
            return floor;
        }
        public static void PositionOnBench(Transform set,GameObject pavilion,float floor)
        {
            Bounds seat=MeasureBench(pavilion,floor);
            // 원본 인체의 골반 최저점 .68, 발 최저점 .02를 좌판과 바닥에 각각 맞춘다.
            float scale=(seat.center.y-floor)/(.68f-.02f);
            set.localScale=Vector3.one*scale;
            set.rotation=Quaternion.Euler(0,-90,0); // 인체 전방(-Z)을 정자 안쪽(+X)으로.
            set.position=new Vector3(seat.center.x,floor-.02f*scale,seat.center.z-.25f);
            Directory.CreateDirectory(MidSceneBuilder.Root+"/Documentation");
            File.WriteAllText(MidSceneBuilder.Root+"/Documentation/BenchPlacement.txt",
                "Existing left pavilion bench (original FBX unchanged)\nSeat bounds: "+seat+"\nFloor: "+floor+
                "\nCreature root: "+set.position+"\nScale: "+scale+"\nYaw: -90\nPelvis bottom: "+
                (set.position.y+.68f*scale)+" / feet bottom: "+(set.position.y+.02f*scale));
        }
        public static void TuneCreatureLighting(MidAtmosphere atmosphere,float floor)
        {
            atmosphere.farRevealIntensity=.25f;atmosphere.nearRevealIntensity=5.5f;
            var light=atmosphere.revealLight;
            light.intensity=atmosphere.farRevealIntensity;light.range=5.8f;
            light.spotAngle=64;light.innerSpotAngle=30;
            light.transform.position=new Vector3(.9f,floor+2.2f,.55f);
            light.transform.LookAt(atmosphere.creature.TransformPoint(new Vector3(0,1.3f,0)));
        }
        [MenuItem("Tools/Mid/The Silent Pavilion/05 Apply Lighting and Bench Revision")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 종료한 뒤 적용하세요.");
            var scene=GetScene();
            if(SceneManager.GetActiveScene()!=scene) throw new InvalidOperationException("Mid 장면을 활성화한 뒤 적용하세요.");
            var set=Find(scene,"THE SEATED FIGURE");
            var pavilion=Find(scene,"Pavilion / reused original geometry").gameObject;
            float floor=MeasureFloor(pavilion);
            // 사용자 지정 장면 전체를 다시 만들지 않고 요청한 오브젝트/재질/빛만 수정한다.
            foreach(var t in set.GetComponentsInChildren<Transform>(true))
                if(t!=null && t.name=="Old wooden chair") Undo.DestroyObjectImmediate(t.gameObject);
            PositionOnBench(set,pavilion,floor);
            var atmosphere=Find(scene,"DIRECTION / slow reveal").GetComponent<MidAtmosphere>();
            TuneCreatureLighting(atmosphere,floor);
            MidSceneBuilder.ConfigureNight();
            var moon=Find(scene,"Moon through branches").GetComponent<Light>();moon.intensity=.65f;
            Transform sky=null;
            foreach(var t in moon.transform.parent.GetComponentsInChildren<Transform>()) if(t.name=="Soft sky / branch readability") sky=t;
            if(sky==null)
            {
                sky=new GameObject("Soft sky / branch readability",typeof(Light)).transform;
                sky.SetParent(moon.transform.parent,false);
            }
            sky.rotation=Quaternion.Euler(55,140,0);var fill=sky.GetComponent<Light>();
            fill.type=LightType.Directional;fill.color=new Color(.27f,.42f,.52f);fill.intensity=.18f;fill.shadows=LightShadows.None;
            var bark=AssetDatabase.LoadAssetAtPath<Material>(MidSceneBuilder.Root+"/Materials/Mat_Mid_TwistedBark.mat");
            bark.SetColor("_BaseColor",new Color(.26f,.30f,.28f));EditorUtility.SetDirty(bark);
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(MidSceneBuilder.Root+"/Materials/Mid_NightVolume.asset");
            if(profile.TryGet<ColorAdjustments>(out var color)) {color.postExposure.Override(.95f);color.contrast.Override(7);EditorUtility.SetDirty(color);}
            if(profile.TryGet<Vignette>(out var vignette)) {vignette.intensity.Override(.24f);EditorUtility.SetDirty(vignette);}
            EditorUtility.SetDirty(profile);EditorUtility.SetDirty(atmosphere);
            // 삭제 요청을 받은 Mid 전용 별도 의자 에셋만 제거한다.
            if(AssetDatabase.LoadAssetAtPath<GameObject>(MidSceneBuilder.Root+"/Prefabs/Mid_OldChair.prefab")!=null)
                AssetDatabase.DeleteAsset(MidSceneBuilder.Root+"/Prefabs/Mid_OldChair.prefab");
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[Mid] Subtle lighting lift and existing-bench seating saved.");
        }
        internal static Scene GetScene()
        {
            var scene=SceneManager.GetSceneByPath(MidSceneBuilder.ScenePath);
            if(!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("TheSilentPavilion 장면을 먼저 열어야 합니다.");
            return scene;
        }
        static void Inspect()
        {
            var scene=GetScene();var report=new StringBuilder("object\ty\txMin\txMax\tzMin\tzMax\tnormalY\tarea\n");
            foreach(var root in scene.GetRootGameObjects())
                foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
                {
                    if(!AssetDatabase.GetAssetPath(mf.sharedMesh).EndsWith("Pavilion.FBX")) continue;
                    var v=mf.sharedMesh.vertices;var t=mf.sharedMesh.triangles;
                    for(int i=0;i<t.Length;i+=3)
                    {
                        Vector3 a=mf.transform.TransformPoint(v[t[i]]),b=mf.transform.TransformPoint(v[t[i+1]]),c=mf.transform.TransformPoint(v[t[i+2]]);
                        Vector3 cross=Vector3.Cross(b-a,c-a);float y=(a.y+b.y+c.y)/3;
                        if(Mathf.Abs(cross.normalized.y)<.98f || y<.85f || y>1.7f) continue;
                        report.AppendLine(string.Join("\t",mf.name,y.ToString("F4"),Mathf.Min(a.x,b.x,c.x).ToString("F4"),
                            Mathf.Max(a.x,b.x,c.x).ToString("F4"),Mathf.Min(a.z,b.z,c.z).ToString("F4"),Mathf.Max(a.z,b.z,c.z).ToString("F4"),
                            cross.normalized.y.ToString("F1"),(cross.magnitude*.5f).ToString("F4")));
                    }
                }
            File.WriteAllText(MidSceneBuilder.Root+"/Documentation/BenchGeometry.tsv",report.ToString());
        }
    }
}
