// LotteWorldMagicIslandParts.cs  (v1 - Magic Island: architectural parts + the reusable prefab builders)
// Every prefab is built in its own local frame: pivot = centre of the footprint on the ground, front (door side) facing -Z.
// So a prefab placed with yaw 0 faces south (towards the indoor building); yaw 90 faces west, -90 east, 180 north.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotteWorld.MagicIsland
{
    public struct Face { public Vector3 Origin; public Vector3 Normal; public float Length; }

    public static class Lib
    {
        static Vector3 V(Vector2 p, float z) { return new Vector3(p.x, p.y, z); }

        public static Face[] RectFaces(float x0, float x1, float z0, float z1)
        {
            return new[]
            {
                new Face { Origin = new Vector3(x0, 0f, z0), Normal = Vector3.back,    Length = x1 - x0 },
                new Face { Origin = new Vector3(x1, 0f, z0), Normal = Vector3.right,   Length = z1 - z0 },
                new Face { Origin = new Vector3(x1, 0f, z1), Normal = Vector3.forward, Length = x1 - x0 },
                new Face { Origin = new Vector3(x0, 0f, z1), Normal = Vector3.left,    Length = z1 - z0 },
            };
        }

        public static List<Vector2> ArchPts(float cx, float y0, float w, float h, int n = 10)
        {
            var pts = new List<Vector2>();
            float r = w * 0.5f, yc = Mathf.Max(y0, y0 + h - r);
            pts.Add(new Vector2(cx - r, y0));
            for (int i = 0; i <= n; i++) { float a = Mathf.PI - Mathf.PI * i / n; pts.Add(new Vector2(cx + r * Mathf.Cos(a), yc + r * Mathf.Sin(a))); }
            pts.Add(new Vector2(cx + r, y0));
            return pts;
        }

        /// <summary>Extrude a CONVEX outline drawn in the local XY plane between z0 and z1.</summary>
        public static void ExtrudeXY(MeshBuilder b, List<Vector2> poly, float z0, float z1, M k)
        {
            float area = 0f;
            for (int i = 0; i < poly.Count; i++) { Vector2 p = poly[i], q = poly[(i + 1) % poly.Count]; area += p.x * q.y - q.x * p.y; }
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 p = poly[i], q = poly[(i + 1) % poly.Count], e = q - p;
                Vector3 outw = area > 0f ? new Vector3(e.y, -e.x, 0f) : new Vector3(-e.y, e.x, 0f);
                b.Quad(V(p, z0), V(q, z0), V(q, z1), V(p, z1), k, outw);
            }
            for (int i = 1; i < poly.Count - 1; i++)
            {
                b.Tri(V(poly[0], z0), V(poly[i], z0), V(poly[i + 1], z0), k, Vector3.back);
                b.Tri(V(poly[0], z1), V(poly[i], z1), V(poly[i + 1], z1), k, Vector3.forward);
            }
        }

        /// <summary>Prism over a polygon in the XZ plane (points as (x, z)), y0..y1, concave allowed.</summary>
        public static void PrismXZ(MeshBuilder b, List<Vector2> p, float y0, float y1, M k, M top)
        {
            b.Skirt(p, y0, y1, k, true);
            b.PolyFlat(p, null, y1, top, true);
            b.PolyFlat(p, null, y0, k, false);
        }

        /// <summary>Moulded arch ring around an opening w x h (facade frame).</summary>
        public static void ArchBand(MeshBuilder b, float cx, float y0, float w, float h, float bw, float z0, float z1, M k)
        {
            List<Vector2> o = ArchPts(cx, y0, w + 2f * bw, h + bw), n = ArchPts(cx, y0, w, h);
            var cen = new Vector2(cx, y0 + h - w * 0.5f);
            for (int j = 0; j < o.Count - 1; j++)
            {
                b.Quad(V(o[j], z0), V(o[j + 1], z0), V(n[j + 1], z0), V(n[j], z0), k, Vector3.back);
                b.Quad(V(o[j], z1), V(o[j + 1], z1), V(n[j + 1], z1), V(n[j], z1), k, Vector3.forward);
                Vector2 om = (o[j] + o[j + 1]) * 0.5f - cen, im = (n[j] + n[j + 1]) * 0.5f - cen;
                b.Quad(V(o[j], z0), V(o[j + 1], z0), V(o[j + 1], z1), V(o[j], z1), k, new Vector3(om.x, om.y, 0f));
                b.Quad(V(n[j], z0), V(n[j + 1], z0), V(n[j + 1], z1), V(n[j], z1), k, new Vector3(-im.x, -im.y, 0f));
            }
        }

        /// <summary>Wall slab with a walk-through hole (round-arched if arched). Hole sides are real faces.</summary>
        public static void WallWithHole(MeshBuilder b, float x0, float x1, float y0, float y1, float z0, float z1, float cx, float w, float h, bool arched, M k)
        {
            float hx0 = cx - w * 0.5f, hx1 = cx + w * 0.5f, top = y0 + h;
            if (hx0 > x0) b.BoxMM(x0, y0, z0, hx0, y1, z1, k);
            if (x1 > hx1) b.BoxMM(hx1, y0, z0, x1, y1, z1, k);
            if (y1 > top) b.BoxMM(hx0, top, z0, hx1, y1, z1, k);
            if (!arched) return;
            float r = w * 0.5f, yc = top - r;
            for (int sd = -1; sd <= 1; sd += 2)
            {
                var sp = new List<Vector2> { new Vector2(cx + sd * r, top) };
                for (int i = 0; i <= 8; i++) { float a = Mathf.PI * 0.5f * i / 8f; sp.Add(new Vector2(cx + sd * r * Mathf.Cos(a), yc + r * Mathf.Sin(a))); }
                ExtrudeXY(b, sp, z0, z1, k);
            }
        }

        /// <summary>Recessed window (facade frame): dark glazing and a projecting frame.</summary>
        public static void Window(MeshBuilder b, float cx, float y0, float w, float h, M frame, bool arched = false)
        {
            const float t = 0.18f, d = 0.22f;
            if (arched)
            {
                ExtrudeXY(b, ArchPts(cx, y0, w, h, 8), -0.02f, 0.0f, M.Glass);
                ArchBand(b, cx, y0, w, h, t, -d, 0f, frame);
            }
            else
            {
                b.BoxMM(cx - w * 0.5f, y0, -0.03f, cx + w * 0.5f, y0 + h, 0f, M.Glass);
                b.BoxMM(cx - w * 0.5f - t, y0, -d, cx - w * 0.5f, y0 + h, 0f, frame);
                b.BoxMM(cx + w * 0.5f, y0, -d, cx + w * 0.5f + t, y0 + h, 0f, frame);
                b.BoxMM(cx - w * 0.5f - t, y0 + h, -d, cx + w * 0.5f + t, y0 + h + t, 0f, frame);
            }
            b.BoxMM(cx - w * 0.5f - t - 0.08f, y0 - t, -d - 0.06f, cx + w * 0.5f + t + 0.08f, y0, 0f, frame);
        }

        /// <summary>Hipped roof over x0..x1 / z0..z1 (ridge along the longer side), eaves at 'eave'.</summary>
        public static void HipRoof(MeshBuilder b, float x0, float x1, float z0, float z1, float eave, float rise, float ov, M roof, M fascia)
        {
            if (z1 - z0 > x1 - x0)
            {
                b.Push(new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f), 90f);
                float hx = (x1 - x0) * 0.5f, hz = (z1 - z0) * 0.5f;
                HipRoof(b, -hz, hz, -hx, hx, eave, rise, ov, roof, fascia);
                b.Pop();
                return;
            }
            float hd = (z1 - z0) * 0.5f, kk = rise / hd;
            float ex0 = x0 - ov, ex1 = x1 + ov, ez0 = z0 - ov, ez1 = z1 + ov, ey = eave - ov * kk, ry = eave + rise, zc = (z0 + z1) * 0.5f, hs = (ez1 - ez0) * 0.5f;
            float rx0 = ex0 + hs, rx1 = ex1 - hs;
            if (rx1 < rx0) { rx0 = rx1 = (ex0 + ex1) * 0.5f; }
            Vector3 A = new Vector3(ex0, ey, ez0), B = new Vector3(ex1, ey, ez0), C = new Vector3(ex1, ey, ez1), D = new Vector3(ex0, ey, ez1);
            Vector3 R0 = new Vector3(rx0, ry, zc), R1 = new Vector3(rx1, ry, zc);
            b.Quad(A, B, R1, R0, roof, new Vector3(0f, 1f, -1f));
            b.Quad(C, D, R0, R1, roof, new Vector3(0f, 1f, 1f));
            b.Tri(D, A, R0, roof, new Vector3(-1f, 1f, 0f));
            b.Tri(B, C, R1, roof, new Vector3(1f, 1f, 0f));
            b.Quad(A, B, C, D, roof, Vector3.down);
            b.BoxMM(ex0, ey - 0.25f, ez0 - 0.06f, ex1, ey + 0.02f, ez0 + 0.04f, fascia);
            b.BoxMM(ex0, ey - 0.25f, ez1 - 0.04f, ex1, ey + 0.02f, ez1 + 0.06f, fascia);
        }

        /// <summary>Steep gable roof with the ridge along X (gables facing -X / +X); eaves along the front (-Z) and back (+Z).</summary>
        public static void GableRoofX(MeshBuilder b, float x0, float x1, float z0, float z1, float eave, float rise, float ov, M roof, M gable)
        {
            float zc = (z0 + z1) * 0.5f, hs = (z1 - z0) * 0.5f, kk = rise / hs, ey = eave - ov * kk, ry = eave + rise;
            Vector3 A = new Vector3(x0 - ov, ey, z0 - ov), B = new Vector3(x1 + ov, ey, z0 - ov), C = new Vector3(x1 + ov, ey, z1 + ov), D = new Vector3(x0 - ov, ey, z1 + ov);
            Vector3 R0 = new Vector3(x0 - ov, ry + 0.15f, zc), R1 = new Vector3(x1 + ov, ry + 0.15f, zc);
            const float th = 0.25f;
            b.Quad(A, B, R1, R0, roof, new Vector3(0f, 1f, -1f));
            b.Quad(D, C, R1, R0, roof, new Vector3(0f, 1f, 1f));
            Vector3 dn = Vector3.down * th;
            b.Quad(A + dn, B + dn, R1 + dn, R0 + dn, roof, new Vector3(0f, -1f, 1f));
            b.Quad(D + dn, C + dn, R1 + dn, R0 + dn, roof, new Vector3(0f, -1f, -1f));
            b.Quad(A, B, B + dn, A + dn, roof, Vector3.back);
            b.Quad(D, C, C + dn, D + dn, roof, Vector3.forward);
            b.Quad(A, R0, R0 + dn, A + dn, roof, Vector3.left); b.Quad(D, R0, R0 + dn, D + dn, roof, Vector3.left);
            b.Quad(B, R1, R1 + dn, B + dn, roof, Vector3.right); b.Quad(C, R1, R1 + dn, C + dn, roof, Vector3.right);
            b.Tri(new Vector3(x0, eave, z0), new Vector3(x0, eave, z1), new Vector3(x0, ry, zc), gable, Vector3.left);
            b.Tri(new Vector3(x1, eave, z0), new Vector3(x1, eave, z1), new Vector3(x1, ry, zc), gable, Vector3.right);
        }

        /// <summary>Gable roof with the ridge along Z (gable facing the front -Z and the back +Z).</summary>
        public static void GableRoofZ(MeshBuilder b, float x0, float x1, float z0, float z1, float eave, float rise, float ov, M roof, M gable)
        {
            b.Push(new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f), 90f);
            float hx = (x1 - x0) * 0.5f, hz = (z1 - z0) * 0.5f;
            GableRoofX(b, -hz, hz, -hx, hx, eave, rise, ov, roof, gable);
            b.Pop();
        }

        public static void TileFloor(MeshBuilder b, float x0, float x1, float z0, float z1, float y, float s, M a, M c)
        {
            int nx = Mathf.CeilToInt((x1 - x0) / s - 0.01f), nz = Mathf.CeilToInt((z1 - z0) / s - 0.01f);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                    b.BoxMM(x0 + i * s, y, z0 + j * s, Mathf.Min(x1, x0 + (i + 1) * s), y + 0.04f, Mathf.Min(z1, z0 + (j + 1) * s), (i + j) % 2 == 0 ? a : c);
        }

        public static void Bulb(MeshBuilder b, Vector3 ceiling)
        {
            b.Box(ceiling - Vector3.up * 0.35f, new Vector3(0.02f, 0.7f, 0.02f), M.Opening);
            b.Box(ceiling - Vector3.up * 0.8f, new Vector3(0.16f, 0.2f, 0.16f), M.LampGlow);
        }

        /// <summary>Box building with a walk-in front room: front wall (z0, outside -Z) with a door opening at doorX,
        /// room depth roomD and ceiling rh; everything above / behind is solid up to H. Returns the hinge position.</summary>
        public static Vector3 BlockWithRoom(MeshBuilder b, float x0, float x1, float z0, float z1, float H, float roomD, float rh, float doorX, float doorW, float doorH, M wall, M floorA, M floorB, float wt = 0.3f)
        {
            float zr = Mathf.Min(z1, z0 + roomD);
            b.BoxMM(x0, 0f, z0, doorX - doorW * 0.5f, rh, z0 + wt, wall);
            b.BoxMM(doorX + doorW * 0.5f, 0f, z0, x1, rh, z0 + wt, wall);
            b.BoxMM(doorX - doorW * 0.5f, doorH, z0, doorX + doorW * 0.5f, rh, z0 + wt, wall);
            b.BoxMM(x0, 0f, z0 + wt, x0 + wt, rh, zr, wall);
            b.BoxMM(x1 - wt, 0f, z0 + wt, x1, rh, zr, wall);
            if (zr < z1 - 0.01f) b.BoxMM(x0, 0f, zr - wt, x1, rh, z1, wall); else b.BoxMM(x0 + wt, 0f, zr - wt, x1 - wt, rh, zr, wall);
            b.BoxMM(x0, rh, z0, x1, H, z1, wall);
            TileFloor(b, x0 + wt, x1 - wt, z0 + wt, zr - wt, 0f, 1.2f, floorA, floorB);
            Bulb(b, new Vector3((x0 + x1) * 0.5f, rh, (z0 + zr) * 0.5f));
            return new Vector3(doorX - doorW * 0.5f + 0.05f, 0f, z0 + wt * 0.5f);
        }

        /// <summary>Door leaf as its own node: pivot on the hinge, leaf along +X, closed. Rotate local Y (negative = opens inward / +Z).</summary>
        public static Node DoorLeaf(string name, Vector3 hinge, float w, float h, M k, bool glazed)
        {
            var b = new MeshBuilder();
            const float t = 0.07f;
            float cx = w * 0.5f;
            b.Box(new Vector3(cx, h * 0.5f, 0f), new Vector3(w - 0.04f, h, t), k);
            foreach (float zs in new[] { -1f, 1f })
            {
                float zf = zs * (t * 0.5f + 0.012f);
                b.Box(new Vector3(cx, h * 0.27f, zf), new Vector3(w * 0.64f, h * 0.3f, 0.025f), M.Timber);
                b.Box(new Vector3(cx, h * 0.68f, zf), new Vector3(w * 0.64f, h * 0.38f, 0.025f), glazed ? M.Glass : M.Timber);
                b.Box(new Vector3(w - 0.12f, 1.05f, zs * (t * 0.5f + 0.05f)), new Vector3(0.06f, 0.06f, 0.1f), M.Gold);
            }
            var n = new Node(name, b) { IsStatic = false, Note = "door: rotate local Y, negative = opens inward" };
            n.Pos = hinge;
            return n;
        }

        /// <summary>Simple metal railing along a polyline (posts every ~2 m, two rails).</summary>
        public static void Railing(MeshBuilder b, List<Vector3> pts, M k, float h = 1.1f)
        {
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 a = pts[i], c = pts[i + 1]; float L = (c - a).magnitude; if (L < 0.05f) continue;
                int n = Mathf.Max(1, Mathf.RoundToInt(L / 2f));
                for (int j = 0; j < n; j++) b.Box(Vector3.Lerp(a, c, (float)j / n) + Vector3.up * (h * 0.5f), new Vector3(0.08f, h, 0.08f), k);
                b.Member(a + Vector3.up * h, c + Vector3.up * h, 0.07f, k);
                b.Member(a + Vector3.up * (h * 0.5f), c + Vector3.up * (h * 0.5f), 0.04f, k);
            }
            if (pts.Count > 0) b.Box(pts[pts.Count - 1] + Vector3.up * (h * 0.5f), new Vector3(0.08f, h, 0.08f), k);
        }

        /// <summary>Stone balustrade (base rail, turned balusters, hand rail) from a to c.</summary>
        public static void Balustrade(MeshBuilder b, Vector3 a, Vector3 c, M k, float h = 1.05f, float spacing = 0.5f)
        {
            Vector3 d = c - a; d.y = 0f; float L = d.magnitude; if (L < 0.1f) return;
            Vector3 dir = d / L, side = Vector3.Cross(Vector3.up, dir), mid = (a + c) * 0.5f;
            b.BoxO(mid + Vector3.up * 0.08f, dir, Vector3.up, side, new Vector3(L, 0.16f, 0.32f), k);
            b.BoxO(mid + Vector3.up * (h - 0.07f), dir, Vector3.up, side, new Vector3(L, 0.14f, 0.36f), k);
            int n = Mathf.Max(1, Mathf.FloorToInt(L / spacing)); float hh = h - 0.3f;
            var prof = new[] { new Vector2(0.08f, 0f), new Vector2(0.05f, 0.12f * hh), new Vector2(0.1f, 0.42f * hh), new Vector2(0.05f, 0.75f * hh), new Vector2(0.08f, hh) };
            for (int i = 1; i < n; i++) b.Lathe(a + dir * (L * i / n) + Vector3.up * 0.16f, prof, 5, k);
        }

        // ---------- noise + faceted rock (same as the other LotteWorld modules) ----------
        public static float Noise3(Vector3 p)
        {
            float a = Mathf.PerlinNoise(p.x, p.y), c = Mathf.PerlinNoise(p.y + 31.7f, p.z), d = Mathf.PerlinNoise(p.z + 71.3f, p.x);
            return (a + c + d) / 1.5f - 1f;
        }
        public static float Fbm(Vector3 p)
        {
            return Noise3(p) * 0.6f + Noise3(p * 2.1f + Vector3.one * 5.2f) * 0.28f + Noise3(p * 4.3f + Vector3.one * 9.1f) * 0.12f;
        }
        public static void RockBlock(MeshBuilder b, Vector3 c, Vector3 size, float amp, float freq, Vector3 seedOff, M k, float cell = 1.6f)
        {
            Vector3 h = size * 0.5f;
            int nx = Mathf.Clamp(Mathf.RoundToInt(size.x / cell), 3, 16), ny = Mathf.Clamp(Mathf.RoundToInt(size.y / cell), 3, 16), nz = Mathf.Clamp(Mathf.RoundToInt(size.z / cell), 3, 16);
            Func<Vector3, Vector3> D = p =>
            {
                Vector3 dir = p - c; dir.x /= h.x; dir.y /= h.y; dir.z /= h.z; dir.Normalize();
                float e = Fbm((p + seedOff) * freq) * amp;
                float ridge = 1f - Mathf.Abs(Noise3((p - seedOff) * (freq * 0.7f)));
                e += (ridge * ridge - 0.45f) * amp * 0.9f;
                if (p.y <= c.y - h.y + 0.01f) { dir.y = 0f; e *= 0.5f; }
                return p + Vector3.Scale(dir, new Vector3(1f, 0.55f, 1f)) * e;
            };
            int[,] faces = { { 0, 1, 2, 1 }, { 0, -1, 2, 1 }, { 1, 1, 0, 2 }, { 1, -1, 0, 2 }, { 2, 1, 0, 1 }, { 2, -1, 0, 1 } };
            int[] cnt = { nx, ny, nz };
            for (int f = 0; f < 6; f++)
            {
                int ax = faces[f, 0], sg = faces[f, 1], ua = faces[f, 2], va = faces[f, 3];
                if (ax == 1 && sg == -1) continue;                                        // no bottom: rocks sit on the ground
                int nu = cnt[ua], nv = cnt[va];
                var grid = new Vector3[nu + 1, nv + 1];
                for (int i = 0; i <= nu; i++)
                    for (int j = 0; j <= nv; j++)
                    {
                        Vector3 p = c;
                        p[ax] += sg * h[ax];
                        p[ua] += -h[ua] + 2f * h[ua] * i / nu;
                        p[va] += -h[va] + 2f * h[va] * j / nv;
                        grid[i, j] = D(p);
                    }
                for (int i = 0; i < nu; i++)
                    for (int j = 0; j < nv; j++)
                    {
                        Vector3 a = grid[i, j], bb = grid[i + 1, j], cc = grid[i + 1, j + 1], dd = grid[i, j + 1];
                        b.Tri(a, bb, cc, k, (a + bb + cc) / 3f - c);
                        b.Tri(a, cc, dd, k, (a + cc + dd) / 3f - c);
                    }
            }
        }

        /// <summary>Round tower: shaft, cornice, blue cone roof and gold finial. Base centre b.</summary>
        public static void Turret(MeshBuilder b, Vector3 bs, float r, float h, float coneH, M wall, M roof, int seg = 12)
        {
            b.Cyl(bs, r, r, h, seg, wall);
            b.Cyl(bs + Vector3.up * (h - 0.6f), r + 0.25f, r + 0.25f, 0.6f, seg, M.CastlePink);
            b.Cyl(bs + Vector3.up * h, r + 0.35f, r + 0.35f, 0.2f, seg, M.Gold);
            b.Cone(bs + Vector3.up * (h + 0.2f), r + 0.45f, coneH, seg, roof);
            b.Cyl(bs + Vector3.up * (h + 0.2f + coneH * 0.92f), 0.06f, 0.06f, coneH * 0.25f + 0.8f, 6, M.Gold);
            b.Blob(bs + Vector3.up * (h + 0.2f + coneH * 0.98f), 0.18f, 0.18f, 6, M.Gold);
            // narrow window slits
            int nw = Mathf.Max(2, seg / 3);
            for (int i = 0; i < nw; i++)
            {
                float a = (360f * i / nw + 15f) * Mathf.Deg2Rad; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                b.BoxO(bs + d * (r + 0.01f) + Vector3.up * (h * 0.62f), Vector3.Cross(Vector3.up, d), Vector3.up, d, new Vector3(0.45f, 1.3f, 0.06f), M.Glass);
            }
        }

        /// <summary>Crenellated parapet along a rectangle top at height y.</summary>
        public static void Crenels(MeshBuilder b, float x0, float x1, float z0, float z1, float y, M k)
        {
            foreach (Face f in RectFaces(x0, x1, z0, z1))
            {
                b.PushFacade(f.Origin, f.Normal);
                b.BoxMM(-0.2f, y, -0.2f, f.Length + 0.2f, y + 0.6f, 0.4f, k);
                int n = Mathf.Max(2, Mathf.RoundToInt(f.Length / 1.4f));
                for (int i = 0; i < n; i += 2) b.BoxMM(f.Length * i / n - 0.1f, y + 0.6f, -0.2f, f.Length * (i + 1) / n - 0.1f, y + 1.3f, 0.4f, k);
                b.Pop();
            }
        }
    }

    // ------------------------------------------------------------------------------------------------------------------
    // Reusable prefabs (props, landscape, small buildings). Key = prefab file name.
    // ------------------------------------------------------------------------------------------------------------------
    public class PrefabDef
    {
        public string Key, Folder; public Func<Node> Build;
        public PrefabDef(string key, string folder, Func<Node> build) { Key = key; Folder = folder; Build = build; }
    }

    public static class Props
    {
        static Node One(string name, MeshBuilder b, bool collider = true) { var n = new Node(name); n.Add(new Node(name + "_Mesh", b) { Collider = collider }); return n; }

        public static Node StreetLamp()
        {
            var b = new MeshBuilder();
            b.Cyl(Vector3.zero, 0.28f, 0.22f, 0.5f, 8, M.RoofBlueDark);
            b.Lathe(new Vector3(0f, 0.5f, 0f), new[] { new Vector2(0.12f, 0f), new Vector2(0.09f, 1.2f), new Vector2(0.07f, 3.7f), new Vector2(0.11f, 3.85f), new Vector2(0f, 3.9f) }, 8, M.RoofBlueDark);
            b.Box(new Vector3(0f, 4.55f, 0f), new Vector3(0.42f, 0.04f, 0.42f), M.RoofBlueDark);
            b.Box(new Vector3(0f, 4.6f, 0f), new Vector3(0.32f, 0.5f, 0.32f), M.LampGlow);
            foreach (Vector3 c in new[] { new Vector3(-0.17f, 0f, -0.17f), new Vector3(0.17f, 0f, -0.17f), new Vector3(0.17f, 0f, 0.17f), new Vector3(-0.17f, 0f, 0.17f) })
                b.Box(c + Vector3.up * 4.6f, new Vector3(0.04f, 0.55f, 0.04f), M.RoofBlueDark);
            b.Cone(new Vector3(0f, 4.86f, 0f), 0.3f, 0.35f, 4, M.RoofBlueDark, 45f);
            b.Member(new Vector3(0f, 3.9f, 0f), new Vector3(0f, 4.3f, 0f), 0.08f, M.RoofBlueDark);
            Node n = One("StreetLamp", b);
            n.Add(new Node("LampLight") { Pos = new Vector3(0f, 4.6f, 0f), IsStatic = true, Light = new LightSpec { Range = 10f, Intensity = 1.2f, Enabled = false } });
            return n;
        }

        public static Node Bench()
        {
            var b = new MeshBuilder();
            for (int i = 0; i < 4; i++) b.BoxMM(-0.9f, 0.42f, -0.22f + i * 0.12f, 0.9f, 0.46f, -0.13f + i * 0.12f, M.Wood);
            for (int i = 0; i < 3; i++) b.Box(new Vector3(0f, 0.6f + i * 0.15f, 0.26f), new Vector3(1.8f, 0.09f, 0.04f), M.Wood);
            foreach (float x in new[] { -0.8f, 0.8f })
            {
                b.BoxMM(x - 0.04f, 0f, -0.22f, x + 0.04f, 0.42f, -0.16f, M.RoofBlueDark);
                b.BoxMM(x - 0.04f, 0f, 0.22f, x + 0.04f, 0.95f, 0.28f, M.RoofBlueDark);
                b.BoxMM(x - 0.04f, 0.62f, -0.24f, x + 0.04f, 0.66f, 0.24f, M.RoofBlueDark);
            }
            return One("Bench", b);
        }

        public static Node TrashBin()
        {
            var b = new MeshBuilder();
            b.Cyl(Vector3.zero, 0.3f, 0.3f, 0.85f, 10, M.RoofBlueDark);
            b.Cyl(new Vector3(0f, 0.85f, 0f), 0.34f, 0.2f, 0.18f, 10, M.Gold);
            return One("TrashBin", b);
        }

        public static Node Tree(string name, M leaf, float h, float spread, int seed)
        {
            var r = new System.Random(seed);
            var trunk = new MeshBuilder();
            trunk.Cyl(Vector3.zero, 0.28f, 0.16f, h * 0.55f, 7, M.Bark);
            var crown = new MeshBuilder();
            for (int i = 0; i < 3; i++)
            {
                float a = (float)r.NextDouble() * 6.28f;
                Vector3 bo = new Vector3(Mathf.Cos(a) * 0.4f, h * 0.4f, Mathf.Sin(a) * 0.4f);
                trunk.Member(bo, bo + new Vector3(Mathf.Cos(a) * spread * 0.35f, h * 0.18f, Mathf.Sin(a) * spread * 0.35f), 0.14f, M.Bark);
            }
            int nb = 5;
            for (int i = 0; i < nb; i++)
            {
                float a = 6.28f * i / nb + (float)r.NextDouble() * 0.6f, d = i == 0 ? 0f : spread * (0.35f + 0.2f * (float)r.NextDouble());
                float s = spread * (0.55f + 0.2f * (float)r.NextDouble());
                crown.Blob(new Vector3(Mathf.Cos(a) * d, h * (0.62f + 0.12f * (float)r.NextDouble()), Mathf.Sin(a) * d), s, s * 0.8f, 7, leaf, (float)r.NextDouble() * 60f);
            }
            var n = new Node(name);
            n.Add(new Node("Trunk", trunk));
            n.Add(new Node("Crown", crown) { Collider = false });
            return n;
        }

        public static Node Pine()
        {
            var trunk = new MeshBuilder(); trunk.Cyl(Vector3.zero, 0.25f, 0.15f, 3f, 7, M.Bark);
            var crown = new MeshBuilder();
            for (int i = 0; i < 3; i++) crown.Cone(new Vector3(0f, 1.6f + i * 1.9f, 0f), 2.3f - i * 0.6f, 3.6f, 9, M.LeafDark, i * 20f);
            var n = new Node("Tree_Pine");
            n.Add(new Node("Trunk", trunk)); n.Add(new Node("Crown", crown) { Collider = false });
            return n;
        }

        public static Node Hedge()
        {
            var b = new MeshBuilder();
            b.BoxMM(-2f, 0f, -0.55f, 2f, 1.0f, 0.55f, M.LeafDark);
            b.BoxMM(-1.95f, 1.0f, -0.5f, 1.95f, 1.15f, 0.5f, M.Leaf);
            return One("Hedge_4m", b);
        }

        public static Node FlowerBed()
        {
            var b = new MeshBuilder();
            b.Cyl(Vector3.zero, 2.2f, 2.2f, 0.45f, 16, M.StoneGrey);
            b.Cyl(new Vector3(0f, 0.45f, 0f), 2.0f, 2.0f, 0.05f, 16, M.LakeBed);
            var r = new System.Random(7);
            for (int i = 0; i < 18; i++)
            {
                float a = (float)r.NextDouble() * 6.28f, d = 0.3f + (float)r.NextDouble() * 1.5f;
                b.Blob(new Vector3(Mathf.Cos(a) * d, 0.5f, Mathf.Sin(a) * d), 0.3f, 0.22f, 6, i % 3 == 0 ? M.Flower : i % 3 == 1 ? M.Yellow : M.Leaf);
            }
            return One("FlowerBed", b);
        }

        public static Node ParasolSet()
        {
            var b = new MeshBuilder();
            b.Cyl(Vector3.zero, 0.04f, 0.04f, 2.5f, 6, M.White);
            b.Cyl(new Vector3(0f, 0.72f, 0f), 0.5f, 0.5f, 0.05f, 12, M.White);
            b.Cyl(Vector3.zero, 0.3f, 0.3f, 0.05f, 8, M.Metal);
            for (int i = 0; i < 8; i++)
            {
                float a0 = Mathf.PI * 2f * i / 8f, a1 = Mathf.PI * 2f * (i + 1) / 8f;
                b.Tri(new Vector3(0f, 2.75f, 0f), new Vector3(Mathf.Cos(a0) * 1.6f, 2.25f, Mathf.Sin(a0) * 1.6f), new Vector3(Mathf.Cos(a1) * 1.6f, 2.25f, Mathf.Sin(a1) * 1.6f), i % 2 == 0 ? M.Red : M.White, Vector3.up);
                b.Tri(new Vector3(0f, 2.73f, 0f), new Vector3(Mathf.Cos(a0) * 1.6f, 2.23f, Mathf.Sin(a0) * 1.6f), new Vector3(Mathf.Cos(a1) * 1.6f, 2.23f, Mathf.Sin(a1) * 1.6f), i % 2 == 0 ? M.Red : M.White, Vector3.down);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.5f * i; Vector3 c = new Vector3(Mathf.Cos(a) * 0.95f, 0f, Mathf.Sin(a) * 0.95f);
                b.Cyl(c, 0.2f, 0.2f, 0.45f, 8, M.White);
            }
            return One("ParasolSet", b);
        }

        /// <summary>Blue cone turret on a square plinth (bridge piers, gate towers).</summary>
        public static Node ConeTurret(string name, float r, float h, float coneH)
        {
            var b = new MeshBuilder();
            b.BoxMM(-r - 0.4f, 0f, -r - 0.4f, r + 0.4f, 1.0f, r + 0.4f, M.StoneGrey);
            Lib.Turret(b, new Vector3(0f, 1.0f, 0f), r, h - 1.0f, coneH, M.CastleCream, M.RoofBlue);
            return One(name, b);
        }

        public static Node Fountain()
        {
            var b = new MeshBuilder();
            b.Cone(new Vector3(0f, -1.2f, 0f), 1.4f, 14f, 10, M.Foam);
            b.Cyl(new Vector3(0f, -1.25f, 0f), 4.5f, 3.5f, 0.6f, 14, M.Foam);
            var r = new System.Random(3);
            for (int i = 0; i < 10; i++) { float a = 0.628f * i; b.Blob(new Vector3(Mathf.Cos(a) * 3f, -0.6f + (float)r.NextDouble() * 0.4f, Mathf.Sin(a) * 3f), 0.8f, 0.5f, 6, M.Foam); }
            var n = new Node("LakeFountain"); n.Add(new Node("Jet", b) { Collider = false });
            return n;
        }

        public static Node MoonBoat()
        {
            var b = new MeshBuilder();
            var pts = new List<Vector3>();
            for (int i = 0; i <= 12; i++) { float a = Mathf.PI * (0.1f + 0.8f * i / 12f); pts.Add(new Vector3(-Mathf.Cos(a) * 2.2f, 1.6f - Mathf.Sin(a) * 1.5f, 0f)); }
            for (int i = 0; i < 12; i++)
            {
                float th = 0.55f + 0.35f * Mathf.Sin(Mathf.PI * (i + 0.5f) / 12f);
                b.BoxO((pts[i] + pts[i + 1]) * 0.5f, (pts[i + 1] - pts[i]).normalized, Vector3.Cross(Vector3.forward, (pts[i + 1] - pts[i]).normalized), Vector3.forward, new Vector3((pts[i + 1] - pts[i]).magnitude + 0.05f, th, 1.4f), M.Yellow);
            }
            b.BoxMM(-1.0f, 0.2f, -0.6f, 1.0f, 0.55f, 0.6f, M.White);
            var n = new Node("Boat_Moon"); n.Add(new Node("Hull", b) { IsStatic = false });
            return n;
        }

        /// <summary>Food kiosk: counter window with a striped awning at the front, side door into a small store room.</summary>
        public static Node Kiosk(string name, M wall, M stripe, M roof)
        {
            var b = new MeshBuilder();
            float x0 = -3f, x1 = 3f, z0 = -2.2f, z1 = 2.2f, H = 3.2f;
            // front wall with a serving hatch, back and right walls, left wall with the door
            b.Begin("Walls");
            Lib.WallWithHole(b, x0, x1, 0f, H, z0, z0 + 0.25f, 0f, 3.6f, 1.2f, false, wall);
            b.BoxMM(-1.8f, 0f, z0, 1.8f, 1.05f, z0 + 0.25f, wall);
            b.BoxMM(x0, 0f, z1 - 0.25f, x1, H, z1, wall);
            b.BoxMM(x1 - 0.25f, 0f, z0 + 0.25f, x1, H, z1 - 0.25f, wall);
            b.BoxMM(x0, 0f, z0 + 0.25f, x0 + 0.25f, H, -0.45f, wall);
            b.BoxMM(x0, 2.2f, -0.45f, x0 + 0.25f, H, 0.75f, wall);
            b.BoxMM(x0, 0f, 0.75f, x0 + 0.25f, H, z1 - 0.25f, wall);
            b.Begin("Counter");
            b.BoxMM(-1.85f, 1.05f, z0 - 0.35f, 1.85f, 1.12f, z0 + 0.1f, M.Wood);                 // counter
            b.BoxMM(x0 + 0.25f, 0.9f, z0 + 0.25f, x1 - 0.25f, 0.95f, z0 + 0.8f, M.Metal);      // inner work top
            b.Begin("Floor_Ceiling");
            Lib.TileFloor(b, x0 + 0.25f, x1 - 0.25f, z0 + 0.25f, z1 - 0.25f, 0f, 1.0f, M.White, M.StoneGrey);
            b.BoxMM(x0 + 0.25f, H - 0.15f, z0 + 0.25f, x1 - 0.25f, H, z1 - 0.25f, M.White);    // ceiling
            b.Begin("Roof");
            Lib.HipRoof(b, x0, x1, z0, z1, H, 1.6f, 0.5f, roof, M.White);
            b.Begin("Awning_Sign");
            b.PushFacade(new Vector3(x0, 0f, z0), Vector3.back);
            for (int i = 0; i < 8; i++)                                                         // striped awning
            {
                float u0 = 1.1f + i * 0.475f;
                b.BoxO(new Vector3(u0 + 0.237f, 2.75f, -0.6f), Vector3.right, new Vector3(0f, 0.9f, -0.44f).normalized, new Vector3(0f, -0.44f, -0.9f).normalized, new Vector3(0.475f, 0.04f, 1.35f), i % 2 == 0 ? stripe : M.White);
            }
            b.BoxMM(1.4f, 3.35f, -0.08f, 4.6f, 4.1f, 0f, M.White);                               // blank sign board
            b.Pop();
            var n = new Node(name);
            Furn.Kiosk(b, n, "Store_", new Room(x0 + 0.25f, x1 - 0.25f, z0 + 0.85f, z1 - 0.25f, H - 0.15f, 99f), name.Length * 13);
            n.Add(new Node(name + "_Body", b));
            n.Add(Lib.DoorLeaf("Door_" + name, new Vector3(x0 + 0.125f, 0f, -0.42f), 1.15f, 2.15f, M.Wood, false)).Euler = new Vector3(0f, -90f, 0f);   // leaf runs +Z along the left wall
            return n;
        }

        /// <summary>Restroom block: two open doorways (screen walls inside), tiled rooms, flat roof with a parapet.</summary>
        public static Node Restroom()
        {
            var b = new MeshBuilder();
            float x0 = -4.5f, x1 = 4.5f, z0 = -3f, z1 = 3f, H = 3.4f, t = 0.25f;
            b.Begin("Walls");
            Lib2.Openings(b, x0, x1, 0f, H, z0, z0 + t, new[] { -2.2f, 2.2f }, new[] { 1.4f, 1.4f }, new[] { 0f, 0f }, new[] { 2.3f, 2.3f }, M.Plaster);
            b.BoxMM(x0, 0f, z1 - t, x1, H, z1, M.Plaster);
            b.BoxMM(x0, 0f, z0 + t, x0 + t, H, z1 - t, M.Plaster);
            b.BoxMM(x1 - t, 0f, z0 + t, x1, H, z1 - t, M.Plaster);
            b.BoxMM(-0.12f, 0f, z0 + t, 0.12f, H, z1 - t, M.Plaster);                            // middle wall
            b.BoxMM(-3.4f, 0f, -1.4f, -1.0f, 2.2f, -1.25f, M.Plaster);                           // privacy screens
            b.BoxMM(1.0f, 0f, -1.4f, 3.4f, 2.2f, -1.25f, M.Plaster);
            b.Begin("Floor");
            Lib.TileFloor(b, x0 + t, x1 - t, z0 + t, z1 - t, 0f, 0.8f, M.White, M.PaleBlue);
            b.Begin("Roof_Sign");
            b.BoxMM(x0, H, z0, x1, H + 0.3f, z1, M.StoneGrey);
            b.BoxMM(x0 - 0.1f, 0f, z0 - 0.1f, x1 + 0.1f, 0.5f, z0, M.RoofBlueDark);
            b.BoxMM(-1f, H + 0.3f, z0 - 0.06f, 1f, H + 1.1f, z0, M.White);                       // blank sign
            Lib.Bulb(b, new Vector3(-2.2f, H, 0.5f)); Lib.Bulb(b, new Vector3(2.2f, H, 0.5f));
            var n = new Node("Restroom");
            Furn.Toilets(b, n, "Left_", new Room(x0 + t, -0.12f, z0 + t, z1 - t, H, -2.2f), false);
            Furn.Toilets(b, n, "Right_", new Room(0.12f, x1 - t, z0 + t, z1 - t, H, 2.2f), true);
            n.Add(new Node("Restroom_Body", b));
            return n;
        }
    }

    /// <summary>Wall-with-openings helper (same as Lib2.Openings in the NorthGate module).</summary>
    public static class Lib2
    {
        public static void Openings(MeshBuilder b, float x0, float x1, float y0, float y1, float z0, float z1, float[] cx, float[] w, float[] sill, float[] top, M k)
        {
            float left = x0;
            for (int i = 0; i < cx.Length; i++)
            {
                float hx0 = cx[i] - w[i] * 0.5f, hx1 = cx[i] + w[i] * 0.5f;
                b.BoxMM(left, y0, z0, hx0, y1, z1, k);
                b.BoxMM(hx0, y0, z0, hx1, sill[i], z1, k);
                b.BoxMM(hx0, top[i], z0, hx1, y1, z1, k);
                left = hx1;
            }
            b.BoxMM(left, y0, z0, x1, y1, z1, k);
        }
    }
}
#endif
