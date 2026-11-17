// LotteWorldMagicIslandLayout.cs  (v1 - Magic Island: terrain, water, paths, bridge and where every prefab goes)
// All numbers are module metres (origin = outside face of the indoor main gate, +Z north). Edit a number here and run
// Tools > LotteWorld > Magic Island > Rebuild Scene (keep prefabs), or just move the objects in the scene.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotteWorld.MagicIsland
{
    public static class Layout
    {
        // ---------------- scene root placement (Unity world) ----------------
        public static readonly Vector3 RootPosition = new Vector3(-49.95f, 0.03f, 126.9f);   // indoor LotteWorld_NorthGate x, Wall_02 outer face z
        public const float RootYaw = 0f;

        // ---------------- levels ----------------
        public const float WaterY = -1.2f, BedY = -3.5f;

        // ---------------- outlines (module x, z) ----------------
        /// <summary>Island coast traced from the guide map (smoothed in Island()).</summary>
        public static readonly float[] IslandRaw =
        {
            -204.8f,142.5f, -184.5f,156.0f, -160.9f,176.2f, -141.8f,194.2f, -114.8f,201.0f, -92.2f,202.1f, -74.2f,206.6f, -56.2f,212.2f,
            -38.2f,207.8f, -29.2f,194.2f, -20.2f,183.0f, -2.2f,169.5f, 15.8f,160.5f, 32.6f,142.5f, 34.9f,115.5f, 24.8f,90.8f,
            13.5f,78.4f, -6.8f,80.6f, -29.2f,90.8f, -47.2f,94.1f, -59.0f,88.5f, -60.8f,36.8f, -76.5f,25.5f, -114.8f,6.4f,
            -135.0f,16.5f, -147.4f,32.2f, -148.5f,52.5f, -164.2f,70.5f, -166.5f,84.0f, -184.5f,88.5f, -204.8f,95.2f,
        };
        /// <summary>Inner lagoon west of the castle (Petit cars drive round it).</summary>
        public static readonly float[] LagoonRaw = { -114.8f,135.8f, -96.8f,139.1f, -92.2f,124.5f, -95.6f,105.4f, -111.4f,104.2f, -117.0f,120.0f };
        /// <summary>Lake shore (Seokchon lake, smoothed). Keeps clear of the indoor building's outer walls.</summary>
        public static readonly float[] LakeRaw =
        {
            -14f,24f, 14f,26f, 55f,30f, 80f,60f, 88f,115f, 72f,178f, 30f,232f, -40f,260f, -120f,264f, -195f,247f, -246f,196f, -262f,120f,
            -250f,55f, -215f,8f, -160f,-15f, -110f,-12f, -70f,0f, -40f,12f,
        };
        /// <summary>Lakeside park ground: everything outside the lake, minus the indoor building footprint (south edge follows Wall_00..Wall_04).</summary>
        public static readonly float[] GroundRaw = { -320f,-40f, -96f,-40f, -78f,-33f, -36f,-33f, -36f,0f, 64f,0f, 64f,18f, 160f,18f, 160f,320f, -320f,320f };

        // ---------------- bridge ----------------
        public static readonly Vector2 BridgeShore = new Vector2(0f, 24f), BridgeIsland = new Vector2(-50.6f, 94.1f);
        public const float BridgeWidth = 9f;

        public static List<Vector2> Pts(float[] a) { var o = new List<Vector2>(); for (int i = 0; i < a.Length; i += 2) o.Add(new Vector2(a[i], a[i + 1])); return o; }
        public static List<Vector2> Island() { return Geo.Chaikin(Pts(IslandRaw), 2); }
        public static List<Vector2> Lagoon() { return Geo.Chaikin(Pts(LagoonRaw), 2); }
        public static List<Vector2> Lake() { return Geo.SmoothClosed(Pts(LakeRaw), 8); }
        public static List<Vector2> GroundOuter() { return Pts(GroundRaw); }
        public static float BridgeYaw { get { Vector2 d = BridgeIsland - BridgeShore; return Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; } }

        // ---------------- prefab placements: key, object name, group, x, z, yaw, keep-out radius ----------------
        public class Place
        {
            public string Key, Name, Group; public float X, Z, Yaw, R;
            public Place(string key, string name, string group, float x, float z, float yaw, float r) { Key = key; Name = name; Group = group; X = x; Z = z; Yaw = yaw; R = r; }
        }

        public static readonly List<Place> Places = new List<Place>
        {
            // landmarks
            new Place("Bld_MagicCastle",        "MagicCastle",            "04_Buildings/Castle",      -66f, 135f,    0f, 24f),
            new Place("Ride_GyroDrop",          "GyroDrop_36",            "05_Rides",                -144f, 140f,    0f, 13f),
            new Place("Ride_GyroSwing",         "GyroSwing_34",           "05_Rides",                 -50f, 188f,    0f, 16f),
            new Place("Ride_AtlantisTemple",    "Atlantis_33_Temple",     "05_Rides/Atlantis_33",     13.5f, 133.5f, 90f, 18f),
            new Place("Ride_GyroSpin",          "GyroSpin_39",            "05_Rides",                -145f,  71f,   90f, 13f),
            new Place("Ride_FantasyDream",      "FantasyDream_37",        "05_Rides",                -160f, 116f,    0f, 11f),
            new Place("Ride_EosTower",          "EosTower_42",            "05_Rides/KidsZone",        -92f,  70f,    0f,  6f),
            new Place("Ride_ArcanaRide",        "ArcanaRide_41",          "05_Rides/KidsZone",       -106f,  47f,    0f,  9f),
            new Place("Bld_CometExpress",       "CometExpress_38",        "05_Rides",                -148f,  95f,    0f, 14f),
            new Place("Bld_StoneFort",          "StoneExpress_40_Station","05_Rides/KidsZone",       -124f,  26f,  180f,  9f),
            // gate on the shore
            new Place("Bld_IslandGate",         "IslandGate",             "03_Paths/Bridge",           0f, 21.5f,  -35.6f, 9f),
            // village on the north shore (food 46/47) and the lakeview restaurant
            new Place("Bld_TownHouse_A",        "TownHouse_01",           "04_Buildings/Village",    -120f, 186f,    0f,  7f),
            new Place("Bld_TownHouse_B",        "TownHouse_02",           "04_Buildings/Village",    -108f, 188f,    0f,  7f),
            new Place("Bld_TownHouse_C",        "TownHouse_03_Food46",    "04_Buildings/Village",     -96f, 189f,    0f,  7f),
            new Place("Bld_TownHouse_B",        "TownHouse_04",           "04_Buildings/Village",     -84f, 191f,    0f,  7f),
            new Place("Bld_Restaurant_Lakeview","Restaurant_Lakeview_47", "04_Buildings/Village",    -140f, 170f,  -45f, 14f),
            // west cottages (food 49/50)
            new Place("Bld_Cottage_Red",        "Cottage_50",             "04_Buildings/Cottages",   -192f, 108f,  -90f,  8f),
            new Place("Bld_Cottage_RedTall",    "Cottage_49",             "04_Buildings/Cottages",   -193f, 127f,  -90f,  7f),
            // shops / food
            new Place("Bld_MapleShop",          "MapleStore_21",          "04_Buildings/Shops",       -72f,  62f,   90f, 10f),
            new Place("Bld_CafePavilion",       "Cafe_42",                "04_Buildings/Shops",       -20f, 103f,    0f,  9f),
            new Place("Kiosk_Yellow",           "Kiosk_43_Waffle",        "06_Facilities/Kiosks",     -24f, 120f,   90f,  4f),
            new Place("Kiosk_Red",              "Kiosk_44",               "06_Facilities/Kiosks",     -50f, 162f,    0f,  4f),
            new Place("Kiosk_Blue",             "Kiosk_45",               "06_Facilities/Kiosks",     -82f, 162f,    0f,  4f),
            new Place("Kiosk_Red",              "Kiosk_48_Candy",         "06_Facilities/Kiosks",    -130f, 104f,   90f,  4f),
            new Place("Kiosk_Yellow",           "Kiosk_51",               "06_Facilities/Kiosks",     -88f,  38f,  -90f,  4f),
            new Place("Restroom",               "Restroom_North",         "06_Facilities/Restrooms",  -36f, 170f,    0f,  6f),
            new Place("Restroom",               "Restroom_West",          "06_Facilities/Restrooms", -178f, 150f,  -90f,  6f),
            new Place("Restroom",               "Restroom_Kids",          "06_Facilities/Restrooms", -108f,  84f,    0f,  6f),
            new Place("Prop_LakeFountain",      "LakeFountain_East",      "02_Water",                  56f,  95f,    0f,  0f),
            new Place("Prop_LakeFountain",      "LakeFountain_North",     "02_Water",                -120f, 236f,    0f,  0f),
        };

        public static readonly List<PrefabDef> Prefabs = new List<PrefabDef>
        {
            new PrefabDef("Prop_StreetLamp",        "Props",      Props.StreetLamp),
            new PrefabDef("Prop_Bench",             "Props",      Props.Bench),
            new PrefabDef("Prop_TrashBin",          "Props",      Props.TrashBin),
            new PrefabDef("Prop_ParasolSet",        "Props",      Props.ParasolSet),
            new PrefabDef("Prop_ConeTurret_Small",  "Props",      () => Props.ConeTurret("ConeTurret_Small", 1.2f, 6.5f, 4.2f)),
            new PrefabDef("Prop_MoonBoat",          "Props",      Props.MoonBoat),
            new PrefabDef("Prop_PetitCar_Red",      "Props",      () => Buildings.PetitCar("PetitCar_Red", M.Red)),
            new PrefabDef("Prop_PetitCar_Yellow",   "Props",      () => Buildings.PetitCar("PetitCar_Yellow", M.Yellow)),
            new PrefabDef("Prop_PetitCar_Blue",     "Props",      () => Buildings.PetitCar("PetitCar_Blue", M.RoofBlue)),
            new PrefabDef("Prop_LakeFountain",      "Props",      Props.Fountain),
            new PrefabDef("Tree_Green",             "Landscape",  () => Props.Tree("Tree_Green", M.Leaf, 8f, 3.2f, 11)),
            new PrefabDef("Tree_Cherry",            "Landscape",  () => Props.Tree("Tree_Cherry", M.LeafPink, 7f, 3.4f, 23)),
            new PrefabDef("Tree_Autumn",            "Landscape",  () => Props.Tree("Tree_Autumn", M.Orange, 7.5f, 3.0f, 37)),
            new PrefabDef("Tree_Pine",              "Landscape",  Props.Pine),
            new PrefabDef("Hedge_4m",               "Landscape",  Props.Hedge),
            new PrefabDef("FlowerBed",              "Landscape",  Props.FlowerBed),
            new PrefabDef("Kiosk_Blue",             "Facilities", () => Props.Kiosk("Kiosk_Blue", M.White, M.RoofBlue, M.RoofBlue)),
            new PrefabDef("Kiosk_Red",              "Facilities", () => Props.Kiosk("Kiosk_Red", M.Plaster, M.Red, M.RoofRed)),
            new PrefabDef("Kiosk_Yellow",           "Facilities", () => Props.Kiosk("Kiosk_Yellow", M.White, M.Yellow, M.Orange)),
            new PrefabDef("Restroom",               "Facilities", Props.Restroom),
            new PrefabDef("Bld_TownHouse_A",        "Buildings",  () => Buildings.TownHouse("TownHouse_A", 10f, 12f, 3, M.Plaster, M.RoofBlue, false, true, M.RoofBlueDark, 1)),
            new PrefabDef("Bld_TownHouse_B",        "Buildings",  () => Buildings.TownHouse("TownHouse_B", 9f, 11f, 2, M.Plaster, M.RoofBlueDark, true, false, M.Timber, 2)),
            new PrefabDef("Bld_TownHouse_C",        "Buildings",  () => Buildings.TownHouse("TownHouse_C", 11f, 12f, 3, M.CastleCream, M.RoofBlue, false, false, M.CastlePink, 3)),
            new PrefabDef("Bld_Cottage_Red",        "Buildings",  () => Buildings.TownHouse("Cottage_Red", 12f, 9f, 2, M.Plaster, M.RoofRed, true, false, M.Timber, 4)),
            new PrefabDef("Bld_Cottage_RedTall",    "Buildings",  () => Buildings.TownHouse("Cottage_RedTall", 10f, 10f, 3, M.Plaster, M.RoofRed, true, true, M.Timber, 5)),
            new PrefabDef("Bld_Restaurant_Lakeview","Buildings",  () => Buildings.Restaurant("Restaurant_Lakeview", 24f, 13f, M.CastleCream, M.RoofBlue)),
            new PrefabDef("Bld_CafePavilion",       "Buildings",  () => Buildings.CafePavilion("CafePavilion")),
            new PrefabDef("Bld_CometExpress",       "Buildings",  Buildings.CometExpress),
            new PrefabDef("Bld_MapleShop",          "Buildings",  () => Buildings.ColourShop("MapleShop")),
            new PrefabDef("Bld_StoneFort",          "Buildings",  Buildings.StoneFort),
            new PrefabDef("Bld_IslandGate",         "Buildings",  Buildings.IslandGate),
            new PrefabDef("Bld_MagicCastle",        "Buildings",  Buildings.MagicCastle),
            new PrefabDef("Ride_AtlantisTemple",    "Rides",      Buildings.AtlantisTemple),
            new PrefabDef("Ride_GyroDrop",          "Rides",      Buildings.GyroDrop),
            new PrefabDef("Ride_GyroSwing",         "Rides",      Buildings.GyroSwing),
            new PrefabDef("Ride_GyroSpin",          "Rides",      Buildings.GyroSpin),
            new PrefabDef("Ride_EosTower",          "Rides",      Buildings.EosTower),
            new PrefabDef("Ride_ArcanaRide",        "Rides",      Buildings.ArcanaRide),
            new PrefabDef("Ride_FantasyDream",      "Rides",      Buildings.FantasyDream),
        };

        // ---------------- coaster routes (module x, y, z) ----------------
        public static readonly float[] AtlantisRoute =
        {
            11.5f,2.5f,141f,  1f,2.5f,144.5f,  -6f,3f,148f,  -12f,4f,156f,  -24f,26f,176f,  -26f,27f,183f,  -14f,24f,191f,  6f,6f,182f,
            26f,4f,162f,  42f,6f,126f,  34f,8f,90f,  12f,6f,64f,  -8f,5f,58f,  -16f,6f,70f,  -10f,5f,86f,  -4f,3f,108f,  -6f,2.5f,128f,
            1f,2.5f,136f,  11.5f,2.5f,136.5f,
        };

        // ======================================================================================================================
        // Scene tree
        // ======================================================================================================================
        static readonly List<Vector3> keep = new List<Vector3>();   // keep-out circles (x, z, r) for trees / lamps

        static bool Free(Vector2 p, float pad)
        {
            foreach (Vector3 k in keep) if ((new Vector2(k.x, k.y) - p).magnitude < k.z + pad) return false;
            return true;
        }

        public static Node BuildScene()
        {
            keep.Clear(); PathLines.Clear();
            foreach (Place p in Places) if (p.R > 0f) keep.Add(new Vector3(p.X, p.Z, p.R));

            var root = new Node("MagicIsland") { Note = "scene root: move/rotate this to re-align the whole outdoor map with the indoor main gate" };
            var groups = new Dictionary<string, Node>();
            Func<string, Node> G = null;
            G = path =>
            {
                Node g; if (groups.TryGetValue(path, out g)) return g;
                int i = path.LastIndexOf('/');
                Node parent = i < 0 ? root : G(path.Substring(0, i));
                g = parent.Group(i < 0 ? path : path.Substring(i + 1)); groups[path] = g; return g;
            };
            foreach (string s in new[] { "00_Markers", "01_Terrain", "02_Water", "03_Paths", "04_Buildings", "05_Rides", "06_Facilities", "07_Landscape", "08_Props", "09_Background", "10_Lighting" }) G(s);

            List<Vector2> island = Island(), lagoon = Lagoon(), lake = Lake(), outer = GroundOuter();

            // ---------- markers ----------
            G("00_Markers").Add(new Node("IndoorLink") { Note = "outside face of the indoor main gate (NorthGate opening, x -7.8..7.8)" });
            G("00_Markers").Add(new Node("PlayerSpawn") { Pos = new Vector3(0f, 0f, 6f) });
            G("00_Markers").Add(new Node("PreviewCamera") { Pos = new Vector3(0f, 1.65f, 6f), Euler = new Vector3(4f, -30f, 0f), Note = "camera" });

            // ---------- terrain ----------
            var g1 = G("01_Terrain");
            var gb = new MeshBuilder(); gb.PolyFlat(outer, new List<List<Vector2>> { lake }, 0f, M.Grass);
            g1.Add(new Node("Ground_Lakeside", gb));
            var ib = new MeshBuilder(); ib.PolyFlat(island, new List<List<Vector2>> { lagoon }, 0f, M.Paving);
            g1.Add(new Node("Island_Ground", ib));
            var iw = new MeshBuilder(); iw.Skirt(island, BedY, 0f, M.StoneGrey, true); Coping(iw, island); iw.Skirt(lagoon, BedY, 0f, M.StoneGrey, false); Coping(iw, lagoon);
            g1.Add(new Node("Island_RetainingWall", iw));
            var lw = new MeshBuilder(); lw.Skirt(lake, BedY, 0f, M.StoneGrey, false); Coping(lw, lake);
            g1.Add(new Node("Lakeside_RetainingWall", lw));
            Node lawns = g1.Group("Lawns");
            AddLawn(lawns, "Lawn_KidsZone", Geo.Circle(new Vector2(-104f, 47f), 25f, 17f, 32), null);
            AddLawn(lawns, "Lawn_CastleWest", Geo.Rect(-89f, 140f, -85.5f, 156f), null);
            AddLawn(lawns, "Lawn_CastleEast", Geo.Rect(-46.5f, 120f, -40f, 150f), null);
            AddLawn(lawns, "Lawn_DropRing", Geo.Circle(new Vector2(-144f, 140f), 15f, 15f, 32), new List<List<Vector2>> { Geo.Circle(new Vector2(-144f, 140f), 12.2f, 12.2f, 32) });
            AddLawn(lawns, "Lawn_SwingRing", Geo.Circle(new Vector2(-50f, 188f), 15f, 15f, 32), new List<List<Vector2>> { Geo.Circle(new Vector2(-50f, 188f), 7.2f, 7.2f, 24) });
            AddLawn(lawns, "Lawn_GateWest", Geo.Rect(-18f, 2f, -10f, 16f), null);
            AddLawn(lawns, "Lawn_GateEast", Geo.Rect(10f, 2f, 18f, 16f), null);

            // ---------- water ----------
            var g2 = G("02_Water");
            var ws = new MeshBuilder(); ws.PolyFlat(lake, new List<List<Vector2>> { island }, WaterY, M.Water);
            g2.Add(new Node("Lake_Surface", ws) { Collider = false });
            var ls = new MeshBuilder(); ls.PolyFlat(lagoon, null, WaterY, M.Water);
            g2.Add(new Node("Lagoon_Surface", ls) { Collider = false });
            var bed = new MeshBuilder(); bed.PolyFlat(lake, null, BedY, M.LakeBed);
            g2.Add(new Node("Lake_Bed", bed));

            // ---------- paths, plaza, bridge, dock ----------
            var g3 = G("03_Paths");
            var plaza = new MeshBuilder(); plaza.PolyFlat(Geo.Rect(-10f, 0f, 10f, 22f), null, 0.08f, M.PavingDark); plaza.Skirt(Geo.Rect(-10f, 0f, 10f, 22f), 0f, 0.08f, M.PavingDark, true);
            g3.Add(new Node("Plaza_Gate", plaza));
            var prom = new MeshBuilder(); List<Vector2> pl = Geo.Offset(lake, 3.2f);
            prom.Ribbon(pl, 5f, 0f, 0.04f, M.PavingDark, M.StoneGrey, 0f, true);
            g3.Add(new Node("Promenade_Lakeside", prom));
            Path(g3, "Path_CastleSouth", 6f, -52.5f, 100f, -58f, 107f, -66f, 112f, -66f, 116.5f);
            Path(g3, "Path_CastleNorth", 6f, -66f, 153.5f, -66f, 164f, -66f, 176f);
            Path(g3, "Path_WestLoop", 5f, -66f, 166f, -95f, 174f, -128f, 166f, -158f, 145f, -170f, 128f, -168f, 100f, -150f, 80f, -128f, 76f, -108f, 92f, -86f, 100f, -66f, 110f);
            Path(g3, "Path_East", 5f, -60f, 168f, -36f, 160f, -14f, 140f, -10f, 118f, -28f, 108f, -48f, 102f);
            Path(g3, "Path_KidsZone", 5f, -72f, 98f, -84f, 84f, -90f, 60f, -96f, 30f, -110f, 18f);
            Path(g3, "Path_ToDock", 4f, -168f, 100f, -174f, 88f);
            var road = new MeshBuilder(); List<Vector2> rl = Geo.Offset(lagoon, 4.4f);
            road.Ribbon(rl, 3.6f, 0f, 0.06f, M.Asphalt, M.White, 0.16f, true);
            g3.Add(new Node("Road_PetitCars_35", road));
            BuildBridge(G("03_Paths/Bridge"), G("08_Props/Bridge"));
            BuildDock(g3);

            // ---------- prefab placements ----------
            foreach (Place p in Places) G(p.Group).Add(Node.Inst(p.Key, p.Name, p.X, p.Z, p.Yaw));

            // ---------- coasters and ride extras ----------
            List<Vector3> trackPath;
            var at = Track.Build(Pts3(AtlantisRoute), true, 1.5f, M.PaleBlue, M.PaleBlue, 7f, BedY, out trackPath);
            G("05_Rides/Atlantis_33").Add(new Node("Atlantis_Track", at));
            var kc = new List<Vector3>();
            for (int i = 0; i < 12; i++) { float a = Mathf.PI * 2f * i / 12f; kc.Add(new Vector3(-106f + Mathf.Cos(a) * 20f, 1.6f + 2.6f * (1f + Mathf.Sin(a * 2f + 0.6f)), 48f + Mathf.Sin(a) * 13f)); }
            G("05_Rides/KidsZone").Add(new Node("StoneExpress_40_Track", Track.Build(kc, true, 1.0f, M.Purple, M.Purple, 6f, 0f, out trackPath)));
            Node cars = G("05_Rides").Group("PetitCars_35");
            string[] ck = { "Prop_PetitCar_Red", "Prop_PetitCar_Yellow", "Prop_PetitCar_Blue" };
            for (int i = 0; i < 3; i++)
            {
                Vector2 a = rl[i * 9], c = rl[i * 9 + 1], d = c - a;
                cars.Add(Node.Inst(ck[i], "PetitCar_" + (i + 1), a.x, a.y, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg));
            }

            // ---------- landscape ----------
            var g7 = G("07_Landscape");
            Node it = g7.Group("Trees_Island"), st = g7.Group("Trees_Lakeside"), beds = g7.Group("FlowerBeds"), hed = g7.Group("Hedges");
            var gaps = new List<Vector3> { new Vector3(BridgeIsland.x, BridgeIsland.y, 9f), new Vector3(BridgeShore.x, BridgeShore.y, 9f), new Vector3(-174f, 88f, 4f) };
            var pos = new List<Vector2>(); var nrm = new List<Vector2>();
            Geo.Walk(Geo.Offset(island, -4.5f), 11f, 3f, pos, nrm);
            string[] tk = { "Tree_Green", "Tree_Pine", "Tree_Cherry", "Tree_Green", "Tree_Autumn" };
            int ti = 0;
            for (int i = 0; i < pos.Count; i++)
            {
                if (!Free(pos[i], 3f) || NearGap(pos[i], gaps, 4f) || !Geo.Inside(island, pos[i]) || Geo.DistToPoly(island, pos[i]) < 3f) continue;
                it.Add(Node.Inst(tk[ti % tk.Length], "Tree_" + ti.ToString("000"), pos[i].x, pos[i].y, (ti * 47) % 360)); ti++;
            }
            pos.Clear(); nrm.Clear(); Geo.Walk(Geo.Offset(lake, 10f), 12f, 0f, pos, nrm);
            for (int i = 0; i < pos.Count; i++)
            {
                Vector2 q = pos[i];
                if (!Geo.Inside(outer, q) || (Mathf.Abs(q.x) < 22f && q.y < 32f) || Geo.DistToPoly(outer, q) < 3f) continue;
                st.Add(Node.Inst(i % 3 == 2 ? "Tree_Green" : "Tree_Cherry", "ShoreTree_" + i.ToString("000"), q.x, q.y, (i * 61) % 360));
            }
            float[] fb = { -66f, 166f, -11f, 9f, 11f, 9f, -96f, 104f, -40f, 112f, -150f, 132f, -88f, 48f, -30f, 146f };
            for (int i = 0; i < fb.Length; i += 2) beds.Add(Node.Inst("FlowerBed", "FlowerBed_" + (i / 2 + 1), fb[i], fb[i + 1]));
            float[] hd = { -14f, 1.5f, 90f, -14f, 6.5f, 90f, -14f, 11.5f, 90f, 14f, 1.5f, 90f, 14f, 6.5f, 90f, 14f, 11.5f, 90f, -88f, 112f, 0f, -84f, 112f, 0f, -48f, 112f, 0f, -44f, 112f, 0f };
            for (int i = 0; i < hd.Length; i += 3) hed.Add(Node.Inst("Hedge_4m", "Hedge_" + (i / 3 + 1), hd[i], hd[i + 1], hd[i + 2]));

            // ---------- props ----------
            var g8 = G("08_Props");
            Node lamps = g8.Group("Lamps"), benches = g8.Group("Benches"), bins = g8.Group("TrashBins"), paras = g8.Group("Parasols");
            int li = 0, bi = 0;
            foreach (float[] pth in PathLines)
            {
                var line = new List<Vector2>(); for (int i = 0; i < pth.Length; i += 2) line.Add(new Vector2(pth[i], pth[i + 1]));
                pos.Clear(); nrm.Clear(); WalkOpen(line, 16f, 6f, pos, nrm);
                for (int i = 0; i < pos.Count; i++)
                {
                    float sd = (i % 2 == 0) ? 1f : -1f; Vector2 q = pos[i] + nrm[i] * (sd * 4.2f);
                    if (!Free(q, 1.5f) || !Geo.Inside(island, q) || Geo.Inside(lagoon, q) || Geo.DistToPoly(lagoon, q) < 6.5f) continue;
                    lamps.Add(Node.Inst("Prop_StreetLamp", "Lamp_" + (li++).ToString("000"), q.x, q.y));
                    if (i % 3 == 1)
                    {
                        Vector2 bq = pos[i] - nrm[i] * (sd * 4.0f);
                        if (Free(bq, 1.5f) && Geo.Inside(island, bq) && Geo.DistToPoly(lagoon, bq) > 6.5f)
                        {
                            float yaw = Mathf.Atan2(nrm[i].x * sd, nrm[i].y * sd) * Mathf.Rad2Deg;
                            benches.Add(Node.Inst("Prop_Bench", "Bench_" + bi.ToString("000"), bq.x, bq.y, yaw));
                            Vector2 tq = bq + new Vector2(nrm[i].y, -nrm[i].x) * 1.6f;
                            bins.Add(Node.Inst("Prop_TrashBin", "Bin_" + (bi++).ToString("000"), tq.x, tq.y));
                        }
                    }
                }
            }
            pos.Clear(); nrm.Clear(); Geo.Walk(Geo.Offset(lake, 6f), 24f, 5f, pos, nrm);
            for (int i = 0; i < pos.Count; i++)
                if (Geo.Inside(outer, pos[i]) && Geo.DistToPoly(outer, pos[i]) > 2f) lamps.Add(Node.Inst("Prop_StreetLamp", "Lamp_Shore_" + i.ToString("000"), pos[i].x, pos[i].y));
            foreach (Vector2 q in new[] { new Vector2(-9f, 3f), new Vector2(9f, 3f), new Vector2(-9f, 19f), new Vector2(9f, 19f) }) lamps.Add(Node.Inst("Prop_StreetLamp", "Lamp_Plaza_" + (li++), q.x, q.y));
            float[] ps = { -72f, 170f, -60f, 172f, -56f, 158f, -76f, 158f, -30f, 112f, -12f, 112f, -150f, 158f, -160f, 150f, -84f, 70f, -82f, 52f, -96f, 84f };
            for (int i = 0; i < ps.Length; i += 2) paras.Add(Node.Inst("Prop_ParasolSet", "Parasol_" + (i / 2 + 1), ps[i], ps[i + 1]));
            Node rails = g8.Group("Railings");
            AddRailings(rails, "Railing_Island", Geo.Offset(island, -0.45f), gaps);
            AddRailings(rails, "Railing_Lagoon", Geo.Offset(lagoon, 0.45f), new List<Vector3>());
            AddRailings(rails, "Railing_Lakeside", Geo.Offset(lake, 0.45f), new List<Vector3> { new Vector3(BridgeShore.x, BridgeShore.y, 9f) });

            // ---------- background ----------
            G("09_Background").Add(new Node("City_Skyline", Skyline()) { Collider = false });

            // ---------- lighting ----------
            G("10_Lighting").Add(new Node("Sun") { Euler = new Vector3(42f, -35f, 0f), Light = new LightSpec { Directional = true, Color = new Color(1f, 0.93f, 0.82f), Intensity = 1.25f, Enabled = true } });
            return root;
        }

        // ---------------- helpers ----------------
        static readonly List<float[]> PathLines = new List<float[]>();
        static void Path(Node g, string name, float w, params float[] xz)
        {
            PathLines.Add(xz);
            var line = new List<Vector2>(); for (int i = 0; i < xz.Length; i += 2) line.Add(new Vector2(xz[i], xz[i + 1]));
            List<Vector2> sm = line.Count > 2 ? SmoothOpen(line) : line;
            var b = new MeshBuilder(); b.Ribbon(sm, w, 0f, 0.06f, M.PavingDark, M.StoneGrey, 0.2f);
            g.Add(new Node(name, b));
        }
        static List<Vector2> SmoothOpen(List<Vector2> p)
        {
            var o = new List<Vector2>(); int n = p.Count;
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 p0 = i == 0 ? p[0] * 2f - p[1] : p[i - 1], p3 = i + 2 >= n ? p[n - 1] * 2f - p[n - 2] : p[i + 2];
                int k = Mathf.Max(2, Mathf.CeilToInt((p[i + 1] - p[i]).magnitude / 3f));
                for (int s = 0; s < k; s++) o.Add(Geo.CR(p0, p[i], p[i + 1], p3, (float)s / k));
            }
            o.Add(p[n - 1]);
            return o;
        }
        static void WalkOpen(List<Vector2> p, float step, float start, List<Vector2> pos, List<Vector2> nrm)
        {
            float acc = start;
            for (int i = 0; i < p.Count - 1; i++)
            {
                Vector2 e = p[i + 1] - p[i]; float L = e.magnitude; if (L < 1e-3f) continue; Vector2 t = e / L;
                while (acc < L) { pos.Add(p[i] + t * acc); nrm.Add(new Vector2(t.y, -t.x)); acc += step; }
                acc -= L;
            }
        }
        static bool NearGap(Vector2 q, List<Vector3> gaps, float pad)
        {
            foreach (Vector3 g in gaps) if ((new Vector2(g.x, g.y) - q).magnitude < g.z + pad) return true;
            return false;
        }
        static List<Vector3> Pts3(float[] a) { var o = new List<Vector3>(); for (int i = 0; i < a.Length; i += 3) o.Add(new Vector3(a[i], a[i + 1], a[i + 2])); return o; }

        static void Coping(MeshBuilder b, List<Vector2> poly)
        {
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 p = poly[i], q = poly[(i + 1) % poly.Count];
                b.Member(new Vector3(p.x, 0.12f, p.y), new Vector3(q.x, 0.12f, q.y), 0.5f, M.StoneGrey);
            }
        }

        static void AddLawn(Node g, string name, List<Vector2> poly, List<List<Vector2>> holes)
        {
            var b = new MeshBuilder();
            b.PolyFlat(poly, holes, 0.03f, M.Grass);
            b.Skirt(poly, 0f, 0.03f, M.GrassDark, true);
            g.Add(new Node(name, b));
        }

        static void AddRailings(Node g, string name, List<Vector2> poly, List<Vector3> gaps)
        {
            int n = poly.Count, start = -1;
            for (int i = 0; i < n; i++) if (NearGap(poly[i], gaps, 0f)) { start = i; break; }
            var runs = new List<List<Vector3>>(); List<Vector3> cur = null;
            for (int k = 0; k <= n; k++)
            {
                int i = ((start < 0 ? 0 : start) + k) % n;
                if (NearGap(poly[i], gaps, 0f)) { if (cur != null && cur.Count > 1) runs.Add(cur); cur = null; continue; }
                if (cur == null) cur = new List<Vector3>();
                cur.Add(new Vector3(poly[i].x, 0.25f, poly[i].y));
            }
            if (cur != null && cur.Count > 1) runs.Add(cur);
            for (int r = 0; r < runs.Count; r++)
            {
                var b = new MeshBuilder(); Lib.Railing(b, runs[r], M.White);
                g.Add(new Node(name + "_" + (r + 1).ToString("00"), b));
            }
        }

        /// <summary>Main bridge from the shore gate to the castle plaza: deck, piers, balustrades, cone turret pairs, lamps and the kiosk 41 platform.</summary>
        static void BuildBridge(Node g, Node props)
        {
            Vector2 A = BridgeShore + (BridgeShore - BridgeIsland).normalized * 1.5f, B = BridgeIsland + (BridgeIsland - BridgeShore).normalized * 4f;
            Vector2 d = (B - A).normalized, s = new Vector2(d.y, -d.x);   // s = right-hand side looking towards the island
            float L = (B - A).magnitude, hw = BridgeWidth * 0.5f;
            var b = new MeshBuilder();
            Func<float, float, Vector3> P = (t, o) => { Vector2 q = A + d * t + s * o; return new Vector3(q.x, 0f, q.y); };
            float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            b.Push(P(0f, 0f), yaw);
            b.BoxMM(-hw, -0.9f, 0f, hw, 0.06f, L, M.CastleCream);
            Lib.TileFloor(b, -hw + 0.4f, hw - 0.4f, 0f, L, 0.06f, 1.5f, M.PavingDark, M.Paving);
            for (float t = 6f; t < L - 3f; t += 12f)
            {
                b.BoxMM(-hw - 0.3f, BedY, t - 1.2f, hw + 0.3f, -0.9f, t + 1.2f, M.StoneGrey);
                b.BoxMM(-hw - 0.5f, -1.6f, t - 1.4f, hw + 0.5f, -0.9f, t + 1.4f, M.CastlePink);
            }
            b.Pop();
            float kt = 30f;                                                          // kiosk 41 platform on the west side
            Vector3 pc = P(kt, -hw - 4.5f);
            b.Push(pc, yaw);
            b.BoxMM(-4.5f, -0.9f, -5f, 4.5f, 0.04f, 5f, M.CastleCream);
            b.BoxMM(-4f, BedY, -4f, 4f, -0.9f, 4f, M.StoneGrey);
            Lib.Balustrade(b, new Vector3(-4.3f, 0.04f, -4.8f), new Vector3(-4.3f, 0.04f, 4.8f), M.CastleCream);
            Lib.Balustrade(b, new Vector3(-4.3f, 0.04f, -4.8f), new Vector3(4.5f, 0.04f, -4.8f), M.CastleCream);
            Lib.Balustrade(b, new Vector3(-4.3f, 0.04f, 4.8f), new Vector3(4.5f, 0.04f, 4.8f), M.CastleCream);
            b.Pop();
            // balustrades with a gap at the platform
            foreach (float sd in new[] { -1f, 1f })
            {
                float o = sd * (hw - 0.2f);
                if (sd < 0f) { Lib.Balustrade(b, P(0f, o), P(kt - 5.2f, o), M.CastleCream); Lib.Balustrade(b, P(kt + 5.2f, o), P(L, o), M.CastleCream); }
                else Lib.Balustrade(b, P(0f, o), P(L, o), M.CastleCream);
            }
            g.Add(new Node("Bridge_Main", b));
            Node kiosk = Node.Inst("Kiosk_Blue", "Kiosk_41_Bridge", pc.x, pc.z, yaw - 90f, 1f, 0.04f);
            g.Add(kiosk);
            int k = 0;
            foreach (float t in new[] { 16f, 44f, 60f, L - 6f })
                foreach (float sd in new[] { -1f, 1f })
                {
                    Vector3 q = P(t, sd * (hw + 0.9f));
                    g.Add(Node.Inst("Prop_ConeTurret_Small", "BridgeTurret_" + (k++).ToString("00"), q.x, q.z, yaw));
                }
            k = 0;
            foreach (float t in new[] { 8f, 24f, 38f, 52f, 68f })
                foreach (float sd in new[] { -1f, 1f })
                {
                    Vector3 q = P(t, sd * (hw - 0.6f));
                    props.Add(Node.Inst("Prop_StreetLamp", "BridgeLamp_" + (k++).ToString("00"), q.x, q.z));
                }
        }

        /// <summary>Moon Boat (20) dock on the west shore: timber pier on piles with moored boats.</summary>
        static void BuildDock(Node g)
        {
            var b = new MeshBuilder();
            var line = new List<Vector2> { new Vector2(-174f, 89f), new Vector2(-181f, 74f) };
            b.Ribbon(line, 3.2f, -0.35f, 0.25f, M.Wood, M.Timber, 0f);
            Vector2 d = (line[1] - line[0]).normalized, s = new Vector2(d.y, -d.x);
            for (float t = 0f; t <= 16.6f; t += 3f)
                foreach (float sd in new[] { -1.4f, 1.4f })
                {
                    Vector2 q = line[0] + d * t + s * sd;
                    b.Cyl(new Vector3(q.x, BedY, q.y), 0.18f, 0.18f, 4.3f, 6, M.Timber);
                }
            var rl = new List<Vector3> { new Vector3(line[0].x + s.x * 1.5f, -0.1f, line[0].y + s.y * 1.5f), new Vector3(line[1].x + s.x * 1.5f, -0.1f, line[1].y + s.y * 1.5f) };
            Lib.Railing(b, rl, M.Timber, 1.0f);
            var node = new Node("Dock_MoonBoat_20", b);
            g.Add(node);
            for (int i = 0; i < 3; i++)
            {
                Vector2 q = line[0] + d * (5f + i * 4.5f) - s * 3.2f;
                g.Add(Node.Inst("Prop_MoonBoat", "MoonBoat_" + (i + 1), q.x, q.y, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg + 90f, 1f, WaterY - 0.25f));
            }
        }

        /// <summary>Distant apartment blocks round the lake (no colliders) so the horizon is not empty.</summary>
        static MeshBuilder Skyline()
        {
            var b = new MeshBuilder(); var r = new System.Random(5);
            M[] cols = { M.CityA, M.CityB, M.CityC };
            Action<float, float, float, float, float> tower = (x, z, w, dpt, h) =>
            {
                M c = cols[r.Next(3)];
                b.BoxMM(x - w * 0.5f, 0f, z - dpt * 0.5f, x + w * 0.5f, h, z + dpt * 0.5f, c);
                for (float y = 4f; y < h - 2f; y += 3.2f)
                {
                    b.BoxMM(x - w * 0.5f - 0.05f, y, z - dpt * 0.5f - 0.05f, x + w * 0.5f + 0.05f, y + 1.3f, z - dpt * 0.5f + 0.2f, M.Glass);
                    b.BoxMM(x - w * 0.5f - 0.05f, y, z + dpt * 0.5f - 0.2f, x + w * 0.5f + 0.05f, y + 1.3f, z + dpt * 0.5f + 0.05f, M.Glass);
                }
            };
            for (float x = -300f; x <= 150f; x += 34f) tower(x + (float)r.NextDouble() * 10f, 345f + (float)r.NextDouble() * 20f, 18f, 14f, 40f + (float)r.NextDouble() * 60f);
            for (float z = 40f; z <= 320f; z += 36f)
            {
                tower(-345f - (float)r.NextDouble() * 20f, z, 14f, 18f, 35f + (float)r.NextDouble() * 55f);
                tower(185f + (float)r.NextDouble() * 20f, z, 14f, 18f, 35f + (float)r.NextDouble() * 55f);
            }
            return b;
        }
    }
}
#endif
