using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SilentPavilion.Editor
{
    public static class MidFinalSubmission
    {
        public const string Folder="Assets/Mid/Submission";
        [MenuItem("Tools/Mid/The Silent Pavilion/07 Final Audit and Lantern Placement")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MidSceneBuilder.ScenePath);
            MidLanternPlacement.Apply();
            foreach(string name in new[] {"WispGlow","FaintEyes","LanternWarm"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mid/Materials/Mat_Mid_"+name+".mat");
                if(material==null)throw new InvalidOperationException("Missing emission material: "+name);
                material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
                material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);
            }
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            MidSceneValidation.Validate();CheckWinding();
        }
        static void CheckWinding()
        {
            var b=new MidMeshBuilder();b.AddTube(new[] {Vector3.zero,Vector3.up},new[] {.3f,.3f},4,12,1,0);
            var tube=b.Build("WindingProbe");var v=tube.vertices;var tris=tube.triangles;
            // 앞의 4*12*2 삼각형은 튜브 옆면. 외적이 중심축 바깥을 향해야 한다.
            for(int i=0;i<4*12*6;i+=3)
            {
                Vector3 center=(v[tris[i]]+v[tris[i+1]]+v[tris[i+2]])/3;center.y=0;
                if(Vector3.Dot(Vector3.Cross(v[tris[i+1]]-v[tris[i]],v[tris[i+2]]-v[tris[i]]),center)<=0)
                    throw new InvalidOperationException("Tube winding failure");
            }
            Object.DestroyImmediate(tube);
            b=new MidMeshBuilder();b.AddEllipsoid(Vector3.zero,Vector3.one);
            var sphere=b.Build("WindingProbe");v=sphere.vertices;tris=sphere.triangles;
            for(int i=0;i<tris.Length;i+=3)
            {
                Vector3 center=(v[tris[i]]+v[tris[i+1]]+v[tris[i+2]])/3;
                if(Vector3.Dot(Vector3.Cross(v[tris[i+1]]-v[tris[i]],v[tris[i+2]]-v[tris[i]]),center)<=0)
                    throw new InvalidOperationException("Ellipsoid winding failure");
            }
            Object.DestroyImmediate(sphere);
            File.WriteAllText(Folder+"/GEOMETRY_AUDIT.txt","PASS: generated tube side faces and ellipsoid faces point outward.\nActual scene vertex/index/degenerate-face checks: Documentation/Validation.txt\nRuntime mesh deformation and rendering: Documentation/PlayModeValidation.txt\n");
        }
        public static void ExportPackage()
        {
            var guids=AssetDatabase.FindAssets("",new[] {"Assets/Mid/Scenes","Assets/Mid/Scripts","Assets/Mid/Materials","Assets/Mid/Shaders","Assets/Mid/Prefabs"});
            var paths=new System.Collections.Generic.List<string>();
            foreach(var guid in guids){string path=AssetDatabase.GUIDToAssetPath(guid);if(!AssetDatabase.IsValidFolder(path))paths.Add(path);}
            paths.Add("Assets/Mid/README.md");
            AssetDatabase.ExportPackage(paths.ToArray(),Folder+"/The_Silent_Pavilion_Midterm.unitypackage",ExportPackageOptions.IncludeDependencies);
            AssetDatabase.Refresh();
        }
    }
}
