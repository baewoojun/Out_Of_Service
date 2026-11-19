// LotteWorldMagicIsland.cs  (v1 - Magic Island outdoor map: geometry, prefab builders and layout)
// Goes in Assets/Maps/LotteWorld/MagicIsland/Editor/ together with LotteWorldMagicIslandEditor.cs (own namespace, no clashes).
// The editor file turns what is described here into materials, mesh assets, prefabs and the scene
//   Assets/Maps/LotteWorld/Map3_MagicIsland.unity            (menu: Tools > LotteWorld > Magic Island > ...)
//
// Units are metres. MODULE FRAME: origin = outside face of the indoor main gate (LotteWorld_NorthGate root, Wall_02 line),
// +X east, +Z north (away from the indoor building, out over Seokchon lake), Y up.
// The scene root 'MagicIsland' carries the placement: default (-49.95, 0.03, 126.9) = right outside the indoor main gate.
// (0.03 lifts the outdoor paving just above the indoor floor slab where they overlap.)
//
// Plan source: the Magic Island guide map. Positions were read off the map picture with 1 px (of the 1100 px wide crop) = 0.225 m,
// the bridge's shore end = (0, 30). Heights follow the real park (Gyro Drop ~78 m, castle spire ~50 m) and the indoor map scale.
// Graphic rules (same as the other LotteWorld modules): colour, no logos / characters / lettering (all signs are blank boards),
// solid-colour clay materials. Buildings you can walk into have a 'Door_*' leaf (pivot = hinge, rotate local Y) and an empty room.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotteWorld.MagicIsland
{
    // ------------------------------------------------------------------------------------------------------------------
    // Materials (created as Assets/Maps/LotteWorld/MagicIsland/Materials/MI_<name>.mat)
    // ------------------------------------------------------------------------------------------------------------------
    public enum M
    {
        CastleCream, CastlePink, RoofBlue, RoofBlueDark, PaleBlue, White, Gold, StoneGrey, StoneDark, Paving, PavingDark, Asphalt,
        Grass, GrassDark, Leaf, LeafDark, LeafPink, Bark, Water, LakeBed, Foam, Rock, RockDark, RockGrey, RoofRed, RoofGrey, Timber,
        Plaster, Opening, Glass, Metal, Purple, Pink, Yellow, Orange, Red, Teal, Mint, Navy, Brick, LampGlow, Wood, Flower, CityA, CityB, CityC
    }

    public static class Palette
    {
        public static readonly Dictionary<M, Color> Colors = new Dictionary<M, Color>
        {
            { M.CastleCream,  new Color(0.93f, 0.87f, 0.80f) }, { M.CastlePink,   new Color(0.87f, 0.70f, 0.68f) },
            { M.RoofBlue,     new Color(0.20f, 0.45f, 0.74f) }, { M.RoofBlueDark, new Color(0.14f, 0.28f, 0.50f) },
            { M.PaleBlue,     new Color(0.60f, 0.82f, 0.88f) }, { M.White,        new Color(0.93f, 0.93f, 0.91f) },
            { M.Gold,         new Color(0.84f, 0.66f, 0.28f) }, { M.StoneGrey,    new Color(0.68f, 0.65f, 0.60f) },
            { M.StoneDark,    new Color(0.45f, 0.43f, 0.40f) }, { M.Paving,       new Color(0.80f, 0.70f, 0.57f) },
            { M.PavingDark,   new Color(0.62f, 0.55f, 0.47f) }, { M.Asphalt,      new Color(0.30f, 0.30f, 0.31f) },
            { M.Grass,        new Color(0.37f, 0.52f, 0.25f) }, { M.GrassDark,    new Color(0.26f, 0.39f, 0.19f) },
            { M.Leaf,         new Color(0.31f, 0.47f, 0.21f) }, { M.LeafDark,     new Color(0.17f, 0.32f, 0.17f) },
            { M.LeafPink,     new Color(0.95f, 0.76f, 0.83f) }, { M.Bark,         new Color(0.33f, 0.24f, 0.18f) },
            { M.Water,        new Color(0.21f, 0.43f, 0.42f, 0.86f) }, { M.LakeBed, new Color(0.13f, 0.16f, 0.13f) },
            { M.Foam,         new Color(0.90f, 0.95f, 0.97f, 0.75f) }, { M.Rock,      new Color(0.47f, 0.39f, 0.31f) },
            { M.RockDark,     new Color(0.31f, 0.27f, 0.23f) }, { M.RockGrey,     new Color(0.58f, 0.56f, 0.53f) },
            { M.RoofRed,      new Color(0.70f, 0.23f, 0.18f) }, { M.RoofGrey,     new Color(0.24f, 0.25f, 0.27f) },
            { M.Timber,       new Color(0.38f, 0.24f, 0.15f) }, { M.Plaster,      new Color(0.95f, 0.91f, 0.82f) },
            { M.Opening,      new Color(0.10f, 0.11f, 0.13f) }, { M.Glass,        new Color(0.16f, 0.22f, 0.28f) },
            { M.Metal,        new Color(0.45f, 0.47f, 0.50f) }, { M.Purple,       new Color(0.50f, 0.33f, 0.64f) },
            { M.Pink,         new Color(0.93f, 0.55f, 0.70f) }, { M.Yellow,       new Color(0.96f, 0.80f, 0.22f) },
            { M.Orange,       new Color(0.93f, 0.52f, 0.18f) }, { M.Red,          new Color(0.80f, 0.17f, 0.15f) },
            { M.Teal,         new Color(0.15f, 0.50f, 0.52f) }, { M.Mint,         new Color(0.58f, 0.82f, 0.72f) },
            { M.Navy,         new Color(0.14f, 0.17f, 0.32f) }, { M.Brick,        new Color(0.62f, 0.36f, 0.26f) },
            { M.LampGlow,     new Color(1.00f, 0.86f, 0.60f) }, { M.Wood,         new Color(0.55f, 0.40f, 0.26f) },
            { M.Flower,       new Color(0.86f, 0.30f, 0.38f) }, { M.CityA,        new Color(0.70f, 0.69f, 0.66f) },
            { M.CityB,        new Color(0.58f, 0.60f, 0.62f) }, { M.CityC,        new Color(0.46f, 0.49f, 0.53f) },
        };
        public static bool Transparent(M k) { return k == M.Water || k == M.Foam; }
        public static bool Emissive(M k) { return k == M.LampGlow; }
        public static float Smoothness(M k)
        {
            if (k == M.Water) return 0.92f;
            if (k == M.Glass || k == M.Opening) return 0.65f;
            if (k == M.Gold || k == M.Metal || k == M.PaleBlue) return 0.45f;
            return 0.15f;
        }
        public static float Metallic(M k) { return (k == M.Gold || k == M.Metal) ? 0.5f : 0f; }
    }

    // ------------------------------------------------------------------------------------------------------------------
    // Mesh builder: flat-shaded faces grouped into one sub-mesh per material, with a transform stack.
    // Every face gets an outward hint; the winding is fixed automatically (same builder as the other LotteWorld modules).
    // ------------------------------------------------------------------------------------------------------------------
    public class MeshBuilder
    {
        public class Part
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> T = new List<int>();
        }

        /// <summary>Named parts: everything drawn after Begin("Tower_SW") goes into the part 'Tower_SW'. Each part becomes its own
        /// GameObject (own mesh + collider), so every tower, wall, roof, window set ... can be moved or edited on its own.</summary>
        public readonly Dictionary<string, Dictionary<M, Part>> Groups = new Dictionary<string, Dictionary<M, Part>>();
        public readonly List<string> Order = new List<string>();
        string cur = "";
        public void Begin(string partName) { cur = partName ?? ""; }
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();

        /// <summary>All parts merged (per material).</summary>
        public Dictionary<M, Part> Parts
        {
            get
            {
                if (Order.Count == 1) return Groups[Order[0]];
                var o = new Dictionary<M, Part>();
                foreach (string g in Order)
                    foreach (KeyValuePair<M, Part> kv in Groups[g])
                    {
                        Part d; if (!o.TryGetValue(kv.Key, out d)) { d = new Part(); o[kv.Key] = d; }
                        int off = d.V.Count; d.V.AddRange(kv.Value.V); d.N.AddRange(kv.Value.N); d.UV.AddRange(kv.Value.UV);
                        foreach (int t in kv.Value.T) d.T.Add(t + off);
                    }
                return o;
            }
        }
        /// <summary>A builder holding just one named part.</summary>
        public MeshBuilder Only(string g) { var b = new MeshBuilder(); b.Groups[""] = Groups[g]; b.Order.Add(""); return b; }
        public int PartCount { get { int n = 0; foreach (string g in Order) { int t = 0; foreach (KeyValuePair<M, Part> kv in Groups[g]) t += kv.Value.T.Count; if (t > 0) n++; } return n; } }
        Matrix4x4 mtx = Matrix4x4.identity;

        public void Push(Vector3 pos, Quaternion rot) { stack.Push(mtx); mtx = mtx * Matrix4x4.TRS(pos, rot, Vector3.one); }
        public void Push(Vector3 pos, float yaw) { Push(pos, Quaternion.Euler(0f, yaw, 0f)); }
        /// <summary>Facade frame: x runs along the wall (left to right seen from outside), y up, wall surface z = 0, outside is -z.</summary>
        public void PushFacade(Vector3 origin, Vector3 outward)
        {
            Vector3 o = new Vector3(outward.x, 0f, outward.z).normalized;
            Push(origin, Quaternion.LookRotation(-o, Vector3.up));
        }
        public void Pop() { mtx = stack.Pop(); }
        public bool Empty { get { return TriangleCount == 0; } }
        public int TriangleCount { get { int n = 0; foreach (string g in Order) foreach (KeyValuePair<M, Part> kv in Groups[g]) n += kv.Value.T.Count / 3; return n; } }

        Part Get(M k)
        {
            Dictionary<M, Part> d;
            if (!Groups.TryGetValue(cur, out d)) { d = new Dictionary<M, Part>(); Groups[cur] = d; Order.Add(cur); }
            Part p;
            if (!d.TryGetValue(k, out p)) { p = new Part(); d[k] = p; }
            return p;
        }

        static Vector2 PlanarUV(Vector3 p, Vector3 n)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ax >= ay && ax >= az) return new Vector2(p.z, p.y) * 0.5f;
            if (ay >= az) return new Vector2(p.x, p.z) * 0.5f;
            return new Vector2(p.x, p.y) * 0.5f;
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, M k, Vector3 outDir)
        {
            Vector3 A = mtx.MultiplyPoint3x4(a), B = mtx.MultiplyPoint3x4(b), C = mtx.MultiplyPoint3x4(c), D = mtx.MultiplyPoint3x4(d);
            Vector3 o = mtx.MultiplyVector(outDir);
            Vector3 n = Vector3.Cross(B - A, C - A) + Vector3.Cross(C - A, D - A);
            if (n.sqrMagnitude < 1e-14f) return;
            if (Vector3.Dot(n, o) < 0f) { Vector3 t = B; B = D; D = t; n = -n; }
            n.Normalize();
            Part p = Get(k);
            int i = p.V.Count;
            p.V.Add(A); p.V.Add(B); p.V.Add(C); p.V.Add(D);
            for (int q = 0; q < 4; q++) p.N.Add(n);
            p.UV.Add(PlanarUV(A, n)); p.UV.Add(PlanarUV(B, n)); p.UV.Add(PlanarUV(C, n)); p.UV.Add(PlanarUV(D, n));
            p.T.Add(i); p.T.Add(i + 1); p.T.Add(i + 2);
            p.T.Add(i); p.T.Add(i + 2); p.T.Add(i + 3);
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, M k, Vector3 outDir)
        {
            Vector3 A = mtx.MultiplyPoint3x4(a), B = mtx.MultiplyPoint3x4(b), C = mtx.MultiplyPoint3x4(c);
            Vector3 o = mtx.MultiplyVector(outDir);
            Vector3 n = Vector3.Cross(B - A, C - A);
            if (n.sqrMagnitude < 1e-14f) return;
            if (Vector3.Dot(n, o) < 0f) { Vector3 t = B; B = C; C = t; n = -n; }
            n.Normalize();
            Part p = Get(k);
            int i = p.V.Count;
            p.V.Add(A); p.V.Add(B); p.V.Add(C);
            for (int q = 0; q < 3; q++) p.N.Add(n);
            p.UV.Add(PlanarUV(A, n)); p.UV.Add(PlanarUV(B, n)); p.UV.Add(PlanarUV(C, n));
            p.T.Add(i); p.T.Add(i + 1); p.T.Add(i + 2);
        }

        public void Box(Vector3 c, Vector3 size, M k) { BoxO(c, Vector3.right, Vector3.up, Vector3.forward, size, k); }

        public void BoxMM(float x0, float y0, float z0, float x1, float y1, float z1, M k)
        {
            if (x1 - x0 < 1e-4f || y1 - y0 < 1e-4f || z1 - z0 < 1e-4f) return;
            Box(new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0), k);
        }

        public void BoxO(Vector3 c, Vector3 ax, Vector3 ay, Vector3 az, Vector3 size, M k)
        {
            Vector3 X = ax * (size.x * 0.5f), Y = ay * (size.y * 0.5f), Z = az * (size.z * 0.5f);
            Vector3 p000 = c - X - Y - Z, p100 = c + X - Y - Z, p110 = c + X + Y - Z, p010 = c - X + Y - Z;
            Vector3 p001 = c - X - Y + Z, p101 = c + X - Y + Z, p111 = c + X + Y + Z, p011 = c - X + Y + Z;
            Quad(p100, p110, p111, p101, k, ax);
            Quad(p000, p001, p011, p010, k, -ax);
            Quad(p010, p011, p111, p110, k, ay);
            Quad(p000, p100, p101, p001, k, -ay);
            Quad(p001, p101, p111, p011, k, az);
            Quad(p000, p010, p110, p100, k, -az);
        }

        /// <summary>Square beam from p to q.</summary>
        public void Member(Vector3 p, Vector3 q, float th, M k)
        {
            Vector3 d = q - p; float L = d.magnitude;
            if (L < 1e-4f) return;
            Vector3 az = d / L;
            Vector3 ax = Vector3.Cross(Vector3.up, az);
            if (ax.sqrMagnitude < 1e-6f) ax = Vector3.right;
            ax.Normalize();
            Vector3 ay = Vector3.Cross(az, ax);
            BoxO((p + q) * 0.5f, ax, ay, az, new Vector3(th, th, L), k);
        }

        /// <summary>Surface of revolution around the local Y axis through b. Profile = (radius, height) from bottom to top.
        /// A first/last radius of 0 closes the shape.</summary>
        public void Lathe(Vector3 b, Vector2[] prof, int seg, M k, float rotDeg = 0f)
        {
            for (int j = 0; j < prof.Length - 1; j++)
            {
                float dr = prof[j + 1].x - prof[j].x, dy = prof[j + 1].y - prof[j].y;
                for (int i = 0; i < seg; i++)
                {
                    float a0 = (rotDeg + 360f * i / seg) * Mathf.Deg2Rad, a1 = (rotDeg + 360f * (i + 1) / seg) * Mathf.Deg2Rad, am = (a0 + a1) * 0.5f;
                    Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                    Vector3 dm = new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am));
                    Vector3 p00 = b + d0 * prof[j].x + Vector3.up * prof[j].y, p01 = b + d1 * prof[j].x + Vector3.up * prof[j].y;
                    Vector3 p10 = b + d0 * prof[j + 1].x + Vector3.up * prof[j + 1].y, p11 = b + d1 * prof[j + 1].x + Vector3.up * prof[j + 1].y;
                    Vector3 o = dm * dy - Vector3.up * dr;
                    if (prof[j].x < 1e-4f) Tri(p00, p11, p10, k, o);
                    else if (prof[j + 1].x < 1e-4f) Tri(p00, p01, p10, k, o);
                    else Quad(p00, p01, p11, p10, k, o);
                }
            }
        }

        /// <summary>Closed cylinder (or frustum) from y0 to y0 + h with radii r0 (bottom) and r1 (top).</summary>
        public void Cyl(Vector3 b, float r0, float r1, float h, int seg, M k, float rotDeg = 0f)
        {
            Lathe(b, new[] { new Vector2(0f, 0f), new Vector2(r0, 0f), new Vector2(r1, h), new Vector2(0f, h) }, seg, k, rotDeg);
        }
        public void Cone(Vector3 b, float r, float h, int seg, M k, float rotDeg = 0f)
        {
            Lathe(b, new[] { new Vector2(0f, 0f), new Vector2(r, 0f), new Vector2(0f, h) }, seg, k, rotDeg);
        }
        /// <summary>Low poly sphere / ellipsoid (radii rx, ry).</summary>
        public void Blob(Vector3 c, float rx, float ry, int seg, M k, float rotDeg = 0f)
        {
            const int rings = 5;
            var prof = new Vector2[rings + 1];
            for (int i = 0; i <= rings; i++) { float a = -Mathf.PI * 0.5f + Mathf.PI * i / rings; prof[i] = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry + ry); }
            prof[0].x = 0f; prof[rings].x = 0f;
            Lathe(c - Vector3.up * ry, prof, seg, k, rotDeg);
        }
        /// <summary>Tube made of square members along a polyline.</summary>
        public void Tube(List<Vector3> pts, float th, M k, bool closed)
        {
            int n = closed ? pts.Count : pts.Count - 1;
            for (int i = 0; i < n; i++) Member(pts[i], pts[(i + 1) % pts.Count], th, k);
        }

        /// <summary>Flat polygon (points as (x, z)) at height y, facing up (or down), holes allowed. Uses ear clipping.</summary>
        public void PolyFlat(List<Vector2> outer, List<List<Vector2>> holes, float y, M k, bool up = true)
        {
            List<Vector2> pts; List<int> tri = Geo.Triangulate(outer, holes, out pts);
            Vector3 o = up ? Vector3.up : Vector3.down;
            for (int i = 0; i < tri.Count; i += 3)
                Tri(new Vector3(pts[tri[i]].x, y, pts[tri[i]].y), new Vector3(pts[tri[i + 1]].x, y, pts[tri[i + 1]].y), new Vector3(pts[tri[i + 2]].x, y, pts[tri[i + 2]].y), k, o);
        }

        /// <summary>Vertical skirt under a closed outline from y0 to y1. outwardIsLeft: which side of the walking direction is outside.</summary>
        public void Skirt(List<Vector2> poly, float y0, float y1, M k, bool facingOut)
        {
            float area = Geo.Area(poly);
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 p = poly[i], q = poly[(i + 1) % poly.Count], e = q - p;
                Vector3 nOut = area > 0f ? new Vector3(e.y, 0f, -e.x) : new Vector3(-e.y, 0f, e.x);
                if (!facingOut) nOut = -nOut;
                Quad(new Vector3(p.x, y0, p.y), new Vector3(q.x, y0, q.y), new Vector3(q.x, y1, q.y), new Vector3(p.x, y1, p.y), k, nOut);
            }
        }

        /// <summary>Flat ribbon along a polyline of (x, z) points at height y (width w), with optional kerbs.</summary>
        public void Ribbon(List<Vector2> line, float w, float y, float th, M k, M kerb, float kerbW, bool closed = false)
        {
            int n = line.Count;
            var L = new List<Vector2>(); var R = new List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                Vector2 t = closed ? (line[(i + 1) % n] - line[(i + n - 1) % n]).normalized
                          : (i == 0 ? line[1] - line[0] : i == n - 1 ? line[n - 1] - line[n - 2] : line[i + 1] - line[i - 1]).normalized;
                var nrm = new Vector2(-t.y, t.x);
                L.Add(line[i] + nrm * (w * 0.5f)); R.Add(line[i] - nrm * (w * 0.5f));
            }
            if (closed) { L.Add(L[0]); R.Add(R[0]); n++; }
            for (int i = 0; i < n - 1; i++)
            {
                Quad(V3(L[i], y + th), V3(L[i + 1], y + th), V3(R[i + 1], y + th), V3(R[i], y + th), k, Vector3.up);
                Vector2 lo = (L[i] + L[i + 1]) * 0.5f - (R[i] + R[i + 1]) * 0.5f;
                Quad(V3(L[i], y), V3(L[i + 1], y), V3(L[i + 1], y + th), V3(L[i], y + th), k, new Vector3(lo.x, 0f, lo.y));
                Quad(V3(R[i], y), V3(R[i + 1], y), V3(R[i + 1], y + th), V3(R[i], y + th), k, new Vector3(-lo.x, 0f, -lo.y));
                if (kerbW > 0f)
                {
                    Member(V3(L[i], y + th + 0.06f), V3(L[i + 1], y + th + 0.06f), kerbW, kerb);
                    Member(V3(R[i], y + th + 0.06f), V3(R[i + 1], y + th + 0.06f), kerbW, kerb);
                }
            }
        }
        static Vector3 V3(Vector2 p, float y) { return new Vector3(p.x, y, p.y); }
    }

    // ------------------------------------------------------------------------------------------------------------------
    // 2D geometry helpers (points are (x, z))
    // ------------------------------------------------------------------------------------------------------------------
    public static class Geo
    {
        public static float Area(List<Vector2> p)
        {
            float a = 0f;
            for (int i = 0; i < p.Count; i++) { Vector2 u = p[i], v = p[(i + 1) % p.Count]; a += u.x * v.y - v.x * u.y; }
            return a * 0.5f;
        }
        static float Cross(Vector2 a, Vector2 b, Vector2 c) { return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x); }
        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(a, b, p), d2 = Cross(b, c, p), d3 = Cross(c, a, p);
            return d1 >= -1e-6f && d2 >= -1e-6f && d3 >= -1e-6f;
        }
        static bool Same(Vector2 a, Vector2 b) { return (a - b).sqrMagnitude < 1e-8f; }

        public static List<Vector2> Reversed(List<Vector2> p) { var r = new List<Vector2>(p); r.Reverse(); return r; }

        /// <summary>Ear clipping with holes (holes are bridged into the outline first). Returns indices into 'pts'.</summary>
        public static List<int> Triangulate(List<Vector2> outer, List<List<Vector2>> holes, out List<Vector2> pts)
        {
            List<Vector2> poly = Area(outer) > 0f ? new List<Vector2>(outer) : Reversed(outer);   // CCW
            if (holes != null)
            {
                var hs = new List<List<Vector2>>();
                foreach (List<Vector2> h in holes) hs.Add(Area(h) < 0f ? new List<Vector2>(h) : Reversed(h));   // CW
                hs.Sort((a, b) => MaxX(b).CompareTo(MaxX(a)));
                foreach (List<Vector2> h in hs) poly = Bridge(poly, h);
            }
            pts = poly;
            var tris = new List<int>();
            var idx = new List<int>(); for (int i = 0; i < poly.Count; i++) idx.Add(i);
            int guard = 0;
            while (idx.Count > 3 && guard++ < 100000)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                    Vector2 a = poly[ia], b = poly[ib], c = poly[ic];
                    if (Cross(a, b, c) <= 1e-7f) continue;                    // reflex or degenerate
                    bool ear = true;
                    for (int j = 0; j < idx.Count && ear; j++)
                    {
                        int ij = idx[j]; if (ij == ia || ij == ib || ij == ic) continue;
                        Vector2 p = poly[ij];
                        if (Same(p, a) || Same(p, b) || Same(p, c)) continue;
                        if (InTri(p, a, b, c)) ear = false;
                    }
                    if (!ear) continue;
                    tris.Add(ia); tris.Add(ib); tris.Add(ic);
                    idx.RemoveAt(i); clipped = true; break;
                }
                if (!clipped)                                                     // numerical trouble: drop the flattest vertex
                {
                    int best = 0; float bc = float.MaxValue;
                    for (int i = 0; i < idx.Count; i++)
                    {
                        float cr = Mathf.Abs(Cross(poly[idx[(i + idx.Count - 1) % idx.Count]], poly[idx[i]], poly[idx[(i + 1) % idx.Count]]));
                        if (cr < bc) { bc = cr; best = i; }
                    }
                    idx.RemoveAt(best);
                }
            }
            if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
            return tris;
        }
        static float MaxX(List<Vector2> p) { float m = float.MinValue; foreach (Vector2 v in p) m = Mathf.Max(m, v.x); return m; }

        static List<Vector2> Bridge(List<Vector2> poly, List<Vector2> hole)
        {
            int mi = 0; for (int i = 1; i < hole.Count; i++) if (hole[i].x > hole[mi].x) mi = i;
            Vector2 Mp = hole[mi];
            int pi = -1; float bestX = float.MaxValue; Vector2 I = Mp;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
                if ((a.y > Mp.y) == (b.y > Mp.y)) continue;
                float x = a.x + (Mp.y - a.y) * (b.x - a.x) / (b.y - a.y);
                if (x < Mp.x || x > bestX) continue;
                bestX = x; I = new Vector2(x, Mp.y); pi = a.x > b.x ? i : (i + 1) % poly.Count;
            }
            if (pi < 0) return poly;
            Vector2 P = poly[pi];
            float bestAng = float.MaxValue;
            for (int i = 0; i < poly.Count; i++)                                 // a reflex vertex inside (M, I, P) is the better bridge
            {
                Vector2 v = poly[i]; if (i == pi) continue;
                Vector2 pv = poly[(i + poly.Count - 1) % poly.Count], nv = poly[(i + 1) % poly.Count];
                if (Cross(pv, v, nv) > 0f) continue;
                bool inside = Cross(Mp, I, P) > 0f ? InTri(v, Mp, I, P) : InTri(v, Mp, P, I);
                if (!inside) continue;
                Vector2 d = v - Mp; float ang = Mathf.Abs(Mathf.Atan2(d.y, d.x));
                if (ang < bestAng) { bestAng = ang; pi = i; }
            }
            var o = new List<Vector2>();
            for (int i = 0; i <= pi; i++) o.Add(poly[i]);
            for (int i = 0; i <= hole.Count; i++) o.Add(hole[(mi + i) % hole.Count]);
            o.Add(poly[pi]);
            for (int i = pi + 1; i < poly.Count; i++) o.Add(poly[i]);
            return o;
        }

        /// <summary>Closed Catmull-Rom curve through the points, 'per' samples per span.</summary>
        public static List<Vector2> SmoothClosed(List<Vector2> p, int per)
        {
            var o = new List<Vector2>(); int n = p.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = p[(i + n - 1) % n], p1 = p[i], p2 = p[(i + 1) % n], p3 = p[(i + 2) % n];
                for (int s = 0; s < per; s++) o.Add(CR(p0, p1, p2, p3, (float)s / per));
            }
            return o;
        }
        public static Vector2 CR(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
        public static Vector3 CR3(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
        /// <summary>Catmull-Rom through 3D points (open or closed), roughly 'step' metres between samples.</summary>
        public static List<Vector3> Spline(List<Vector3> p, bool closed, float step)
        {
            var o = new List<Vector3>(); int n = p.Count, spans = closed ? n : n - 1;
            for (int i = 0; i < spans; i++)
            {
                Vector3 p1 = p[i], p2 = p[(i + 1) % n];
                Vector3 p0 = closed ? p[(i + n - 1) % n] : (i == 0 ? p1 * 2f - p2 : p[i - 1]);
                Vector3 p3 = closed ? p[(i + 2) % n] : (i + 2 >= n ? p2 * 2f - p1 : p[i + 2]);
                int k = Mathf.Max(2, Mathf.CeilToInt((p2 - p1).magnitude / step));
                for (int s = 0; s < k; s++) o.Add(CR3(p0, p1, p2, p3, (float)s / k));
            }
            if (!closed) o.Add(p[n - 1]);
            return o;
        }
        /// <summary>Chaikin corner cutting (closed).</summary>
        public static List<Vector2> Chaikin(List<Vector2> p, int it)
        {
            for (int k = 0; k < it; k++)
            {
                var o = new List<Vector2>();
                for (int i = 0; i < p.Count; i++) { Vector2 a = p[i], b = p[(i + 1) % p.Count]; o.Add(a * 0.75f + b * 0.25f); o.Add(a * 0.25f + b * 0.75f); }
                p = o;
            }
            return p;
        }
        /// <summary>Offset a closed polygon outward by d (negative = inward), mitre joints clamped.</summary>
        public static List<Vector2> Offset(List<Vector2> p, float d)
        {
            float s = Area(p) > 0f ? 1f : -1f; var o = new List<Vector2>(); int n = p.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = p[(i + n - 1) % n], b = p[i], c = p[(i + 1) % n];
                Vector2 e0 = (b - a).normalized, e1 = (c - b).normalized;
                Vector2 n0 = new Vector2(e0.y, -e0.x) * s, n1 = new Vector2(e1.y, -e1.x) * s;
                Vector2 m = (n0 + n1).normalized; float cs = Vector2.Dot(m, n0);
                o.Add(b + m * (d / Mathf.Max(0.35f, cs)));
            }
            return o;
        }
        /// <summary>Points every 'step' metres along a closed polyline, with the outward normal.</summary>
        public static void Walk(List<Vector2> p, float step, float startOff, List<Vector2> pos, List<Vector2> nrm)
        {
            float s = Area(p) > 0f ? 1f : -1f, acc = startOff;
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % p.Count], e = b - a; float L = e.magnitude; if (L < 1e-4f) continue;
                Vector2 t = e / L, nn = new Vector2(t.y, -t.x) * s;
                while (acc < L) { pos.Add(a + t * acc); nrm.Add(nn); acc += step; }
                acc -= L;
            }
        }
        public static bool Inside(List<Vector2> poly, Vector2 q)
        {
            bool c = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                if (((poly[i].y > q.y) != (poly[j].y > q.y)) && (q.x < (poly[j].x - poly[i].x) * (q.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)) c = !c;
            return c;
        }
        public static float DistToPoly(List<Vector2> poly, Vector2 q)
        {
            float best = float.MaxValue;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Count], ab = b - a;
                float t = Mathf.Clamp(Vector2.Dot(q - a, ab) / Mathf.Max(1e-6f, Vector2.Dot(ab, ab)), 0f, 1f);
                best = Mathf.Min(best, (a + ab * t - q).magnitude);
            }
            return best;
        }
        public static List<Vector2> Circle(Vector2 c, float rx, float rz, int n)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < n; i++) { float a = Mathf.PI * 2f * i / n; o.Add(new Vector2(c.x + rx * Mathf.Cos(a), c.y + rz * Mathf.Sin(a))); }
            return o;
        }
        public static List<Vector2> Rect(float x0, float z0, float x1, float z1)
        {
            return new List<Vector2> { new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1) };
        }
    }

    // ------------------------------------------------------------------------------------------------------------------
    // Scene description: a tree of nodes. The editor turns a node into a GameObject (mesh asset + MeshCollider when it has a
    // mesh), or into a prefab instance when Prefab is set.
    // ------------------------------------------------------------------------------------------------------------------
    public class LightSpec
    {
        public bool Directional; public Color Color = new Color(1f, 0.85f, 0.62f); public float Intensity = 1.5f, Range = 9f; public bool Enabled;
    }

    public class Node
    {
        public string Name;
        public Vector3 Pos;
        public Vector3 Euler;
        public float Scale = 1f;
        public MeshBuilder Mesh;
        public string Prefab;                 // prefab key -> prefab instance
        public bool Collider = true;          // MeshCollider (Convex off) on the mesh
        public bool IsStatic = true;          // false for parts meant to move (doors, ride rotors)
        public LightSpec Light;
        public string Note;                   // shown in the console summary
        public readonly List<Node> Kids = new List<Node>();

        public Node(string name) { Name = name; }

        /// <summary>Turns every mesh drawn in several named parts into one child node per part (named after the part).
        /// Unnamed geometry keeps the node's own name (as a child '<name>_Main' when there are named parts too).</summary>
        public static void SplitParts(Node n)
        {
            foreach (Node c in n.Kids) SplitParts(c);
            if (n.Mesh == null || n.Mesh.PartCount < 2) return;
            MeshBuilder m = n.Mesh; n.Mesh = null;
            var add = new List<Node>();
            foreach (string g in m.Order)
            {
                MeshBuilder pb = m.Only(g); if (pb.Empty) continue;
                add.Add(new Node(g == "" ? n.Name + "_Main" : g, pb) { Collider = n.Collider, IsStatic = n.IsStatic });
            }
            n.Kids.InsertRange(0, add);
        }
        public Node(string name, MeshBuilder mesh) { Name = name; Mesh = mesh; }
        public Node Add(Node c) { Kids.Add(c); return c; }
        public Node Group(string name) { var c = new Node(name); Kids.Add(c); return c; }
        public Node At(float x, float y, float z, float yaw = 0f) { Pos = new Vector3(x, y, z); Euler = new Vector3(0f, yaw, 0f); return this; }
        public static Node Inst(string prefab, string name, float x, float z, float yaw = 0f, float scale = 1f, float y = 0f)
        {
            var n = new Node(name) { Prefab = prefab, Scale = scale };
            n.At(x, y, z, yaw);
            return n;
        }
    }
}
#endif
