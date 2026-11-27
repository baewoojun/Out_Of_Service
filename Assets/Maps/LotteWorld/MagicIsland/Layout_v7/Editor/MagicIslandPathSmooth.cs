// MagicIslandPathSmooth.cs — 매직아일랜드 v7 길(road) 매끄럽게 잇기
// 위치: Assets/Maps/LotteWorld/MagicIsland/Layout_v7/Editor/
// 메뉴: Tools > LotteWorld > Magic Island > Layout v7 > 3b 길 매끄럽게 다시 만들기  ("3 바닥 만들기" 끝에도 자동으로 돌아감)
//
// json 의 길은 곧은 띠(사각형)를 이어 붙인 것이라 꺾이는 곳이 각지고, 띠끼리 만나는 곳에 V 자 틈이 나고,
// 띠가 겹치는 곳은 두 장이 같은 높이로 겹쳐 깜빡였음. 그래서:
//   1) 길 Path_00~03 을 0.2 m 칸에 칠해 하나로 합침 (겹침 없어짐)
//   2) 닫기(넓혔다 좁히기, 반지름 CloseR) → 안쪽 모서리·V 틈을 둥글게 메움
//      열기(좁혔다 넓히기, 반지름 OpenR)  → 바깥 모서리를 둥글게 깎음
//      광장도 같이 넣고 닫아서 길이 광장에 닿는 곳도 부드럽게 퍼지게 함
//   3) 거리장으로 가장자리를 살짝 흐려 계단 무늬 없앰, 광장 안쪽은 잘라냄 (광장 무늬 위에 길이 덮이지 않게)
//   4) 등고선(마칭 스퀘어)으로 한 장짜리 메시 Path_Smooth 를 만듦. 안쪽은 큰 사각형으로 묶어 삼각형 수를 줄임
// 옛 Path_00~03 은 지우지 않고 꺼 둠 (콜라이더 포함). 다시 누르면 Path_Smooth 만 새로 만듦. Undo 됨. 씬 저장은 직접.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LotteWorld.MagicIsland
{
    public static partial class MagicIslandLayoutV7
    {
        const float PathCell = 0.2f;      // 칸 크기 (m)
        const float CloseR = 1.6f;        // 안쪽 모서리·V 틈 메우는 반지름 (이보다 2배 좁은 틈은 메워짐)
        const float OpenR = 0.9f;         // 바깥 모서리 깎는 반지름
        const float PlazaOverlap = 0.12f; // 광장 가장자리 안으로 길이 들어가는 폭 (틈 안 보이게)
        const string SmoothName = "Path_Smooth";

        [MenuItem(Menu + "3b 길 매끄럽게 다시 만들기", false, 13)]
        public static void SmoothPaths()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[MI v7] 플레이 중에는 길을 다시 만들 수 없음 (끝나면 사라짐). 플레이를 멈추고 다시 눌러줘"); return; }
            var d = Load(); if (d == null) return;
            var roads = d.meshes.Where(m => m.layer == "road").ToList();
            var plazas = d.meshes.Where(m => m.layer == "plaza").ToList();
            if (roads.Count == 0) { Debug.LogWarning("[MI v7] json 에 길(road)이 없음"); return; }

            var grp = Group("03_Floors", false);
            try
            {
                Progress("길 매끄럽게", "칸에 칠하기", 0.05f);
                float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
                foreach (var m in roads.Concat(plazas))
                    for (int i = 0; i < m.v.Length; i += 2)
                    {
                        minX = Mathf.Min(minX, m.v[i]); maxX = Mathf.Max(maxX, m.v[i]);
                        minZ = Mathf.Min(minZ, m.v[i + 1]); maxZ = Mathf.Max(maxZ, m.v[i + 1]);
                    }
                float pad = CloseR + OpenR + 2f;
                var g = new Grid(minX - pad, minZ - pad, maxX + pad, maxZ + pad, PathCell);

                bool[] road = new bool[g.N], plaza = new bool[g.N];
                foreach (var m in roads) g.Fill(m, road);
                foreach (var m in plazas) g.Fill(m, plaza);
                bool[] both = new bool[g.N];
                for (int i = 0; i < g.N; i++) both[i] = road[i] || plaza[i];

                Progress("길 매끄럽게", "닫기 (안쪽 모서리 메우기)", 0.2f);
                bool[] shape = g.Threshold(g.Sdf(both), CloseR);       // 넓히기
                shape = g.Threshold(g.Sdf(shape), -CloseR);             // 좁히기
                Progress("길 매끄럽게", "열기 (바깥 모서리 깎기)", 0.4f);
                shape = g.Threshold(g.Sdf(shape), -OpenR);              // 좁히기
                shape = g.Threshold(g.Sdf(shape), OpenR);               // 넓히기

                Progress("길 매끄럽게", "가장자리 다듬기", 0.6f);
                float[] f = g.Sdf(shape);
                g.Blur(f); g.Blur(f);
                float[] p = g.Sdf(plaza);
                g.Blur(p);
                for (int i = 0; i < g.N; i++) f[i] = Mathf.Max(f[i], -(p[i] + PlazaOverlap));   // 광장 안쪽 잘라냄
                // 원래 길도 광장도 아니던 먼 곳에 생긴 살은 버림 (광장끼리 닫혀 생긴 덩어리 막기)
                float[] r = g.Sdf(road);
                for (int i = 0; i < g.N; i++) if (r[i] > CloseR * 2f) f[i] = Mathf.Max(f[i], 0.01f);

                Progress("길 매끄럽게", "메시 만들기", 0.8f);
                float uvScale = roads[0].uv > 0 ? roads[0].uv : 4f;
                var mesh = g.Contour(f, uvScale);
                mesh.name = "MIv7_" + SmoothName;

                string path = MeshDir + "/" + mesh.name + ".asset";
                System.IO.Directory.CreateDirectory(MeshDir);
                var oldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (oldMesh != null) { EditorUtility.CopySerialized(mesh, oldMesh); mesh = oldMesh; } else AssetDatabase.CreateAsset(mesh, path);

                var old = grp.Find(SmoothName);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                var go = new GameObject(SmoothName);
                Undo.RegisterCreatedObjectUndo(go, "MI v7 smooth path");
                go.transform.SetParent(grp, false);
                go.transform.localPosition = new Vector3(0, roads.Max(m => m.y), 0);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = Mat("road");
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic);

                int off = 0;
                foreach (var m in roads)
                {
                    var t = grp.Find(m.name);
                    if (t != null && t.gameObject.activeSelf) { Undo.RecordObject(t.gameObject, "MI v7 old path"); t.gameObject.SetActive(false); off++; }
                }
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(go.scene);
                Done(string.Format("길 매끄럽게: {0} (꼭짓점 {1}, 삼각형 {2}), 옛 길 {3}개 끔", SmoothName, mesh.vertexCount, mesh.triangles.Length / 3, off));
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        // ---- 평면 칸 (칸 가운데 = 마디) ----
        class Grid
        {
            public readonly int W, H, N; readonly float x0, z0, h;
            public Grid(float minX, float minZ, float maxX, float maxZ, float cell)
            {
                h = cell; x0 = minX; z0 = minZ;
                W = Mathf.CeilToInt((maxX - minX) / cell) + 1; H = Mathf.CeilToInt((maxZ - minZ) / cell) + 1; N = W * H;
            }
            float X(int i) { return x0 + i * h; }
            float Z(int j) { return z0 + j * h; }

            public void Fill(MeshData m, bool[] mask)
            {
                for (int k = 0; k + 2 < m.t.Length; k += 3)
                {
                    Vector2 a = V(m, m.t[k]), b = V(m, m.t[k + 1]), c = V(m, m.t[k + 2]);
                    float area = Cross(b - a, c - a); if (Mathf.Abs(area) < 1e-8f) continue;
                    int i0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - x0) / h));
                    int i1 = Mathf.Min(W - 1, Mathf.CeilToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - x0) / h));
                    int j0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, Mathf.Min(b.y, c.y)) - z0) / h));
                    int j1 = Mathf.Min(H - 1, Mathf.CeilToInt((Mathf.Max(a.y, Mathf.Max(b.y, c.y)) - z0) / h));
                    for (int j = j0; j <= j1; j++)
                        for (int i = i0; i <= i1; i++)
                        {
                            var q = new Vector2(X(i), Z(j));
                            float w0 = Cross(b - a, q - a) / area, w1 = Cross(c - b, q - b) / area, w2 = Cross(a - c, q - c) / area;
                            if (w0 >= -1e-4f && w1 >= -1e-4f && w2 >= -1e-4f) mask[j * W + i] = true;
                        }
                }
            }
            static Vector2 V(MeshData m, int i) { return new Vector2(m.v[2 * i], m.v[2 * i + 1]); }
            static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }

            public bool[] Threshold(float[] sd, float r)
            {
                var o = new bool[N]; for (int i = 0; i < N; i++) o[i] = sd[i] <= r; return o;
            }

            // 부호 거리 (m): 안 = 음수, 밖 = 양수
            public float[] Sdf(bool[] mask)
            {
                float[] dOut = Edt(mask, true), dIn = Edt(mask, false);
                var sd = new float[N];
                for (int i = 0; i < N; i++)
                    sd[i] = mask[i] ? -(Mathf.Sqrt(dIn[i]) - 0.5f) * h : (Mathf.Sqrt(dOut[i]) - 0.5f) * h;
                return sd;
            }
            // Felzenszwalb 정확한 거리 변환 (칸 단위 제곱 거리). target 인 칸까지의 거리
            float[] Edt(bool[] mask, bool target)
            {
                const float INF = 1e20f;
                var g = new float[N];
                for (int i = 0; i < N; i++) g[i] = mask[i] == target ? 0f : INF;
                int n = Mathf.Max(W, H);
                var f = new float[n]; var d = new float[n]; var v = new int[n]; var z = new float[n + 1];
                for (int x = 0; x < W; x++)
                {
                    for (int y = 0; y < H; y++) f[y] = g[y * W + x];
                    Dt1(f, H, d, v, z);
                    for (int y = 0; y < H; y++) g[y * W + x] = d[y];
                }
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++) f[x] = g[y * W + x];
                    Dt1(f, W, d, v, z);
                    for (int x = 0; x < W; x++) g[y * W + x] = d[x];
                }
                return g;
            }
            static float Sep(float[] f, int p, int q) { return ((f[q] + q * q) - (f[p] + p * p)) / (2f * q - 2f * p); }
            static void Dt1(float[] f, int n, float[] d, int[] v, float[] z)
            {
                int k = 0; v[0] = 0; z[0] = float.NegativeInfinity; z[1] = float.PositiveInfinity;
                for (int q = 1; q < n; q++)
                {
                    float s = Sep(f, v[k], q);
                    while (k > 0 && s <= z[k]) { k--; s = Sep(f, v[k], q); }
                    k++; v[k] = q; z[k] = s; z[k + 1] = float.PositiveInfinity;
                }
                k = 0;
                for (int q = 0; q < n; q++)
                {
                    while (z[k + 1] < q) k++;
                    int p = v[k]; d[q] = (q - p) * (q - p) + f[p];
                }
            }

            // 1-4-6-4-1 가로·세로 흐리기
            public void Blur(float[] a)
            {
                var t = new float[N];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                        t[y * W + x] = (a[y * W + C(x - 2, W)] + 4 * a[y * W + C(x - 1, W)] + 6 * a[y * W + x] + 4 * a[y * W + C(x + 1, W)] + a[y * W + C(x + 2, W)]) / 16f;
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                        a[y * W + x] = (t[C(y - 2, H) * W + x] + 4 * t[C(y - 1, H) * W + x] + 6 * t[y * W + x] + 4 * t[C(y + 1, H) * W + x] + t[C(y + 2, H) * W + x]) / 16f;
            }
            static int C(int i, int n) { return i < 0 ? 0 : i >= n ? n - 1 : i; }

            // f < 0 인 곳을 채운 평면 메시. 꽉 찬 칸은 큰 사각형으로 묶고, 가장자리 칸은 마칭 스퀘어
            public Mesh Contour(float[] f, float uvScale)
            {
                var verts = new List<Vector3>(); var tris = new List<int>();
                var nodeIdx = new Dictionary<int, int>(); var edgeIdx = new Dictionary<long, int>();
                Func<int, int, int> Node = (i, j) =>
                {
                    int key = j * W + i, id;
                    if (!nodeIdx.TryGetValue(key, out id)) { id = verts.Count; verts.Add(new Vector3(X(i), 0, Z(j))); nodeIdx[key] = id; }
                    return id;
                };
                // 마디 a(i,j) → b 사이 0 지점. dir 0 = +x, 1 = +z
                Func<int, int, int, int> Edge = (i, j, dir) =>
                {
                    long key = ((long)(j * W + i) << 1) | (long)dir; int id;
                    if (!edgeIdx.TryGetValue(key, out id))
                    {
                        int i2 = dir == 0 ? i + 1 : i, j2 = dir == 0 ? j : j + 1;
                        float fa = f[j * W + i], fb = f[j2 * W + i2];
                        float t = Mathf.Clamp(fa / (fa - fb), 0.02f, 0.98f);
                        id = verts.Count; verts.Add(new Vector3(Mathf.Lerp(X(i), X(i2), t), 0, Mathf.Lerp(Z(j), Z(j2), t)));
                        edgeIdx[key] = id;
                    }
                    return id;
                };
                Action<List<int>> Fan = poly =>
                {
                    for (int k = 1; k + 1 < poly.Count; k++) AddTri(verts, tris, poly[0], poly[k], poly[k + 1]);
                };

                int cw = W - 1, ch = H - 1;
                var full = new bool[cw * ch];
                for (int j = 0; j < ch; j++)
                    for (int i = 0; i < cw; i++)
                    {
                        bool in0 = f[j * W + i] < 0, in1 = f[j * W + i + 1] < 0, in2 = f[(j + 1) * W + i + 1] < 0, in3 = f[(j + 1) * W + i] < 0;
                        if (in0 && in1 && in2 && in3) { full[j * cw + i] = true; continue; }
                        if (!in0 && !in1 && !in2 && !in3) continue;
                        // 마디 0(i,j) 1(i+1,j) 2(i+1,j+1) 3(i,j+1), 변 0-1 = (i,j,+x) 1-2 = (i+1,j,+z) 2-3 = (i,j+1,+x) 3-0 = (i,j,+z)
                        bool[] ins = { in0, in1, in2, in3 };
                        int[] ni = { i, i + 1, i + 1, i }, nj = { j, j, j + 1, j + 1 };
                        Func<int, int> EdgeAfter = c => c == 0 ? Edge(i, j, 0) : c == 1 ? Edge(i + 1, j, 1) : c == 2 ? Edge(i, j + 1, 0) : Edge(i, j, 1);
                        bool saddle = (in0 && in2 && !in1 && !in3) || (in1 && in3 && !in0 && !in2);
                        float center = (f[j * W + i] + f[j * W + i + 1] + f[(j + 1) * W + i + 1] + f[(j + 1) * W + i]) * 0.25f;
                        if (saddle && center >= 0)
                        {
                            for (int c = 0; c < 4; c++)
                                if (ins[c]) AddTri(verts, tris, EdgeAfter((c + 3) % 4), Node(ni[c], nj[c]), EdgeAfter(c));
                            continue;
                        }
                        var poly = new List<int>(6);
                        for (int c = 0; c < 4; c++)
                        {
                            if (ins[c]) poly.Add(Node(ni[c], nj[c]));
                            if (ins[c] != ins[(c + 1) % 4]) poly.Add(EdgeAfter(c));
                        }
                        Fan(poly);
                    }
                // 꽉 찬 칸 → 큰 사각형 (가로로 늘리고 같은 폭이면 세로로 늘림)
                var used = new bool[cw * ch];
                for (int j = 0; j < ch; j++)
                    for (int i = 0; i < cw; i++)
                    {
                        if (!full[j * cw + i] || used[j * cw + i]) continue;
                        int i2 = i; while (i2 + 1 < cw && full[j * cw + i2 + 1] && !used[j * cw + i2 + 1]) i2++;
                        int j2 = j;
                        while (j2 + 1 < ch)
                        {
                            bool ok = true;
                            for (int x = i; x <= i2 && ok; x++) ok = full[(j2 + 1) * cw + x] && !used[(j2 + 1) * cw + x];
                            if (!ok) break; j2++;
                        }
                        for (int y = j; y <= j2; y++) for (int x = i; x <= i2; x++) used[y * cw + x] = true;
                        int a = Node(i, j), b = Node(i2 + 1, j), c2 = Node(i2 + 1, j2 + 1), d = Node(i, j2 + 1);
                        AddTri(verts, tris, a, b, c2); AddTri(verts, tris, a, c2, d);
                    }

                var uv = new Vector2[verts.Count]; var nor = new Vector3[verts.Count];
                for (int k = 0; k < verts.Count; k++) { uv[k] = new Vector2(verts[k].x / uvScale, verts[k].z / uvScale); nor[k] = Vector3.up; }
                var mesh = new Mesh();
                mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(verts); mesh.uv = uv; mesh.normals = nor; mesh.SetTriangles(tris, 0);
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                return mesh;
            }
            // 위에서 볼 때 앞면이 되게 (Unity: Cross(b-a, c-a).y > 0)
            static void AddTri(List<Vector3> v, List<int> t, int a, int b, int c)
            {
                if (a == b || b == c || a == c) return;
                float y = Vector3.Cross(v[b] - v[a], v[c] - v[a]).y;
                if (Mathf.Abs(y) < 1e-7f) return;
                if (y > 0) { t.Add(a); t.Add(b); t.Add(c); } else { t.Add(a); t.Add(c); t.Add(b); }
            }
        }
    }
}
#endif
