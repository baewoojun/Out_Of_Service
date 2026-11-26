// FoliageSwap.cs - 열린 씬의 옛 나무를 새 나무 프리팹(Foliage/Prefabs)으로 바꿈
// 메뉴: Tools > LotteWorld > Look > 나무 교체 (열린 씬) / 나무 교체 되돌리기 (열린 씬)
// 옛 나무는 지우지 않고 끔(프리팹 인스턴스 = 비활성, FBX 안 메시 = 렌더러 끔). 새 나무는 씬 루트 "LotteWorld_FoliageNew" 아래.
// 씬 저장은 하지 않음(확인 후 직접 저장). Ctrl+Z 로도 되돌릴 수 있음
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Look
{
    public static class FoliageSwap
    {
        const string Root = "Assets/Maps/LotteWorld/Foliage";
        const string NewRoot = "LotteWorld_FoliageNew";

        class Rule { public string[] Variants; public bool Pit; public Rule(bool pit, params string[] v) { Pit = pit; Variants = v; } }

        // 프리팹 인스턴스(원본 프리팹 이름) → 새 프리팹
        static readonly Dictionary<string, Rule> PrefabRules = new Dictionary<string, Rule> {
            { "Tree_Green", new Rule(false, "Tree_Leafy_A", "Tree_Leafy_B", "Tree_Leafy_C") },
            { "Tree_Autumn", new Rule(false, "Tree_Autumn_A", "Tree_Autumn_B") },
            { "Tree_Cherry", new Rule(false, "Tree_Cherry_A", "Tree_Cherry_B") },
            { "Tree_Pine", new Rule(false, "Tree_Pine_A", "Tree_Pine_B") },
            { "Deco_Tree_Leafy", new Rule(true, "Tree_Leafy_A", "Tree_Leafy_B", "Tree_Leafy_C") },
            { "Deco_Tree_Autumn", new Rule(true, "Tree_Autumn_A", "Tree_Autumn_B") },
            { "Deco_Tree_Dead", new Rule(true, "Tree_Dead_A", "Tree_Dead_B") },
            { "Hedge_4m", new Rule(false, "Hedge_4m") },
            { "Prop_PlantPot", new Rule(false, "Pot_Plant") },
            { "Prop_PlantPot_Dry", new Rule(false, "Pot_PlantDry") },
        };
        static readonly Regex DecoTree = new Regex(@"^Deco_Tree_\d+_(leafy|autumn|dead)$");
        static readonly Regex Suffix = new Regex(@"\s*\(\d+\)$");

        static readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, float> heightCache = new Dictionary<string, float>();

        [MenuItem("Tools/LotteWorld/Look/나무 교체 (열린 씬)")]
        public static void Swap()
        {
            if (!AssetDatabase.IsValidFolder(Root + "/Prefabs") || AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Prefabs" }).Length == 0) FoliageSetup.Run();
            prefabCache.Clear(); heightCache.Clear();
            var count = new Dictionary<string, int>(); int total = 0;
            for (int si = 0; si < SceneManager.sceneCount; si++)
            {
                Scene sc = SceneManager.GetSceneAt(si); if (!sc.isLoaded) continue;
                var all = new List<Transform>();
                foreach (GameObject r in sc.GetRootGameObjects()) { if (r.name == NewRoot) continue; all.AddRange(r.GetComponentsInChildren<Transform>(false)); }
                Transform parent = null;
                System.Func<Transform> getParent = () =>
                {
                    if (parent != null) return parent;
                    foreach (GameObject r in sc.GetRootGameObjects()) if (r.name == NewRoot) parent = r.transform;
                    if (parent == null) { var g = new GameObject(NewRoot); SceneManager.MoveGameObjectToScene(g, sc); Undo.RegisterCreatedObjectUndo(g, "Foliage root"); parent = g.transform; }
                    return parent;
                };
                var done = new HashSet<Transform>();
                foreach (Transform t in all)
                {
                    if (t == null || !t.gameObject.activeInHierarchy || InsideDone(t, done)) continue;
                    GameObject go = t.gameObject; string key = null; Rule rule = null;
                    // 1) 프리팹 인스턴스
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
                    {
                        GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(go);
                        string a = src != null ? src.name : null, b = Suffix.Replace(go.name, "");
                        if (a != null && PrefabRules.TryGetValue(a, out rule)) key = a;
                        else if (PrefabRules.TryGetValue(b, out rule)) key = b;
                    }
                    // 2) Deco FBX 안의 가로수
                    if (key == null)
                    {
                        Match m = DecoTree.Match(go.name);
                        if (m.Success) { key = "Deco_Tree_" + m.Groups[1].Value; rule = m.Groups[1].Value == "leafy" ? PrefabRules["Deco_Tree_Leafy"] : m.Groups[1].Value == "autumn" ? PrefabRules["Deco_Tree_Autumn"] : PrefabRules["Deco_Tree_Dead"]; }
                        else if (go.name == "Deco_RoundBed_Tree") { key = "Deco_RoundBed_Tree"; rule = PrefabRules["Deco_Tree_Dead"]; }
                    }
                    if (key != null)
                    {
                        Bounds bd; if (!RendBounds(go.GetComponentsInChildren<Renderer>(false), out bd)) continue;
                        bool isInstance = PrefabUtility.IsAnyPrefabInstanceRoot(go);
                        Place(rule, bd, getParent(), key.StartsWith("Hedge") || key.StartsWith("Prop_PlantPot") ? (float?)t.eulerAngles.y : null);
                        if (isInstance) { Undo.RecordObject(go, "Foliage swap"); go.SetActive(false); }
                        else foreach (Renderer r in go.GetComponentsInChildren<Renderer>(false)) { Undo.RecordObject(r, "Foliage swap"); r.enabled = false; }
                        done.Add(t); Add(count, key); total++; continue;
                    }
                    // 3) 놀이기구 FBX 에 통째로 붙은 나무 (메시 한 덩어리에 여러 그루)
                    string n = go.name; MeshFilter mf = go.GetComponent<MeshFilter>(); Renderer rr = go.GetComponent<Renderer>();
                    if (mf == null || rr == null || !rr.enabled || mf.sharedMesh == null) continue;
                    Rule cr = null; float minH = 1f, refFrac = 1f; string hide = null;
                    if (n == "Static_PalmTrunk") { cr = new Rule(false, "Tree_Palm_A", "Tree_Palm_B"); minH = 2.5f; refFrac = 0.92f; hide = "Static_PalmLeaf"; }
                    else if (n.EndsWith("_Static_G_Trunk")) { cr = new Rule(false, "Tree_Dead_A", "Tree_Dead_B"); minH = 1.5f; refFrac = 0.62f; }
                    else if (n.EndsWith("_FD2_Bark")) { cr = new Rule(false, "Tree_Dead37_A", "Tree_Dead37_B"); minH = 1.0f; hide = "_FD3_DeadLeaf"; }
                    if (cr != null)
                    {
                        List<Bounds> comps = Components(mf.sharedMesh, t.localToWorldMatrix, minH);
                        if (comps.Count == 0) { Debug.LogWarning("[Foliage] 덩어리를 못 나눔(메시 읽기 불가?): " + Path(t)); continue; }
                        foreach (Bounds c in comps) { Place(cr, c, getParent(), null, refFrac); Add(count, n); total++; }
                        Undo.RecordObject(rr, "Foliage swap"); rr.enabled = false;
                        if (hide != null && t.parent != null)
                            foreach (Transform sib in t.parent)
                                if (sib.name == hide || sib.name.EndsWith(hide)) { Renderer sr = sib.GetComponent<Renderer>(); if (sr != null) { Undo.RecordObject(sr, "Foliage swap"); sr.enabled = false; } }
                        continue;
                    }
                }
                // 4) 41 정령의 나무: 이름에 ARC_Spirit 이 든 메시 전부를 한 그루로
                var spirit = new List<Renderer>();
                foreach (Transform t in all) if (t != null && t.name.Contains("ARC_Spirit")) { Renderer r = t.GetComponent<Renderer>(); if (r != null && r.enabled && t.gameObject.activeInHierarchy) spirit.Add(r); }
                Bounds sb;
                if (spirit.Count > 0 && RendBounds(spirit.ToArray(), out sb))
                {
                    Place(new Rule(false, "Tree_Spirit41"), sb, getParent(), null);
                    foreach (Renderer r in spirit) { Undo.RecordObject(r, "Foliage swap"); r.enabled = false; }
                    Add(count, "ARC_Spirit(41)"); total++;
                }
                if (total > 0) EditorSceneManager.MarkSceneDirty(sc);
            }
            var sbd = new System.Text.StringBuilder();
            foreach (var kv in count) sbd.Append(kv.Key).Append(" ").Append(kv.Value).Append(", ");
            Debug.Log("[Foliage] 교체 " + total + "그루 — " + sbd + " (씬 저장은 직접)");
        }

        [MenuItem("Tools/LotteWorld/Look/나무 교체 되돌리기 (열린 씬)")]
        public static void Revert()
        {
            int back = 0, removed = 0;
            for (int si = 0; si < SceneManager.sceneCount; si++)
            {
                Scene sc = SceneManager.GetSceneAt(si); if (!sc.isLoaded) continue;
                foreach (GameObject r in sc.GetRootGameObjects())
                {
                    if (r.name == NewRoot) { removed += r.transform.childCount; Undo.DestroyObjectImmediate(r); continue; }
                    foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
                    {
                        GameObject go = t.gameObject; string b = Suffix.Replace(go.name, ""); string n = go.name;
                        GameObject src = PrefabUtility.IsAnyPrefabInstanceRoot(go) ? PrefabUtility.GetCorrespondingObjectFromSource(go) : null;
                        if (!go.activeSelf && ((src != null && PrefabRules.ContainsKey(src.name)) || (PrefabUtility.IsAnyPrefabInstanceRoot(go) && PrefabRules.ContainsKey(b))))
                        { Undo.RecordObject(go, "Foliage revert"); go.SetActive(true); back++; continue; }
                        bool mesh = DecoTree.IsMatch(n) || n == "Deco_RoundBed_Tree" || HasDecoTreeParent(t) || n == "Static_PalmTrunk" || n == "Static_PalmLeaf" || n.EndsWith("_Static_G_Trunk") || n.EndsWith("_FD2_Bark") || n.EndsWith("_FD3_DeadLeaf") || n.Contains("ARC_Spirit");
                        if (mesh) { Renderer rr = go.GetComponent<Renderer>(); if (rr != null && !rr.enabled) { Undo.RecordObject(rr, "Foliage revert"); rr.enabled = true; back++; } }
                    }
                }
                EditorSceneManager.MarkSceneDirty(sc);
            }
            Debug.Log("[Foliage] 되돌림: 옛 나무 " + back + "개 다시 켬, 새 나무 " + removed + "그루 지움");
        }

        // ------------------------------------------------------------ helpers
        static bool HasDecoTreeParent(Transform t) { for (Transform p = t.parent; p != null; p = p.parent) if (DecoTree.IsMatch(p.name) || p.name == "Deco_RoundBed_Tree") return true; return false; }
        static bool InsideDone(Transform t, HashSet<Transform> done) { for (Transform p = t.parent; p != null; p = p.parent) if (done.Contains(p)) return true; return false; }
        static void Add(Dictionary<string, int> d, string k) { int v; d.TryGetValue(k, out v); d[k] = v + 1; }
        static string Path(Transform t) { string s = t.name; for (Transform p = t.parent; p != null; p = p.parent) s = p.name + "/" + s; return s; }

        static bool RendBounds(Renderer[] rs, out Bounds b)
        {
            b = new Bounds(); bool any = false;
            foreach (Renderer r in rs) { if (r == null || !r.enabled) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return any && b.size.y > 0.05f;
        }

        static GameObject Prefab(string name)
        {
            GameObject p; if (prefabCache.TryGetValue(name, out p)) return p;
            p = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + name + ".prefab"); prefabCache[name] = p;
            if (p == null) Debug.LogError("[Foliage] 프리팹 없음: " + name);
            return p;
        }

        static float PrefabHeight(string name, GameObject inst)
        {
            float h; if (heightCache.TryGetValue(name, out h)) return h;
            var rs = new List<Renderer>();
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true)) if (!r.name.EndsWith("_Pit") && !r.transform.parent.name.EndsWith("_LOD1")) rs.Add(r);
            Bounds b; h = RendBounds(rs.ToArray(), out b) ? b.max.y - inst.transform.position.y : 1f; heightCache[name] = Mathf.Max(0.1f, h); return heightCache[name];
        }

        static void Place(Rule rule, Bounds old, Transform parent, float? yaw, float refFrac = 1f)
        {
            Vector3 pos = new Vector3(old.center.x, old.min.y, old.center.z);
            int hsh = Mathf.Abs(Mathf.RoundToInt(pos.x * 7.3f) * 73856093 ^ Mathf.RoundToInt(pos.z * 5.1f) * 19349663);
            string name = rule.Variants[hsh % rule.Variants.Length]; GameObject pf = Prefab(name); if (pf == null) return;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent.gameObject.scene);
            inst.transform.SetParent(parent, false); inst.transform.position = pos;
            Quaternion baseRot = pf.transform.rotation; Vector3 baseScale = pf.transform.localScale;      // FBX 루트 회전(세우는 회전)·스케일은 그대로 둠
            inst.transform.rotation = baseRot; inst.transform.localScale = baseScale;
            float h = PrefabHeight(name, inst);
            float s = Mathf.Clamp(old.size.y / (h * refFrac), 0.3f, 3f);
            inst.transform.localScale = baseScale * s;
            inst.transform.rotation = Quaternion.Euler(0f, yaw.HasValue ? yaw.Value : (hsh % 360), 0f) * baseRot;
            foreach (Transform c in inst.GetComponentsInChildren<Transform>(true)) if (c.name.EndsWith("_Pit")) c.gameObject.SetActive(rule.Pit);
            LODGroup lg = inst.GetComponent<LODGroup>(); if (lg != null) lg.RecalculateBounds();
            Undo.RegisterCreatedObjectUndo(inst, "Foliage swap");
        }

        /// <summary>메시를 이어진 덩어리로 나눠 월드 경계 상자 목록으로 (높이 minH 이상만)</summary>
        static List<Bounds> Components(Mesh mesh, Matrix4x4 l2w, float minH)
        {
            var res = new List<Bounds>();
            Vector3[] vs; int[] tr;
            try { vs = mesh.vertices; tr = mesh.triangles; } catch { return res; }
            if (vs == null || vs.Length == 0 || tr == null || tr.Length == 0)
            {
                string ap = AssetDatabase.GetAssetPath(mesh); var mi = AssetImporter.GetAtPath(ap) as ModelImporter;
                if (mi != null && !mi.isReadable) { mi.isReadable = true; mi.SaveAndReimport(); }
                return res;       // 다시 한 번 메뉴 실행
            }
            int n = vs.Length; int[] par = new int[n]; for (int i = 0; i < n; i++) par[i] = i;
            System.Func<int, int> find = null; find = x => { while (par[x] != x) { par[x] = par[par[x]]; x = par[x]; } return x; };
            var weld = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < n; i++)
            {
                var k = new Vector3Int(Mathf.RoundToInt(vs[i].x * 500f), Mathf.RoundToInt(vs[i].y * 500f), Mathf.RoundToInt(vs[i].z * 500f)); int j;
                if (weld.TryGetValue(k, out j)) par[find(i)] = find(j); else weld[k] = i;
            }
            for (int i = 0; i < tr.Length; i += 3) { int a = find(tr[i]), b = find(tr[i + 1]), c = find(tr[i + 2]); par[b] = a; par[find(c)] = a; }
            var bs = new Dictionary<int, Bounds>();
            for (int i = 0; i < n; i++)
            {
                int r = find(i); Vector3 w = l2w.MultiplyPoint3x4(vs[i]); Bounds b;
                if (bs.TryGetValue(r, out b)) { b.Encapsulate(w); bs[r] = b; } else bs[r] = new Bounds(w, Vector3.zero);
            }
            foreach (var kv in bs) if (kv.Value.size.y >= minH) res.Add(kv.Value);
            return res;
        }
    }
}
#endif
