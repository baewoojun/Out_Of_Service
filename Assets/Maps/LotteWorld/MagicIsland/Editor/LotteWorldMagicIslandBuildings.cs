// LotteWorldMagicIslandBuildings.cs  (v1 - Magic Island: buildings and rides, each one a prefab)
// Local frame of every builder: pivot = footprint centre on the ground, front (entrance) facing -Z.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotteWorld.MagicIsland
{
    public static class Buildings
    {
        /// <summary>Part-name helpers: compass side of a facade normal / corner of a footprint point (front = south = -Z).</summary>
        public static string Side(Vector3 n) { return n.z < -0.5f ? "S" : n.z > 0.5f ? "N" : n.x > 0.5f ? "E" : "W"; }
        public static string Corner(float x, float z) { return (z < 0f ? "S" : "N") + (x < 0f ? "W" : "E"); }

        // ------------------------------------------------------------------------------------------------------------------
        // Town house / cottage: walk-in shop room on the ground floor (Door_*), windows above, steep roof.
        // ------------------------------------------------------------------------------------------------------------------
        public static Node TownHouse(string name, float W, float D, int floors, M wall, M roof, bool timber, bool gableFront, M trim, int seed)
        {
            var b = new MeshBuilder();
            float x0 = -W * 0.5f, x1 = W * 0.5f, z0 = -D * 0.5f, z1 = D * 0.5f, H = 3.8f + 3.1f * (floors - 1);
            float doorX = x0 + W * 0.28f;
            b.Begin("Walls_Room");
            Vector3 hinge = Lib.BlockWithRoom(b, x0, x1, z0, z1, H, D - 2.5f, 3.4f, doorX, 1.3f, 2.4f, wall, M.Wood, M.Timber);
            b.Begin("Plinth");
            b.BoxMM(x0 - 0.08f, 0f, z0 - 0.08f, x1 + 0.08f, 0.5f, z1 + 0.08f, M.StoneGrey);                 // plinth
            b.Begin("FloorBand");
            b.BoxMM(x0 - 0.1f, 3.75f, z0 - 0.14f, x1 + 0.1f, 3.95f, z1 + 0.14f, trim);                       // floor band
            foreach (Face f in Lib.RectFaces(x0, x1, z0, z1))
            {
                b.PushFacade(f.Origin, f.Normal);
                float L = f.Length; int n = Mathf.Max(1, Mathf.RoundToInt(L / 3.2f));
                b.Begin("Windows_Upper");
                for (int fl = 1; fl < floors; fl++)
                    for (int i = 0; i < n; i++) Lib.Window(b, L * (i + 0.5f) / n, 3.8f + 3.1f * (fl - 1) + 0.9f, 1.0f, 1.6f, trim);
                if (timber)
                {
                    b.Begin("HalfTimber");
                    for (int i = 0; i <= n * 2; i++) b.BoxMM(L * i / (n * 2) - 0.08f, 3.95f, -0.08f, L * i / (n * 2) + 0.08f, H, 0f, M.Timber);
                    for (int fl = 1; fl < floors; fl++) b.BoxMM(0f, 3.8f + 3.1f * fl - 0.15f, -0.08f, L, 3.8f + 3.1f * fl + 0.05f, 0f, M.Timber);
                }
                if (f.Normal == Vector3.back)
                {
                    b.Begin("ShopFront");
                    float sx = doorX - x0, wx = x1 - x0 - W * 0.26f;
                    b.BoxMM(sx - 1.0f, 2.45f, -0.12f, sx + 1.0f, 2.65f, 0f, trim);                          // door head
                    Lib.Window(b, wx, 0.8f, Mathf.Min(3.2f, W * 0.3f), 2.0f, trim);                        // shop window (closed glass)
                    b.BoxMM(wx - 1.7f, 2.95f, -0.08f, wx + 1.7f, 3.55f, 0f, M.White);                      // blank sign board
                }
                b.Pop();
            }
            var lx = new Node("lights");
            var rm = new Room(x0 + 0.3f, x1 - 0.3f, z0 + 0.3f, z0 + D - 2.5f - 0.3f, 3.4f, doorX);
            if (name.StartsWith("Cottage") || name == "TownHouse_C") Furn.Dining(b, lx, "Room_", rm, seed * 31, M.Red);
            else Furn.Shop(b, lx, "Room_", rm, seed * 31);
            float rise = (gableFront ? W : D) * 0.62f;
            b.Begin("Roof");
            if (gableFront) Lib.GableRoofZ(b, x0, x1, z0, z1, H, rise, 0.45f, roof, wall);
            else Lib.GableRoofX(b, x0, x1, z0, z1, H, rise, 0.45f, roof, wall);
            var r = new System.Random(seed);
            float cx = x0 + 1.2f + (float)r.NextDouble() * (W - 2.4f);
            b.Begin("Chimney");
            b.BoxMM(cx - 0.45f, H, 0.5f, cx + 0.45f, H + rise + 0.8f, 1.4f, M.Brick);                          // chimney
            b.BoxMM(cx - 0.55f, H + rise + 0.8f, 0.4f, cx + 0.55f, H + rise + 1.0f, 1.5f, M.StoneGrey);
            var n0 = new Node(name);
            n0.Add(new Node(name + "_Body", b));
            n0.Add(Lib.DoorLeaf("Door_" + name, hinge, 1.2f, 2.35f, M.Wood, true));
            n0.Kids.AddRange(lx.Kids);
            return n0;
        }

        /// <summary>Long two-storey restaurant hall with an arched glass front, double door and a big empty dining room.</summary>
        public static Node Restaurant(string name, float W, float D, M wall, M roof)
        {
            var b = new MeshBuilder();
            float x0 = -W * 0.5f, x1 = W * 0.5f, z0 = -D * 0.5f, z1 = D * 0.5f, H = 7.4f;
            b.Begin("Walls_Hall");
            Vector3 hinge = Lib.BlockWithRoom(b, x0, x1, z0, z1, H, D - 0.3f, 4.2f, 0f, 2.4f, 2.7f, wall, M.Wood, M.Timber);
            b.Begin("Plinth_Band");
            b.BoxMM(x0 - 0.1f, 0f, z0 - 0.1f, x1 + 0.1f, 0.5f, z1 + 0.1f, M.StoneGrey);
            b.BoxMM(x0 - 0.15f, 4.2f, z0 - 0.2f, x1 + 0.15f, 4.5f, z1 + 0.2f, M.White);
            foreach (Face f in Lib.RectFaces(x0, x1, z0, z1))
            {
                b.PushFacade(f.Origin, f.Normal);
                float L = f.Length; int n = Mathf.Max(1, Mathf.RoundToInt(L / 3.4f));
                for (int i = 0; i < n; i++)
                {
                    float cx = L * (i + 0.5f) / n;
                    if (f.Normal == Vector3.back && Mathf.Abs(cx - L * 0.5f) < 2.2f) continue;
                    b.Begin("Windows");
                    Lib.Window(b, cx, 0.9f, 1.6f, 2.6f, M.White, true);
                    Lib.Window(b, cx, 5.0f, 1.1f, 1.6f, M.White);
                }
                if (f.Normal == Vector3.back)
                {
                    b.Begin("Entrance_Arch_Sign");
                    Lib.ArchBand(b, L * 0.5f, 0f, 2.6f, 3.3f, 0.3f, -0.3f, 0f, M.White);
                    b.BoxMM(L * 0.5f - 3f, 4.6f, -0.08f, L * 0.5f + 3f, 5.6f, 0f, M.White);        // blank sign
                }
                b.Pop();
            }
            var lx = new Node("lights");
            Furn.Dining(b, lx, "Hall_", new Room(x0 + 0.3f, x1 - 0.3f, z0 + 0.3f, z0 + D - 0.6f, 4.2f, 0f), 47, M.RoofBlue);
            b.Begin("Roof");
            Lib.HipRoof(b, x0, x1, z0, z1, H, 4.2f, 0.6f, roof, M.White);
            for (int i = 0; i < 3; i++) { b.Begin("RoofTurret_" + (i + 1)); Lib.Turret(b, new Vector3(x0 + W * (i + 1) / 4f, H + 0.5f, z0 + 1.6f), 0.7f, 2.0f, 2.2f, wall, roof, 8); }   // dormer turrets
            var n0 = new Node(name);
            n0.Add(new Node(name + "_Body", b));
            n0.Add(Lib.DoorLeaf("Door_" + name + "_L", hinge, 1.15f, 2.6f, M.Wood, true));
            Node r = Lib.DoorLeaf("Door_" + name + "_R", hinge + new Vector3(2.3f, 0f, 0f), 1.15f, 2.6f, M.Wood, true);
            r.Euler = new Vector3(0f, 180f, 0f); n0.Add(r);
            n0.Kids.AddRange(lx.Kids);
            return n0;
        }

        /// <summary>Single-storey cafe pavilion with a dark wide-eaved roof on timber posts (veranda all round).</summary>
        public static Node CafePavilion(string name)
        {
            var b = new MeshBuilder();
            float x0 = -6f, x1 = 6f, z0 = -4f, z1 = 4f, H = 4.2f;
            b.Begin("Walls_Room");
            Vector3 hinge = Lib.BlockWithRoom(b, x0, x1, z0, z1, H, 8f, 4.0f, -3f, 1.6f, 2.6f, M.Plaster, M.Wood, M.Timber);
            b.Begin("Windows");
            b.PushFacade(new Vector3(x0, 0f, z0), Vector3.back);
            for (int i = 0; i < 3; i++) Lib.Window(b, 5.5f + i * 2.4f, 0.9f, 1.8f, 2.4f, M.Timber);
            b.Pop();
            b.Begin("Veranda");
            b.BoxMM(-8f, 0f, -6f, 8f, 0.25f, 6f, M.StoneGrey);                                               // veranda deck
            foreach (Vector2 p in new[] { new Vector2(-7.6f, -5.6f), new Vector2(0f, -5.6f), new Vector2(7.6f, -5.6f), new Vector2(-7.6f, 5.6f), new Vector2(7.6f, 5.6f), new Vector2(-7.6f, 0f), new Vector2(7.6f, 0f) })
                b.BoxMM(p.x - 0.15f, 0.25f, p.y - 0.15f, p.x + 0.15f, H, p.y + 0.15f, M.Timber);
            var lx = new Node("lights");
            Furn.Dining(b, lx, "Cafe_", new Room(-5.7f, 5.7f, -3.7f, 3.7f, 4.0f, -3f), 42, M.White);
            b.Begin("Roof");
            Lib.HipRoof(b, -7.6f, 7.6f, -5.6f, 5.6f, H, 3.2f, 0.8f, M.RoofGrey, M.Timber);
            var n0 = new Node(name);
            n0.Add(new Node(name + "_Body", b));
            n0.Add(Lib.DoorLeaf("Door_" + name, hinge, 1.5f, 2.55f, M.Wood, true));
            n0.Kids.AddRange(lx.Kids);
            return n0;
        }

        /// <summary>Comet Express show building: dark walls, red hipped roof, arched entrance with a door into the queue lobby.</summary>
        public static Node CometExpress()
        {
            var b = new MeshBuilder();
            float x0 = -13f, x1 = 13f, z0 = -9f, z1 = 9f, H = 10f;
            b.Begin("Walls_Lobby");
            Vector3 hinge = Lib.BlockWithRoom(b, x0, x1, z0, z1, H, 8f, 4.5f, -5f, 2.2f, 3.0f, M.Navy, M.StoneGrey, M.StoneDark);
            b.Begin("Facade_Decor");
            b.BoxMM(x0 - 0.1f, 0f, z0 - 0.1f, x1 + 0.1f, 0.6f, z1 + 0.1f, M.StoneDark);
            foreach (Face f in Lib.RectFaces(x0, x1, z0, z1))
            {
                b.PushFacade(f.Origin, f.Normal);
                b.BoxMM(-0.1f, H - 1.2f, -0.2f, f.Length + 0.1f, H - 0.9f, 0f, M.Gold);
                var r = new System.Random((int)(f.Length * 10f));
                for (int i = 0; i < 9; i++)                                                              // star studs
                    b.Box(new Vector3((float)r.NextDouble() * f.Length, 5f + (float)r.NextDouble() * 3.5f, -0.05f), new Vector3(0.35f, 0.35f, 0.1f), M.Yellow);
                if (f.Normal == Vector3.back)
                {
                    Lib.ArchBand(b, 8f, 0f, 2.4f, 3.3f, 0.45f, -0.5f, 0f, M.Red);
                    b.BoxMM(5f, 5.2f, -0.1f, 15f, 7.6f, 0f, M.White);                                    // blank marquee board
                }
                b.Pop();
            }
            var lx = new Node("lights");
            Furn.Queue(b, lx, "Lobby_", new Room(-12.7f, 12.7f, -8.7f, -1.3f, 4.5f, -5f), 38);
            b.Begin("Roof");
            Lib.HipRoof(b, x0, x1, z0, z1, H, 4.5f, 0.6f, M.RoofRed, M.Gold);
            var n0 = new Node("CometExpress");
            n0.Add(new Node("CometExpress_Body", b));
            n0.Add(Lib.DoorLeaf("Door_CometExpress", hinge, 2.1f, 2.9f, M.Red, false));
            n0.Kids.AddRange(lx.Kids);
            return n0;
        }

        /// <summary>Kids' shop with a row of brightly coloured fronts (blank signs), walk-in shop room.</summary>
        public static Node ColourShop(string name)
        {
            var b = new MeshBuilder();
            float x0 = -8f, x1 = 8f, z0 = -7f, z1 = 7f, H = 7f;
            b.Begin("Walls_Shop");
            Vector3 hinge = Lib.BlockWithRoom(b, x0, x1, z0, z1, H, 11f, 4f, -1.5f, 2.0f, 2.7f, M.Plaster, M.Pink, M.White);
            M[] cols = { M.Pink, M.Yellow, M.Mint, M.Purple };
            for (int i = 0; i < 4; i++)
            {
                float a = x0 + 4f * i, top = H + 1.2f + (i % 2) * 1.6f;
                b.Begin("Front_" + (i + 1));
                if (i == 1) Lib.WallWithHole(b, a, a + 4f, 0f, top, z0 - 0.35f, z0, -1.5f, 2.0f, 2.7f, false, cols[i]);   // skin with the door hole
                else b.BoxMM(a, 0f, z0 - 0.35f, a + 4f, top, z0, cols[i]);                                // facade skins
                b.BoxMM(a + 0.4f, top, z0 - 0.35f, a + 3.6f, top + 0.8f, z0, cols[(i + 2) % 4]);
                b.PushFacade(new Vector3(x0, 0f, z0 - 0.35f), Vector3.back);
                if (i != 1) Lib.Window(b, 4f * i + 2f, 0.9f, 2.4f, 2.2f, M.White);
                Lib.Window(b, 4f * i + 2f, 4.4f, 1.4f, 1.6f, M.White, true);
                b.BoxMM(4f * i + 0.4f, 3.3f, -0.1f, 4f * i + 3.6f, 3.9f, 0f, M.White);
                b.Pop();
            }
            var lx = new Node("lights");
            Furn.Shop(b, lx, "Shop_", new Room(-7.7f, 7.7f, -6.7f, 3.7f, 4f, -1.5f), 21);
            b.Begin("Roof");
            Lib.HipRoof(b, x0, x1, z0, z1, H, 2.2f, 0.4f, M.Purple, M.Yellow);
            var n0 = new Node(name);
            n0.Add(new Node(name + "_Body", b));
            n0.Add(Lib.DoorLeaf("Door_" + name, hinge + new Vector3(0f, 0f, -0.35f), 1.9f, 2.65f, M.Teal, true));
            n0.Kids.AddRange(lx.Kids);
            return n0;
        }

        /// <summary>Rocky station building (Stone Express) with a green roof terrace.</summary>
        public static Node StoneFort()
        {
            var b = new MeshBuilder();
            float x0 = -8f, x1 = 8f, z0 = -6f, z1 = 6f, H = 7f;
            b.Begin("Walls_Room");
            Vector3 hinge = Lib.BlockWithRoom(b, x0, x1, z0, z1, H, 8f, 3.6f, 0f, 2.2f, 2.8f, M.RockGrey, M.StoneGrey, M.StoneDark);
            b.Begin("Entrance_Arch");
            b.PushFacade(new Vector3(x0, 0f, z0), Vector3.back);
            Lib.ArchBand(b, 8f, 0f, 2.4f, 3.0f, 0.6f, -0.6f, 0f, M.RockDark);
            b.Pop();
            var off = new Vector3(3.1f, 7.7f, 1.3f);
            b.Begin("Rock_West");
            Lib.RockBlock(b, new Vector3(-5.6f, 2.5f, z0 - 0.4f), new Vector3(4.4f, 5f, 2f), 0.8f, 0.4f, off, M.Rock, 0.9f);
            b.Begin("Rock_East");
            Lib.RockBlock(b, new Vector3(5.6f, 2.8f, z0 - 0.4f), new Vector3(4.4f, 5.6f, 2f), 0.8f, 0.4f, off * 2f, M.Rock, 0.9f);
            var lx = new Node("lights");
            Furn.Queue(b, lx, "Station_", new Room(-7.7f, 7.7f, -5.7f, 1.7f, 3.6f, 0f), 40);
            b.Begin("RoofGarden");
            b.BoxMM(x0 - 0.3f, H, z0 - 0.3f, x1 + 0.3f, H + 0.35f, z1 + 0.3f, M.Grass);
            for (int i = 0; i < 6; i++) b.Blob(new Vector3(-6f + i * 2.4f, H + 0.35f, 4f), 1.0f, 0.8f, 7, M.Leaf);
            var n0 = new Node("StoneFort");
            n0.Add(new Node("StoneFort_Body", b));
            n0.Add(Lib.DoorLeaf("Door_StoneFort", hinge, 2.1f, 2.75f, M.Timber, false));
            n0.Kids.AddRange(lx.Kids);
            return n0;
        }

        /// <summary>Shore-side entrance gate to the bridge: two blue-cone towers and a crenellated arch (open walkway).</summary>
        public static Node IslandGate()
        {
            var b = new MeshBuilder();
            const float half = 7.5f;
            b.Begin("Gate_Arch");
            b.PushFacade(new Vector3(-half, 0f, -1.5f), Vector3.back);
            Lib.WallWithHole(b, 0f, half * 2f, 0f, 9f, 0f, 3f, half, 9f, 6.8f, true, M.CastleCream);
            Lib.ArchBand(b, half, 0f, 9f, 6.8f, 0.5f, -0.3f, 0f, M.CastlePink);
            Lib.ArchBand(b, half, 0f, 9f, 6.8f, 0.5f, 3f, 3.3f, M.CastlePink);
            b.BoxMM(0f, 7.7f, -0.25f, half * 2f, 8.1f, 3.25f, M.CastlePink);
            b.Pop();
            b.Begin("Gate_Crenels");
            Lib.Crenels(b, -half, half, -1.5f, 1.5f, 9f, M.CastleCream);
            b.Begin("Tower_West");
            Lib.Turret(b, new Vector3(-half - 1.6f, 0f, 0f), 2.6f, 12f, 7f, M.CastleCream, M.RoofBlue);
            b.Begin("Tower_East");
            Lib.Turret(b, new Vector3(half + 1.6f, 0f, 0f), 2.6f, 12f, 7f, M.CastleCream, M.RoofBlue);
            b.Begin("Medallion");
            b.PushFacade(new Vector3(-1.6f, 0f, -1.5f), Vector3.back);                                    // blank medallion
            Lib.ExtrudeXY(b, Geo.Circle(new Vector2(1.6f, 8.4f), 1.4f, 0.9f, 18), -0.4f, 0f, M.Gold);
            b.Pop();
            b.Begin("Gate_Floor");
            Lib.TileFloor(b, -4.5f, 4.5f, -1.5f, 1.5f, 0.07f, 1.5f, M.PavingDark, M.Paving);
            var n0 = new Node("IslandGate"); n0.Add(new Node("IslandGate_Body", b));
            return n0;
        }

        // ------------------------------------------------------------------------------------------------------------------
        // Magic Castle: 36 x 34 m base with a walk-through arch passage (north-south), a shop room (Door_Castle_Shop),
        // corner towers, an upper keep and the central spire (~53 m).
        // ------------------------------------------------------------------------------------------------------------------
        public static Node MagicCastle()
        {
            var b = new MeshBuilder();
            const float X = 18f, Z = 17f, HB = 12f, pw = 3.6f;
            // left block: shop room at the front; right block: solid
            b.Begin("Base_West_Shop");
            Vector3 hinge = Lib.BlockWithRoom(b, -X, -pw, -Z, Z, HB, 10f, 4.2f, -11f, 1.9f, 2.8f, M.CastleCream, M.CastlePink, M.CastleCream, 0.5f);
            var lx = new Node("lights");
            Furn.Shop(b, lx, "Shop19_", new Room(-17.5f, -4.1f, -16.5f, -7.5f, 4.2f, -11f), 19);
            b.Begin("Base_East");
            b.BoxMM(pw, 0f, -Z, X, HB, Z, M.CastleCream);
            // passage: floor, ceiling, arched portals front and back
            b.Begin("Passage_Floor");
            Lib.TileFloor(b, -pw, pw, -Z - 1f, Z + 1f, 0f, 1.8f, M.CastlePink, M.Paving);
            b.Begin("Passage_Vault");
            b.BoxMM(-pw, 7.6f, -Z, pw, HB, Z, M.CastleCream);
            foreach (float zf in new[] { -Z, Z })
            {
                bool front = zf < 0f;
                b.Begin(front ? "Portal_South" : "Portal_North");
                b.PushFacade(new Vector3(front ? -pw - 1.2f : pw + 1.2f, 0f, zf + (front ? -0.8f : 0.8f)), front ? Vector3.back : Vector3.forward);
                Lib.WallWithHole(b, 0f, pw * 2f + 2.4f, 0f, HB + 2.5f, 0f, 0.8f, pw + 1.2f, pw * 2f, 7.6f, true, M.CastleCream);
                Lib.ArchBand(b, pw + 1.2f, 0f, pw * 2f, 7.6f, 0.45f, -0.35f, 0f, M.CastlePink);
                b.Pop();
            }
            // facade dressing: plinth, pink bands, windows
            b.Begin("Plinth");
            b.BoxMM(-X - 0.15f, 0f, -Z - 0.15f, -pw - 1.2f, 0.9f, Z + 0.15f, M.StoneGrey);
            b.BoxMM(pw + 1.2f, 0f, -Z - 0.15f, X + 0.15f, 0.9f, Z + 0.15f, M.StoneGrey);
            foreach (Face f in Lib.RectFaces(-X, X, -Z, Z))
            {
                b.Begin("Facade_" + Side(f.Normal));
                b.PushFacade(f.Origin, f.Normal);
                float L = f.Length; bool fb = f.Normal == Vector3.back || f.Normal == Vector3.forward;
                foreach (float y in new[] { 5.6f, 10.6f })
                {
                    if (fb) { b.BoxMM(-0.2f, y, -0.2f, X - pw - 1.2f, y + 0.4f, 0f, M.CastlePink); b.BoxMM(X + pw + 1.2f, y, -0.2f, L + 0.2f, y + 0.4f, 0f, M.CastlePink); }
                    else b.BoxMM(-0.2f, y, -0.2f, L + 0.2f, y + 0.4f, 0f, M.CastlePink);
                }
                int n = Mathf.RoundToInt(L / 4f);
                for (int i = 0; i < n; i++)
                {
                    float cx = L * (i + 0.5f) / n;
                    if (fb && Mathf.Abs(cx - L * 0.5f) < pw + 3f) continue;
                    if (f.Normal == Vector3.back && Mathf.Abs(cx - (X - 11f)) < 1.6f) continue;      // shop door
                    if (cx < 4.5f || cx > L - 4.5f) continue;                                          // corner towers
                    Lib.Window(b, cx, 7.0f, 1.0f, 2.2f, M.CastlePink, true);
                    if (f.Normal == Vector3.back && cx < X) Lib.Window(b, cx, 1.0f, 1.4f, 2.4f, M.CastlePink, true);   // shop windows
                }
                b.Pop();
            }
            b.Begin("Battlements_Base");
            Lib.Crenels(b, -X, X, -Z, Z, HB, M.CastleCream);
            // corner towers and the pair flanking the arch
            foreach (Vector2 c in new[] { new Vector2(-X, -Z), new Vector2(X, -Z), new Vector2(X, Z), new Vector2(-X, Z) })
            {
                b.Begin("Tower_" + Corner(c.x, c.y));
                Lib.Turret(b, new Vector3(c.x, 0f, c.y), 4.2f, 22f, 10f, M.CastleCream, M.RoofBlue, 14);
            }
            foreach (float sx in new[] { -1f, 1f })
                foreach (float zf in new[] { -Z - 0.6f, Z + 0.6f })
                {
                    b.Begin("GateTower_" + Corner(sx, zf));
                    Lib.Turret(b, new Vector3(sx * (pw + 2.6f), 0f, zf), 2.4f, 17f, 7f, M.CastleCream, M.RoofBlue, 12);
                }
            // upper keep
            float kx = 11f, kz0 = -9f, kz1 = 10f, kh = 25f;
            b.Begin("Keep");
            b.BoxMM(-kx, HB, kz0, kx, kh, kz1, M.CastleCream);
            foreach (Face f in Lib.RectFaces(-kx, kx, kz0, kz1))
            {
                b.Begin("Keep_Facade_" + Side(f.Normal));
                b.PushFacade(f.Origin + Vector3.up * HB, f.Normal);
                float L = f.Length; int n = Mathf.RoundToInt(L / 3.6f);
                for (int i = 0; i < n; i++) { Lib.Window(b, L * (i + 0.5f) / n, 2.6f, 1.0f, 2.2f, M.CastlePink, true); Lib.Window(b, L * (i + 0.5f) / n, 7.4f, 1.0f, 2.2f, M.CastlePink, true); }
                b.BoxMM(-0.2f, 6.0f, -0.25f, L + 0.2f, 6.4f, 0f, M.CastlePink);
                b.Pop();
            }
            b.Begin("Battlements_Keep");
            Lib.Crenels(b, -kx, kx, kz0, kz1, kh, M.CastleCream);
            foreach (Vector2 c in new[] { new Vector2(-kx, kz0), new Vector2(kx, kz0), new Vector2(kx, kz1), new Vector2(-kx, kz1) })
            {
                b.Begin("KeepTurret_" + Corner(c.x, c.y));
                Lib.Turret(b, new Vector3(c.x, HB, c.y), 2.0f, 17f, 6.5f, M.CastleCream, M.RoofBlue, 10);
            }
            b.Begin("Keep_Medallion");
            b.PushFacade(new Vector3(-3f, HB, kz0), Vector3.back);                                        // blank gold medallion on the keep
            Lib.ExtrudeXY(b, Geo.Circle(new Vector2(3f, 9.6f), 1.8f, 1.8f, 20), -0.2f, 0f, M.Gold);
            b.Pop();
            // central tower and spire
            b.Begin("CentralTower");
            b.Cyl(new Vector3(0f, kh, 0.5f), 6.2f, 6.2f, 1.0f, 8, M.CastlePink, 22.5f);
            b.Cyl(new Vector3(0f, kh + 1f, 0.5f), 5.4f, 5.4f, 10f, 8, M.CastleCream, 22.5f);
            for (int i = 0; i < 8; i++)
            {
                float a = (22.5f + 45f * i + 22.5f) * Mathf.Deg2Rad; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                b.PushFacade(new Vector3(0f, kh + 1f, 0.5f) + d * (5.4f * Mathf.Cos(22.5f * Mathf.Deg2Rad)) - Vector3.Cross(Vector3.up, -d) * 1.0f, d);
                Lib.Window(b, 1.0f, 3.0f, 1.1f, 3.0f, M.CastlePink, true);
                b.Pop();
            }
            b.Begin("Spire");
            b.Cyl(new Vector3(0f, kh + 11f, 0.5f), 6.0f, 6.0f, 0.5f, 8, M.Gold, 22.5f);
            b.Cone(new Vector3(0f, kh + 11.5f, 0.5f), 6.2f, 14f, 8, M.RoofBlue, 22.5f);
            b.Cyl(new Vector3(0f, kh + 24.5f, 0.5f), 0.12f, 0.08f, 4f, 6, M.Gold);
            b.Blob(new Vector3(0f, kh + 25.8f, 0.5f), 0.35f, 0.35f, 8, M.Gold);
            foreach (Vector2 c in new[] { new Vector2(-6.5f, -6f), new Vector2(6.5f, -6f), new Vector2(6.5f, 7f), new Vector2(-6.5f, 7f) })
            {
                b.Begin("SpireTurret_" + Corner(c.x, c.y));
                Lib.Turret(b, new Vector3(c.x, kh, c.y), 1.4f, 6f, 4.2f, M.CastleCream, M.RoofBlue, 10);
            }
            var n0 = new Node("MagicCastle");
            n0.Add(new Node("MagicCastle_Body", b));
            n0.Add(Lib.DoorLeaf("Door_Castle_Shop", hinge, 1.8f, 2.75f, M.RoofBlueDark, true));
            n0.Kids.AddRange(lx.Kids);
            return n0;
        }

        // ------------------------------------------------------------------------------------------------------------------
        // Atlantis temple station on its rock: front (track tunnel + public stair) facing -Z.
        // ------------------------------------------------------------------------------------------------------------------
        public static Node AtlantisTemple()
        {
            var b = new MeshBuilder();
            const float top = 7f;
            b.Begin("RockCore");
            b.BoxMM(-16f, 0f, -12.5f, 16f, top, 12.5f, M.RockDark);                                       // core (hidden by rocks)
            var r = new System.Random(33);
            for (int i = 0; i < 14; i++)                                                                     // rock skin round the core
            {
                float a = Mathf.PI * 2f * i / 14f; Vector3 c = new Vector3(Mathf.Cos(a) * 16.5f, 0f, Mathf.Sin(a) * 12.8f);
                if (Mathf.Abs(c.x - 6f) < 4f && c.z < -8f) continue;                                         // keep the stair clear
                if (c.x < 1f && c.x > -15f && c.z < -8f) continue;                                          // and the track portals
                float h = top + 0.6f + (float)r.NextDouble() * 3f;
                b.Begin("Rock_" + (i + 1).ToString("00"));
                Lib.RockBlock(b, new Vector3(c.x, h * 0.5f, c.z), new Vector3(9f, h, 7f), 2.6f, 0.3f, new Vector3(i * 3.1f, 1.7f, i * 0.7f), i % 2 == 0 ? M.RockGrey : M.RockDark, 1.2f);
            }
            b.Begin("Terrace");
            b.BoxMM(-15.5f, top, -12f, 15.5f, top + 0.3f, 12f, M.Paving);
            b.Begin("TrackPortals");
            // track portal (fake dark tunnel mouth) and the public stair up to the temple terrace
            b.PushFacade(new Vector3(-16f, 0f, -12.5f), Vector3.back);                                       // track out (x -11) and back in (x -2.5)
            foreach (float px in new[] { 5f, 13.5f })
            {
                Lib.ExtrudeXY(b, Lib.ArchPts(px, 0f, 4.6f, 5.2f), -0.02f, 0f, M.Opening);
                Lib.ArchBand(b, px, 0f, 4.6f, 5.2f, 0.7f, -0.7f, 0f, M.StoneGrey);
            }
            b.Pop();
            b.Begin("Stair");
            int steps = 38; float rise = top / steps, run = 0.3f;
            for (int i = 0; i < steps; i++)
                b.BoxMM(4f, 0f, -12.5f - (steps - i) * run, 8f, rise * (i + 1), -12.5f - (steps - i - 1) * run, M.StoneGrey);
            foreach (float sx in new[] { 3.7f, 8.3f })
            {
                b.BoxMM(sx - 0.3f, 0f, -12.5f - steps * run, sx + 0.3f, 1.0f, -12.5f, M.CastleCream);
                for (int i = 0; i < steps; i += 2) b.BoxMM(sx - 0.3f, 0f, -12.5f - (steps - i) * run, sx + 0.3f, rise * (i + 1) + 1.0f, -12.5f - (steps - i - 2) * run, M.CastleCream);
            }
            // drum, dome, lantern
            Vector3 c0 = new Vector3(0f, top + 0.3f, 2f);
            b.Begin("Drum");
            b.Cyl(c0, 10.5f, 10.5f, 0.8f, 8, M.StoneGrey, 22.5f);
            b.Cyl(c0 + Vector3.up * 0.8f, 9.6f, 9.6f, 8.6f, 8, M.CastleCream, 22.5f);
            for (int i = 0; i < 8; i++)
            {
                float a = (45f * i + 45f) * Mathf.Deg2Rad; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float ap = 9.6f * Mathf.Cos(22.5f * Mathf.Deg2Rad);
                b.PushFacade(c0 + d * ap - Vector3.Cross(Vector3.up, -d) * 1.4f, d);
                Lib.Window(b, 1.4f, 2.2f, 1.6f, 4.6f, M.Gold, true);
                b.Pop();
            }
            b.Cyl(c0 + Vector3.up * 9.4f, 10.3f, 10.3f, 0.6f, 16, M.Gold);
            b.Cyl(c0 + Vector3.up * 10f, 8.6f, 8.6f, 2.2f, 16, M.CastleCream);
            var dome = new List<Vector2>();
            for (int i = 0; i <= 8; i++) { float a = Mathf.PI * 0.5f * i / 8f; dome.Add(new Vector2(Mathf.Cos(a) * 8.8f, Mathf.Sin(a) * 8.8f)); }
            dome[8] = new Vector2(1.3f, dome[8].y); dome.Add(new Vector2(0f, 8.8f));
            b.Begin("Dome");
            b.Lathe(c0 + Vector3.up * 12.2f, dome.ToArray(), 16, M.White);
            for (int i = 0; i < 8; i++)
            {
                float a = Mathf.PI * 2f * i / 8f; var rib = new List<Vector3>();
                for (int j = 0; j <= 8; j++) { float t = Mathf.PI * 0.5f * j / 8f; rib.Add(c0 + Vector3.up * 12.2f + new Vector3(Mathf.Cos(a) * Mathf.Cos(t) * 8.95f, Mathf.Sin(t) * 8.95f, Mathf.Sin(a) * Mathf.Cos(t) * 8.95f)); }
                b.Tube(rib, 0.25f, M.Gold, false);
            }
            b.Begin("Lantern");
            b.Cyl(c0 + Vector3.up * 20.8f, 1.3f, 1.3f, 1.8f, 8, M.CastleCream);
            b.Cone(c0 + Vector3.up * 22.6f, 1.6f, 1.8f, 8, M.Gold);
            b.Cyl(c0 + Vector3.up * 24.2f, 0.08f, 0.08f, 2f, 6, M.Gold);
            // porch with a door into a small vestibule
            b.Begin("Porch");
            b.Push(new Vector3(0f, top + 0.3f, -9f), 0f);
            Vector3 hinge = Lib.BlockWithRoom(b, -4f, 4f, -3f, 1.6f, 6.5f, 4.6f, 3.4f, 0f, 1.8f, 2.7f, M.CastleCream, M.StoneGrey, M.Gold);
            b.BoxMM(-4.4f, 6.5f, -3.4f, 4.4f, 7.1f, 2f, M.Gold);
            var lx = new Node("lights");
            Furn.Queue(b, lx, "Vestibule_", new Room(-3.7f, 3.7f, -2.7f, 1.3f, 3.4f, 0f), 33);
            foreach (Node ln in lx.Kids) ln.Pos += new Vector3(0f, top + 0.3f, -9f);
            b.Pop();
            foreach (Vector2 m in new[] { new Vector2(-13.5f, -9.5f), new Vector2(13.5f, -9.5f), new Vector2(13.5f, 9.5f), new Vector2(-13.5f, 9.5f) })
            {
                Vector3 mb = new Vector3(m.x, top + 0.3f, m.y);
                b.Begin("Minaret_" + Corner(m.x, m.y));
                b.Cyl(mb, 1.5f, 1.5f, 1f, 8, M.StoneGrey);
                b.Cyl(mb + Vector3.up, 1.15f, 1.0f, 12f, 8, M.CastleCream);
                b.Cyl(mb + Vector3.up * 9f, 1.5f, 1.5f, 0.5f, 8, M.Gold);
                b.Blob(mb + Vector3.up * 13f, 1.3f, 1.4f, 8, M.White);
                b.Cyl(mb + Vector3.up * 15.6f, 0.06f, 0.06f, 1.4f, 6, M.Gold);
            }
            var n0 = new Node("AtlantisTemple");
            n0.Add(new Node("AtlantisTemple_Body", b));
            n0.Add(Lib.DoorLeaf("Door_Atlantis_Temple", hinge + new Vector3(0f, top + 0.3f, -9f), 1.75f, 2.65f, M.Gold, false));
            n0.Kids.AddRange(lx.Kids);
            return n0;
        }

        // ------------------------------------------------------------------------------------------------------------------
        // Rides
        // ------------------------------------------------------------------------------------------------------------------
        /// <summary>Gyro Drop: ~78 m white tower, top cabin, seat ring at the bottom (DropRing, move it along Y).</summary>
        public static Node GyroDrop()
        {
            var b = new MeshBuilder();
            b.Begin("BasePlatform");
            b.Cyl(Vector3.zero, 12f, 12f, 0.6f, 24, M.StoneGrey);
            b.Begin("Tower");
            b.Cyl(new Vector3(0f, 0.6f, 0f), 3.4f, 3.0f, 2f, 12, M.RoofBlueDark);
            b.Cyl(new Vector3(0f, 2.6f, 0f), 2.4f, 2.4f, 70f, 16, M.White);
            for (int i = 0; i < 28; i++)                                                                     // blue checker band
            {
                float y = 22f + (i / 14) * 1.2f; float a = (360f / 14f * (i % 14) + (i / 14) * 12.85f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                b.BoxO(d * 2.42f + Vector3.up * (y + 0.6f), Vector3.Cross(Vector3.up, d), Vector3.up, d, new Vector3(1.05f, 1.2f, 0.06f), M.RoofBlue);
            }
            b.Begin("TopCabin");
            b.Cyl(new Vector3(0f, 72.6f, 0f), 4.6f, 4.6f, 5.4f, 8, M.Yellow, 22.5f);
            b.Cyl(new Vector3(0f, 74.2f, 0f), 4.65f, 4.65f, 1.6f, 8, M.Glass, 22.5f);
            b.Cyl(new Vector3(0f, 78f, 0f), 5.2f, 3.6f, 0.8f, 8, M.Gold, 22.5f);
            b.Cyl(new Vector3(0f, 78.8f, 0f), 0.15f, 0.08f, 7f, 6, M.Metal);
            b.Begin("Fence");                                                                                // fence ring with an entrance gap at the front
            var fence = new List<Vector3>();
            for (int i = 2; i <= 30; i++) { float a = -Mathf.PI * 0.5f + Mathf.PI * 2f * i / 32f; fence.Add(new Vector3(Mathf.Cos(a) * 11.4f, 0.6f, Mathf.Sin(a) * 11.4f)); }
            Lib.Railing(b, fence, M.RoofBlueDark);
            var ring = new MeshBuilder();
            ring.Cyl(Vector3.zero, 5.2f, 5.2f, 0.5f, 24, M.Yellow);
            ring.Cyl(new Vector3(0f, 0.5f, 0f), 3.0f, 3.0f, 2.6f, 16, M.Gold);
            for (int i = 0; i < 20; i++)
            {
                float a = Mathf.PI * 2f * i / 20f; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), s = Vector3.Cross(Vector3.up, d);
                ring.BoxO(d * 4.2f + Vector3.up * 0.75f, s, Vector3.up, d, new Vector3(0.7f, 0.5f, 0.8f), M.Red);
                ring.BoxO(d * 3.75f + Vector3.up * 1.6f, s, Vector3.up, d, new Vector3(0.7f, 1.8f, 0.25f), M.Red);
            }
            var n0 = new Node("GyroDrop");
            n0.Add(new Node("GyroDrop_Tower", b));
            n0.Add(new Node("DropRing", ring) { Pos = new Vector3(0f, 2.8f, 0f), IsStatic = false, Note = "move along local Y (2.8 .. 70)" });
            return n0;
        }

        /// <summary>Gyro Swing: A-frames, pendulum (Swing_Pivot, rotate local Z) with a spinning seat disc (Disc_Rotor, rotate local Y).</summary>
        public static Node GyroSwing()
        {
            const float PH = 29f, arm = 21f;
            var f = new MeshBuilder();
            foreach (float zs in new[] { -1f, 1f })
            {
                Vector3 top = new Vector3(0f, PH, zs * 4.2f);
                f.Begin(zs < 0f ? "AFrame_South" : "AFrame_North");
                f.Member(new Vector3(-13f, 0.5f, zs * 7.5f), top, 1.0f, M.White);
                f.Member(new Vector3(13f, 0.5f, zs * 7.5f), top, 1.0f, M.White);
                f.Member(new Vector3(-6.5f, PH * 0.5f, zs * 5.9f), new Vector3(6.5f, PH * 0.5f, zs * 5.9f), 0.5f, M.Red);
                foreach (float xs in new[] { -13f, 13f }) f.Cyl(new Vector3(xs, 0f, zs * 7.5f), 1.3f, 1.3f, 0.5f, 8, M.StoneGrey);
            }
            f.Begin("AFrame_South");
            f.Cyl(new Vector3(0f, PH, -4.6f), 1.6f, 1.6f, 0.6f, 12, M.Red);
            f.Begin("LoadingPlatform");
            f.Cyl(new Vector3(0f, 0f, 0f), 7f, 7f, 0.5f, 20, M.StoneGrey);                                   // loading platform
            f.Begin("Fence");
            var fence = new List<Vector3>();
            for (int i = 2; i <= 30; i++) { float a = -Mathf.PI * 0.5f + Mathf.PI * 2f * i / 32f; fence.Add(new Vector3(Mathf.Cos(a) * 16f, 0f, Mathf.Sin(a) * 16f)); }
            Lib.Railing(f, fence, M.Red);
            var axle = new MeshBuilder();
            axle.BoxO(Vector3.zero, Vector3.right, Vector3.up, Vector3.forward, new Vector3(1.2f, 1.2f, 8.6f), M.Metal);
            var armB = new MeshBuilder();
            armB.Member(new Vector3(-0.8f, 0f, 0f), new Vector3(-0.5f, -arm, 0f), 0.9f, M.Red);
            armB.Member(new Vector3(0.8f, 0f, 0f), new Vector3(0.5f, -arm, 0f), 0.9f, M.Red);
            for (int i = 1; i < 6; i++) armB.Member(new Vector3(-0.8f, -arm * i / 6f, 0f), new Vector3(0.8f, -arm * i / 6f, 0f), 0.3f, M.White);
            armB.Box(new Vector3(0f, 3f, 0f), new Vector3(3f, 6f, 3f), M.Yellow);                                 // counterweight
            var disc = new MeshBuilder();
            disc.Cyl(new Vector3(0f, -0.4f, 0f), 5.4f, 5.4f, 0.5f, 24, M.Yellow);
            disc.Cyl(new Vector3(0f, 0.1f, 0f), 1.4f, 1.0f, 1.2f, 12, M.Red);
            for (int i = 0; i < 24; i++)
            {
                float a = Mathf.PI * 2f * i / 24f; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), s = Vector3.Cross(Vector3.up, d);
                disc.BoxO(d * 4.6f + Vector3.up * 0.35f, s, Vector3.up, d, new Vector3(0.75f, 0.5f, 0.8f), i % 2 == 0 ? M.Red : M.RoofBlue);
                disc.BoxO(d * 4.1f + Vector3.up * 1.0f, s, Vector3.up, d, new Vector3(0.75f, 1.6f, 0.25f), i % 2 == 0 ? M.Red : M.RoofBlue);
            }
            var n0 = new Node("GyroSwing");
            n0.Add(new Node("GyroSwing_Frame", f));
            Node piv = n0.Add(new Node("Swing_Pivot") { Pos = new Vector3(0f, PH, 0f), IsStatic = false, Note = "rotate local Z to swing (+-120 deg)" });
            piv.Add(new Node("Swing_Axle", axle) { IsStatic = false });
            piv.Add(new Node("Swing_Arm", armB) { IsStatic = false });
            Node rot = piv.Add(new Node("Disc_Rotor") { Pos = new Vector3(0f, -arm, 0f), IsStatic = false, Note = "rotate local Y to spin" });
            rot.Add(new Node("Disc_Seats", disc) { IsStatic = false });
            return n0;
        }

        /// <summary>Gyro Spin: U-shaped half-pipe track along X with a spinning disc (Spin_Disc).</summary>
        public static Node GyroSpin()
        {
            var b = new MeshBuilder();
            const float half = 14f, h = 11f;
            var rails = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>() };
            b.Begin("Track");
            for (int i = 0; i <= 28; i++)
            {
                float x = -half + 2f * half * i / 28f, y = 1.6f + h * Mathf.Pow(Mathf.Abs(x) / half, 2.2f);
                rails[0].Add(new Vector3(x, y, -1.6f)); rails[1].Add(new Vector3(x, y, 1.6f));
                if (i % 2 == 0) b.Member(new Vector3(x, y, -1.8f), new Vector3(x, y, 1.8f), 0.25f, M.Purple);
            }
            b.Tube(rails[0], 0.45f, M.Pink, false); b.Tube(rails[1], 0.45f, M.Pink, false);
            b.Begin("Supports");
            for (int i = 0; i <= 28; i += 4)
            {
                Vector3 p = rails[0][i];
                foreach (float z in new[] { -2.4f, 2.4f }) b.Member(new Vector3(p.x, 0f, z * 1.6f), new Vector3(p.x, p.y - 0.2f, z * 0.7f), 0.5f, M.Purple);
            }
            b.Begin("LoadingDeck");
            b.BoxMM(-6f, 0f, -7f, 6f, 0.4f, -3.2f, M.StoneGrey);                                           // loading deck
            var d = new MeshBuilder();
            d.Cyl(new Vector3(0f, -0.3f, 0f), 3.4f, 3.4f, 0.5f, 18, M.Pink);
            for (int i = 0; i < 16; i++)
            {
                float a = Mathf.PI * 2f * i / 16f; Vector3 dd = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), s = Vector3.Cross(Vector3.up, dd);
                d.BoxO(dd * 2.9f + Vector3.up * 0.55f, s, Vector3.up, dd, new Vector3(0.7f, 1.2f, 0.5f), M.Yellow);
            }
            var n0 = new Node("GyroSpin");
            n0.Add(new Node("GyroSpin_Track", b));
            n0.Add(new Node("Spin_Disc", d) { Pos = new Vector3(0f, 2.2f, 0f), IsStatic = false, Note = "slide along the track, rotate local Y" });
            return n0;
        }

        /// <summary>Eos Tower: colourful kids' drop tower with a crown top and a seat ring (Eos_Ring).</summary>
        public static Node EosTower()
        {
            var b = new MeshBuilder();
            b.Begin("Base");
            b.Cyl(Vector3.zero, 5f, 5f, 0.4f, 16, M.Mint);
            b.Begin("Tower");
            b.BoxMM(-1.4f, 0.4f, -1.4f, 1.4f, 20f, 1.4f, M.Yellow);
            for (int i = 0; i < 6; i++) b.BoxMM(-1.5f, 2f + i * 3f, -1.5f, 1.5f, 2.6f + i * 3f, 1.5f, M.Orange);
            b.Begin("Crown");
            b.Cyl(new Vector3(0f, 20f, 0f), 3.2f, 3.2f, 1.2f, 8, M.Purple, 22.5f);
            for (int i = 0; i < 8; i++) { float a = Mathf.PI * 2f * i / 8f; b.Cone(new Vector3(Mathf.Cos(a) * 2.8f, 21.2f, Mathf.Sin(a) * 2.8f), 0.5f, 1.8f, 6, M.Gold); }
            b.Cone(new Vector3(0f, 21.2f, 0f), 1.8f, 4.5f, 8, M.Pink);
            b.Blob(new Vector3(0f, 25.8f, 0f), 0.6f, 0.6f, 8, M.Yellow);
            var ring = new MeshBuilder();
            ring.Cyl(Vector3.zero, 3.6f, 3.6f, 0.4f, 16, M.Purple);
            for (int i = 0; i < 12; i++)
            {
                float a = Mathf.PI * 2f * i / 12f; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), s = Vector3.Cross(Vector3.up, d);
                ring.BoxO(d * 2.9f + Vector3.up * 0.9f, s, Vector3.up, d, new Vector3(0.7f, 1.4f, 0.5f), i % 2 == 0 ? M.Pink : M.Mint);
            }
            var n0 = new Node("EosTower");
            n0.Add(new Node("EosTower_Body", b));
            n0.Add(new Node("Eos_Ring", ring) { Pos = new Vector3(0f, 1.0f, 0f), IsStatic = false, Note = "move along local Y (1 .. 17)" });
            return n0;
        }

        /// <summary>Arcana Ride: purple rock mound with a cave mouth and a big gnarled spirit tree.</summary>
        public static Node ArcanaRide()
        {
            var b = new MeshBuilder();
            b.Begin("RockMound");
            Lib.RockBlock(b, new Vector3(0f, 3.2f, 0f), new Vector3(16f, 6.4f, 12f), 2.6f, 0.28f, new Vector3(4f, 2f, 9f), M.Purple, 1.1f);
            b.Begin("CaveMouth");
            b.PushFacade(new Vector3(-3f, 0f, -6.6f), Vector3.back);
            Lib.ExtrudeXY(b, Lib.ArchPts(3f, 0f, 3.6f, 3.6f), -0.02f, 0f, M.Opening);
            Lib.ArchBand(b, 3f, 0f, 3.6f, 3.6f, 0.6f, -0.8f, 0f, M.RockDark);
            b.Pop();
            var t = new MeshBuilder();
            t.Cyl(new Vector3(1f, 5.5f, 1f), 1.4f, 0.8f, 6f, 8, M.Bark);
            var r = new System.Random(41);
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.PI * 2f * i / 6f; Vector3 o = new Vector3(1f, 10.5f, 1f), e = o + new Vector3(Mathf.Cos(a) * 4.5f, 2.5f + (float)r.NextDouble() * 2f, Mathf.Sin(a) * 4.5f);
                t.Member(o, e, 0.45f, M.Bark);
                t.Blob(e, 2.4f, 1.8f, 7, i % 2 == 0 ? M.Purple : M.Pink);
            }
            t.Blob(new Vector3(1f, 13.5f, 1f), 3.2f, 2.4f, 8, M.Pink);
            var n0 = new Node("ArcanaRide");
            n0.Add(new Node("ArcanaRide_Rock", b));
            n0.Add(new Node("ArcanaRide_Tree", t) { Collider = false });
            return n0;
        }

        /// <summary>Little ride car (Petit cars, kids train cars).</summary>
        public static Node PetitCar(string name, M body)
        {
            var b = new MeshBuilder();
            b.BoxMM(-0.75f, 0.25f, -1.2f, 0.75f, 0.8f, 1.2f, body);
            b.BoxMM(-0.65f, 0.8f, -0.2f, 0.65f, 1.0f, 0.9f, M.White);
            b.BoxMM(-0.7f, 0.8f, -1.1f, 0.7f, 1.25f, -0.95f, M.Glass);
            foreach (float x in new[] { -0.75f, 0.75f }) foreach (float z in new[] { -0.8f, 0.8f })
                    b.BoxO(new Vector3(x, 0.25f, z), Vector3.forward, Vector3.up, Vector3.right, new Vector3(0.5f, 0.5f, 0.2f), M.Opening);
            var n0 = new Node(name); n0.Add(new Node(name + "_Body", b) { IsStatic = false });
            return n0;
        }

        /// <summary>Fantasy Dream: little station shed + oval narrow-gauge track + a 3-car train (Train node).</summary>
        public static Node FantasyDream()
        {
            var b = new MeshBuilder();
            var oval = new List<Vector3>();
            for (int i = 0; i < 40; i++) { float a = Mathf.PI * 2f * i / 40f; oval.Add(new Vector3(Mathf.Cos(a) * 11f, 0.08f, Mathf.Sin(a) * 5.5f)); }
            var inner = new List<Vector3>(); var outer = new List<Vector3>();
            b.Begin("Rails");
            for (int i = 0; i < 40; i++)
            {
                Vector3 p = oval[i], q = oval[(i + 1) % 40], t = (q - p).normalized, s = Vector3.Cross(Vector3.up, t);
                inner.Add(p - s * 0.45f); outer.Add(p + s * 0.45f);
                b.BoxO(p + Vector3.up * 0.02f, s, Vector3.up, t, new Vector3(1.4f, 0.1f, 0.25f), M.Timber);
            }
            b.Tube(inner, 0.1f, M.Metal, true); b.Tube(outer, 0.1f, M.Metal, true);
            b.Begin("StationShed");
            b.Push(new Vector3(0f, 0f, -8.5f), 0f);                                                          // station shed on the front side
            b.BoxMM(-4f, 0f, -1.6f, 4f, 0.5f, 1.6f, M.StoneGrey);
            foreach (float x in new[] { -3.6f, 3.6f }) foreach (float z in new[] { -1.2f, 1.2f }) b.BoxMM(x - 0.12f, 0.5f, z - 0.12f, x + 0.12f, 3.2f, z + 0.12f, M.White);
            Lib.GableRoofX(b, -4f, 4f, -1.6f, 1.6f, 3.2f, 1.6f, 0.4f, M.RoofRed, M.White);
            b.Pop();
            var tr = new MeshBuilder();
            M[] cols = { M.Red, M.Yellow, M.RoofBlue };
            for (int c = 0; c < 3; c++)
            {
                float x = -c * 2.6f;
                tr.BoxMM(x - 1.1f, 0.25f, -0.6f, x + 1.1f, 1.0f, 0.6f, cols[c]);
                if (c == 0) { tr.Cyl(new Vector3(x + 0.4f, 1.0f, 0f), 0.4f, 0.4f, 0.9f, 8, M.Navy); tr.BoxMM(x - 1.1f, 1.0f, -0.6f, x - 0.2f, 1.9f, 0.6f, M.Navy); }
                foreach (float wx in new[] { x - 0.7f, x + 0.7f }) foreach (float z in new[] { -0.62f, 0.62f })
                        tr.BoxO(new Vector3(wx, 0.3f, z), Vector3.right, Vector3.up, Vector3.forward, new Vector3(0.5f, 0.5f, 0.1f), M.Opening);
            }
            var n0 = new Node("FantasyDream");
            n0.Add(new Node("FantasyDream_Track", b));
            n0.Add(new Node("Train", tr) { Pos = new Vector3(0f, 0f, -5.5f), IsStatic = false, Note = "drive round the oval" });
            return n0;
        }
    }

    // ------------------------------------------------------------------------------------------------------------------
    // Coaster track along a 3D spline: two tubular rails, spine, ties, supports down to groundY (+ cross bracing when tall).
    // ------------------------------------------------------------------------------------------------------------------
    public static class Track
    {
        public static MeshBuilder Build(List<Vector3> ctrl, bool closed, float gauge, M rail, M sup, float supEvery, float groundY, out List<Vector3> path)
        {
            return Build(ctrl, closed, gauge, rail, sup, supEvery, groundY, null, out path);
        }

        /// <summary>skipSupport(p): true = no support post here (e.g. it would stand on a walkway); the next segment gets it instead.</summary>
        public static MeshBuilder Build(List<Vector3> ctrl, bool closed, float gauge, M rail, M sup, float supEvery, float groundY, Func<Vector3, bool> skipSupport, out List<Vector3> path)
        {
            var b = new MeshBuilder();
            path = Geo.Spline(ctrl, closed, 1.6f);
            int n = path.Count, segs = closed ? n : n - 1;
            float acc = supEvery;
            for (int i = 0; i < segs; i++)
            {
                Vector3 p = path[i], q = path[(i + 1) % n];
                Vector3 t = (q - p); float L = t.magnitude; if (L < 1e-3f) continue; t /= L;
                Vector3 s = Vector3.Cross(Vector3.up, t).normalized, u = Vector3.Cross(t, s);
                Vector3 tp = path[(i + 2) % n] - q, sq = Vector3.Cross(Vector3.up, tp.sqrMagnitude > 1e-6f ? tp.normalized : t).normalized;
                b.Begin("Rails");
                b.Member(p + s * (gauge * 0.5f), q + sq * (gauge * 0.5f), 0.22f, rail);
                b.Member(p - s * (gauge * 0.5f), q - sq * (gauge * 0.5f), 0.22f, rail);
                b.Member(p - u * 0.55f, q - u * 0.55f, 0.42f, rail);
                b.BoxO(p - u * 0.25f, s, u, t, new Vector3(gauge + 0.1f, 0.12f, 0.2f), rail);
                acc += L;
                if (acc >= supEvery && (skipSupport == null || !skipSupport(p)))
                {
                    acc = 0f;
                    Vector3 bot = new Vector3(p.x, groundY, p.z), top = p - u * 0.8f;
                    b.Begin("Supports");
                    float h = top.y - groundY;
                    if (h > 0.5f)
                    {
                        if (h > 10f)
                        {
                            Vector3 sp = s * Mathf.Min(3f, h * 0.12f);
                            b.Member(bot - sp, top, 0.55f, sup); b.Member(bot + sp, top, 0.55f, sup);
                            for (float y = 6f; y < h - 2f; y += 6f) b.Member(Vector3.Lerp(bot - sp, top, y / h), Vector3.Lerp(bot + sp, top, y / h), 0.3f, sup);
                        }
                        else b.Member(bot, top, 0.55f, sup);
                        b.Member(top - s * (gauge * 0.5f), top + s * (gauge * 0.5f), 0.35f, sup);
                    }
                }
            }
            return b;
        }
    }
}
#endif
