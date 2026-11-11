using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace SilentPavilion.Editor
{
    public static class MidScenePolish
    {
        public static Material CreateBarkMaterial()
        {
            const string path="Assets/Mid/Materials/Mat_Mid_CreatureBark.mat";
            var shader=Shader.Find("Mid/Mid_CreatureBark");
            if(shader==null) throw new InvalidOperationException("Mid_CreatureBark shader missing");
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(shader){name="Mat_Mid_CreatureBark"};AssetDatabase.CreateAsset(material,path);}
            material.shader=shader;material.SetColor("_BaseColor",new Color(.48f,.43f,.34f));material.SetFloat("_Smoothness",.14f);
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Pavilion/Textures/Wood_c.png"));
            material.SetTextureScale("_BaseMap",new Vector2(1,1.8f));EditorUtility.SetDirty(material);return material;
        }
        static Mesh StoreMesh(Mesh mesh)
        {
            string path="Assets/Mid/Prefabs/GeneratedMeshes/"+mesh.name+".asset";
            var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(previous==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,previous);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(previous);return previous;
        }
        public static MidProximityStatic EnsureStaticEffect(Transform player,Transform creature)
        {
            var fx=Object.FindFirstObjectByType<MidProximityStatic>();
            if(fx==null)
            {
                var go=new GameObject("Proximity static / short bursts",typeof(RectTransform),typeof(Canvas));
                var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
                fx=go.AddComponent<MidProximityStatic>();
                var child=new GameObject("Static overlay",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
                child.transform.SetParent(go.transform,false);
                var rect=child.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
                fx.overlay=child.GetComponent<RawImage>();
            }
            const string path="Assets/Mid/Materials/Mat_Mid_ScreenStatic.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                var shader=Shader.Find("Mid/Mid_ScreenStatic");if(shader==null)throw new InvalidOperationException("Mid_ScreenStatic shader missing");
                material=new Material(shader){name="Mat_Mid_ScreenStatic"};AssetDatabase.CreateAsset(material,path);
            }
            material.SetFloat("_Intensity",0);EditorUtility.SetDirty(material);
            fx.player=player;fx.creature=creature;fx.overlay.material=material;fx.overlay.texture=Texture2D.whiteTexture;
            fx.overlay.raycastTarget=false;fx.overlay.enabled=false;EditorUtility.SetDirty(fx);return fx;
        }
        public static void ApplyBatch()
        {
            EditorSceneManager.OpenScene(MidSceneBuilder.ScenePath);Apply();MidSceneValidation.Validate();
        }
        [MenuItem("Tools/Mid/The Silent Pavilion/06 Apply Branch Creature and Walking")]
        public static void Apply()
        {
            // 부분 갱신: 환경의 수동 배치를 전체 재생성으로 잃지 않는다.
            MidSceneRevision.Apply();
            var breath=Object.FindFirstObjectByType<MidCreatureBreathing>();
            var root=breath.transform;
            breath.enabled=false;
            // '/'가 들어간 오브젝트 이름은 Transform.Find의 경로로 해석되므로 이름으로 직접 비교.
            var lower=Array.Find(root.GetComponentsInChildren<MeshFilter>(),m=>m.name=="Lower body / anchored pelvis and legs");
            if(lower==null) throw new InvalidOperationException("Creature lower-body mesh missing");
            lower.sharedMesh=StoreMesh(MidCreatureMesh.BuildLowerBody());
            breath.upperBody.sharedMesh=StoreMesh(MidCreatureMesh.BuildUpperBody());
            root.Find("Eyes").GetComponent<MeshFilter>().sharedMesh=StoreMesh(MidCreatureMesh.BuildEyes());
            var bark=CreateBarkMaterial();lower.GetComponent<Renderer>().sharedMaterial=bark;
            breath.upperBody.GetComponent<Renderer>().sharedMaterial=bark;
            breath.breathsPerSecond=.22f;breath.expansion=.028f;breath.angleDegrees=3.2f;breath.swayDegrees=.9f;breath.enabled=true;
            EditorUtility.SetDirty(breath);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Mid/Prefabs/Mid_SeatedCreature.prefab");
            var walker=Object.FindFirstObjectByType<MidFirstPersonController>();
            walker.moveSpeed=1.55f;walker.acceleration=10;walker.deceleration=20;walker.strideLength=1.45f;
            walker.headBobHeight=.027f;walker.headBobSide=.013f;walker.footstepAudio=true;EditorUtility.SetDirty(walker);
            EnsureStaticEffect(walker.transform,root);
            MidLanternPlacement.Apply();
            var scene=MidSceneRevision.GetScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[Mid] Branch creature, readable motion, grounded walking, static bursts and text removal saved.");
        }
    }
}
