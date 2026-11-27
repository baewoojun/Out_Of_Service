// MagicIslandFest.cs — 매직아일랜드 야외를 실내(LotteWorld_Fest)처럼 "놀이공원 느낌"으로 꾸미기
// 위치: Assets/Maps/LotteWorld/MagicIsland/Layout_v7/Editor/
// 메뉴: Tools > LotteWorld > Magic Island > Layout v7 > 8 놀이공원 꾸미기 (깃발 줄·풍선 수레·가게)
//
//  1) 깃발 줄: 길 가로등(08_Props/Lamp_*) 가까운 것끼리 꼭대기에 삼각 깃발 줄(Prop_Bunting12/18)을 건다.
//     줄이 지나는 자리에 건물·놀이기구·나무가 있으면 그 줄은 뺀다.
//  2) 길가 소품: 가로등 옆 길 가장자리 빈 자리에 풍선 수레·수레·먹거리/기념품 가게·식탁을 번갈아 놓는다 (길 안쪽을 보게).
//     자리 = 바닥이 길·광장이고, 반경 안에 다른 물건이 없고, 다른 Fest 소품과 떨어진 곳.
//  3) 놀이기구 앞: 기구마다 가장 가까운 길 쪽에 안내 간판(Prop_Sign) + 차단봉(Prop_Stanchions).
// 프리팹은 실내에서 쓰는 것 그대로 (Rides/Props/Prefabs, Rides/Deco/Prefabs).
// MI_Layout_v7/10_Fest 한 곳에만 만듦. 다시 누르면 10_Fest 만 지우고 새로 만듦 (같은 씨앗 = 같은 결과). Undo 됨. 씬 저장은 직접.
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LotteWorld.MagicIsland
{
    public static partial class MagicIslandLayoutV7
    {
        const string FestGroup = "10_Fest";
        const float LampTop = 4.75f;          // 가로등 뿌리에서 깃발 줄 거는 높이 (가로등 키 5.2 m)
        const float BuntingMin = 6f, BuntingMax = 24f;      // 길 가로등 간격이 20 m 남짓

        struct FestBox { public float x0, x1, z0, z1, y0, y1; public Transform owner; }

        [MenuItem(Menu + "8 놀이공원 꾸미기 (깃발 줄·풍선 수레·가게)", false, 18)]
        public static void FestMenu()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[MI v7] 플레이 중에는 꾸밀 수 없음. 플레이를 멈추고 다시 눌러줘"); return; }
            BuildFest();
        }

        public static void BuildFest()
        {
            var grp = Group(FestGroup, true);
            var scene = grp.gameObject.scene;
            var layout = grp.parent;
            Physics.SyncTransforms();

            // ---- 가로등
            var lamps = new List<Transform>();
            var props = layout.Find("08_Props");
            if (props != null) foreach (Transform c in props) if (c.name.StartsWith("Lamp_")) lamps.Add(c);
            lamps.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            // ---- 장애물 상자 (바닥·지형·배경 빼고 씬의 모든 렌더러). 나무는 줄기만 따로.
            var boxes = new List<FestBox>();       // 온전한 크기 (깃발 줄 검사)
            var ground = new List<FestBox>();      // 땅 위 물건 (소품 자리 검사) — 나무는 줄기 둘레만
            foreach (var root in scene.GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
                    var t = r.transform;
                    if (t.IsChildOf(grp)) continue;
                    if (Under(t, "03_Floors") || Under(t, "01_Ground") || Under(t, "09_Background") || Under(t, "01_Terrain") || Under(t, "02_Water")) continue;
                    var b = r.bounds;
                    if (b.size.y < 0.12f) continue;                       // 바닥 무늬 같은 납작한 것
                    // 놀이기구 레일·합친 건물 벽처럼 큰 덩어리는 상자가 실제보다 훨씬 커서 못 씀 → 그런 것은 콜라이더(Physics)로 검사
                    if ((b.size.x > 20f || b.size.z > 20f) && TreeRootOf(t) == null) continue;
                    var treeRoot = TreeRootOf(t);
                    var bx = new FestBox { x0 = b.min.x, x1 = b.max.x, z0 = b.min.z, z1 = b.max.z, y0 = b.min.y, y1 = b.max.y, owner = treeRoot != null ? treeRoot : t };
                    if (treeRoot != null)
                    {
                        // 깃발 줄: 잎 덩어리는 상자보다 작으니 70%만. 소품 자리: 줄기 둘레 1.3 m
                        var c = b.center; float ex = b.extents.x * 0.7f, ez = b.extents.z * 0.7f;
                        boxes.Add(new FestBox { x0 = c.x - ex, x1 = c.x + ex, z0 = c.z - ez, z1 = c.z + ez, y0 = b.min.y + 2.2f, y1 = b.max.y, owner = treeRoot });
                        var p = treeRoot.position;
                        ground.Add(new FestBox { x0 = p.x - 1.3f, x1 = p.x + 1.3f, z0 = p.z - 1.3f, z1 = p.z + 1.3f, y0 = p.y, y1 = p.y + 3f, owner = treeRoot });
                    }
                    else { boxes.Add(bx); ground.Add(bx); }
                }

            int nBunting = 0, nStall = 0, nSign = 0;
            try
            {
                nBunting = FestBunting(grp, lamps, boxes);
                var placed = new List<Vector3>();
                nStall = FestPlazas(grp, layout, ground, placed);          // 광장 가운데 먼저 (자리 우선)
                nStall += FestStalls(grp, lamps, ground, placed);
                nSign = FestRideSigns(grp, layout, ground, placed);
            }
            finally { EditorUtility.ClearProgressBar(); }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(string.Format("[MI v7] 놀이공원 꾸미기: 깃발 줄 {0}, 수레·가게·식탁 {1}, 기구 앞 간판·차단봉 {2} -> {3}", nBunting, nStall, nSign, FestGroup));
        }

        static bool Under(Transform t, string name)
        {
            for (var p = t; p != null; p = p.parent) if (p.name == name) return true;
            return false;
        }
        static Transform TreeRootOf(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.name.StartsWith("Tree_") && !p.name.Contains("_LOD")) return p;
            return null;
        }

        static GameObject FestPut(Transform parent, string path, Vector3 pos, float yaw, Vector3 scale)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab");
            if (pf == null) { Debug.LogWarning("[MI v7] 프리팹 없음: " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(go, "MI v7 fest");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0) * pf.transform.localRotation;
            go.transform.localScale = Vector3.Scale(pf.transform.localScale, scale);
            return go;
        }
        static Transform FestHolder(Transform grp, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "MI v7 fest");
            go.transform.SetParent(grp, false);
            return go.transform;
        }

        // ------------------------------------------------------------ 1) 깃발 줄
        static int FestBunting(Transform grp, List<Transform> lamps, List<FestBox> boxes)
        {
            var holder = FestHolder(grp, "Fest_Bunting");
            var done = new HashSet<long>();
            var degree = new int[lamps.Count];
            int n = 0;
            for (int i = 0; i < lamps.Count; i++)
            {
                Progress("놀이공원 꾸미기", "깃발 줄 " + i, 0.5f * i / Mathf.Max(1, lamps.Count));
                // 가까운 순서로 둘까지
                var near = new List<int>();
                for (int j = 0; j < lamps.Count; j++)
                {
                    if (j == i) continue;
                    float d = FlatDist(lamps[i].position, lamps[j].position);
                    if (d >= BuntingMin && d <= BuntingMax && Mathf.Abs(lamps[i].position.y - lamps[j].position.y) < 1.5f) near.Add(j);
                }
                near.Sort((a, b) => FlatDist(lamps[i].position, lamps[a].position).CompareTo(FlatDist(lamps[i].position, lamps[b].position)));
                int linked = 0;
                foreach (int j in near)
                {
                    if (linked >= 2 || degree[i] >= 3) break;
                    if (degree[j] >= 3) continue;
                    long key = i < j ? (long)i * 10000 + j : (long)j * 10000 + i;
                    if (done.Contains(key)) { linked++; continue; }
                    Vector3 a = lamps[i].position + Vector3.up * LampTop, b = lamps[j].position + Vector3.up * LampTop;
                    if (!LineClear(a, b, boxes, lamps[i], lamps[j])) continue;
                    done.Add(key); degree[i]++; degree[j]++; linked++;

                    Vector3 dir = b - a; float len = new Vector2(dir.x, dir.z).magnitude - 0.5f;   // 가로등 기둥 굵기만큼 안쪽
                    bool longOne = len > 14.5f;
                    float yaw = Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg;                          // 깃발 줄은 X 방향으로 뻗음
                    FestPut(holder, PropPf + (longOne ? "Prop_Bunting18" : "Prop_Bunting12"), (a + b) * 0.5f, yaw,
                        new Vector3(len / (longOne ? 18.2f : 12.2f), 1f, 1f));
                    n++;
                }
            }
            return n;
        }

        static float FlatDist(Vector3 a, Vector3 b) { return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)); }

        // 줄(a→b, 아래로 1.6 m 처짐 포함)이 지나는 곳에 다른 물건 상자가 있으면 false
        static bool LineClear(Vector3 a, Vector3 b, List<FestBox> boxes, Transform la, Transform lb)
        {
            int steps = Mathf.CeilToInt(FlatDist(a, b) / 0.5f);
            for (int s = 1; s < steps; s++)
            {
                var p = Vector3.Lerp(a, b, (float)s / steps);
                foreach (var bx in boxes)
                {
                    if (p.x < bx.x0 || p.x > bx.x1 || p.z < bx.z0 || p.z > bx.z1) continue;
                    if (bx.y1 < p.y - 1.6f || bx.y0 > p.y + 0.3f) continue;
                    if (bx.owner != null && (bx.owner.IsChildOf(la) || bx.owner.IsChildOf(lb))) continue;
                    return false;
                }
            }
            // 큰 덩어리(놀이기구·건물)는 콜라이더로: 줄보다 조금 아래, 가로등에서 1.2 m 안쪽 구간
            var dir = (b - a).normalized;
            if (Physics.CheckCapsule(a + dir * 1.2f - Vector3.up * 0.7f, b - dir * 1.2f - Vector3.up * 0.7f, 0.75f, ~0, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }

        // ------------------------------------------------------------ 2) 길가 수레·가게
        static readonly string[] FestItems =
        {
            "P:Prop_Cart_Balloons", "D:Deco_Food_Popcorn", "P:Prop_Cart_Balloons", "D:Deco_Shop_Headbands", "P:Prop_Cart", "D:Deco_Food_Churros",
            "P:Prop_Cart_Balloons", "D:Deco_Shop_Souvenir", "P:Prop_Dining_Cloth", "D:Deco_Food_IceCream", "P:Prop_Cart_Balloons", "D:Deco_Shop_Balloons",
        };

        static bool OnPavement(Vector3 p)
        {
            foreach (var h in Physics.RaycastAll(new Vector3(p.x, p.y + 30f, p.z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                var n = h.collider.name;
                if ((n.StartsWith("Path") || n.StartsWith("Plaza")) && Mathf.Abs(h.point.y - p.y) < 0.6f) return true;
            }
            return false;
        }

        // 반지름 r 원 안(땅에서 3 m 높이까지)에 물건이 없으면 true
        static bool SpotFree(Vector3 p, float r, List<FestBox> ground, List<Vector3> placed, float minGap)
        {
            foreach (var bx in ground)
            {
                if (bx.y1 < p.y + 0.15f || bx.y0 > p.y + 3f) continue;
                float cx = Mathf.Clamp(p.x, bx.x0, bx.x1), cz = Mathf.Clamp(p.z, bx.z0, bx.z1);
                if ((cx - p.x) * (cx - p.x) + (cz - p.z) * (cz - p.z) < r * r) return false;
            }
            foreach (var q in placed) if (FlatDist(p, q) < minGap) return false;
            // 큰 덩어리(놀이기구·건물)는 콜라이더로: 바닥에서 0.4 m 띄운 상자
            if (Physics.CheckBox(p + Vector3.up * 1.7f, new Vector3(r * 0.7f, 1.3f, r * 0.7f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }

        // ------------------------------------------------------------ 광장 가운데: 풍선 가게 + 식탁 + 풍선 수레
        static int FestPlazas(Transform grp, Transform layout, List<FestBox> ground, List<Vector3> placed)
        {
            var floors = layout.Find("03_Floors");
            if (floors == null) return 0;
            var holder = FestHolder(grp, "Fest_Plaza");
            string[] center = { "D:Deco_Shop_Balloons", "D:Deco_Food_IceCream", "D:Deco_Shop_Souvenir", "D:Deco_Food_Popcorn" };
            int n = 0, k = 0;
            foreach (Transform f in floors)
            {
                if (!f.name.StartsWith("Plaza")) continue;
                var r = f.GetComponent<Renderer>();
                if (r == null) continue;
                var c = r.bounds.center; c.y = GroundY(c.x, c.z);
                float rad = Mathf.Min(r.bounds.extents.x, r.bounds.extents.z);
                if (!OnPavement(c) || !SpotFree(c, 3.2f, ground, placed, 8f)) { k++; continue; }
                string spec = center[k++ % center.Length];
                float yaw = k * 90f;
                if (FestPut(holder, DecoPf + spec.Substring(2), c, yaw, Vector3.one) == null) continue;
                placed.Add(c); n++;
                // 둘레에 식탁·풍선 수레·수레를 고리로 (광장이 넓을수록 많이)
                float ring = Mathf.Clamp(rad * 0.55f, 5f, 9f);
                int cnt = rad >= 10f ? 6 : 4;
                string[] around = { "Prop_Dining_Cloth", "Prop_Cart_Balloons", "Prop_Dining_Plain", "Prop_Cart", "Prop_Dining_Cloth", "Prop_Cart_Balloons" };
                for (int i = 0; i < cnt; i++)
                {
                    var d = Quaternion.Euler(0, yaw + 30f + i * 360f / cnt, 0) * Vector3.forward;
                    var p = c + d * ring; p.y = GroundY(p.x, p.z);
                    if (!OnPavement(p) || !SpotFree(p, 2.2f, ground, placed, 4f)) continue;
                    if (FestPut(holder, PropPf + around[i % around.Length], p, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg + 180f, Vector3.one) != null) { placed.Add(p); n++; }
                }
            }
            return n;
        }

        static int FestStalls(Transform grp, List<Transform> lamps, List<FestBox> ground, List<Vector3> placed)
        {
            var holder = FestHolder(grp, "Fest_Stalls");
            var rnd = new System.Random(1128);
            int n = 0, item = 0;
            for (int i = 0; i < lamps.Count; i++)
            {
                Progress("놀이공원 꾸미기", "수레·가게 " + i, 0.5f + 0.4f * i / Mathf.Max(1, lamps.Count));
                var lp = lamps[i].position;
                // 가로등에서 3.2 m 떨어진 길 가장자리 자리: 그 너머(6.5 m)는 길이 아닌 방향을 고른다
                float start = (float)rnd.NextDouble() * 360f;
                Vector3 best = Vector3.zero, bestDir = Vector3.zero; bool found = false;
                for (int k = 0; k < 12 && !found; k++)
                {
                    var d = Quaternion.Euler(0, start + k * 30f, 0) * Vector3.forward;
                    var p = lp + d * 3.2f;
                    p.y = GroundY(p.x, p.z);
                    if (!OnPavement(p) || !OnPavement(p + d * 1.6f) || !OnPavement(p - d * 1.6f)) continue;
                    var side = new Vector3(d.z, 0, -d.x);
                    if (!OnPavement(p + side * 1.8f) || !OnPavement(p - side * 1.8f)) continue;
                    if (OnPavement(lp + d * 7.5f)) continue;                 // 그 너머도 길이면 길 한가운데 → 건너뜀
                    if (!SpotFree(p, 2.6f, ground, placed, 11f)) continue;
                    best = p; bestDir = d; found = true;
                }
                if (!found) continue;
                string spec = FestItems[item++ % FestItems.Length];
                string path = (spec[0] == 'D' ? DecoPf : PropPf) + spec.Substring(2);
                float yaw = Mathf.Atan2(-bestDir.x, -bestDir.z) * Mathf.Rad2Deg;   // 길 안쪽(가로등 쪽)을 보게
                if (FestPut(holder, path, best, yaw, Vector3.one) != null) { placed.Add(best); n++; }
            }
            return n;
        }

        // ------------------------------------------------------------ 3) 놀이기구 앞 간판·차단봉
        static int FestRideSigns(Transform grp, Transform layout, List<FestBox> ground, List<Vector3> placed)
        {
            var rides = layout.Find("05_Rides");
            if (rides == null) return 0;
            var holder = FestHolder(grp, "Fest_RideFront");
            int n = 0, idx = 0;
            foreach (Transform ride in rides)
            {
                Progress("놀이공원 꾸미기", "기구 앞 " + ride.name, 0.9f + 0.1f * idx++ / Mathf.Max(1, rides.childCount));
                var rs = ride.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float rad = Mathf.Max(b.extents.x, b.extents.z);
                // 기구 둘레를 돌며 길 위이고 빈 자리를 찾는다 (기구 상자 바로 바깥)
                for (int k = 0; k < 24; k++)
                {
                    var d = Quaternion.Euler(0, k * 15f + idx * 37f, 0) * Vector3.forward;
                    // 상자 가장자리까지 거리 + 2 m
                    float tx = Mathf.Abs(d.x) > 1e-3f ? b.extents.x / Mathf.Abs(d.x) : 1e6f, tz = Mathf.Abs(d.z) > 1e-3f ? b.extents.z / Mathf.Abs(d.z) : 1e6f;
                    float edge = Mathf.Min(tx, tz);
                    var p = new Vector3(b.center.x, 0, b.center.z) + d * (edge + 3.4f);
                    p.y = GroundY(p.x, p.z);
                    if (!OnPavement(p) || !OnPavement(p + d * 2f)) continue;
                    if (!SpotFree(p, 2.6f, ground, placed, 6f)) continue;
                    float face = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;          // 길 쪽(기구 바깥)을 보게
                    FestPut(holder, PropPf + "Prop_Stanchions", p, face, Vector3.one);
                    var side = new Vector3(d.z, 0, -d.x);
                    FestPut(holder, PropPf + "Prop_Sign", p + side * 3.4f + d * 0.3f, face, Vector3.one);
                    placed.Add(p); n += 2;
                    break;
                }
            }
            return n;
        }
    }
}
#endif
