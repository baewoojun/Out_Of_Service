// LotteWorldWaterFlow.cs  (v1, 11/20) - flow / foam baking for LotteWorld/Water (pure maths, no editor calls)
// Put next to LotteWorldWaterTool.cs in an Editor folder. Used by Tools > LotteWorld > Water.
//
// Input: water triangles in WORLD space. Output per vertex: (flow direction xyz, impact foam w) + splash spots.
//   * "strip" pieces (long ribbon of small quads, e.g. the flume channel): flow runs along the ribbon. Direction = the short
//     edge of each triangle (= one path step), made consistent across the ribbon, and signed so the steep drops run DOWNHILL.
//   * "sheet" pieces (boxes / fans, e.g. the 3-step waterfall and its pool): flat water flows toward the falling sheet whose
//     top is at its level and away from the sheet that lands on it; still water far from any sheet gets no flow.
//   * impact foam: water near the bottom of a drop / falling sheet (radius FoamRadius).
//   * splash spots: bottom edge of every drop / sheet (for particle emitters).

using System.Collections.Generic;
using UnityEngine;

namespace LotteWorld.Water
{
    public struct SplashSpot
    {
        public Vector3 Position;   // world
        public Vector3 Edge;       // world, horizontal unit vector along the landing edge
        public float Width;        // metres along Edge
        public bool Big;           // waterfall sheet (true) or ride drop (false)
    }

    public static class FlowBake
    {
        public const float Weld = 0.01f;            // 1 cm
        public const float FoamRadius = 4.0f;
        const float StripSlope = 0.15f;             // 1 - |n.y| above which a strip triangle counts as a slope
        const float SheetSlope = 0.6f;              // falling sheet

        static long Key(Vector3 p)
        {
            long x = (long)Mathf.Round(p.x / Weld) + 1000000, y = (long)Mathf.Round(p.y / Weld) + 1000000, z = (long)Mathf.Round(p.z / Weld) + 1000000;
            return (x * 2000003L + y) * 2000003L + z;
        }

        static int Find(int[] p, int a) { while (p[a] != a) { p[a] = p[p[a]]; a = p[a]; } return a; }

