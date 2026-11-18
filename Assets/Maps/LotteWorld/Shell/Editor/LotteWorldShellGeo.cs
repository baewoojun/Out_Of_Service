// LotteWorldShellGeo.cs - 롯데월드 실내 셸 형상 (Unity API 없이 순수 계산: 에디터 창과 시험 하네스가 같이 씀)
// 좌표 = Unity 월드 (X 동, Z 북, Y 위). 미터.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotteWorld.Shell
{
    public class ShellSettings
    {
        public float H = 58f;                                   // 평천장 높이
        public float DomeK = 3f;                                // 돔 넓이 배수 (면적)
        public Vector2 LoopC = new Vector2(-5f, 9f);            // 퍼레이드 루프 중심 / 반폭
        public float LoopHX = 44.85f, LoopHZ = 29.9f;
        public bool Ice = false;                                // 얼음판도 만들지 (팀 씬에 이미 있으면 끔)
        public float DomeHX { get { return 72f * Mathf.Sqrt(DomeK); } }
        public float DomeHZ { get { return 54f * Mathf.Sqrt(DomeK); } }
        public float DomeRise { get { return 22f * Mathf.Pow(DomeK, 0.3f); } }
        public static readonly Vector2[] Wall =
        {
            new Vector2(-128.92f, 91.92f), new Vector2(-86.3f, 91.92f), new Vector2(-86.3f, 126.4f), new Vector2(9.2f, 126.4f), new Vector2(9.2f, 140.4f),
            new Vector2(109.4f, 142.6f), new Vector2(149.45f, 117.2f), new Vector2(157.58f, 80.65f), new Vector2(151.78f, 23.03f), new Vector2(146.1f, -48.3f),
            new Vector2(117.25f, -66.54f), new Vector2(65.55f, -91.9f), new Vector2(52.85f, -115.0f), new Vector2(-11.53f, -117.14f), new Vector2(-59.87f, -124.17f),
            new Vector2(-124.35f, -117.16f), new Vector2(-187.5f, -82.8f), new Vector2(-186.3f, -34.5f), new Vector2(-180.65f, 59.75f), new Vector2(-146.2f, 85.0f),
        };
        public const int GateWall = 2; public const float GateX0 = -64f, GateX1 = -46f;   // Wall_02 정문 구멍
        public static readonly Vector2 ColumnPos = new Vector2(-36.18f, -84.53f);          // 돔(3배) 남쪽 끝 테두리 보 아래
        public static readonly float[] Gal = { 7f, 14f, 21f, 28f };
        public const float Band0 = 35f;
    }

    /// <summary>mesh under construction for one material: planar UVs in metres / tile</summary>
    public class MB
    {
        public readonly List<Vector3> V = new List<Vector3>(); public readonly List<Vector3> N = new List<Vector3>();
        public readonly List<Vector2> UV = new List<Vector2>(); public readonly List<int> T = new List<int>();
        public float Tile = 4f;
        public MB(float tile) { Tile = tile; }
        Vector2 Proj(Vector3 p, Vector3 n)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x, p.z) / Tile;
            if (ax >= az) return new Vector2(p.z, p.y) / Tile;
            return new Vector2(p.x, p.y) / Tile;
        }
        /// front face toward outDir (Unity: clockwise seen from the front; normal = cross(b-a, c-a))
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outDir)
        {
            Vector3 n = Vector3.Cross(b - a, c - a) + Vector3.Cross(c - a, d - a);
            if (n.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(n, outDir) < 0f) { Vector3 t = b; b = d; d = t; n = -n; }
            n = n.normalized; int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c); V.Add(d);
            for (int k = 0; k < 4; k++) N.Add(n);
            UV.Add(Proj(a, n)); UV.Add(Proj(b, n)); UV.Add(Proj(c, n)); UV.Add(Proj(d, n));
            T.Add(i); T.Add(i + 1); T.Add(i + 2); T.Add(i); T.Add(i + 2); T.Add(i + 3);
        }
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outDir)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(n, outDir) < 0f) { Vector3 t = b; b = c; c = t; n = -n; }
            n = n.normalized; int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c); for (int k = 0; k < 3; k++) N.Add(n);
            UV.Add(Proj(a, n)); UV.Add(Proj(b, n)); UV.Add(Proj(c, n));
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
        }
        public void BoxO(Vector3 c, Vector3 ax, Vector3 ay, Vector3 az, Vector3 s)
        {
            Vector3 X = ax * (s.x * 0.5f), Y = ay * (s.y * 0.5f), Z = az * (s.z * 0.5f);
            Vector3 p000 = c - X - Y - Z, p100 = c + X - Y - Z, p110 = c + X + Y - Z, p010 = c - X + Y - Z;
            Vector3 p001 = c - X - Y + Z, p101 = c + X - Y + Z, p111 = c + X + Y + Z, p011 = c - X + Y + Z;
            Quad(p100, p110, p111, p101, ax); Quad(p000, p001, p011, p010, -ax);
            Quad(p010, p011, p111, p110, ay); Quad(p000, p100, p101, p001, -ay);
            Quad(p001, p101, p111, p011, az); Quad(p000, p010, p110, p100, -az);
        }
        public void Box(Vector3 c, Vector3 s) { BoxO(c, Vector3.right, Vector3.up, Vector3.forward, s); }
        /// box in a wall frame: origin o, u = along wall (xz), n = inward (xz); local (x along, y up, z inward)
        public void BoxW(Vector3 o, Vector3 u, Vector3 n, float x, float y, float z, float sx, float sy, float sz)
        { BoxO(o + u * x + Vector3.up * y + n * z, u, Vector3.up, n, new Vector3(sx, sy, sz)); }
        public void Member(Vector3 p, Vector3 q, float th)
        {
            Vector3 d = q - p; float L = d.magnitude; if (L < 1e-4f) return;
            Vector3 az = d / L, ax = Vector3.Cross(Vector3.up, az); if (ax.sqrMagnitude < 1e-6f) ax = Vector3.right; ax = ax.normalized;
            BoxO((p + q) * 0.5f, ax, Vector3.Cross(az, ax), az, new Vector3(th, th, L));
        }
        /// solid of revolution about +Y at b: profile (radius, height)
        public void Lathe(Vector3 b, Vector2[] prof, int seg)
        {
            for (int j = 0; j < prof.Length - 1; j++)
                for (int i = 0; i < seg; i++)
                {
                    float a0 = 2f * Mathf.PI * i / seg, a1 = 2f * Mathf.PI * (i + 1) / seg, am = (a0 + a1) * 0.5f;
                    Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)), dm = new Vector3(Mathf.Cos(am), 0, Mathf.Sin(am));
                    Vector3 p00 = b + d0 * prof[j].x + Vector3.up * prof[j].y, p01 = b + d1 * prof[j].x + Vector3.up * prof[j].y;
                    Vector3 p10 = b + d0 * prof[j + 1].x + Vector3.up * prof[j + 1].y, p11 = b + d1 * prof[j + 1].x + Vector3.up * prof[j + 1].y;
                    float dr = prof[j + 1].x - prof[j].x, dy = prof[j + 1].y - prof[j].y;
                    Quad(p00, p01, p11, p10, dm * dy - Vector3.up * dr + (Mathf.Abs(dy) + Mathf.Abs(dr) < 1e-5f ? Vector3.up : Vector3.zero));
                }
        }
        public void Cyl(Vector3 b, float r, float h, int seg, float r2 = -1f)
        {
            if (r2 < 0f) r2 = r;
            Lathe(b, new[] { new Vector2(0.0001f, 0f), new Vector2(r, 0f), new Vector2(r2, h), new Vector2(0.0001f, h) }, seg);
        }
        public void Sphere(Vector3 c, float r, float sy, int seg)
        {
            var prof = new Vector2[seg / 2 + 1];
            for (int i = 0; i <= seg / 2; i++) { float a = -Mathf.PI / 2 + Mathf.PI * i / (seg / 2); prof[i] = new Vector2(Mathf.Max(0.0001f, r * Mathf.Cos(a)), r * sy * Mathf.Sin(a)); }
            Lathe(c, prof, seg);
        }
    }

    public static class ShellGeo
    {
        // ---------------------------------------------------------------- 2D helpers (x, z)
        public static void Stadium(Vector2 c, float hx, float hz, float off, int n, List<Vector2> pts, List<Vector2> nrm)
        {
            pts.Clear(); nrm.Clear();
            float r = hz + off, a = Mathf.Max(0f, hx - hz), per = 4f * a + 2f * Mathf.PI * r;
            for (int i = 0; i < n; i++)
            {
                float s = per * i / n; Vector2 p, nr;
                if (s < 2f * a) { p = new Vector2(-a + s, -r); nr = new Vector2(0, -1); }
                else if (s < 2f * a + Mathf.PI * r) { float t = (s - 2f * a) / r - Mathf.PI / 2f; nr = new Vector2(Mathf.Cos(t), Mathf.Sin(t)); p = new Vector2(a, 0) + nr * r; }
                else if (s < 4f * a + Mathf.PI * r) { p = new Vector2(a - (s - 2f * a - Mathf.PI * r), r); nr = new Vector2(0, 1); }
                else { float t = (s - 4f * a - Mathf.PI * r) / r + Mathf.PI / 2f; nr = new Vector2(Mathf.Cos(t), Mathf.Sin(t)); p = new Vector2(-a, 0) + nr * r; }
                pts.Add(c + p); nrm.Add(nr);
            }
        }
        public static Vector2 AxisPt(Vector2 p, Vector2 c, float hx, float hz)
        { float a = Mathf.Max(0f, hx - hz); return new Vector2(Mathf.Clamp(p.x - c.x, -a, a) + c.x, c.y); }
        public static float DistStadium(Vector2 p, Vector2 c, float hx, float hz) { return (p - AxisPt(p, c, hx, hz)).magnitude - hz; }
        public static bool Inside(Vector2 p, Vector2[] poly)
        {
            bool ins = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) ins = !ins;
            return ins;
        }
        static float Area(Vector2[] poly) { float s = 0; for (int i = 0; i < poly.Length; i++) { Vector2 a = poly[i], b = poly[(i + 1) % poly.Length]; s += a.x * b.y - b.x * a.y; } return s * 0.5f; }
        /// ray from c in direction d hits polygon: distance (nearest positive)
        static float RayPoly(Vector2 c, Vector2 d, Vector2[] poly)
        {
            float best = 1e9f;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length], e = b - a;
                float den = d.x * e.y - d.y * e.x; if (Mathf.Abs(den) < 1e-9f) continue;
                Vector2 w = a - c; float t = (w.x * e.y - w.y * e.x) / den, u = (w.x * d.y - w.y * d.x) / den;
                if (t > 1e-4f && u >= -1e-5f && u <= 1f + 1e-5f && t < best) best = t;
            }
            return best;
        }
        /// ray from c hits stadium boundary (c inside stadium)
        static float RayStadium(Vector2 c0, Vector2 d, Vector2 c, float hx, float hz)
        {
            float lo = 0f, hi = 1000f;
            for (int k = 0; k < 40; k++) { float m = (lo + hi) * 0.5f; if (DistStadium(c0 + d * m, c, hx, hz) < 0f) lo = m; else hi = m; }
            return lo;
        }
        /// flat ring between a stadium hole and the wall polygon, built as a polar fan from the stadium centre
        /// (angles include every polygon corner so walls are exact). up = face up (floor) or down (ceiling)
        public static void Ring(MB mb, Vector2 c, float hx, float hz, Vector2[] poly, float y, bool faceUp, int n = 720)
        {
            var ang = new List<float>();
            for (int i = 0; i < n; i++) ang.Add(2f * Mathf.PI * i / n);
            foreach (Vector2 p in poly) { float a = Mathf.Atan2(p.y - c.y, p.x - c.x); if (a < 0) a += 2f * Mathf.PI; ang.Add(a); ang.Add(a - 1e-4f); ang.Add(a + 1e-4f); }
            ang.Sort();
            Vector3 up = faceUp ? Vector3.up : Vector3.down;
            for (int i = 0; i < ang.Count; i++)
            {
                float a0 = ang[i], a1 = ang[(i + 1) % ang.Count] + (i + 1 == ang.Count ? 2f * Mathf.PI : 0f);
                if (a1 - a0 < 1e-6f) continue;
                Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                float i0 = RayStadium(c, d0, c, hx, hz), i1 = RayStadium(c, d1, c, hx, hz), o0 = RayPoly(c, d0, poly), o1 = RayPoly(c, d1, poly);
                if (o0 <= i0 + 0.05f && o1 <= i1 + 0.05f) continue;
                o0 = Mathf.Max(o0, i0); o1 = Mathf.Max(o1, i1);
                Vector2 A = c + d0 * i0, B = c + d1 * i1, C = c + d1 * o1, D = c + d0 * o0;
                mb.Quad(new Vector3(A.x, y, A.y), new Vector3(B.x, y, B.y), new Vector3(C.x, y, C.y), new Vector3(D.x, y, D.y), up);
            }
        }
        public static void StadiumStrip(MB mb, Vector2 c, float hx, float hz, float o0, float o1, float y, int n)
        {
            var A = new List<Vector2>(); var B = new List<Vector2>(); var na = new List<Vector2>(); var nb = new List<Vector2>();
            Stadium(c, hx, hz, o0, n, A, na); Stadium(c, hx, hz, o1, n, B, nb);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                mb.Quad(new Vector3(A[i].x, y, A[i].y), new Vector3(A[j].x, y, A[j].y), new Vector3(B[j].x, y, B[j].y), new Vector3(B[i].x, y, B[i].y), Vector3.up);
            }
        }
        public static void StadiumWall(MB mb, Vector2 c, float hx, float hz, float off, float y0, float y1, int n, bool faceIn)
        {
            var A = new List<Vector2>(); var na = new List<Vector2>(); Stadium(c, hx, hz, off, n, A, na);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n; Vector2 m = (na[i] + na[j]) * (faceIn ? -0.5f : 0.5f);
                mb.Quad(new Vector3(A[i].x, y0, A[i].y), new Vector3(A[j].x, y0, A[j].y), new Vector3(A[j].x, y1, A[j].y), new Vector3(A[i].x, y1, A[i].y), new Vector3(m.x, 0, m.y));
            }
        }

        // ---------------------------------------------------------------- parts. key = material name
        public static Dictionary<string, MB> NewSet() { return new Dictionary<string, MB>(); }
        public static MB Get(Dictionary<string, MB> set, string mat)
        {
            MB m; if (set.TryGetValue(mat, out m)) return m;
            float tile = mat == "FloorBrick" ? 4f : mat == "ParadeBrick" || mat == "StoneBand" ? 2f : mat == "White" || mat == "Plinth" ? 4f : 8f;
            m = new MB(tile); set[mat] = m; return m;
        }

        public static Dictionary<string, MB> Floor(ShellSettings S)
        {
            var set = NewSet(); var W = ShellSettings.Wall;
            Ring(Get(set, "FloorBrick"), S.LoopC, S.LoopHX - 0.3f, S.LoopHZ - 0.3f, W, 0.02f, true);
            bool ccw = Area(W) > 0f;
            for (int i = 0; i < W.Length; i++)                                     // 벽 쪽 돌 띠 4 m
            {
                Vector2 a = W[i], b = W[(i + 1) % W.Length], u = (b - a).normalized, n = new Vector2(-u.y, u.x) * (ccw ? 1f : -1f);
                Get(set, "StoneBand").Quad(new Vector3(a.x, 0.03f, a.y), new Vector3(b.x, 0.03f, b.y), new Vector3(b.x + n.x * 4, 0.03f, b.y + n.y * 4), new Vector3(a.x + n.x * 4, 0.03f, a.y + n.y * 4), Vector3.up);
            }
            StadiumStrip(Get(set, "ParadeBrick"), S.LoopC, S.LoopHX, S.LoopHZ, 0f, 10f, 0.035f, 240);
            StadiumStrip(Get(set, "StoneBand"), S.LoopC, S.LoopHX, S.LoopHZ, 10f, 11.2f, 0.04f, 240);
            StadiumStrip(Get(set, "YellowLine"), S.LoopC, S.LoopHX, S.LoopHZ, -0.3f, 0.25f, 0.045f, 240);
            for (int k = 1; k < 5; k++) StadiumStrip(Get(set, "StoneBand"), S.LoopC, S.LoopHX, S.LoopHZ, k * 2f - 0.06f, k * 2f + 0.06f, 0.042f, 240);
            return set;
        }

        public static Dictionary<string, MB> Rink(ShellSettings S, System.Random rnd)
        {
            var set = NewSet();
            StadiumWall(Get(set, "Plaster"), S.LoopC, S.LoopHX, S.LoopHZ, -0.3f, -6f, 0.02f, 200, true);
            StadiumWall(Get(set, "White"), S.LoopC, S.LoopHX, S.LoopHZ, -3.2f, -5.95f, -4.85f, 200, false);
            StadiumWall(Get(set, "RoofBlue"), S.LoopC, S.LoopHX, S.LoopHZ, -3.18f, -5.95f, -5.7f, 200, false);
            StadiumStrip(Get(set, "White"), S.LoopC, S.LoopHX, S.LoopHZ, -3.3f, -3.1f, -4.85f, 200);
            if (S.Ice) { var ice = Get(set, "Ice"); var P = new List<Vector2>(); var Nn = new List<Vector2>(); Stadium(S.LoopC, S.LoopHX - 0.3f, S.LoopHZ - 0.3f, 0, 120, P, Nn);
                for (int i = 0; i < P.Count; i++) { int j = (i + 1) % P.Count; ice.Tri(new Vector3(S.LoopC.x, -5.95f, S.LoopC.y), new Vector3(P[i].x, -5.95f, P[i].y), new Vector3(P[j].x, -5.95f, P[j].y), Vector3.up); } }
            // glass rail + white top rail + posts
            StadiumWall(Get(set, "RailGlass"), S.LoopC, S.LoopHX, S.LoopHZ, -0.6f, 0.1f, 1.05f, 200, false);
            var R = new List<Vector2>(); var Rn = new List<Vector2>(); Stadium(S.LoopC, S.LoopHX, S.LoopHZ, -0.6f, 200, R, Rn);
            for (int i = 0; i < R.Count; i++)
            {
                int j = (i + 1) % R.Count;
                Get(set, "White").Member(new Vector3(R[i].x, 1.1f, R[i].y), new Vector3(R[j].x, 1.1f, R[j].y), 0.1f);
                if (i % 4 == 0) Get(set, "White").Box(new Vector3(R[i].x, 0.55f, R[i].y), new Vector3(0.1f, 1.1f, 0.1f));
            }
            // turrets (파란 고깔 망루) 18
            Stadium(S.LoopC, S.LoopHX, S.LoopHZ, 0.6f, 18, R, Rn);
            for (int k = 0; k < R.Count; k++)
            {
                Vector3 b = new Vector3(R[k].x, 0f, R[k].y);
                Get(set, "Plaster").Cyl(b, 0.95f, 0.6f, 20); Get(set, "Plaster").Cyl(b + Vector3.up * 0.6f, 0.75f, 3.4f, 20);
                Get(set, "RoofBlue").Cyl(b + Vector3.up * 0.88f, 0.77f, 0.25f, 20); Get(set, "White").Cyl(b + Vector3.up * 4.0f, 0.9f, 0.2f, 20);
                bool lit = rnd.NextDouble() > 0.35;
                for (int a = 0; a < 4; a++)
                {
                    float t = a * Mathf.PI / 2f; Vector3 d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t));
                    Get(set, lit ? "LampOn" : "WinDark").BoxO(b + d * 0.74f + Vector3.up * 3.3f, Vector3.Cross(Vector3.up, d), Vector3.up, d, new Vector3(0.35f, 0.6f, 0.06f));
                }
                Get(set, "RoofBlue").Cyl(b + Vector3.up * 4.2f, 1.05f, 1.8f, 20, 0.04f); Get(set, "Gold").Sphere(b + Vector3.up * 6.1f, 0.15f, 1f, 10);
            }
            return set;
        }

        public static Dictionary<string, MB> Facades(ShellSettings S, System.Random rnd)
        {
            var set = NewSet(); var W = ShellSettings.Wall; bool ccw = Area(W) > 0f; float H = S.H, B0 = ShellSettings.Band0; float[] G = ShellSettings.Gal;
            for (int idx = 0; idx < W.Length; idx++)
            {
                Vector2 a2 = W[idx], b2 = W[(idx + 1) % W.Length]; float L = (b2 - a2).magnitude; if (L < 3f) continue;
                Vector2 u2 = (b2 - a2) / L, n2 = new Vector2(-u2.y, u2.x) * (ccw ? 1f : -1f);
                Vector3 o = new Vector3(a2.x, 0, a2.y), u = new Vector3(u2.x, 0, u2.y), n = new Vector3(n2.x, 0, n2.y);
                float g0 = -1, g1 = -1;
                if (idx == ShellSettings.GateWall) { float t0 = (ShellSettings.GateX0 - a2.x) / u2.x, t1 = (ShellSettings.GateX1 - a2.x) / u2.x; g0 = Mathf.Min(t0, t1); g1 = Mathf.Max(t0, t1); }
                bool hasGap = g1 > g0;
                MB cream = Get(set, "Plaster"), white = Get(set, "White"), dark = Get(set, "Recess"), teal = Get(set, "Teal"), plinth = Get(set, "Plinth");
                // cladding (gate: leave a 16 x 9 m hole)
                if (hasGap)
                {
                    plinth.BoxW(o, u, n, g0 / 2, 0.6f, 0.25f, g0, 1.2f, 0.5f); plinth.BoxW(o, u, n, (g1 + L) / 2, 0.6f, 0.25f, L - g1, 1.2f, 0.5f);
                    cream.BoxW(o, u, n, g0 / 2, 4.1f, 0.1f, g0, 5.8f, 0.2f); cream.BoxW(o, u, n, (g1 + L) / 2, 4.1f, 0.1f, L - g1, 5.8f, 0.2f);
                    teal.BoxW(o, u, n, g0 / 2, 8f, 0.1f, g0, 2f, 0.2f); teal.BoxW(o, u, n, (g1 + L) / 2, 8f, 0.1f, L - g1, 2f, 0.2f);
                    teal.BoxW(o, u, n, (g0 + g1) / 2, 9.5f, 0.1f, g1 - g0, 1f, 0.2f);
                    white.BoxW(o, u, n, (g0 + g1) / 2, 9.5f, 0.6f, g1 - g0 + 2f, 1f, 1.2f);
                }
                else { plinth.BoxW(o, u, n, L / 2, 0.6f, 0.25f, L, 1.2f, 0.5f); cream.BoxW(o, u, n, L / 2, 4.1f, 0.1f, L, 5.8f, 0.2f); }
                teal.BoxW(o, u, n, L / 2, (10f + H) / 2, 0.1f, L, H - 10f, 0.2f);                          // 7 m 위는 전부 청록 한 면 (v2: 회랑·칸 없음)
                if (!hasGap) teal.BoxW(o, u, n, L / 2, 8.5f, 0.1f, L, 3f, 0.2f);
                cream.BoxW(o, u, n, L / 2, 6.4f, 0.45f, L, 0.4f, 0.5f);
                white.BoxW(o, u, n, L / 2, H - 3f, 0.7f, L, 1.2f, 1.0f); white.BoxW(o, u, n, L / 2, H - 0.8f, 1.0f, L, 0.8f, 1.6f);
                int bays = Mathf.Max(1, (int)(L / 6f)); float bw = L / bays;
                for (int k = 0; k < bays; k++)
                {
                    float x = (k + 0.5f) * bw;
                    if (hasGap && x > g0 - 2 && x < g1 + 2) continue;
                    dark.BoxW(o, u, n, x, 3.2f, 0.22f, Mathf.Min(3.4f, bw - 1.2f), 3.6f, 0.05f);             // 1F 막힌 아치
                    float ar = Mathf.Min(1.7f, bw / 2 - 0.6f);
                    for (int s = 0; s < 8; s++)
                    {
                        float t0 = Mathf.PI * s / 8, t1 = Mathf.PI * (s + 1) / 8;
                        Vector3 p0 = o + u * (x - ar * Mathf.Cos(t0)) + n * 0.24f + Vector3.up * (5f + ar * Mathf.Sin(t0));
                        Vector3 p1 = o + u * (x - ar * Mathf.Cos(t1)) + n * 0.24f + Vector3.up * (5f + ar * Mathf.Sin(t1));
                        Vector3 pc = o + u * x + n * 0.22f + Vector3.up * 5f;
                        dark.Tri(pc, p0, p1, n); white.Member(p0 + n * 0.05f, p1 + n * 0.05f, 0.18f);
                    }
                    if (k % 3 == 1) teal.BoxW(o, u, n, x, 6.1f, 0.3f, Mathf.Min(3f, bw - 1.5f), 0.55f, 0.1f);   // 빈 간판
                    white.BoxW(o, u, n, x - bw / 2, 3.6f, 0.35f, 0.5f, 6f, 0.5f);
                }
            }
            return set;
        }

        public static Dictionary<string, MB> Ceiling(ShellSettings S, System.Random rnd)
        {
            var set = NewSet(); var W = ShellSettings.Wall; float H = S.H;
            Ring(Get(set, "Ceiling"), S.LoopC, S.DomeHX, S.DomeHZ, W, H, false);
            Ring(Get(set, "Ceiling"), S.LoopC, S.DomeHX, S.DomeHZ, W, H + 1.2f, true);
            float bx0 = 1e9f, bx1 = -1e9f, bz0 = 1e9f, bz1 = -1e9f;
            foreach (Vector2 p in W) { bx0 = Mathf.Min(bx0, p.x); bx1 = Mathf.Max(bx1, p.x); bz0 = Mathf.Min(bz0, p.y); bz1 = Mathf.Max(bz1, p.y); }
            const float step = 10f;
            System.Func<float, float, float, bool> ok = (x, z, m) => Inside(new Vector2(x, z), W) && DistStadium(new Vector2(x, z), S.LoopC, S.DomeHX, S.DomeHZ) > m;
            for (int axis = 0; axis < 2; axis++)
            {
                float v0 = axis == 0 ? bx0 : bz0, v1 = axis == 0 ? bx1 : bz1, lo = axis == 0 ? bz0 : bx0, hi = axis == 0 ? bz1 : bx1;
                for (float v = v0; v <= v1; v += step)
                {
                    float seg0 = float.NaN;
                    for (float t = lo; t <= hi + 1f; t += 1f)
                    {
                        bool good = t <= hi && (axis == 0 ? ok(v, t, 2f) : ok(t, v, 2f));
                        if (good && float.IsNaN(seg0)) seg0 = t;
                        if (!good && !float.IsNaN(seg0))
                        {
                            float c = (seg0 + t) / 2, len = t - seg0;
                            if (len > 1f) Get(set, "CeilBeam").Box(axis == 0 ? new Vector3(v, H - 0.6f, c) : new Vector3(c, H - 0.6f, v), axis == 0 ? new Vector3(0.8f, 1.2f, len) : new Vector3(len, 1.2f, 0.8f));
                            seg0 = float.NaN;
                        }
                    }
                }
            }
            for (int i = 0; i < (int)((bx1 - bx0) / step); i++)
                for (int j = 0; j < (int)((bz1 - bz0) / step); j++)
                {
                    float x = bx0 + (i + 0.5f) * step, z = bz0 + (j + 0.5f) * step;
                    if (!ok(x, z, 6f) || (i + j) % 2 != 0) continue;
                    Get(set, rnd.NextDouble() < 0.8 ? "CeilLightOn" : "CeilLightOff").Box(new Vector3(x, H - 0.08f, z), new Vector3(3.2f, 0.12f, 3.2f));
                }
            return set;
        }

        public static Dictionary<string, MB> Dome(ShellSettings S, System.Random rnd)
        {
            var set = NewSet(); float H = S.H, hx = S.DomeHX, hz = S.DomeHZ, rise = S.DomeRise;
            const int NB = 192, NT = 24;
            var bp = new List<Vector2>(); var bn = new List<Vector2>(); Stadium(S.LoopC, hx, hz, 0f, NB, bp, bn);
            System.Func<int, float, Vector3> P = (i, t) =>
            {
                Vector2 pb = bp[((i % NB) + NB) % NB], pa = AxisPt(pb, S.LoopC, hx, hz), q = pa + (pb - pa) * (1 - t);
                return new Vector3(q.x, H + 1.2f + rise * Mathf.Sqrt(Mathf.Max(0f, 1 - (1 - t) * (1 - t))), q.y);
            };
            for (int i = 0; i < NB; i++)
                for (int k = 0; k < NT; k++)
                {
                    Vector3 a = P(i, (float)k / NT), b = P(i + 1, (float)k / NT), c = P(i + 1, (float)(k + 1) / NT), d = P(i, (float)(k + 1) / NT);
                    Vector3 cen = (a + b + c + d) * 0.25f, ax = new Vector3(AxisPt(new Vector2(cen.x, cen.z), S.LoopC, hx, hz).x, H - 30f, S.LoopC.y);
                    Get(set, "DomeGlass").Quad(a, b, c, d, ax - cen);       // face inward (seen from below)
                }
            // 리브: 양쪽이 맞물리게 해석적으로 배치 — 가운데 직선 구간은 가로 아치(양쪽 끝이 같은 x), 양 끝 반 돔은 꼭짓점으로 모이는 방사 리브,
            // 꼭대기 용마루 보가 두 꼭짓점을 잇는다
            float aa = Mathf.Max(0f, hx - hz);
            System.Func<Vector2, float, Vector3> Q = (pb, t) =>
            {
                Vector2 pa = AxisPt(pb, S.LoopC, hx, hz), q = pa + (pb - pa) * (1 - t);
                return new Vector3(q.x, H + 1.2f + rise * Mathf.Sqrt(Mathf.Max(0f, 1 - (1 - t) * (1 - t))), q.y);
            };
            MB steel = Get(set, "SteelWhite");
            System.Action<Vector2, float> Rib = (pb, th) => { for (int k = 0; k < NT; k++) steel.Member(Q(pb, (float)k / NT), Q(pb, (float)(k + 1) / NT), th); };
            int m = Mathf.Max(1, Mathf.RoundToInt(2f * aa / 16f));
            for (int j = 0; j <= m; j++)                                                        // 직선 구간 가로 아치 (아래·위 한 쌍)
            {
                float x = S.LoopC.x - aa + 2f * aa * j / m; float th = (j == 0 || j == m) ? 1.0f : 0.7f;
                Rib(new Vector2(x, S.LoopC.y - hz), th); Rib(new Vector2(x, S.LoopC.y + hz), th);
            }
            const int MC = 16;
            for (int side = -1; side <= 1; side += 2)                                           // 양 끝 반 돔 방사 리브
                for (int j = 1; j < MC; j++)
                {
                    float th = -Mathf.PI / 2f + Mathf.PI * j / MC; Vector2 cc = S.LoopC + new Vector2(side * aa, 0);
                    Rib(cc + new Vector2(side * Mathf.Cos(th), Mathf.Sin(th)) * hz, j % 2 == 0 ? 0.7f : 0.5f);
                }
            if (aa > 0.1f) steel.Member(new Vector3(S.LoopC.x - aa, H + 1.2f + rise, S.LoopC.y), new Vector3(S.LoopC.x + aa, H + 1.2f + rise, S.LoopC.y), 1.0f);   // 용마루
            for (int k = 0; k < NT; k += 3)
                for (int i = 0; i < NB; i++) steel.Member(P(i, (float)k / NT), P(i + 1, (float)k / NT), 0.4f);
            for (int i = 0; i < NB; i++)                                                       // 돔 테두리 보
            {
                Vector2 a = bp[i], b = bp[(i + 1) % NB], d = b - a; Vector3 mid = new Vector3((a.x + b.x) / 2, H - 0.3f, (a.y + b.y) / 2);
                Vector3 ax = new Vector3(d.x, 0, d.y).normalized, az = Vector3.Cross(ax, Vector3.up);
                Get(set, "White").BoxO(mid, ax, Vector3.up, az, new Vector3(d.magnitude + 0.05f, 3.0f, 2.2f));
            }
            return set;
        }

        /// one straight column, pivot at its base centre (prefab mesh)
        public static MB Column(float h, float r = 1.5f)
        {
            var mb = new MB(4f); mb.Cyl(Vector3.zero, r, h, 32); return mb;
        }
    }
}
