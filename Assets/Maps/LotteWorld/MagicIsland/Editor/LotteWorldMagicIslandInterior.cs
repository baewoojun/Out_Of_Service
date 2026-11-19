// LotteWorldMagicIslandInterior.cs  (v1 - Magic Island: furniture + room fit-outs for every walk-in room)
// Every piece of furniture is its own named part (MeshBuilder.Begin) -> its own GameObject, so each table, chair, shelf, rail
// can be moved or deleted on its own. Rooms are given as their inner floor rectangle (x0..x1, z0..z1) in the building's local
// frame, door on the front wall (z0) at doorX; nothing is placed in a 2 m corridor in front of the door.
// Abandoned-park look: a few toppled chairs, scattered papers, wet-floor signs, empty shelf gaps, some bulbs off.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotteWorld.MagicIsland
{
    public struct Room
    {
        public float x0, x1, z0, z1, rh, doorX;
        public Room(float x0, float x1, float z0, float z1, float rh, float doorX) { this.x0 = x0; this.x1 = x1; this.z0 = z0; this.z1 = z1; this.rh = rh; this.doorX = doorX; }
        public float W { get { return x1 - x0; } }
        public float D { get { return z1 - z0; } }
        public Vector3 Centre(float y) { return new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f); }
        /// <summary>True if (x, z) +- pad stays clear of the door corridor (2.2 m wide, 2.2 m deep).</summary>
        public bool ClearOfDoor(float x, float z, float pad) { return !(Mathf.Abs(x - doorX) < 1.1f + pad && z < z0 + 2.2f + pad); }
    }

    public static class Furn
    {
        static readonly M[] Goods = { M.Red, M.Yellow, M.RoofBlue, M.Pink, M.Mint, M.Purple, M.Orange, M.White, M.Teal };
        static float Rf(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

        // ---------------- single pieces (each call = one named part) ----------------
        public static void Chair(MeshBuilder b, string name, float x, float z, float yaw, bool toppled, M seat)
        {
            b.Begin(name);
            b.Push(new Vector3(x, toppled ? 0.24f : 0f, z), Quaternion.Euler(0f, yaw, 0f) * (toppled ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity));
            b.BoxMM(-0.22f, 0.44f, -0.22f, 0.22f, 0.48f, 0.22f, seat);
            b.BoxMM(-0.22f, 0.48f, 0.18f, 0.22f, 0.92f, 0.22f, seat);
            foreach (float lx in new[] { -0.19f, 0.19f }) foreach (float lz in new[] { -0.19f, 0.19f }) b.BoxMM(lx - 0.02f, 0f, lz - 0.02f, lx + 0.02f, 0.44f, lz + 0.02f, M.Metal);
            b.Pop();
        }

        public static void RoundTable(MeshBuilder b, string name, float x, float z, float r, M top)
        {
            b.Begin(name);
            b.Cyl(new Vector3(x, 0.72f, z), r, r, 0.04f, 14, top);
            b.Cyl(new Vector3(x, 0f, z), 0.05f, 0.05f, 0.72f, 6, M.Metal);
            b.Cyl(new Vector3(x, 0f, z), 0.28f, 0.22f, 0.04f, 10, M.Metal);
        }

        /// <summary>Table + 4 chairs (separate parts). Optionally one chair knocked over, plates and a cup on the table.</summary>
        public static void TableSet(MeshBuilder b, string name, float x, float z, System.Random r, bool topple, M chairCol)
        {
            RoundTable(b, name + "_Table", x, z, 0.5f, M.Wood);
            b.Begin(name + "_Table");
            b.Cyl(new Vector3(x + 0.18f, 0.76f, z - 0.12f), 0.12f, 0.12f, 0.015f, 10, M.White);
            if (r.NextDouble() < 0.6) b.Cyl(new Vector3(x - 0.15f, 0.76f, z + 0.15f), 0.04f, 0.045f, 0.1f, 8, Goods[r.Next(Goods.Length)]);
            int tk = topple ? r.Next(4) : -1;
            for (int k = 0; k < 4; k++)
            {
                float a = k * 90f + Rf(r, -15f, 15f), d = 0.85f + Rf(r, -0.08f, 0.12f);
                float cx = x + Mathf.Sin(a * Mathf.Deg2Rad) * d, cz = z + Mathf.Cos(a * Mathf.Deg2Rad) * d;
                Chair(b, name + "_Chair" + (k + 1), cx, cz, a + 180f + Rf(r, -20f, 20f), k == tk, chairCol);
            }
        }

        /// <summary>Shop / bar counter along X (customer side -Z): body, top, till, glass display case.</summary>
        public static void Counter(MeshBuilder b, string name, float x0, float x1, float z, float yaw, M body)
        {
            b.Begin(name);
            float cx = (x0 + x1) * 0.5f, L = x1 - x0;
            b.Push(new Vector3(cx, 0f, z), yaw);
            b.BoxMM(-L * 0.5f, 0f, -0.35f, L * 0.5f, 0.98f, 0.35f, body);
            b.BoxMM(-L * 0.5f - 0.05f, 0.98f, -0.42f, L * 0.5f + 0.05f, 1.03f, 0.4f, M.StoneGrey);
            b.BoxMM(-L * 0.5f + 0.3f, 1.03f, -0.1f, -L * 0.5f + 0.75f, 1.25f, 0.25f, M.Opening);                  // till
            b.BoxMM(-L * 0.5f + 0.35f, 1.25f, 0.05f, -L * 0.5f + 0.7f, 1.45f, 0.2f, M.Metal);
            if (L > 2.4f)
            {
                b.BoxMM(0.2f, 1.03f, -0.3f, Mathf.Min(L * 0.5f - 0.2f, 1.9f), 1.5f, 0.3f, M.Glass);                 // display case
                b.BoxMM(0.2f, 1.5f, -0.32f, Mathf.Min(L * 0.5f - 0.2f, 1.9f), 1.53f, 0.32f, M.Gold);
            }
            b.Pop();
        }

        /// <summary>Wall shelf from (ax, az) to (bx, bz), goods facing 'face' (unit (x, z)). Random goods, some empty gaps.</summary>
        public static void Shelf(MeshBuilder b, string name, float ax, float az, float bx, float bz, Vector2 face, float H, System.Random r)
        {
            b.Begin(name);
            Vector3 p0 = new Vector3(ax, 0f, az), p1 = new Vector3(bx, 0f, bz), u = (p1 - p0).normalized, f = new Vector3(face.x, 0f, face.y);
            float L = (p1 - p0).magnitude, depth = 0.45f; int rows = 4;
            Vector3 s = (p0 + p1) * 0.5f + f * (depth * 0.5f);
            b.BoxO(s + Vector3.up * (H * 0.5f) - f * (depth * 0.5f - 0.02f), u, Vector3.up, f, new Vector3(L, H, 0.04f), M.Wood);     // back panel
            for (int i = 0; i <= rows; i++)
            {
                float y = 0.1f + i * (H - 0.15f) / rows;
                b.BoxO(s + Vector3.up * y, u, Vector3.up, f, new Vector3(L, i == 0 ? 0.2f : 0.03f, depth), M.Wood);
                if (i == rows) break;
                float t = 0.1f;
                while (t < L - 0.2f)
                {
                    float w = Rf(r, 0.15f, 0.35f), h = Rf(r, 0.12f, (H - 0.15f) / rows - 0.1f);
                    if (r.NextDouble() < 0.18) { t += w + Rf(r, 0.1f, 0.5f); continue; }                                       // empty gap
                    Vector3 q = p0 + u * (t + w * 0.5f) + f * Rf(r, 0.12f, depth - 0.12f) + Vector3.up * (y + 0.015f + h * 0.5f);
                    if (r.NextDouble() < 0.7) b.BoxO(q, u, Vector3.up, f, new Vector3(w, h, Rf(r, 0.12f, 0.28f)), Goods[r.Next(Goods.Length)]);
                    else b.Cyl(q - Vector3.up * (h * 0.5f), w * 0.35f, w * 0.35f, h, 8, Goods[r.Next(Goods.Length)]);
                    t += w + Rf(r, 0.02f, 0.08f);
                }
            }
            foreach (float e in new[] { 0f, L }) b.BoxO(p0 + u * e + f * (depth * 0.5f) + Vector3.up * (H * 0.5f), u, Vector3.up, f, new Vector3(0.04f, H, depth), M.Wood);
        }

        /// <summary>Low display table with stacked goods.</summary>
        public static void DisplayTable(MeshBuilder b, string name, float x, float z, float w, float d, System.Random r)
        {
            b.Begin(name);
            b.BoxMM(x - w * 0.5f, 0f, z - d * 0.5f, x + w * 0.5f, 0.8f, z + d * 0.5f, M.Wood);
            b.BoxMM(x - w * 0.5f - 0.04f, 0.8f, z - d * 0.5f - 0.04f, x + w * 0.5f + 0.04f, 0.84f, z + d * 0.5f + 0.04f, M.White);
            for (int i = 0; i < 12; i++)
            {
                float px = x + Rf(r, -w * 0.4f, w * 0.4f), pz = z + Rf(r, -d * 0.4f, d * 0.4f), s = Rf(r, 0.15f, 0.3f);
                if (r.NextDouble() < 0.5) b.Box(new Vector3(px, 0.84f + s * 0.4f, pz), new Vector3(s, s * 0.8f, s), Goods[r.Next(Goods.Length)]);
                else b.Blob(new Vector3(px, 0.84f + s * 0.5f, pz), s * 0.5f, s * 0.5f, 7, Goods[r.Next(Goods.Length)]);   // plush toy
            }
        }

        public static void ClothesRack(MeshBuilder b, string name, float x, float z, System.Random r)
        {
            b.Begin(name);
            b.Cyl(new Vector3(x, 0f, z), 0.3f, 0.3f, 0.05f, 10, M.Metal);
            b.Cyl(new Vector3(x, 0.05f, z), 0.03f, 0.03f, 1.55f, 6, M.Metal);
            b.Cyl(new Vector3(x, 1.5f, z), 0.55f, 0.55f, 0.03f, 14, M.Metal);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                b.BoxO(new Vector3(x, 1.12f, z) + d * 0.55f, Vector3.Cross(Vector3.up, d), Vector3.up, d, new Vector3(0.45f, 0.7f, 0.04f), Goods[r.Next(Goods.Length)]);
            }
        }

        /// <summary>Hanging lamp (cord + shade + bulb), bulb lit or dead.</summary>
        public static void Pendant(MeshBuilder b, string name, float x, float z, float ceiling, bool lit)
        {
            b.Begin(name);
            b.Box(new Vector3(x, ceiling - 0.4f, z), new Vector3(0.02f, 0.8f, 0.02f), M.Opening);
            b.Cone(new Vector3(x, ceiling - 0.95f, z), 0.28f, 0.22f, 10, M.Teal);
            b.Blob(new Vector3(x, ceiling - 1.0f, z), 0.07f, 0.07f, 6, lit ? M.LampGlow : M.Glass);
        }

        public static void WetSign(MeshBuilder b, string name, float x, float z, float yaw)
        {
            b.Begin(name);
            b.Push(new Vector3(x, 0f, z), yaw);
            b.BoxO(new Vector3(0f, 0.31f, -0.12f), Vector3.right, new Vector3(0f, 0.95f, -0.3f).normalized, new Vector3(0f, 0.3f, 0.95f).normalized, new Vector3(0.3f, 0.64f, 0.02f), M.Yellow);
            b.BoxO(new Vector3(0f, 0.31f, 0.12f), Vector3.right, new Vector3(0f, 0.95f, 0.3f).normalized, new Vector3(0f, -0.3f, 0.95f).normalized, new Vector3(0.3f, 0.64f, 0.02f), M.Yellow);
            b.Pop();
        }

        /// <summary>Scattered papers / flyers on the floor (one part).</summary>
        public static void Litter(MeshBuilder b, string name, Room rm, int n, System.Random r)
        {
            b.Begin(name);
            for (int i = 0; i < n; i++)
            {
                float x = Rf(r, rm.x0 + 0.3f, rm.x1 - 0.3f), z = Rf(r, rm.z0 + 0.3f, rm.z1 - 0.3f);
                b.Push(new Vector3(x, 0.045f, z), Rf(r, 0f, 180f));
                b.BoxMM(-Rf(r, 0.06f, 0.14f), 0f, -Rf(r, 0.05f, 0.11f), Rf(r, 0.06f, 0.14f), 0.004f, Rf(r, 0.05f, 0.11f), r.NextDouble() < 0.7 ? M.White : M.Yellow);
                b.Pop();
            }
        }

        /// <summary>Blank board on a wall (menu / poster), facade frame given by origin and outward normal.</summary>
        public static void WallBoard(MeshBuilder b, string name, Vector3 at, Vector3 outward, float w, float h, M board)
        {
            b.Begin(name);
            b.PushFacade(at, outward);
            b.BoxMM(-w * 0.5f - 0.06f, -h * 0.5f - 0.06f, -0.05f, w * 0.5f + 0.06f, h * 0.5f + 0.06f, 0f, M.Gold);
            b.BoxMM(-w * 0.5f, -h * 0.5f, -0.07f, w * 0.5f, h * 0.5f, -0.05f, board);
            b.Pop();
        }

        /// <summary>Zig-zag queue rails across the room (posts + two rails), rows along X.</summary>
        public static void QueueRails(MeshBuilder b, string name, Room rm, float zStart, int rows, float gap)
        {
            b.Begin(name);
            float xa = rm.x0 + 1.4f, xb = rm.x1 - 1.4f;
            for (int i = 0; i < rows; i++)
            {
                float z = zStart + i * gap; if (z > rm.z1 - 0.8f) break;
                bool left = i % 2 == 0;
                float s = left ? xa : xa + 1.2f, e = left ? xb - 1.2f : xb;
                Lib.Railing(b, new List<Vector3> { new Vector3(s, 0f, z), new Vector3(e, 0f, z) }, M.Gold, 1.0f);
            }
        }

        public static void Turnstile(MeshBuilder b, string name, float x, float z)
        {
            b.Begin(name);
            b.BoxMM(x - 0.15f, 0f, z - 0.45f, x + 0.15f, 1.0f, z + 0.45f, M.Metal);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                b.Member(new Vector3(x + 0.15f, 0.85f, z), new Vector3(x + 0.15f + 0.55f * Mathf.Cos(a) * 0.3f + 0.4f, 0.85f + 0.4f * Mathf.Sin(a), z), 0.04f, M.Metal);
            }
        }

        public static void Fridge(MeshBuilder b, string name, float x, float z, float yaw, System.Random r)
        {
            b.Begin(name);
            b.Push(new Vector3(x, 0f, z), yaw);
            b.BoxMM(-0.4f, 0f, -0.35f, 0.4f, 1.9f, 0.35f, M.White);
            b.BoxMM(-0.34f, 0.15f, -0.37f, 0.34f, 1.8f, -0.35f, M.Glass);
            for (int i = 0; i < 4; i++) for (int k = 0; k < 4; k++)
                    b.Cyl(new Vector3(-0.25f + k * 0.17f, 0.25f + i * 0.4f, -0.15f), 0.04f, 0.04f, 0.2f, 6, Goods[r.Next(Goods.Length)]);
            b.Pop();
        }

        public static void Crates(MeshBuilder b, string name, float x, float z, System.Random r)
        {
            b.Begin(name);
            int n = 2 + r.Next(3);
            for (int i = 0; i < n; i++)
            {
                float s = Rf(r, 0.4f, 0.6f);
                b.Push(new Vector3(x + Rf(r, -0.3f, 0.3f), i < 2 ? 0f : s, z + Rf(r, -0.3f, 0.3f)), Rf(r, -20f, 20f));
                b.BoxMM(-s * 0.5f, 0f, -s * 0.5f, s * 0.5f, s, s * 0.5f, M.Timber);
                b.Pop();
            }
        }

        // ---------------- room fit-outs ----------------
        /// <summary>Gift / goods shop: shelves on the side and back walls, display tables, counter, clothes rack, pendants.</summary>
        public static void Shop(MeshBuilder b, Node room, string p, Room rm, int seed)
        {
            var r = new System.Random(seed);
            Shelf(b, p + "Shelf_West", rm.x0 + 0.02f, rm.z0 + 1.2f, rm.x0 + 0.02f, rm.z1 - 0.6f, new Vector2(1f, 0f), 2.1f, r);
            Shelf(b, p + "Shelf_East", rm.x1 - 0.02f, rm.z1 - 0.6f, rm.x1 - 0.02f, rm.z0 + 1.2f, new Vector2(-1f, 0f), 2.1f, r);
            if (rm.W > 4f) Shelf(b, p + "Shelf_Back", rm.x0 + 0.8f, rm.z1 - 0.02f, rm.x1 - 2.4f, rm.z1 - 0.02f, new Vector2(0f, -1f), 2.1f, r);
            if (rm.W > 3.2f) Counter(b, p + "Counter", rm.x1 - 2.2f, rm.x1 - 0.7f, rm.z1 - 0.9f, 90f, M.Wood);
            float cx = (rm.x0 + rm.x1) * 0.5f, cz = (rm.z0 + rm.z1) * 0.5f + 0.4f;
            if (rm.W > 5f && rm.D > 4.5f) { DisplayTable(b, p + "Display_1", cx - rm.W * 0.18f, cz, 1.6f, 1.0f, r); DisplayTable(b, p + "Display_2", cx + rm.W * 0.18f, cz, 1.6f, 1.0f, r); }
            else if (rm.D > 3.5f) DisplayTable(b, p + "Display_1", cx, cz, 1.4f, 0.9f, r);
            if (rm.W > 7f) ClothesRack(b, p + "ClothesRack", rm.x0 + 1.6f, rm.z1 - 1.8f, r);
            Pendants(b, p, rm, 2, r);
            Litter(b, p + "Litter", rm, 10, r);
            AddLight(room, p, rm);
        }

        /// <summary>Restaurant / food court: counter along the back wall, menu board, table sets, pendants, a wet-floor sign.</summary>
        public static void Dining(MeshBuilder b, Node room, string p, Room rm, int seed, M chairCol)
        {
            var r = new System.Random(seed);
            float cl = Mathf.Min(rm.W - 1.2f, 6f), cxm = rm.x1 - 0.6f - cl * 0.5f;
            Counter(b, p + "Counter", cxm - cl * 0.5f, cxm + cl * 0.5f, rm.z1 - 1.3f, 0f, M.Teal);
            WallBoard(b, p + "MenuBoard", new Vector3(cxm, rm.rh * 0.62f + 0.6f, rm.z1), Vector3.back, Mathf.Min(cl, 3.6f), 0.9f, M.Opening);
            if (rm.W > 4f) Fridge(b, p + "Fridge", rm.x0 + 0.5f, rm.z1 - 0.45f, 180f, r);
            int k = 0;
            for (float z = rm.z0 + 1.6f; z < rm.z1 - 2.6f; z += 2.4f)
                for (float x = rm.x0 + 1.3f; x < rm.x1 - 1.1f; x += 2.5f)
                {
                    if (!rm.ClearOfDoor(x, z, 0.9f)) continue;
                    TableSet(b, p + "TableSet_" + (++k).ToString("00"), x, z, r, r.NextDouble() < 0.3, chairCol);
                }
            if (k == 0 && rm.D > 3f) TableSet(b, p + "TableSet_01", rm.x0 + 1.2f, rm.z0 + rm.D * 0.5f, r, true, chairCol);
            WetSign(b, p + "WetFloorSign", rm.doorX + 1.6f, rm.z0 + 2.4f, 30f);
            Pendants(b, p, rm, 3, r);
            Litter(b, p + "Litter", rm, 12, r);
            AddLight(room, p, rm);
        }

        /// <summary>Ride queue lobby: zig-zag rails, a turnstile at the far end, blank posters, dim lamps.</summary>
        public static void Queue(MeshBuilder b, Node room, string p, Room rm, int seed)
        {
            var r = new System.Random(seed);
            QueueRails(b, p + "QueueRails", rm, rm.z0 + 2.6f, 4, 1.3f);
            Turnstile(b, p + "Turnstile", rm.x1 - 0.9f, rm.z1 - 0.8f);
            for (int i = 0; i < Mathf.Min(4, (int)(rm.W / 3f)); i++)
                WallBoard(b, p + "Poster_" + (i + 1), new Vector3(rm.x0 + 1.5f + i * 3f, 1.8f, rm.z1), Vector3.back, 1.0f, 1.4f, Goods[r.Next(Goods.Length)]);
            Crates(b, p + "Crates", rm.x0 + 0.6f, rm.z1 - 0.6f, r);
            Pendants(b, p, rm, 2, r);
            Litter(b, p + "Litter", rm, 8, r);
            AddLight(room, p, rm);
        }

        /// <summary>Kiosk back room: shelf on the back wall, fridge, crates.</summary>
        public static void Kiosk(MeshBuilder b, Node room, string p, Room rm, int seed)
        {
            var r = new System.Random(seed);
            Shelf(b, p + "Shelf_Back", rm.x0 + 1.0f, rm.z1 - 0.02f, rm.x1 - 1.2f, rm.z1 - 0.02f, new Vector2(0f, -1f), 1.9f, r);
            Fridge(b, p + "Fridge", rm.x1 - 0.5f, rm.z1 - 0.45f, 180f, r);
            Crates(b, p + "Crates", rm.x0 + 0.6f, rm.z1 - 0.6f, r);
            Litter(b, p + "Litter", rm, 5, r);
            AddLight(room, p, rm);
        }

        /// <summary>Restroom half: two stalls on the back wall, sinks + mirror on the side wall.</summary>
        public static void Toilets(MeshBuilder b, Node room, string p, Room rm, bool sinksOnEast)
        {
            b.Begin(p + "Stalls");
            float sw = Mathf.Min(1.3f, (rm.W - 0.4f) / 2f), d = 1.5f;
            for (int i = 0; i < 2; i++)
            {
                float sx = rm.x0 + 0.2f + i * sw;
                b.BoxMM(sx, 0.1f, rm.z1 - d, sx + 0.04f, 2.0f, rm.z1, M.PaleBlue);
                b.Box(new Vector3(sx + sw * 0.5f, 0.2f, rm.z1 - 0.4f), new Vector3(0.4f, 0.4f, 0.6f), M.White);    // toilet
                b.Box(new Vector3(sx + sw * 0.5f, 0.6f, rm.z1 - 0.12f), new Vector3(0.4f, 0.4f, 0.2f), M.White);
                b.BoxMM(sx + 0.04f, 0.1f, rm.z1 - d - 0.02f, sx + sw * 0.45f, 2.0f, rm.z1 - d + 0.02f, M.PaleBlue);    // half-open door
            }
            b.BoxMM(rm.x0 + 0.2f + 2 * sw, 0.1f, rm.z1 - d, rm.x0 + 0.24f + 2 * sw, 2.0f, rm.z1, M.PaleBlue);
            b.Begin(p + "Sinks");
            float wx = sinksOnEast ? rm.x1 - 0.3f : rm.x0 + 0.3f;
            b.BoxMM(wx - 0.3f, 0.8f, rm.z0 + 1.6f, wx + 0.3f, 0.88f, rm.z1 - d - 0.3f, M.White);
            b.BoxMM(sinksOnEast ? rm.x1 - 0.04f : rm.x0, 1.2f, rm.z0 + 1.8f, sinksOnEast ? rm.x1 : rm.x0 + 0.04f, 2.0f, rm.z1 - d - 0.5f, M.Glass);   // mirror
            Litter(b, p + "Litter", rm, 6, new System.Random(p.Length * 7));
            AddLight(room, p, rm);
        }

        static void Pendants(MeshBuilder b, string p, Room rm, int n, System.Random r)
        {
            for (int i = 0; i < n; i++)
            {
                float x = rm.x0 + rm.W * (i + 1f) / (n + 1f), z = (rm.z0 + rm.z1) * 0.5f;
                Pendant(b, p + "Pendant_" + (i + 1), x, z, rm.rh, r.NextDouble() > 0.3);
            }
        }

        /// <summary>One warm point light in the room (a child node; on by default, no shadows).</summary>
        public static void AddLight(Node room, string p, Room rm)
        {
            if (room == null) return;
            room.Add(new Node(p + "RoomLight") { Pos = rm.Centre(rm.rh - 0.9f), Light = new LightSpec { Range = Mathf.Max(rm.W, rm.D) * 0.9f + 2f, Intensity = 1.1f, Enabled = true } });
        }
    }
}
#endif
