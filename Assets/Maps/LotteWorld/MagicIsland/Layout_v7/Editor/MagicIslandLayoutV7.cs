// MagicIslandLayoutV7.cs — 매직아일랜드 배치 v7 (편집기 db layout/main v92, 11/25) 를 씬에 놓기
// 위치: Assets/Maps/LotteWorld/MagicIsland/Layout_v7/Editor/MagicIslandLayoutV7.cs
// 데이터: Assets/Maps/LotteWorld/MagicIsland/Layout_v7/mi_layout_v7.json (export_unity.py 로 만든 것)
//
// 메뉴 Tools > LotteWorld > Magic Island > Layout v7 >
//   0 전부 만들기 (1~5)
//   1 놀이기구 FBX 놓기        Rides/MIxx/MIxx.fbx 11개. 블렌더에서 제자리(모듈 좌표)에 만든 것인지,
//                              원점에 만든 것인지 바운즈로 자동 판단해서 놓음 (콘솔에 둘 다 겹침 비율 찍음)
//   2 가게·편의시설 블록 놓기   음식점 41~50, 상점 20·21, WC·의무실, 마을 집, 노점 → 자리표시 상자(앞 = +Z, 문 표시)
//   3 길·광장·잔디·물 바닥     미리 삼각형으로 나눈 평면 메시 (섬 바닥 -6 m 위로 3~7 cm)
//   4 섬·호수 지형 새로 만들기   새 섬 외곽선(옛 v1보다 약 40% 큼)으로 섬 바닥 -6 m (석호·풀·37/38 계단 구멍),
//                              호수 물 -7.2 / 바닥 -9 / 호숫가 0 m 띠, 섬·호숫가·석호·풀 옹벽, 섬·호숫가 난간.
//                              옛 v1 01_Terrain·02_Water 는 끔 (v1 섬은 바닥 0 m, 물 -1.2 m 라 새 섬을 덮음)
//   5 소품 놓기                길 따라 가로등 46·벤치 19·쓰레기통 19 (있는 프리팹 쓰고, 없으면 단순 모양)
//   6 동작 스크립트 붙이기      AtlantisCoaster·GyroSwing … 이 프로젝트에 있으면 MIxx 루트에 붙임 (경로 txt 는 직접 연결)
//   옛 v1 그룹 끄기 / 켜기       03_Paths·04_Buildings·05_Rides·06_Facilities·08_Props·Island_Ground (지우지 않음)
//   v7 지우기
// 모두 MagicIsland 루트 아래 MI_Layout_v7 한 곳에만 만듦. 다시 누르면 그 칸만 지우고 새로 만듦. Undo 됨. 씬 저장은 직접.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.MagicIsland
{
    public static class MagicIslandLayoutV7
    {
        const string Dir = "Assets/Maps/LotteWorld/MagicIsland/Layout_v7";
        const string JsonPath = Dir + "/mi_layout_v7.json";
        const string MatDir = Dir + "/Materials";
        const string MeshDir = Dir + "/Meshes";
        const string RidesDir = "Assets/Maps/LotteWorld/Rides";
        const string RootName = "MagicIsland";
        static readonly Vector3 RootPos = new Vector3(-49.95f, 0.03f, 126.9f);
        const string GroupName = "MI_Layout_v7";
        const string Menu = "Tools/LotteWorld/Magic Island/Layout v7/";

        // ---------------- json ----------------
        [Serializable] public class Item { public string id, name, kind, shape, ride; public float x, z, w, d, yaw, r; public float[] bx; }
        [Serializable] public class Prop { public string kind; public float x, z, yaw; }
        [Serializable] public class MeshData { public string name, layer; public float y, uv; public float[] v; public int[] t; }
        [Serializable] public class WallData { public string name, layer; public float top, bottom; public float[] pts; }
        [Serializable] public class RailData { public string name; public float y, h; public float[] pts; public float[] gaps; }
        [Serializable] public class Data { public string source; public float floorY; public Item[] items; public Prop[] props; public MeshData[] meshes; public WallData[] walls; public RailData[] rails; }

        static Data Load()
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            string txt = ta != null ? ta.text : (File.Exists(JsonPath) ? File.ReadAllText(JsonPath) : null);
            if (txt == null) { Debug.LogError("[MI v7] 배치 json 이 없어: " + JsonPath); return null; }
            var d = JsonUtility.FromJson<Data>(txt);
            if (d == null || d.items == null) { Debug.LogError("[MI v7] json 읽기 실패"); return null; }
            ReadGaps(txt, d);
            return d;
        }
        // json 의 난간 틈은 "gaps":[[x,z,r],...] 중첩 배열 → JsonUtility 가 못 읽어서 늘 비어 있었음.
        // 나오는 순서대로(= rails 순서) 직접 읽어 x,z,r 평평한 배열로 넣음.
        static void ReadGaps(string txt, Data d)
        {
            if (d.rails == null) return;
            var ms = System.Text.RegularExpressions.Regex.Matches(txt, "\"gaps\"\\s*:\\s*\\[((?:\\s*\\[[^\\]]*\\]\\s*,?)*)\\s*\\]");
            for (int i = 0; i < d.rails.Length && i < ms.Count; i++)
            {
                var nums = new List<float>();
                foreach (System.Text.RegularExpressions.Match n in System.Text.RegularExpressions.Regex.Matches(ms[i].Groups[1].Value, "-?[0-9]+(?:\\.[0-9]+)?(?:[eE][-+]?[0-9]+)?"))
                    nums.Add(float.Parse(n.Value, System.Globalization.CultureInfo.InvariantCulture));
                d.rails[i].gaps = nums.ToArray();
            }
        }

        // ---------------- 씬 그룹 ----------------
        // 열린 씬 전부에서 MagicIsland 루트를 찾음. 야외맵 씬(Map3_MagicIsland) 것을 먼저 씀.
        // (실내 씬만 활성인 채로 눌러서 실내 씬에 새 루트가 따로 생긴 경우 → 그 빈 루트·v7 묶음은 지움)
        static List<Transform> AllRoots()
        {
            var r = new List<Transform>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i); if (!sc.isLoaded) continue;
                foreach (var g in sc.GetRootGameObjects()) if (g.name == RootName) r.Add(g.transform);
            }
            return r;
        }
        static bool OnlyV7(Transform t) { foreach (Transform c in t) if (c.name != GroupName) return false; return true; }
        static Transform cachedRoot;
        static Transform Root()
        {
            if (cachedRoot != null) return cachedRoot;
            var roots = AllRoots();
            Transform pick = roots.FirstOrDefault(t => t.gameObject.scene.name == "Map3_MagicIsland")
                          ?? roots.FirstOrDefault(t => !OnlyV7(t)) ?? roots.FirstOrDefault();
            if (pick == null)
            {
                var r = new GameObject(RootName); r.transform.position = RootPos;
                Undo.RegisterCreatedObjectUndo(r, "MI v7 root");
                Debug.LogWarning("[MI v7] 열린 씬에 MagicIsland 루트가 없어서 활성 씬에 새로 만듦. 야외맵 씬(Map3_MagicIsland)을 같이 열고 하는 게 맞음");
                pick = r.transform;
            }
            foreach (var t in roots)
            {
                if (t == pick) continue;
                var v7 = t.Find(GroupName);
                if (OnlyV7(t)) { Debug.Log("[MI v7] " + t.gameObject.scene.name + " 씬에 따로 생겼던 빈 MagicIsland 루트 지움"); Undo.DestroyObjectImmediate(t.gameObject); }
                else if (v7 != null) { Debug.Log("[MI v7] " + t.gameObject.scene.name + " 씬의 MI_Layout_v7 지움 (한 곳에만 둠)"); Undo.DestroyObjectImmediate(v7.gameObject); }
            }
            Debug.Log("[MI v7] 만드는 곳: " + pick.gameObject.scene.name + " 씬 / MagicIsland " + pick.position);
            if (pick.gameObject.scene.name != "Map3_MagicIsland")
                Debug.LogWarning("[MI v7] 야외맵 씬이 안 열려 있음 — 옛 v1 지형·기구를 끌 수 없어. Magic Island > Open Indoor + Outdoor Together 로 같이 열고 다시 해줘");
            cachedRoot = pick;
            EditorApplication.delayCall += () => cachedRoot = null;
            return pick;
        }
        static Transform Group(string sub, bool clear)
        {
            var root = Root();
            var g = root.Find(GroupName);
            if (g == null) { var go = new GameObject(GroupName); Undo.RegisterCreatedObjectUndo(go, "MI v7"); g = go.transform; g.SetParent(root, false); }
            var s = g.Find(sub);
            if (s != null && clear) { Undo.DestroyObjectImmediate(s.gameObject); s = null; }
            if (s == null) { var go = new GameObject(sub); Undo.RegisterCreatedObjectUndo(go, "MI v7 " + sub); s = go.transform; s.SetParent(g, false); }
            return s;
        }
        static void Done(string what)
        {
            EditorUtility.ClearProgressBar();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[MI v7] " + what);
        }
        static bool Progress(string title, string info, float t)
        {
            return EditorUtility.DisplayCancelableProgressBar("매직아일랜드 v7 — " + title, string.Format("{0:0}%  {1}  ({2:HH:mm:ss})", t * 100f, info, DateTime.Now), t);
        }

        [MenuItem(Menu + "0 전부 만들기 (지형·바닥·기구·블록·소품)", false, 0)]
        public static void All()
        {
            var t0 = DateTime.Now;
            IslandGround(); Floors(); Rides(); Blocks(); Props(); SetOld(false);
            Debug.Log("[MI v7] 전부 끝 " + (DateTime.Now - t0).TotalSeconds.ToString("0") + "초. 씬 저장은 직접(Ctrl+S)");
        }

        // ---------------- 1 놀이기구 ----------------
        [MenuItem(Menu + "1 놀이기구 FBX 놓기", false, 11)]
        public static void Rides()
        {
            var d = Load(); if (d == null) return;
            var rides = d.items.Where(i => !string.IsNullOrEmpty(i.ride)).ToList();
            // 구운 FBX 재질·축 설정이 아직 안 됐으면(bakeAxisConversion 꺼짐) 먼저 Setup Ride Materials 실행
            bool needSetup = false;
            foreach (var it in rides)
            {
                var f = FindRide(it.ride); if (f == null) continue;
                var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(f)) as ModelImporter;
                if (imp != null && !imp.bakeAxisConversion) { needSetup = true; break; }
            }
            if (needSetup)
            {
                Debug.Log("[MI v7] 기구 FBX 재질·축 설정이 안 돼 있어서 Setup Ride Materials 먼저 실행");
                if (!EditorApplication.ExecuteMenuItem("Tools/LotteWorld/Magic Island/Setup Ride Materials"))
                    Debug.LogWarning("[MI v7] Setup Ride Materials 메뉴를 못 찾음 — 기구가 누워 있으면 그 메뉴를 먼저 누르고 다시 해줘");
            }
            var grp = Group("05_Rides", true);
            var log = new StringBuilder();
            int ok = 0, miss = 0, warn = 0;
            try
            {
                for (int n = 0; n < rides.Count; n++)
                {
                    var it = rides[n];
                    if (Progress("놀이기구", it.ride + " " + it.name, (float)n / rides.Count)) break;
                    var fbx = FindRide(it.ride);
                    if (fbx == null) { log.AppendLine("  ✗ " + it.ride + " FBX 없음 (" + RidesDir + "/" + it.ride + "/)"); miss++; continue; }
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, grp);
                    Undo.RegisterCreatedObjectUndo(inst, "MI v7 ride");
                    inst.name = it.ride;
                    Vector3 p0 = inst.transform.localPosition; Quaternion r0 = inst.transform.localRotation;
                    var exp = new Rect(it.bx[0], it.bx[1], it.bx[2] - it.bx[0], it.bx[3] - it.bx[1]);
                    // 가설 W: 블렌더에서 모듈 좌표 그대로 만들었음 → 원점에 두고 바닥만 -6
                    inst.transform.localPosition = p0 + new Vector3(0, d.floorY, 0); inst.transform.localRotation = r0;
                    Rect bw = FootRect(inst, grp.parent.parent); float iw = IoU(bw, exp);
                    // 가설 L: 원점 기준으로 만들었음 → 항목 자리·yaw 로 옮김
                    var q = Quaternion.Euler(0, it.yaw, 0);
                    inst.transform.localPosition = new Vector3(it.x, d.floorY, it.z) + q * p0; inst.transform.localRotation = q * r0;
                    Rect bl = FootRect(inst, grp.parent.parent); float il = IoU(bl, exp);
                    bool world = iw >= il;
                    if (world) { inst.transform.localPosition = p0 + new Vector3(0, d.floorY, 0); inst.transform.localRotation = r0; }
                    float best = Mathf.Max(iw, il);
                    string mark = best < 0.25f ? "⚠" : "✓"; if (best < 0.25f) warn++; else ok++;
                    log.AppendLine(string.Format("  {0} {1} {2}: {3}  겹침 제자리 {4:0.00} / 옮김 {5:0.00}  바운즈 x {6:0.0}..{7:0.0} z {8:0.0}..{9:0.0}  (기대 x {10:0.0}..{11:0.0} z {12:0.0}..{13:0.0})",
                        mark, it.ride, it.name, world ? "제자리" : "옮김", iw, il,
                        (world ? bw : bl).xMin, (world ? bw : bl).xMax, (world ? bw : bl).yMin, (world ? bw : bl).yMax, exp.xMin, exp.xMax, exp.yMin, exp.yMax));
                    foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
                        if (mf.gameObject.name.Contains("_Static_")) GameObjectUtility.SetStaticEditorFlags(mf.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Done(string.Format("놀이기구 {0}개 놓음, 확인 필요 {1}, FBX 없음 {2}\n{3}", ok + warn, warn, miss, log));
            if (warn > 0) Debug.LogWarning("[MI v7] ⚠ 표시 기구는 바운즈가 배치와 잘 안 맞아. 콘솔 줄을 그대로 알려줘 (축·뒤집힘 확인)");
        }
        static GameObject FindRide(string pre)
        {
            var p = RidesDir + "/" + pre + "/" + pre + ".fbx";
            var g = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (g != null) return g;
            foreach (var guid in AssetDatabase.FindAssets(pre + " t:Model", new[] { RidesDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == pre) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }
        // 메시 꼭짓점 바운즈를 MagicIsland 루트 로컬 XZ 로 (Renderer.bounds 갱신 시점에 안 기대려고 직접 계산)
        static Rect FootRect(GameObject go, Transform root)
        {
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds; var m = toRoot * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(c);
                    x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z);
                }
            }
            if (x0 > x1) return new Rect(0, 0, 0, 0);
            return Rect.MinMaxRect(x0, z0, x1, z1);
        }
        static float IoU(Rect a, Rect b)
        {
            float ix = Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
            float iz = Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
            float inter = ix * iz, uni = a.width * a.height + b.width * b.height - inter;
            return uni <= 0 ? 0 : inter / uni;
        }

        // ---------------- 2 가게·편의시설 블록 ----------------
        static readonly string[] SkipWords = { "에스컬레이터", "엘리베이터", "가로등", "포토존", "위 다리" };
        static bool RidePart(Item it)
        {
            // 33~42 놀이기구에 딸린 운전실·대기줄·계단·간판·조형은 기구 FBX 안에 있음
            if (it.kind != "fac" && it.kind != "arch" && it.kind != "bld" && it.kind != "ride") return false;
            if (it.kind == "ride") return true;
            if (it.name.StartsWith("아트란티스")) return true;
            int num; var head = it.name.Split(' ')[0];
            return int.TryParse(head, out num) && num >= 33 && num <= 42;
        }
        [MenuItem(Menu + "2 가게·편의시설 블록 놓기", false, 12)]
        public static void Blocks()
        {
            var d = Load(); if (d == null) return;
            var grp = Group("04_Blocks", true);
            var list = d.items.Where(i => string.IsNullOrEmpty(i.ride) && !RidePart(i) && !SkipWords.Any(w => i.name.Contains(w))
                                          && (i.kind == "food" || i.kind == "shop" || i.kind == "wc" || i.kind == "bld" || i.kind == "fac" || i.kind == "arch")).ToList();
            var cnt = new Dictionary<string, int>();
            for (int n = 0; n < list.Count; n++)
            {
                var it = list[n];
                Progress("가게·편의시설", it.name, (float)n / list.Count);
                string sub = it.kind == "food" ? (it.w * it.d < 20 ? "Kiosks" : "Food") : it.kind == "shop" ? "Shops" : it.kind == "wc" ? "Facilities" : it.kind == "bld" ? "Houses" : "Facilities";
                var parent = grp.Find(sub); if (parent == null) { parent = new GameObject(sub).transform; parent.SetParent(grp, false); }
                float y = it.id == "r48" ? 0f : d.floorY;   // 41 모리스딜라이트 = 위 다리(0 m)
                var go = it.kind == "arch" ? Arch(it) : (it.shape == "circle" ? Post(it) : Box(it));
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(it.x, y, it.z);
                go.transform.localRotation = Quaternion.Euler(0, it.yaw, 0);
                Undo.RegisterCreatedObjectUndo(go, "MI v7 block");
                cnt[sub] = cnt.ContainsKey(sub) ? cnt[sub] + 1 : 1;
            }
            Done("가게·편의시설 자리표시 " + list.Count + "개 (" + string.Join(", ", cnt.Select(kv => kv.Key + " " + kv.Value)) + ") — 진짜 건물 만들면 같은 자리에서 바꾸면 됨");
        }
        static float Height(Item it)
        {
            float area = it.w * it.d;
            switch (it.kind)
            {
                case "bld": return 8.5f;
                case "wc": return 3.6f;
                case "shop": return area < 12 ? 2.6f : 4.8f;
                case "food": return area < 20 ? 2.7f : area > 150 ? 6.5f : 4.8f;
                default: return 3f;
            }
        }
        static GameObject Box(Item it)
        {
            float h = Height(it);
            var go = new GameObject("Blk_" + it.id + "_" + Clean(it.name));
            Cube(go.transform, "Body", new Vector3(0, h / 2, 0), new Vector3(it.w, h, it.d), Mat(it.kind));
            Cube(go.transform, "Roof", new Vector3(0, h + 0.15f, 0), new Vector3(it.w + 0.6f, 0.3f, it.d + 0.6f), Mat("roof"));
            float dw = Mathf.Min(2.2f, it.w * 0.45f), dh = Mathf.Min(2.3f, h - 0.3f);
            var door = Cube(go.transform, "Door_Front", new Vector3(0, dh / 2, it.d / 2 + 0.03f), new Vector3(dw, dh, 0.08f), Mat("door"));
            UnityEngine.Object.DestroyImmediate(door.GetComponent<Collider>());
            return go;
        }
        static GameObject Post(Item it)
        {
            var go = new GameObject("Blk_" + it.id + "_" + Clean(it.name));
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder); c.name = "Body"; c.transform.SetParent(go.transform, false);
            c.transform.localPosition = new Vector3(0, 1.2f, 0); c.transform.localScale = new Vector3(it.r * 2, 1.2f, it.r * 2);
            c.GetComponent<Renderer>().sharedMaterial = Mat(it.kind);
            return go;
        }
        static GameObject Arch(Item it)
        {
            var go = new GameObject("Blk_" + it.id + "_" + Clean(it.name));
            float w = Mathf.Max(it.w, it.d), t = Mathf.Min(it.w, it.d);
            bool alongX = it.w >= it.d;
            for (int s = -1; s <= 1; s += 2)
            {
                var pos = alongX ? new Vector3(s * (w / 2 - 0.4f), 2.4f, 0) : new Vector3(0, 2.4f, s * (w / 2 - 0.4f));
                Cube(go.transform, s < 0 ? "Post_L" : "Post_R", pos, new Vector3(0.8f, 4.8f, 0.8f), Mat("arch"));
            }
            Cube(go.transform, "Beam", new Vector3(0, 5.1f, 0), alongX ? new Vector3(w, 0.8f, t) : new Vector3(t, 0.8f, w), Mat("arch"));
            return go;
        }
        static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = name; c.transform.SetParent(parent, false); c.transform.localPosition = pos; c.transform.localScale = size;
            c.GetComponent<Renderer>().sharedMaterial = m;
            return c;
        }
        static string Clean(string s) { foreach (var ch in "/\\:*?\"<>|") s = s.Replace(ch, '_'); return s; }

        // ---------------- 재질 ----------------
        static readonly Dictionary<string, Color> Col = new Dictionary<string, Color> {
            {"food", new Color(.72f,.45f,.50f)}, {"shop", new Color(.55f,.45f,.70f)}, {"wc", new Color(.38f,.58f,.55f)},
            {"bld", new Color(.62f,.52f,.42f)}, {"fac", new Color(.50f,.50f,.50f)}, {"arch", new Color(.60f,.35f,.50f)},
            {"roof", new Color(.30f,.28f,.30f)}, {"door", new Color(.10f,.09f,.08f)},
            {"ground", new Color(.66f,.61f,.50f)}, {"road", new Color(.80f,.72f,.57f)}, {"plaza", new Color(.75f,.66f,.50f)},
            {"lawn", new Color(.36f,.48f,.28f)}, {"water", new Color(.20f,.32f,.38f)},
            {"lakewater", new Color(.20f,.32f,.38f)}, {"bed", new Color(.16f,.18f,.17f)}, {"lakeside", new Color(.33f,.42f,.27f)}, {"wall", new Color(.42f,.40f,.37f)}, {"rail", new Color(.12f,.12f,.13f)},
            {"lamp", new Color(.15f,.15f,.16f)}, {"bench", new Color(.40f,.27f,.15f)}, {"bin", new Color(.20f,.24f,.20f)} };
        // 바닥은 옛 야외맵 이어지는 텍스처 재질이 있으면 그걸 씀
        static readonly Dictionary<string, string> Existing = new Dictionary<string, string> {
            {"ground", "MI_Paving"}, {"road", "MI_PavingDark"}, {"plaza", "MI_StoneGrey"}, {"lawn", "MI_Grass"}, {"water", "MI_Water"}, {"lakewater", "MI_Water"}, {"bed", "MI_LakeBed"}, {"lakeside", "MI_GrassDark"}, {"wall", "MI_StoneDark"} };
        static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();
        static Material Mat(string key)
        {
            Material m;
            if (MatCache.TryGetValue(key, out m) && m != null) return m;
            string ex;
            if (Existing.TryGetValue(key, out ex))
                foreach (var g in AssetDatabase.FindAssets(ex + " t:Material", new[] { "Assets/Maps/LotteWorld/MagicIsland" }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (Path.GetFileNameWithoutExtension(p) == ex) { m = AssetDatabase.LoadAssetAtPath<Material>(p); if (m != null) { MatCache[key] = m; return m; } }
                }
            string path = MatDir + "/MIv7_" + key + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Directory.CreateDirectory(MatDir);
                var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(sh);
                Color c; if (!Col.TryGetValue(key, out c)) c = Color.gray;
                m.SetColor("_BaseColor", c); m.color = c;
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", key == "water" ? 0.85f : 0.15f);
                AssetDatabase.CreateAsset(m, path);
            }
            MatCache[key] = m; return m;
        }

        // ---------------- 3·4 바닥 ----------------
        [MenuItem(Menu + "3 길·광장·잔디·물 바닥 만들기", false, 13)]
        public static void Floors() { BuildMeshes("03_Floors", l => l == "road" || l == "plaza" || l == "lawn" || l == "water", "길·광장·잔디·물"); }
        [MenuItem(Menu + "4 섬·호수 지형 새로 만들기", false, 14)]
        public static void IslandGround()
        {
            BuildMeshes("01_Ground", l => l == "ground" || l == "bed" || l == "lakeside" || l == "lakewater", "섬·호수 바닥");
            var d = Load(); if (d == null) return;
            var grp = Group("02_Edges", true);
            int wt = 0, rt = 0;
            if (d.walls != null) foreach (var w in d.walls) { var m = WallMesh(w); Save(m, grp, "wall", true); wt += m.triangles.Length / 3; }
            if (d.rails != null) foreach (var r in d.rails) { var m = RailMesh(r); Save(m, grp, "rail", true); rt += m.triangles.Length / 3; }
            foreach (var p in new[] { "01_Terrain", "02_Water" })
            {
                var old = FindOld(p);
                if (old != null && old.activeSelf) { Undo.RecordObject(old, "MI v7 old terrain"); old.SetActive(false); Debug.Log("[MI v7] 옛 v1 " + p + " 끔"); }
            }
            Done(string.Format("섬 둘레 옹벽 삼각형 {0}, 난간 삼각형 {1}", wt, rt));
        }
        static void Save(Mesh mesh, Transform grp, string matKey, bool collider)
        {
            Directory.CreateDirectory(MeshDir);
            string path = MeshDir + "/" + mesh.name + ".asset";
            var oldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (oldMesh != null) { EditorUtility.CopySerialized(mesh, oldMesh); mesh = oldMesh; } else AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(mesh.name.Replace("MIv7_", ""));
            go.transform.SetParent(grp, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(matKey);
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic);
            Undo.RegisterCreatedObjectUndo(go, "MI v7 edge");
        }
        // 닫힌 선을 따라 세운 벽 (양면)
        static Mesh WallMesh(WallData w)
        {
            int n = w.pts.Length / 2;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float run = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                var a = new Vector3(w.pts[2 * i], 0, w.pts[2 * i + 1]); var b = new Vector3(w.pts[2 * j], 0, w.pts[2 * j + 1]);
                float len = Mathf.Sqrt((b.x - a.x) * (b.x - a.x) + (b.z - a.z) * (b.z - a.z));
                int k = v.Count;
                v.Add(new Vector3(a.x, w.bottom, a.z)); v.Add(new Vector3(b.x, w.bottom, b.z)); v.Add(new Vector3(b.x, w.top, b.z)); v.Add(new Vector3(a.x, w.top, a.z));
                uv.Add(new Vector2(run / 4f, w.bottom / 4f)); uv.Add(new Vector2((run + len) / 4f, w.bottom / 4f)); uv.Add(new Vector2((run + len) / 4f, w.top / 4f)); uv.Add(new Vector2(run / 4f, w.top / 4f));
                t.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2, k, k + 1, k + 2, k, k + 2, k + 3 });
                run += len;
            }
            var m = new Mesh { name = "MIv7_" + w.name };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = v.ToArray(); m.uv = uv.ToArray(); m.triangles = t.ToArray();
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
        // 닫힌 선을 따라 기둥(2.4 m 간격) + 위 손잡이 + 가운데 가로대. gaps(x,z,r) 안은 비움
        static Mesh RailMesh(RailData r)
        {
            int n = r.pts.Length / 2;
            var v = new List<Vector3>(); var t = new List<int>();
            Func<float, float, bool> inGap = (x, z) =>
            {
                if (r.gaps == null) return false;
                for (int g = 0; g + 2 < r.gaps.Length; g += 3)
                { float dx = x - r.gaps[g], dz = z - r.gaps[g + 1]; if (dx * dx + dz * dz < r.gaps[g + 2] * r.gaps[g + 2]) return true; }
                return false;
            };
            float carry = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float ax = r.pts[2 * i], az = r.pts[2 * i + 1], bx = r.pts[2 * j], bz = r.pts[2 * j + 1];
                float len = Mathf.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az)); if (len < 0.01f) continue;
                // 손잡이·가로대: 구간을 0.5 m 조각으로 나눠 틈(gaps) 안 조각만 뺌.
                // (예전엔 구간 가운데 점만 봐서, 긴 구간이 틈을 지나가면 다리·선착장 입구를 그대로 막았음)
                int pieces = Mathf.Max(1, Mathf.CeilToInt(len / 0.5f));
                int runStart = -1;
                for (int pc = 0; pc <= pieces; pc++)
                {
                    bool open = pc < pieces && !inGap(ax + (bx - ax) * (pc + 0.5f) / pieces, az + (bz - az) * (pc + 0.5f) / pieces);
                    if (open && runStart < 0) runStart = pc;
                    if (!open && runStart >= 0)
                    {
                        float u0 = (float)runStart / pieces, u1 = (float)pc / pieces;
                        float sx = ax + (bx - ax) * u0, sz = az + (bz - az) * u0, ex = ax + (bx - ax) * u1, ez = az + (bz - az) * u1;
                        Seg(v, t, sx, sz, ex, ez, r.y + r.h - 0.04f, 0.08f, 0.07f);   // 손잡이
                        Seg(v, t, sx, sz, ex, ez, r.y + r.h * 0.5f, 0.04f, 0.04f);    // 가로대
                        runStart = -1;
                    }
                }
                float s = carry;
                for (; s < len; s += 2.4f)
                {
                    float px = ax + (bx - ax) * s / len, pz = az + (bz - az) * s / len;
                    if (!inGap(px, pz)) Post(v, t, px, pz, r.y, r.h, 0.07f);
                }
                carry = s - len;
            }
            var m = new Mesh { name = "MIv7_" + r.name };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = v.ToArray(); m.triangles = t.ToArray();
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
        static void Box(List<Vector3> v, List<int> t, Vector3[] c)
        {
            // c: 아래 4점(반시계) + 위 4점
            int k = v.Count; v.AddRange(c);
            int[] q = { 0, 1, 2, 3, 4, 7, 6, 5, 0, 4, 5, 1, 1, 5, 6, 2, 2, 6, 7, 3, 3, 7, 4, 0 };
            for (int f = 0; f < 6; f++)
            {
                int a = k + q[4 * f], b = k + q[4 * f + 1], cc = k + q[4 * f + 2], d = k + q[4 * f + 3];
                t.AddRange(new[] { a, b, cc, a, cc, d, a, cc, b, a, d, cc });   // 양면 (방향 실수 막기)
            }
        }
        static void Seg(List<Vector3> v, List<int> t, float ax, float az, float bx, float bz, float y, float w, float h)
        {
            float len = Mathf.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
            float nx = -(bz - az) / len * w / 2, nz = (bx - ax) / len * w / 2;
            Box(v, t, new[] {
                new Vector3(ax + nx, y - h, az + nz), new Vector3(bx + nx, y - h, bz + nz), new Vector3(bx - nx, y - h, bz - nz), new Vector3(ax - nx, y - h, az - nz),
                new Vector3(ax + nx, y, az + nz), new Vector3(bx + nx, y, bz + nz), new Vector3(bx - nx, y, bz - nz), new Vector3(ax - nx, y, az - nz) });
        }
        static void Post(List<Vector3> v, List<int> t, float x, float z, float y, float h, float r)
        {
            Box(v, t, new[] {
                new Vector3(x - r, y, z - r), new Vector3(x + r, y, z - r), new Vector3(x + r, y, z + r), new Vector3(x - r, y, z + r),
                new Vector3(x - r, y + h, z - r), new Vector3(x + r, y + h, z - r), new Vector3(x + r, y + h, z + r), new Vector3(x - r, y + h, z + r) });
        }
        static void BuildMeshes(string sub, Func<string, bool> pick, string what)
        {
            var d = Load(); if (d == null) return;
            var grp = Group(sub, true);
            Directory.CreateDirectory(MeshDir);
            var list = d.meshes.Where(m => pick(m.layer)).ToList();
            int tris = 0;
            for (int n = 0; n < list.Count; n++)
            {
                var md = list[n];
                Progress(what, md.name, (float)n / list.Count);
                int nv = md.v.Length / 2;
                var verts = new Vector3[nv]; var uv = new Vector2[nv]; var nor = new Vector3[nv];
                for (int i = 0; i < nv; i++)
                {
                    verts[i] = new Vector3(md.v[2 * i], 0, md.v[2 * i + 1]);
                    uv[i] = new Vector2(md.v[2 * i] / md.uv, md.v[2 * i + 1] / md.uv); nor[i] = Vector3.up;
                }
                var mesh = new Mesh { name = "MIv7_" + md.name };
                mesh.indexFormat = nv > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.vertices = verts; mesh.uv = uv; mesh.normals = nor; mesh.triangles = md.t;
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                string path = MeshDir + "/MIv7_" + md.name + ".asset";
                var oldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (oldMesh != null) { EditorUtility.CopySerialized(mesh, oldMesh); mesh = oldMesh; } else AssetDatabase.CreateAsset(mesh, path);
                var go = new GameObject(md.name);
                go.transform.SetParent(grp, false); go.transform.localPosition = new Vector3(0, md.y, 0);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = Mat(md.layer);
                if (md.layer != "water" && md.layer != "lakewater") go.AddComponent<MeshCollider>().sharedMesh = mesh;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic);
                Undo.RegisterCreatedObjectUndo(go, "MI v7 floor");
                tris += md.t.Length / 3;
            }
            AssetDatabase.SaveAssets();
            Done(what + " 메시 " + list.Count + "개, 삼각형 " + tris);
        }

        // ---------------- 5 소품 ----------------
        static readonly string[] PropDirs = { "Assets/Maps/LotteWorld/MagicIsland/Prefabs/Props", "Assets/Maps/LotteWorld/Rides/Props/Prefabs" };
        static List<GameObject> FindProps(params string[] words)
        {
            var r = new List<GameObject>();
            foreach (var dir in PropDirs)
            {
                if (!AssetDatabase.IsValidFolder(dir)) continue;
                foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { dir }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g); var n = Path.GetFileNameWithoutExtension(p);
                    if (words.Any(w => n.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)) r.Add(AssetDatabase.LoadAssetAtPath<GameObject>(p));
                }
                if (r.Count > 0) break;   // 앞 폴더(야외맵 것)에 있으면 그것만
            }
            return r;
        }
        [MenuItem(Menu + "5 소품 놓기 (가로등·벤치·쓰레기통)", false, 15)]
        public static void Props()
        {
            var d = Load(); if (d == null) return;
            var grp = Group("08_Props", true);
            var lamps = FindProps("Lamp", "가로등"); var benches = FindProps("Bench", "벤치"); var bins = FindProps("Bin", "Trash", "쓰레기");
            var rnd = new System.Random(1125);
            int n = 0, prim = 0;
            foreach (var p in d.props)
            {
                Progress("소품", p.kind, (float)n / d.props.Length);
                var pool = p.kind == "lamp" ? lamps : p.kind == "bench" ? benches : bins;
                GameObject go;
                if (pool.Count > 0)
                {
                    // 가로등은 반쯤 꺼진 것(Half/Off)을 30% 섞음
                    GameObject pf = pool[0];
                    if (p.kind == "lamp")
                    {
                        var on = pool.Where(x => x.name.Contains("On")).ToList(); var half = pool.Where(x => x.name.Contains("Half") || x.name.Contains("Off")).ToList();
                        pf = (half.Count > 0 && rnd.NextDouble() < 0.3) ? half[0] : (on.Count > 0 ? on[0] : pool[0]);
                    }
                    go = (GameObject)PrefabUtility.InstantiatePrefab(pf, grp);
                    var r0 = go.transform.localRotation;
                    go.transform.localPosition = new Vector3(p.x, d.floorY, p.z);
                    go.transform.localRotation = Quaternion.Euler(0, p.yaw, 0) * r0;
                }
                else
                {
                    go = SimpleProp(p.kind); go.transform.SetParent(grp, false); prim++;
                    go.transform.localPosition = new Vector3(p.x, d.floorY, p.z);
                    go.transform.localRotation = Quaternion.Euler(0, p.yaw, 0);
                }
                go.name = (p.kind == "lamp" ? "Lamp_" : p.kind == "bench" ? "Bench_" : "Bin_") + (n + 1).ToString("000");
                Undo.RegisterCreatedObjectUndo(go, "MI v7 prop");
                n++;
            }
            Done(string.Format("소품 {0}개 (프리팹: 가로등 {1}종·벤치 {2}종·쓰레기통 {3}종, 단순 모양 {4}개)", n, lamps.Count, benches.Count, bins.Count, prim));
        }
        static GameObject SimpleProp(string kind)
        {
            var go = new GameObject(kind);
            if (kind == "lamp")
            {
                Cube(go.transform, "Pole", new Vector3(0, 2.0f, 0), new Vector3(0.14f, 4.0f, 0.14f), Mat("lamp"));
                Cube(go.transform, "Head", new Vector3(0, 4.05f, 0), new Vector3(0.45f, 0.35f, 0.45f), Mat("lamp"));
            }
            else if (kind == "bench")
            {
                Cube(go.transform, "Seat", new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.08f, 0.45f), Mat("bench"));
                Cube(go.transform, "Back", new Vector3(0, 0.8f, -0.22f), new Vector3(1.6f, 0.5f, 0.06f), Mat("bench"));
                Cube(go.transform, "Leg_L", new Vector3(-0.7f, 0.22f, 0), new Vector3(0.08f, 0.44f, 0.45f), Mat("lamp"));
                Cube(go.transform, "Leg_R", new Vector3(0.7f, 0.22f, 0), new Vector3(0.08f, 0.44f, 0.45f), Mat("lamp"));
            }
            else
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder); c.name = "Body"; c.transform.SetParent(go.transform, false);
                c.transform.localPosition = new Vector3(0, 0.45f, 0); c.transform.localScale = new Vector3(0.5f, 0.45f, 0.5f);
                c.GetComponent<Renderer>().sharedMaterial = Mat("bin");
            }
            return go;
        }

        // ---------------- 6 동작 스크립트 ----------------
        static readonly Dictionary<string, string[]> Motion = new Dictionary<string, string[]> {
            {"MI33", new[]{"AtlantisCoaster"}}, {"MI34", new[]{"GyroSwing"}}, {"MI35", new[]{"PetitTrain"}}, {"MI36", new[]{"GyroDrop"}},
            {"MI37", new[]{"FantasyTrain","FantasyDarkFX","AnimatronicFX","FantasyAudio"}}, {"MI38", new[]{"CometExpress"}},
            {"MI39", new[]{"GyroSpin"}}, {"MI40", new[]{"StoneExpress"}}, {"MI41", new[]{"ArcanaWhip"}}, {"MI42", new[]{"EosTower"}} };
        [MenuItem(Menu + "6 동작 스크립트 붙이기 (있는 것만)", false, 16)]
        public static void AttachMotion()
        {
            var grp = Root().Find(GroupName + "/05_Rides");
            if (grp == null) { Debug.LogError("[MI v7] 먼저 1 놀이기구 놓기"); return; }
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t)).GroupBy(t => t.Name).ToDictionary(g => g.Key, g => g.First());
            var log = new StringBuilder();
            foreach (Transform r in grp)
            {
                string[] names; if (!Motion.TryGetValue(r.name, out names)) continue;
                foreach (var nm in names)
                {
                    Type t;
                    if (!types.TryGetValue(nm, out t)) { log.AppendLine("  ✗ " + r.name + " " + nm + " 스크립트 없음"); continue; }
                    if (r.GetComponent(t) == null) { Undo.AddComponent(r.gameObject, t); log.AppendLine("  ✓ " + r.name + " + " + nm); }
                    else log.AppendLine("  = " + r.name + " " + nm + " 이미 있음");
                }
            }
            Done("동작 스크립트\n" + log + "경로 txt(comet/atlantis/stone_path.txt)가 필요한 기구는 인스펙터에서 직접 연결");
        }

        // ---------------- 옛 v1 ----------------
        static readonly string[] OldGroups = { "01_Terrain", "02_Water", "03_Paths", "04_Buildings", "05_Rides", "06_Facilities", "08_Props" };
        static GameObject FindOld(string path) { foreach (var r in AllRoots()) { var t = r.Find(path); if (t != null) return t.gameObject; } return null; }
        [MenuItem(Menu + "옛 v1 그룹 끄기", false, 31)] public static void OldOff() { SetOld(false); }
        [MenuItem(Menu + "옛 v1 그룹 켜기", false, 32)] public static void OldOn() { SetOld(true); }
        static void SetOld(bool on)
        {
            var done = new List<string>();
            foreach (var p in OldGroups) { var g = FindOld(p); if (g == null) continue; Undo.RecordObject(g, "MI v1 toggle"); g.SetActive(on); done.Add(p); }
            Done("옛 v1 " + (on ? "켬" : "끔") + ": " + (done.Count > 0 ? string.Join(", ", done) : "찾은 그룹 없음"));
        }
        [MenuItem(Menu + "v7 지우기", false, 51)]
        public static void Clear()
        {
            var g = Root().Find(GroupName);
            if (g != null) Undo.DestroyObjectImmediate(g.gameObject);
            Done("MI_Layout_v7 지움");
        }
    }

}
#endif
