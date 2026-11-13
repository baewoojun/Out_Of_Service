// 롯데월드 어드벤처 1층 블록아웃.
// Tools/LotteWorld/Generate Blockout 을 다시 실행하면 씬의 Blockout 루트만 갈아끼운다.
//
// 단위 m. 원점 = 아이스링크 중심. +X 동쪽, +Z 북쪽. 회전은 Y축 도, 위에서 봤을 때 시계방향 +.
// 위치·크기·높이는 배치표 그대로다. 겹치거나 바닥 밖으로 나가도 옮기지 않는다.

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
    const string MeshDir = "Assets/Maps/LotteWorld/GeneratedMeshes";
    const string UrpBasicTemplate =
        "Packages/com.unity.render-pipelines.universal/Editor/SceneTemplates/Basic.scenetemplate";
    const string RootName = "Blockout";

    const float FloorY = 0f;
    const float FloorThickness = 0.4f;
    const float B1Y = -6f;
    const float IceThickness = 0.4f;

    const float VoidHalfX = 32.5f;
    const float VoidHalfZ = 18f;
    const float ParadeHalfX = 66f;
    const float ParadeHalfZ = 43f;
    const float ParadeWidth = 6f;
    const float ParadeLift = 0.02f;
    const float ParadeThickness = 0.03f;
    const int ParadeWaypointCount = 12;
    const float KeepOutBeyondOuter = 5f;

    const float RailingHeight = 1.1f;
    const float RailingThickness = 0.35f;
    const float ShaftThickness = 0.5f;

    const float EscThickness = 0.3f;
    const float EscShutterHeight = 4f;
    const float ShutterThickness = 0.35f;
    const float HallAlong = 15f;
    const float HallAcross = 20f;
    const float HallWallHeight = 4f;
    const float HallWallThickness = 0.4f;
    const float GateWidth = 6f;
    const float GateHeight = 3.2f;

    const float Balcony2Y = 9f;
    const float Balcony3Y = 18f;
    const float BalconyWidth = 6f;
    const float BalconyThickness = 0.3f;
    const float BalconyRailHeight = 1.1f;
    const float BalconyRailThickness = 0.22f;
    const float DomeY = 40f;
    const float DomeMargin = 6f;
    const float DomeThickness = 0.2f;
    const float WallHeight = 40f;
    const float WallThickness = 0.5f;
    const float MarkerHover = 0.1f;
    const float HideClearance = 1.2f;
    const float TouchEpsilon = 0.05f;

    static readonly Color GrayColor = new Color(0.62f, 0.62f, 0.64f, 1f);
    static readonly Color DarkColor = new Color(0.11f, 0.11f, 0.12f, 1f);
    static readonly Color RedColor = new Color(0.74f, 0.07f, 0.06f, 1f);
    static readonly Color PathColor = new Color(0.93f, 0.76f, 0.18f, 1f);
    static readonly Color RideColor = new Color(0.22f, 0.45f, 0.82f, 1f);
    static readonly Color RailColor = new Color(0.93f, 0.93f, 0.94f, 1f);
    static readonly Color IceColor = new Color(0.55f, 0.82f, 0.95f, 1f);
    static readonly Color FoodColor = new Color(0.95f, 0.46f, 0.12f, 1f);
    static readonly Color ShopColor = new Color(0.56f, 0.30f, 0.76f, 1f);

    static readonly Color LabelRide = new Color(0.95f, 0.15f, 0.12f);
    static readonly Color LabelFood = new Color(1f, 0.45f, 0.72f);
    static readonly Color LabelShop = new Color(0.62f, 0.32f, 0.95f);
    static readonly Color LabelDark = new Color(0.72f, 0.72f, 0.74f);

    enum Slot { Ride, Food, Shop, Dark, Plaza, Escalator }
    enum Shape { Rect, Circle, Poly }
    enum StaticMode { None, Walkable, Background }

    struct Place
    {
        public string id, title;
        public Slot slot;
        public Shape shape;
        public float x, z, w, d, rot, h;
        public Vector2[] points;

        public Place(string id, string title, Slot slot, Shape shape, float x, float z, float w, float d, float rot, float h, Vector2[] points)
        {
            this.id = id;
            this.title = title;
            this.slot = slot;
            this.shape = shape;
            this.x = x;
            this.z = z;
            this.w = w;
            this.d = d;
            this.rot = rot;
            this.h = h;
            this.points = points;
        }
    }

    struct Stadium
    {
        public float straightHalf, radiusX, radiusZ;
    }

    class Footprint
    {
        public string id;
        public bool circle;
        public Vector2 center;
        public float radius;
        public List<Vector2> poly;
    }

    struct Mats
    {
        public Material gray, dark, red, path, ride, rail, ice, food, shop;
    }

    static readonly Vector2[] FloorOutline =
    {
        new Vector2(-198.3f, 141.8f), new Vector2(-133f, 141.8f), new Vector2(-133f, 194.8f),
        new Vector2(14.3f, 194.8f), new Vector2(14.3f, 216.2f), new Vector2(168.4f, 219.7f),
        new Vector2(230.2f, 180.6f), new Vector2(242.8f, 124.1f), new Vector2(233.9f, 35.4f),
        new Vector2(225.1f, -74.4f), new Vector2(180.6f, -102.7f), new Vector2(101.1f, -141.7f),
        new Vector2(81.5f, -177.1f), new Vector2(-17.7f, -180.6f), new Vector2(-92.1f, -191.4f),
        new Vector2(-191.4f, -180.6f), new Vector2(-288.7f, -127.5f), new Vector2(-287f, -53.1f),
        new Vector2(-278.2f, 92.1f), new Vector2(-225.1f, 131.1f),
    };

    static readonly Place[] Places =
    {
        P("A06", "신밧드의모험", Slot.Ride, Shape.Poly, -254f, 84f, 112f, 120f, 12f, V(-20f, -15f), V(20f, -15f), V(56f, 60f), V(-20f, 15f)),
        P("A07", "스페인해적선", Slot.Ride, Shape.Poly, -252f, -46f, 44f, 142f, 17.5f, V(-22f, -71f), V(16.5f, -63.5f), V(17f, 40f), V(-19f, 40f)),
        P("후룸라이드", "후룸라이드", Slot.Ride, Shape.Poly, -115f, -147f, 344f, 168f, 16f, V(-101f, -28f), V(172f, -26f), V(19f, 12f), V(-4f, 18f), V(-99f, 84f)),
        P("A09", "회전바구니", Slot.Ride, Shape.Circle, -107f, -109f, 42f, 42f, 4f),
        P("A11", "배틀그라운드", Slot.Ride, Shape.Rect, 77f, -53f, 23f, 20f, 8f),
        P("A12", "플라이벤처(성)", Slot.Ride, Shape.Poly, 119f, -93f, 102f, 170f, 25f, V(-47f, -85f), V(46.5f, -15f), V(51f, 17f), V(-46.5f, 15f)),
        P("A13", "회전목마", Slot.Ride, Shape.Circle, 135f, -14f, 42f, 42f, 12f),
        P("A15", "키즈토리아", Slot.Ride, Shape.Poly, 131f, 61f, 118f, 180f, 8f, V(-36.5f, -28.5f), V(49f, -35f), V(59f, 90f), V(-36.5f, 28.5f)),
        P("롤러코스터", "롤러코스터", Slot.Ride, Shape.Poly, 207f, 86f, 80f, 386f, 6f, V(-40f, -193f), V(17f, -162f), V(37f, 73f), V(-13.5f, 73.5f)),
        P("GardenStage", "가든스테이지", Slot.Ride, Shape.Rect, -139f, -27f, 97f, 95f, 12f),
        P("5", "건물", Slot.Shop, Shape.Rect, -23f, 68f, 22f, 9f, 5f),
        P("S06", "기프트숍", Slot.Shop, Shape.Rect, -59f, 65f, 20f, 18f, 5f),
        P("MeetingPlaza", "만남의 광장", Slot.Plaza, Shape.Rect, 5f, 78f, 30f, 12f, 0f),
        P("E_Escalator", "정문 에스컬레이터", Slot.Escalator, Shape.Rect, -122f, 191f, 18f, 8f, 0f),
        P("S_new1", "새 구조물", Slot.Shop, Shape.Rect, -51f, 142f, 65f, 106f, 6f),
        P("A15_2", "키즈토리아", Slot.Ride, Shape.Poly, 84f, 119f, 244f, 154f, 8f, V(-81.5f, -29.5f), V(81.5f, -29.5f), V(122f, 77f), V(-109f, 75f)),
        P("폭포", "폭포", Slot.Dark, Shape.Rect, -8f, 102f, 20f, 26f, 6f),
        P("F_new1", "새 구조물", Slot.Food, Shape.Rect, -147f, 107f, 56f, 67f, 6f),
        P("F_new2", "새 구조물", Slot.Food, Shape.Rect, -92f, 171f, 17f, 48f, 6f),
        P("출입구(ES)", "정문 에스컬레이터", Slot.Escalator, Shape.Rect, -42f, -80f, 9f, 28f, 0f),
        P("건물", "건물", Slot.Ride, Shape.Poly, 10f, -129f, 96f, 124f, 25f, V(-48f, -18f), V(45.5f, -41f), V(40f, 62f), V(-47f, 62f)),
    };

    static readonly List<Footprint> Footprints = new List<Footprint>();
    static readonly List<Vector2> Waypoints = new List<Vector2>();
    static float FloorCenterX;
    static float FloorCenterZ;
    static float FloorWidth = 80f;
    static float FloorDepth = 80f;
    static string ReportNote = "";

    static Vector2 V(float x, float z) => new Vector2(x, z);

    static Place P(string id, string title, Slot slot, Shape shape, float x, float z, float w, float d, float h, params Vector2[] points)
    {
        return new Place(id, title, slot, shape, x, z, w, d, 0f, h, points == null || points.Length == 0 ? null : points);
    }

    static Stadium MakeStadium(float halfX, float halfZ)
    {
        return new Stadium
        {
            straightHalf = halfX - halfZ,
            radiusX = halfZ,
            radiusZ = halfZ,
        };
    }

    static Stadium VoidShape => MakeStadium(VoidHalfX, VoidHalfZ);
    static Stadium ParadeShape => MakeStadium(ParadeHalfX, ParadeHalfZ);

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
            ClearGeneratedMeshes();
            Mats mats = EnsureMaterials();
            MeasureFloor();

            EditorUtility.DisplayProgressBar("LotteWorld", "씬 열기", 0.15f);
            bool createdScene = OpenOrCreateScene();
            Scene active = SceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                Debug.LogError("대상 씬을 열지 못했습니다: " + ScenePath + " (현재: " + active.path + ")");
                return;
            }

            Transform carousel = FindNamed("Carousel_A13");
            if (carousel != null && IsUnderNamed(carousel, RootName))
                carousel.SetParent(null, true);

            var root = new GameObject("__BlockoutNew");
            ApplyStatic(root, StaticMode.Walkable);
            try
            {
                EditorUtility.DisplayProgressBar("LotteWorld", "배치", 0.35f);
                Footprints.Clear();
                Waypoints.Clear();

                Transform floor1 = CreateGroup(root.transform, "Floor1", StaticMode.Walkable);
                Transform b1 = CreateGroup(root.transform, "B1", StaticMode.Walkable);
                Transform background = CreateGroup(root.transform, "Background", StaticMode.Background);
                Transform blocked = CreateGroup(root.transform, "Blocked", StaticMode.Walkable);
                Transform markers = CreateGroup(root.transform, "Markers", StaticMode.None);

                BuildFloor(floor1, mats);
                BuildVoid(floor1, mats);
                BuildParade(floor1, mats);
                BuildPlaces(floor1, blocked, mats);
                Place mainExit = FindPlace("출입구(ES)");
                BuildGateHall(b1, mats, mainExit);
                BuildIce(b1, mats);
                BuildOuterWalls(floor1, mats);
                BuildBackground(background, mats);
                BuildMarkers(markers);
                BuildKeepOut(markers);
                ReportNote = BuildReport();

                ReplaceOldRoot(root);
                PlaceCarousel(root.transform, carousel);
                PlacePlayer();

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
                    + "m, 중심 (" + FloorCenterX.ToString("0.0", culture) + ", " + FloorCenterZ.ToString("0.0", culture)
                    + "). 메시 " + (ProBuilderPrimitives.Available ? "ProBuilder" : "Mesh")
                    + ". " + ReportNote);
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

    static void MeasureFloor()
    {
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
        for (int i = 0; i < FloorOutline.Length; i++)
        {
            minX = Mathf.Min(minX, FloorOutline[i].x);
            maxX = Mathf.Max(maxX, FloorOutline[i].x);
            minZ = Mathf.Min(minZ, FloorOutline[i].y);
            maxZ = Mathf.Max(maxZ, FloorOutline[i].y);
        }
        FloorCenterX = (minX + maxX) * 0.5f;
        FloorCenterZ = (minZ + maxZ) * 0.5f;
        FloorWidth = maxX - minX;
        FloorDepth = maxZ - minZ;
    }

    static void BuildFloor(Transform parent, Mats mats)
    {
        List<Vector2> outer = new List<Vector2>(FloorOutline);
        var holes = new List<List<Vector2>>
        {
            SampleStadium(VoidShape, 0f, 16, 6),
        };
        for (int i = 0; i < Places.Length; i++)
        {
            if (Places[i].slot != Slot.Escalator)
                continue;
            holes.Add(RectPoly(Places[i].x, Places[i].z, Places[i].w, Places[i].d));
        }
        CreatePrism(parent, "Floor", outer, holes, FloorY - FloorThickness, FloorThickness, mats.gray, StaticMode.Walkable, true, true);
    }

    static void BuildVoid(Transform parent, Mats mats)
    {
        Transform shaft = CreateGroup(parent, "VoidShaft", StaticMode.Walkable);
        Transform railing = CreateGroup(parent, "VoidRailing", StaticMode.Walkable);
        List<Vector2> edge = SampleStadium(VoidShape, 0f, 18, 8);
        int index = 0;
        float shaftY = (FloorY + B1Y) * 0.5f;
        EmitRibbon(shaft, "VoidShaft_", edge, true, ShaftThickness, FloorY - B1Y, shaftY, mats.gray, StaticMode.Walkable, null, ref index);
        index = 0;
        List<Vector2> rail = SampleStadium(VoidShape, RailingThickness * 0.5f, 18, 8);
        EmitRibbon(railing, "VoidRailing_", rail, true, RailingThickness, RailingHeight, FloorY + RailingHeight * 0.5f, mats.rail, StaticMode.Walkable, null, ref index);
    }

    static void BuildParade(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "ParadeLoop", StaticMode.Walkable);
        List<Vector2> loop = SampleStadium(ParadeShape, 0f, 20, 8);
        int index = 0;
        float y = FloorY + ParadeLift - ParadeThickness * 0.5f;
        EmitRibbon(group, "Parade_", loop, true, ParadeWidth, ParadeThickness, y, mats.path, StaticMode.Walkable, null, ref index);

        Waypoints.Clear();
        int start = ClosestIndex(loop, new Vector2(ParadeHalfX, 0f));
        List<Vector2> even = EvenPoints(loop, ParadeWaypointCount, start);
        for (int i = 0; i < even.Count; i++)
            Waypoints.Add(even[i]);
    }

    static void BuildPlaces(Transform floor1, Transform blocked, Mats mats)
    {
        Transform food = CreateGroup(floor1, "Restaurants", StaticMode.Walkable);
        Transform shops = CreateGroup(floor1, "GiftShops", StaticMode.Walkable);
        Transform escape = CreateGroup(floor1, "Escape", StaticMode.Walkable);
        for (int i = 0; i < Places.Length; i++)
        {
            Place place = Places[i];
            Footprints.Add(MakeFootprint(place));
            switch (place.slot)
            {
                case Slot.Escalator:
                    BuildEscalator(escape, place, mats);
                    break;
                case Slot.Plaza:
                    BuildPlaza(floor1, place, mats);
                    break;
                default:
                    Transform parent = floor1;
                    if (place.slot == Slot.Food)
                        parent = food;
                    else if (place.slot == Slot.Shop)
                        parent = shops;
                    else if (place.slot == Slot.Dark)
                        parent = blocked;
                    BuildSolid(parent, place, SlotMaterial(place.slot, mats), LabelColor(place.slot));
                    break;
            }
        }
    }

    static void BuildSolid(Transform parent, Place place, Material mat, Color label)
    {
        GameObject go;
        if (place.shape == Shape.Circle)
        {
            go = CreateCylinder(parent, place.id, new Vector3(place.x, place.h * 0.5f, place.z),
                place.w * 0.5f, place.w * 0.5f, place.h, Quaternion.Euler(0f, -place.rot, 0f), mat, StaticMode.Walkable);
        }
        else if (place.shape == Shape.Poly)
        {
            go = CreatePrism(parent, place.id, WorldPoly(place), null, 0f, place.h, mat, StaticMode.Walkable, true, true);
        }
        else
        {
            go = CreateBox(parent, place.id, new Vector3(place.x, place.h * 0.5f, place.z),
                new Vector3(place.w, place.h, place.d), Quaternion.Euler(0f, -place.rot, 0f), mat, StaticMode.Walkable);
        }
        AddLabel(go, CaptionOf(place), label);
    }

    static void BuildPlaza(Transform parent, Place place, Mats mats)
    {
        GameObject go = CreateBox(parent, place.id, new Vector3(place.x, FloorY + 0.03f, place.z),
            new Vector3(place.w, 0.04f, place.d), Quaternion.identity, mats.gray, StaticMode.Walkable);
        AddLabel(go, CaptionOf(place), Color.white);
    }

    static void BuildEscalator(Transform parent, Place place, Mats mats)
    {
        EscalatorEnds(place, out Vector2 entrance, out Vector2 exit, out float width, out Vector2 down);
        Vector3 p0 = new Vector3(entrance.x, FloorY, entrance.y);
        Vector3 p1 = new Vector3(exit.x, B1Y, exit.y);
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
        Transform group = CreateGroup(parent, place.id, StaticMode.Walkable);
        GameObject ramp = CreateBox(group, "Ramp", boxCenter, new Vector3(width, EscThickness, length),
            Quaternion.LookRotation(forward, up), mats.red, StaticMode.Walkable);
        AddLabel(ramp, CaptionOf(place), LabelRide);

        bool escape = place.id == "출입구(ES)";
        Quaternion yaw = Quaternion.LookRotation(new Vector3(down.x, 0f, down.y), Vector3.up);
        CreateBox(group, escape ? "EscapeShutter" : "Shutter",
            new Vector3(entrance.x, FloorY + EscShutterHeight * 0.5f, entrance.y),
            new Vector3(width, EscShutterHeight, ShutterThickness), yaw,
            escape ? mats.red : mats.dark, StaticMode.None, true, true);
    }

    static void BuildGateHall(Transform parent, Mats mats, Place place)
    {
        EscalatorEnds(place, out _, out Vector2 exit, out float rampWidth, out Vector2 down);
        Transform group = CreateGroup(parent, "GateHall", StaticMode.Walkable);
        Vector2 side = new Vector2(-down.y, down.x);
        Vector2 center = exit + down * (HallAlong * 0.5f);
        Vector2 far = exit + down * HallAlong;
        Quaternion yaw = Quaternion.LookRotation(new Vector3(down.x, 0f, down.y), Vector3.up);
        CreateBox(group, "GateHall_Floor", new Vector3(center.x, B1Y - 0.2f, center.y),
            new Vector3(HallAcross, 0.4f, HallAlong), yaw, mats.gray, StaticMode.Walkable);

        float wallY = B1Y + HallWallHeight * 0.5f;
        WallWithGap(group, "GateHall_Wall_Near", exit, side, HallAcross, rampWidth, HallWallThickness, HallWallHeight, wallY, yaw, mats.gray);
        WallWithGap(group, "GateHall_Wall_Far", far, side, HallAcross, GateWidth, HallWallThickness, HallWallHeight, wallY, yaw, mats.gray);
        Vector2 left = center + side * (HallAcross * 0.5f);
        Vector2 right = center - side * (HallAcross * 0.5f);
        CreateBox(group, "GateHall_Wall_L", new Vector3(left.x, wallY, left.y),
            new Vector3(HallWallThickness, HallWallHeight, HallAlong), yaw, mats.gray, StaticMode.Walkable);
        CreateBox(group, "GateHall_Wall_R", new Vector3(right.x, wallY, right.y),
            new Vector3(HallWallThickness, HallWallHeight, HallAlong), yaw, mats.gray, StaticMode.Walkable);
        CreateBox(group, "MainGate", new Vector3(far.x, B1Y + GateHeight * 0.5f, far.y),
            new Vector3(GateWidth, GateHeight, HallWallThickness), yaw, mats.red, StaticMode.None, true, true);
    }

    static void WallWithGap(Transform parent, string name, Vector2 at, Vector2 side, float length, float gap, float thickness, float height, float y, Quaternion yaw, Material mat)
    {
        float piece = (length - gap) * 0.5f;
        if (piece < 0.05f)
            return;
        float offset = gap * 0.5f + piece * 0.5f;
        Vector2 a = at + side * offset;
        Vector2 b = at - side * offset;
        CreateBox(parent, name + "_A", new Vector3(a.x, y, a.y), new Vector3(piece, height, thickness), yaw, mat, StaticMode.Walkable);
        CreateBox(parent, name + "_B", new Vector3(b.x, y, b.y), new Vector3(piece, height, thickness), yaw, mat, StaticMode.Walkable);
    }

    static void EscalatorEnds(Place place, out Vector2 entrance, out Vector2 exit, out float width, out Vector2 down)
    {
        bool alongX = place.w >= place.d;
        float halfLong = (alongX ? place.w : place.d) * 0.5f;
        width = alongX ? place.d : place.w;
        Vector2 axis = alongX ? Vector2.right : Vector2.up;
        Vector2 center = new Vector2(place.x, place.z);
        Vector2 positive = center + axis * halfLong;
        Vector2 negative = center - axis * halfLong;
        if (positive.sqrMagnitude <= negative.sqrMagnitude)
        {
            entrance = positive;
            exit = negative;
        }
        else
        {
            entrance = negative;
            exit = positive;
        }
        down = exit - entrance;
        if (down.sqrMagnitude > 0.0001f)
            down.Normalize();
        else
            down = Vector2.down;
    }

    static void BuildIce(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "IceRink", StaticMode.Walkable);
        CreatePrism(group, "Ice", SampleStadium(VoidShape, 0f, 16, 6), null, B1Y - IceThickness, IceThickness, mats.ice, StaticMode.Walkable, true, true);
    }

    static void BuildOuterWalls(Transform parent, Mats mats)
    {
        Transform group = CreateGroup(parent, "OuterWalls", StaticMode.Walkable);
        int index = 0;
        EmitRibbon(group, "Wall_", new List<Vector2>(FloorOutline), true, WallThickness, WallHeight, FloorY + WallHeight * 0.5f, mats.gray, StaticMode.Walkable, null, ref index);
    }

    static void BuildBackground(Transform parent, Mats mats)
    {
        List<Vector2> outer = new List<Vector2>(FloorOutline);
        EnsureWinding(outer, true);
        List<Vector2> inner = OffsetPolygon(outer, BalconyWidth);
        BuildBalcony(parent, "Balcony2F", Balcony2Y, outer, inner, mats);
        BuildBalcony(parent, "Balcony3F", Balcony3Y, outer, inner, mats);

        List<Vector2> dome = OffsetPolygon(outer, -DomeMargin);
        CreatePrism(parent, "GlassDome", dome, null, DomeY - DomeThickness * 0.5f, DomeThickness, mats.gray, StaticMode.Background, false, false);
    }

    static void BuildBalcony(Transform parent, string name, float levelY, List<Vector2> outer, List<Vector2> inner, Mats mats)
    {
        Transform group = CreateGroup(parent, name, StaticMode.Background);
        var holes = new List<List<Vector2>> { new List<Vector2>(inner) };
        CreatePrism(group, name + "_Slab", new List<Vector2>(outer), holes, levelY - BalconyThickness, BalconyThickness, mats.gray, StaticMode.Background, true, true);
        int index = 0;
        float railY = levelY + BalconyRailHeight * 0.5f;
        EmitRibbon(group, name + "_Rail_", inner, true, BalconyRailThickness, BalconyRailHeight, railY, mats.rail, StaticMode.Background, null, ref index);
    }

    static void BuildMarkers(Transform parent)
    {
        Place plaza = FindPlace("MeetingPlaza");
        CreateMarker(parent, "PlayerSpawn", plaza.x, plaza.z);
        for (int i = 0; i < Waypoints.Count; i++)
            CreateMarker(parent, "ParadeWaypoint_" + i.ToString("00"), Waypoints[i].x, Waypoints[i].y);

        int hide = 0;
        for (int i = 0; i < Places.Length; i++)
        {
            if (Places[i].slot != Slot.Ride)
                continue;
            hide++;
            Vector2 spot = HideSpot(Places[i]);
            CreateMarker(parent, "HideSpot_" + hide.ToString("00"), spot.x, spot.y);
        }
    }

    static void BuildKeepOut(Transform parent)
    {
        float outward = ParadeWidth * 0.5f + KeepOutBeyondOuter;
        List<Vector2> loop = SampleStadium(ParadeShape, outward, 24, 10);
        var go = new GameObject("ParadeKeepOut");
        go.transform.SetParent(parent, false);
        ApplyStatic(go, StaticMode.None);
        var gizmo = go.AddComponent<ParadeKeepOutGizmo>();
        gizmo.Points = new Vector3[loop.Count];
        for (int i = 0; i < loop.Count; i++)
            gizmo.Points[i] = new Vector3(loop[i].x, FloorY + 0.2f, loop[i].y);
    }

    static Vector2 HideSpot(Place place)
    {
        Vector2 center = new Vector2(place.x, place.z);
        Vector2 dir = center;
        if (dir.sqrMagnitude < 0.01f)
            dir = Vector2.up;
        dir.Normalize();
        float maxT = 0f;
        bool hit = false;
        if (place.shape == Shape.Circle)
        {
            maxT = place.w * 0.5f;
            hit = true;
        }
        else
        {
            List<Vector2> poly = WorldPoly(place);
            for (int i = 0; i < poly.Count; i++)
            {
                if (RaySegment(center, dir, poly[i], poly[(i + 1) % poly.Count], out float t) && t > maxT)
                {
                    maxT = t;
                    hit = true;
                }
            }
            if (!hit)
            {
                for (int i = 0; i < poly.Count; i++)
                    maxT = Mathf.Max(maxT, Vector2.Dot(poly[i] - center, dir));
            }
        }
        return center + dir * (maxT + HideClearance);
    }

    static void PlaceCarousel(Transform root, Transform carousel)
    {
        if (carousel == null)
        {
            Debug.Log("Carousel_A13 인스턴스가 없어 A13 블록아웃은 켜 두었습니다.");
            return;
        }
        carousel.position = new Vector3(135f, 0f, -14f);
        Transform block = FindChild(root, "A13");
        if (block != null)
            block.gameObject.SetActive(false);
        Debug.Log("Carousel_A13 을 (135, 0, -14)로 옮기고 A13 블록아웃을 껐습니다.");
    }

    static void PlacePlayer()
    {
        Place plaza = FindPlace("MeetingPlaza");
        Transform player = FindNamed("PlayerCapsule");
        if (player == null)
        {
            Debug.LogWarning("PlayerCapsule 을 찾지 못했습니다.");
            return;
        }
        player.position = new Vector3(plaza.x, 0.1f, plaza.z);
    }

    static string BuildReport()
    {
        var outside = new List<string>();
        var overlaps = new List<string>();
        for (int i = 0; i < Footprints.Count; i++)
        {
            if (IsOutside(Footprints[i]))
                outside.Add(Footprints[i].id);
        }
        for (int i = 0; i < Footprints.Count; i++)
        {
            for (int j = i + 1; j < Footprints.Count; j++)
            {
                if (Overlaps(Footprints[i], Footprints[j]))
                    overlaps.Add(Footprints[i].id + "↔" + Footprints[j].id);
            }
        }
        string outText = outside.Count == 0
            ? "바닥 밖으로 나간 구조물 없음."
            : "바닥 밖: " + string.Join(", ", outside) + ".";
        string overText = overlaps.Count == 0
            ? "겹침 없음."
            : "겹침: " + string.Join(", ", overlaps) + ".";
        return outText + " " + overText + " 위치는 표 그대로 두었습니다.";
    }

    static bool IsOutside(Footprint shape)
    {
        if (shape.circle)
        {
            if (!PointInPoly(shape.center, FloorOutline))
                return true;
            const int samples = 48;
            for (int i = 0; i < samples; i++)
            {
                float t = i * Mathf.PI * 2f / samples;
                var p = new Vector2(shape.center.x + Mathf.Cos(t) * shape.radius, shape.center.y + Mathf.Sin(t) * shape.radius);
                if (!PointInPoly(p, FloorOutline))
                    return true;
            }
            return false;
        }
        for (int i = 0; i < shape.poly.Count; i++)
        {
            if (!PointInPoly(shape.poly[i], FloorOutline))
                return true;
        }
        for (int i = 0; i < shape.poly.Count; i++)
        {
            Vector2 a = shape.poly[i];
            Vector2 b = shape.poly[(i + 1) % shape.poly.Count];
            for (int k = 0; k < FloorOutline.Length; k++)
            {
                if (ProperCross(a, b, FloorOutline[k], FloorOutline[(k + 1) % FloorOutline.Length]))
                    return true;
            }
        }
        return false;
    }

    static bool Overlaps(Footprint a, Footprint b)
    {
        if (a.circle && b.circle)
            return Vector2.Distance(a.center, b.center) < a.radius + b.radius - TouchEpsilon;
        if (a.circle)
            return CircleHitsPoly(a.center, a.radius, b.poly);
        if (b.circle)
            return CircleHitsPoly(b.center, b.radius, a.poly);
        return PolysOverlap(a.poly, b.poly);
    }

    static bool CircleHitsPoly(Vector2 center, float radius, List<Vector2> poly)
    {
        if (PointInPoly(center, poly))
            return true;
        for (int i = 0; i < poly.Count; i++)
        {
            if (DistanceToSegment(center, poly[i], poly[(i + 1) % poly.Count]) < radius - TouchEpsilon)
                return true;
        }
        return false;
    }

    static bool PolysOverlap(List<Vector2> a, List<Vector2> b)
    {
        for (int i = 0; i < a.Count; i++)
        {
            Vector2 a0 = a[i];
            Vector2 a1 = a[(i + 1) % a.Count];
            for (int j = 0; j < b.Count; j++)
            {
                if (ProperCross(a0, a1, b[j], b[(j + 1) % b.Count]))
                    return true;
            }
        }
        for (int i = 0; i < a.Count; i++)
        {
            if (StrictlyInside(a[i], b))
                return true;
        }
        for (int i = 0; i < b.Count; i++)
        {
            if (StrictlyInside(b[i], a))
                return true;
        }
        return false;
    }

    static bool StrictlyInside(Vector2 point, List<Vector2> poly)
    {
        if (!PointInPoly(point, poly))
            return false;
        float best = float.MaxValue;
        for (int i = 0; i < poly.Count; i++)
            best = Mathf.Min(best, DistanceToSegment(point, poly[i], poly[(i + 1) % poly.Count]));
        return best > TouchEpsilon;
    }

    static Footprint MakeFootprint(Place place)
    {
        var footprint = new Footprint { id = place.id, center = new Vector2(place.x, place.z) };
        if (place.shape == Shape.Circle)
        {
            footprint.circle = true;
            footprint.radius = place.w * 0.5f;
        }
        else
        {
            footprint.poly = WorldPoly(place);
        }
        return footprint;
    }

    static List<Vector2> WorldPoly(Place place)
    {
        float rad = -place.rot * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        if (place.shape == Shape.Poly && place.points != null)
        {
            var list = new List<Vector2>(place.points.Length);
            for (int i = 0; i < place.points.Length; i++)
            {
                Vector2 local = place.points[i];
                float x = local.x * cos + local.y * sin;
                float z = -local.x * sin + local.y * cos;
                list.Add(new Vector2(place.x + x, place.z + z));
            }
            return list;
        }
        return RectPoly(place.x, place.z, place.w, place.d);
    }

    static List<Vector2> RectPoly(float cx, float cz, float w, float d)
    {
        float hx = w * 0.5f;
        float hz = d * 0.5f;
        return new List<Vector2>
        {
            new Vector2(cx - hx, cz - hz),
            new Vector2(cx + hx, cz - hz),
            new Vector2(cx + hx, cz + hz),
            new Vector2(cx - hx, cz + hz),
        };
    }

    static Place FindPlace(string id)
    {
        for (int i = 0; i < Places.Length; i++)
        {
            if (Places[i].id == id)
                return Places[i];
        }
        throw new InvalidOperationException("배치를 찾지 못했습니다: " + id);
    }

    static string CaptionOf(Place place)
    {
        if (string.IsNullOrEmpty(place.title) || place.title == place.id)
            return place.id;
        return place.id + " | " + place.title;
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

    static Color LabelColor(Slot slot)
    {
        switch (slot)
        {
            case Slot.Food: return LabelFood;
            case Slot.Shop: return LabelShop;
            case Slot.Dark: return LabelDark;
            default: return LabelRide;
        }
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
        return true;
    }

    static void FrameNewSceneCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;
        Vector3 target = new Vector3(FloorCenterX, 1f, FloorCenterZ);
        cam.transform.position = new Vector3(FloorCenterX, 40f, FloorCenterZ - 80f);
        cam.transform.rotation = Quaternion.LookRotation(target - cam.transform.position, Vector3.up);
        cam.farClipPlane = 800f;
    }

    static void FrameView()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null)
            return;
        view.Frame(new Bounds(
            new Vector3(FloorCenterX, DomeY * 0.25f, FloorCenterZ),
            new Vector3(FloorWidth, DomeY + 8f, FloorDepth)), false);
        view.Repaint();
    }

    static Transform FindNamed(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < all.Length; j++)
            {
                if (all[j].name == objectName)
                    return all[j];
            }
        }
        return null;
    }

    static Transform FindChild(Transform root, string objectName)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == objectName)
                return all[i];
        }
        return null;
    }

    static bool IsUnderNamed(Transform t, string objectName)
    {
        Transform cursor = t.parent;
        while (cursor != null)
        {
            if (cursor.name == objectName)
                return true;
            cursor = cursor.parent;
        }
        return false;
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
                new Vector3(width, height, len + 0.08f), Quaternion.Euler(0f, yaw, 0f), mat, mode);
            index++;
        }
    }

    static List<Vector2> EvenPoints(List<Vector2> loop, int count, int startIndex)
    {
        int n = loop.Count;
        var seg = new float[n];
        float total = 0f;
        for (int i = 0; i < n; i++)
        {
            seg[i] = Vector2.Distance(loop[i], loop[(i + 1) % n]);
            total += seg[i];
        }
        float startDist = 0f;
        for (int i = 0; i < startIndex; i++)
            startDist += seg[i];
        var result = new List<Vector2>(count);
        for (int k = 0; k < count; k++)
        {
            float target = startDist + total * k / count;
            target %= total;
            if (target < 0f)
                target += total;
            float acc = 0f;
            for (int i = 0; i < n; i++)
            {
                float next = acc + seg[i];
                if (target <= next + 0.0001f || i == n - 1)
                {
                    float t = seg[i] < 0.0001f ? 0f : (target - acc) / seg[i];
                    result.Add(Vector2.Lerp(loop[i], loop[(i + 1) % n], Mathf.Clamp01(t)));
                    break;
                }
                acc = next;
            }
        }
        return result;
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

    static GameObject CreatePrism(Transform parent, string name, List<Vector2> outer, List<List<Vector2>> holes, float y0, float height, Material mat, StaticMode mode, bool collider, bool shadows)
    {
        var ring = new List<Vector2>(outer);
        RemoveDuplicates(ring);
        EnsureWinding(ring, true);
        var holeRings = new List<List<Vector2>>();
        if (holes != null)
        {
            for (int i = 0; i < holes.Count; i++)
            {
                var hole = new List<Vector2>(holes[i]);
                RemoveDuplicates(hole);
                hole = PullInside(hole, ring, 0.15f);
                EnsureWinding(hole, false);
                if (hole.Count >= 3)
                    holeRings.Add(hole);
            }
            holeRings.Sort((a, b) => MaxX(b).CompareTo(MaxX(a)));
        }

        List<Vector2> merged = new List<Vector2>(ring);
        for (int i = 0; i < holeRings.Count; i++)
            merged = LinkHole(merged, holeRings[i]);
        RemoveCollinear(merged);
        List<int> tris = EarClip(merged);
        if (tris == null || tris.Count != (merged.Count - 2) * 3)
            throw new InvalidOperationException(name + " 다각형을 삼각분할하지 못했습니다.");

        var vertices = new List<Vector3>(merged.Count * 2);
        var uvs = new List<Vector2>(merged.Count * 2);
        for (int i = 0; i < merged.Count; i++)
        {
            vertices.Add(new Vector3(merged[i].x, y0 + height, merged[i].y));
            uvs.Add(merged[i] * 0.02f);
        }
        for (int i = 0; i < merged.Count; i++)
        {
            vertices.Add(new Vector3(merged[i].x, y0, merged[i].y));
            uvs.Add(merged[i] * 0.02f);
        }

        var triangles = new List<int>(tris.Count * 2);
        for (int i = 0; i < tris.Count; i += 3)
        {
            triangles.Add(tris[i]);
            triangles.Add(tris[i + 2]);
            triangles.Add(tris[i + 1]);
        }
        int bottom = merged.Count;
        for (int i = 0; i < tris.Count; i += 3)
        {
            triangles.Add(tris[i] + bottom);
            triangles.Add(tris[i + 1] + bottom);
            triangles.Add(tris[i + 2] + bottom);
        }

        Vector3 probe = Vector3.Cross(vertices[triangles[1]] - vertices[triangles[0]], vertices[triangles[2]] - vertices[triangles[0]]);
        if (probe.y < 0f)
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int swap = triangles[i + 1];
                triangles[i + 1] = triangles[i + 2];
                triangles[i + 2] = swap;
            }
        }

        AddSideWall(vertices, uvs, triangles, ring, y0, y0 + height);
        for (int i = 0; i < holeRings.Count; i++)
            AddSideWall(vertices, uvs, triangles, holeRings[i], y0, y0 + height);

        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = ProBuilderPrimitives.CreateTriangles(vertices.ToArray(), triangles.ToArray());
        if (go == null)
            go = CreateMeshAssetObject(name, mesh);
        else
            UnityEngine.Object.DestroyImmediate(mesh);
        return FinishMeshObject(go, parent, name, mat, mode, collider, shadows);
    }

    // 외곽은 반시계, 구멍은 시계. 진행 방향의 오른쪽(바깥 / 구멍 안)을 보도록 옆면을 만든다.
    static void AddSideWall(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, List<Vector2> ring, float y0, float y1)
    {
        int n = ring.Count;
        int start = vertices.Count;
        for (int i = 0; i < n; i++)
        {
            vertices.Add(new Vector3(ring[i].x, y0, ring[i].y));
            uvs.Add(ring[i] * 0.02f);
        }
        for (int i = 0; i < n; i++)
        {
            vertices.Add(new Vector3(ring[i].x, y1, ring[i].y));
            uvs.Add(ring[i] * 0.02f);
        }
        for (int i = 0; i < n; i++)
        {
            int i1 = (i + 1) % n;
            int b0 = start + i;
            int b1 = start + i1;
            int t0 = start + n + i;
            int t1 = start + n + i1;
            triangles.Add(b1);
            triangles.Add(b0);
            triangles.Add(t0);
            triangles.Add(b1);
            triangles.Add(t0);
            triangles.Add(t1);
        }
    }

    static GameObject FinishMeshObject(GameObject go, Transform parent, string name, Material mat, StaticMode mode, bool collider, bool shadows)
    {
        go.name = name;
        go.transform.SetParent(parent, true);
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = true;
        ApplyStatic(go, mode);
        if (collider)
        {
            MeshFilter filter = go.GetComponent<MeshFilter>();
            MeshCollider meshCollider = go.GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = filter.sharedMesh;
            meshCollider.convex = false;
        }
        return go;
    }

    static GameObject CreateMeshAssetObject(string name, Mesh mesh)
    {
        EnsureAssetFolder(MeshDir);
        string path = MeshDir + "/" + Sanitize(name) + ".asset";
        AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(name);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        return go;
    }

    static string Sanitize(string name)
    {
        char[] bad = Path.GetInvalidFileNameChars();
        var chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            for (int j = 0; j < bad.Length; j++)
            {
                if (chars[i] == bad[j])
                    chars[i] = '_';
            }
        }
        return new string(chars);
    }

    static void ClearGeneratedMeshes()
    {
        if (!AssetDatabase.IsValidFolder(MeshDir))
            return;
        string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { MeshDir });
        for (int i = 0; i < guids.Length; i++)
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guids[i]));
    }

    static List<Vector2> PullInside(List<Vector2> hole, List<Vector2> outer, float margin)
    {
        var result = new List<Vector2>(hole.Count);
        for (int i = 0; i < hole.Count; i++)
        {
            Vector2 p = hole[i];
            if (PointInPoly(p, outer))
            {
                result.Add(p);
                continue;
            }
            float best = float.MaxValue;
            Vector2 closest = p;
            int edge = 0;
            for (int k = 0; k < outer.Count; k++)
            {
                Vector2 a = outer[k];
                Vector2 b = outer[(k + 1) % outer.Count];
                Vector2 q = ClosestOnSegment(p, a, b);
                float d = (q - p).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    closest = q;
                    edge = k;
                }
            }
            Vector2 inward = LeftNormal(outer[(edge + 1) % outer.Count] - outer[edge]);
            result.Add(closest + inward * margin);
        }
        return result;
    }

    static List<Vector2> LinkHole(List<Vector2> poly, List<Vector2> hole)
    {
        int hi = 0;
        for (int i = 1; i < hole.Count; i++)
        {
            if (hole[i].x > hole[hi].x)
                hi = i;
        }
        Vector2 m = hole[hi];
        int connect = -1;
        float best = float.MaxValue;
        for (int i = 0; i < poly.Count; i++)
        {
            if ((poly[i] - m).sqrMagnitude < 1e-8f)
                continue;
            if (!Visible(poly, m, poly[i], hole))
                continue;
            float d = (poly[i] - m).sqrMagnitude;
            if (d < best)
            {
                best = d;
                connect = i;
            }
        }
        if (connect < 0)
            throw new InvalidOperationException("바닥 구멍을 외곽선에 잇지 못했습니다.");

        var merged = new List<Vector2>(poly.Count + hole.Count + 2);
        for (int i = 0; i <= connect; i++)
            merged.Add(poly[i]);
        for (int k = 0; k <= hole.Count; k++)
            merged.Add(hole[(hi + k) % hole.Count]);
        for (int i = connect; i < poly.Count; i++)
            merged.Add(poly[i]);
        return merged;
    }

    static bool Visible(List<Vector2> poly, Vector2 a, Vector2 b, List<Vector2> hole)
    {
        Vector2 mid = (a + b) * 0.5f;
        if (!PointInPoly(mid, poly))
            return false;
        for (int i = 0; i < poly.Count; i++)
        {
            if (ProperCross(a, b, poly[i], poly[(i + 1) % poly.Count]))
                return false;
        }
        if (hole != null)
        {
            for (int i = 0; i < hole.Count; i++)
            {
                if (ProperCross(a, b, hole[i], hole[(i + 1) % hole.Count]))
                    return false;
            }
        }
        return true;
    }

    static List<int> EarClip(List<Vector2> poly)
    {
        var idx = new List<int>(poly.Count);
        for (int i = 0; i < poly.Count; i++)
            idx.Add(i);
        var tris = new List<int>((poly.Count - 2) * 3);
        int guard = 0;
        while (idx.Count > 3 && guard++ < 20000)
        {
            int n = idx.Count;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                int i0 = idx[(i + n - 1) % n];
                int i1 = idx[i];
                int i2 = idx[(i + 1) % n];
                Vector2 a = poly[i0];
                Vector2 b = poly[i1];
                Vector2 c = poly[i2];
                if (Cross(a, b, c) <= 1e-7f)
                    continue;
                bool bad = false;
                for (int k = 0; k < idx.Count; k++)
                {
                    int j = idx[k];
                    if (j == i0 || j == i1 || j == i2)
                        continue;
                    Vector2 p = poly[j];
                    if (Cross(a, b, p) > 1e-7f && Cross(b, c, p) > 1e-7f && Cross(c, a, p) > 1e-7f)
                    {
                        bad = true;
                        break;
                    }
                }
                if (bad)
                    continue;
                tris.Add(i0);
                tris.Add(i1);
                tris.Add(i2);
                idx.RemoveAt(i);
                found = true;
                break;
            }
            if (!found)
                return null;
        }
        if (idx.Count == 3)
        {
            tris.Add(idx[0]);
            tris.Add(idx[1]);
            tris.Add(idx[2]);
        }
        return tris;
    }

    static List<Vector2> OffsetPolygon(List<Vector2> ccw, float dist)
    {
        int n = ccw.Count;
        var result = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = ccw[(i + n - 1) % n];
            Vector2 cur = ccw[i];
            Vector2 next = ccw[(i + 1) % n];
            Vector2 n0 = LeftNormal(cur - prev);
            Vector2 n1 = LeftNormal(next - cur);
            Vector2 a0 = prev + n0 * dist;
            Vector2 a1 = cur + n0 * dist;
            Vector2 b0 = cur + n1 * dist;
            Vector2 b1 = next + n1 * dist;
            if (!LineIntersect(a0, a1, b0, b1, out Vector2 hit))
                hit = a1;
            result.Add(hit);
        }
        return result;
    }

    static void EnsureWinding(List<Vector2> poly, bool ccw)
    {
        if ((SignedArea(poly) > 0f) != ccw)
            poly.Reverse();
    }

    static void RemoveDuplicates(List<Vector2> poly)
    {
        for (int i = poly.Count - 1; i >= 0; i--)
        {
            Vector2 next = poly[(i + 1) % poly.Count];
            if ((poly[i] - next).sqrMagnitude < 1e-8f)
                poly.RemoveAt(i);
        }
    }

    static void RemoveCollinear(List<Vector2> poly)
    {
        bool removed = true;
        int guard = 0;
        while (removed && poly.Count > 3 && guard++ < poly.Count * 3)
        {
            removed = false;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[(i + poly.Count - 1) % poly.Count];
                Vector2 b = poly[i];
                Vector2 c = poly[(i + 1) % poly.Count];
                if (Mathf.Abs(Cross(a, b, c)) > 1e-4f)
                    continue;
                Vector2 ab = b - a;
                Vector2 ac = c - a;
                float dot = Vector2.Dot(ab, ac);
                if (dot < -1e-4f || dot > ac.sqrMagnitude + 1e-4f)
                    continue;
                poly.RemoveAt(i);
                removed = true;
                break;
            }
        }
    }

    static float MaxX(List<Vector2> poly)
    {
        float x = poly[0].x;
        for (int i = 1; i < poly.Count; i++)
            x = Mathf.Max(x, poly[i].x);
        return x;
    }

    static float SignedArea(List<Vector2> poly)
    {
        float area = 0f;
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[(i + 1) % poly.Count];
            area += a.x * b.y - b.x * a.y;
        }
        return area * 0.5f;
    }

    static float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    static Vector2 LeftNormal(Vector2 edge)
    {
        float m = edge.magnitude;
        if (m < 1e-6f)
            return Vector2.up;
        return new Vector2(-edge.y / m, edge.x / m);
    }

    static bool LineIntersect(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2, out Vector2 hit)
    {
        Vector2 r = p2 - p;
        Vector2 s = q2 - q;
        float den = r.x * s.y - r.y * s.x;
        if (Mathf.Abs(den) < 1e-8f)
        {
            hit = p2;
            return false;
        }
        float t = ((q.x - p.x) * s.y - (q.y - p.y) * s.x) / den;
        hit = p + r * t;
        return true;
    }

    static bool PointInPoly(Vector2 point, IList<Vector2> poly)
    {
        bool inside = false;
        int n = poly.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[(i + 1) % n];
            if ((a.y > point.y) != (b.y > point.y)
                && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    static bool ProperCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float c1 = Cross(a, b, c);
        float c2 = Cross(a, b, d);
        float c3 = Cross(c, d, a);
        float c4 = Cross(c, d, b);
        if (Mathf.Abs(c1) < 1e-6f || Mathf.Abs(c2) < 1e-6f || Mathf.Abs(c3) < 1e-6f || Mathf.Abs(c4) < 1e-6f)
            return false;
        return (c1 > 0f) != (c2 > 0f) && (c3 > 0f) != (c4 > 0f);
    }

    static bool RaySegment(Vector2 origin, Vector2 dir, Vector2 a, Vector2 b, out float t)
    {
        Vector2 v = b - a;
        float den = dir.x * v.y - dir.y * v.x;
        t = 0f;
        if (Mathf.Abs(den) < 1e-8f)
            return false;
        Vector2 w = a - origin;
        float tt = (w.x * v.y - w.y * v.x) / den;
        float u = (w.x * dir.y - w.y * dir.x) / den;
        if (tt > 0.001f && u >= -0.001f && u <= 1.001f)
        {
            t = tt;
            return true;
        }
        return false;
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        return Vector2.Distance(p, ClosestOnSegment(p, a, b));
    }

    static Vector2 ClosestOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 v = b - a;
        float l2 = v.sqrMagnitude;
        float t = l2 < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, v) / l2);
        return a + v * t;
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

    static void AddLabel(GameObject go, string caption, Color color)
    {
        if (go == null)
            return;
        BlockoutLabel label = go.GetComponent<BlockoutLabel>();
        if (label == null)
            label = go.AddComponent<BlockoutLabel>();
        label.Set(caption);
        label.LabelColor = color;
    }

    static GameObject CreateBox(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rotation, Material mat, StaticMode mode, bool castShadows = true, bool door = false, bool withCollider = true)
    {
        return CreatePrimitive(parent, name, center, size, rotation, mat, mode, false, castShadows, door, withCollider);
    }

    static GameObject CreateCylinder(Transform parent, string name, Vector3 center, float radiusX, float radiusZ, float height, Quaternion rotation, Material mat, StaticMode mode)
    {
        return CreatePrimitive(parent, name, center, new Vector3(radiusX * 2f, height, radiusZ * 2f), rotation, mat, mode, true, true, false, true);
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
        static Type s_Face;
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

        public static GameObject CreateTriangles(Vector3[] vertices, int[] triangles)
        {
            Ensure();
            if (!s_Available)
                return null;
            try
            {
                ConstructorInfo ctor = s_Face.GetConstructor(new[] { typeof(IEnumerable<int>) });
                MethodInfo create = null;
                MethodInfo[] methods = s_Mesh.GetMethods(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Name != "Create")
                        continue;
                    ParameterInfo[] parameters = methods[i].GetParameters();
                    if (parameters.Length == 2)
                    {
                        create = methods[i];
                        break;
                    }
                }
                if (ctor == null || create == null)
                    return Fail("ProBuilder Face");

                int faceCount = triangles.Length / 3;
                Array faces = Array.CreateInstance(s_Face, faceCount);
                for (int i = 0; i < faceCount; i++)
                {
                    faces.SetValue(ctor.Invoke(new object[]
                    {
                        new[] { triangles[i * 3], triangles[i * 3 + 1], triangles[i * 3 + 2] }
                    }), i);
                }
                object mesh = create.Invoke(null, new object[] { vertices, faces });
                return Finalize(mesh);
            }
            catch (Exception e)
            {
                return Fail(e.GetBaseException().Message);
            }
        }

        static GameObject Fail(string reason)
        {
            if (!s_Failed)
                Debug.LogWarning("ProBuilder 메시를 만들지 못해 기본 메시를 사용합니다: " + reason);
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
            s_Face = Type.GetType("UnityEngine.ProBuilder.Face, Unity.ProBuilder");
            if (s_Shape == null || s_Pivot == null || s_Mesh == null || s_Face == null)
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
