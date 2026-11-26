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
using System.IO.Compression;
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
        static int meshCount, triCount, doorCount, bakedCount;
        static bool buildingScene;

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

        [MenuItem("Tools/LotteWorld/Magic Island/Apply Baked Textures (keep scene edits)", false, 22)]
        public static void ApplyBakedAfterUnpack()
        {
            Scene act = SceneManager.GetActiveScene();
            if (act.path == ScenePath) { ApplyBaked(); return; }
            ApplyBaked();                                                // prefabs + ground materials (scene objects need the scene)
            if (File.Exists(ScenePath) && EditorUtility.DisplayDialog("Magic Island", "구운 재질을 프리팹과 땅에 적용했습니다.\n다리·선착장·트랙도 바꾸려면 야외맵 씬을 열어야 합니다.", "야외맵 열고 적용", "나중에") &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                ApplyBaked();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
        }

        static void ApplyBaked()
        {
            // 1) ground materials + prefabs that have a baked FBX (prefab GUIDs stay, so scene instances update in place)
            mats.Clear(); prefabAssets.Clear(); meshCount = triCount = doorCount = bakedCount = 0;
            foreach (M k in System.Enum.GetValues(typeof(M))) Mat(k);
            foreach (PrefabDef def in Layout.Prefabs)
            {
                if (!File.Exists(BakedFbx(def.Key))) continue;
                string path = Root + "/Prefabs/" + def.Folder + "/" + def.Key + ".prefab";
                EnsureFolder(Root + "/Prefabs/" + def.Folder);
                Node node = def.Build(); node.Name = def.Key; Node.SplitParts(node);
                EditorUtility.DisplayProgressBar("Magic Island", "Baked prefab " + def.Key, 0.5f);
                MakeBakedPrefab(def, node, path);
            }
            EditorUtility.ClearProgressBar();
            // 2) scene objects (bridge, dock, tracks) in the open outdoor scene: swap the solid mesh for the baked FBX, same place
            int swapped = 0;
            Scene sc = SceneManager.GetActiveScene();
            Node root = Layout.BuildScene(); Node.SplitParts(root);
            foreach (string nm in Layout.BakedSceneNodes)
            {
                if (!File.Exists(BakedSceneFbx(nm))) continue;
                GameObject target = null;
                foreach (GameObject r in sc.GetRootGameObjects())
                    foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
                        if (t.name == nm && !PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) target = t.gameObject;   // our plain holder, not the FBX inside it
                if (target == null) continue;
                Node n = FindNode(root, nm);
                GameObject model = PrepBaked(BakedSceneFbx(nm));
                if (model == null || n == null) continue;
                for (int i = target.transform.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(target.transform.GetChild(i).gameObject);
                foreach (Component c in target.GetComponents<Component>()) if (c is MeshFilter || c is MeshRenderer || c is MeshCollider) Undo.DestroyObjectImmediate(c);
                var bi = (GameObject)PrefabUtility.InstantiatePrefab(model);
                bi.transform.SetParent(target.transform, false);
                Undo.RegisterCreatedObjectUndo(bi, "Baked " + nm);
                Transform inner = bi.transform.Find(nm); if (inner != null) ApplyNodeInfo(inner, n);
                swapped++;
            }
            if (swapped > 0) EditorSceneManager.MarkSceneDirty(sc);
            if (sc.path == ScenePath && bakedCount > 0) RepairPlacement();                 // instance overrides do not survive the prefab swap
            AssetDatabase.SaveAssets();
            Debug.Log("Magic Island: 구운 프리팹 " + bakedCount + "개 적용, 씬 오브젝트 " + swapped + "개 교체(씬 저장은 직접). 땅 재질 텍스처 적용.");
        }

        // ------------------------------------------------------------------ placement repair
        /// <summary>Re-creates every prefab instance under the MagicIsland root of the open outdoor scene from the Layout (name,
        /// position, rotation). Needed after a prefab is replaced by its baked FBX variant: the old instance overrides point at
        /// the old prefab root and are dropped, so the instances fall back to the origin. Non-prefab objects are left alone.</summary>
        [MenuItem("Tools/LotteWorld/Magic Island/Repair Prefab Placement", false, 23)]
        public static void RepairPlacement()
        {
            Scene sc = SceneManager.GetActiveScene();
            GameObject root = null;
            foreach (GameObject r in sc.GetRootGameObjects()) if (r.name == "MagicIsland") root = r;
            if (root == null) { Debug.LogWarning("Magic Island: 야외맵 씬(MagicIsland 루트)을 열고 실행하세요."); return; }
            int removed = 0, placed = 0;
            var kill = new List<GameObject>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                string ap = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                if (ap != null && ap.StartsWith(Root + "/Prefabs/")) kill.Add(t.gameObject);
            }
            foreach (GameObject g in kill) { Undo.DestroyObjectImmediate(g); removed++; }
            Node tree = Layout.BuildScene(); Node.SplitParts(tree);
            var stack = new List<KeyValuePair<Node, Transform>> { new KeyValuePair<Node, Transform>(tree, root.transform) };
            while (stack.Count > 0)
            {
                KeyValuePair<Node, Transform> cur = stack[stack.Count - 1]; stack.RemoveAt(stack.Count - 1);
                foreach (Node c in cur.Key.Kids)
                {
                    if (!string.IsNullOrEmpty(c.Prefab))
                    {
                        GameObject pa = null;
                        foreach (PrefabDef d in Layout.Prefabs) if (d.Key == c.Prefab) pa = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + d.Folder + "/" + d.Key + ".prefab");
                        if (pa == null) { Debug.LogWarning("Magic Island: 프리팹 없음 " + c.Prefab); continue; }
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(pa, sc);
                        go.name = c.Name;
                        go.transform.SetParent(cur.Value, false);
                        PlaceInstance(go, pa, c);
                        Undo.RegisterCreatedObjectUndo(go, "Place " + c.Name);
                        placed++;
                        continue;
                    }
                    Transform ct = cur.Value.Find(c.Name);
                    if (ct != null) stack.Add(new KeyValuePair<Node, Transform>(c, ct));
                }
            }
            EditorSceneManager.MarkSceneDirty(sc);
            Debug.Log("Magic Island: 프리팹 배치 복구 — 지운 것 " + removed + "개, 다시 놓은 것 " + placed + "개.");
        }

        // ------------------------------------------------------------------ 11/20 layout fix (keeps other scene roots)
        /// <summary>Rebuilds only the MagicIsland root of the outdoor scene from the current Layout (merged walkways, lawns, props,
        /// moved buildings, rerouted coasters, dock) in place. Other roots in the scene (LotteWorld_LotteTower ...) stay. Prefab
        /// assets are reused as they are; scene objects with a baked FBX (bridge, dock, tracks) use it.</summary>
        [MenuItem("Tools/LotteWorld/Magic Island/Apply Layout Fix (keep Lotte Tower)", false, 24)]
        public static void ApplyLayoutFix()
        {
            Scene sc = SceneManager.GetActiveScene();
            if (sc.path != ScenePath)
            {
                if (!EditorUtility.DisplayDialog("Magic Island", "야외맵 씬(Map3_MagicIsland)에서 실행해야 합니다. 지금 열까요?", "열기", "취소")) return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                sc = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            if (!EditorUtility.DisplayDialog("Magic Island",
                "MagicIsland 루트를 새 배치(11/20: 길·잔디·소품·건물 위치·트랙·선착장)로 다시 만듭니다.\n" +
                "롯데타워 등 다른 루트는 그대로 둡니다.\n\nUndo는 안 됩니다. 마음에 안 들면 씬을 저장하지 말고 다시 여세요. 씬 저장은 직접 하세요.", "진행", "취소")) return;

            meshCount = triCount = doorCount = bakedCount = 0; usedMeshPaths.Clear(); prefabAssets.Clear(); mats.Clear();
            foreach (M k in System.Enum.GetValues(typeof(M))) Mat(k);
            var missing = new List<string>();
            foreach (PrefabDef def in Layout.Prefabs)
            {
                GameObject pa = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + def.Folder + "/" + def.Key + ".prefab");
                if (pa == null) missing.Add(def.Key); else prefabAssets[def.Key] = pa;
            }
            if (missing.Count > 0) { Debug.LogError("Magic Island: 프리팹이 없습니다 (" + string.Join(", ", missing.ToArray()) + "). Rebuild All을 먼저 하세요."); return; }

            GameObject old = null;
            foreach (GameObject r in sc.GetRootGameObjects()) if (r.name == "MagicIsland") old = r;
            int sibling = old != null ? old.transform.GetSiblingIndex() : 0;
            if (old != null) Object.DestroyImmediate(old);
            string meshDir = Root + "/Meshes/Scene";
            AssetDatabase.DeleteAsset(meshDir); EnsureFolder(meshDir);
            try
            {
                EditorUtility.DisplayProgressBar("Magic Island", "Layout fix 11/20", 0.4f);
                Node root = Layout.BuildScene(); Node.SplitParts(root);
                buildingScene = true;
                GameObject rootGo = Make(root, null, meshDir);
                buildingScene = false;
                rootGo.transform.SetPositionAndRotation(Layout.RootPosition, Quaternion.Euler(0f, Layout.RootYaw, 0f));
                rootGo.transform.SetSiblingIndex(sibling);
                SetupLighting(rootGo);
                AssetDatabase.SaveAssets();
            }
            finally { buildingScene = false; EditorUtility.ClearProgressBar(); }
            EditorSceneManager.MarkSceneDirty(sc);
            Debug.Log("Magic Island 11/20 배치 수정 적용: 새 메시 " + meshCount + "개(삼각형 " + triCount.ToString("N0") + "), 구운 씬 FBX " + bakedCount +
                      "개 사용. 롯데타워 등 다른 루트는 유지. 확인 후 씬을 저장하세요. (밤 조명을 쓰던 중이면 Night 메뉴의 Set Night를 다시 누르세요)");
        }

        /// <summary>Place a prefab instance on top of the prefab root's own transform: a baked FBX whose file has a single top
        /// object keeps the FBX axis turn (e.g. 90 deg about X) on the root, and overwriting it lays the object on its side.</summary>
        static void PlaceInstance(GameObject go, GameObject prefab, Node n)
        {
            Transform pr = prefab.transform;
            Quaternion yaw = Quaternion.Euler(n.Euler);
            go.transform.localRotation = yaw * pr.localRotation;
            go.transform.localPosition = n.Pos + yaw * pr.localPosition;
            go.transform.localScale = pr.localScale * n.Scale;
        }

        static Node FindNode(Node n, string name) { if (n.Name == name) return n; foreach (Node c in n.Kids) { Node r = FindNode(c, name); if (r != null) return r; } return null; }

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

            meshCount = triCount = doorCount = bakedCount = 0; buildingScene = false; usedMeshPaths.Clear(); prefabAssets.Clear(); mats.Clear();
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
                    bool baked = File.Exists(BakedFbx(def.Key));
                    bool upgrade = baked && existing != null && !PrefabUtility.IsPartOfVariantPrefab(existing);   // old solid-colour prefab -> baked
                    if (existing != null && !overwritePrefabs && !upgrade) { prefabAssets[def.Key] = existing; continue; }
                    EnsureFolder(Root + "/Prefabs/" + def.Folder);
                    string meshDir = Root + "/Meshes/Prefabs/" + def.Key;
                    AssetDatabase.DeleteAsset(meshDir); EnsureFolder(meshDir);
                    Node node = def.Build();
                    node.Name = def.Key;
                    Node.SplitParts(node);                               // every named part = its own GameObject
                    if (baked)
                    {
                        GameObject bp = MakeBakedPrefab(def, node, path);
                        if (bp != null) { prefabAssets[def.Key] = bp; continue; }
                    }
                    GameObject go = Make(node, null, meshDir);
                    prefabAssets[def.Key] = PrefabUtility.SaveAsPrefabAsset(go, path);
                    Object.DestroyImmediate(go);
                }

                EditorUtility.DisplayProgressBar("Magic Island", "Scene", 0.7f);
                EnsureFolder(Root + "/Meshes/Scene");
                Node root = Layout.BuildScene();
                Node.SplitParts(root);
                buildingScene = true;
                GameObject rootGo = Make(root, null, Root + "/Meshes/Scene");
                buildingScene = false;
                rootGo.transform.SetPositionAndRotation(Layout.RootPosition, Quaternion.Euler(0f, Layout.RootYaw, 0f));
                SetupLighting(rootGo);
                EditorUtility.DisplayProgressBar("Magic Island", "Saving", 0.95f);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }
            finally { EditorUtility.ClearProgressBar(); }

            string msg = "Magic Island 야외맵 생성 완료: " + ScenePath + "\n프리팹 " + Layout.Prefabs.Count + "종, 새 메시 " + meshCount + "개(삼각형 " + triCount.ToString("N0") + "), 프리팹 속 문 " + doorCount + "개, 구운 FBX 사용 " + bakedCount + "개.\n" +
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
            if (!string.IsNullOrEmpty(n.Prefab) && prefabAssets.ContainsKey(n.Prefab) && prefabAssets[n.Prefab] != null) PlaceInstance(go, prefabAssets[n.Prefab], n);
            else
            {
                go.transform.localPosition = n.Pos;
                go.transform.localRotation = Quaternion.Euler(n.Euler);
                go.transform.localScale = Vector3.one * n.Scale;
            }
            if (n.Name.StartsWith("Door_")) doorCount++;

            if (buildingScene && System.Array.IndexOf(Layout.BakedSceneNodes, n.Name) >= 0 && File.Exists(BakedSceneFbx(n.Name)))
            {
                GameObject model = PrepBaked(BakedSceneFbx(n.Name));
                if (model != null)
                {
                    var bi = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    bi.transform.SetParent(go.transform, false);
                    Transform inner = bi.transform.Find(n.Name);
                    if (inner != null) ApplyNodeInfo(inner, n);
                    bakedCount++;
                    return go;
                }
            }

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
                    // 구도 확인용이라 꺼 둔다. 켜 두면 플레이 중 MainCamera 밑에서 맵 전체를 한 번 더 그려 프레임이 두 배 넘게 느려진다.
                    cam.enabled = false;
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
            Dictionary<M, MeshBuilder.Part> parts = b.Parts;
            foreach (KeyValuePair<M, MeshBuilder.Part> kv in parts) if (kv.Value.T.Count > 0) keys.Add(kv.Key);
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var subs = new List<List<int>>();
            mm = new Material[keys.Count];
            for (int s = 0; s < keys.Count; s++)
            {
                MeshBuilder.Part p = parts[keys[s]]; int off = verts.Count;
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
        /// <summary>Tile size (metres) of the seamless ground textures in MagicIsland/Textures/MI_*.png (mesh UV unit = 2 m).</summary>
        static readonly Dictionary<M, float> TileMetres = new Dictionary<M, float>
        {
            { M.Paving, 4f }, { M.PavingDark, 4f }, { M.StoneGrey, 4f }, { M.StoneDark, 4f },
            { M.Grass, 8f }, { M.GrassDark, 8f }, { M.Asphalt, 8f }, { M.LakeBed, 8f }, { M.Water, 8f },
        };

        static Material Mat(M k)
        {
            Material mat;
            if (mats.TryGetValue(k, out mat) && mat != null) return mat;
            string path = Root + "/Materials/MI_" + k + ".mat";
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(LitShader()) { name = "MI_" + k };
                AssetDatabase.CreateAsset(mat, path);
            }
            Color c = Palette.Colors[k];
            Texture2D baseTex = null, nrm = null; float tile;
            if (TileMetres.TryGetValue(k, out tile))
            {
                baseTex = Tex(Root + "/Textures/MI_" + k + ".png", false);
                nrm = Tex(Root + "/Textures/MI_" + k + "_Normal.png", true);
                Vector2 sc = Vector2.one * (2f / tile);
                if (baseTex != null) { c = new Color(1f, 1f, 1f, c.a); SetTex(mat, "_BaseMap", baseTex, sc); SetTex(mat, "_MainTex", baseTex, sc); }
                if (nrm != null) { SetTex(mat, "_BumpMap", nrm, sc); mat.EnableKeyword("_NORMALMAP"); if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", k == M.Water ? 0.35f : 0.8f); }
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", Palette.Smoothness(k));
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", Palette.Smoothness(k));
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", Palette.Metallic(k));
            LotteWorld.Look.LotteWorldSurfaceMaps.Attach(mat);   // 땅 텍스처 옆에 _MS 가 있으면 붙인다 (_Smoothness 1)
            if (Palette.Transparent(k)) MakeTransparent(mat);
            if (Palette.Emissive(k))
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Palette.Colors[k] * 1.6f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);
            mats[k] = mat;
            return mat;
        }

        static Shader LitShader()
        {
            Shader sh = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : null;
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            return sh;
        }

        static void SetTex(Material m, string prop, Texture t, Vector2 scale)
        {
            if (!m.HasProperty(prop)) return;
            m.SetTexture(prop, t); m.SetTextureScale(prop, scale);
        }

        /// <summary>Load a texture, fixing its import settings first (normal map / linear / repeat).</summary>
        static Texture2D Tex(string path, bool normal, bool linear = false)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return null;
            bool dirty = false;
            if (normal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
            if (!normal && linear && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
            if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
            if (LotteWorldTextureLimits.Apply(ti, path, 4096)) dirty = true;   // _MS 512, _Normal 1024, 나머지는 4096 까지
            if (dirty) ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ baked FBX (worn textures, parts kept separate)
        public static string BakedFbx(string key) { return Root + "/Baked/" + key + "/" + key + ".fbx"; }
        public static string BakedSceneFbx(string name) { return Root + "/Baked/Scene/" + name + "/" + name + ".fbx"; }

        /// <summary>Import settings + URP materials for one baked FBX. Material 'X' in the FBX gets Textures/X_{BaseColor,Normal,MS}.png.</summary>
        static GameObject PrepBaked(string fbx)
        {
            var mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (mi == null) return null;
            string dir = Path.GetDirectoryName(fbx).Replace('\\', '/');
            bool dirty = false;
            if (!mi.bakeAxisConversion) { mi.bakeAxisConversion = true; dirty = true; }
            if (mi.importCameras || mi.importLights) { mi.importCameras = false; mi.importLights = false; dirty = true; }
            if (mi.importAnimation) { mi.importAnimation = false; dirty = true; }
            if (mi.materialImportMode == ModelImporterMaterialImportMode.None) { mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; dirty = true; }
            if (dirty) mi.SaveAndReimport();
            var names = new HashSet<string>();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbx)) if (o is Material) names.Add(o.name);
            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> kv in mi.GetExternalObjectMap()) if (kv.Key.type == typeof(Material)) names.Add(kv.Key.name);
            EnsureFolder(dir + "/Materials");
            bool remapped = false;
            foreach (string nm in names)
            {
                Material mat;
                if (nm.EndsWith("_LampGlow")) mat = Mat(M.LampGlow);
                else
                {
                    string mp = dir + "/Materials/" + nm + ".mat";
                    mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                    if (mat == null) { mat = new Material(LitShader()) { name = nm }; AssetDatabase.CreateAsset(mat, mp); }
                    Texture2D bc = Tex(dir + "/Textures/" + nm + "_BaseColor.png", false), nmTex = Tex(dir + "/Textures/" + nm + "_Normal.png", true), ms = Tex(dir + "/Textures/" + nm + "_MS.png", false, true);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                    if (bc != null) { SetTex(mat, "_BaseMap", bc, Vector2.one); SetTex(mat, "_MainTex", bc, Vector2.one); }
                    if (nmTex != null) { SetTex(mat, "_BumpMap", nmTex, Vector2.one); mat.EnableKeyword("_NORMALMAP"); }
                    if (ms != null)
                    {
                        SetTex(mat, "_MetallicGlossMap", ms, Vector2.one); mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f);
                        if (mat.HasProperty("_GlossMapScale")) mat.SetFloat("_GlossMapScale", 1f);
                        if (mat.HasProperty("_SmoothnessTextureChannel")) mat.SetFloat("_SmoothnessTextureChannel", 0f);
                    }
                    EditorUtility.SetDirty(mat);
                }
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), nm);
                Object cur;
                if (!mi.GetExternalObjectMap().TryGetValue(id, out cur) || cur != mat) { mi.AddRemap(id, mat); remapped = true; }
            }
            if (remapped) mi.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        }

        /// <summary>Colliders, static flags and lights from the node tree onto the FBX instance (matched by child name).</summary>
        static void ApplyNodeInfo(Transform t, Node n)
        {
            foreach (Node c in n.Kids)
            {
                Transform ct = t.Find(c.Name);
                if (ct == null) { Debug.LogWarning("Magic Island: baked FBX has no part '" + c.Name + "' under " + t.name); continue; }
                MeshFilter mf = ct.GetComponent<MeshFilter>();
                if (mf != null && c.Collider && ct.GetComponent<MeshCollider>() == null) ct.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                if (mf != null && c.IsStatic) GameObjectUtility.SetStaticEditorFlags(ct.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
                if (c.Name.StartsWith("Door_")) doorCount++;
                if (mf != null) { meshCount++; }
                if (c.Light != null && ct.GetComponent<Light>() == null)
                {
                    Light l = ct.gameObject.AddComponent<Light>();
                    l.type = c.Light.Directional ? LightType.Directional : LightType.Point;
                    l.color = c.Light.Color; l.intensity = c.Light.Intensity; l.range = c.Light.Range; l.shadows = LightShadows.None; l.enabled = c.Light.Enabled;
                }
                ApplyNodeInfo(ct, c);
            }
        }

        static GameObject MakeBakedPrefab(PrefabDef def, Node node, string path)
        {
            GameObject model = PrepBaked(BakedFbx(def.Key));
            if (model == null) return null;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.name = def.Key;
            ApplyNodeInfo(inst.transform, node);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(inst, path);
            Object.DestroyImmediate(inst);
            bakedCount++;
            return saved;
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
        const string Zip = MagicIslandBuilder.Root + "/MagicIsland_Baked.zip";
        const string RepairFlag = MagicIslandBuilder.Root + "/_RepairPlacement.txt";

        /// <summary>One-shot: open the outdoor scene, put every prefab instance back where the Layout says, save, delete the flag.</summary>
        static void AutoRepair()
        {
            AssetDatabase.DeleteAsset(RepairFlag);
            if (File.Exists(RepairFlag)) File.Delete(RepairFlag);
            if (SceneManager.GetActiveScene().path != MagicIslandBuilder.ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(MagicIslandBuilder.ScenePath, OpenSceneMode.Single);
            }
            MagicIslandBuilder.RepairPlacement();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        static void Try()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Try; return; }
            if (File.Exists(Zip)) { Unpack(); return; }
            if (File.Exists(RepairFlag)) { AutoRepair(); return; }
            if (File.Exists(MagicIslandBuilder.ScenePath) || SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, true);
            MagicIslandBuilder.Build(false, true);
        }

        /// <summary>Baked FBX + textures arrive as one zip (hundreds of files): unpack it into MagicIsland/, delete it, then switch
        /// the prefabs to the baked versions.</summary>
        static void Unpack()
        {
            string root = MagicIslandBuilder.Root; int n = 0;
            try
            {
                using (var za = new ZipArchive(File.OpenRead(Zip), ZipArchiveMode.Read))
                    foreach (ZipArchiveEntry e in za.Entries)
                    {
                        if (string.IsNullOrEmpty(e.Name)) continue;
                        string dst = Path.Combine(root, e.FullName.Replace('\\', '/'));
                        Directory.CreateDirectory(Path.GetDirectoryName(dst));
                        using (Stream i = e.Open()) using (FileStream o = File.Create(dst)) i.CopyTo(o);
                        n++;
                    }
            }
            catch (System.Exception ex) { Debug.LogError("Magic Island: zip 풀기 실패 " + ex.Message); return; }
            AssetDatabase.DeleteAsset(Zip);
            if (File.Exists(Zip)) File.Delete(Zip);
            AssetDatabase.Refresh();
            Debug.Log("Magic Island: 구운 파일 " + n + "개를 풀었습니다.");
            EditorApplication.delayCall += () => MagicIslandBuilder.ApplyBakedAfterUnpack();
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
