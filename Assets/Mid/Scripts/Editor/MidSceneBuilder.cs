using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilentPavilion.Editor
{
    /// <summary>원본 프리팹/재질/프로젝트 설정을 쓰지 않고 Mid 전용 장면과 에셋을 만든다.</summary>
    public static class MidSceneBuilder
    {
        public const string Root="Assets/Mid";
        public const string ScenePath=Root+"/Scenes/TheSilentPavilion.unity";
        const string Source="Assets/ThirdParty/Pavilion/";
        static Material wood,skin,bark,ground,puddle,metal,eyes,mist,glow;

        [MenuItem("Tools/Mid/The Silent Pavilion/01 Open or Create Scene")]
        public static void OpenOrCreate()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Build();
        }

        [MenuItem("Tools/Mid/The Silent Pavilion/02 Rebuild Generated Scene")]
        public static void Rebuild()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        // -batchmode -executeMethod SilentPavilion.Editor.MidSceneBuilder.BuildBatch -quit
        public static void BuildBatch() { Build(); MidSceneValidation.Validate(); }

        static void EnsureFolders()
        {
            foreach(string relative in new[] {"Scenes","Scripts/Geometry","Scripts/Transforms","Scripts/Player",
                "Scripts/Effects","Scripts/Editor","Materials","Shaders","Prefabs","Prefabs/GeneratedMeshes","Documentation"})
                Directory.CreateDirectory(Root+"/"+relative);
            AssetDatabase.Refresh();
        }

        static void Build()
        {
            EnsureFolders();
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Prefabs/Pavilion.prefab")==null)
                throw new InvalidOperationException("기존 Pavilion.prefab을 찾을 수 없습니다.");
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene.name="TheSilentPavilion";
            MakeMaterials();
            ConfigureNight();
            Transform environment=Group("ENVIRONMENT");
            Box("Wet earth",environment,new Vector3(0,-.18f,0),new Vector3(95,.36f,95),ground);
            GameObject pavilion=CreatePavilion(environment,out float floor);
            Debug.Log("[Mid] Pavilion floor="+floor+", bounds="+CombinedBounds(pavilion));
            Transform set=Group("THE SEATED FIGURE");
            MidSceneRevision.PositionOnBench(set,pavilion,floor);
            GameObject creature=CreateCreature(set);
            CreateForest(environment);
            CreateGroundDetails(environment);
            CreateMist(environment,floor);
            var lights=CreateLighting(environment,floor);
            Transform player=CreatePlayer();
            var atmosphere=Group("DIRECTION / slow reveal").gameObject.AddComponent<MidAtmosphere>();
            atmosphere.player=player; atmosphere.creature=creature.transform;
            atmosphere.lantern=lights[0]; atmosphere.revealLight=lights[1];
            atmosphere.eyes=creature.transform.Find("Eyes").GetComponent<Renderer>();
            MidSceneRevision.TuneCreatureLighting(atmosphere,floor);
            MidScenePolish.EnsureStaticEffect(player,creature.transform);
            MidLanternPlacement.Apply();
            CreateVolume();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("장면 저장 실패");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject=creature;
            if(SceneView.lastActiveSceneView!=null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0,floor+1.3f,0),Quaternion.Euler(8,0,0),10);
            Debug.Log("[Mid] Created "+ScenePath+". WASD / click to look / Esc to release.");
        }

        static Transform Group(string name,Transform parent=null)
        { var t=new GameObject(name).transform; t.SetParent(parent,false); return t; }
        static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        static T SaveAsset<T>(T asset,string path) where T:Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing==null) { AssetDatabase.CreateAsset(asset,path);return asset; }
            EditorUtility.CopySerialized(asset,existing);Object.DestroyImmediate(asset);EditorUtility.SetDirty(existing);return existing;
        }
        static Mesh MeshAsset(Mesh mesh) { return SaveAsset(mesh,Root+"/Prefabs/GeneratedMeshes/"+mesh.name+".asset"); }

        static Material Lit(string name,Color color,float smoothness=0,float metallic=0,string texture=null)
        {
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null) throw new InvalidOperationException("URP Lit shader가 없습니다.");
            var m=new Material(shader) {name="Mat_Mid_"+name};
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Metallic",metallic);
            if(texture!=null) m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
            m.enableInstancing=true;
            return SaveAsset(m,Root+"/Materials/"+m.name+".mat");
        }
        static void MakeMaterials()
        {
            wood=Lit("OldChairWood",new Color(.28f,.24f,.19f),.2f,0,Source+"Textures/Table_Wood.png");
            skin=MidScenePolish.CreateBarkMaterial();
            bark=Lit("TwistedBark",new Color(.26f,.30f,.28f),.18f,0,Source+"Textures/Wood_c.png");
            ground=Lit("WetEarth",new Color(.17f,.22f,.21f),.76f,.08f,
                "Assets/ThirdParty/Textures/rough-igneous-rock-albedo.png");
            ground.SetTextureScale("_BaseMap",new Vector2(32,32));
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Textures/rough-igneous-rock-normal-ogl.png");
            if(normal!=null) {ground.SetTexture("_BumpMap",normal);ground.SetFloat("_BumpScale",.45f);ground.EnableKeyword("_NORMALMAP");}
            puddle=Lit("Puddles",new Color(.025f,.055f,.06f),.97f,.35f);
            metal=Lit("TarnishedIron",new Color(.07f,.09f,.095f),.48f,.7f);
            eyes=Lit("FaintEyes",new Color(.04f,.07f,.06f),.25f);
            eyes.EnableKeyword("_EMISSION");eyes.SetColor("_EmissionColor",new Color(.25f,.5f,.43f)*.4f);
            glow=Lit("WispGlow",new Color(.12f,.3f,.3f),.4f);
            glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.25f,.9f,.8f)*3);
            var shader=Shader.Find("Mid/Mid_GroundMist");
            if(shader==null) throw new InvalidOperationException("Mid_GroundMist shader가 없습니다.");
            mist=SaveAsset(new Material(shader) {name="Mat_Mid_GroundMist"},Root+"/Materials/Mat_Mid_GroundMist.mat");
        }

        internal static void ConfigureNight()
        {
            RenderSettings.skybox=null;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.10f,.14f,.17f);
            RenderSettings.ambientEquatorColor=new Color(.045f,.065f,.075f);
            RenderSettings.ambientGroundColor=new Color(.008f,.012f,.014f);
            RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.018f,.035f,.041f);RenderSettings.fogDensity=.032f;
            RenderSettings.reflectionIntensity=.35f;
        }

        static Bounds CombinedBounds(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>();
            var b=renderers[0].bounds; foreach(var r in renderers) b.Encapsulate(r.bounds);return b;
        }
        static GameObject CreatePavilion(Transform parent,out float floor)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Prefabs/Pavilion.prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name="Pavilion / reused original geometry";
            PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            go.transform.SetParent(parent,false);
            // 임포트 단위에 의존하지 않도록 실측 bounds로 폭을 맞춘다.
            var b=CombinedBounds(go);float factor=8.6f/Mathf.Max(b.size.x,b.size.z);
            go.transform.localScale=Vector3.one*factor;
            b=CombinedBounds(go);go.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                // sharedMaterial의 값을 바꾸면 원본 실습까지 바뀌므로 새 URP 재질만 할당한다.
                var originals=r.sharedMaterials;var replacements=new Material[originals.Length];
                for(int i=0;i<originals.Length;i++)
                {
                    var original=originals[i];string n=original!=null?original.name:"Wood_a";
                    string materialPath=Root+"/Materials/Mat_Mid_Pavilion_"+n+".mat";
                    var replacement=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(replacement==null)
                    {
                        replacement=new Material(Shader.Find("Universal Render Pipeline/Lit")) {name="Mat_Mid_Pavilion_"+n};
                        Texture tex=original!=null && original.HasProperty("_BaseMap")?original.GetTexture("_BaseMap"):null;
                        if(tex==null && original!=null && original.HasProperty("_MainTex")) tex=original.GetTexture("_MainTex");
                        if(tex==null) tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/"+n+".png");
                        replacement.SetTexture("_BaseMap",tex);
                        replacement.SetColor("_BaseColor",new Color(.40f,.45f,.43f));
                        replacement.SetFloat("_Smoothness",.46f);replacement.enableInstancing=true;
                        AssetDatabase.CreateAsset(replacement,materialPath);
                    }
                    replacements[i]=replacement;
                }
                r.sharedMaterials=replacements;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,0);
            }
            Physics.SyncTransforms();
            // 지붕 대신 바닥을 잡도록 낮은 위치에서 아래로 측정. 의자 높이의 기준이다.
            floor=0;
            if(Physics.Raycast(new Vector3(0,1.8f,0),Vector3.down,out RaycastHit hit,3)) floor=hit.point.y;
            return go;
        }

        static GameObject CreateCreature(Transform parent)
        {
            var root=Group("Creature / seated procedural anatomy",parent).gameObject;
            var lower=MeshObject("Lower body / anchored pelvis and legs",root.transform,MeshAsset(MidCreatureMesh.BuildLowerBody()),skin);
            var upper=MeshObject("Upper body / sampled curved surfaces",root.transform,MeshAsset(MidCreatureMesh.BuildUpperBody()),skin);
            var eyeObject=MeshObject("Eyes",root.transform,MeshAsset(MidCreatureMesh.BuildEyes()),eyes);
            var breath=root.AddComponent<MidCreatureBreathing>();breath.upperBody=upper.GetComponent<MeshFilter>();breath.eyes=eyeObject.transform;
            // 접촉만 막는 capsule. 시각적 크리처는 전부 직접 생성한 Mesh이다.
            var collider=root.AddComponent<CapsuleCollider>();collider.center=new Vector3(0,1.15f,-.1f);collider.radius=.38f;collider.height=2.25f;
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Mid_SeatedCreature.prefab");
            return root;
        }

        static void CreateForest(Transform parent)
        {
            Transform forest=Group("Forest / procedural branching meshes",parent);
            var variants=new Mesh[9];
            for(int i=0;i<variants.Length;i++) variants[i]=MeshAsset(MidTwistedTree.Build(2371068+i,5.0f+i*.28f));
            var rng=new System.Random(2371068);
            for(int i=0;i<76;i++)
            {
                float angle=(float)rng.NextDouble()*Mathf.PI*2;
                float radius=7.5f+(float)rng.NextDouble()*26;
                Vector3 p=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                // 입구 쪽 시선과 걸어 들어갈 통로를 비워 둔다.
                if(p.z<0 && Mathf.Abs(p.x)<2.7f) {i--;continue;}
                GameObject tree=MeshObject("Twisted tree "+i.ToString("00"),forest,variants[i%variants.Length],bark);
                tree.transform.localPosition=p;tree.transform.localRotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,0);
                float scale=.75f+(float)rng.NextDouble()*.7f;tree.transform.localScale=Vector3.one*scale;
                if(radius<15) {var c=tree.AddComponent<CapsuleCollider>();c.radius=.32f;c.height=4;c.center=Vector3.up*2;}
            }
            var branchMesh=MeshAsset(MidTwistedTree.Build(520,2.1f));
            var broken=MeshObject("Broken tree / additional geometry exhibit",forest,branchMesh,bark);
            broken.transform.localPosition=new Vector3(-5.8f,.12f,-3.8f);broken.transform.localRotation=Quaternion.Euler(0,25,68);
        }
        static void CreateGroundDetails(Transform parent)
        {
            Transform details=Group("Wet path / puddles and scattered timber",parent);
            var builder=new MidMeshBuilder();builder.AddEllipsoid(Vector3.zero,Vector3.one,8,28);
            Mesh patch=MeshAsset(builder.Build("Mid_PuddleEllipse"));
            var rng=new System.Random(51);
            for(int i=0;i<34;i++)
            {
                float x=(float)rng.NextDouble()*22-11,z=(float)rng.NextDouble()*27-19;
                if(Mathf.Abs(x)<4.5f && Mathf.Abs(z)<4.5f) continue;
                var go=MeshObject("Still puddle "+i,details,patch,puddle);
                go.transform.localPosition=new Vector3(x,.008f,z);
                go.transform.localScale=new Vector3(.35f+(float)rng.NextDouble()*1.2f,.004f,.3f+(float)rng.NextDouble()*.7f);
                go.transform.localRotation=Quaternion.Euler(0,(float)rng.NextDouble()*180,0);
            }
            for(int i=0;i<12;i++)
            {
                var plank=Box("Discarded timber",details,new Vector3(-4.8f-i*.11f,.055f,-2.4f+i*.21f),new Vector3(.12f,.07f,.7f+(i%3)*.25f),wood);
                plank.transform.localRotation=Quaternion.Euler(2,i*47,3);
            }
        }
        static Mesh MistQuad()
        {
            var mesh=new Mesh {name="Mid_MistPlane"};
            mesh.vertices=new[] {new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
            mesh.uv=new[] {Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[] {0,2,1,0,3,2};
            mesh.RecalculateNormals();mesh.RecalculateBounds();return MeshAsset(mesh);
        }
        static void CreateMist(Transform parent,float floor)
        {
            var group=Group("Low drifting mist / layered procedural shader",parent);var quad=MistQuad();
            for(int i=0;i<7;i++)
            {
                var plane=MeshObject("Mist layer "+i,group,quad,mist);plane.transform.localPosition=new Vector3(i%2==0?-4:4,.16f+i*.17f,-3+i);
                plane.transform.localScale=new Vector3(33,1,34);plane.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            var ps=Group("Suspended dust",group).gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.startLifetime=12;main.startSpeed=.025f;main.startSize=new ParticleSystem.MinMaxCurve(.008f,.025f);
            main.startColor=new Color(.12f,.25f,.25f,.2f);main.maxParticles=90;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.rateOverTime=4;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(6,2.5f,6);
            ps.transform.localPosition=new Vector3(0,floor+1.5f,0);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=glow;
        }
        static Light Light(string name,Transform parent,Vector3 position,Color color,float intensity,float range,LightType type=LightType.Point)
        {
            var l=Group(name,parent).gameObject.AddComponent<Light>();l.type=type;l.transform.localPosition=position;
            l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;return l;
        }
        static Light[] CreateLighting(Transform parent,float floor)
        {
            Transform lighting=Group("Lighting / cold moon and failing lantern",parent);
            var moon=Light("Moon through branches",lighting,new Vector3(0,12,0),new Color(.36f,.60f,.72f),.65f,50,LightType.Directional);
            moon.transform.rotation=Quaternion.Euler(38,-40,0);moon.shadows=LightShadows.Soft;RenderSettings.sun=moon;
            var sky=Light("Soft sky / branch readability",lighting,Vector3.zero,new Color(.27f,.42f,.52f),.18f,50,LightType.Directional);
            sky.transform.rotation=Quaternion.Euler(55,140,0);
            var lantern=Light("Lantern / warm flicker",lighting,new Vector3(.95f,floor+2.65f,-.9f),new Color(1,.52f,.23f),2.3f,5.5f);
            lantern.shadows=LightShadows.Soft;
            // 원본 램프도 복제 후 전용 재질을 할당해 재사용한다.
            var lampSource=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Prefabs/Lamp.prefab");
            if(lampSource!=null)
            {
                var lamp=Object.Instantiate(lampSource,lighting);lamp.name="Old hanging lamp / reused mesh";
                var b=CombinedBounds(lamp);lamp.transform.localScale*=.46f/Mathf.Max(.01f,b.size.y);
                b=CombinedBounds(lamp);lamp.transform.position+=lantern.transform.position-b.center;
                foreach(var r in lamp.GetComponentsInChildren<Renderer>()) r.sharedMaterial=metal;
                foreach(var c in lamp.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                var flameBuilder=new MidMeshBuilder();flameBuilder.AddEllipsoid(Vector3.zero,new Vector3(.045f,.09f,.045f),10,12);
                var flame=MeshObject("Failing lantern glow",lighting,MeshAsset(flameBuilder.Build("Mid_LanternGlow")),glow);
                flame.transform.position=lantern.transform.position;
            }
            var reveal=Light("Creature / gradual soft reveal",lighting,new Vector3(-.8f,floor+2.55f,-1.7f),new Color(.52f,.75f,.69f),.12f,4.8f,LightType.Spot);
            reveal.spotAngle=58;reveal.innerSpotAngle=26;
            reveal.transform.LookAt(new Vector3(0,floor+1.35f,.7f));reveal.shadows=LightShadows.Soft;
            var rim=Light("Cold silhouette rim",lighting,new Vector3(.5f,floor+2.2f,2.6f),new Color(.2f,.56f,.69f),2.4f,5);
            Light("Path / reflected moon",lighting,new Vector3(-3,3,-7),new Color(.2f,.43f,.52f),1.6f,11);
            var wispParent=Group("WISPS / independent hierarchy motion",parent);
            var sphereBuilder=new MidMeshBuilder();sphereBuilder.AddEllipsoid(Vector3.zero,Vector3.one*.023f,8,12);
            Mesh wispMesh=MeshAsset(sphereBuilder.Build("Mid_WispMesh"));
            for(int i=0;i<4;i++)
            {
                var wisp=MeshObject("Wisp "+i,wispParent,wispMesh,glow);
                var motion=wisp.AddComponent<MidWispMotion>();
                motion.orbitCenter=new Vector3(i%2==0?-4.7f:4.7f,1.05f+(i%2)*.4f,-1-i*2.8f);
                motion.radius=.5f+i*.13f;motion.phase=i*1.9f;motion.angularSpeed=.16f+i*.025f;
                wisp.transform.localPosition=motion.PositionAt(0);
                Light("Wisp faint spill",wisp.transform,Vector3.zero,new Color(.2f,.65f,.6f),.35f,1.4f);
            }
            return new[] {lantern,reveal};
        }
        static Transform CreatePlayer()
        {
            var player=Group("PLAYER / first person");player.position=new Vector3(0,.15f,-12);
            var controller=player.gameObject.AddComponent<CharacterController>();
            controller.height=1.75f;controller.radius=.23f;controller.center=Vector3.up*.88f;
            controller.stepOffset=.35f;controller.slopeLimit=55;controller.skinWidth=.035f;
            var view=Group("Player camera",player);view.localPosition=new Vector3(0,1.64f,0);
            var camera=view.gameObject.AddComponent<Camera>();camera.tag="MainCamera";
            camera.nearClipPlane=.04f;camera.farClipPlane=95;camera.fieldOfView=63;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.009f,.018f,.023f);
            camera.allowHDR=true;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
            data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;data.volumeLayerMask=1;
            view.gameObject.AddComponent<AudioListener>();
            var walker=player.gameObject.AddComponent<MidFirstPersonController>();walker.view=view;return player;
        }
        static void CreateVolume()
        {
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Mid_NightVolume";
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.48f);bloom.threshold.Override(.85f);bloom.scatter.Override(.65f);
            var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.95f);color.contrast.Override(7);color.saturation.Override(-22);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.24f);vignette.smoothness.Override(.7f);
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            string path=Root+"/Materials/Mid_NightVolume.asset";
            // Volume 컴포넌트는 프로파일의 subasset으로 저장해야 재실행 후에도 남는다.
            if(AssetDatabase.LoadAssetAtPath<VolumeProfile>(path)!=null)
            {
                // 생성된 프로파일만 교체. 기존 실습 프로파일은 참조/수정하지 않는다.
                var existing=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                foreach(var c in existing.components.ToArray()) Object.DestroyImmediate(c,true);
                existing.components.Clear();
                foreach(var c in profile.components) {existing.components.Add(c);AssetDatabase.AddObjectToAsset(c,existing);}
                // 임시 프로파일의 OnDisable이 옮긴 컴포넌트까지 Release하지 않도록 참조를 먼저 비운다.
                profile.components.Clear();
                Object.DestroyImmediate(profile);profile=existing;EditorUtility.SetDirty(profile);
            }
            else
            {
                AssetDatabase.CreateAsset(profile,path);
                foreach(var c in profile.components) AssetDatabase.AddObjectToAsset(c,profile);
            }
            var volume=Group("Night grading / Bloom + ACES").gameObject.AddComponent<Volume>();
            volume.isGlobal=true;volume.priority=20;volume.sharedProfile=profile;
        }
    }
}
