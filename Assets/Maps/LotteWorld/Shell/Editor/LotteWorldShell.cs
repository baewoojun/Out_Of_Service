// LotteWorldShell.cs (v1.1 - 11/18 ParadeLoop 자식 합치기 + 반폭 검사) 롯데월드 실내 셸: 바닥 · 퍼레이드 길 · 링크 둘레 · 둘레 벽 파사드 · 평천장 · 유리 돔(3배) · 기둥(독립)
// 넣는 곳: Assets/Maps/LotteWorld/Shell/Editor/ (이 파일 + LotteWorldShellGeo.cs), 텍스처는 Assets/Maps/LotteWorld/Shell/Textures/
// 메뉴: Tools > LotteWorld > Shell (Floor · Walls · Ceiling · Dome)
//  - [전부 만들기] -> 씬 루트 LotteWorld_Shell 아래 Floor / Rink / Facades / Ceiling / Dome (부위별로 다시 만들기·지우기 가능)
//  - 기둥은 따로: 씬 루트 LotteWorld_Shell_Columns 아래 프리팹(Shell/Prefabs/LW_Shell_Column) 하나. 셸을 다시 만들어도 안 건드림
//    -> 그냥 선택해서 옮기고 Ctrl+D 로 복사. 높이를 바꾸면 [기둥 높이 맞추기]로 프리팹 메시만 바뀌어 전부 따라감
//  - 메시는 Shell/Meshes/*.asset 로 저장(씬 파일이 커지지 않게). 바닥만 Mesh Collider
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LotteWorld.Shell
{
    public class ShellWindow : EditorWindow
    {
        const string Dir = "Assets/Maps/LotteWorld/Shell";
        const string RootName = "LotteWorld_Shell", ColRootName = "LotteWorld_Shell_Columns";
        static readonly string[] Parts = { "Floor", "Rink", "Facades", "Ceiling", "Dome" };
        ShellSettings S = new ShellSettings();
        int seed = 11;
        Vector2 scroll;

        [MenuItem("Tools/LotteWorld/Shell (Floor · Walls · Ceiling · Dome)")]
        public static void Open() { GetWindow<ShellWindow>("LW Shell"); }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            S.H = EditorGUILayout.FloatField("평천장 높이 (m)", S.H);
            S.DomeK = EditorGUILayout.FloatField("돔 넓이 배수", S.DomeK);
            EditorGUILayout.LabelField(string.Format("  돔 {0:F0} x {1:F0} m, 높이 +{2:F0} m", S.DomeHX * 2, S.DomeHZ * 2, S.DomeRise), EditorStyles.miniLabel);
            S.LoopC = EditorGUILayout.Vector2Field("퍼레이드 루프 중심 (x, z)", S.LoopC);
            S.LoopHX = EditorGUILayout.FloatField("루프 반폭 X", S.LoopHX);
            S.LoopHZ = EditorGUILayout.FloatField("루프 반폭 Z", S.LoopHZ);
            if (GUILayout.Button("루프 = 씬의 ParadeLoop 에서 읽기")) ReadLoop();
            S.Ice = EditorGUILayout.Toggle("얼음판도 만들기", S.Ice);
            EditorGUILayout.Space();
            if (GUILayout.Button("전부 만들기", GUILayout.Height(30))) { foreach (string p in Parts) Build(p); EnsureColumn(); Finish(); }
            foreach (string p in Parts)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(p, GUILayout.Width(80));
                if (GUILayout.Button("만들기")) { Build(p); Finish(); }
                if (GUILayout.Button("지우기", GUILayout.Width(60))) { ClearPart(p); Finish(); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("기둥 (독립 오브젝트)", EditorStyles.boldLabel);
            if (GUILayout.Button("기둥 하나 놓기")) { EnsureColumn(true); Finish(); }
            if (GUILayout.Button("기둥 높이 맞추기 (프리팹 메시 = 평천장 높이)")) { ColumnPrefab(true); Finish(); }
            EditorGUILayout.Space();
            if (GUILayout.Button("셸 전부 지우기 (기둥은 남김)")) { GameObject r = Find(RootName); if (r) Undo.DestroyObjectImmediate(r); Finish(); }
            EditorGUILayout.HelpBox("저장은 직접(Ctrl+S). 팀 블록아웃 바닥과 겹치면 셸 바닥이 2 cm 위에 있습니다. 천장 조명판은 발광 재질이라 실제로 비추려면 라이트를 따로 두거나 GI를 구우세요.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ helpers
        static GameObject Find(string name)
        {
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects()) if (g.name == name) return g;
            return null;
        }
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        static void Finish() { AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); }
        /// 퍼레이드 루프 찾기 (셸이 만든 것은 제외)
        /// 0) 이름 ParadeLoop 인 오브젝트(렌더러 없는 부모): 자식 렌더러(Parade_*) bounds 를 전부 합침  1) 없으면 IceRink 의 Ice (+0.3 m = 노란 선)
        /// 반폭이 기대값(44.85 x 29.90) ±3 m 를 벗어나면 숫자를 바꾸지 않고 경고만 (콘솔에 조각 목록을 찍음)
        void ReadLoop()
        {
            Bounds b = new Bounds(); bool found = false; string src = ""; float grow = 0f; int parts = 0;
            var sb = new System.Text.StringBuilder();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name != "ParadeLoop" || t.root.name.StartsWith(RootName)) continue;
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                {
                    if (!found) { b = r.bounds; found = true; } else b.Encapsulate(r.bounds);
                    parts++;
                    sb.AppendLine("  " + PathOf(r.transform) + "  중심 (" + r.bounds.center.x.ToString("F2") + ", " + r.bounds.center.z.ToString("F2") + ")  크기 " +
                                  r.bounds.size.x.ToString("F2") + " x " + r.bounds.size.z.ToString("F2"));
                }
                if (found) { src = PathOf(t) + " (렌더러 " + parts + "개 합침)"; break; }
            }
            if (!found)
                foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (r.transform.root.name.StartsWith(RootName)) continue;
                    string par = r.transform.parent != null ? r.transform.parent.name : "";
                    if (r.name == "Ice" || (r.name.Contains("Ice") && par.Contains("IceRink"))) { b = r.bounds; found = true; grow = 0.3f; src = PathOf(r.transform) + " (얼음판 + 0.3 m)"; break; }
                }
            if (!found) { Debug.LogWarning("[Shell] ParadeLoop / Ice 를 못 찾음 — 창의 숫자를 직접 넣으세요"); return; }
            float hx = b.extents.x + grow, hz = b.extents.z + grow;
            string msg = "[Shell] 루프 = '" + src + "': 중심 (" + b.center.x.ToString("F2") + ", " + b.center.z.ToString("F2") + "), 반폭 " +
                         hx.ToString("F2") + " x " + hz.ToString("F2") + "\n" + sb;
            if (Mathf.Abs(hx - 44.85f) > 3f || Mathf.Abs(hz - 29.9f) > 3f)
            { Debug.LogWarning(msg + "\n→ 반폭이 기대값 44.85 x 29.90 (±3 m) 과 달라서 창의 숫자를 바꾸지 않았음. 이 로그를 그대로 보내 주세요"); return; }
            S.LoopC = new Vector2(b.center.x, b.center.z); S.LoopHX = hx; S.LoopHZ = hz;
            Debug.Log(msg + "\n→ 창에 반영함. 맞으면 [전부 만들기] (또는 Floor·Rink·Ceiling·Dome 다시 만들기)");
        }
        static string PathOf(Transform t) { string p = t.name; for (Transform q = t.parent; q != null; q = q.parent) p = q.name + "/" + p; return p; }

        static Shader Lit() { return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"); }
        static readonly Dictionary<string, string> TexOf = new Dictionary<string, string>
        {
            { "FloorBrick", "LW_Shell_FloorBrick" }, { "ParadeBrick", "LW_Shell_ParadeBrick" }, { "StoneBand", "LW_Shell_StoneBand" }, { "Plaster", "LW_Shell_Plaster" },
            { "Teal", "LW_Shell_Teal" }, { "White", "LW_Shell_White" }, { "Ceiling", "LW_Shell_Ceiling" }, { "CeilBeam", "LW_Shell_Ceiling" }, { "Plinth", "LW_Shell_Plinth" },
        };
        static Color ColOf(string m)
        {
            switch (m)
            {
                case "CeilBeam": return new Color(0.85f, 0.83f, 0.8f);
                case "YellowLine": return new Color(0.85f, 0.65f, 0.12f);
                case "Recess": return new Color(0.05f, 0.05f, 0.06f);
                case "WinDark": return new Color(0.06f, 0.07f, 0.08f);
                case "SteelWhite": return new Color(0.82f, 0.82f, 0.8f);
                case "DomeGlass": return new Color(0.6f, 0.78f, 0.8f, 0.28f);
                case "RailGlass": return new Color(0.8f, 0.9f, 0.9f, 0.25f);
                case "DomeDirty": return new Color(0.28f, 0.3f, 0.28f);
                case "Ice": return new Color(0.72f, 0.85f, 0.92f);
                case "RoofBlue": return new Color(0.15f, 0.32f, 0.55f);
                case "FadedRed": return new Color(0.62f, 0.16f, 0.1f);
                case "FadedOrange": return new Color(0.78f, 0.42f, 0.14f);
                case "Gold": return new Color(0.6f, 0.45f, 0.2f);
                case "Wicker": return new Color(0.4f, 0.28f, 0.15f);
                case "LampOn": return new Color(1f, 0.8f, 0.5f);
                case "CeilLightOn": return new Color(1f, 0.93f, 0.82f);
                case "CeilLightOff": return new Color(0.55f, 0.55f, 0.53f);
                default: return Color.white;
            }
        }
        static Material Mat(string m)
        {
            EnsureFolder(Dir + "/Materials");
            string p = Dir + "/Materials/LW_Shell_" + m + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (mat == null) { mat = new Material(Lit()); AssetDatabase.CreateAsset(mat, p); }
            mat.shader = Lit(); Color c = ColOf(m); mat.SetColor("_BaseColor", c);
            string tex; mat.SetFloat("_Smoothness", m == "Ice" || m.Contains("Glass") ? 0.9f : 0.15f);
            mat.SetFloat("_Metallic", m == "SteelWhite" || m == "Gold" ? 0.5f : 0f);
            if (TexOf.TryGetValue(m, out tex))
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/Textures/" + tex + ".png");
                if (t) mat.SetTexture("_BaseMap", t); else Debug.LogWarning("[Shell] 텍스처 없음: " + tex);
            }
            if (m.Contains("Glass"))
            {
                mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = (int)RenderQueue.Transparent; mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetFloat("_Cull", 0f);                                         // 양면
            }
            if (m == "LampOn" || m == "CeilLightOn")
            {
                mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", c * (m == "LampOn" ? 3f : 2f));
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            LotteWorld.Look.LotteWorldSurfaceMaps.Attach(mat);   // 색상 텍스처 옆에 _Normal / _MS 가 있으면 붙인다 (_Smoothness 1)
            EditorUtility.SetDirty(mat); return mat;
        }

        static GameObject Root()
        {
            GameObject r = Find(RootName);
            if (r == null) { r = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(r, "Create " + RootName); }
            return r;
        }
        static void ClearPart(string part)
        {
            GameObject r = Find(RootName); if (r == null) return;
            Transform t = r.transform.Find(part); if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
        }
        static Mesh SaveMesh(MB mb, string name)
        {
            EnsureFolder(Dir + "/Meshes");
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(mb.V); mesh.SetNormals(mb.N); mesh.SetUVs(0, mb.UV); mesh.SetTriangles(mb.T, 0); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            string p = Dir + "/Meshes/" + name + ".asset";
            AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(mesh, p);
            return mesh;
        }

        void Build(string part)
        {
            ClearPart(part);
            var rnd = new System.Random(seed + part.Length * 31);
            Dictionary<string, MB> set =
                part == "Floor" ? ShellGeo.Floor(S) : part == "Rink" ? ShellGeo.Rink(S, rnd) : part == "Facades" ? ShellGeo.Facades(S, rnd) :
                part == "Ceiling" ? ShellGeo.Ceiling(S, rnd) : ShellGeo.Dome(S, rnd);
            var holder = new GameObject(part); holder.transform.SetParent(Root().transform, false);
            int tris = 0;
            foreach (KeyValuePair<string, MB> kv in set)
            {
                if (kv.Value.T.Count == 0) continue;
                Mesh mesh = SaveMesh(kv.Value, "LWS_" + part + "_" + kv.Key);
                var g = new GameObject(part + "_" + kv.Key); g.transform.SetParent(holder.transform, false);
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = Mat(kv.Key);
                if (kv.Key.Contains("Glass")) mr.shadowCastingMode = ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                if (part == "Floor" && kv.Key == "FloorBrick") g.AddComponent<MeshCollider>().sharedMesh = mesh;
                tris += kv.Value.T.Count / 3;
            }
            Undo.RegisterCreatedObjectUndo(holder, "Build " + part);
            Debug.Log("[Shell] " + part + ": 재질 " + set.Count + "개, 삼각형 " + tris.ToString("N0"));
        }

        // ------------------------------------------------------------------ column (independent prefab instances)
        GameObject ColumnPrefab(bool rebuildMesh)
        {
            EnsureFolder(Dir + "/Prefabs");
            string pp = Dir + "/Prefabs/LW_Shell_Column.prefab";
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(pp);
            if (pf != null && !rebuildMesh) return pf;
            Mesh mesh = SaveMesh(ShellGeo.Column(S.H), "LWS_Column");
            if (pf != null)
            {
                pf.GetComponent<MeshFilter>().sharedMesh = mesh;
                var cc = pf.GetComponent<CapsuleCollider>(); if (cc) { cc.height = S.H; cc.center = new Vector3(0, S.H / 2, 0); }
                EditorUtility.SetDirty(pf); AssetDatabase.SaveAssets();
                Debug.Log("[Shell] 기둥 프리팹 높이 = " + S.H + " m (씬의 모든 기둥이 따라감)"); return pf;
            }
            var g = new GameObject("LW_Shell_Column");
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = Mat("White");
            var col = g.AddComponent<CapsuleCollider>(); col.radius = 1.5f; col.height = S.H; col.center = new Vector3(0, S.H / 2, 0);
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            pf = PrefabUtility.SaveAsPrefabAsset(g, pp); DestroyImmediate(g);
            return pf;
        }
        void EnsureColumn(bool another = false)
        {
            GameObject pf = ColumnPrefab(false);
            GameObject root = Find(ColRootName);
            if (root == null) { root = new GameObject(ColRootName); Undo.RegisterCreatedObjectUndo(root, "Create " + ColRootName); }
            if (root.transform.childCount > 0 && !another) return;                  // 이미 있으면 그대로 (사용자가 옮긴 것 보존)
            var g = (GameObject)PrefabUtility.InstantiatePrefab(pf, root.transform);
            Vector2 p = ShellSettings.ColumnPos + (another ? new Vector2(6f * root.transform.childCount, 0) : Vector2.zero);
            g.transform.position = new Vector3(p.x, 0f, p.y);
            g.name = "LW_Shell_Column" + (root.transform.childCount > 1 ? " (" + (root.transform.childCount - 1) + ")" : "");
            Undo.RegisterCreatedObjectUndo(g, "Place column");
            Selection.activeGameObject = g;
            Debug.Log("[Shell] 기둥 놓음 " + g.transform.position + " — 옮기고 Ctrl+D 로 복사하세요");
        }
    }
}
#endif
