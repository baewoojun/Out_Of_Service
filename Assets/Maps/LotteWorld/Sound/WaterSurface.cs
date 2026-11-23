// WaterSurface.cs - LotteWorldSfx.cs 가 런타임에 자동으로 붙임. 설명은 LotteWorldSfx.cs 맨 위 참고.
using System.Collections;
using System.Collections.Generic;
using LotteWorld.Rides;
using UnityEngine;

namespace LotteWorld.Sound
{
    /// <summary>물 표면 높이를 알려줌. 물 재질 슬롯의 위쪽을 향한 삼각형만 써서 (x,z) 위치의 수면 높이를 구함.</summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class WaterSurface : MonoBehaviour
    {
        public static readonly List<WaterSurface> All = new List<WaterSurface>();
        const float Cell = 2f;
        readonly List<Vector3> tris = new List<Vector3>();              // 3개씩 월드 좌표
        readonly Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
        Bounds bounds; bool flatOnly;

        void Awake()
        {
            var r = GetComponent<MeshRenderer>();
            var mf = GetComponent<MeshFilter>();
            bounds = r.bounds;
            Mesh mesh = mf ? mf.sharedMesh : null;
            if (mesh == null || !mesh.isReadable) { flatOnly = true; return; }   // 못 읽으면 상자 윗면을 수면으로 침
            var verts = mesh.vertices;
            var mats = r.sharedMaterials;
            for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
            {
                if (!mats[s] || !mats[s].shader || mats[s].shader.name != "LotteWorld/Water") continue;
                int[] idx = mesh.GetTriangles(s);
                for (int i = 0; i + 2 < idx.Length; i += 3)
                {
                    Vector3 a = transform.TransformPoint(verts[idx[i]]), b = transform.TransformPoint(verts[idx[i + 1]]), c = transform.TransformPoint(verts[idx[i + 2]]);
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    if (n.sqrMagnitude < 1e-10f || Mathf.Abs(n.normalized.y) < 0.35f) continue;   // 폭포처럼 선 면은 뺌
                    int t = tris.Count / 3;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)) / Cell), x1 = Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) / Cell);
                    int z0 = Mathf.FloorToInt(Mathf.Min(a.z, Mathf.Min(b.z, c.z)) / Cell), z1 = Mathf.FloorToInt(Mathf.Max(a.z, Mathf.Max(b.z, c.z)) / Cell);
                    for (int x = x0; x <= x1; x++)
                        for (int z = z0; z <= z1; z++)
                        {
                            long key = Key(x, z);
                            if (!grid.TryGetValue(key, out var list)) grid[key] = list = new List<int>();
                            list.Add(t);
                        }
                }
            }
            if (tris.Count == 0) flatOnly = true;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        static long Key(int x, int z) { return ((long)x << 32) ^ (uint)z; }

        /// <summary>(x,z) 바로 위/아래에서 belowY 보다 높고 aboveY 보다 낮은 수면 중 가장 높은 것.</summary>
        public bool HeightAt(Vector3 p, float searchDown, float searchUp, out float height)
        {
            height = float.MinValue;
            if (p.x < bounds.min.x || p.x > bounds.max.x || p.z < bounds.min.z || p.z > bounds.max.z) return false;
            if (flatOnly)
            {
                height = bounds.max.y;
                return height > p.y - searchDown && height < p.y + searchUp;
            }
            if (!grid.TryGetValue(Key(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell)), out var list)) return false;
            bool found = false;
            foreach (int t in list)
            {
                Vector3 a = tris[t * 3], b = tris[t * 3 + 1], c = tris[t * 3 + 2];
                float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(d) < 1e-8f) continue;
                float u = ((b.z - c.z) * (p.x - c.x) + (c.x - b.x) * (p.z - c.z)) / d;
                float v = ((c.z - a.z) * (p.x - c.x) + (a.x - c.x) * (p.z - c.z)) / d;
                if (u < -0.001f || v < -0.001f || u + v > 1.002f) continue;
                float y = u * a.y + v * b.y + (1f - u - v) * c.y;
                if (y > p.y - searchDown && y < p.y + searchUp && y > height) { height = y; found = true; }
            }
            return found;
        }

        public static bool Query(Vector3 p, float searchDown, float searchUp, out float height)
        {
            height = float.MinValue; bool any = false;
            foreach (var w in All)
                if (w.HeightAt(p, searchDown, searchUp, out float h) && h > height) { height = h; any = true; }
            return any;
        }
    }
}
