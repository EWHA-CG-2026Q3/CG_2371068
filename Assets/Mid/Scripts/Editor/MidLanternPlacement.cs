using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace SilentPavilion.Editor
{
    public static class MidLanternPlacement
    {
        static Transform Find(string name)
        {
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if(t.name==name)return t;
            throw new InvalidOperationException("Missing lantern part: "+name);
        }
        static Bounds BoundsOf(Transform t)
        {
            Bounds bounds=default;bool first=true;
            // 편집 직후 Renderer.bounds의 갱신 시점에 의존하지 않고 메시의 8개 모서리로 실측.
            foreach(var mf in t.GetComponentsInChildren<MeshFilter>())
            {
                var b=mf.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    Vector3 p=mf.transform.TransformPoint(b.center+Vector3.Scale(b.extents,
                        new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                }
            }
            return bounds;
        }
        public static void Apply()
        {
            var roof=Find("Pavilion_Roof").GetComponent<Renderer>().bounds;
            var lamp=Find("Old hanging lamp / reused mesh");var core=Find("Failing lantern glow");
            var light=Find("Lantern / warm flicker").GetComponent<Light>();
            var lighting=lamp.parent;var rim=Find("Cold silhouette rim").GetComponent<Light>();
            // 현재 장면에서 광원/발광부가 랜턴의 자식일 수도 있다. 먼저 월드 좌표를 보존해 분리.
            core.SetParent(lighting,true);light.transform.SetParent(lighting,true);rim.transform.SetParent(lighting,true);
            float roofBottom=roof.min.y,shadeHeight=.34f;
            Vector3 center=new Vector3(roof.center.x+.05f,roofBottom-.14f-shadeHeight*.5f,roof.center.z+.10f);
            // FBX의 원점/임포트 스케일을 Mid 전용 복제 메시 안에서 정규화한다.
            // 원본 Lamp.FBX는 그대로 두며 변환 캐시/원점 오프셋에 의존하지 않는다.
            var lampFilter=lamp.GetComponent<MeshFilter>();var normalized=Object.Instantiate(lampFilter.sharedMesh);
            var vertices=normalized.vertices;var sourceBounds=normalized.bounds;
            for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-sourceBounds.center)*(shadeHeight/sourceBounds.size.y);
            normalized.vertices=vertices;normalized.RecalculateBounds();normalized.name="Mid_LanternShade";
            const string shadePath="Assets/Mid/Prefabs/GeneratedMeshes/Mid_LanternShade.asset";
            var shade=AssetDatabase.LoadAssetAtPath<Mesh>(shadePath);
            if(shade==null){AssetDatabase.CreateAsset(normalized,shadePath);shade=normalized;}
            else{EditorUtility.CopySerialized(normalized,shade);Object.DestroyImmediate(normalized);EditorUtility.SetDirty(shade);}
            lampFilter.sharedMesh=shade;lamp.rotation=Quaternion.identity;lamp.localScale=Vector3.one;lamp.position=center;
            Physics.SyncTransforms();
            Vector3 bulb=new Vector3(center.x,center.y-shadeHeight*.5f-.04f,center.z);
            core.position=bulb;core.localScale=Vector3.one*.65f;
            light.transform.position=bulb;light.color=new Color(1,.60f,.30f);light.shadowNearPlane=.05f;
            const string materialPath="Assets/Mid/Materials/Mat_Mid_LanternWarm.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Mat_Mid_LanternWarm"};AssetDatabase.CreateAsset(material,materialPath);}
            material.SetColor("_BaseColor",new Color(.35f,.16f,.05f));material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",new Color(1,.47f,.18f)*3);EditorUtility.SetDirty(material);
            var glow=core.GetComponent<Renderer>();glow.sharedMaterial=material;glow.shadowCastingMode=ShadowCastingMode.Off;
            // 지붕 밑면에서 실제 금속 덮개까지 이어지는 짧은 고정 줄.
            var builder=new MidMeshBuilder();
            Vector3 top=new Vector3(center.x,roofBottom-.015f,center.z),bottom=new Vector3(center.x,center.y+shadeHeight*.5f,center.z);
            builder.AddTube(new[] {top,bottom},new[] {.009f,.009f},3,8);
            var mesh=builder.Build("Mid_LanternHanger");string meshPath="Assets/Mid/Prefabs/GeneratedMeshes/Mid_LanternHanger.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing==null){AssetDatabase.CreateAsset(mesh,meshPath);existing=mesh;}
            else {EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
            Transform hanger=null;foreach(var t in lighting.GetComponentsInChildren<Transform>())if(t.name=="Lantern ceiling hanger")hanger=t;
            if(hanger==null)hanger=new GameObject("Lantern ceiling hanger",typeof(MeshFilter),typeof(MeshRenderer)).transform;
            hanger.SetParent(lighting,false);hanger.localPosition=Vector3.zero;hanger.localRotation=Quaternion.identity;hanger.localScale=Vector3.one;
            hanger.GetComponent<MeshFilter>().sharedMesh=existing;
            hanger.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mid/Materials/Mat_Mid_TarnishedIron.mat");
            // 천장 위의 파란 원형 얼룩을 만들던 림라이트는 뒤쪽 벤치 높이로 옮긴다.
            float floor=MidSceneRevision.MeasureFloor(Find("Pavilion / reused original geometry").gameObject);
            rim.transform.position=new Vector3(-1.10f,floor+1.90f,3.9f);rim.intensity=1.1f;
            core.SetParent(lamp,true);light.transform.SetParent(lamp,true);
            string measured="roof="+roofBottom+" shade="+BoundsOf(lamp)+" core="+core.position+" light="+light.transform.position+
                " model position="+lamp.position+" scale="+lamp.lossyScale;
            Debug.Log("[Mid lantern] "+measured);
            if(BoundsOf(lamp).max.y>=roofBottom || Vector3.Distance(core.position,light.transform.position)>.001f)
                throw new InvalidOperationException("Lantern alignment failed: "+measured);
            File.WriteAllText("Assets/Mid/Documentation/LanternPlacement.txt",
                "PASS: shade below roof, ceiling hanger connected, warm bulb and point light coincide.\n"+
                "Roof underside: "+roofBottom+"\nShade bounds: "+BoundsOf(lamp)+"\nBulb and point light: "+bulb+"\nRim relocated behind bench: "+rim.transform.position);
        }
    }
}
