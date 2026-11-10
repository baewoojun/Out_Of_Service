// 롯데월드 어드벤처 1층 블록아웃 생성기.
// 가이드맵 픽셀을 미터로 바꿔 배치한다. 메뉴 Tools/LotteWorld/Generate Blockout 을
// 다시 실행하면 씬의 Blockout 루트만 지우고 같은 규칙으로 다시 만든다.
//
// 단위 1 = 1m. 원점 (0,0,0) = 아이스링크 보이드 중심 = 픽셀 (560, 400).
// +X = 동쪽, +Z = 북쪽(픽셀 Y가 작아지는 쪽), +Y = 위.
// X = (px - 560) * MetersPerPixel
// Z = -(py - 400) * MetersPerPixel * IsoDepthFactor
// 가로 픽셀은 MetersPerPixel, 세로 픽셀은 IsoDepthFactor 까지 곱한다.
// 원형 놀이기구는 가로 픽셀 지름만 쓴 진원이다. 보이드 양 끝 반원은 깊이 36m를 지름으로 하는 진원이다.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LotteWorldBlockoutGenerator
{
    const string ScenePath = "Assets/Maps/LotteWorld/Map3_LotteWorld.unity";
    const string MaterialDir = "Assets/Maps/LotteWorld/Materials";
    const string UrpBasicTemplate =
        "Packages/com.unity.render-pipelines.universal/Editor/SceneTemplates/Basic.scenetemplate";
    const string RootName = "Blockout";

    // 이 값 하나로 맵 전체 크기가 바뀐다. 보이드 가로 280px = 아이스링크 약 65m.
    const float MetersPerPixel = 0.23f;
    // 가이드맵은 비스듬한 조감도라 세로가 눌려 있다. 세로(Z)만 늘린다.
    const float IsoDepthFactor = 1.12f; // 아이스링크 36m 기록 기준
    const float OriginPx = 560f;
    const float OriginPy = 400f;

    const float FloorMargin = 20f;
    static float FloorCenterX;
    static float FloorCenterZ;
    static float FloorWidth = 80f;
    static float FloorDepth = 80f;
    const float FloorY = 0f;
    const float FloorThickness = 0.4f;
    const float FloorCellSize = 1f;

    const float B1Y = -6f;
    const float IceThickness = 0.4f;

    // 보이드. 픽셀 280 x 140 스타디움. 양 끝은 깊이 36m를 지름으로 하는 진짜 반원.
    const float MaxNudge = 8f;
    const float VoidCapPx = 70f;
    const float ShaftThickness = 0.7f;
    const float ShaftOuterBias = 0.55f;
    const float RailingHeight = 1.1f;
    const float RailingThickness = 0.35f;
    const float RailingInnerBias = 0.6f;

    // 퍼레이드. 보이드 가장자리 바깥 7m, 폭 6m. 스타디움과 가든스테이지 우회로.
    const float ParadeClearance = 7f;
    const float ParadeWidth = 6f;
    const float ParadeLift = 0.02f;
    const float ParadeThickness = 0.03f;
    const int ParadeWaypointCount = 12;

    const float StageHeight = 1.5f;
    const float StageBackHeight = 12f;
    const float StageBackThickness = 0.45f;
    const float StageBackSign = -1f;

    const float ShipHullHeight = 3f;
    const float ShipSupportThickness = 0.7f;

    const float FlumeChannelWidth = 3f;
    const float FlumeChannelDrop = 1f;

    const float ShutterWidth = 8f;
    const float ShutterHeight = 4f;
    const float ShutterThickness = 0.4f;

    // 정문 에스컬레이터. 상단 픽셀 (700, 500)에서 남쪽으로 B1 까지. 수평 이동 14m.
    const float EscTopPx = 700f;
    const float EscTopPy = 500f;
    const float EscWidth = 4f;
    const float EscRun = 14f;
    const float EscThickness = 0.3f;
    const float EscShutterHeight = 4f;

    const float HallSizeX = 20f;
    const float HallSizeZ = 15f;
    const float HallWallHeight = 4f;
    const float HallWallThickness = 0.4f;
    const float HallJoin = 1f;
    const float GateWidth = 6f;
    const float GateHeight = 3.2f;

    // 기둥·링은 가로세로가 같은 진원. 링 지름 20m.
    const float GyroRadius = 2.4f;
    const float GyroHeight = 18f;
    const float GyroRingRadius = 10f;
    const float GyroRingThickness = 0.45f;

    const float Balcony2Y = 9f;
    const float Balcony3Y = 18f;
    const float BalconyWidth = 6f;
    const float BalconyThickness = 0.3f;
    const float BalconyRailHeight = 1.1f;
    const float BalconyRailThickness = 0.22f;
    const float BalconyCutPadding = 0.4f;
    const float StairBlockWidth = 4f;
    const float StairBlockHeight = 3f;
    const float DomeY = 40f;
    const float DomeMargin = 6f;
    const float DomeThickness = 0.2f;

    const float FrenchRailWidth = 2f;
    const float FrenchRailThickness = 0.4f;
    const float FrenchRailMinY = 12f;
    const float FrenchRailMaxY = 28f;
    const float FrenchLoopRadius = (FrenchRailMaxY - FrenchRailMinY) * 0.5f;
    const float FrenchRailStep = 3f;

    const float BalloonDiameter = 4f;
    const float BalloonBasket = 1.5f;
    const float WallHeight = 40f;
    const float WallThickness = 0.5f;
    const float PlazaSizeX = 12f;
    const float PlazaSizeZ = 8f;
    const float MarkerHover = 0.1f;
    const float GroundSink = 0.02f;

    static readonly Vector3[] BalloonSpots =
    {
        new Vector3(-14f, 33f, 8f),
        new Vector3(16f, 34f, -6f),
        new Vector3(4f, 35f, 12f),
    };

    static readonly Color GrayColor = new Color(0.62f, 0.62f, 0.64f, 1f);
    static readonly Color DarkColor = new Color(0.11f, 0.11f, 0.12f, 1f);
    static readonly Color RedColor = new Color(0.74f, 0.07f, 0.06f, 1f);
    static readonly Color PathColor = new Color(0.93f, 0.76f, 0.18f, 1f);
    static readonly Color RideColor = new Color(0.22f, 0.45f, 0.82f, 1f);
    static readonly Color RailColor = new Color(0.93f, 0.93f, 0.94f, 1f);
    static readonly Color IceColor = new Color(0.55f, 0.82f, 0.95f, 1f);
    static readonly Color FoodColor = new Color(0.95f, 0.46f, 0.12f, 1f);
    static readonly Color ShopColor = new Color(0.56f, 0.30f, 0.76f, 1f);

    enum PlaceKind { Box, Ellipse, Ship, Flume, Carousel, Stage }
    enum Slot { Ride, Food, Shop, Dark }

    struct Place
    {
        public string name;
        public float px, py, spx, spy, height;
        public PlaceKind kind;
        public Slot slot;
        public Place(string name, float px, float py, float spx, float spy, float height, PlaceKind kind, Slot slot)
        {
            this.name = name;
            this.px = px;
            this.py = py;
            this.spx = spx;
            this.spy = spy;
            this.height = height;
            this.kind = kind;
            this.slot = slot;
        }
    }

    // 높이 지정이 없는 놀이기구는 6~10m 안에서 고정했다.
    static readonly Place[] Places =
    {
        new Place("A06_신밧드의모험", 200f, 380f, 70f, 60f, 10f, PlaceKind.Box, Slot.Ride),
        new Place("A07_스페인해적선", 145f, 495f, 90f, 60f, 17.5f, PlaceKind.Ship, Slot.Ride),
        new Place("A08_후룸라이드", 255f, 600f, 140f, 90f, 16f, PlaceKind.Flume, Slot.Ride),
        new Place("A09_회전바구니", 405f, 590f, 70f, 0f, 8f, PlaceKind.Ellipse, Slot.Ride),
        new Place("A10_로티트레인", 685f, 470f, 40f, 30f, 6f, PlaceKind.Box, Slot.Ride),
        new Place("A11_배틀그라운드", 805f, 418f, 50f, 40f, 7f, PlaceKind.Box, Slot.Ride),
        new Place("A12_플라이벤처", 875f, 470f, 180f, 150f, 25f, PlaceKind.Box, Slot.Ride),
        new Place("A13_회전목마", 828f, 330f, 80f, 0f, 12f, PlaceKind.Carousel, Slot.Ride),
        new Place("A14_황야의무법자", 685f, 268f, 50f, 40f, 8f, PlaceKind.Box, Slot.Ride),
        new Place("A15_키즈토리아", 770f, 275f, 70f, 50f, 9f, PlaceKind.Box, Slot.Ride),
        new Place("A16_드래곤와일드슈팅", 910f, 327f, 50f, 40f, 8f, PlaceKind.Box, Slot.Ride),
        new Place("A17_어린이범퍼카", 918f, 250f, 50f, 40f, 7f, PlaceKind.Box, Slot.Ride),
        new Place("A18_햇님달님", 970f, 208f, 40f, 40f, 6f, PlaceKind.Box, Slot.Ride),
        new Place("A19_점핑피쉬", 960f, 158f, 40f, 40f, 7f, PlaceKind.Box, Slot.Ride),
        new Place("A20_언더씨킹덤", 825f, 100f, 80f, 60f, 10f, PlaceKind.Box, Slot.Ride),
        new Place("A21_유레카", 780f, 118f, 40f, 40f, 6f, PlaceKind.Box, Slot.Ride),
        new Place("A22_스윙팡팡", 745f, 135f, 40f, 40f, 7f, PlaceKind.Box, Slot.Ride),
        new Place("A23_매직붕붕카", 680f, 103f, 60f, 40f, 8f, PlaceKind.Box, Slot.Ride),
        new Place("A24_어린이동화극장", 628f, 125f, 60f, 50f, 9f, PlaceKind.Box, Slot.Ride),
        new Place("A25_카트라이더", 573f, 252f, 50f, 40f, 8f, PlaceKind.Box, Slot.Ride),
        new Place("GardenStage", 330f, 470f, 150f, 100f, StageHeight, PlaceKind.Stage, Slot.Ride),
        new Place("X_언더랜드건물", 390f, 130f, 260f, 140f, 15f, PlaceKind.Box, Slot.Dark),

        new Place("F07", 213f, 383f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F08", 300f, 362f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F09", 322f, 378f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F10", 313f, 410f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F11", 278f, 420f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F12", 308f, 453f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F13", 475f, 532f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F14", 446f, 575f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F15", 511f, 560f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F16", 639f, 490f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F17", 828f, 282f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F18", 937f, 207f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F19", 746f, 338f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F20", 665f, 240f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F21", 575f, 278f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),
        new Place("F23", 280f, 268f, 30f, 25f, 5f, PlaceKind.Box, Slot.Food),

        new Place("S04", 432f, 308f, 30f, 25f, 5f, PlaceKind.Box, Slot.Shop),
        new Place("S05", 348f, 333f, 30f, 25f, 5f, PlaceKind.Box, Slot.Shop),
        new Place("S06", 380f, 374f, 30f, 25f, 5f, PlaceKind.Box, Slot.Shop),
        new Place("S07", 575f, 535f, 30f, 25f, 5f, PlaceKind.Box, Slot.Shop),
        new Place("S08", 605f, 505f, 30f, 25f, 5f, PlaceKind.Box, Slot.Shop),
        new Place("S09", 778f, 485f, 30f, 25f, 5f, PlaceKind.Box, Slot.Shop),
        new Place("S10", 718f, 318f, 30f, 25f, 5f, PlaceKind.Box, Slot.Shop),
    };

    struct ShutterPx
    {
        public string name;
        public float px, py;
        public ShutterPx(string name, float px, float py)
        {
            this.name = name;
            this.px = px;
            this.py = py;
        }
    }

    static readonly ShutterPx[] Shutters =
    {
        new ShutterPx("X_언더랜드", 380f, 200f),
        new ShutterPx("X_매직아일랜드", 240f, 300f),
        new ShutterPx("X_원더도어", 165f, 555f),
        new ShutterPx("X_피크닉2층", 500f, 650f),
        new ShutterPx("X_민속박물관", 710f, 600f),
    };

    enum Edge { North, South, East, West }

    struct BarrierSpec
    {
        public string name;
        public Edge edge;
        public float along;
        public BarrierSpec(string name, Edge edge, float along)
        {
            this.name = name;
            this.edge = edge;
            this.along = along;
        }
    }

    static BarrierSpec[] StairBlocks = new BarrierSpec[0];

    enum StaticMode { None, Walkable, Background }

    struct Mats
    {
        public Material gray, dark, red, path, ride, rail, ice, food, shop;
    }

    struct Stadium
    {
        public float straightHalf, radiusX, radiusZ;
    }

    struct Footprint
    {
        public string name;
        public float x, z, sx, sz;
        public Footprint(string name, float x, float z, float sx, float sz)
        {
            this.name = name;
            this.x = x;
            this.z = z;
            this.sx = sx;
            this.sz = sz;
        }
    }

    class Body
    {
        public string name;
        public Slot slot;
        public bool stall;
        public bool kids;
        public bool building;
        public float x, z, sx, sz;
        public float originX, originZ;
        public float moved;
    }

    static readonly string[] KidsNames =
    {
        "A14_황야의무법자", "A15_키즈토리아", "A16_드래곤와일드슈팅", "A17_어린이범퍼카", "A18_햇님달님", "A19_점핑피쉬",
        "A20_언더씨킹덤", "A21_유레카", "A22_스윙팡팡", "A23_매직붕붕카", "A24_어린이동화극장",
    };

    static readonly List<Footprint> Solids = new List<Footprint>();
    static readonly List<Body> BodyList = new List<Body>();
    static readonly Dictionary<string, Body> Bodies = new Dictionary<string, Body>();
    static readonly List<Vector2> ParadePoints = new List<Vector2>();
    static readonly List<Vector2> Waypoints = new List<Vector2>();
    static string OverlapNote = "";

    static float PxToX(float px) => (px - OriginPx) * MetersPerPixel;
    static float PyToZ(float py) => -(py - OriginPy) * MetersPerPixel * IsoDepthFactor;
    static float PxSize(float pixels) => pixels * MetersPerPixel;
    static float PySize(float pixels) => pixels * MetersPerPixel * IsoDepthFactor;
    static Vector2 Map(float px, float py) => new Vector2(PxToX(px), PyToZ(py));

    static float EscTopX => PxToX(EscTopPx);
    static float EscTopZ => PyToZ(EscTopPy);
    static float PlayerSpawnX => PxToX(580f);
    static float PlayerSpawnZ => PyToZ(210f);

    static Stadium VoidShape
    {
        get
        {
            // 세로 140px = 36m 가 반원 지름. 가로는 280px 를 유지하고 직선 구간이 나머지를 맡는다.
            float radius = PySize(140f) * 0.5f;
            return new Stadium
            {
                straightHalf = PxSize(280f) * 0.5f - radius,
                radiusX = radius,
                radiusZ = radius,
            };
        }
    }

    static float VoidWidth => PxSize(280f);
    static float VoidDepth => PySize(140f);
    static float CarouselDiameter => PxSize(80f);

    [MenuItem("Tools/LotteWorld/Generate Blockout", false, 1)]
    public static void Generate()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("플레이 모드에서는 블록아웃을 생성할 수 없습니다.");
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (!open.isDirty || open.path == ScenePath)
                continue;
            if (!TryDiscardDirtyFlag(open))
            {
                Debug.LogError("다른 씬에 저장되지 않은 변경이 있어 중단했습니다: " + open.path);
                return;
            }
            Debug.LogWarning("다른 씬은 저장하지 않고 넘어갔습니다: " + open.path);
        }

        try
        {
            EditorUtility.DisplayProgressBar("LotteWorld", "머티리얼 준비", 0.05f);
            EnsureAssetFolder(MaterialDir);
            Mats mats = EnsureMaterials();

            EditorUtility.DisplayProgressBar("LotteWorld", "씬 열기", 0.15f);
            bool createdScene = OpenOrCreateScene();
            Scene active = SceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                Debug.LogError("대상 씬을 열지 못했습니다: " + ScenePath + " (현재: " + active.path + ")");
                return;
            }

            var root = new GameObject("__BlockoutNew");
            ApplyStatic(root, StaticMode.Walkable);
            try
            {
                EditorUtility.DisplayProgressBar("LotteWorld", "배치", 0.3f);
                PrepareLayout();

                Transform floor1 = CreateGroup(root.transform, "Floor1", StaticMode.Walkable);
                Transform b1 = CreateGroup(root.transform, "B1", StaticMode.Walkable);
                Transform background = CreateGroup(root.transform, "Background", StaticMode.Background);
                Transform blocked = CreateGroup(root.transform, "Blocked", StaticMode.Walkable);
                Transform markers = CreateGroup(root.transform, "Markers", StaticMode.None);

                int floorTiles = BuildFloor(floor1, mats);
                BuildVoidEdge(floor1, mats);
                BuildParade(floor1, mats);
                BuildPlaces(floor1, blocked, mats);
                BuildPlaza(floor1, mats);
                BuildOuterWalls(floor1, mats);
                BuildEscape(floor1, mats);

                EditorUtility.DisplayProgressBar("LotteWorld", "B1 / 배경", 0.7f);
                BuildBlocked(blocked, mats);
                int iceTiles = BuildB1(b1, mats);
                BuildBackground(background, mats);
                BuildMarkers(markers);

                ReplaceOldRoot(root);
                Selection.activeGameObject = root;
                EditorSceneManager.MarkSceneDirty(active);
                EditorSceneManager.SaveScene(active);
                AssetDatabase.SaveAssets();
                FrameView();
                if (createdScene)
                    FrameNewSceneCamera();

                CultureInfo culture = CultureInfo.InvariantCulture;
                Debug.Log(
                    "롯데월드 블록아웃 생성 완료. 바닥 "
                    + FloorWidth.ToString("0.0", culture) + " x " + FloorDepth.ToString("0.0", culture)
                    + "m. 보이드 " + VoidWidth.ToString("0.0", culture) + " x " + VoidDepth.ToString("0.0", culture)
                    + "m. 회전목마 지름 " + CarouselDiameter.ToString("0.0", culture)
                    + "m. 바닥 조각 " + floorTiles + " / 아이스 " + iceTiles
                    + " / 메시 " + (ProBuilderPrimitives.Available ? "ProBuilder" : "Primitive")
                    + ". " + OverlapNote);
            }
            catch (Exception)
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("Tools/LotteWorld/Generate Blockout", true)]
    static bool ValidateGenerate() => !EditorApplication.isPlaying;

    static void PrepareLayout()
    {
        Solids.Clear();
        Waypoints.Clear();
        BuildBodies();
        CacheParade();
        ResolveOverlaps();

        for (int i = 0; i < BodyList.Count; i++)
        {
            Body body = BodyList[i];
            Solids.Add(new Footprint(body.name, body.x, body.z, body.sx, body.sz));
        }

        float rampZ = EscTopZ - EscRun * 0.5f;
        Solids.Add(new Footprint("정문 에스컬레이터", EscTopX, rampZ, EscWidth, EscRun));
        float rampEnd = EscTopZ - EscRun;
        float hallNorth = rampEnd + HallJoin;
        float hallSouth = rampEnd - HallSizeZ;
        Solids.Add(new Footprint("게이트 홀", EscTopX, (hallNorth + hallSouth) * 0.5f, HallSizeX, hallNorth - hallSouth));

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;
        for (int i = 0; i < Solids.Count; i++)
            Include(ref minX, ref maxX, ref minZ, ref maxZ, Solids[i].x, Solids[i].z, Solids[i].sx, Solids[i].sz);

        Include(ref minX, ref maxX, ref minZ, ref maxZ, PlayerSpawnX, PlayerSpawnZ, PlazaSizeX, PlazaSizeZ);

        List<Vector2> outer = SampleStadium(VoidShape, ParadeClearance + ParadeWidth * 0.5f, 16, 8);
        for (int i = 0; i < outer.Count; i++)
            Include(ref minX, ref maxX, ref minZ, ref maxZ, outer[i].x, outer[i].y, 0f, 0f);

        List<Vector2> loop = SampleStadium(VoidShape, ParadeClearance, 16, 8);
        List<Vector2> branch = GardenBranch(loop);
        for (int i = 0; i < branch.Count; i++)
            Include(ref minX, ref maxX, ref minZ, ref maxZ, branch[i].x, branch[i].y, ParadeWidth, ParadeWidth);

        FloorCenterX = (minX + maxX) * 0.5f;
        FloorCenterZ = (minZ + maxZ) * 0.5f;
        FloorWidth = (maxX - minX) + FloorMargin * 2f;
        FloorDepth = (maxZ - minZ) + FloorMargin * 2f;

        StairBlocks = new[]
        {
            new BarrierSpec("MainEscalator", Edge.South, EscTopX),
            new BarrierSpec("Plaza", Edge.North, PlayerSpawnX),
            new BarrierSpec("Underland", Edge.North, PxToX(390f)),
            new BarrierSpec("Sinbad", Edge.North, PxToX(200f)),
        };
        OverlapNote = DescribeResolution();
    }

    static void Include(ref float minX, ref float maxX, ref float minZ, ref float maxZ, float x, float z, float sx, float sz)
    {
        minX = Mathf.Min(minX, x - sx * 0.5f);
        maxX = Mathf.Max(maxX, x + sx * 0.5f);
        minZ = Mathf.Min(minZ, z - sz * 0.5f);
        maxZ = Mathf.Max(maxZ, z + sz * 0.5f);
    }

    static void BuildBodies()
    {
        BodyList.Clear();
        Bodies.Clear();
        for (int i = 0; i < Places.Length; i++)
        {
            Place place = Places[i];
            PlaceSize(place, out float sx, out float sz);
            bool kids = false;
            for (int k = 0; k < KidsNames.Length; k++)
            {
                if (KidsNames[k] == place.name)
                {
                    kids = true;
                    break;
                }
            }
            AddBody(place.name, place.slot, PxToX(place.px), PyToZ(place.py), sx, sz, kids);
        }
        for (int i = 0; i < Shutters.Length; i++)
        {
            ShutterPx shutter = Shutters[i];
            AddBody(shutter.name, Slot.Dark, PxToX(shutter.px), PyToZ(shutter.py), ShutterWidth, ShutterThickness, false, true);
        }
    }

    static void AddBody(string name, Slot slot, float x, float z, float sx, float sz, bool kids, bool passage = false)
    {
        var body = new Body
        {
            name = name,
            slot = slot,
            stall = slot == Slot.Food || slot == Slot.Shop,
            kids = kids,
            building = (slot == Slot.Ride || slot == Slot.Dark) && !passage,
            x = x,
            z = z,
            sx = sx,
            sz = sz,
            originX = x,
            originZ = z,
        };
        BodyList.Add(body);
        Bodies.Add(name, body);
    }

    static void CacheParade()
    {
        ParadePoints.Clear();
        List<Vector2> loop = SampleStadium(VoidShape, ParadeClearance, 24, 12);
        ParadePoints.AddRange(loop);
        ParadePoints.AddRange(GardenBranch(loop));
    }

    static void ResolveOverlaps()
    {
        ShrinkKids();
        TryAttachToFront(Bodies["A11_배틀그라운드"], Bodies["A12_플라이벤처"]);
        TryAttachToFront(Bodies["X_언더랜드"], Bodies["X_언더랜드건물"]);
        ResolveStallsAgainstBuildings();
        ResolveStallsAgainstStalls();
    }

    static void ShrinkKids()
    {
        for (int iter = 0; iter < 24; iter++)
        {
            var hit = new HashSet<Body>();
            for (int i = 0; i < BodyList.Count; i++)
            {
                Body a = BodyList[i];
                if (!a.kids)
                    continue;
                for (int j = i + 1; j < BodyList.Count; j++)
                {
                    Body b = BodyList[j];
                    if (!b.kids || !Overlaps(a, b))
                        continue;
                    hit.Add(a);
                    hit.Add(b);
                }
            }
            if (hit.Count == 0)
                return;
            foreach (Body body in hit)
            {
                body.sx *= 0.8f;
                body.sz *= 0.8f;
            }
        }
    }

    static void ResolveStallsAgainstBuildings()
    {
        for (int pass = 0; pass < 4; pass++)
        {
            bool moved = false;
            for (int i = 0; i < BodyList.Count; i++)
            {
                Body stall = BodyList[i];
                if (!stall.stall)
                    continue;
                Body building = DeepestBuilding(stall);
                if (building == null)
                    continue;
                ShrinkStall(stall);
                building = DeepestBuilding(stall);
                if (building == null)
                    continue;
                float x0 = stall.x;
                float z0 = stall.z;
                float moved0 = stall.moved;
                if (TryAttachToFront(stall, building) && !Overlaps(stall, building))
                    moved = true;
                else
                {
                    stall.x = x0;
                    stall.z = z0;
                    stall.moved = moved0;
                }
            }
            if (!moved)
                return;
        }
    }

    static Body DeepestBuilding(Body stall)
    {
        Body best = null;
        float bestArea = 0f;
        for (int i = 0; i < BodyList.Count; i++)
        {
            Body other = BodyList[i];
            if (!other.building || !Overlaps(stall, other))
                continue;
            float overlapX = (stall.sx + other.sx) * 0.5f - Mathf.Abs(stall.x - other.x);
            float overlapZ = (stall.sz + other.sz) * 0.5f - Mathf.Abs(stall.z - other.z);
            float area = Mathf.Max(0f, overlapX) * Mathf.Max(0f, overlapZ);
            if (area > bestArea)
            {
                bestArea = area;
                best = other;
            }
        }
        return best;
    }

    static void ResolveStallsAgainstStalls()
    {
        for (int pass = 0; pass < 16; pass++)
        {
            bool moved = false;
            for (int i = 0; i < BodyList.Count; i++)
            {
                Body a = BodyList[i];
                if (!a.stall)
                    continue;
                for (int j = i + 1; j < BodyList.Count; j++)
                {
                    Body b = BodyList[j];
                    if (!b.stall || !Overlaps(a, b))
                        continue;
                    ShrinkStall(a);
                    ShrinkStall(b);
                    if (!Overlaps(a, b))
                        continue;
                    Body mover = SmallerStall(a, b);
                    Body other = mover == a ? b : a;
                    float x0 = mover.x;
                    float z0 = mover.z;
                    if (TrySlideToClear(mover, candidate => Overlaps(candidate, other) || OverlapsAnyBuilding(candidate)))
                        moved = true;
                    else
                    {
                        mover.x = x0;
                        mover.z = z0;
                        NoteMove(mover);
                    }
                }
            }
            if (!moved)
                return;
        }
    }

    static void ShrinkStall(Body stall)
    {
        float sx = PxSize(20f);
        float sz = PySize(16f);
        if (stall.sx > sx)
            stall.sx = sx;
        if (stall.sz > sz)
            stall.sz = sz;
    }

    static Body SmallerStall(Body a, Body b)
    {
        float areaA = a.sx * a.sz;
        float areaB = b.sx * b.sz;
        if (areaA < areaB - 0.01f)
            return a;
        if (areaB < areaA - 0.01f)
            return b;
        float da = (NearestParade(new Vector2(a.x, a.z)) - new Vector2(a.x, a.z)).sqrMagnitude;
        float db = (NearestParade(new Vector2(b.x, b.z)) - new Vector2(b.x, b.z)).sqrMagnitude;
        if (Mathf.Abs(da - db) > 0.01f)
            return da > db ? a : b;
        return string.Compare(a.name, b.name, StringComparison.Ordinal) > 0 ? a : b;
    }

    // 건물 앞면(퍼레이드 쪽)에 바깥으로 붙인다. 필요한 이동이 8m 를 넘으면 그대로 둔다.
    static bool TryAttachToFront(Body mover, Body building)
    {
        Vector2 normal = FrontNormal(building);
        float face = normal.x != 0f
            ? building.x + normal.x * building.sx * 0.5f
            : building.z + normal.y * building.sz * 0.5f;
        float half = normal.x != 0f ? mover.sx * 0.5f : mover.sz * 0.5f;
        float target = face + (normal.x != 0f ? normal.x : normal.y) * half;
        float current = normal.x != 0f ? mover.x : mover.z;
        if (Mathf.Abs(target - current) > MaxNudge)
            return false;
        if (normal.x != 0f)
            mover.x = target;
        else
            mover.z = target;
        NoteMove(mover);
        if (mover.moved > MaxNudge + 0.001f)
        {
            mover.x = mover.originX;
            mover.z = mover.originZ;
            mover.moved = 0f;
            return false;
        }
        return true;
    }

    static Vector2 FrontNormal(Body building)
    {
        Vector2 to = NearestParade(new Vector2(building.x, building.z)) - new Vector2(building.x, building.z);
        Vector2 best = Vector2.right;
        float bestDot = float.NegativeInfinity;
        Vector2[] normals = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
        for (int i = 0; i < normals.Length; i++)
        {
            float dot = Vector2.Dot(normals[i], to);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = normals[i];
            }
        }
        return best;
    }

    static bool TrySlideToClear(Body mover, Func<Body, bool> blocked)
    {
        Vector2 origin = new Vector2(mover.originX, mover.originZ);
        Vector2 pos = new Vector2(mover.x, mover.z);
        Vector2 dir = NearestParade(pos) - pos;
        if (dir.sqrMagnitude < 0.0001f)
            return false;
        dir.Normalize();
        float maxT = MaxTravel(pos, origin, dir, MaxNudge);
        if (maxT <= 0.02f)
            return false;

        bool Clears(float t)
        {
            mover.x = pos.x + dir.x * t;
            mover.z = pos.y + dir.y * t;
            return !blocked(mover);
        }

        if (!Clears(maxT))
        {
            mover.x = pos.x;
            mover.z = pos.y;
            return false;
        }

        float lo = 0f;
        float hi = maxT;
        for (int i = 0; i < 22; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (Clears(mid))
                hi = mid;
            else
                lo = mid;
        }
        mover.x = pos.x + dir.x * hi;
        mover.z = pos.y + dir.y * hi;
        NoteMove(mover);
        return true;
    }

    static float MaxTravel(Vector2 pos, Vector2 origin, Vector2 dir, float radius)
    {
        Vector2 fromHome = pos - origin;
        float along = Vector2.Dot(fromHome, dir);
        float disc = along * along - fromHome.sqrMagnitude + radius * radius;
        if (disc < 0f)
            return 0f;
        return Mathf.Max(0f, -along + Mathf.Sqrt(disc));
    }

    static void NoteMove(Body body)
    {
        body.moved = Vector2.Distance(new Vector2(body.x, body.z), new Vector2(body.originX, body.originZ));
    }

    static Vector2 NearestParade(Vector2 point)
    {
        Vector2 best = ParadePoints[0];
        float bestD = float.MaxValue;
        for (int i = 0; i < ParadePoints.Count; i++)
        {
            float d = (ParadePoints[i] - point).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = ParadePoints[i];
            }
        }
        return best;
    }

    static bool Overlaps(Body a, Body b)
    {
        return Mathf.Abs(a.x - b.x) < (a.sx + b.sx) * 0.5f - 0.05f
            && Mathf.Abs(a.z - b.z) < (a.sz + b.sz) * 0.5f - 0.05f;
    }

    static bool OverlapsAnyBuilding(Body stall)
    {
        for (int i = 0; i < BodyList.Count; i++)
        {
            Body other = BodyList[i];
            if (other.building && Overlaps(stall, other))
                return true;
        }
        return false;
    }

    static string DescribeResolution()
    {
        var moves = new List<string>();
        for (int i = 0; i < BodyList.Count; i++)
        {
            Body body = BodyList[i];
            if (body.moved > 0.05f)
                moves.Add(body.name + " " + body.moved.ToString("0.0", CultureInfo.InvariantCulture) + "m");
        }

        var pairs = new List<string>();
        for (int i = 0; i < Solids.Count; i++)
        {
            for (int j = i + 1; j < Solids.Count; j++)
            {
                Footprint a = Solids[i];
                Footprint b = Solids[j];
                if ((a.name == "정문 에스컬레이터" && b.name == "게이트 홀")
                    || (a.name == "게이트 홀" && b.name == "정문 에스컬레이터"))
                    continue;
                if (Mathf.Abs(a.x - b.x) < (a.sx + b.sx) * 0.5f - 0.05f
                    && Mathf.Abs(a.z - b.z) < (a.sz + b.sz) * 0.5f - 0.05f)
                    pairs.Add(a.name + "↔" + b.name);
            }
        }

        string moveText = moves.Count == 0
            ? "이동한 구조물 없음."
            : "이동: " + string.Join(", ", moves) + ".";
        string leftText = pairs.Count == 0
            ? "해결 못 한 겹침 없음."
            : "해결 못 한 겹침: " + string.Join(", ", pairs) + ".";
        return moveText + " " + leftText;
    }

    static void ReplaceOldRoot(GameObject fresh)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != fresh && roots[i].name == RootName)
                UnityEngine.Object.DestroyImmediate(roots[i]);
        }
        fresh.name = RootName;
    }

    static bool TryDiscardDirtyFlag(Scene scene)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo clear = typeof(EditorSceneManager).GetMethod(
            "ClearSceneDirtiness", flags, null, new[] { typeof(Scene) }, null);
        if (clear != null)
        {
            clear.Invoke(null, new object[] { scene });
            return !scene.isDirty;
        }
        MethodInfo onScene = typeof(Scene).GetMethod("ClearDirtiness", flags);
        if (onScene != null)
        {
            object boxed = scene;
            onScene.Invoke(boxed, null);
            return !scene.isDirty;
        }
        return false;
    }

    static bool OpenOrCreateScene()
    {
        EnsureAssetFolder("Assets/Maps/LotteWorld");
        if (File.Exists(ScenePath))
        {
            Scene current = SceneManager.GetActiveScene();
            if (current.path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return false;
        }

        var template = AssetDatabase.LoadAssetAtPath<UnityEditor.SceneTemplate.SceneTemplateAsset>(UrpBasicTemplate);
        if (template != null)
            UnityEditor.SceneTemplate.SceneTemplateService.Instantiate(template, false, ScenePath);
        else
        {
            Scene created = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(created, ScenePath);
        }

        Scene active = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path))
            EditorSceneManager.SaveScene(active, ScenePath);
        FrameNewSceneCamera();
        return true;
    }

    static void FrameNewSceneCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 target = new Vector3(FloorCenterX, 1f, FloorCenterZ);
            cam.transform.position = new Vector3(FloorCenterX, 14f, FloorCenterZ - 49f);
            cam.transform.rotation = Quaternion.LookRotation(target - cam.transform.position, Vector3.up);
            cam.farClipPlane = 600f;
        }
    }

    static void FrameView()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null)
            return;
        view.Frame(new Bounds(
            new Vector3(FloorCenterX, DomeY * 0.35f, FloorCenterZ),
            new Vector3(FloorWidth * 0.92f, DomeY + 8f, FloorDepth * 0.92f)), false);
        view.Repaint();
    }

    static int BuildFloor(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "Floor", StaticMode.Walkable);
        bool[,] solid = MakeGrid(out float originX, out float originZ, out float cellX, out float cellZ, out int nx, out int nz);
        Stadium stadium = VoidShape;
        for (int z = 0; z < nz; z++)
        {
            for (int x = 0; x < nx; x++)
            {
                CellCenter(originX, originZ, cellX, cellZ, x, z, out float cx, out float cz);
                solid[x, z] = !InStadium(cx, cz, stadium)
                    && !InFlumeChannel(cx, cz)
                    && !InEscalatorSlot(cx, cz);
            }
        }
        float centerY = FloorY - FloorThickness * 0.5f;
        return EmitGrid(group, "Floor_", originX, originZ, cellX, cellZ, solid, centerY, FloorThickness, mats.gray, StaticMode.Walkable);
    }

    static void BuildVoidEdge(Transform parent, Mats mats)
    {
        Transform shaft = CreateGroup(parent, "VoidShaft", StaticMode.Walkable);
        Transform railing = CreateGroup(parent, "VoidRailing", StaticMode.Walkable);
        float floorBottom = FloorY - FloorThickness;
        float shaftTop = floorBottom + 0.06f;
        float shaftBottom = B1Y - 0.02f;
        float shaftHeight = shaftTop - shaftBottom;
        float shaftY = (shaftTop + shaftBottom) * 0.5f;
        float shaftBias = ShaftOuterBias - ShaftThickness * 0.5f;
        List<Vector2> shaftLine = SampleStadium(VoidShape, shaftBias, 18, 10);
        int index = 0;
        EmitRibbon(shaft, "VoidShaft_", shaftLine, true, ShaftThickness, shaftHeight, shaftY, mats.gray, StaticMode.Walkable, null, ref index);

        float railY = FloorY + RailingHeight * 0.5f - 0.02f;
        float railBias = RailingInnerBias + RailingThickness * 0.5f;
        List<Vector2> railLine = SampleStadium(VoidShape, railBias, 18, 10);
        index = 0;
        EmitRibbon(railing, "VoidRailing_", railLine, true, RailingThickness, RailingHeight, railY, mats.rail, StaticMode.Walkable, null, ref index);
    }

    static void BuildParade(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "ParadeLoop", StaticMode.Walkable);
        float y = FloorY + ParadeLift - ParadeThickness * 0.5f;
        List<Vector2> loop = SampleStadium(VoidShape, ParadeClearance, 20, 12);
        int index = 0;
        EmitRibbon(group, "Parade_", loop, true, ParadeWidth, ParadeThickness, y, mats.path, StaticMode.Walkable, ParadeHitsStage, ref index);

        List<Vector2> branch = GardenBranch(loop);
        Transform branchGroup = CreateGroup(parent, "GardenStageParade", StaticMode.Walkable);
        EmitRibbon(branchGroup, "GardenParade_", branch, false, ParadeWidth, ParadeThickness, y, mats.path, StaticMode.Walkable, null, ref index);

        Waypoints.Clear();
        int start = ClosestIndex(loop, Map(400f, 310f));
        for (int i = 0; i < ParadeWaypointCount; i++)
        {
            int idx = (start + i * loop.Count / ParadeWaypointCount) % loop.Count;
            Waypoints.Add(loop[idx]);
        }
        for (int i = 1; i < branch.Count; i++)
            Waypoints.Add(branch[i]);
    }

    static bool ParadeHitsStage(Vector2 mid)
    {
        Place stage = FindPlace("GardenStage");
        Placed(stage, out float x, out float z, out float sx, out float sz);
        return InRect(mid.x, mid.y, x, z, sx + 1f, sz + 1f);
    }

    // 보이드 왼쪽 위 (400, 310)에서 출발해 (300, 420), (175, 575)를 지나
    // 가든스테이지 서쪽을 빠져 남쪽을 돌아 큰 순환로로 돌아온다.
    static List<Vector2> GardenBranch(List<Vector2> loop)
    {
        Place stage = FindPlace("GardenStage");
        Placed(stage, out float cx, out float cz, out float sx, out float sz);
        Vector2 north = Map(300f, 420f);
        Vector2 southWest = Map(175f, 575f);
        float clear = ParadeWidth * 0.5f;
        Vector2 westGate = new Vector2(cx - sx * 0.5f - clear, north.y);
        Vector2 pastSouth = new Vector2(cx + sx * 0.5f + clear, southWest.y);
        int start = ClosestIndex(loop, Map(400f, 310f));
        int end = ClosestIndex(loop, pastSouth);
        return new List<Vector2>
        {
            loop[start],
            north,
            westGate,
            southWest,
            pastSouth,
            loop[end],
        };
    }

    static void BuildPlaces(Transform floor1, Transform blocked, Mats mats)
    {
        Transform food = CreateGroup(floor1, "Restaurants", StaticMode.Walkable);
        Transform shops = CreateGroup(floor1, "GiftShops", StaticMode.Walkable);
        for (int i = 0; i < Places.Length; i++)
        {
            Place place = Places[i];
            Transform parent = floor1;
            if (place.slot == Slot.Food)
                parent = food;
            else if (place.slot == Slot.Shop)
                parent = shops;
            else if (place.slot == Slot.Dark)
                parent = blocked;
            Material mat = SlotMaterial(place.slot, mats);
            switch (place.kind)
            {
                case PlaceKind.Ship:
                    BuildShip(parent, place, mat);
                    break;
                case PlaceKind.Flume:
                    BuildFlume(parent, place, mat);
                    break;
                case PlaceKind.Carousel:
                    BuildCarousel(parent, place, mat);
                    break;
                case PlaceKind.Stage:
                    BuildStage(parent, place, mat);
                    break;
                case PlaceKind.Ellipse:
                    BuildEllipseRide(parent, place, mat);
                    break;
                default:
                    Placed(place, out float x, out float z, out float sx, out float sz);
                    AddLabel(CreateStandingBox(parent, place.name, x, z, sx, place.height, sz, mat, StaticMode.Walkable));
                    break;
            }
        }
    }

    static void BuildStage(Transform parent, Place place, Material mat)
    {
        Transform group = CreateGroup(parent, place.name, StaticMode.Walkable);
        AddLabel(group.gameObject);
        Placed(place, out float x, out float z, out float sx, out float sz);
        CreateStandingBox(group, "Platform", x, z, sx, StageHeight, sz, mat, StaticMode.Walkable);
        float wallX = x + StageBackSign * sx * 0.5f;
        CreateStandingBox(group, "BackWall", wallX, z, StageBackThickness, StageBackHeight, sz, mat, StaticMode.Walkable);
    }

    static void BuildShip(Transform parent, Place place, Material mat)
    {
        Transform group = CreateGroup(parent, place.name, StaticMode.Walkable);
        AddLabel(group.gameObject);
        Placed(place, out float x, out float z, out float sx, out float sz);
        CreateStandingBox(group, "Base", x, z, sx, 0.4f, sz, mat, StaticMode.Walkable);
        float supportH = place.height;
        float offset = sz * 0.28f;
        CreateStandingBox(group, "Support_N", x, z + offset, sx * 0.55f, supportH, ShipSupportThickness, mat, StaticMode.Walkable);
        CreateStandingBox(group, "Support_S", x, z - offset, sx * 0.55f, supportH, ShipSupportThickness, mat, StaticMode.Walkable);
        // 선체는 수평으로 정지. 회전하지 않는다.
        CreateBox(group, "Hull", new Vector3(x, FloorY + supportH, z),
            new Vector3(sx * 0.72f, ShipHullHeight, sz * 0.42f), Quaternion.identity, mat, StaticMode.Walkable);
    }

    static void BuildFlume(Transform parent, Place place, Material mat)
    {
        Transform group = CreateGroup(parent, place.name, StaticMode.Walkable);
        AddLabel(group.gameObject);
        Placed(place, out float x, out float z, out float sx, out float sz);
        float side = (sz - FlumeChannelWidth) * 0.5f;
        if (side > 0.3f)
        {
            float north = z + FlumeChannelWidth * 0.5f + side * 0.5f;
            float south = z - FlumeChannelWidth * 0.5f - side * 0.5f;
            CreateStandingBox(group, "Shell_N", x, north, sx, place.height, side, mat, StaticMode.Walkable);
            CreateStandingBox(group, "Shell_S", x, south, sx, place.height, side, mat, StaticMode.Walkable);
        }
        float channelTop = FloorY - FlumeChannelDrop;
        float thick = 0.35f;
        CreateBox(group, "Channel", new Vector3(x, channelTop - thick * 0.5f, z),
            new Vector3(sx, thick, FlumeChannelWidth), Quaternion.identity, mat, StaticMode.Walkable);
    }

    static void BuildCarousel(Transform parent, Place place, Material mat)
    {
        Transform group = CreateGroup(parent, place.name, StaticMode.Walkable);
        AddLabel(group.gameObject);
        Placed(place, out float x, out float z, out float sx, out float sz);
        float rx = sx * 0.5f;
        float rz = sz * 0.5f;
        CreateCylinder(group, "Disc", new Vector3(x, FloorY + 0.2f, z), rx, rz, 0.4f, Quaternion.identity, mat, StaticMode.Walkable);
        CreateCylinder(group, "Pole", new Vector3(x, FloorY + 5.7f, z), 0.45f, 0.45f, 10.6f, Quaternion.identity, mat, StaticMode.Walkable);
        float roofThick = 2f;
        float roofCenter = place.height - roofThick * 0.5f;
        CreateCylinder(group, "Roof", new Vector3(x, FloorY + roofCenter, z), rx * 1.08f, rz * 1.08f, roofThick, Quaternion.identity, mat, StaticMode.Walkable);
    }

    static void BuildEllipseRide(Transform parent, Place place, Material mat)
    {
        Transform group = CreateGroup(parent, place.name, StaticMode.Walkable);
        AddLabel(group.gameObject);
        Placed(place, out float x, out float z, out float sx, out float sz);
        CreateCylinder(group, "Disc", new Vector3(x, FloorY + 0.2f, z), sx * 0.5f, sz * 0.5f, 0.4f, Quaternion.identity, mat, StaticMode.Walkable);
        CreateCylinder(group, "Hub", new Vector3(x, FloorY + place.height * 0.5f, z), 1.3f, 1.3f, place.height, Quaternion.identity, mat, StaticMode.Walkable);
    }

    static void BuildPlaza(Transform parent, Mats mats)
    {
        AddLabel(CreateBox(parent, "MeetingPlaza", new Vector3(PlayerSpawnX, FloorY + 0.03f, PlayerSpawnZ),
            new Vector3(PlazaSizeX, 0.04f, PlazaSizeZ), Quaternion.identity, mats.gray, StaticMode.Walkable));
    }

    static void BuildOuterWalls(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "OuterWalls", StaticMode.Walkable);
        FloorBounds(out float minX, out float maxX, out float minZ, out float maxZ);
        float top = FloorY + WallHeight;
        float bottom = FloorY - GroundSink;
        float height = top - bottom;
        float y = (bottom + top) * 0.5f;
        float t = WallThickness;
        float embed = 0.05f;
        float northZ = maxZ + t * 0.5f - embed;
        float southZ = minZ - t * 0.5f + embed;
        float spanX0 = minX - t;
        float spanX1 = maxX + t;
        CreateBox(group, "Wall_North", new Vector3((spanX0 + spanX1) * 0.5f, y, northZ),
            new Vector3(spanX1 - spanX0, height, t), Quaternion.identity, mats.gray, StaticMode.Walkable);

        float gap0 = EscTopX - EscWidth * 0.5f - 0.5f;
        float gap1 = EscTopX + EscWidth * 0.5f + 0.5f;
        CreateBox(group, "Wall_South_W", new Vector3((spanX0 + gap0) * 0.5f, y, southZ),
            new Vector3(gap0 - spanX0, height, t), Quaternion.identity, mats.gray, StaticMode.Walkable);
        CreateBox(group, "Wall_South_E", new Vector3((gap1 + spanX1) * 0.5f, y, southZ),
            new Vector3(spanX1 - gap1, height, t), Quaternion.identity, mats.gray, StaticMode.Walkable);

        float westX = minX - t * 0.5f + embed;
        float eastX = maxX + t * 0.5f - embed;
        CreateBox(group, "Wall_West", new Vector3(westX, y, (minZ + maxZ) * 0.5f),
            new Vector3(t, height, maxZ - minZ), Quaternion.identity, mats.gray, StaticMode.Walkable);
        CreateBox(group, "Wall_East", new Vector3(eastX, y, (minZ + maxZ) * 0.5f),
            new Vector3(t, height, maxZ - minZ), Quaternion.identity, mats.gray, StaticMode.Walkable);
    }

    static void BuildEscape(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "Escape", StaticMode.Walkable);
        Vector3 p0 = new Vector3(EscTopX, FloorY, EscTopZ);
        Vector3 p1 = new Vector3(EscTopX, B1Y, EscTopZ - EscRun);
        Vector3 delta = p1 - p0;
        float length = delta.magnitude;
        Vector3 forward = delta / length;
        Vector3 up = Vector3.Cross(Vector3.right, forward);
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;
        up.Normalize();
        if (up.y < 0f)
            up = -up;

        Vector3 walkCenter = (p0 + p1) * 0.5f;
        Vector3 boxCenter = walkCenter - up * (EscThickness * 0.5f);
        CreateBox(group, "EscapeRamp", boxCenter, new Vector3(EscWidth, EscThickness, length),
            Quaternion.LookRotation(forward, up), mats.red, StaticMode.Walkable);

        FloorBounds(out _, out _, out float minZ, out _);
        float curbZ1 = Mathf.Min(EscTopZ, minZ);
        float curbLen = EscTopZ - curbZ1;
        if (curbLen > 0.2f)
        {
            float curbY = FloorY + RailingHeight * 0.5f - GroundSink * 0.5f;
            float curbH = RailingHeight + GroundSink;
            float curbT = 0.25f;
            float z = (EscTopZ + curbZ1) * 0.5f;
            float left = EscTopX - EscWidth * 0.5f - curbT * 0.5f;
            float right = EscTopX + EscWidth * 0.5f + curbT * 0.5f;
            CreateBox(group, "EscapeCurb_L", new Vector3(left, curbY, z),
                new Vector3(curbT, curbH, curbLen), Quaternion.identity, mats.rail, StaticMode.Walkable);
            CreateBox(group, "EscapeCurb_R", new Vector3(right, curbY, z),
                new Vector3(curbT, curbH, curbLen), Quaternion.identity, mats.rail, StaticMode.Walkable);
        }

        float shutterT = 0.35f;
        float shutterZ = EscTopZ + shutterT * 0.5f + 0.08f;
        CreateStandingBox(group, "EscapeShutter", EscTopX, shutterZ, EscWidth + 0.8f, EscShutterHeight, shutterT, mats.red, StaticMode.None, true);
    }

    static void BuildBlocked(Transform parent, Mats mats)
    {
        for (int i = 0; i < Shutters.Length; i++)
        {
            ShutterPx shutter = Shutters[i];
            Body body = Bodies[shutter.name];
            AddLabel(CreateStandingBox(parent, shutter.name, body.x, body.z,
                body.sx, ShutterHeight, body.sz, mats.dark, StaticMode.Walkable));
        }
    }

    static int BuildB1(Transform parent, Mats mats)
    {
        Transform ice = CreateGroup(parent, "IceRink", StaticMode.Walkable);
        bool[,] solid = MakeGrid(out float originX, out float originZ, out float cellX, out float cellZ, out int nx, out int nz);
        Stadium stadium = VoidShape;
        for (int z = 0; z < nz; z++)
        {
            for (int x = 0; x < nx; x++)
            {
                CellCenter(originX, originZ, cellX, cellZ, x, z, out float cx, out float cz);
                solid[x, z] = InStadium(cx, cz, stadium);
            }
        }
        int tiles = EmitGrid(ice, "Ice_", originX, originZ, cellX, cellZ, solid, B1Y - IceThickness * 0.5f, IceThickness, mats.ice, StaticMode.Walkable);

        Transform gyro = CreateGroup(parent, "Gyroscope", StaticMode.Walkable);
        CreateCylinder(gyro, "Gyroscope_Column", new Vector3(0f, B1Y + GyroHeight * 0.5f, 0f),
            GyroRadius, GyroRadius, GyroHeight, Quaternion.identity, mats.gray, StaticMode.Walkable);
        float ringY = B1Y + GyroRingRadius;
        CreateCylinder(gyro, "Gyroscope_Ring", new Vector3(0f, ringY, 0f),
            GyroRingRadius, GyroRingRadius, GyroRingThickness, Quaternion.Euler(0f, 0f, 90f), mats.gray, StaticMode.Walkable);

        BuildGateHall(parent, mats);
        return tiles;
    }

    static void BuildGateHall(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "GateHall", StaticMode.Walkable);
        float rampEndZ = EscTopZ - EscRun;
        float northEdge = rampEndZ + HallJoin;
        float southEdge = rampEndZ - HallSizeZ;
        float sizeZ = northEdge - southEdge;
        float centerZ = (northEdge + southEdge) * 0.5f;
        float floorThick = 0.4f;
        CreateBox(group, "GateHall_Floor", new Vector3(EscTopX, B1Y - floorThick * 0.5f, centerZ),
            new Vector3(HallSizeX, floorThick, sizeZ), Quaternion.identity, mats.gray, StaticMode.Walkable);

        float wallH = HallWallHeight + GroundSink;
        float wallY = B1Y - GroundSink + wallH * 0.5f;
        float t = HallWallThickness;
        float halfX = HallSizeX * 0.5f;
        AddWallRunX(group, "GateHall_Wall_N", EscTopX, rampEndZ, HallSizeX, wallH, wallY, t, EscWidth + 0.4f, mats.gray);
        AddWallRunX(group, "GateHall_Wall_S", EscTopX, southEdge, HallSizeX, wallH, wallY, t, GateWidth, mats.gray);
        CreateBox(group, "GateHall_Wall_W", new Vector3(EscTopX - halfX, wallY, centerZ),
            new Vector3(t, wallH, sizeZ), Quaternion.identity, mats.gray, StaticMode.Walkable);
        CreateBox(group, "GateHall_Wall_E", new Vector3(EscTopX + halfX, wallY, centerZ),
            new Vector3(t, wallH, sizeZ), Quaternion.identity, mats.gray, StaticMode.Walkable);
        CreateBox(group, "MainGate", new Vector3(EscTopX, B1Y + GateHeight * 0.5f, southEdge),
            new Vector3(GateWidth, GateHeight, t), Quaternion.identity, mats.red, StaticMode.None, true, true);
    }

    static void AddWallRunX(Transform parent, string name, float centerX, float z, float length, float height, float y, float thickness, float gap, Material mat)
    {
        float minX = centerX - length * 0.5f;
        float maxX = centerX + length * 0.5f;
        float gap0 = centerX - gap * 0.5f;
        float gap1 = centerX + gap * 0.5f;
        if (gap0 - minX > 0.05f)
        {
            CreateBox(parent, name + "_W", new Vector3((minX + gap0) * 0.5f, y, z),
                new Vector3(gap0 - minX, height, thickness), Quaternion.identity, mat, StaticMode.Walkable);
        }
        if (maxX - gap1 > 0.05f)
        {
            CreateBox(parent, name + "_E", new Vector3((gap1 + maxX) * 0.5f, y, z),
                new Vector3(maxX - gap1, height, thickness), Quaternion.identity, mat, StaticMode.Walkable);
        }
    }

    static void BuildBackground(Transform parent, Mats mats)
    {
        BuildBalcony(parent, "Balcony2F", Balcony2Y, mats);
        BuildBalcony(parent, "Balcony3F", Balcony3Y, mats);
        BuildFrenchRevolution(parent, mats);
        BuildBalloons(parent, mats);
        CreateBox(parent, "GlassDome", new Vector3(FloorCenterX, DomeY, FloorCenterZ),
            new Vector3(FloorWidth + DomeMargin * 2f, DomeThickness, FloorDepth + DomeMargin * 2f),
            Quaternion.identity, mats.gray, StaticMode.Background, false, false);
    }

    static void BuildBalcony(Transform parent, string name, float levelY, Mats mats)
    {
        Transform group = CreateGroup(parent, name, StaticMode.Background);
        bool[,] solid = MakeGrid(out float originX, out float originZ, out float cellX, out float cellZ, out int nx, out int nz);
        float innerW = FloorWidth - BalconyWidth * 2f;
        float innerD = FloorDepth - BalconyWidth * 2f;
        float slabMin = levelY - BalconyThickness;
        float slabMax = levelY;
        for (int z = 0; z < nz; z++)
        {
            for (int x = 0; x < nx; x++)
            {
                CellCenter(originX, originZ, cellX, cellZ, x, z, out float cx, out float cz);
                bool ring = !InRect(cx, cz, FloorCenterX, FloorCenterZ, innerW, innerD);
                solid[x, z] = ring && !BlocksBalcony(cx, cz, slabMin, slabMax) && !FrenchRailCutsSlab(cx, cz, slabMin, slabMax);
            }
        }
        EmitGrid(group, name + "_", originX, originZ, cellX, cellZ, solid, levelY - BalconyThickness * 0.5f, BalconyThickness, mats.gray, StaticMode.Background);

        FloorBounds(out float minX, out float maxX, out float minZ, out float maxZ);
        float innerMinX = minX + BalconyWidth;
        float innerMaxX = maxX - BalconyWidth;
        float innerMinZ = minZ + BalconyWidth;
        float innerMaxZ = maxZ - BalconyWidth;
        float railY = levelY + BalconyRailHeight * 0.5f - 0.02f;
        EmitEdgeRail(group, name + "_Rail_N", Edge.North, true, innerMaxZ, minX, maxX, railY, slabMin, levelY + BalconyRailHeight, mats);
        EmitEdgeRail(group, name + "_Rail_S", Edge.South, true, innerMinZ, minX, maxX, railY, slabMin, levelY + BalconyRailHeight, mats);
        EmitEdgeRail(group, name + "_Rail_W", Edge.West, false, innerMinX, innerMinZ, innerMaxZ, railY, slabMin, levelY + BalconyRailHeight, mats);
        EmitEdgeRail(group, name + "_Rail_E", Edge.East, false, innerMaxX, innerMinZ, innerMaxZ, railY, slabMin, levelY + BalconyRailHeight, mats);

        for (int i = 0; i < StairBlocks.Length; i++)
        {
            BarrierSpec spec = StairBlocks[i];
            BalconyPoint(spec.edge, spec.along, out float x, out float z);
            if (BlocksBalcony(x, z, slabMin, levelY + 1f))
                continue;
            Vector3 size = spec.edge == Edge.North || spec.edge == Edge.South
                ? new Vector3(StairBlockWidth, StairBlockHeight, BalconyWidth)
                : new Vector3(BalconyWidth, StairBlockHeight, StairBlockWidth);
            CreateBox(group, name + "_StairBlock_" + spec.name, new Vector3(x, levelY + StairBlockHeight * 0.5f - 0.02f, z),
                size, Quaternion.identity, mats.dark, StaticMode.Background);
        }
    }

    static void BuildMarkers(Transform parent)
    {
        CreateMarker(parent, "PlayerSpawn", PlayerSpawnX, PlayerSpawnZ);
        for (int i = 0; i < Waypoints.Count; i++)
            CreateMarker(parent, "ParadeWaypoint_" + i.ToString("00"), Waypoints[i].x, Waypoints[i].y);

        Place stage = FindPlace("GardenStage");
        Place ship = FindPlace("A07_스페인해적선");
        Place sinbad = FindPlace("A06_신밧드의모험");
        Place castle = FindPlace("A12_플라이벤처");
        Place kids = FindPlace("A15_키즈토리아");
        Place flume = FindPlace("A08_후룸라이드");
        Place carousel = FindPlace("A13_회전목마");
        HideBeside(parent, "HideSpot_01", stage, StageBackSign, 0f);
        HideBeside(parent, "HideSpot_02", ship, -1f, 0f);
        HideBeside(parent, "HideSpot_03", ship, 1f, 0f);
        HideBeside(parent, "HideSpot_04", sinbad, 0f, 1f);
        HideBeside(parent, "HideSpot_05", castle, 1f, 0f);
        HideBeside(parent, "HideSpot_06", kids, 0f, 1f);
        HideBeside(parent, "HideSpot_07", flume, 0f, -1f);
        HideBeside(parent, "HideSpot_08", carousel, 1f, 0f);
    }

    static void HideBeside(Transform parent, string name, Place place, float signX, float signZ)
    {
        Placed(place, out float cx, out float cz, out float sx, out float sz);
        float x = cx + signX * (sx * 0.5f + 1.2f);
        float z = cz + signZ * (sz * 0.5f + 1.2f);
        CreateMarker(parent, name, x, z);
    }

    static bool BlocksBalcony(float x, float z, float y0, float y1)
    {
        float pad = BalconyCutPadding;
        for (int i = 0; i < Places.Length; i++)
        {
            Place place = Places[i];
            float top = place.kind == PlaceKind.Ship ? place.height + ShipHullHeight * 0.5f : place.height;
            if (place.kind == PlaceKind.Stage)
                top = StageBackHeight;
            if (!Band(y0, y1, FloorY, FloorY + top))
                continue;

            Placed(place, out float cx, out float cz, out float sx, out float sz);
            if (place.kind == PlaceKind.Stage)
            {
                if (InRect(x, z, cx + StageBackSign * sx * 0.5f, cz, StageBackThickness + pad, sz + pad))
                    return true;
                continue;
            }
            if (place.kind == PlaceKind.Ellipse || place.kind == PlaceKind.Carousel)
            {
                if (InEllipse(x, z, cx, cz, sx * 0.5f + pad, sz * 0.5f + pad))
                    return true;
            }
            else if (InRect(x, z, cx, cz, sx + pad, sz + pad))
            {
                return true;
            }
        }
        return false;
    }

    static bool Band(float a0, float a1, float b0, float b1) => a0 < b1 && b0 < a1;

    static void BuildFrenchRevolution(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "FrenchRevolution", StaticMode.Background);
        Vector2 a = Map(780f, 40f);
        Vector2 b = Map(1000f, 250f);
        Vector2 c = Map(960f, 450f);
        float len0 = Vector2.Distance(a, b);
        float len1 = Vector2.Distance(b, c);
        float total = Mathf.Max(0.01f, len0 + len1);
        int index = 0;
        index = EmitFrenchSpan(group, a, b, 0f, len0, total, mats, index);
        EmitFrenchSpan(group, b, c, len0, len1, total, mats, index);
        BuildFrenchLoop(group, b, (b - a).normalized, mats);
    }

    static int EmitFrenchSpan(Transform parent, Vector2 from, Vector2 to, float along0, float length, float total, Mats mats, int index)
    {
        if (length < 0.2f)
            return index;
        Vector2 dir = (to - from) / length;
        float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        int steps = Mathf.Max(1, Mathf.CeilToInt(length / FrenchRailStep));
        float step = length / steps;
        for (int i = 0; i < steps; i++)
        {
            float along = along0 + (i + 0.5f) * step;
            float u = Mathf.Clamp01(along / total);
            float y = FrenchRailMinY + (FrenchRailMaxY - FrenchRailMinY) * Mathf.Sin(Mathf.PI * u);
            Vector2 p = from + dir * ((i + 0.5f) * step);
            CreateBox(parent, "FrenchRail_" + index.ToString("00"), new Vector3(p.x, y, p.y),
                new Vector3(FrenchRailWidth, FrenchRailThickness, step + 0.15f), Quaternion.Euler(0f, yaw, 0f),
                mats.gray, StaticMode.Background, true, false, false);
            index++;
        }
        return index;
    }

    static void BuildFrenchLoop(Transform parent, Vector2 at, Vector2 dir, Mats mats)
    {
        Vector3 forward = new Vector3(dir.x, 0f, dir.y);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 center = new Vector3(at.x, (FrenchRailMinY + FrenchRailMaxY) * 0.5f, at.y);
        int steps = 16;
        for (int i = 0; i < steps; i++)
        {
            float t0 = i * Mathf.PI * 2f / steps;
            float t1 = (i + 1) * Mathf.PI * 2f / steps;
            Vector3 p0 = FrenchLoopPoint(center, forward, t0);
            Vector3 p1 = FrenchLoopPoint(center, forward, t1);
            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 tangent = p1 - p0;
            float len = tangent.magnitude;
            if (len < 0.05f)
                continue;
            CreateBox(parent, "FrenchLoop_" + i.ToString("00"), mid,
                new Vector3(FrenchRailWidth, FrenchRailThickness, len + 0.2f),
                Quaternion.LookRotation(tangent.normalized, Vector3.up),
                mats.gray, StaticMode.Background, true, false, false);
        }
    }

    static Vector3 FrenchLoopPoint(Vector3 center, Vector3 forward, float theta)
    {
        return center + forward * (Mathf.Cos(theta) * FrenchLoopRadius) + Vector3.up * (Mathf.Sin(theta) * FrenchLoopRadius);
    }

    static void BuildBalloons(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "BalloonFlight", StaticMode.Background);
        for (int i = 0; i < BalloonSpots.Length; i++)
        {
            Vector3 spot = BalloonSpots[i];
            string id = (i + 1).ToString("00");
            CreateSphere(group, "Balloon_" + id, spot, BalloonDiameter, mats.gray, StaticMode.Background);
            float basketY = spot.y - BalloonDiameter * 0.5f - BalloonBasket * 0.5f;
            CreateBox(group, "BalloonBasket_" + id, new Vector3(spot.x, basketY, spot.z),
                new Vector3(BalloonBasket, BalloonBasket, BalloonBasket), Quaternion.identity,
                mats.gray, StaticMode.Background, true, false, false);
        }
    }

    static bool FrenchRailCutsSlab(float x, float z, float y0, float y1)
    {
        Vector2 a = Map(780f, 40f);
        Vector2 b = Map(1000f, 250f);
        Vector2 c = Map(960f, 450f);
        float len0 = Vector2.Distance(a, b);
        float len1 = Vector2.Distance(b, c);
        float total = Mathf.Max(0.01f, len0 + len1);
        if (ClosestOnSegment(x, z, a, b, out float along0, out float dist0) && dist0 <= FrenchRailWidth * 0.5f + 0.75f)
        {
            float y = FrenchRailMinY + (FrenchRailMaxY - FrenchRailMinY) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(along0 / total));
            if (y0 < y + 0.4f && y1 > y - 0.4f)
                return true;
        }
        if (ClosestOnSegment(x, z, b, c, out float along1, out float dist1) && dist1 <= FrenchRailWidth * 0.5f + 0.75f)
        {
            float y = FrenchRailMinY + (FrenchRailMaxY - FrenchRailMinY) * Mathf.Sin(Mathf.PI * Mathf.Clamp01((len0 + along1) / total));
            if (y0 < y + 0.4f && y1 > y - 0.4f)
                return true;
        }

        float dx = x - b.x;
        float dz = z - b.y;
        if (dx * dx + dz * dz <= (FrenchLoopRadius + 1.2f) * (FrenchLoopRadius + 1.2f)
            && y1 >= FrenchRailMinY && y0 <= FrenchRailMaxY)
            return true;
        return false;
    }

    static bool ClosestOnSegment(float x, float z, Vector2 a, Vector2 b, out float along, out float distance)
    {
        Vector2 v = b - a;
        float len2 = v.sqrMagnitude;
        float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(((x - a.x) * v.x + (z - a.y) * v.y) / len2);
        Vector2 c = a + v * t;
        float dx = x - c.x;
        float dz = z - c.y;
        along = Mathf.Sqrt(len2) * t;
        distance = Mathf.Sqrt(dx * dx + dz * dz);
        return true;
    }

    static List<Vector2> SampleStadium(Stadium stadium, float outward, int capSteps, int straightSteps)
    {
        var pts = new List<Vector2>();
        capSteps = Mathf.Max(4, capSteps);
        straightSteps = Mathf.Max(2, straightSteps);
        for (int i = 0; i <= capSteps; i++)
        {
            float t = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, i / (float)capSteps);
            AddCap(pts, stadium.straightHalf, t, stadium, outward);
        }
        for (int i = 1; i <= straightSteps; i++)
        {
            float u = i / (float)straightSteps;
            float x = Mathf.Lerp(stadium.straightHalf, -stadium.straightHalf, u);
            pts.Add(new Vector2(x, stadium.radiusZ + outward));
        }
        for (int i = 1; i <= capSteps; i++)
        {
            float t = Mathf.Lerp(Mathf.PI * 0.5f, Mathf.PI * 1.5f, i / (float)capSteps);
            AddCap(pts, -stadium.straightHalf, t, stadium, outward);
        }
        for (int i = 1; i < straightSteps; i++)
        {
            float u = i / (float)straightSteps;
            float x = Mathf.Lerp(-stadium.straightHalf, stadium.straightHalf, u);
            pts.Add(new Vector2(x, -(stadium.radiusZ + outward)));
        }
        return pts;
    }

    static void AddCap(List<Vector2> pts, float centerX, float theta, Stadium stadium, float outward)
    {
        float c = Mathf.Cos(theta);
        float s = Mathf.Sin(theta);
        var normal = new Vector2(c / stadium.radiusX, s / stadium.radiusZ).normalized;
        pts.Add(new Vector2(centerX + stadium.radiusX * c, stadium.radiusZ * s) + normal * outward);
    }

    static void EmitRibbon(Transform parent, string prefix, IList<Vector2> pts, bool closed, float width, float height, float y, Material mat, StaticMode mode, Func<Vector2, bool> skip, ref int index)
    {
        int n = pts.Count;
        int last = closed ? n : n - 1;
        for (int i = 0; i < last; i++)
        {
            Vector2 a = pts[i];
            Vector2 b = pts[(i + 1) % n];
            Vector2 mid = (a + b) * 0.5f;
            if (skip != null && skip(mid))
                continue;
            float dx = b.x - a.x;
            float dz = b.y - a.y;
            float len = Mathf.Sqrt(dx * dx + dz * dz);
            if (len < 0.05f)
                continue;
            float yaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
            CreateBox(parent, prefix + index.ToString("00"), new Vector3(mid.x, y, mid.y),
                new Vector3(width, height, len + 0.12f), Quaternion.Euler(0f, yaw, 0f), mat, mode);
            index++;
        }
    }

    static int ClosestIndex(List<Vector2> pts, Vector2 target)
    {
        int best = 0;
        float bestD = float.MaxValue;
        for (int i = 0; i < pts.Count; i++)
        {
            float d = (pts[i] - target).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }
        return best;
    }

    static bool InStadium(float x, float z, Stadium stadium)
    {
        float ax = Mathf.Abs(x);
        if (ax <= stadium.straightHalf)
            return Mathf.Abs(z) <= stadium.radiusZ;
        float dx = (ax - stadium.straightHalf) / stadium.radiusX;
        float dz = z / stadium.radiusZ;
        return dx * dx + dz * dz <= 1f;
    }

    static bool InEllipse(float x, float z, float cx, float cz, float rx, float rz)
    {
        float dx = (x - cx) / Mathf.Max(0.001f, rx);
        float dz = (z - cz) / Mathf.Max(0.001f, rz);
        return dx * dx + dz * dz <= 1f;
    }

    static bool InRect(float x, float z, float cx, float cz, float sizeX, float sizeZ)
    {
        return Mathf.Abs(x - cx) <= sizeX * 0.5f && Mathf.Abs(z - cz) <= sizeZ * 0.5f;
    }

    static bool InFlumeChannel(float x, float z)
    {
        Place flume = FindPlace("A08_후룸라이드");
        Placed(flume, out float fx, out float fz, out float sx, out _);
        return InRect(x, z, fx, fz, sx, FlumeChannelWidth);
    }

    static bool InEscalatorSlot(float x, float z)
    {
        float half = EscWidth * 0.5f;
        return Mathf.Abs(x - EscTopX) <= half && z <= EscTopZ && z >= EscTopZ - EscRun;
    }

    static Place FindPlace(string name)
    {
        for (int i = 0; i < Places.Length; i++)
        {
            if (Places[i].name == name)
                return Places[i];
        }
        throw new InvalidOperationException("배치를 찾지 못했습니다: " + name);
    }

    static void PlaceSize(Place place, out float sx, out float sz)
    {
        if (place.kind == PlaceKind.Ellipse || place.kind == PlaceKind.Carousel)
        {
            float diameter = PxSize(place.spx);
            sx = diameter;
            sz = diameter;
            return;
        }
        sx = PxSize(place.spx);
        sz = PySize(place.spy);
    }

    static void Placed(Place place, out float x, out float z, out float sx, out float sz)
    {
        Body body = Bodies[place.name];
        x = body.x;
        z = body.z;
        sx = body.sx;
        sz = body.sz;
    }

    static Material SlotMaterial(Slot slot, Mats mats)
    {
        switch (slot)
        {
            case Slot.Food: return mats.food;
            case Slot.Shop: return mats.shop;
            case Slot.Dark: return mats.dark;
            default: return mats.ride;
        }
    }

    static bool[,] MakeGrid(out float originX, out float originZ, out float cellX, out float cellZ, out int nx, out int nz)
    {
        float cell = Mathf.Max(0.25f, FloorCellSize);
        nx = Mathf.Max(1, Mathf.RoundToInt(FloorWidth / cell));
        nz = Mathf.Max(1, Mathf.RoundToInt(FloorDepth / cell));
        cellX = FloorWidth / nx;
        cellZ = FloorDepth / nz;
        FloorBounds(out originX, out _, out originZ, out _);
        return new bool[nx, nz];
    }

    static void FloorBounds(out float minX, out float maxX, out float minZ, out float maxZ)
    {
        minX = FloorCenterX - FloorWidth * 0.5f;
        maxX = FloorCenterX + FloorWidth * 0.5f;
        minZ = FloorCenterZ - FloorDepth * 0.5f;
        maxZ = FloorCenterZ + FloorDepth * 0.5f;
    }

    static void CellCenter(float originX, float originZ, float cellX, float cellZ, int x, int z, out float cx, out float cz)
    {
        cx = originX + (x + 0.5f) * cellX;
        cz = originZ + (z + 0.5f) * cellZ;
    }

    static int EmitGrid(Transform parent, string prefix, float originX, float originZ, float cellX, float cellZ, bool[,] solid, float centerY, float thickness, Material mat, StaticMode mode)
    {
        int nx = solid.GetLength(0);
        int nz = solid.GetLength(1);
        bool[,] used = new bool[nx, nz];
        int count = 0;
        for (int z = 0; z < nz; z++)
        {
            for (int x = 0; x < nx; x++)
            {
                if (!solid[x, z] || used[x, z])
                    continue;
                int w = 1;
                while (x + w < nx && solid[x + w, z] && !used[x + w, z])
                    w++;
                int h = 1;
                while (z + h < nz)
                {
                    bool row = true;
                    for (int i = 0; i < w; i++)
                    {
                        if (!solid[x + i, z + h] || used[x + i, z + h])
                        {
                            row = false;
                            break;
                        }
                    }
                    if (!row)
                        break;
                    h++;
                }
                for (int zz = z; zz < z + h; zz++)
                {
                    for (int xx = x; xx < x + w; xx++)
                        used[xx, zz] = true;
                }

                float sx = w * cellX;
                float sz = h * cellZ;
                float cx = originX + x * cellX + sx * 0.5f;
                float cz = originZ + z * cellZ + sz * 0.5f;
                CreateBox(parent, prefix + count.ToString("000"), new Vector3(cx, centerY, cz),
                    new Vector3(sx, thickness, sz), Quaternion.identity, mat, mode);
                count++;
            }
        }
        return count;
    }

    static void EmitEdgeRail(Transform parent, string name, Edge edge, bool alongX, float fixedCoord, float from, float to, float y, float blockY0, float blockY1, Mats mats)
    {
        float span = to - from;
        if (span <= 0.1f)
            return;
        int count = Mathf.Max(1, Mathf.RoundToInt(span));
        float cell = span / count;
        int run = -1;
        int piece = 0;
        for (int i = 0; i <= count; i++)
        {
            bool solid = false;
            if (i < count)
            {
                float c = from + (i + 0.5f) * cell;
                float x = alongX ? c : fixedCoord;
                float z = alongX ? fixedCoord : c;
                solid = !BlocksBalcony(x, z, blockY0, blockY1) && !StairGap(edge, c);
            }
            if (solid)
            {
                if (run < 0)
                    run = i;
            }
            else if (run >= 0)
            {
                EmitRailPiece(parent, name, alongX, fixedCoord, from, cell, run, i, y, mats, piece);
                piece++;
                run = -1;
            }
        }
    }

    static void EmitRailPiece(Transform parent, string name, bool alongX, float fixedCoord, float from, float cell, int run, int end, float y, Mats mats, int piece)
    {
        float a = from + run * cell;
        float b = from + end * cell;
        float mid = (a + b) * 0.5f;
        float len = b - a;
        Vector3 pos = alongX ? new Vector3(mid, y, fixedCoord) : new Vector3(fixedCoord, y, mid);
        Vector3 size = alongX
            ? new Vector3(len, BalconyRailHeight, BalconyRailThickness)
            : new Vector3(BalconyRailThickness, BalconyRailHeight, len);
        CreateBox(parent, name + "_" + piece.ToString("00"), pos, size, Quaternion.identity, mats.rail, StaticMode.Background);
    }

    static bool StairGap(Edge edge, float along)
    {
        for (int i = 0; i < StairBlocks.Length; i++)
        {
            if (StairBlocks[i].edge == edge && Mathf.Abs(StairBlocks[i].along - along) <= StairBlockWidth * 0.5f)
                return true;
        }
        return false;
    }

    static void BalconyPoint(Edge edge, float along, out float x, out float z)
    {
        FloorBounds(out float minX, out float maxX, out float minZ, out float maxZ);
        switch (edge)
        {
            case Edge.North:
                x = along;
                z = maxZ - BalconyWidth * 0.5f;
                break;
            case Edge.South:
                x = along;
                z = minZ + BalconyWidth * 0.5f;
                break;
            case Edge.East:
                x = maxX - BalconyWidth * 0.5f;
                z = along;
                break;
            default:
                x = minX + BalconyWidth * 0.5f;
                z = along;
                break;
        }
    }

    static Transform CreateGroup(Transform parent, string name, StaticMode mode)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        ApplyStatic(go, mode);
        return go.transform;
    }

    static void CreateMarker(Transform parent, string name, float x, float z)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, FloorY + MarkerHover, z);
    }

    static Vector3 StandingCenter(float x, float height, float z)
    {
        float h = height + GroundSink;
        return new Vector3(x, FloorY - GroundSink + h * 0.5f, z);
    }

    static Vector3 StandingSize(float sx, float height, float sz) => new Vector3(sx, height + GroundSink, sz);

    static GameObject CreateStandingBox(Transform parent, string name, float x, float z, float sx, float height, float sz, Material mat, StaticMode mode, bool door = false)
    {
        return CreateBox(parent, name, StandingCenter(x, height, z), StandingSize(sx, height, sz), Quaternion.identity, mat, mode, true, door);
    }

    static void AddLabel(GameObject go)
    {
        if (go == null)
            return;
        BlockoutLabel label = go.GetComponent<BlockoutLabel>();
        if (label == null)
            label = go.AddComponent<BlockoutLabel>();
        label.Set(go.name);
    }

    static GameObject CreateBox(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rotation, Material mat, StaticMode mode, bool castShadows = true, bool door = false, bool withCollider = true)
    {
        return CreatePrimitive(parent, name, center, size, rotation, mat, mode, false, castShadows, door, withCollider);
    }

    static GameObject CreateCylinder(Transform parent, string name, Vector3 center, float radiusX, float radiusZ, float height, Quaternion rotation, Material mat, StaticMode mode)
    {
        return CreatePrimitive(parent, name, center, new Vector3(radiusX * 2f, height, radiusZ * 2f), rotation, mat, mode, true, true, false, true);
    }

    static GameObject CreateSphere(Transform parent, string name, Vector3 center, float diameter, Material mat, StaticMode mode)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        return FinishPrimitive(go, parent, name, center, Vector3.one * diameter, Quaternion.identity, mat, mode, true, false, false);
    }

    static GameObject CreatePrimitive(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rotation, Material mat, StaticMode mode, bool cylinder, bool castShadows, bool door, bool withCollider)
    {
        GameObject go = cylinder ? CreateUnitCylinder() : CreateUnitCube();
        return FinishPrimitive(go, parent, name, center, size, rotation, mat, mode, castShadows, door, withCollider);
    }

    static GameObject FinishPrimitive(GameObject go, Transform parent, string name, Vector3 center, Vector3 size, Quaternion rotation, Material mat, StaticMode mode, bool castShadows, bool door, bool withCollider)
    {
        MeshFilter filter = go.GetComponent<MeshFilter>();
        Bounds bounds = filter.sharedMesh.bounds;
        Collider existing = go.GetComponent<Collider>();
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
        BoxCollider box = null;
        if (withCollider || door)
        {
            box = go.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }

        go.name = name;
        go.transform.SetPositionAndRotation(center, rotation);
        Vector3 meshSize = bounds.size;
        go.transform.localScale = new Vector3(
            size.x / Mathf.Max(0.0001f, meshSize.x),
            size.y / Mathf.Max(0.0001f, meshSize.y),
            size.z / Mathf.Max(0.0001f, meshSize.z));
        go.transform.SetParent(parent, true);

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        if (door)
        {
            NavMeshObstacle obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = box.center;
            obstacle.size = box.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
        }

        ApplyStatic(go, mode);
        return go;
    }

    static GameObject CreateUnitCube()
    {
        GameObject proBuilder = ProBuilderPrimitives.CreateCube();
        if (proBuilder != null)
            return proBuilder;
        return GameObject.CreatePrimitive(PrimitiveType.Cube);
    }

    static GameObject CreateUnitCylinder()
    {
        GameObject proBuilder = ProBuilderPrimitives.CreateCylinder();
        if (proBuilder != null)
            return proBuilder;
        return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    }

    static void ApplyStatic(GameObject go, StaticMode mode)
    {
        if (mode == StaticMode.None)
        {
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            return;
        }

        StaticEditorFlags flags = StaticEditorFlags.BatchingStatic
            | StaticEditorFlags.OccluderStatic
            | StaticEditorFlags.OccludeeStatic
            | StaticEditorFlags.ContributeGI
            | StaticEditorFlags.ReflectionProbeStatic;
        if (mode == StaticMode.Walkable)
#pragma warning disable CS0618
            flags |= StaticEditorFlags.NavigationStatic;
#pragma warning restore CS0618
        GameObjectUtility.SetStaticEditorFlags(go, flags);
    }

    static Mats EnsureMaterials()
    {
        return new Mats
        {
            gray = EnsureMaterial("BO_Gray", GrayColor),
            dark = EnsureMaterial("BO_Dark", DarkColor),
            red = EnsureMaterial("BO_Red", RedColor),
            path = EnsureMaterial("BO_Path", PathColor),
            ride = EnsureMaterial("BO_Ride", RideColor),
            rail = EnsureMaterial("BO_Rail", RailColor),
            ice = EnsureMaterial("BO_Ice", IceColor),
            food = EnsureMaterial("BO_Food", FoodColor),
            shop = EnsureMaterial("BO_Shop", ShopColor),
        };
    }

    static Material EnsureMaterial(string name, Color color)
    {
        string path = MaterialDir + "/" + name + ".mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("URP/Lit 셰이더를 찾지 못했습니다.");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.08f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureAssetFolder(string folder)
    {
        folder = folder.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(folder))
            return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf = Path.GetFileName(folder);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static class ProBuilderPrimitives
    {
        static bool s_Checked;
        static bool s_Available;
        static bool s_Failed;
        static Type s_Shape;
        static Type s_Pivot;
        static Type s_Mesh;
        static object s_Center;

        public static bool Available
        {
            get
            {
                Ensure();
                return s_Available && !s_Failed;
            }
        }

        public static GameObject CreateCube()
        {
            if (!Available)
                return null;
            try
            {
                MethodInfo method = s_Shape.GetMethod("GenerateCube", new[] { s_Pivot, typeof(Vector3) });
                if (method == null)
                    return Fail("GenerateCube");
                object mesh = method.Invoke(null, new object[] { s_Center, Vector3.one });
                return Finalize(mesh);
            }
            catch (Exception e)
            {
                return Fail(e.Message);
            }
        }

        public static GameObject CreateCylinder()
        {
            if (!Available)
                return null;
            try
            {
                MethodInfo method = s_Shape.GetMethod("GenerateCylinder", new[]
                {
                    s_Pivot, typeof(int), typeof(float), typeof(float), typeof(int), typeof(int)
                });
                object mesh;
                if (method != null)
                {
                    mesh = method.Invoke(null, new object[] { s_Center, 20, 0.5f, 2f, 1, -1 });
                }
                else
                {
                    method = s_Shape.GetMethod("GenerateCylinder", new[]
                    {
                        s_Pivot, typeof(int), typeof(float), typeof(float), typeof(int)
                    });
                    if (method == null)
                        return Fail("GenerateCylinder");
                    mesh = method.Invoke(null, new object[] { s_Center, 20, 0.5f, 2f, 1 });
                }
                return Finalize(mesh);
            }
            catch (Exception e)
            {
                return Fail(e.Message);
            }
        }

        static GameObject Fail(string reason)
        {
            if (!s_Failed)
                Debug.LogWarning("ProBuilder 메시를 만들지 못해 기본 Primitive 를 사용합니다: " + reason);
            s_Failed = true;
            return null;
        }

        static void Ensure()
        {
            if (s_Checked)
                return;
            s_Checked = true;
            s_Shape = Type.GetType("UnityEngine.ProBuilder.ShapeGenerator, Unity.ProBuilder");
            s_Pivot = Type.GetType("UnityEngine.ProBuilder.PivotLocation, Unity.ProBuilder");
            s_Mesh = Type.GetType("UnityEngine.ProBuilder.ProBuilderMesh, Unity.ProBuilder");
            if (s_Shape == null || s_Pivot == null || s_Mesh == null)
                return;
            s_Center = Enum.Parse(s_Pivot, "Center");
            s_Available = true;
        }

        static GameObject Finalize(object mesh)
        {
            if (mesh == null)
                return null;
            Type type = mesh.GetType();
            MethodInfo toMesh = null;
            MethodInfo refresh = null;
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "ToMesh" && toMesh == null)
                    toMesh = methods[i];
                if (methods[i].Name == "Refresh" && refresh == null)
                    refresh = methods[i];
            }
            if (toMesh != null)
                toMesh.Invoke(mesh, toMesh.GetParameters().Length == 0 ? null : new object[] { 0 });
            if (refresh != null)
            {
                ParameterInfo[] p = refresh.GetParameters();
                if (p.Length == 0)
                    refresh.Invoke(mesh, null);
                else if (p[0].ParameterType.IsEnum)
                    refresh.Invoke(mesh, new[] { Enum.ToObject(p[0].ParameterType, -1) });
            }

            Component component = mesh as Component;
            return component != null ? component.gameObject : null;
        }
    }
}
