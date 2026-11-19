using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class S20CombinedShaderDemo
{
    const string Root = "Assets/S23";
    const string ScenePath = Root + "/Scenes/S20_ToonOutlineVertexColor_Comparison.unity";

    [MenuItem("Tools/S20/Create Toon Outline Vertex Color Comparison")]
    public static void Build()
    {
        if (File.Exists(ScenePath))
            throw new InvalidOperationException("Comparison scene already exists. Open it instead of overwriting it: " + ScenePath);

        var shader = Shader.Find("CG/S20_ToonOutlineVertexColor_Finish");
        Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Combined shader compiles");
        var material = new Material(shader) { name = "Mat_S20_ToonOutlineVertexColor_Finish" };
        material.SetColor("_SpecularColor", new Color(.25f, .25f, .25f, 1));
        AssetDatabase.CreateAsset(material, Root + "/Materials/" + material.name + ".mat");
        Require(material.passCount == 2, "Exactly two passes");

        // An additive scene preserves the user's currently open scene and edits.
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        try
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            mesh.name = "S20_VertexColoredSphere";
            Object.DestroyImmediate(primitive);
            var vertices = mesh.vertices;
            var colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.Lerp(new Color(.12f, .35f, 1), new Color(1, .22f, .12f), vertices[i].y + .5f);
            mesh.colors = colors;
            AssetDatabase.CreateAsset(mesh, Root + "/Meshes/S20_VertexColoredSphere.asset");

            Material Load(string path) => AssetDatabase.LoadAssetAtPath<Material>(Root + "/References/S20_Blinn_Phong/" + path);
            var toon = Load("S20_ToonShading/Mat_S20_Toon_Finish.mat");
            var outline = Load("S20_ToonShading/Mat_S20_Outline_Finish.mat");
            var vertex = Load("S20_Mirror_Rim_VertexColor_Flow/Mat_S20_VertexColor_Finish.mat");
            Require(toon && outline && vertex, "Three original materials exist");
            AddSphere("01 Stacked - Toon, Outline, VertexColor", -1.6f, mesh, new[] { toon, outline, vertex });
            AddSphere("02 Stacked - VertexColor, Outline, Toon", 0, mesh, new[] { vertex, outline, toon });
            var combined = AddSphere("03 Combined - 2 passes, 1 material", 1.6f, mesh, new[] { material });
            Require(combined.sharedMaterials.Length == 1 && mesh.subMeshCount == 1, "One material slot and one submesh");
            Require(mesh.colors.Length == mesh.vertexCount, "Every mesh vertex has a color");

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.cullingMask = 1 << 31;
            light.intensity = 1;
            light.transform.rotation = Quaternion.Euler(35, -35, 0);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.cullingMask = 1 << 31;
            camera.transform.position = new Vector3(0, .25f, -7);
            camera.transform.LookAt(Vector3.zero);
            camera.orthographic = true;
            camera.orthographicSize = 1.3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.18f, .21f, .27f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            var rt = new RenderTexture(1440, 640, 24);
            var before = RenderTexture.active;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try
            {
                rt.Create();
                camera.aspect = (float)rt.width / rt.height;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Root + "/Documentation/S20_Comparison.png", texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = before;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(texture);
            }
            Require(!ShaderUtil.ShaderHasError(shader), "No shader errors after rendering");
            File.WriteAllText(Root + "/Documentation/S20_Validation.txt",
                "Unity " + Application.unityVersion + "\nPASS: shader compiled and rendered with URP.\n" +
                "PASS: combined shader has exactly 2 passes; renderer has 1 material; mesh has 1 submesh.\n" +
                "PASS: mesh.colors contains a color for every vertex.\n" +
                "Preview left to right: Toon/Outline/VertexColor; VertexColor/Outline/Toon; combined.\n");
            AssetDatabase.Refresh();
            Debug.Log("S20_COMBINED_VALIDATION_PASS");
        }
        finally
        {
            if (!Application.isBatchMode) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        }
    }

    static MeshRenderer AddSphere(string name, float x, Mesh mesh, Material[] materials)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.layer = 31;
        go.transform.position = new Vector3(x, 0, 0);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterials = materials;
        return renderer;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("S20: " + message);
        Debug.Log("S20 PASS: " + message);
    }
}
