// LotteWorldMagicIslandEditor.cs  (v1 - Magic Island: turns the description in the other LotteWorldMagicIsland*.cs files into
// materials, mesh assets, prefabs and the scene Assets/Maps/LotteWorld/Map3_MagicIsland.unity)
//
// First time the project compiles with these files and the scene does not exist yet, everything is built automatically
// (without touching the scene you have open). Afterwards use the menu:
//   Tools > LotteWorld > Magic Island > Open Outdoor Scene
//   Tools > LotteWorld > Magic Island > Open Indoor + Outdoor Together      (Map3_LotteWorld + Map3_MagicIsland additive)
//   Tools > LotteWorld > Magic Island > Rebuild Scene (keep prefabs)       (layout numbers changed; your prefab edits stay)
//   Tools > LotteWorld > Magic Island > Rebuild All (overwrite prefabs)     (regenerates the prefabs too)
//   Tools > LotteWorld > Magic Island > Open Main Gate In Indoor Wall_02    (cuts the gate opening into the indoor wall; undoable)

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LotteWorld.MagicIsland
{
    public static class MagicIslandBuilder
    {
        public const string Root = "Assets/Maps/LotteWorld/MagicIsland";
        public const string ScenePath = "Assets/Maps/LotteWorld/Map3_MagicIsland.unity";
        public const string IndoorScenePath = "Assets/Maps/LotteWorld/Map3_LotteWorld.unity";

        static readonly Dictionary<M, Material> mats = new Dictionary<M, Material>();
        static readonly Dictionary<string, GameObject> prefabAssets = new Dictionary<string, GameObject>();
        static readonly HashSet<string> usedMeshPaths = new HashSet<string>();
        static int meshCount, triCount, doorCount;

        // ------------------------------------------------------------------ menus
        [MenuItem("Tools/LotteWorld/Magic Island/Open Outdoor Scene", false, 1)]
        static void OpenOutdoor()
        {
            if (!File.Exists(ScenePath)) { Build(false, false); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/LotteWorld/Magic Island/Open Indoor + Outdoor Together", false, 2)]
        static void OpenBoth()
        {
            if (!File.Exists(ScenePath)) Build(false, false);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(IndoorScenePath)) EditorSceneManager.OpenScene(IndoorScenePath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(ScenePath, File.Exists(IndoorScenePath) ? OpenSceneMode.Additive : OpenSceneMode.Single);
        }

        [MenuItem("Tools/LotteWorld/Magic Island/Rebuild Scene (keep prefabs)", false, 20)]
        static void RebuildScene()
        {
            if (EditorUtility.DisplayDialog("Magic Island", "Map3_MagicIsland.unity를 Layout 값으로 다시 만듭니다.\n씬에서 직접 옮긴 위치는 사라집니다(프리팹 수정은 유지).", "다시 만들기", "취소"))
                Build(false, false);
        }

        [MenuItem("Tools/LotteWorld/Magic Island/Rebuild All (overwrite prefabs)", false, 21)]
        static void RebuildAll()
        {
            if (EditorUtility.DisplayDialog("Magic Island", "프리팹·메시·재질과 씬을 전부 새로 만듭니다.\n프리팹을 직접 고친 내용도 덮어씁니다.", "전부 다시 만들기", "취소"))
                Build(true, false);
        }

        [MenuItem("Tools/LotteWorld/Magic Island/Open Main Gate In Indoor Wall_02", false, 40)]
        static void CutGate() { IndoorGateCutter.Run(); }

        // ------------------------------------------------------------------ build
        public static bool Build(bool overwritePrefabs, bool auto)
        {
            Scene prevActive = SceneManager.GetActiveScene();
            bool additive = true;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (string.IsNullOrEmpty(s.path) || s.path == ScenePath) additive = false;
            }
            if (!additive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            meshCount = triCount = doorCount = 0; usedMeshPaths.Clear(); prefabAssets.Clear(); mats.Clear();
            EnsureFolder(Root + "/Materials"); EnsureFolder(Root + "/Prefabs"); EnsureFolder(Root + "/Meshes");
            foreach (M k in System.Enum.GetValues(typeof(M))) Mat(k);
            AssetDatabase.DeleteAsset(Root + "/Meshes/Scene");
            if (overwritePrefabs) AssetDatabase.DeleteAsset(Root + "/Meshes/Prefabs");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);                         // temporary prefab objects live here, not in your open scene
            try
            {
                int n = 0;
                foreach (PrefabDef def in Layout.Prefabs)
                {
                    EditorUtility.DisplayProgressBar("Magic Island", "Prefab " + def.Key, (float)n++ / Layout.Prefabs.Count * 0.6f);
                    string path = Root + "/Prefabs/" + def.Folder + "/" + def.Key + ".prefab";
                    GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (existing != null && !overwritePrefabs) { prefabAssets[def.Key] = existing; continue; }
                    EnsureFolder(Root + "/Prefabs/" + def.Folder);
                    string meshDir = Root + "/Meshes/Prefabs/" + def.Key;
                    AssetDatabase.DeleteAsset(meshDir); EnsureFolder(meshDir);
                    Node node = def.Build();
                    node.Name = def.Key;
                    GameObject go = Make(node, null, meshDir);
                    prefabAssets[def.Key] = PrefabUtility.SaveAsPrefabAsset(go, path);
                    Object.DestroyImmediate(go);
                }

                EditorUtility.DisplayProgressBar("Magic Island", "Scene", 0.7f);
                EnsureFolder(Root + "/Meshes/Scene");
                Node root = Layout.BuildScene();
                GameObject rootGo = Make(root, null, Root + "/Meshes/Scene");
                rootGo.transform.SetPositionAndRotation(Layout.RootPosition, Quaternion.Euler(0f, Layout.RootYaw, 0f));
                SetupLighting(rootGo);
                EditorUtility.DisplayProgressBar("Magic Island", "Saving", 0.95f);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }
            finally { EditorUtility.ClearProgressBar(); }

            string msg = "Magic Island 야외맵 생성 완료: " + ScenePath + "\n프리팹 " + Layout.Prefabs.Count + "종, 새 메시 " + meshCount + "개(삼각형 " + triCount.ToString("N0") + "), 프리팹 속 문 " + doorCount + "개.\n" +
                         "실내와 같이 보려면 Tools > LotteWorld > Magic Island > Open Indoor + Outdoor Together.";
            Debug.Log(msg);
            if (additive)
            {
                EditorSceneManager.CloseScene(scene, true);
                if (prevActive.IsValid() && prevActive.isLoaded) SceneManager.SetActiveScene(prevActive);
                Object sa = AssetDatabase.LoadAssetAtPath<Object>(ScenePath);
                if (sa != null) EditorGUIUtility.PingObject(sa);
                if (EditorUtility.DisplayDialog("Magic Island", msg, "야외맵 씬 열기", "닫기") && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else FrameCastle();
            return true;
        }

        static void FrameCastle()
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null) return;
            Vector3 c = Layout.RootPosition + new Vector3(-70f, 10f, 120f);
            sv.LookAt(c, Quaternion.Euler(35f, 20f, 0f), 160f);
        }

        // ------------------------------------------------------------------ nodes -> GameObjects
        static GameObject Make(Node n, Transform parent, string meshDir)
        {
            GameObject go;
            if (!string.IsNullOrEmpty(n.Prefab))
            {
                GameObject pa;
                if (!prefabAssets.TryGetValue(n.Prefab, out pa) || pa == null) { Debug.LogWarning("Magic Island: missing prefab " + n.Prefab); go = new GameObject(n.Name); }
                else go = (GameObject)PrefabUtility.InstantiatePrefab(pa);
                go.name = n.Name;
            }
            else go = new GameObject(n.Name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = n.Pos;
            go.transform.localRotation = Quaternion.Euler(n.Euler);
            go.transform.localScale = Vector3.one * n.Scale;
            if (n.Name.StartsWith("Door_")) doorCount++;

            if (string.IsNullOrEmpty(n.Prefab))
            {
                if (n.Mesh != null && !n.Mesh.Empty)
                {
                    Mesh mesh; Material[] mm;
                    ToMesh(n.Mesh, n.Name, out mesh, out mm);
                    string path = meshDir + "/" + n.Name + ".asset"; int k = 2;
                    while (usedMeshPaths.Contains(path)) path = meshDir + "/" + n.Name + "_" + (k++) + ".asset";
                    usedMeshPaths.Add(path);
                    AssetDatabase.CreateAsset(mesh, path);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials = mm;
                    if (n.Collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
                    meshCount++; triCount += n.Mesh.TriangleCount;
                }
                if (n.Light != null)
                {
                    Light l = go.AddComponent<Light>();
                    l.type = n.Light.Directional ? LightType.Directional : LightType.Point;
                    l.color = n.Light.Color; l.intensity = n.Light.Intensity; l.range = n.Light.Range;
                    l.shadows = n.Light.Directional ? LightShadows.Soft : LightShadows.None;
                    l.enabled = n.Light.Enabled;
                }
                if (n.Name == "PreviewCamera")
                {
                    Camera cam = go.AddComponent<Camera>();
                    cam.depth = -10f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 1500f; cam.fieldOfView = 60f;
                }
                if (n.IsStatic && n.Mesh != null)
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            }
            foreach (Node c in n.Kids) Make(c, go.transform, meshDir);
            return go;
        }

        static void ToMesh(MeshBuilder b, string name, out Mesh mesh, out Material[] mm)
        {
            var keys = new List<M>();
            foreach (KeyValuePair<M, MeshBuilder.Part> kv in b.Parts) if (kv.Value.T.Count > 0) keys.Add(kv.Key);
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var subs = new List<List<int>>();
            mm = new Material[keys.Count];
            for (int s = 0; s < keys.Count; s++)
            {
                MeshBuilder.Part p = b.Parts[keys[s]]; int off = verts.Count;
                verts.AddRange(p.V); norms.AddRange(p.N); uvs.AddRange(p.UV);
                var tris = new List<int>(p.T.Count); foreach (int t in p.T) tris.Add(t + off);
                subs.Add(tris); mm[s] = Mat(keys[s]);
            }
            mesh = new Mesh { name = name, indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs);
            mesh.subMeshCount = keys.Count;
            for (int s = 0; s < subs.Count; s++) mesh.SetTriangles(subs[s], s, false);
            mesh.RecalculateBounds();
        }

        // ------------------------------------------------------------------ materials
        static Material Mat(M k)
        {
            Material mat;
            if (mats.TryGetValue(k, out mat) && mat != null) return mat;
            string path = Root + "/Materials/MI_" + k + ".mat";
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool urp = GraphicsSettings.currentRenderPipeline != null;
            if (mat == null)
            {
                Shader sh = urp ? Shader.Find("Universal Render Pipeline/Lit") : null;
                if (sh == null) sh = Shader.Find("Standard");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(sh) { name = "MI_" + k };
                AssetDatabase.CreateAsset(mat, path);
                Color c = Palette.Colors[k];
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", Palette.Smoothness(k));
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", Palette.Smoothness(k));
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", Palette.Metallic(k));
                if (Palette.Transparent(k)) MakeTransparent(mat);
                if (Palette.Emissive(k))
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 1.6f);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                EditorUtility.SetDirty(mat);
            }
            mats[k] = mat;
            return mat;
        }

        static void MakeTransparent(Material m)
        {
            if (m.HasProperty("_Surface"))                                                  // URP Lit
            {
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else if (m.HasProperty("_Mode"))                                                // Built-in Standard
            {
                m.SetFloat("_Mode", 3f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_ALPHABLEND_ON");
            }
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
        }

        static void SetupLighting(GameObject root)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0016f;
            RenderSettings.fogColor = new Color(0.74f, 0.77f, 0.80f);
            Material sky = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            if (sky != null) RenderSettings.skybox = sky;
            foreach (Light l in root.GetComponentsInChildren<Light>(true)) if (l.type == LightType.Directional) RenderSettings.sun = l;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    // ----------------------------------------------------------------------------------------------------------------------
    // Builds the outdoor map once, automatically, when the scene file does not exist yet.
    // ----------------------------------------------------------------------------------------------------------------------
    [InitializeOnLoad]
    static class MagicIslandAutoBuild
    {
        const string Key = "LotteWorld.MagicIsland.AutoBuildTried";
        static MagicIslandAutoBuild()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += Try;
        }
        static void Try()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Try; return; }
            if (File.Exists(MagicIslandBuilder.ScenePath) || SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, true);
            MagicIslandBuilder.Build(false, true);
        }
    }

    // ----------------------------------------------------------------------------------------------------------------------
    // Cuts the main-gate opening into the indoor outer wall Wall_02 (the team blockout wall is one solid box).
    // The original is kept, disabled, so this is fully reversible (and undoable). Run it with Map3_LotteWorld open.
    // ----------------------------------------------------------------------------------------------------------------------
    public static class IndoorGateCutter
    {
        const float HalfWidth = 7.9f, Height = 8.2f;

        public static void Run()
        {
            GameObject wall = null, gate = null;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i); if (!s.isLoaded) continue;
                foreach (GameObject r in s.GetRootGameObjects())
                {
                    foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Wall_02" && t.parent != null && t.parent.name == "OuterWalls" && t.gameObject.activeSelf) wall = t.gameObject;
                        if (t.name.StartsWith("LotteWorld_NorthGate")) gate = t.gameObject;
                    }
                }
            }
            if (wall == null) { EditorUtility.DisplayDialog("Magic Island", "활성화된 Blockout/Floor1/OuterWalls/Wall_02를 찾지 못했습니다.\nMap3_LotteWorld 씬을 먼저 여세요(이미 뚫었다면 원본은 꺼져 있습니다).", "확인"); return; }
            Transform w = wall.transform;
            Vector3 c = w.position, a = w.forward;                    // the wall's length runs along its local Z
            float L = w.lossyScale.z, H = w.lossyScale.y, T = w.lossyScale.x, y0 = c.y - H * 0.5f;
            Vector3 gp = gate != null ? gate.transform.position : Layout.RootPosition;
            float t0 = Vector3.Dot(gp - c, a);
            if (Mathf.Abs(t0) > L * 0.5f - HalfWidth) { EditorUtility.DisplayDialog("Magic Island", "정문 위치가 Wall_02 범위 밖입니다. 정문(LotteWorld_NorthGate) 위치를 확인하세요.", "확인"); return; }
            Undo.SetCurrentGroupName("Open main gate in Wall_02");
            float l0 = -L * 0.5f, l1 = t0 - HalfWidth, r0 = t0 + HalfWidth, r1 = L * 0.5f;
            Piece(wall, "Wall_02_L", c + a * ((l0 + l1) * 0.5f), new Vector3(T, H, l1 - l0));
            Piece(wall, "Wall_02_R", c + a * ((r0 + r1) * 0.5f), new Vector3(T, H, r1 - r0));
            Vector3 topC = c + a * t0; topC.y = (y0 + Height + y0 + H) * 0.5f;
            Piece(wall, "Wall_02_GateTop", topC, new Vector3(T, H - Height, HalfWidth * 2f));
            Undo.RecordObject(wall, "Disable Wall_02"); wall.SetActive(false);
            EditorSceneManager.MarkSceneDirty(wall.scene);
            Debug.Log("Magic Island: Wall_02에 정문 개구부(폭 " + (HalfWidth * 2f) + " m, 높이 " + Height + " m)를 냈습니다. 원본 Wall_02는 꺼 두었습니다. 씬 저장은 직접 하세요.");
        }

        static void Piece(GameObject src, string name, Vector3 pos, Vector3 worldSize)
        {
            GameObject p = Object.Instantiate(src, src.transform.parent);
            p.name = name;
            Vector3 ps = src.transform.parent != null ? src.transform.parent.lossyScale : Vector3.one;
            p.transform.position = pos;
            p.transform.localScale = new Vector3(worldSize.x / ps.x, worldSize.y / ps.y, worldSize.z / ps.z);
            p.SetActive(true);
            Undo.RegisterCreatedObjectUndo(p, "Create " + name);
        }
    }
}
#endif
