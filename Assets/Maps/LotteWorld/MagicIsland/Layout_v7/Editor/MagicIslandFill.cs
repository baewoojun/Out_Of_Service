// MagicIslandFill.cs — 매직아일랜드 v7 빈 바닥 꾸미기
// 위치: Assets/Maps/LotteWorld/MagicIsland/Layout_v7/Editor/
// 메뉴: Tools > LotteWorld > Magic Island > Layout v7 > 7 빈 곳 꾸미기
//
// 섬 바닥에서 길·놀이기구·건물·소품이 하나도 없는 곳을 1 m 칸으로 찾아(11/27 배치 기준) 넓은 순서로 고른 자리 38곳.
// 자리마다 넓이·길과의 거리에 맞춰 꾸밈 종류를 정함 (방향 = 가장 가까운 길 쪽):
//   Garden  둥근 정원: 가운데 벚나무, 꽃밭 고리, 벤치 4(가운데 보게), 가로등 2, 바깥 나무 고리
//   Snack   먹거리 마당: 아이스크림·츄러스·팝콘 가게(길 보게) + 식탁·쓰레기통·가로등·나무
//   Shops   기념품 마당: 기념품·풍선·머리띠 가게 + 풍선 수레 + 벤치
//   Grove   작은 숲: 나무 3~4, 덤불, 바위, 길 보는 벤치 (가로등은 정원·가게 마당에만 — 빛·삼각형 아끼려고)
//   Woods   길에서 먼 가장자리 숲: 소나무·활엽수, 큰 바위, 마른 풀
//   Rocks   바위 쉼터: 큰 바위 무리 + 소나무 + 벤치
//   Nook    좁은 틈: 길 가까우면 나무 + 벤치 + 화분, 멀면 나무 + 덤불
// 프리팹은 이미 만들어 둔 것만 씀 (Rides/Deco/Prefabs, Rides/Props/Prefabs, Foliage/Prefabs). 나무는 LOD 있는 Foliage 나무.
// MI_Layout_v7/09_Fill 한 곳에만 만듦. 다시 누르면 09_Fill 만 지우고 새로 만듦 (같은 씨앗이라 같은 결과). Undo 됨. 씬 저장은 직접.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LotteWorld.MagicIsland
{
    public static partial class MagicIslandLayoutV7
    {
        const string FillGroup = "09_Fill";
        const string DecoPf = "Assets/Maps/LotteWorld/Rides/Deco/Prefabs/";
        const string PropPf = "Assets/Maps/LotteWorld/Rides/Props/Prefabs/";
        const string FolPf = "Assets/Maps/LotteWorld/Foliage/Prefabs/";
        const float IslandFloorY = -5.97f;

        enum Kind { Garden, Snack, Shops, Grove, Woods, Rocks, Nook }

        struct Spot
        {
            public float x, z, r, yaw, pathDist; public Kind kind;
            public Spot(float x, float z, float r, float yaw, float pathDist, Kind kind) { this.x = x; this.z = z; this.r = r; this.yaw = yaw; this.pathDist = pathDist; this.kind = kind; }
        }

        // 월드 좌표 x, z / 빈 반지름 r / 가장 가까운 길 방향(도, +Z 기준 시계) / 길까지 거리 / 종류
        static readonly Spot[] FillSpots =
        {
            new Spot(-208.4f, 194.3f, 20.7f, 28f, 19f, Kind.Garden),
            new Spot(-232.4f, 251.3f, 15.6f, -45f, 16f, Kind.Snack),
            new Spot(-143.4f, 288.3f, 13.9f, 107f, 14f, Kind.Garden),
            new Spot(-197.4f, 130.3f, 13.0f, 2f, 35f, Kind.Rocks),
            new Spot(-175.4f, 197.3f, 12.4f, -60f, 14f, Kind.Shops),
            new Spot(-192.4f, 246.3f, 10.0f, -96f, 10f, Kind.Grove),
            new Spot(-24.4f, 292.3f, 9.9f, -67f, 49f, Kind.Woods),
            new Spot(-59.4f, 313.3f, 9.4f, -84f, 9f, Kind.Snack),
            new Spot(-232.4f, 212.3f, 9.0f, -21f, 9f, Kind.Grove),
            new Spot(-76.4f, 289.3f, 9.0f, -36f, 9f, Kind.Garden),
            new Spot(-104.4f, 295.3f, 9.0f, 63f, 9f, Kind.Grove),
            new Spot(-34.4f, 308.3f, 8.5f, -80f, 35f, Kind.Woods),
            new Spot(-115.4f, 273.3f, 7.8f, -90f, 8f, Kind.Nook),
            new Spot(-123.4f, 181.3f, 7.7f, -145f, 12f, Kind.Grove),
            new Spot(-216.4f, 139.3f, 7.1f, 38f, 33f, Kind.Rocks),
            new Spot(7.6f, 249.3f, 6.7f, -152f, 45f, Kind.Woods),
            new Spot(-185.4f, 179.3f, 6.2f, 180f, 8f, Kind.Nook),
            new Spot(-0.4f, 265.3f, 6.2f, -156f, 57f, Kind.Woods),
            new Spot(-173.4f, 152.3f, 6.0f, 63f, 9f, Kind.Nook),
            new Spot(-263.4f, 225.3f, 6.0f, 153f, 7f, Kind.Nook),
            new Spot(-101.4f, 265.3f, 6.0f, 121f, 6f, Kind.Nook),
            new Spot(-197.4f, 269.3f, 6.0f, 180f, 7f, Kind.Nook),
            new Spot(-8.4f, 277.3f, 5.7f, -159f, 65f, Kind.Woods),
            new Spot(-86.4f, 309.3f, 5.7f, -121f, 6f, Kind.Nook),
            new Spot(-211.4f, 220.3f, 5.4f, -11f, 5f, Kind.Nook),
            new Spot(-205.4f, 303.3f, 5.4f, 112f, 5f, Kind.Nook),
            new Spot(-93.4f, 324.3f, 5.4f, -156f, 12f, Kind.Nook),
            new Spot(-212.4f, 287.3f, 5.2f, 101f, 5f, Kind.Nook),
            new Spot(-181.4f, 139.3f, 5.0f, 57f, 20f, Kind.Nook),
            new Spot(-132.4f, 143.3f, 5.0f, -35f, 19f, Kind.Nook),
            new Spot(-139.4f, 211.3f, 5.0f, -90f, 5f, Kind.Nook),
            new Spot(-138.4f, 227.3f, 5.0f, 180f, 5f, Kind.Nook),
            new Spot(-238.4f, 230.3f, 5.0f, -143f, 5f, Kind.Nook),
            new Spot(-266.4f, 266.3f, 5.0f, 115f, 17f, Kind.Nook),
            new Spot(-131.4f, 303.3f, 5.0f, 90f, 5f, Kind.Nook),
            new Spot(-161.4f, 308.3f, 5.0f, 0f, 5f, Kind.Nook),
            new Spot(-118.4f, 319.3f, 5.0f, 180f, 5f, Kind.Nook),
            new Spot(-44.4f, 321.3f, 5.0f, -82f, 22f, Kind.Woods),
        };

        static readonly string[] LeafyTrees = { "Tree_Leafy_A", "Tree_Leafy_B", "Tree_Leafy_C", "Tree_Autumn_A", "Tree_Autumn_B" };
        static readonly string[] EdgeTrees = { "Tree_Pine_A", "Tree_Pine_B", "Tree_Leafy_C", "Tree_Autumn_B" };

        [MenuItem(Menu + "7 빈 곳 꾸미기", false, 17)]
        public static void FillEmpty()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[MI v7] 플레이 중에는 꾸밀 수 없음 (끝나면 사라짐). 플레이를 멈추고 다시 눌러줘"); return; }
            BuildFill();
        }

        // 플레이 중 미리보기용으로도 부름 (그때 만든 것은 플레이가 끝나면 사라짐)
        public static void BuildFill()
        {
            var grp = Group(FillGroup, true);
            var counts = new Dictionary<Kind, int>();
            int items = 0;
            Physics.SyncTransforms();
            try
            {
                for (int s = 0; s < FillSpots.Length; s++)
                {
                    var sp = FillSpots[s];
                    Progress("빈 곳 꾸미기", sp.kind + " " + s, (float)s / FillSpots.Length);
                    var holder = new GameObject(string.Format("Fill_{0:00}_{1}", s, sp.kind));
                    Undo.RegisterCreatedObjectUndo(holder, "MI v7 fill");
                    holder.transform.SetParent(grp, false);
                    holder.transform.position = new Vector3(sp.x, GroundY(sp.x, sp.z), sp.z);
                    var b = new Builder(holder.transform, sp, new System.Random(1127 + s * 31));
                    switch (sp.kind)
                    {
                        case Kind.Garden: b.Garden(); break;
                        case Kind.Snack: b.Court(new[] { "Deco_Food_IceCream", "Deco_Food_Churros", "Deco_Food_Popcorn" }, true); break;
                        case Kind.Shops: b.Court(new[] { "Deco_Shop_Souvenir", "Deco_Shop_Balloons", "Deco_Shop_Headbands" }, false); break;
                        case Kind.Grove: b.Grove(); break;
                        case Kind.Woods: b.Woods(); break;
                        case Kind.Rocks: b.RockRest(); break;
                        default: b.Nook(); break;
                    }
                    items += b.count;
                    counts[sp.kind] = counts.TryGetValue(sp.kind, out var c) ? c + 1 : 1;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(grp.gameObject.scene);
            var sb = new System.Text.StringBuilder();
            foreach (var kv in counts) sb.Append(kv.Key).Append(' ').Append(kv.Value).Append("  ");
            Debug.Log(string.Format("[MI v7] 빈 곳 꾸미기: 자리 {0}곳, 놓은 것 {1}개 ({2})", FillSpots.Length, items, sb));
        }

        // 섬 바닥·길·광장 윗면 높이 (없으면 섬 바닥 높이)
        static float GroundY(float x, float z)
        {
            float best = float.NegativeInfinity;
            foreach (var h in Physics.RaycastAll(new Vector3(x, 40f, z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                var n = h.collider.name;
                if (n == "Ground_Island" || n.StartsWith("Path") || n.StartsWith("Plaza")) best = Mathf.Max(best, h.point.y);
            }
            return float.IsNegativeInfinity(best) ? IslandFloorY : best;
        }

        class Builder
        {
            readonly Transform root; readonly Spot sp; readonly System.Random rnd;
            public int count;
            public Builder(Transform root, Spot sp, System.Random rnd) { this.root = root; this.sp = sp; this.rnd = rnd; }

            float R(float a, float b) { return a + (float)rnd.NextDouble() * (b - a); }
            string Pick(string[] a) { return a[rnd.Next(a.Length)]; }
            // 자리 기준 좌표: fwd = 길 쪽, side = 길 보고 오른쪽
            Vector3 Fwd { get { return Quaternion.Euler(0, sp.yaw, 0) * Vector3.forward; } }
            Vector3 Side { get { return Quaternion.Euler(0, sp.yaw + 90f, 0) * Vector3.forward; } }
            Vector3 Polar(float deg, float rad) { return Quaternion.Euler(0, sp.yaw + deg, 0) * Vector3.forward * rad; }

            GameObject Put(string path, Vector3 local, float yaw, float scale = 1f)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab");
                if (pf == null) { Debug.LogWarning("[MI v7] 프리팹 없음: " + path); return null; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, root.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(go, "MI v7 fill");
                go.transform.SetParent(root, false);
                var w = root.position + local;
                go.transform.position = new Vector3(w.x, GroundY(w.x, w.z), w.z);
                // FBX 프리팹은 뿌리에 -90° X 같은 회전·크기가 들어 있어서 그 위에 Y 회전만 얹음 (안 그러면 나무가 눕음)
                go.transform.rotation = Quaternion.Euler(0, yaw, 0) * pf.transform.localRotation;
                go.transform.localScale = pf.transform.localScale * scale;
                count++;
                return go;
            }
            void Tree(string name, Vector3 p) { Put(FolPf + name, p, R(0, 360), R(0.85f, 1.1f)); }
            void Bush(Vector3 p) { Put(FolPf + (rnd.NextDouble() < 0.7 ? "Bush_Green" : "Bush_Autumn"), p, R(0, 360), R(0.8f, 1.2f)); }
            void Rock(string size, Vector3 p) { Put(DecoPf + "Deco_Rock_" + size + "_" + "ABC"[rnd.Next(3)], p, R(0, 360), R(0.8f, 1.15f)); }
            // 벤치는 앞(+Z)을 봄. lookYaw = 앉은 사람이 보는 방향
            void Bench(Vector3 p, float lookYaw) { Put(PropPf + "Prop_Bench", p, lookYaw); }
            float YawOf(Vector3 dir) { return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; }

            public void Garden()
            {
                float r = sp.r;
                Put(FolPf + "Tree_Cherry_A", Vector3.zero, R(0, 360));
                for (int i = 0; i < 6; i++) { var p = Polar(i * 60f + 30f, 3.6f); Put(FolPf + "FlowerBed", p, YawOf(p) + 90f, 0.9f); }
                for (int i = 0; i < 4; i++) { var p = Polar(i * 90f + 45f, 6.4f); Bench(p, YawOf(-p)); }
                for (int i = 0; i < 2; i++) Put(PropPf + "Prop_Lamp_Half", Polar(90f + i * 180f, 7.4f), sp.yaw + i * 180f);   // 가로등은 무거워서(삼각형 9.6k·빛) 2개만
                Put(PropPf + "Prop_Bin", Polar(45f, 8.0f) + Side * 1.6f, sp.yaw);
                if (r >= 12f)
                {
                    float ring = Mathf.Min(r - 4f, 15f);
                    int n = Mathf.Max(5, Mathf.FloorToInt(2f * Mathf.PI * ring / 8.5f));
                    for (int i = 0; i < n; i++)
                    {
                        float a = 360f * i / n + 180f / n;
                        if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 25f) continue;          // 길 쪽은 비워 들어오는 입구
                        Tree(i % 3 == 0 ? "Tree_Cherry_B" : Pick(LeafyTrees), Polar(a, ring + R(-0.8f, 0.8f)));
                        Bush(Polar(a + 180f / n, ring - 1.5f));
                    }
                }
                if (r >= 18f)   // 아주 넓으면 안쪽 고리에 꽃밭 한 겹 더
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45f + 22.5f; if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 30f) continue;
                        var p = Polar(a, 10.5f); Put(FolPf + "FlowerBed", p, YawOf(p) + 90f, 1.1f);
                    }
            }

            // 가게 3개를 뒤쪽 호에 놓고 길(앞)을 보게, 앞마당에 식탁 또는 벤치
            public void Court(string[] stalls, bool food)
            {
                float back = Mathf.Min(sp.r * 0.45f, 6.5f);
                float gap = Mathf.Min(5.2f, sp.r * 0.5f);
                for (int i = 0; i < stalls.Length; i++)
                {
                    float off = (i - 1) * gap;
                    var p = -Fwd * (back + Mathf.Abs(off) * -0.15f) + Side * off;
                    Put(DecoPf + stalls[i], p, sp.yaw - off * 3f);   // 살짝 부채꼴로 가운데를 보게
                }
                if (food)
                {
                    Put(PropPf + "Prop_Dining_Cloth", Fwd * 1.5f - Side * 2.6f, R(0, 360));
                    Put(PropPf + "Prop_Dining_Plain", Fwd * 1.5f + Side * 2.6f, R(0, 360));
                    if (sp.r >= 12f)
                    {
                        Put(PropPf + "Prop_Dining_Cloth", Fwd * 5f, R(0, 360));
                        Put(PropPf + "Prop_Cart", Fwd * 3.5f + Side * 6.5f, sp.yaw - 90f);
                    }
                    Put(PropPf + "Prop_Bin", -Fwd * (back - 2.6f) + Side * (gap * 1.45f), sp.yaw);
                    Put(PropPf + "Prop_Bin", -Fwd * (back - 2.6f) - Side * (gap * 1.45f), sp.yaw);
                }
                else
                {
                    Put(PropPf + "Prop_Cart_Balloons", Fwd * 2.5f + Side * 3.5f, sp.yaw + 200f);
                    Bench(Fwd * 2.0f - Side * 3.5f, sp.yaw + 90f);
                    Bench(Fwd * 4.5f - Side * 1.0f, sp.yaw + 180f);
                    Put(PropPf + "Prop_Bin", Fwd * 2.0f - Side * 5.2f, sp.yaw);
                }
                Put(PropPf + "Prop_Lamp_On", Fwd * (sp.r * 0.55f) + Side * (sp.r * 0.5f), sp.yaw);
                Put(PropPf + "Prop_Lamp_On", Fwd * (sp.r * 0.55f) - Side * (sp.r * 0.5f), sp.yaw);
                // 예전 저폴리 화분 나무(Prop_Planter_Tree) 대신 LOD 있는 Foliage 나무를 심는다
                Tree(Pick(LeafyTrees), -Fwd * (back - 0.5f) + Side * (gap * 1.5f + 2.6f));
                Tree(Pick(LeafyTrees), -Fwd * (back - 0.5f) - Side * (gap * 1.5f + 2.6f));
                if (sp.r >= 12f)   // 가게 뒤를 나무로 막아줌
                    for (int i = -1; i <= 1; i++) Tree(Pick(LeafyTrees), -Fwd * (sp.r - 3f) + Side * (i * 7f));
            }

            public void Grove()
            {
                int n = sp.r >= 9f ? 4 : 3;
                float ring = sp.r * 0.45f;
                for (int i = 0; i < n; i++) Tree(Pick(LeafyTrees), Polar(180f + (i - (n - 1) * 0.5f) * (300f / n) + R(-12, 12), ring + R(-1, 1)));
                for (int i = 0; i < 3; i++) Bush(Polar(R(0, 360), R(1.5f, sp.r - 2.5f)));
                Rock("M", Polar(R(100, 260), sp.r * 0.6f));
                Rock("S", Polar(R(100, 260), sp.r * 0.3f));
                Bench(Fwd * (sp.r - 2.2f), sp.yaw);
            }

            public void Woods()
            {
                int n = sp.r >= 8f ? 5 : 3;
                for (int i = 0; i < n; i++) Tree(Pick(EdgeTrees), i == 0 ? Vector3.zero : Polar(360f * i / (n - 1) + R(-20, 20), sp.r * 0.55f + R(-0.8f, 0.8f)));
                Rock("L", Polar(R(0, 360), sp.r * 0.35f));
                Rock("M", Polar(R(0, 360), sp.r * 0.6f));
                for (int i = 0; i < 3; i++) Put(FolPf + "Grass_Dry", Polar(R(0, 360), R(1.5f, sp.r - 1.5f)), R(0, 360), R(0.9f, 1.3f));
                Bush(Polar(R(0, 360), sp.r * 0.7f));
            }

            public void RockRest()
            {
                Rock("L", -Fwd * 1.5f);
                Rock("L", -Fwd * 3.5f + Side * 4f);
                Rock("M", -Fwd * 0.5f - Side * 4f);
                Rock("S", Side * 2.5f);
                Rock("S", -Side * 2f + Fwd * 1.5f);
                Tree(Pick(EdgeTrees), -Fwd * (sp.r * 0.6f) - Side * 3f);
                Tree(Pick(EdgeTrees), -Fwd * (sp.r * 0.55f) + Side * (sp.r * 0.5f));
                for (int i = 0; i < 4; i++) Put(FolPf + "Grass_Dry", Polar(R(0, 360), R(2f, sp.r - 2f)), R(0, 360), R(0.9f, 1.3f));
                Bench(Fwd * (sp.r * 0.45f) - Side * 1.5f, sp.yaw);
                Bench(Fwd * (sp.r * 0.45f) + Side * 1.5f, sp.yaw);
            }

            public void Nook()
            {
                if (sp.pathDist <= 9f)
                {
                    Tree(rnd.NextDouble() < 0.4 ? "Tree_Cherry_A" : Pick(LeafyTrees), -Fwd * 1.2f);   // 예전 저폴리 화분 나무 대신
                    Bench(Fwd * 1.8f, sp.yaw);
                    Put(FolPf + "Pot_Plant", Fwd * 1.6f - Side * 1.9f, R(0, 360));
                }
                else
                {
                    Tree(Pick(LeafyTrees), Vector3.zero);
                    Bush(Polar(R(0, 360), 2.6f));
                    Bush(Polar(R(0, 360), 2.8f));
                }
            }
        }
    }
}
#endif
