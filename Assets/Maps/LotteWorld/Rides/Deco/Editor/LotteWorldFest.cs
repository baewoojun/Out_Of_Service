// LotteWorldFest.cs  (v1 - 11/20) 롯데월드 실내 "놀이공원 느낌" 꾸미기: 가로등·깃발 줄·노점·벤치·식탁·수레·차단봉·쓰레기
// 위치: Assets/Maps/LotteWorld/Rides/Deco/Editor/ .  메뉴: Tools > LotteWorld > Fest (놀이공원 꾸미기)
//
//  [놀이공원 꾸미기]
//   - 씬 루트 LotteWorld_Fest 를 새로 만들고 구역별 자식(Fest_Parade, Fest_Carousel, ...) 아래에 프리팹을 놓음
//   - 프리팹: Rides/Props/Prefabs/Prop_*.prefab (정문 소품, Props 창 [1. 프리팹 만들기]로 만든 것)
//            Rides/Deco/Prefabs/Deco_Food_* / Deco_Shop_* (Deco 창 [낱개 프리팹 만들기]로 만든 것)
//   - 좌표 = Unity 월드 (x, y, z), Y 회전(도). 깃발 줄(Prop_Bunting12/18)은 길이에 맞게 X 스케일
//   - 배치표는 블렌더에서 씬(Map3_LotteWorld.unity 11/20 00:24 저장본)을 다시 세워 겹침 검사한 결과
//     (높이 0.35~3.2 m에 뭐가 있으면 최대 3 m 옮기고, 그래도 막히면 뺌). 377개
//   - 구역 체크를 끄면 그 구역은 안 놓음
//  다시 누르면 LotteWorld_Fest 만 지우고 새로 만듦. 다른 루트(Deco·Props·놀이기구)는 안 건드림. Undo 됨. 씬 저장은 직접(Ctrl+S).
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Fest
{
    public class FestWindow : EditorWindow
    {
        const string RootName = "LotteWorld_Fest";
        const string PropDir = "Assets/Maps/LotteWorld/Rides/Props/Prefabs";
        const string DecoDir = "Assets/Maps/LotteWorld/Rides/Deco/Prefabs";
        const string FoliageDir = "Assets/Maps/LotteWorld/Foliage/Prefabs";
        static bool[] on;
        Vector2 scroll;

        [MenuItem("Tools/LotteWorld/Fest (놀이공원 꾸미기)")]
        public static void Open() { GetWindow<FestWindow>("LW Fest"); }

        struct F { public string n; public float x, y, z, yaw, sx; public int zone;
            public F(string n, float x, float y, float z, float yaw, float sx, int zone) { this.n = n; this.x = x; this.y = y; this.z = z; this.yaw = yaw; this.sx = sx; this.zone = zone; } }
        struct Z { public string obj, label; public Z(string o, string l) { obj = o; label = l; } }

        static readonly Z[] Zones =
        {
            new Z("Fest_Carousel", "② 회전목마 광장"),
            new Z("Fest_SE_Plaza", "③ 동남 작은 광장"),
            new Z("Fest_South_Street", "④ 남쪽 노점 거리"),
            new Z("Fest_NE_Walk", "⑤ 링크-A15 사이"),
            new Z("Fest_Basket", "⑦ 바스켓 둘레"),
            new Z("Fest_A_GateCourt", "A 정문 안쪽 마당"),
            new Z("Fest_B_CarouselNorth", "B 회전목마 북쪽 길"),
            new Z("Fest_C_CastleFront", "C 성·상가 앞 먹거리 마당"),
            new Z("Fest_D_SWCorner", "D 남서쪽 구석"),
            new Z("Fest_E_FoodCourt", "E 남쪽 먹거리 광장"),
            new Z("Fest_F_FlumeFront", "F 후룸 앞"),
            new Z("Fest_G_CastleWest", "G 성 서쪽 마당"),
            new Z("Fest_Parade", "① 퍼레이드 길 둘레"),
            new Z("Fest_West", "⑥ 서쪽 산책로·대로")
        };

        // (프리팹, x, y, z, Y회전, X스케일, 구역)
        static readonly F[] Items =
        {
            new F("Prop_Lamp_On", 114.885f, 0.000f, 10.252f, -105.00f, 1.000f, 0),
            new F("Prop_Lamp_On", 107.483f, 0.000f, 23.073f, -135.00f, 1.000f, 0),
            new F("Prop_Lamp_Half", 94.662f, 0.000f, 30.475f, -165.00f, 1.000f, 0),
            new F("Prop_Lamp_On", 79.858f, 0.000f, 30.475f, 165.00f, 1.000f, 0),
            new F("Prop_Lamp_Off", 66.330f, 0.000f, 22.366f, 135.00f, 1.000f, 0),
            new F("Prop_Lamp_On", 59.635f, 0.000f, 10.252f, 105.00f, 1.000f, 0),
            new F("Prop_Lamp_On", 59.635f, 0.000f, -4.552f, 75.00f, 1.000f, 0),
            new F("Prop_Lamp_Half", 67.037f, 0.000f, -17.373f, 45.00f, 1.000f, 0),
            new F("Prop_Lamp_On", 79.858f, 0.000f, -24.775f, 15.00f, 1.000f, 0),
            new F("Prop_Lamp_Off", 94.662f, 0.000f, -24.775f, -15.00f, 1.000f, 0),
            new F("Prop_Lamp_On", 107.483f, 0.000f, -17.373f, -45.00f, 1.000f, 0),
            new F("Prop_Lamp_On", 114.885f, 0.000f, -4.552f, -75.00f, 1.000f, 0),
            new F("Prop_Lamp_On", 54.500f, 0.000f, -35.500f, -135.00f, 1.000f, 1),
            new F("Prop_Lamp_On", 43.500f, 0.000f, -35.500f, 135.00f, 1.000f, 1),
            new F("Prop_Lamp_Half", 43.500f, 0.000f, -46.500f, 45.00f, 1.000f, 1),
            new F("Prop_Lamp_On", 54.500f, 0.000f, -46.500f, -45.00f, 1.000f, 1),
            new F("Prop_Lamp_On", -30.000f, 0.000f, -51.500f, 0.00f, 1.000f, 2),
            new F("Prop_Lamp_Half", -9.000f, 0.000f, -51.500f, 0.00f, 1.000f, 2),
            new F("Prop_Lamp_On", 5.000f, 0.000f, -51.500f, 0.00f, 1.000f, 2),
            new F("Prop_Lamp_Off", 24.000f, 0.000f, -51.500f, 0.00f, 1.000f, 2),
            new F("Prop_Lamp_On", 21.293f, 0.000f, 51.053f, 180.00f, 1.000f, 3),
            new F("Prop_Lamp_On", 34.000f, 0.000f, 50.320f, 180.00f, 1.000f, 3),
            new F("Prop_Lamp_Half", 46.000f, 0.000f, 48.880f, 180.00f, 1.000f, 3),
            new F("Prop_Lamp_On", 58.000f, 0.000f, 47.440f, 180.00f, 1.000f, 3),
            new F("Prop_Lamp_On", -40.117f, 0.000f, -42.341f, -112.50f, 1.000f, 4),
            new F("Prop_Lamp_On", -52.792f, 0.000f, -31.080f, -157.50f, 1.000f, 4),
            new F("Prop_Lamp_Half", -66.302f, 0.000f, -32.494f, 157.50f, 1.000f, 4),
            new F("Prop_Lamp_On", -76.856f, 0.000f, -43.048f, 112.50f, 1.000f, 4),
            new F("Prop_Lamp_Off", -76.856f, 0.000f, -57.972f, 67.50f, 1.000f, 4),
            new F("Prop_Lamp_On", -66.302f, 0.000f, -68.526f, 22.50f, 1.000f, 4),
            new F("Prop_Lamp_On", -51.378f, 0.000f, -68.526f, -22.50f, 1.000f, 4),
            new F("Prop_Lamp_Half", -40.824f, 0.000f, -57.972f, -67.50f, 1.000f, 4),
            new F("Prop_Lamp_On", -55.757f, 0.000f, 117.243f, -135.00f, 1.000f, 5),
            new F("Prop_Lamp_On", -64.950f, 0.000f, 117.950f, 135.00f, 1.000f, 5),
            new F("Prop_Lamp_Half", -64.950f, 0.000f, 108.050f, 45.00f, 1.000f, 5),
            new F("Prop_Lamp_On", -55.050f, 0.000f, 108.050f, -45.00f, 1.000f, 5),
            new F("Prop_Lamp_On", 77.317f, 0.000f, 43.975f, 8.21f, 1.000f, 6),
            new F("Prop_Lamp_Half", 78.601f, 0.000f, 52.883f, -171.79f, 1.000f, 6),
            new F("Prop_Lamp_On", 88.337f, 0.000f, 42.386f, 8.21f, 1.000f, 6),
            new F("Prop_Lamp_On", 89.622f, 0.000f, 51.293f, -171.79f, 1.000f, 6),
            new F("Prop_Lamp_Half", 99.358f, 0.000f, 40.796f, 8.21f, 1.000f, 6),
            new F("Prop_Lamp_Off", 100.642f, 0.000f, 49.704f, -171.79f, 1.000f, 6),
            new F("Prop_Lamp_On", 110.378f, 0.000f, 39.207f, 8.21f, 1.000f, 6),
            new F("Prop_Lamp_On", 111.663f, 0.000f, 48.114f, -171.79f, 1.000f, 6),
            new F("Prop_Lamp_Off", 121.399f, 0.000f, 37.617f, 8.21f, 1.000f, 6),
            new F("Prop_Lamp_On", 123.683f, 0.000f, 46.525f, -171.79f, 1.000f, 6),
            new F("Prop_Lamp_On", -167.000f, 0.000f, -24.000f, 90.00f, 1.000f, 7),
            new F("Prop_Lamp_On", -167.000f, 0.000f, -2.000f, 90.00f, 1.000f, 7),
            new F("Prop_Lamp_Half", -167.000f, 0.000f, 20.000f, 90.00f, 1.000f, 7),
            new F("Prop_Lamp_On", -167.000f, 0.000f, 42.000f, 90.00f, 1.000f, 7),
            new F("Prop_Lamp_On", -174.000f, 0.000f, -59.500f, 180.00f, 1.000f, 8),
            new F("Prop_Lamp_On", -179.629f, 0.000f, -69.250f, 60.00f, 1.000f, 8),
            new F("Prop_Lamp_Half", -168.371f, 0.000f, -69.250f, -60.00f, 1.000f, 8),
            new F("Prop_Lamp_On", -90.000f, 0.000f, -100.000f, -90.00f, 1.000f, 9),
            new F("Prop_Lamp_On", -95.000f, 0.000f, -91.340f, -150.00f, 1.000f, 9),
            new F("Prop_Lamp_Half", -105.000f, 0.000f, -91.340f, 150.00f, 1.000f, 9),
            new F("Prop_Lamp_On", -110.000f, 0.000f, -100.000f, 90.00f, 1.000f, 9),
            new F("Prop_Lamp_Off", -105.000f, 0.000f, -108.660f, 30.00f, 1.000f, 9),
            new F("Prop_Lamp_On", -95.000f, 0.000f, -108.660f, -30.00f, 1.000f, 9),
            new F("Prop_Lamp_On", -47.000f, 0.000f, -68.000f, 0.00f, 1.000f, 10),
            new F("Prop_Lamp_On", -41.000f, 0.000f, -78.000f, 0.00f, 1.000f, 10),
            new F("Prop_Lamp_Half", -47.000f, 0.000f, -88.000f, 0.00f, 1.000f, 10),
            new F("Prop_Lamp_On", 55.010f, 0.000f, -59.990f, -135.00f, 1.000f, 11),
            new F("Prop_Lamp_On", 42.990f, 0.000f, -59.990f, 135.00f, 1.000f, 11),
            new F("Prop_Lamp_Half", 42.990f, 0.000f, -72.010f, 45.00f, 1.000f, 11),
            new F("Prop_Lamp_On", 55.010f, 0.000f, -72.010f, -45.00f, 1.000f, 11),
            new F("Prop_Bunting12", -8.275f, 4.300f, -41.700f, -0.00f, 1.121f, 12),
            new F("Prop_Bunting12", 5.160f, 4.300f, -41.700f, -0.00f, 1.118f, 12),
            new F("Prop_Bunting12", 18.520f, 4.300f, -41.065f, -5.45f, 1.113f, 12),
            new F("Prop_Bunting12", 31.295f, 4.300f, -37.780f, -23.40f, 1.112f, 12),
            new F("Prop_Bunting12", 42.395f, 4.300f, -30.675f, -41.84f, 1.113f, 12),
            new F("Prop_Bunting12", 55.310f, 4.300f, -8.085f, -78.68f, 1.112f, 12),
            new F("Prop_Bunting12", 55.795f, 4.300f, 5.085f, -97.10f, 1.113f, 12),
            new F("Prop_Bunting12", 52.095f, 4.300f, 17.735f, -115.51f, 1.113f, 12),
            new F("Prop_Bunting12", 44.590f, 4.300f, 28.565f, -133.94f, 1.112f, 12),
            new F("Prop_Bunting12", 34.045f, 4.300f, 36.465f, -152.38f, 1.113f, 12),
            new F("Prop_Bunting12", 8.245f, 4.300f, 41.700f, -180.00f, 1.117f, 12),
            new F("Prop_Bunting12", -5.165f, 4.300f, 41.700f, -180.00f, 1.117f, 12),
            new F("Prop_Bunting12", -18.520f, 4.300f, 41.065f, 174.55f, 1.113f, 12),
            new F("Prop_Bunting12", -31.295f, 4.300f, 37.780f, 156.60f, 1.112f, 12),
            new F("Prop_Bunting12", -42.395f, 4.300f, 30.675f, 138.16f, 1.113f, 12),
            new F("Prop_Bunting12", -55.310f, 4.300f, 8.085f, 101.32f, 1.112f, 12),
            new F("Prop_Bunting12", -55.795f, 4.300f, -5.085f, 82.90f, 1.113f, 12),
            new F("Prop_Bunting12", -52.095f, 4.300f, -17.735f, 64.49f, 1.113f, 12),
            new F("Prop_Bunting12", -44.590f, 4.300f, -28.565f, 46.06f, 1.112f, 12),
            new F("Prop_Bunting12", -34.045f, 4.300f, -36.465f, 27.62f, 1.113f, 12),
            new F("Tree_Leafy_A", 20.000f, 0.000f, 52.000f, 243.77f, 1.000f, 12),
            new F("Prop_Bench", 21.500f, 0.000f, 47.980f, 112.20f, 1.000f, 12),
            new F("Prop_Bench", 18.207f, 0.000f, 50.727f, -67.80f, 1.000f, 12),
            new F("Prop_Bin", 18.942f, 0.000f, 46.408f, -157.80f, 1.000f, 12),
            new F("Tree_Dead_A", -28.586f, 0.000f, 49.414f, 282.57f, 1.000f, 12),
            new F("Prop_Bench", -27.710f, 0.000f, 49.431f, 57.99f, 1.000f, 12),
            new F("Prop_Bench", -30.169f, 0.000f, 48.690f, -122.01f, 1.000f, 12),
            new F("Prop_Bin", -28.516f, 0.000f, 47.626f, 147.99f, 1.000f, 12),
            new F("Tree_Leafy_A", 62.000f, 0.000f, 10.000f, 187.37f, 1.000f, 12),
            new F("Prop_Bench", 62.430f, 0.000f, 7.334f, 170.84f, 1.000f, 12),
            new F("Prop_Bench", 61.570f, 0.000f, 12.666f, -9.16f, 1.000f, 12),
            new F("Prop_Bin", 59.236f, 0.000f, 9.554f, -99.16f, 1.000f, 12),
            new F("Tree_Dead_A", -61.000f, 0.000f, 6.000f, 184.14f, 1.000f, 12),
            new F("Prop_Bench", -62.744f, 0.000f, 8.688f, 5.44f, 1.000f, 12),
            new F("Prop_Bench", -62.256f, 0.000f, 3.312f, -174.56f, 1.000f, 12),
            new F("Prop_Bin", -60.213f, 0.000f, 5.735f, 95.44f, 1.000f, 12),
            new F("Prop_Cart_Balloons", 60.500f, 0.000f, -3.000f, -90.00f, 1.000f, 12),
            new F("Prop_Cart", -62.000f, 0.000f, -14.000f, 78.69f, 1.000f, 12),
            new F("Prop_Sign", 58.000f, 0.000f, 18.000f, -106.70f, 1.000f, 12),
            new F("Prop_Sign", -58.500f, 0.000f, 20.000f, 106.70f, 1.000f, 12),
            new F("Prop_Litter", -9.374f, 0.000f, 52.430f, 104.17f, 1.000f, 12),
            new F("Prop_Litter", 1.870f, 0.000f, 47.513f, 93.76f, 1.000f, 12),
            new F("Prop_Litter", -1.499f, 0.000f, 47.844f, 38.75f, 1.000f, 12),
            new F("Prop_Litter", 53.866f, 0.000f, -17.785f, 204.93f, 1.000f, 12),
            new F("Prop_Litter", 55.170f, 0.000f, -19.460f, 72.92f, 1.000f, 12),
            new F("Prop_Litter", -58.000f, 0.000f, -5.814f, 264.05f, 1.000f, 12),
            new F("Prop_Litter", -53.923f, 0.000f, -8.136f, 35.77f, 1.000f, 12),
            new F("Prop_Bunting12", 111.184f, 4.300f, 16.663f, -120.00f, 1.234f, 0),
            new F("Prop_Bunting12", 101.073f, 4.300f, 26.774f, -150.00f, 1.234f, 0),
            new F("Prop_Bunting12", 87.260f, 4.300f, 30.475f, -180.00f, 1.234f, 0),
            new F("Prop_Bunting12", 73.094f, 4.300f, 26.420f, 149.06f, 1.314f, 0),
            new F("Prop_Bunting12", 62.982f, 4.300f, 16.309f, 118.93f, 1.153f, 0),
            new F("Prop_Bunting12", 73.447f, 4.300f, -21.074f, 30.00f, 1.234f, 0),
            new F("Prop_Bunting12", 87.260f, 4.300f, -24.775f, 0.00f, 1.234f, 0),
            new F("Prop_Bunting12", 101.073f, 4.300f, -21.074f, -30.00f, 1.234f, 0),
            new F("Prop_Bunting12", 111.184f, 4.300f, -10.963f, -60.00f, 1.234f, 0),
            new F("Prop_Bunting12", 114.885f, 4.300f, 2.850f, -90.00f, 1.234f, 0),
            new F("Prop_Bunting12", 111.018f, 4.400f, 9.216f, 165.00f, 0.667f, 0),
            new F("Prop_Bunting12", 80.894f, 4.400f, 26.608f, 75.00f, 0.667f, 0),
            new F("Prop_Bunting12", 93.626f, 4.400f, -20.908f, -105.00f, 0.667f, 0),
            new F("Prop_Stanchions", 64.110f, 0.000f, 1.350f, -90.00f, 1.000f, 0),
            new F("Prop_Stanchions", 63.060f, 0.000f, 1.350f, -90.00f, 1.000f, 0),
            new F("Prop_Stanchions", 62.010f, 0.000f, 1.350f, -90.00f, 1.000f, 0),
            new F("Prop_Sign", 59.660f, 0.000f, -3.150f, -90.00f, 1.000f, 0),
            new F("Prop_Medallion", 53.760f, 0.000f, 2.850f, 0.00f, 1.000f, 0),
            new F("Prop_Cart_Balloons", 56.260f, 0.000f, 12.350f, -111.80f, 1.000f, 0),
            new F("Prop_Cart", 55.760f, 0.000f, -7.650f, -68.20f, 1.000f, 0),
            new F("Prop_Bench", 102.160f, 0.000f, 28.658f, -150.00f, 1.000f, 0),
            new F("Prop_Bin", 100.586f, 0.000f, 29.505f, 0.00f, 1.000f, 0),
            new F("Prop_Bench", 72.360f, 0.000f, 28.658f, 150.00f, 1.000f, 0),
            new F("Prop_Bin", 70.839f, 0.000f, 27.718f, 0.00f, 1.000f, 0),
            new F("Prop_Bench", 72.360f, 0.000f, -22.958f, 30.00f, 1.000f, 0),
            new F("Prop_Bin", 73.934f, 0.000f, -23.805f, 0.00f, 1.000f, 0),
            new F("Prop_Bench", 102.160f, 0.000f, -22.958f, -30.00f, 1.000f, 0),
            new F("Prop_Bin", 103.681f, 0.000f, -22.018f, 0.00f, 1.000f, 0),
            new F("Prop_Bin_Tipped", 58.260f, 0.000f, 6.850f, 30.00f, 1.000f, 0),
            new F("Prop_Litter", 56.625f, 0.000f, -1.639f, 142.58f, 1.000f, 0),
            new F("Prop_Litter", 56.279f, 0.000f, 1.347f, 329.36f, 1.000f, 0),
            new F("Prop_Litter", 59.637f, 0.000f, -1.264f, 343.41f, 1.000f, 0),
            new F("Prop_Medallion", 49.000f, 0.000f, -41.000f, 0.00f, 1.000f, 1),
            new F("Prop_Bunting12", 49.000f, 4.300f, -35.500f, -180.00f, 0.917f, 1),
            new F("Prop_Bunting12", 43.500f, 4.300f, -41.000f, 90.00f, 0.917f, 1),
            new F("Prop_Bunting12", 49.000f, 4.300f, -46.500f, -0.00f, 0.917f, 1),
            new F("Prop_Bunting12", 54.500f, 4.300f, -41.000f, -90.00f, 0.917f, 1),
            new F("Prop_Bunting12", 49.000f, 4.450f, -41.000f, 135.00f, 1.296f, 1),
            new F("Prop_Bunting12", 49.000f, 4.450f, -41.000f, 45.00f, 1.296f, 1),
            new F("Prop_Bench", 49.000f, 0.000f, -36.800f, 180.00f, 1.000f, 1),
            new F("Prop_Bench", 49.000f, 0.000f, -45.200f, 0.00f, 1.000f, 1),
            new F("Prop_Cart_Balloons", 46.000f, 0.000f, -41.000f, 90.00f, 1.000f, 1),
            new F("Prop_Bin", 53.000f, 0.000f, -39.500f, 0.00f, 1.000f, 1),
            new F("Prop_Sign", 55.500f, 0.000f, -34.000f, -45.00f, 1.000f, 1),
            new F("Prop_Litter", 49.354f, 0.000f, -37.748f, 263.30f, 1.000f, 1),
            new F("Prop_Litter", 49.739f, 0.000f, -43.222f, 49.88f, 1.000f, 1),
            new F("Prop_Litter", 48.559f, 0.000f, -41.447f, 253.93f, 1.000f, 1),
            new F("Prop_Bunting12", -29.065f, 4.300f, -45.530f, -81.10f, 1.007f, 2),
            new F("Prop_Bunting12", -12.000f, 4.300f, -46.600f, -121.48f, 0.958f, 2),
            new F("Prop_Bunting12", 24.585f, 4.300f, -45.965f, -83.97f, 0.928f, 2),
            new F("Prop_Bunting18", -19.500f, 4.300f, -51.500f, -0.00f, 1.167f, 2),
            new F("Prop_Bunting12", -2.000f, 4.300f, -51.500f, -0.00f, 1.167f, 2),
            new F("Prop_Bunting18", 14.500f, 4.300f, -51.500f, -0.00f, 1.056f, 2),
            new F("Prop_Bench", -25.000f, 0.000f, -49.800f, 0.00f, 1.000f, 2),
            new F("Prop_Bin", -23.400f, 0.000f, -49.800f, 0.00f, 1.000f, 2),
            new F("Prop_Bench", 17.500f, 0.000f, -49.800f, 0.00f, 1.000f, 2),
            new F("Prop_Bin", 17.100f, 0.000f, -49.800f, 0.00f, 1.000f, 2),
            new F("Prop_Sign", -43.500f, 0.000f, -41.500f, 63.43f, 1.000f, 2),
            new F("Prop_Cart_Balloons", 36.000f, 0.000f, -46.500f, -73.30f, 1.000f, 2),
            new F("Prop_Bin_Tipped", -5.000f, 0.000f, -52.500f, 200.00f, 1.000f, 2),
            new F("Prop_Litter", 8.173f, 0.000f, -47.643f, 172.71f, 1.000f, 2),
            new F("Prop_Litter", 6.311f, 0.000f, -42.094f, 180.02f, 1.000f, 2),
            new F("Prop_Litter", -10.961f, 0.000f, -36.162f, 311.79f, 1.000f, 2),
            new F("Prop_Litter", 3.398f, 0.000f, -50.808f, 9.32f, 1.000f, 2),
            new F("Prop_Bunting12", 27.646f, 4.300f, 50.686f, 3.30f, 1.061f, 3),
            new F("Prop_Bunting12", 40.000f, 4.300f, 49.600f, 6.84f, 1.007f, 3),
            new F("Prop_Bunting12", 52.000f, 4.300f, 48.160f, 6.84f, 1.007f, 3),
            new F("Prop_Bunting18", 48.980f, 4.300f, 40.405f, 142.05f, 1.271f, 3),
            new F("Tree_Leafy_A", 40.707f, 0.000f, 57.207f, 135.58f, 1.000f, 3),
            new F("Prop_Bench", 37.300f, 0.000f, 56.500f, -90.00f, 1.000f, 3),
            new F("Prop_Bench", 42.700f, 0.000f, 56.500f, 90.00f, 1.000f, 3),
            new F("Prop_Bin", 40.000f, 0.000f, 59.300f, 0.00f, 1.000f, 3),
            new F("Prop_Cart_Balloons", 52.000f, 0.000f, 57.000f, 180.00f, 1.000f, 3),
            new F("Prop_Sign", 28.000f, 0.000f, 56.000f, 180.00f, 1.000f, 3),
            new F("Prop_Litter", 45.331f, 0.000f, 54.617f, 94.66f, 1.000f, 3),
            new F("Prop_Litter", 41.964f, 0.000f, 52.048f, 145.23f, 1.000f, 3),
            new F("Prop_Litter", 39.567f, 0.000f, 45.410f, 108.42f, 1.000f, 3),
            new F("Prop_Bunting18", -147.200f, 4.300f, 9.000f, -90.00f, 1.222f, 13),
            new F("Prop_Bunting18", -147.200f, 4.300f, 31.000f, -90.00f, 1.222f, 13),
            new F("Prop_Bunting18", -62.804f, 4.300f, 34.828f, -178.14f, 1.317f, 13),
            new F("Prop_Bunting18", -85.327f, 4.300f, 40.256f, -154.70f, 1.312f, 13),
            new F("Prop_Bunting18", -129.885f, 4.300f, 40.312f, -163.10f, 1.380f, 13),
            new F("Prop_Bunting18", -73.224f, 4.300f, -23.905f, 149.68f, 1.298f, 13),
            new F("Prop_Bunting18", -121.108f, 4.300f, -43.047f, 151.88f, 1.120f, 13),
            new F("Prop_Bunting18", -138.661f, 4.300f, -40.236f, -138.87f, 1.278f, 13),
            new F("Prop_Cart_Balloons", -140.000f, 0.000f, 8.000f, -90.00f, 1.000f, 13),
            new F("Prop_Cart", -140.000f, 0.000f, 31.000f, -90.00f, 1.000f, 13),
            new F("Tree_Dead_A", -138.500f, 0.000f, -14.000f, 243.85f, 1.000f, 13),
            new F("Prop_Bench", -138.500f, 0.000f, -11.300f, -0.00f, 1.000f, 13),
            new F("Prop_Bench", -138.500f, 0.000f, -16.700f, 180.00f, 1.000f, 13),
            new F("Prop_Bin", -135.700f, 0.000f, -14.000f, 90.00f, 1.000f, 13),
            new F("Prop_Sign", -128.000f, 0.000f, -56.000f, 16.70f, 1.000f, 13),
            new F("Prop_Stanchions", -132.494f, 0.000f, -62.920f, -28.81f, 1.000f, 13),
            new F("Prop_Stanchions", -133.000f, 0.000f, -62.000f, -28.81f, 1.000f, 13),
            new F("Prop_Stanchions", -133.506f, 0.000f, -61.080f, -28.81f, 1.000f, 13),
            new F("Prop_Litter", -141.988f, 0.000f, 10.881f, 247.49f, 1.000f, 13),
            new F("Prop_Litter", -144.984f, 0.000f, 8.480f, 23.93f, 1.000f, 13),
            new F("Prop_Litter", -154.841f, 0.000f, 9.657f, 5.80f, 1.000f, 13),
            new F("Prop_Litter", -103.799f, 0.000f, 41.600f, 30.64f, 1.000f, 13),
            new F("Prop_Litter", -97.959f, 0.000f, 43.771f, 168.13f, 1.000f, 13),
            new F("Prop_Bunting12", -71.579f, 4.300f, -37.771f, 135.00f, 1.244f, 4),
            new F("Prop_Bunting12", -71.579f, 4.300f, -63.249f, 45.00f, 1.244f, 4),
            new F("Prop_Bunting12", -58.840f, 4.300f, -68.526f, -0.00f, 1.244f, 4),
            new F("Prop_Bunting12", -46.101f, 4.300f, -63.249f, -45.00f, 1.244f, 4),
            new F("Prop_Bunting12", -40.470f, 4.300f, -50.156f, -87.41f, 1.304f, 4),
            new F("Prop_Stanchions", -54.840f, 0.000f, -33.560f, -0.00f, 1.000f, 4),
            new F("Prop_Stanchions", -54.840f, 0.000f, -33.510f, -0.00f, 1.000f, 4),
            new F("Prop_Stanchions", -54.840f, 0.000f, -31.460f, -0.00f, 1.000f, 4),
            new F("Prop_Sign", -60.840f, 0.000f, -30.010f, 0.00f, 1.000f, 4),
            new F("Prop_Cart", -40.840f, 0.000f, -40.510f, 63.43f, 1.000f, 4),
            new F("Prop_Litter", -56.005f, 0.000f, -30.099f, 316.35f, 1.000f, 4),
            new F("Prop_Litter", -59.005f, 0.000f, -32.883f, 233.46f, 1.000f, 4),
            new F("Prop_Bunting12", -60.354f, 4.300f, 117.596f, -175.60f, 0.768f, 5),
            new F("Prop_Bunting12", -60.000f, 4.300f, 108.050f, -0.00f, 0.825f, 5),
            new F("Prop_Bunting12", -55.404f, 4.300f, 112.646f, -94.40f, 0.768f, 5),
            new F("Prop_Bunting12", -60.354f, 4.450f, 112.646f, 135.00f, 1.083f, 5),
            new F("Prop_Bunting12", -60.000f, 4.450f, 113.000f, 45.00f, 1.167f, 5),
            new F("Deco_Food_Popcorn", -69.500f, 0.000f, 113.000f, 90.00f, 1.000f, 5),
            new F("Deco_Shop_Souvenir", -60.000f, 0.000f, 103.000f, 0.00f, 1.000f, 5),   // 11/20 정문 앞 길에서 비킴 (원래 -49.5, 113)
            new F("Prop_Bench", -60.000f, 0.000f, 109.000f, 0.00f, 1.000f, 5),
            new F("Prop_Bench", -58.586f, 0.000f, 118.414f, 180.00f, 1.000f, 5),
            new F("Prop_Bin", -58.000f, 0.000f, 108.800f, 0.00f, 1.000f, 5),
            new F("Prop_Cart_Balloons", -64.000f, 0.000f, 121.500f, 180.00f, 1.000f, 5),
            new F("Prop_Litter", -60.041f, 0.000f, 112.954f, 138.49f, 1.000f, 5),
            new F("Prop_Litter", -63.834f, 0.000f, 107.565f, 334.42f, 1.000f, 5),
            new F("Prop_Litter", -63.738f, 0.000f, 111.263f, 345.05f, 1.000f, 5),
            new F("Prop_Bunting12", 77.959f, 4.400f, 48.429f, -81.80f, 0.750f, 6),
            new F("Prop_Bunting12", 83.469f, 4.300f, 47.634f, -30.74f, 1.193f, 6),
            new F("Prop_Bunting12", 83.469f, 4.300f, 47.635f, 47.15f, 1.193f, 6),
            new F("Prop_Bunting12", 88.980f, 4.400f, 46.840f, -81.79f, 0.750f, 6),
            new F("Prop_Bunting12", 94.489f, 4.300f, 46.045f, -30.74f, 1.193f, 6),
            new F("Prop_Bunting12", 94.490f, 4.300f, 46.044f, 47.15f, 1.193f, 6),
            new F("Prop_Bunting12", 100.000f, 4.400f, 45.250f, -81.80f, 0.750f, 6),
            new F("Prop_Bunting12", 105.511f, 4.300f, 44.455f, -30.74f, 1.193f, 6),
            new F("Prop_Bunting12", 105.510f, 4.300f, 44.456f, 47.15f, 1.193f, 6),
            new F("Prop_Bunting12", 111.020f, 4.400f, 43.660f, -81.79f, 0.750f, 6),
            new F("Prop_Bunting12", 116.531f, 4.300f, 42.865f, 47.15f, 1.193f, 6),
            new F("Deco_Food_IceCream", 85.111f, 0.000f, 55.985f, -171.79f, 1.000f, 6),
            new F("Deco_Shop_Souvenir", 98.978f, 0.000f, 54.130f, -171.79f, 1.000f, 6),
            new F("Deco_Shop_Headbands", 121.732f, 0.000f, 50.704f, -171.79f, 1.000f, 6),
            new F("Prop_Dining_Cloth", 97.947f, 0.000f, 33.757f, 82.93f, 1.000f, 6),
            new F("Prop_Dining_Plain", 97.947f, 0.000f, 37.157f, 166.85f, 1.000f, 6),
            new F("Prop_Dining_ClothTopple", 101.347f, 0.000f, 33.757f, 184.61f, 1.000f, 6),
            new F("Prop_Dining_Cloth", 101.347f, 0.000f, 37.157f, 76.92f, 1.000f, 6),
            new F("Prop_Dining_Cloth", 104.747f, 0.000f, 33.757f, 180.71f, 1.000f, 6),
            new F("Prop_Dining_ClothTopple", 104.747f, 0.000f, 37.157f, 104.64f, 1.000f, 6),
            new F("Tree_Leafy_A", 88.480f, 0.000f, 37.313f, 289.49f, 1.000f, 6),
            new F("Prop_Bench", 91.152f, 0.000f, 36.928f, 98.21f, 1.000f, 6),
            new F("Prop_Bench", 85.808f, 0.000f, 37.699f, -81.79f, 1.000f, 6),
            new F("Prop_Bin", 88.080f, 0.000f, 34.542f, -171.79f, 1.000f, 6),
            new F("Prop_Sign", 75.694f, 0.000f, 46.735f, -81.79f, 1.000f, 6),
            new F("Prop_Cart", 114.285f, 0.000f, 34.097f, 8.21f, 1.000f, 6),
            new F("Prop_Litter", 102.310f, 0.000f, 37.852f, 258.00f, 1.000f, 6),
            new F("Prop_Litter", 99.399f, 0.000f, 41.017f, 268.59f, 1.000f, 6),
            new F("Prop_Litter", 90.834f, 0.000f, 43.679f, 204.91f, 1.000f, 6),
            new F("Prop_Litter", 105.724f, 0.000f, 33.639f, 313.81f, 1.000f, 6),
            new F("Prop_Litter", 99.825f, 0.000f, 45.830f, 307.13f, 1.000f, 6),
            new F("Prop_Bunting18", -157.100f, 4.400f, -24.000f, -0.00f, 1.100f, 7),
            new F("Prop_Bunting18", -157.100f, 4.400f, -2.000f, -0.00f, 1.100f, 7),
            new F("Prop_Bunting18", -157.100f, 4.400f, 20.000f, -0.00f, 1.100f, 7),
            new F("Prop_Bunting18", -157.100f, 4.400f, 42.000f, -0.00f, 1.100f, 7),
            new F("Prop_Bunting18", -167.000f, 4.300f, -13.000f, -90.00f, 1.222f, 7),
            new F("Prop_Bunting18", -167.000f, 4.300f, 9.000f, -90.00f, 1.222f, 7),
            new F("Prop_Bunting18", -167.000f, 4.300f, 31.000f, -90.00f, 1.222f, 7),
            new F("Deco_Food_Churros", -158.000f, 0.000f, -30.000f, 90.00f, 1.000f, 7),
            new F("Deco_Food_Popcorn", -158.000f, 0.000f, -13.000f, 90.00f, 1.000f, 7),
            new F("Deco_Food_IceCream", -158.000f, 0.000f, 37.000f, 90.00f, 1.000f, 7),
            new F("Deco_Shop_Souvenir", -157.000f, 0.000f, 49.000f, 90.00f, 1.000f, 7),
            new F("Prop_Dining_Cloth", -164.700f, 0.000f, -16.400f, 0.39f, 1.000f, 7),
            new F("Prop_Dining_Plain", -164.700f, 0.000f, -13.000f, -7.18f, 1.000f, 7),
            new F("Prop_Dining_Cloth", -164.700f, 0.000f, -9.600f, 275.88f, 1.000f, 7),
            new F("Prop_Dining_ClothTopple", -161.300f, 0.000f, -16.400f, 174.89f, 1.000f, 7),
            new F("Prop_Dining_Cloth", -161.300f, 0.000f, -13.000f, 88.36f, 1.000f, 7),
            new F("Prop_Dining_Plain", -161.300f, 0.000f, -9.600f, 194.13f, 1.000f, 7),
            new F("Prop_Dining_Cloth", -164.700f, 0.000f, 29.300f, 4.08f, 1.000f, 7),
            new F("Prop_Dining_Plain", -164.700f, 0.000f, 32.700f, 267.39f, 1.000f, 7),
            new F("Prop_Dining_ClothTopple", -161.300f, 0.000f, 29.300f, 11.15f, 1.000f, 7),
            new F("Prop_Dining_Cloth", -161.300f, 0.000f, 32.700f, -5.49f, 1.000f, 7),
            new F("Prop_Bench", -170.000f, 0.000f, -6.000f, 90.00f, 1.000f, 7),
            new F("Prop_PlantPot", -170.000f, 0.000f, -4.000f, 0.00f, 1.000f, 7),
            new F("Prop_Bench", -170.000f, 0.000f, 9.000f, 90.00f, 1.000f, 7),
            new F("Prop_PlantPot", -170.000f, 0.000f, 11.000f, 0.00f, 1.000f, 7),
            new F("Prop_Bench", -170.000f, 0.000f, 25.000f, 90.00f, 1.000f, 7),
            new F("Prop_PlantPot", -170.000f, 0.000f, 27.000f, 0.00f, 1.000f, 7),
            new F("Prop_Cart_Balloons", -165.000f, 0.000f, 4.000f, 90.00f, 1.000f, 7),
            new F("Prop_Sign", -162.000f, 0.000f, -36.000f, 116.57f, 1.000f, 7),
            new F("Prop_Litter", -170.955f, 0.000f, 16.809f, 132.16f, 1.000f, 7),
            new F("Prop_Litter", -164.551f, 0.000f, 13.262f, 351.82f, 1.000f, 7),
            new F("Prop_Litter", -178.120f, 0.000f, 20.246f, 59.62f, 1.000f, 7),
            new F("Prop_Litter", -168.731f, 0.000f, -1.494f, 255.56f, 1.000f, 7),
            new F("Prop_Litter", -180.408f, 0.000f, 6.447f, 211.52f, 1.000f, 7),
            new F("Prop_Bunting12", -176.815f, 4.300f, -64.375f, 120.00f, 0.938f, 8),
            new F("Prop_Bunting12", -174.000f, 4.300f, -69.250f, -0.00f, 0.938f, 8),
            new F("Prop_Bunting12", -171.185f, 4.300f, -64.375f, -120.00f, 0.938f, 8),
            new F("Deco_Shop_Balloons", -177.000f, 0.000f, -73.000f, 16.70f, 1.000f, 8),
            new F("Prop_Cart", -167.500f, 0.000f, -63.000f, -90.00f, 1.000f, 8),
            new F("Prop_Bench", -174.000f, 0.000f, -65.000f, 0.00f, 1.000f, 8),
            new F("Prop_Bin", -172.200f, 0.000f, -65.000f, 0.00f, 1.000f, 8),
            new F("Tree_Dead_A", -181.000f, 0.000f, -61.000f, 40.00f, 1.000f, 8),
            new F("Prop_Litter", -170.732f, 0.000f, -65.014f, 220.74f, 1.000f, 8),
            new F("Prop_Litter", -173.889f, 0.000f, -62.348f, 300.86f, 1.000f, 8),
            new F("Prop_Bunting12", -92.500f, 4.300f, -95.670f, -120.00f, 0.833f, 9),
            new F("Prop_Bunting12", -100.000f, 4.300f, -91.340f, -180.00f, 0.833f, 9),
            new F("Prop_Bunting12", -107.500f, 4.300f, -95.670f, 120.00f, 0.833f, 9),
            new F("Prop_Bunting12", -107.500f, 4.300f, -104.330f, 60.00f, 0.833f, 9),
            new F("Prop_Bunting12", -92.500f, 4.300f, -104.330f, -60.00f, 0.833f, 9),
            new F("Prop_Bunting18", -100.000f, 4.500f, -100.000f, -180.00f, 1.111f, 9),
            new F("Prop_Bunting18", -100.000f, 4.500f, -100.000f, 60.00f, 1.111f, 9),
            new F("Deco_Shop_Headbands", -88.857f, 0.000f, -94.164f, -120.00f, 1.000f, 9),
            new F("Deco_Food_Churros", -100.000f, 0.000f, -85.500f, -180.00f, 1.000f, 9),
            new F("Deco_Food_Popcorn", -112.557f, 0.000f, -92.750f, 120.00f, 1.000f, 9),
            new F("Deco_Food_IceCream", -112.557f, 0.000f, -107.250f, 60.00f, 1.000f, 9),
            new F("Deco_Shop_Souvenir", -100.000f, 0.000f, -114.500f, 0.00f, 1.000f, 9),
            new F("Prop_Dining_Cloth", -103.600f, 0.000f, -101.800f, 170.74f, 1.000f, 9),
            new F("Prop_Dining_Plain", -103.600f, 0.000f, -98.200f, 83.67f, 1.000f, 9),
            new F("Prop_Dining_ClothTopple", -100.000f, 0.000f, -101.800f, 258.85f, 1.000f, 9),
            new F("Prop_Dining_Cloth", -100.000f, 0.000f, -98.200f, 264.32f, 1.000f, 9),
            new F("Prop_Dining_Cloth", -96.400f, 0.000f, -101.800f, 272.36f, 1.000f, 9),
            new F("Prop_Dining_ClothTopple", -96.400f, 0.000f, -98.200f, 187.17f, 1.000f, 9),
            new F("Tree_Leafy_A", -112.000f, 0.000f, -96.000f, 10.00f, 1.000f, 9),
            new F("Tree_Dead_A", -88.000f, 0.000f, -108.000f, 60.00f, 1.000f, 9),
            new F("Prop_Bin", -94.500f, 0.000f, -94.500f, 0.00f, 1.000f, 9),
            new F("Prop_Bin_Tipped", -105.000f, 0.000f, -106.000f, 120.00f, 1.000f, 9),
            new F("Prop_Sign", -95.000f, 0.000f, -87.000f, 0.00f, 1.000f, 9),
            new F("Prop_WetFloor", -98.500f, 0.000f, -103.200f, 30.00f, 1.000f, 9),
            new F("Prop_Litter", -97.888f, 0.000f, -98.398f, 49.52f, 1.000f, 9),
            new F("Prop_Litter", -105.956f, 0.000f, -98.472f, 199.86f, 1.000f, 9),
            new F("Prop_Litter", -99.494f, 0.000f, -100.419f, 305.75f, 1.000f, 9),
            new F("Prop_Litter", -109.869f, 0.000f, -95.945f, 221.30f, 1.000f, 9),
            new F("Prop_Litter", -104.248f, 0.000f, -99.814f, 101.14f, 1.000f, 9),
            new F("Prop_Litter", -106.234f, 0.000f, -91.433f, 17.89f, 1.000f, 9),
            new F("Prop_Bunting12", -44.000f, 4.300f, -73.000f, 59.04f, 0.972f, 10),
            new F("Prop_Bunting12", -44.000f, 4.300f, -83.000f, 120.96f, 0.972f, 10),
            new F("Deco_Food_Churros", -48.500f, 0.000f, -74.000f, 90.00f, 1.000f, 10),
            new F("Deco_Shop_Headbands", -48.500f, 0.000f, -83.000f, 90.00f, 1.000f, 10),
            new F("Prop_Cart_Balloons", -40.000f, 0.000f, -70.000f, -90.00f, 1.000f, 10),
            new F("Prop_Bench", -39.500f, 0.000f, -82.000f, -90.00f, 1.000f, 10),
            new F("Prop_Bin", -39.500f, 0.000f, -84.000f, 0.00f, 1.000f, 10),
            new F("Prop_Stanchions", -43.000f, 0.000f, -91.050f, -0.00f, 1.000f, 10),
            new F("Prop_Stanchions", -43.000f, 0.000f, -90.000f, -0.00f, 1.000f, 10),
            new F("Prop_Stanchions", -43.000f, 0.000f, -88.950f, -0.00f, 1.000f, 10),
            new F("Prop_Litter", -47.085f, 0.000f, -75.439f, 238.07f, 1.000f, 10),
            new F("Prop_Litter", -44.579f, 0.000f, -77.215f, 143.89f, 1.000f, 10),
            new F("Prop_Bunting12", 49.000f, 4.300f, -59.990f, -180.00f, 1.002f, 11),
            new F("Prop_Bunting12", 42.990f, 4.300f, -66.000f, 90.00f, 1.002f, 11),
            new F("Prop_Bunting12", 49.000f, 4.300f, -72.010f, -0.00f, 1.002f, 11),
            new F("Prop_Bunting12", 55.010f, 4.300f, -66.000f, -90.00f, 1.002f, 11),
            new F("Prop_Bunting18", 49.000f, 4.450f, -66.000f, 135.00f, 0.944f, 11),
            new F("Prop_Bunting18", 49.000f, 4.450f, -66.000f, 45.00f, 0.944f, 11),
            new F("Deco_Food_IceCream", 38.000f, 0.000f, -64.000f, 90.00f, 1.000f, 11),
            new F("Deco_Food_Popcorn", 38.000f, 0.000f, -71.000f, 90.00f, 1.000f, 11),
            new F("Deco_Shop_Souvenir", 50.000f, 0.000f, -77.500f, 0.00f, 1.000f, 11),
            new F("Prop_Dining_Cloth", 48.300f, 0.000f, -66.700f, 95.65f, 1.000f, 11),
            new F("Prop_Dining_Plain", 48.300f, 0.000f, -63.300f, 79.69f, 1.000f, 11),
            new F("Prop_Dining_ClothTopple", 51.700f, 0.000f, -66.700f, 96.46f, 1.000f, 11),
            new F("Prop_Dining_Cloth", 51.700f, 0.000f, -63.300f, 257.58f, 1.000f, 11),
            new F("Tree_Dead_A", 51.000f, 0.000f, -55.500f, 272.40f, 1.000f, 11),
            new F("Prop_Bench", 48.300f, 0.000f, -55.500f, -90.00f, 1.000f, 11),
            new F("Prop_Bench", 53.700f, 0.000f, -55.500f, 90.00f, 1.000f, 11),
            new F("Prop_Bin", 51.000f, 0.000f, -52.700f, 0.00f, 1.000f, 11),
            new F("Prop_Cart", 58.793f, 0.000f, -68.293f, -90.00f, 1.000f, 11),
            new F("Prop_Litter", 48.523f, 0.000f, -65.806f, 242.66f, 1.000f, 11),
            new F("Prop_Litter", 54.731f, 0.000f, -67.434f, 302.90f, 1.000f, 11),
            new F("Prop_Litter", 51.013f, 0.000f, -66.816f, 175.06f, 1.000f, 11),
        };

        void OnGUI()
        {
            if (on == null || on.Length != Zones.Length) { on = new bool[Zones.Length]; for (int i = 0; i < on.Length; i++) on[i] = true; }
            EditorGUILayout.LabelField("구역 (끄면 안 놓음)", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(260));
            for (int i = 0; i < Zones.Length; i++)
            {
                int n = Items.Count(it => it.zone == i);
                on[i] = EditorGUILayout.ToggleLeft(Zones[i].label + "  (" + n + "개)", on[i]);
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("놀이공원 꾸미기", GUILayout.Height(30))) Build(on);
            if (GUILayout.Button("꾸미기 지우기")) Clear();
            EditorGUILayout.HelpBox("LotteWorld_Fest 루트만 만들고 지웁니다. 저장은 직접 하세요(Ctrl+S).", MessageType.Info);
        }

        static GameObject FindRoot(string name)
        {
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects()) if (g.name == name) return g;
            return null;
        }
        static void Clear()
        {
            GameObject g = FindRoot(RootName);
            if (g != null) Undo.DestroyObjectImmediate(g);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static void Build(bool[] zoneOn)
        {
            Clear();
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
            var holders = new Transform[Zones.Length];
            var cache = new Dictionary<string, GameObject>();
            var missing = new HashSet<string>();
            var counts = new int[Zones.Length];
            int lights = 0;
            foreach (F it in Items)
            {
                if (zoneOn != null && !zoneOn[it.zone]) continue;
                if (!cache.TryGetValue(it.n, out GameObject pf))
                {
                    string dir = it.n.StartsWith("Deco_") ? DecoDir : it.n.StartsWith("Tree_") ? FoliageDir : PropDir;   // 나무는 Foliage 프리팹 (예전 저폴리 화분 나무 대신)
                    pf = AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/" + it.n + ".prefab");
                    cache[it.n] = pf;
                }
                if (pf == null) { missing.Add(it.n); continue; }
                if (holders[it.zone] == null)
                {
                    var h = new GameObject(Zones[it.zone].obj); h.transform.SetParent(root.transform, false); holders[it.zone] = h.transform;
                }
                var g = (GameObject)PrefabUtility.InstantiatePrefab(pf, holders[it.zone]);
                g.transform.position = new Vector3(it.x, it.y, it.z);
                g.transform.rotation = Quaternion.Euler(0f, it.yaw, 0f) * pf.transform.localRotation;   // FBX 나무는 뿌리에 회전이 들어 있음
                if (Mathf.Abs(it.sx - 1f) > 0.001f) g.transform.localScale = new Vector3(it.sx, 1f, 1f);
                lights += g.GetComponentsInChildren<Light>(true).Length;
                counts[it.zone]++;
            }
            var lines = new List<string>();
            for (int i = 0; i < Zones.Length; i++) if (counts[i] > 0) lines.Add("  " + Zones[i].label + ": " + counts[i]);
            Debug.Log(string.Format("[Fest] 소품 {0}개, 점광원 {1}개 -> {2}\n{3}", counts.Sum(), lights, RootName, string.Join("\n", lines)));
            if (missing.Count > 0)
                Debug.LogWarning("[Fest] 프리팹을 못 찾음: " + string.Join(", ", missing) + "\n  Props 창 [1. 프리팹 만들기] / Deco 창 [낱개 프리팹 만들기]를 먼저 누르세요.");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
#endif
