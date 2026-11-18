// LotteWorldProps.cs  (v1 - 11/18) 꾸밈 소품 프리팹 + 정문 일대 배치 + 철골·유리 천장 + 퍼레이드 루프 쇠사슬
// 위치: Assets/Maps/LotteWorld/Rides/Props/Editor/ (어느 Editor 폴더든 됨). 메뉴: Tools > LotteWorld > Props (Deco)
//
// 순서
//  0) Tools > LotteWorld > Setup Ride Materials (Rides/Props/LotteWorld_Props.fbx 재질·텍스처 연결; 1번이 대신 불러 줌)
//  1) [1. 프리팹 만들기]  FBX 안 Prop_* 46종 -> Rides/Props/Prefabs/Prop_*.prefab
//        - 점광원 자리(*_LightPt*)에 Point Light, Lamp_Half 는 깜빡임(LotteWorld.Props.LightFlicker)
//        - 단단한 소품(가로등·벤치·화단·수레·진열장 등)은 Mesh Collider(Convex 끔)
//  2) [2. 정문 일대 배치]  씬의 정문 일대 문(Door_Gate_W, Door_F1_L ...) 위치로 모듈 -> 월드 변환(위치·회전·배율)을
//        자동 계산해서 시안 그대로 156개 배치 -> 루트 LotteWorld_Props_NorthGate. 다시 누르면 지우고 새로 배치
//  3) [3. 천장 만들기]   벽(Wall_*) 범위 위에 철골 격자 + 트러스 + 유리판(+ 막힌 판) -> 루트 LotteWorld_Ceiling
//  4) [4. 퍼레이드 쇠사슬] ParadeLoop 바깥으로 쇠사슬 기둥 (스타디움 모양, 출입 틈) -> 루트 LotteWorld_ParadeChain
// 저장은 직접(Ctrl+S). 전부 Undo 됨.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LotteWorld.Props
{
    public class PropsWindow : EditorWindow
    {
        const string PropsDir = "Assets/Maps/LotteWorld/Rides/Props";
        const string FbxPath = PropsDir + "/LotteWorld_Props.fbx";
        const string PrefabDir = PropsDir + "/Prefabs";
        const string MatDir = PropsDir + "/Materials";
        const string NGRootName = "LotteWorld_Props_NorthGate", CeilRootName = "LotteWorld_Ceiling", ChainRootName = "LotteWorld_ParadeChain";

        float lightIntensity = 2.5f, lightRange = 9f;
        bool lightShadows = false;
        float ceilHeight = 40f, ceilSpacing = 6f;
        Vector2 ceilMin = new Vector2(-187.6f, -124.2f), ceilMax = new Vector2(157.6f, 142.6f);
        float chainOffset = 0.8f; int chainGapEvery = 14, chainGapSegs = 2;
        Vector2 scroll;

        [MenuItem("Tools/LotteWorld/Props (Deco)")]
        public static void Open() { GetWindow<PropsWindow>("LW Props"); }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("소품 FBX: " + FbxPath, EditorStyles.miniLabel);
            lightIntensity = EditorGUILayout.FloatField("Light intensity", lightIntensity);
            lightRange = EditorGUILayout.FloatField("Light range (m)", lightRange);
            lightShadows = EditorGUILayout.Toggle("Light shadows (무거움)", lightShadows);
            if (GUILayout.Button("1. 프리팹 만들기", GUILayout.Height(28))) BuildPrefabs();
            EditorGUILayout.Space();
            if (GUILayout.Button("2. 정문 일대 배치 (시안 그대로)", GUILayout.Height(28))) PlaceNorthGate();
            if (GUILayout.Button("   정문 일대 소품 지우기")) Clear(NGRootName);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("천장", EditorStyles.boldLabel);
            ceilHeight = EditorGUILayout.FloatField("높이 (m)", ceilHeight);
            ceilSpacing = EditorGUILayout.FloatField("격자 간격 (m)", ceilSpacing);
            ceilMin = EditorGUILayout.Vector2Field("범위 min (x, z)", ceilMin);
            ceilMax = EditorGUILayout.Vector2Field("범위 max (x, z)", ceilMax);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("범위 = Wall_* 바깥")) FitCeilingToWalls();
            if (GUILayout.Button("높이 = 제일 높은 건물 + 6 m")) FitCeilingHeight();
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("3. 천장 만들기", GUILayout.Height(28))) BuildCeiling();
            if (GUILayout.Button("   천장 지우기")) Clear(CeilRootName);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("퍼레이드 루프 쇠사슬", EditorStyles.boldLabel);
            chainOffset = EditorGUILayout.FloatField("노란 선 바깥 간격 (m)", chainOffset);
            chainGapEvery = EditorGUILayout.IntField("틈 간격 (마디)", chainGapEvery);
            chainGapSegs = EditorGUILayout.IntField("틈 크기 (마디)", chainGapSegs);
            if (GUILayout.Button("4. 퍼레이드 쇠사슬 만들기", GUILayout.Height(28))) BuildParadeChain();
            if (GUILayout.Button("   쇠사슬 지우기")) Clear(ChainRootName);
            EditorGUILayout.HelpBox("저장은 직접 하세요(Ctrl+S). 정문 일대 배치는 씬 안의 정문 일대 문 위치로 맞추므로, 정문 일대를 옮기거나 키운 뒤에는 2번을 다시 누르면 따라갑니다.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ helpers
        static GameObject FindRoot(string name)
        {
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects()) if (g.name == name) return g;
            return null;
        }
        static void Clear(string name)
        {
            GameObject g = FindRoot(name);
            if (g != null) Undo.DestroyObjectImmediate(g);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        static GameObject NewRoot(string name)
        {
            Clear(name);
            var g = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(g, "Create " + name);
            return g;
        }
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        static Type FlickerType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType("LotteWorld.Props.LightFlicker");
                if (t != null) return t;
            }
            return null;
        }
        static Shader Lit() { return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"); }
        static void MakeTransparent(Material m, Color c)
        {
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
        }
        static Material MakeMat(string name, Color c, float metal, float smooth, bool transparent = false)
        {
            EnsureFolder(MatDir);
            string p = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Lit()); AssetDatabase.CreateAsset(m, p); }
            m.shader = Lit();
            m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
            if (transparent) MakeTransparent(m, c);
            EditorUtility.SetDirty(m);
            return m;
        }

        // solid props get Mesh Colliders (Convex off); thin decorations don't
        static readonly string[] NoCollider = { "Litter", "Medallion", "Bunting", "Banner", "Balloons", "Clutter", "Pendant", "Sconce", "Frame", "MenuBoard", "Wainscot", "RoomLitter" };

        // ------------------------------------------------------------------ 1. prefabs
        void BuildPrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath) == null) { Debug.LogError("[Props] FBX가 없어: " + FbxPath); return; }
            EditorApplication.ExecuteMenuItem("Tools/LotteWorld/Setup Ride Materials");          // RideMaterialSetup.cs: 재질·텍스처 연결
            var glassMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/LW_Prop_Glass.mat");
            if (glassMat != null) { MakeTransparent(glassMat, new Color(0.75f, 0.8f, 0.78f, 0.3f)); EditorUtility.SetDirty(glassMat); }
            EnsureFolder(PrefabDir);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            var names = asset.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Prop_") && !t.name.Contains("_LightPt") && !t.name.EndsWith("_Fwd") && !t.name.Contains("__")).Select(t => t.name).Distinct().ToList();
            Type flicker = FlickerType();
            if (flicker == null) Debug.LogWarning("[Props] LightFlicker 스크립트를 못 찾음 (Rides/Props/Scripts/LotteWorldLightFlicker.cs). 깜빡임 없이 진행");
            int made = 0, lights = 0;
            foreach (string n in names)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Transform src = inst.GetComponentsInChildren<Transform>(true).First(t => t.name == n);
                Quaternion r0 = src.rotation; Vector3 s0 = src.lossyScale;
                var holder = new GameObject(n);
                src.SetParent(holder.transform, false);
                src.localPosition = Vector3.zero; src.localRotation = r0; src.localScale = s0;
                // 앞 방향 맞추기: FBX 축 변환이 어떻든 소품 앞(블렌더 로컬 -Y, 표시 *_Fwd)이 프리팹 +Z를 보게 돌림
                Transform fwd = holder.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("_Fwd"));
                if (fwd != null)
                {
                    Vector3 f = holder.transform.InverseTransformPoint(fwd.position);
                    float alpha = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                    src.localRotation = Quaternion.Euler(0f, -alpha, 0f) * r0;
                    DestroyImmediate(fwd.gameObject);
                }
                else Debug.LogWarning("[Props] " + n + ": _Fwd 표시 없음 — 방향 확인 필요");
                bool solid = !NoCollider.Any(k => n.Contains(k));
                foreach (MeshFilter mf in holder.GetComponentsInChildren<MeshFilter>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(mf.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                    if (solid && mf.sharedMesh != null) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; mc.convex = false; }
                }
                foreach (Transform lp in holder.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("_LightPt")).ToList())
                {
                    var L = lp.gameObject.AddComponent<Light>();
                    L.type = LightType.Point; L.color = new Color(1f, 0.78f, 0.5f);
                    L.intensity = n.Contains("Lamp") ? lightIntensity : lightIntensity * 0.5f;
                    L.range = n.Contains("Lamp") ? lightRange : lightRange * 0.55f;
                    L.shadows = lightShadows ? LightShadows.Soft : LightShadows.None;
                    if (flicker != null && n.Contains("Half")) lp.gameObject.AddComponent(flicker);
                    lights++;
                }
                PrefabUtility.SaveAsPrefabAsset(holder, PrefabDir + "/" + n + ".prefab");
                DestroyImmediate(holder); DestroyImmediate(inst); made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Props] 프리팹 " + made + "개, 점광원 " + lights + "개 → " + PrefabDir);
        }

        // ------------------------------------------------------------------ 2. North Gate placement
        struct P { public string n; public float x, y, z, yaw, sx; public P(string n, float x, float y, float z, float yaw, float sx) { this.n = n; this.x = x; this.y = y; this.z = z; this.yaw = yaw; this.sx = sx; } }
        // 정문 일대 모듈 좌표의 문 경첩 (LotteWorldNorthGate.cs v1.5 / HarnessNG). A06·매점은 따로 옮길 수 있어 빼고 본체만
        static readonly string[] AnchorNames = { "Door_Gate_W", "Door_Gate_E", "Door_F2", "Door_F1_L", "Door_F1_R", "Door_SN_W_L", "Door_SN_E_R", "Door_SN_C_L" };
        static readonly Vector2[] AnchorModule =
        {
            new Vector2(-6.40f, -7.82f), new Vector2(5.80f, -7.82f), new Vector2(8.18f, -15.30f), new Vector2(-4.90f, -44.85f),
            new Vector2(-4.90f, -42.45f), new Vector2(19.90f, -52.30f), new Vector2(46.15f, -52.30f), new Vector2(31.85f, -48.30f),
        };

        static Transform FindDoor(string name)
        {
            Transform best = null;
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.name != name) continue;
                if (best == null || t.root.name.Contains("NorthGate")) best = t;
            }
            return best;
        }

        /// <summary>module (x, z) -> world (x, z): w = a * m + b (complex). returns false if fewer than 2 doors found.</summary>
        static bool SolveNorthGate(out Vector2 a, out Vector2 b, out float floorY, out string report)
        {
            a = new Vector2(1, 0); b = Vector2.zero; floorY = 0f; report = "";
            var M = new List<Vector2>(); var W = new List<Vector2>(); var ys = new List<float>();
            for (int i = 0; i < AnchorNames.Length; i++)
            {
                Transform t = FindDoor(AnchorNames[i]);
                if (t == null) { report += AnchorNames[i] + " 없음; "; continue; }
                M.Add(AnchorModule[i]); W.Add(new Vector2(t.position.x, t.position.z)); ys.Add(t.position.y);
            }
            if (M.Count < 2) return false;
            Vector2 mc = Vector2.zero, wc = Vector2.zero;
            for (int i = 0; i < M.Count; i++) { mc += M[i]; wc += W[i]; }
            mc /= M.Count; wc /= M.Count;
            float re = 0, im = 0, den = 0, reM = 0, imM = 0;
            for (int i = 0; i < M.Count; i++)
            {
                Vector2 m = M[i] - mc, w = W[i] - wc;
                re += w.x * m.x + w.y * m.y; im += w.y * m.x - w.x * m.y;      // w * conj(m)
                reM += w.x * m.x - w.y * m.y; imM += w.y * m.x + w.x * m.y;   // w * m  (mirrored fit)
                den += m.sqrMagnitude;
            }
            a = new Vector2(re / den, im / den);
            Vector2 am = new Vector2(reM / den, imM / den);
            float err = 0, errM = 0;
            for (int i = 0; i < M.Count; i++)
            {
                Vector2 m = M[i] - mc, w = W[i] - wc;
                err += (new Vector2(a.x * m.x - a.y * m.y, a.x * m.y + a.y * m.x) - w).sqrMagnitude;
                errM += (new Vector2(am.x * m.x + am.y * m.y, am.y * m.x - am.x * m.y) - w).sqrMagnitude;
            }
            b = wc - new Vector2(a.x * mc.x - a.y * mc.y, a.x * mc.y + a.y * mc.x);
            float s = a.magnitude;
            floorY = ys.Average() - 0.02f * s;
            report += string.Format("문 {0}개, 배율 {1:F3}, 회전 {2:F1}°, 평균 오차 {3:F2} m", M.Count, s, -Mathf.Atan2(a.y, a.x) * Mathf.Rad2Deg, Mathf.Sqrt(err / M.Count));
            if (errM < err * 0.5f) report += "  ※ 좌우 뒤집힌 배치로 보임 — 소품 방향이 틀릴 수 있음";
            return true;
        }

        void PlaceNorthGate()
        {
            Vector2 a, b; float floorY; string rep;
            if (!SolveNorthGate(out a, out b, out floorY, out rep)) { Debug.LogError("[Props] 정문 일대 문을 2개 이상 못 찾음: " + rep); return; }
            float s = a.magnitude, rotDeg = -Mathf.Atan2(a.y, a.x) * Mathf.Rad2Deg;
            GameObject root = NewRoot(NGRootName);
            var cache = new Dictionary<string, GameObject>(); int n = 0, miss = 0;
            foreach (P p in Layout)
            {
                GameObject pf;
                if (!cache.TryGetValue(p.n, out pf)) { pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + p.n + ".prefab"); cache[p.n] = pf; }
                if (pf == null) { miss++; continue; }
                var g = (GameObject)PrefabUtility.InstantiatePrefab(pf, root.transform);
                Vector2 w = new Vector2(a.x * p.x - a.y * p.z, a.x * p.z + a.y * p.x) + b;
                g.transform.position = new Vector3(w.x, floorY + p.y * s, w.y);
                g.transform.rotation = Quaternion.Euler(0f, p.yaw + rotDeg, 0f);
                g.transform.localScale = new Vector3(p.sx, 1f, 1f);
                n++;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Props] 정문 일대 소품 " + n + "개 배치 (" + rep + ")" + (miss > 0 ? ", 프리팹 없음 " + miss + "개 — 1번 먼저" : ""));
        }

        // ------------------------------------------------------------------ 3. ceiling
        void FitCeilingToWalls()
        {
            bool any = false; Bounds bb = new Bounds();
            foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.name.StartsWith("Wall_")) continue;
                if (!any) { bb = r.bounds; any = true; } else bb.Encapsulate(r.bounds);
            }
            if (!any) { Debug.LogWarning("[Props] Wall_* 렌더러를 못 찾음"); return; }
            ceilMin = new Vector2(bb.min.x, bb.min.z); ceilMax = new Vector2(bb.max.x, bb.max.z);
            Debug.Log("[Props] 천장 범위 = " + ceilMin + " .. " + ceilMax);
        }
        void FitCeilingHeight()
        {
            float top = 0f;
            foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Transform t = r.transform.root;
                if (t.name.StartsWith("LotteWorld_") && t.name != CeilRootName && !t.name.StartsWith("LotteWorld_Props")) top = Mathf.Max(top, r.bounds.max.y);
                else if (t.name.Contains("_A0") || t.name.Contains("_A1") || t.name == "GardenStage") top = Mathf.Max(top, r.bounds.max.y);
            }
            ceilHeight = Mathf.Ceil(top + 6f);
            Debug.Log("[Props] 제일 높은 건물 " + top.ToString("F1") + " m → 천장 높이 " + ceilHeight);
        }

        class MB
        {
            public readonly List<Vector3> V = new List<Vector3>(); public readonly List<int> T = new List<int>();
            public void Box(Vector3 c, Vector3 ax, Vector3 ay, Vector3 az, Vector3 size)
            {
                Vector3 X = ax * size.x * 0.5f, Y = ay * size.y * 0.5f, Z = az * size.z * 0.5f;
                Vector3[] q = { c - X - Y - Z, c + X - Y - Z, c + X + Y - Z, c - X + Y - Z, c - X - Y + Z, c + X - Y + Z, c + X + Y + Z, c - X + Y + Z };
                int[][] f = { new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 }, new[] { 3, 7, 6, 2 }, new[] { 0, 4, 7, 3 }, new[] { 1, 2, 6, 5 } };
                foreach (int[] k in f)
                {
                    int i = V.Count; V.Add(q[k[0]]); V.Add(q[k[1]]); V.Add(q[k[2]]); V.Add(q[k[3]]);
                    T.Add(i); T.Add(i + 1); T.Add(i + 2); T.Add(i); T.Add(i + 2); T.Add(i + 3);
                }
            }
            public void AABB(Vector3 c, Vector3 size) { Box(c, Vector3.right, Vector3.up, Vector3.forward, size); }
            public void Member(Vector3 p, Vector3 q, float th)
            {
                Vector3 d = q - p; float L = d.magnitude; if (L < 1e-4f) return;
                Vector3 az = d / L, ax = Vector3.Cross(Vector3.up, az); if (ax.sqrMagnitude < 1e-6f) ax = Vector3.right; ax.Normalize();
                Box((p + q) * 0.5f, ax, Vector3.Cross(az, ax), az, new Vector3(th, th, L));
            }
            public GameObject Make(string name, Transform parent, Material m, bool shadows)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(V); mesh.SetTriangles(T, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var g = new GameObject(name); g.transform.SetParent(parent, false);
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
                return g;
            }
        }

        void BuildCeiling()
        {
            Material steel = MakeMat("LW_Ceiling_Steel", new Color(0.2f, 0.21f, 0.22f), 0.7f, 0.35f);
            Material glass = MakeMat("LW_Ceiling_Glass", new Color(0.55f, 0.62f, 0.58f, 0.4f), 0f, 0.8f, true);
            Material panel = MakeMat("LW_Ceiling_Panel", new Color(0.14f, 0.14f, 0.13f), 0.2f, 0.2f);
            GameObject root = NewRoot(CeilRootName);
            float Z = ceilHeight, st = Mathf.Max(2f, ceilSpacing);
            float x0 = ceilMin.x, x1 = ceilMax.x, z0 = ceilMin.y, z1 = ceilMax.y;
            int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / st)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / st));
            float sx = (x1 - x0) / nx, sz = (z1 - z0) / nz;
            var beams = new MB();
            for (int i = 0; i <= nx; i++) beams.AABB(new Vector3(x0 + i * sx, Z, (z0 + z1) * 0.5f), new Vector3(0.35f, 0.9f, z1 - z0));
            for (int j = 0; j <= nz; j++) beams.AABB(new Vector3((x0 + x1) * 0.5f, Z - 0.2f, z0 + j * sz), new Vector3(x1 - x0, 0.6f, 0.3f));
            for (int i = 0; i < nx; i++)
            {
                float xa = x0 + i * sx, xb = xa + sx;
                beams.AABB(new Vector3((xa + xb) * 0.5f, Z - 2.2f, (z0 + z1) * 0.5f), new Vector3(0.2f, 0.2f, z1 - z0));   // 아래 현
                for (int j = 0; j <= nz; j += 2)
                {
                    float z = z0 + j * sz;
                    beams.Member(new Vector3(xa, Z - 0.45f, z), new Vector3(xb, Z - 2.2f, z), 0.12f);
                    beams.Member(new Vector3(xb, Z - 0.45f, z), new Vector3(xa, Z - 2.2f, z), 0.12f);
                }
            }
            beams.Make("Ceiling_Steel", root.transform, steel, true);
            var gl = new MB(); gl.AABB(new Vector3((x0 + x1) * 0.5f, Z + 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.02f, z1 - z0));
            gl.Make("Ceiling_Glass", root.transform, glass, false);
            var pn = new MB(); var rng = new System.Random(3);
            int panels = Mathf.Max(1, nx * nz / 14);
            for (int k = 0; k < panels; k++)
            {
                int i = rng.Next(nx), j = rng.Next(nz);
                pn.AABB(new Vector3(x0 + (i + 0.5f) * sx, Z + 0.45f, z0 + (j + 0.5f) * sz), new Vector3(sx - 0.4f, 0.05f, sz - 0.4f));
            }
            pn.Make("Ceiling_BlindPanels", root.transform, panel, true);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(string.Format("[Props] 천장 {0:F0} x {1:F0} m, 높이 {2} m, 격자 {3}x{4}, 막힌 판 {5}", x1 - x0, z1 - z0, Z, nx, nz, panels));
        }

        // ------------------------------------------------------------------ 4. parade chain
        void BuildParadeChain()
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Prop_ChainSeg.prefab");
            if (pf == null) { Debug.LogError("[Props] Prop_ChainSeg 프리팹 없음 — 1번 먼저"); return; }
            // ParadeLoop 은 렌더러 없는 부모 + 자식 조각(Parade_*) 구조 -> 자식 렌더러 bounds 를 전부 합침
            Bounds bb = new Bounds(); bool found = false; int parts = 0;
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name != "ParadeLoop") continue;
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) { if (!found) { bb = r.bounds; found = true; } else bb.Encapsulate(r.bounds); parts++; }
                if (found) break;
            }
            if (!found) { Debug.LogError("[Props] ParadeLoop 과 그 자식 렌더러를 못 찾음"); return; }
            Debug.Log("[Props] ParadeLoop 조각 " + parts + "개 합침: 중심 " + bb.center + ", 반폭 " + bb.extents.x.ToString("F2") + " x " + bb.extents.z.ToString("F2"));
            float y = bb.min.y;
            float hx = bb.extents.x + chainOffset, hz = bb.extents.z + chainOffset; Vector3 c = bb.center;
            bool alongX = hx >= hz; float rr = alongX ? hz : hx, half = (alongX ? hx : hz) - rr;
            // stadium perimeter sampled every ~2 m
            var pts = new List<Vector3>();
            int nStr = Mathf.Max(1, Mathf.RoundToInt(2f * half / 2f)), nArc = Mathf.Max(4, Mathf.RoundToInt(Mathf.PI * rr / 2f));
            for (int side = 0; side < 2; side++)
            {
                float sg = side == 0 ? 1f : -1f;
                for (int i = 0; i < nStr; i++) { float u = -half + 2f * half * i / nStr; pts.Add(alongX ? new Vector3(sg * u, 0, sg * rr) : new Vector3(-sg * rr, 0, sg * u)); }
                for (int i = 0; i < nArc; i++)
                {
                    float ang = Mathf.PI * 0.5f - Mathf.PI * i / nArc;      // around the end cap
                    Vector3 cc = alongX ? new Vector3(sg * half, 0, 0) : new Vector3(0, 0, sg * half);
                    Vector3 d = alongX ? new Vector3(sg * Mathf.Cos(ang), 0, sg * Mathf.Sin(ang)) : new Vector3(-sg * Mathf.Sin(ang), 0, sg * Mathf.Cos(ang));
                    pts.Add(cc + d * rr);
                }
            }
            GameObject root = NewRoot(ChainRootName); int n = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                if (chainGapEvery > 0 && (i % chainGapEvery) < chainGapSegs) continue;   // 출입 틈
                Vector3 p = c + pts[i], q = c + pts[(i + 1) % pts.Count]; p.y = q.y = y;
                Vector3 d = q - p; float L = d.magnitude; if (L < 0.05f) continue;
                var g = (GameObject)PrefabUtility.InstantiatePrefab(pf, root.transform);
                g.transform.position = p;
                g.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(d.z, -d.x) * Mathf.Rad2Deg, 0f);   // 소품 로컬 -X = 쇠사슬 방향
                g.transform.localScale = new Vector3(L / 2f, 1f, 1f);
                n++;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Props] 퍼레이드 쇠사슬 " + n + "마디 (루프 " + (2 * hx).ToString("F1") + " x " + (2 * hz).ToString("F1") + " m)");
        }

        // ------------------------------------------------------------------ layout (Unity module coords of LotteWorld_NorthGate: x, y, z, yaw, scaleX)
        static readonly P[] Layout =
        {
            new P("Prop_Lamp_On", 0.60f, 0.00f, -14.00f, -90.0f, 1.000f),
            new P("Prop_Lamp_On", 0.60f, 0.00f, -24.00f, -90.0f, 1.000f),
            new P("Prop_Lamp_Half", 0.60f, 0.00f, -34.00f, -90.0f, 1.000f),
            new P("Prop_Lamp_On", 0.60f, 0.00f, -44.00f, -90.0f, 1.000f),
            new P("Prop_Lamp_Off", 0.60f, 0.00f, -54.00f, -90.0f, 1.000f),
            new P("Prop_Lamp_On", 15.60f, 0.00f, -28.00f, -90.0f, 1.000f),
            new P("Prop_Lamp_Off", 15.60f, 0.00f, -38.00f, -90.0f, 1.000f),
            new P("Prop_Lamp_On", 15.60f, 0.00f, -48.00f, -90.0f, 1.000f),
            new P("Prop_Lamp_On", 8.00f, 0.00f, -60.50f, -0.0f, 1.000f),
            new P("Prop_Lamp_On", 20.00f, 0.00f, -60.50f, -0.0f, 1.000f),
            new P("Prop_Lamp_Half", 32.00f, 0.00f, -60.50f, -0.0f, 1.000f),
            new P("Prop_Lamp_Off", 44.00f, 0.00f, -60.50f, -0.0f, 1.000f),
            new P("Prop_Lamp_On", -8.00f, 0.00f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Lamp_Off", 4.00f, 0.00f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Lamp_On", 16.00f, 0.00f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Lamp_On", 28.00f, 0.00f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Lamp_Off", 40.00f, 0.00f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Lamp_On", 52.00f, 0.00f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Planter_Tree", 8.20f, 0.00f, -31.00f, -224.1f, 1.000f),
            new P("Prop_Bench", 5.60f, 0.00f, -31.00f, -90.0f, 1.000f),
            new P("Prop_Bench", 10.80f, 0.00f, -31.00f, 90.0f, 1.000f),
            new P("Prop_Bin", 8.20f, 0.00f, -33.70f, -0.0f, 1.000f),
            new P("Prop_Planter_TreeDry", 8.20f, 0.00f, -45.00f, -266.9f, 1.000f),
            new P("Prop_Bench", 5.60f, 0.00f, -45.00f, -90.0f, 1.000f),
            new P("Prop_Bench", 10.80f, 0.00f, -45.00f, 90.0f, 1.000f),
            new P("Prop_Bin", 8.20f, 0.00f, -47.70f, -0.0f, 1.000f),
            new P("Prop_Planter_Tree", 22.00f, 0.00f, -65.50f, -286.1f, 1.000f),
            new P("Prop_Bench", 22.00f, 0.00f, -62.70f, -0.0f, 1.000f),
            new P("Prop_Bench", 22.00f, 0.00f, -68.30f, -180.0f, 1.000f),
            new P("Prop_Planter_TreeDry", 38.00f, 0.00f, -66.00f, -339.1f, 1.000f),
            new P("Prop_Bench", 38.00f, 0.00f, -63.20f, -0.0f, 1.000f),
            new P("Prop_Bench", 38.00f, 0.00f, -68.80f, -180.0f, 1.000f),
            new P("Prop_Planter_Tree", -3.00f, 0.00f, -66.00f, -266.2f, 1.000f),
            new P("Prop_Bench", -3.00f, 0.00f, -63.20f, -0.0f, 1.000f),
            new P("Prop_Bench", -3.00f, 0.00f, -68.80f, -180.0f, 1.000f),
            new P("Prop_Bin", 29.50f, 0.00f, -61.30f, -0.0f, 1.000f),
            new P("Prop_Bin_Tipped", 13.00f, 0.00f, -61.30f, -0.0f, 1.000f),
            new P("Prop_Bin", 1.20f, 0.00f, -40.00f, -0.0f, 1.000f),
            new P("Prop_Bin", 15.00f, 0.00f, -33.00f, -0.0f, 1.000f),
            new P("Prop_Bench", 50.00f, 0.00f, -63.30f, -0.0f, 1.000f),
            new P("Prop_Bench", 55.50f, 0.00f, -63.30f, -0.0f, 1.000f),
            new P("Prop_Bin", 52.70f, 0.00f, -63.20f, -0.0f, 1.000f),
            new P("Prop_Cart_Balloons", 12.50f, 0.00f, -57.50f, -11.5f, 1.000f),
            new P("Prop_Cart", 33.50f, 0.00f, -69.50f, 17.2f, 1.000f),
            new P("Prop_Cart", 4.20f, 0.00f, -19.00f, -90.0f, 1.000f),
            new P("Prop_Sign", 9.00f, 0.00f, -24.50f, -0.0f, 1.000f),
            new P("Prop_Sign", 14.50f, 0.00f, -57.20f, -22.9f, 1.000f),
            new P("Prop_Sign", -1.50f, 0.00f, -61.50f, 17.2f, 1.000f),
            new P("Prop_Stanchions", 28.90f, 0.00f, -68.20f, -0.0f, 1.000f),
            new P("Prop_Stanchions", 28.90f, 0.00f, -69.20f, -0.0f, 1.000f),
            new P("Prop_Stanchions", 28.90f, 0.00f, -70.20f, -0.0f, 1.000f),
            new P("Prop_Stanchions", 46.90f, 0.00f, -66.00f, -0.0f, 1.000f),
            new P("Prop_Stanchions", 46.90f, 0.00f, -67.00f, -0.0f, 1.000f),
            new P("Prop_Bunting18", 7.85f, 5.90f, -31.00f, -173.5f, 0.979f),
            new P("Prop_Bunting18", 7.85f, 5.90f, -39.00f, -173.5f, 0.979f),
            new P("Prop_Bunting18", 7.85f, 5.90f, -47.00f, -173.5f, 0.979f),
            new P("Prop_Bunting12", 14.00f, 3.60f, -60.50f, -0.0f, 1.000f),
            new P("Prop_Bunting12", 26.00f, 3.60f, -60.50f, -0.0f, 1.000f),
            new P("Prop_Bunting12", 38.00f, 3.60f, -60.50f, -0.0f, 1.000f),
            new P("Prop_Bunting12", -2.00f, 3.60f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Bunting12", 10.00f, 3.60f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Bunting12", 22.00f, 3.60f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Bunting12", 34.00f, 3.60f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Bunting12", 46.00f, 3.60f, -72.60f, -0.0f, 1.000f),
            new P("Prop_Banner_Red", 8.00f, 15.00f, -38.00f, -0.0f, 1.000f),
            new P("Prop_Banner_Blue", 24.00f, 15.00f, -64.00f, -0.0f, 1.000f),
            new P("Prop_Banner_Yellow", 6.00f, 15.00f, -66.00f, -0.0f, 1.000f),
            new P("Prop_Litter", 8.00f, 0.00f, -20.00f, -331.9f, 1.000f),
            new P("Prop_Litter", 12.00f, 0.00f, -38.00f, -10.4f, 1.000f),
            new P("Prop_Litter", 4.00f, 0.00f, -50.00f, -167.5f, 1.000f),
            new P("Prop_Litter", 20.00f, 0.00f, -62.00f, -339.4f, 1.000f),
            new P("Prop_Litter", 30.00f, 0.00f, -68.00f, -233.5f, 1.000f),
            new P("Prop_Litter", 45.00f, 0.00f, -70.00f, -324.2f, 1.000f),
            new P("Prop_Litter", 10.00f, 0.00f, -70.00f, -40.7f, 1.000f),
            new P("Prop_Litter", 0.00f, 0.00f, -64.00f, -168.8f, 1.000f),
            new P("Prop_Litter", 55.00f, 0.00f, -68.00f, -88.7f, 1.000f),
            new P("Prop_Medallion", 4.00f, 0.00f, -14.00f, -0.0f, 1.000f),
            new P("Prop_Dining_Cloth", -8.60f, 0.00f, -56.50f, -0.0f, 1.000f),
            new P("Prop_Pendant_On", -8.60f, 0.00f, -56.50f, -0.0f, 1.000f),
            new P("Prop_Dining_Plain", -14.60f, 0.00f, -56.50f, -90.0f, 1.000f),
            new P("Prop_Pendant_Off", -14.60f, 0.00f, -56.50f, -0.0f, 1.000f),
            new P("Prop_Dining_ClothTopple", -8.60f, 0.00f, -50.00f, -0.0f, 1.000f),
            new P("Prop_Pendant_On", -8.60f, 0.00f, -50.00f, -0.0f, 1.000f),
            new P("Prop_Dining_Cloth", -14.60f, 0.00f, -50.00f, -90.0f, 1.000f),
            new P("Prop_Pendant_On", -14.60f, 0.00f, -50.00f, -0.0f, 1.000f),
            new P("Prop_Dining_Plain", -14.60f, 0.00f, -43.50f, 90.0f, 1.000f),
            new P("Prop_Pendant_On", -14.60f, 0.00f, -43.50f, -0.0f, 1.000f),
            new P("Prop_Dining_Cloth", -8.60f, 0.00f, -37.00f, -180.0f, 1.000f),
            new P("Prop_Pendant_Off", -8.60f, 0.00f, -37.00f, -0.0f, 1.000f),
            new P("Prop_Dining_ClothTopple", -14.60f, 0.00f, -37.00f, -90.0f, 1.000f),
            new P("Prop_Pendant_On", -14.60f, 0.00f, -37.00f, -0.0f, 1.000f),
            new P("Prop_Dining_Plain", -8.60f, 0.00f, -30.50f, 90.0f, 1.000f),
            new P("Prop_Pendant_Off", -8.60f, 0.00f, -30.50f, -0.0f, 1.000f),
            new P("Prop_Dining_Cloth", -14.60f, 0.00f, -30.50f, -90.0f, 1.000f),
            new P("Prop_Pendant_On", -14.60f, 0.00f, -30.50f, -0.0f, 1.000f),
            new P("Prop_Wainscot_Low", -5.10f, 0.00f, -30.75f, -90.0f, 1.000f),
            new P("Prop_Wainscot_Low", -5.10f, 0.00f, -34.75f, -90.0f, 1.000f),
            new P("Prop_Wainscot_Low", -5.10f, 0.00f, -38.75f, -90.0f, 1.000f),
            new P("Prop_Wainscot_Low", -5.10f, 0.00f, -48.55f, -90.0f, 1.000f),
            new P("Prop_Wainscot_Low", -5.10f, 0.00f, -52.55f, -90.0f, 1.000f),
            new P("Prop_Wainscot_Low", -5.10f, 0.00f, -56.55f, -90.0f, 1.000f),
            new P("Prop_Wainscot", -18.10f, 0.00f, -29.45f, 90.0f, 1.000f),
            new P("Prop_Wainscot", -18.10f, 0.00f, -33.45f, 90.0f, 1.000f),
            new P("Prop_Wainscot", -18.10f, 0.00f, -37.45f, 90.0f, 1.000f),
            new P("Prop_Wainscot", -18.10f, 0.00f, -41.45f, 90.0f, 1.000f),
            new P("Prop_Wainscot", -18.10f, 0.00f, -45.45f, 90.0f, 1.000f),
            new P("Prop_Wainscot", -18.10f, 0.00f, -49.45f, 90.0f, 1.000f),
            new P("Prop_Wainscot", -18.10f, 0.00f, -56.70f, 90.0f, 1.000f),
            new P("Prop_Wainscot", -7.60f, 0.00f, -60.10f, -0.0f, 1.000f),
            new P("Prop_Wainscot", -11.60f, 0.00f, -60.10f, -0.0f, 1.000f),
            new P("Prop_Wainscot", -15.60f, 0.00f, -60.10f, -0.0f, 1.000f),
            new P("Prop_Wainscot", -7.60f, 0.00f, -27.20f, -180.0f, 1.000f),
            new P("Prop_Wainscot", -11.60f, 0.00f, -27.20f, -180.0f, 1.000f),
            new P("Prop_Wainscot", -15.60f, 0.00f, -27.20f, -180.0f, 1.000f),
            new P("Prop_Frame_Tall", -18.10f, 2.10f, -55.00f, 90.0f, 1.000f),
            new P("Prop_Frame_Tall", -18.10f, 2.10f, -48.50f, 90.0f, 1.000f),
            new P("Prop_Frame_Wide", -8.50f, 2.00f, -60.10f, -0.0f, 1.000f),
            new P("Prop_Frame_Wide", -8.50f, 2.00f, -27.20f, -180.0f, 1.000f),
            new P("Prop_Frame_Wide", -14.50f, 2.00f, -60.10f, -0.0f, 1.000f),
            new P("Prop_Frame_Wide", -14.50f, 2.00f, -27.20f, -180.0f, 1.000f),
            new P("Prop_Counter10", -17.20f, 0.00f, -34.50f, 90.0f, 1.000f),
            new P("Prop_MenuBoard", -18.10f, 3.10f, -34.50f, 90.0f, 1.000f),
            new P("Prop_PlantPot_Dry", -12.20f, 0.00f, -52.10f, -0.0f, 1.000f),
            new P("Prop_PlantPot", -11.00f, 0.00f, -52.10f, -0.0f, 1.000f),
            new P("Prop_PlantPot", -12.20f, 0.00f, -35.20f, -0.0f, 1.000f),
            new P("Prop_PlantPot_Dry", -11.00f, 0.00f, -35.20f, -0.0f, 1.000f),
            new P("Prop_PlantPot_Dry", -6.00f, 0.00f, -59.30f, -0.0f, 1.000f),
            new P("Prop_PlantPot", -6.00f, 0.00f, -28.10f, -0.0f, 1.000f),
            new P("Prop_PlantPot_Dry", -17.30f, 0.00f, -59.30f, -0.0f, 1.000f),
            new P("Prop_WetFloor", -16.90f, 0.00f, -50.20f, -0.0f, 1.000f),
            new P("Prop_MopBucket", -17.40f, 0.00f, -49.20f, -0.0f, 1.000f),
            new P("Prop_Chair_Pink", -10.30f, 0.00f, -37.00f, -17.2f, 1.000f),
            new P("Prop_RoomLitter", -11.10f, 0.00f, -43.50f, -0.0f, 1.000f),
            new P("Prop_RoomLitter", -11.10f, 0.00f, -33.00f, -68.8f, 1.000f),
            new P("Prop_Sconce_On", -5.10f, 0.00f, -53.50f, -0.0f, 1.000f),
            new P("Prop_Sconce_Off", -5.10f, 0.00f, -46.50f, -0.0f, 1.000f),
            new P("Prop_Sconce_On", -5.10f, 0.00f, -40.50f, -0.0f, 1.000f),
            new P("Prop_Sconce_On", -5.10f, 0.00f, -33.50f, -0.0f, 1.000f),
            new P("Prop_Shelf3", 17.20f, 0.00f, -49.30f, 90.0f, 1.000f),
            new P("Prop_Shelf3", 25.40f, 0.00f, -49.30f, -90.0f, 1.000f),
            new P("Prop_Shelf3", 17.20f, 0.00f, -45.90f, 90.0f, 1.000f),
            new P("Prop_Shelf3", 25.40f, 0.00f, -45.90f, -90.0f, 1.000f),
            new P("Prop_Shelf3", 17.20f, 0.00f, -42.50f, 90.0f, 1.000f),
            new P("Prop_Shelf3", 25.40f, 0.00f, -42.50f, -90.0f, 1.000f),
            new P("Prop_Shelf3", 23.10f, 0.00f, -38.90f, -180.0f, 1.000f),
            new P("Prop_DisplayTable", 21.30f, 0.00f, -47.90f, -0.0f, 1.000f),
            new P("Prop_DisplayTable", 21.30f, 0.00f, -43.90f, -0.0f, 1.000f),
            new P("Prop_ClothesRack", 23.40f, 0.00f, -41.50f, -0.0f, 1.000f),
            new P("Prop_PostcardSpinner", 19.10f, 0.00f, -42.30f, -0.0f, 1.000f),
            new P("Prop_ShopCounter", 23.80f, 0.00f, -50.75f, -0.0f, 1.000f),
            new P("Prop_Pendant_On", 19.30f, 0.00f, -48.50f, -0.0f, 1.000f),
            new P("Prop_Pendant_Off", 23.30f, 0.00f, -48.50f, -0.0f, 1.000f),
            new P("Prop_Pendant_On", 19.30f, 0.00f, -43.50f, -0.0f, 1.000f),
            new P("Prop_Pendant_On", 23.30f, 0.00f, -43.50f, -0.0f, 1.000f),
            new P("Prop_BalloonsCeiling", 21.30f, 0.00f, -45.50f, -0.0f, 1.000f),
            new P("Prop_ShopClutter", 21.30f, 0.00f, -45.50f, -0.0f, 1.000f),
        };
    }
}
#endif