        public static Vector4[] Compute(Vector3[] V, IList<int> tris, bool reverse, out List<SplashSpot> spots, out string report)
        {
            var result = new Vector4[V.Length];
            spots = new List<SplashSpot>();
            int nT = tris.Count / 3;
            if (nT == 0) { report = "no triangles"; return result; }

            // ---- weld by position
            var keyToId = new Dictionary<long, int>();
            var wid = new int[V.Length];
            for (int i = 0; i < V.Length; i++) wid[i] = -1;
            var wpos = new List<Vector3>();
            foreach (int vi in tris)
            {
                if (wid[vi] >= 0) continue;
                long k = Key(V[vi]); int id;
                if (!keyToId.TryGetValue(k, out id)) { id = wpos.Count; keyToId[k] = id; wpos.Add(V[vi]); }
                wid[vi] = id;
            }
            int nW = wpos.Count;

            // ---- triangle data
            var tn = new Vector3[nT]; var tc = new Vector3[nT]; var area = new float[nT]; var slope = new float[nT];
            var minY = new float[nT]; var maxY = new float[nT]; var low = new Vector3[nT]; var axis = new Vector3[nT]; var ratio = new float[nT];
            var tw = new int[nT * 3];
            for (int t = 0; t < nT; t++)
            {
                Vector3 a = V[tris[3 * t]], b = V[tris[3 * t + 1]], c = V[tris[3 * t + 2]];
                tw[3 * t] = wid[tris[3 * t]]; tw[3 * t + 1] = wid[tris[3 * t + 1]]; tw[3 * t + 2] = wid[tris[3 * t + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a); float m = n.magnitude;
                area[t] = 0.5f * m; tn[t] = m > 1e-9f ? n / m : Vector3.up;
                tc[t] = (a + b + c) / 3f;
                slope[t] = 1f - Mathf.Abs(tn[t].y);
                minY[t] = Mathf.Min(a.y, Mathf.Min(b.y, c.y)); maxY[t] = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
                low[t] = a.y <= b.y && a.y <= c.y ? a : (b.y <= c.y ? b : c);
                Vector3 e0 = b - a, e1 = c - b, e2 = a - c;
                float l0 = e0.magnitude, l1 = e1.magnitude, l2 = e2.magnitude;
                Vector3 s = l0 <= l1 && l0 <= l2 ? e0 : (l1 <= l2 ? e1 : e2);
                float ls = Mathf.Min(l0, Mathf.Min(l1, l2)), ll = Mathf.Max(l0, Mathf.Max(l1, l2));
                ratio[t] = ll > 1e-6f ? ls / ll : 1f;
                s -= tn[t] * Vector3.Dot(s, tn[t]);
                axis[t] = s.sqrMagnitude > 1e-12f ? s.normalized : Vector3.zero;
            }

            // ---- adjacency over shared (welded) edges + connected pieces
            var edgeTri = new Dictionary<long, int>();
            var nbr = new List<int>[nT];
            var parent = new int[nT];
            for (int t = 0; t < nT; t++) { nbr[t] = new List<int>(3); parent[t] = t; }
            for (int t = 0; t < nT; t++)
                for (int e = 0; e < 3; e++)
                {
                    int u = tw[3 * t + e], w = tw[3 * t + (e + 1) % 3];
                    if (u == w) continue;
                    long k = u < w ? ((long)u << 32) | (uint)w : ((long)w << 32) | (uint)u;
                    int o;
                    if (edgeTri.TryGetValue(k, out o)) { nbr[t].Add(o); nbr[o].Add(t); int ra = Find(parent, t), rb = Find(parent, o); if (ra != rb) parent[ra] = rb; }
                    else edgeTri[k] = t;
                }
            var comps = new Dictionary<int, List<int>>();
            for (int t = 0; t < nT; t++) { int r = Find(parent, t); List<int> l; if (!comps.TryGetValue(r, out l)) { l = new List<int>(); comps[r] = l; } l.Add(t); }

            var compOf = new int[nT];
            var isStrip = new Dictionary<int, bool>();
            int nStrip = 0, nSheet = 0;
            foreach (KeyValuePair<int, List<int>> kv in comps)
            {
                List<int> ts = kv.Value;
                foreach (int t in ts) compOf[t] = kv.Key;
                Vector3 mn = Vector3.one * 1e9f, mx = -Vector3.one * 1e9f;
                var rs = new List<float>(ts.Count);
                foreach (int t in ts) { mn = Vector3.Min(mn, tc[t]); mx = Vector3.Max(mx, tc[t]); rs.Add(ratio[t]); }
                rs.Sort();
                float diag = new Vector2(mx.x - mn.x, mx.z - mn.z).magnitude;
                bool strip = ts.Count >= 40 && diag >= 15f && rs[rs.Count / 2] < 0.45f;
                isStrip[kv.Key] = strip;
                if (strip) nStrip++; else nSheet++;
            }

            // ---- strip pieces: orient the axes consistently (BFS), then sign them so slopes run downhill
            var seen = new bool[nT];
            foreach (KeyValuePair<int, List<int>> kv in comps)
            {
                if (!isStrip[kv.Key]) continue;
                var q = new Queue<int>();
                foreach (int s0 in kv.Value)
                {
                    if (seen[s0]) continue;
                    seen[s0] = true; q.Enqueue(s0);
                    while (q.Count > 0)
                    {
                        int t = q.Dequeue();
                        foreach (int o in nbr[t])
                        {
                            if (seen[o]) continue;
                            if (Vector3.Dot(axis[o], axis[t]) < 0f) axis[o] = -axis[o];
                            seen[o] = true; q.Enqueue(o);
                        }
                    }
                }
                float S = 0f;
                foreach (int t in kv.Value) if (slope[t] > StripSlope) S += area[t] * -axis[t].y * slope[t] * slope[t];
                bool flip = S < 0f;
                if (reverse) flip = !flip;
                if (flip) foreach (int t in kv.Value) axis[t] = -axis[t];
            }

            // ---- per original vertex: flat? (FBX splits vertices at hard edges, so box faces keep their own vertices)
            var origFlat = new bool[V.Length];
            for (int t = 0; t < nT; t++)
                if (slope[t] < 0.2f) { origFlat[tris[3 * t]] = true; origFlat[tris[3 * t + 1]] = true; origFlat[tris[3 * t + 2]] = true; }

            var vcomp = new int[V.Length];
            for (int t = 0; t < nT; t++) for (int e = 0; e < 3; e++) vcomp[tris[3 * t + e]] = compOf[t];
            System.Func<int, int> compOfVert = vi => vcomp[vi];

            // ---- strip flow per welded vertex
            var wflow = new Vector3[nW];
            for (int t = 0; t < nT; t++)
                if (isStrip[compOf[t]])
                    for (int e = 0; e < 3; e++) wflow[tw[3 * t + e]] += axis[t] * area[t];
            for (int w = 0; w < nW; w++) wflow[w] = wflow[w].sqrMagnitude > 1e-12f ? wflow[w].normalized : Vector3.zero;

            // ---- sheet pieces: one flow direction per piece, from its top surface toward / away from the falling sheets
            var sheets = new List<int>();
            for (int t = 0; t < nT; t++) if (!isStrip[compOf[t]] && slope[t] > SheetSlope && maxY[t] - minY[t] > 0.8f) sheets.Add(t);
            var compFlow = new Dictionary<int, Vector3>();
            foreach (KeyValuePair<int, List<int>> kv in comps)
            {
                if (isStrip[kv.Key]) continue;
                float top = float.MinValue;
                foreach (int t in kv.Value) if (slope[t] < 0.2f) top = Mathf.Max(top, tc[t].y);
                if (top == float.MinValue) continue;
                Vector3 cen = Vector3.zero; float wsum = 0f;
                Vector3 mn = Vector3.one * 1e9f, mx = -Vector3.one * 1e9f;
                foreach (int t in kv.Value)
                {
                    if (slope[t] >= 0.2f || tc[t].y < top - 0.15f) continue;
                    cen += tc[t] * area[t]; wsum += area[t];
                    for (int e = 0; e < 3; e++) { Vector3 p = V[tris[3 * t + e]]; mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); }
                }
                if (wsum <= 0f) continue;
                cen /= wsum;
                Vector3 acc = Vector3.zero;
                foreach (int s in sheets)
                {
                    if (compOf[s] == kv.Key) continue;
                    float dx = Mathf.Max(0f, Mathf.Max(mn.x - tc[s].x, tc[s].x - mx.x)), dz = Mathf.Max(0f, Mathf.Max(mn.z - tc[s].z, tc[s].z - mx.z));
                    float gap = Mathf.Sqrt(dx * dx + dz * dz);
                    if (gap > 3f) continue;
                    float sign = 0f;
                    if (Mathf.Abs(maxY[s] - top) < 0.7f) sign = 1f;          // sheet starts at my level: water goes over that edge
                    else if (Mathf.Abs(minY[s] - top) < 0.7f) sign = -1f;    // sheet lands on me: water spreads away from it
                    if (sign == 0f) continue;
                    Vector3 d = tc[s] - cen; d.y = 0f;
                    if (d.sqrMagnitude < 1e-8f) continue;
                    acc += d.normalized * (sign * area[s] / (gap * gap + 0.5f));
                }
                compFlow[kv.Key] = acc.sqrMagnitude > 1e-8f ? acc.normalized : Vector3.zero;
            }

            // ---- landing spots: bottoms of drops / sheets that end on flat water
            var impact = new List<int>();
            for (int t = 0; t < nT; t++)
            {
                bool strip = isStrip[compOf[t]];
                if (strip ? (slope[t] > StripSlope && -axis[t].y > 0.25f) : (slope[t] > SheetSlope && maxY[t] - minY[t] > 0.8f)) impact.Add(t);
            }
            var flats = new List<int>();
            for (int t = 0; t < nT; t++) if (slope[t] < 0.1f) flats.Add(t);
            var used = new bool[impact.Count];
            for (int i = 0; i < impact.Count; i++)
            {
                if (used[i]) continue;
                var pts = new List<Vector3>(); var who = new List<int>();
                Vector3 seed = low[impact[i]];
                for (int j = i; j < impact.Count; j++)
                {
                    if (used[j] || (low[impact[j]] - seed).magnitude > 4f) continue;
                    used[j] = true; pts.Add(low[impact[j]]); who.Add(impact[j]);
                }
                float y0 = float.MaxValue; foreach (Vector3 p in pts) y0 = Mathf.Min(y0, p.y);
                var bottom = new List<Vector3>(); Vector3 dh = Vector3.zero; bool big = false;
                for (int k = 0; k < pts.Count; k++)
                {
                    if (pts[k].y > y0 + 0.4f) continue;
                    bottom.Add(pts[k]);
                    Vector3 n = tn[who[k]]; dh += (new Vector3(0f, -1f, 0f) + n * n.y) * area[who[k]];
                    if (!isStrip[compOf[who[k]]]) big = true;
                }
                if (bottom.Count == 0) continue;
                Vector3 c = Vector3.zero; foreach (Vector3 p in bottom) c += p; c /= bottom.Count;
                // landing edge = main horizontal axis of the bottom points (PCA), else across the downhill direction
                float cxx = 0f, czz = 0f, cxz = 0f;
                foreach (Vector3 p in bottom) { float x = p.x - c.x, z = p.z - c.z; cxx += x * x; czz += z * z; cxz += x * z; }
                float ang = 0.5f * (float)System.Math.Atan2(2f * cxz, cxx - czz);
                Vector3 edge = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (Vector3 p in bottom) { float s = Vector3.Dot(p - c, edge); lo = Mathf.Min(lo, s); hi = Mathf.Max(hi, s); }
                float width = hi - lo;
                if (width < 0.5f) { dh.y = 0f; edge = dh.sqrMagnitude > 1e-8f ? Vector3.Cross(Vector3.up, dh).normalized : Vector3.right; width = 1.2f; }
                else c += edge * ((lo + hi) * 0.5f);
                // keep it only if flat water lies right there at the same height (drops a slope's middle)
                bool lands = false;
                foreach (int f in flats)
                {
                    Vector3 d = tc[f] - c; if (Mathf.Abs(d.y) > 0.6f) continue; d.y = 0f;
                    if (d.magnitude < 3f + width * 0.5f) { lands = true; break; }
                }
                if (!lands) continue;
                var sp = new SplashSpot { Position = c, Edge = edge, Width = Mathf.Max(width, 1f), Big = big };
                int dup = spots.FindIndex(o => (o.Position - c).magnitude < 3f);
                if (dup < 0) spots.Add(sp);
                else if (spots[dup].Position.y > c.y) spots[dup] = sp;          // keep the lower one (the real landing)
            }

            // ---- impact foam: around each landing spot, within a metre of its height
            var foam = new float[V.Length];
            var isStripV = new bool[V.Length];
            for (int t = 0; t < nT; t++) if (isStrip[compOf[t]]) for (int e = 0; e < 3; e++) isStripV[tris[3 * t + e]] = true;
            for (int i = 0; i < V.Length; i++)
            {
                if (wid[i] < 0 || (!isStripV[i] && !origFlat[i])) continue;
                float f = 0f;
                foreach (SplashSpot s in spots)
                {
                    Vector3 d = V[i] - s.Position;
                    if (Mathf.Abs(d.y) > 1f) continue;
                    d.y = 0f;
                    float along = Mathf.Clamp(Vector3.Dot(d, s.Edge), -s.Width * 0.5f, s.Width * 0.5f);
                    float dist = (d - s.Edge * along).magnitude;
                    f = Mathf.Max(f, 1f - dist / FoamRadius);
                }
                foam[i] = f > 0f ? Mathf.Clamp01(f * f * 1.4f) : 0f;
            }

            // ---- write back per original vertex
            int moving = 0, foamy = 0;
            for (int i = 0; i < V.Length; i++)
            {
                int w = wid[i];
                if (w < 0) continue;
                Vector3 fl;
                if (isStripV[i]) fl = wflow[w];
                else { Vector3 cf; fl = origFlat[i] && compFlow.TryGetValue(compOfVert(i), out cf) ? cf : Vector3.zero; }
                if (reverse && !isStripV[i]) fl = -fl;
                result[i] = new Vector4(fl.x, fl.y, fl.z, foam[i]);
                if (fl.sqrMagnitude > 0.5f) moving++;
                if (foam[i] > 0.05f) foamy++;
            }
            report = string.Format("{0} tris, {1} strip + {2} sheet pieces, {3} of {4} vertices flowing, {5} foamy, {6} splash spots",
                                   nT, nStrip, nSheet, moving, V.Length, foamy, spots.Count);
            return result;
        }
    }
}
