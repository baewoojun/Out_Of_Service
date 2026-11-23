// One-click Unity setup for the bunny monster, its mirror twin and the turtle mascot monster.
//   Tools > Bunny Monster > 1. Setup        : for every monster found: import settings, clip names/loops,
//                                             LOD materials + LOD Group, Animator Controller, prefab
//   Tools > Bunny Monster > 2. Create Demo Scene : test scene (player capsule + shelves + both monsters)
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BunnyMonsterKit.EditorTools
{
    public static class BunnyMonsterSetup
    {
        // model file, prefab name, texture prefix. Textures per monster:
        //   <tex>_albedo, <tex>_mask (R metallic 0, G AO, B 0, A smoothness),
        //   <tex>_hp_normal (LOD0, fine residual detail), <tex>_normal (LOD1, full high-poly bake)
        struct MonsterDef
        {
            public string Model, Prefab, Tex;
            public MonsterDef(string model, string prefab, string tex) { Model = model; Prefab = prefab; Tex = tex; }
        }
        static readonly MonsterDef[] Monsters =
        {
            new MonsterDef("bunny_monster", "BunnyMonster", "bunny"),   // white bunny, grinning
            new MonsterDef("bunny_twin", "BunnyTwin", "twin"),          // black mirror twin, crying
            new MonsterDef("turtle_monster", "TurtleMonster", "turtle"),  // green turtle mascot, cracked shell + tendrils
        };
        // LOD0 is drawn while the monster fills more than this fraction of the screen height
        public const float Lod0ScreenHeight = 0.30f;
        public const float CullScreenHeight = 0.01f;

        // ground speeds measured from the clips (planted foot), metres per second
        public const float WalkSpeed = 0.9f;
        public const float RunSpeed = 2.6f;

        static readonly string[] LoopClips = { "Idle", "Walk", "Run" };
        static readonly string[] RequiredClips = { "Idle", "Walk", "Run", "Attack", "Scream" };

        [MenuItem("Tools/Bunny Monster/1. Setup (Import + Animator + Prefab)")]
        public static void Setup()
        {
            var made = new List<string>();
            GameObject last = null;
            foreach (MonsterDef def in Monsters)
            {
                string prefabPath = SetupOne(def);
                if (prefabPath == null) continue;
                made.Add(prefabPath);
                last = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            if (made.Count == 0)
            {
                Fail("Model 폴더에서 bunny_monster.fbx / bunny_twin.fbx / turtle_monster.fbx 를 찾지 못했어요. BunnyMonster 폴더를 Assets 아래에 넣어 주세요.");
                return;
            }
            Selection.activeObject = last;
            EditorGUIUtility.PingObject(last);
            EditorUtility.DisplayDialog("Bunny Monster",
                "세팅 완료!\n\n- " + string.Join("\n- ", made.ToArray()) + "\n\n" +
                "Tools > Bunny Monster > 2. Create Demo Scene 으로 바로 테스트해 볼 수 있어요.", "OK");
        }

        // returns the prefab path, or null when this monster's FBX is not in the project
        static string SetupOne(MonsterDef def)
        {
            string fbxPath = FindAsset(def.Model, "t:Model");
            if (fbxPath == null)
            {
                Debug.LogWarning("[BunnyMonster] " + def.Model + ".fbx not found, skipped.");
                return null;
            }
            string root = ParentFolder(ParentFolder(fbxPath));

            // ---- 1. model import settings: Generic rig, avatar from this model, named + looping clips
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.SaveAndReimport();

            var clips = new List<ModelImporterClipAnimation>();
            foreach (ModelImporterClipAnimation c in importer.defaultClipAnimations)
            {
                string n = c.takeName;
                int bar = n.LastIndexOf('|');
                if (bar >= 0) n = n.Substring(bar + 1);
                c.name = n;
                c.loopTime = LoopClips.Contains(n);
                clips.Add(c);
            }
            importer.clipAnimations = clips.ToArray();
            importer.SaveAndReimport();

            // ---- 2. make sure it is ~2.1 m tall (fixes unit mismatches automatically)
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            float height = MeasureHeight(model);
            if (height > 0.01f && (height < 1.2f || height > 4f))
            {
                importer.globalScale = importer.globalScale * (2.1f / height);
                importer.SaveAndReimport();
                model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                Debug.Log("[BunnyMonster] scale corrected: height " + height.ToString("0.00") + " -> 2.1 m");
            }

            // ---- 3. textures + material for the active render pipeline (Built-in / URP / HDRP)
            string texPath = FindAsset(def.Tex + "_albedo", "t:Texture2D");
            string nrmPath = FindAsset(def.Tex + "_normal", "t:Texture2D");
            string hpNrmPath = FindAsset(def.Tex + "_hp_normal", "t:Texture2D") ?? nrmPath;
            string mskPath = FindAsset(def.Tex + "_mask", "t:Texture2D");
            ConfigureTexture(nrmPath, TextureImporterType.NormalMap, false);
            ConfigureTexture(hpNrmPath, TextureImporterType.NormalMap, false);
            ConfigureTexture(mskPath, TextureImporterType.Default, true);
            Texture2D albedo = LoadTexture(texPath), mask = LoadTexture(mskPath);
            Material matLod0 = CreateOrUpdateMaterial(root, def.Prefab + "_LOD0_Mat", albedo, LoadTexture(hpNrmPath), mask);
            Material matLod1 = CreateOrUpdateMaterial(root, def.Prefab + "_LOD1_Mat", albedo, LoadTexture(nrmPath), mask);
            foreach (Material embedded in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>())
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name),
                                  embedded.name.Contains("LOD0") ? matLod0 : matLod1);
            importer.SaveAndReimport();

            // ---- 4. Animator Controller
            var clipMap = new Dictionary<string, AnimationClip>();
            foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>())
            {
                if (clip.name.StartsWith("__preview__")) continue;
                if (!clipMap.ContainsKey(clip.name)) clipMap.Add(clip.name, clip);
            }
            foreach (string n in RequiredClips)
            {
                if (!clipMap.ContainsKey(n))
                {
                    Fail(def.Model + ".fbx 안에 '" + n + "' 클립이 없어요. (찾은 클립: " + string.Join(", ", clipMap.Keys.ToArray()) + ")");
                    return null;
                }
            }
            AnimatorController controller = BuildController(root + "/" + def.Prefab + ".controller", clipMap);

            // ---- 5. prefab (variant of the model, keeps the link to the FBX)
            string prefabPath = root + "/" + def.Prefab + ".prefab";
            model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = def.Prefab;

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            foreach (SkinnedMeshRenderer smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.sharedMaterial = smr.name.EndsWith("_LOD0") ? matLod0 : matLod1;
                // off: the hidden LOD is not skinned every frame. Bounds get headroom for the attack/scream poses.
                smr.updateWhenOffscreen = false;
                Bounds lb = smr.localBounds;
                lb.Expand(lb.size * 0.3f);
                smr.localBounds = lb;
            }
            SetupLods(instance);

            CharacterController cc = instance.GetComponent<CharacterController>();
            if (cc == null) cc = instance.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 1.0f, 0f);
            cc.stepOffset = 0.3f;

            BunnyMonster monster = instance.GetComponent<BunnyMonster>();
            if (monster == null) monster = instance.AddComponent<BunnyMonster>();
            monster.walkSpeed = WalkSpeed;
            monster.runSpeed = RunSpeed;

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            Debug.Log("[BunnyMonster] Setup done: " + prefabPath);
            return prefabPath;
        }

        [MenuItem("Tools/Bunny Monster/2. Create Demo Scene")]
        public static void CreateDemoScene()
        {
            var prefabPaths = Monsters.Select(d => FindAsset(d.Prefab, "t:Prefab")).Where(p => p != null).ToList();
            if (prefabPaths.Count < Monsters.Count(d => FindAsset(d.Model, "t:Model") != null))
            {
                Setup();
                prefabPaths = Monsters.Select(d => FindAsset(d.Prefab, "t:Prefab")).Where(p => p != null).ToList();
            }
            if (prefabPaths.Count == 0) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string root = ParentFolder(prefabPaths[0]);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);

            // mart-like shelves to break line of sight
            for (int i = 0; i < 6; i++)
            {
                GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelf.name = "Shelf_" + i;
                shelf.transform.position = new Vector3(-10f + i * 4f, 1.25f, 4f + (i % 2) * 6f);
                shelf.transform.localScale = new Vector3(1.2f, 2.5f, 4f);
            }

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1.0f, 0f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            CharacterController pcc = player.AddComponent<CharacterController>();
            pcc.center = Vector3.zero;
            pcc.height = 2f;
            pcc.radius = 0.4f;
            BunnyDemoPlayer demoPlayer = player.AddComponent<BunnyDemoPlayer>();

            // the pair stands side by side, mirror images of each other (bunny on the left, twin on the right)
            for (int i = 0; i < prefabPaths.Count; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                var monsterGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                float x = (i - (prefabPaths.Count - 1) * 0.5f) * 2.4f;
                monsterGo.transform.position = new Vector3(x, 0f, 9f);
                monsterGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                BunnyMonster monster = monsterGo.GetComponent<BunnyMonster>();
                monster.target = player.transform;
                UnityEventTools.AddPersistentListener(monster.onAttackHit, demoPlayer.OnCaught);
                PrefabUtility.RecordPrefabInstancePropertyModifications(monster);
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = player.transform.position + demoPlayer.cameraOffset;
                cam.transform.LookAt(player.transform.position + Vector3.up);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            string scenePath = root + "/BunnyMonster_Demo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[BunnyMonster] Demo scene saved: " + scenePath + "  (Play: WASD/arrows, Shift = sprint)");
        }

        // ------------------------------------------------------------------ helpers
        static AnimatorController BuildController(string path, Dictionary<string, AnimationClip> clips)
        {
            AssetDatabase.DeleteAsset(path);
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Scream", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            BlendTree tree;
            AnimatorState locomotion = ctrl.CreateBlendTreeInController("Locomotion", out tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips["Idle"], 0f);
            tree.AddChild(clips["Walk"], WalkSpeed);
            tree.AddChild(clips["Run"], RunSpeed);
            sm.defaultState = locomotion;

            AnimatorState attack = sm.AddState("Attack", new Vector3(320f, 160f, 0f));
            attack.motion = clips["Attack"];
            AnimatorState scream = sm.AddState("Scream", new Vector3(320f, -40f, 0f));
            scream.motion = clips["Scream"];

            AddTriggered(sm, attack, locomotion, "Attack");
            AddTriggered(sm, scream, locomotion, "Scream");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        static void AddTriggered(AnimatorStateMachine sm, AnimatorState state, AnimatorState back, string trigger)
        {
            AnimatorStateTransition enter = sm.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.hasExitTime = false;
            enter.duration = 0.1f;
            enter.canTransitionToSelf = false;

            AnimatorStateTransition exit = state.AddTransition(back);
            exit.hasExitTime = true;
            exit.exitTime = 0.9f;
            exit.duration = 0.15f;
        }

        static Texture2D LoadTexture(string path)
        {
            return path != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(path) : null;
        }

        static void ConfigureTexture(string path, TextureImporterType type, bool linear)
        {
            if (path == null) return;
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            bool dirty = false;
            if (ti.textureType != type) { ti.textureType = type; dirty = true; }
            if (linear && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
            if (dirty) ti.SaveAndReimport();
        }

        // LOD0 = high-poly mesh (~30만 tris), LOD1 = low-poly mesh (~1.7만 tris). Unity usually builds the
        // LOD Group itself from the _LOD0/_LOD1 names; this makes sure it exists and has sensible distances.
        static void SetupLods(GameObject instance)
        {
            SkinnedMeshRenderer[] smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Renderer[] lod0 = smrs.Where(r => r.name.EndsWith("_LOD0")).Cast<Renderer>().ToArray();
            Renderer[] lod1 = smrs.Where(r => r.name.EndsWith("_LOD1")).Cast<Renderer>().ToArray();
            if (lod0.Length == 0 || lod1.Length == 0)
            {
                Debug.LogWarning("[BunnyMonster] _LOD0/_LOD1 meshes not found, LOD Group skipped.");
                return;
            }
            LODGroup group = instance.GetComponentInChildren<LODGroup>(true);
            if (group == null) group = instance.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(new[] { new LOD(Lod0ScreenHeight, lod0), new LOD(CullScreenHeight, lod1) });
            group.RecalculateBounds();
        }

        static Material CreateOrUpdateMaterial(string root, string matName, Texture2D albedo, Texture2D normal, Texture2D mask)
        {
            string folder = EnsureFolder(root, "Materials");
            string path = folder + "/" + matName + ".mat";
            Shader shader;
            string pipeline;
            PickShader(out shader, out pipeline);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            if (pipeline == "hdrp")
            {
                SetTexture(mat, "_BaseColorMap", albedo);
                if (normal != null && mat.HasProperty("_NormalMap"))
                {
                    mat.SetTexture("_NormalMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                    mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
                }
                if (mask != null && mat.HasProperty("_MaskMap"))
                {
                    mat.SetTexture("_MaskMap", mask);   // R metallic, G AO, B detail mask, A smoothness
                    mat.EnableKeyword("_MASKMAP");
                }
                SetFloat(mat, "_Metallic", 0f);
                // matte plush: no screen-space reflections
                if (mat.HasProperty("_ReceivesSSR"))
                {
                    mat.SetFloat("_ReceivesSSR", 0f);
                    mat.EnableKeyword("_DISABLE_SSR");
                }
            }
            else
            {
                // Built-in Standard and URP Lit use the same property names here
                SetTexture(mat, pipeline == "urp" ? "_BaseMap" : "_MainTex", albedo);
                SetTexture(mat, "_MainTex", albedo);
                if (normal != null && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normal);
                    SetFloat(mat, "_BumpScale", 1f);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (mask != null && mat.HasProperty("_MetallicGlossMap"))
                {
                    mat.SetTexture("_MetallicGlossMap", mask);  // smoothness from alpha
                    mat.EnableKeyword(pipeline == "urp" ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
                    SetFloat(mat, "_SmoothnessTextureChannel", 0f);
                    SetFloat(mat, "_GlossMapScale", 1f);
                    SetFloat(mat, "_Smoothness", 1f);
                }
                if (mask != null && mat.HasProperty("_OcclusionMap"))
                {
                    mat.SetTexture("_OcclusionMap", mask);      // occlusion is read from G
                    SetFloat(mat, "_OcclusionStrength", 1f);
                    if (pipeline == "urp") mat.EnableKeyword("_OCCLUSIONMAP");
                }
                SetFloat(mat, "_Metallic", 0f);
                // matte plush: no environment (sky/probe) reflections, which made the face look glossy
                if (pipeline == "urp")
                {
                    SetFloat(mat, "_EnvironmentReflections", 0f);
                    mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                }
                else
                {
                    SetFloat(mat, "_GlossyReflections", 0f);
                    mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                }
            }
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static void SetTexture(Material mat, string prop, Texture tex)
        {
            if (tex != null && mat.HasProperty(prop)) mat.SetTexture(prop, tex);
        }

        static void SetFloat(Material mat, string prop, float value)
        {
            if (mat.HasProperty(prop)) mat.SetFloat(prop, value);
        }

        static void PickShader(out Shader shader, out string pipeline)
        {
            RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
            string rpName = rp != null ? rp.GetType().Name : "";
            shader = null;
            pipeline = "builtin";
            if (rpName.Contains("Universal"))
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
                pipeline = "urp";
            }
            else if (rpName.Contains("HDRenderPipeline"))
            {
                shader = Shader.Find("HDRP/Lit");
                pipeline = "hdrp";
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
                pipeline = "builtin";
            }
        }

        static float MeasureHeight(GameObject model)
        {
            var tmp = (GameObject)PrefabUtility.InstantiatePrefab(model);
            float h = 0f;
            Renderer[] rends = tmp.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                foreach (Renderer r in rends) b.Encapsulate(r.bounds);
                h = b.size.y;
            }
            Object.DestroyImmediate(tmp);
            return h;
        }

        static string FindAsset(string name, string filter)
        {
            foreach (string guid in AssetDatabase.FindAssets(name + " " + filter))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(p) == name) return p;
            }
            return null;
        }

        static string ParentFolder(string path)
        {
            return Path.GetDirectoryName(path).Replace('\\', '/');
        }

        static string EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
            return path;
        }

        static void Fail(string message)
        {
            Debug.LogError("[BunnyMonster] " + message);
            EditorUtility.DisplayDialog("Bunny Monster", message, "OK");
        }
    }
}
#endif
