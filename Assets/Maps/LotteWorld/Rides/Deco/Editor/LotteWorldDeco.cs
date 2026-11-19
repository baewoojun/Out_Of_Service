// LotteWorldDeco.cs  (v3 - 11/19) 롯데월드 실내 빈 곳 꾸미기: 길·잔디·화단·나무 + 가로등·벤치
// 위치: Assets/Maps/LotteWorld/Rides/Deco/Editor/ . 메뉴: Tools > LotteWorld > Deco (길·잔디·화단·나무)
//
//  [꾸미기 만들기]
//   1) Tools/LotteWorld/Setup Ride Materials 실행 (Rides/Deco/LotteWorld_Deco.fbx 재질·타일 텍스처 연결)
//   2) 씬 루트 LotteWorld_Deco 를 새로 만들고 FBX를 원점에 놓음 (FBX 안 좌표 = 월드 좌표 그대로)
//        - Deco_Paths (길·연석) / Deco_Lawn_* / Deco_Bed_## / Deco_RoundBed / Deco_Tree_### 는 따로따로라 옮기거나 지울 수 있음
//   3) 나무 밑동에 Capsule Collider, 화단에 Box Collider
//   4) 가로등·벤치: Rides/Props/Prefabs 의 Prop_Lamp_On / Prop_Lamp_Half / Prop_Bench 를 길 옆에 놓음 (LotteWorld_Deco/Props)
//   5) 위치 검사: 나무·화단·잔디 몇 개가 예상 월드 좌표에 왔는지 콘솔에 찍음 (어긋나면 경고)
//  [낱개 프리팹 만들기]  Rides/Deco/LotteWorld_DecoProps.fbx 안 9종 -> Rides/Deco/Prefabs/Deco_*.prefab
//        나무 3 (Deco_Tree_Leafy/Autumn/Dead), 먹거리 3 (Deco_Food_Churros/Popcorn/IceCream), 기념품 3 (Deco_Shop_Souvenir/Balloons/Headbands)
//        원점 = 바닥 가운데, 앞 = +Z. 나무는 밑동 Capsule Collider, 가게는 Mesh Collider. 가게 전구 자리에 Point Light (팝콘·머리띠는 깜빡임)
//  [바위 프리팹 만들기] LotteWorld_Rocks.fbx -> Prefabs/Deco_Rock_{S,M,L}_{A,B,C}.prefab (Mesh Collider, 풀은 충돌 없음)
//        끌어다 놓고 Y 회전만 돌려 쓰면 됨
//  [건물 프리팹 만들기]  Rides/Bld*/LotteWorld_Bld*.fbx 4종 -> Rides/Deco/Prefabs/Bld_*.prefab
//        반목조 식당+시계탑 / 지중해 카페+종탑 / 둥근 탑 기념품점 / 3칸 타운하우스. 1층 빈 방 + 문(Door_L·Door_R 또는 Door_Main, 피벗 = 경첩)
//        정문이 프리팹 +Z 를 보게 돌림. 몸체는 Mesh Collider, 문짝은 Box Collider
//  다시 누르면 LotteWorld_Deco 만 지우고 새로 만듦. 셸·소품·놀이기구는 안 건드림. 씬 저장은 직접(Ctrl+S). Undo 됨.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Deco
{
    public class DecoWindow : EditorWindow
    {
        const string FbxPath = "Assets/Maps/LotteWorld/Rides/Deco/LotteWorld_Deco.fbx";
        const string PropPrefabDir = "Assets/Maps/LotteWorld/Rides/Props/Prefabs";
        const string RootName = "LotteWorld_Deco";
        const string PropsFbx = "Assets/Maps/LotteWorld/Rides/Deco/LotteWorld_DecoProps.fbx";
        const string RocksFbx = "Assets/Maps/LotteWorld/Rides/Deco/LotteWorld_Rocks.fbx";
        const string PrefabDir = "Assets/Maps/LotteWorld/Rides/Deco/Prefabs";
        const string MatDir = "Assets/Maps/LotteWorld/Rides/Deco/Materials";
        float lightIntensity = 2.0f, lightRange = 7f;
        bool addColliders = true, addLamps = true;

        [MenuItem("Tools/LotteWorld/Deco (길·잔디·화단·나무)")]
        public static void Open() { GetWindow<DecoWindow>("LW Deco"); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("FBX: " + FbxPath, EditorStyles.miniLabel);
            addColliders = EditorGUILayout.Toggle("나무·화단 충돌체", addColliders);
            addLamps = EditorGUILayout.Toggle("가로등·벤치 놓기", addLamps);
            if (GUILayout.Button("꾸미기 만들기", GUILayout.Height(30))) Build(addColliders, addLamps);
            if (GUILayout.Button("꾸미기 지우기")) Clear();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("낱개 소품 (나무 3 · 먹거리 3 · 기념품 3)", EditorStyles.boldLabel);
            lightIntensity = EditorGUILayout.FloatField("가게 조명 세기", lightIntensity);
            lightRange = EditorGUILayout.FloatField("가게 조명 범위 (m)", lightRange);
            if (GUILayout.Button("낱개 프리팹 만들기", GUILayout.Height(28))) BuildSinglePrefabs(PropsFbx, lightIntensity, lightRange);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("바위 9종 (S/M/L x 둥근·층·이끼)", EditorStyles.boldLabel);
            if (GUILayout.Button("바위 프리팹 만들기", GUILayout.Height(28))) BuildSinglePrefabs(RocksFbx, lightIntensity, lightRange);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("단독 건물 6종 (빈 방 + 문, 성 옆 상가 · 성+양쪽 상가 포함)", EditorStyles.boldLabel);
            if (GUILayout.Button("건물 프리팹 만들기", GUILayout.Height(28))) BuildBuildingPrefabs();
            EditorGUILayout.HelpBox("LotteWorld_Deco 루트만 만들고 지웁니다. 저장은 직접 하세요(Ctrl+S).", MessageType.Info);
        }

        struct P { public string n; public float x, z, yaw; public P(string n, float x, float z, float yaw) { this.n = n; this.x = x; this.z = z; this.yaw = yaw; } }
        struct C { public string n; public float x, z; public C(string n, float x, float z) { this.n = n; this.x = x; this.z = z; } }

        // 가로등·벤치 (월드 x, z, yaw). 앞(+Z)이 길을 봄
        static readonly P[] Props =
        {
            new P("Prop_Lamp_Half", -50.955f, 34.444f, -125.05f),
            new P("Prop_Bench", -52.35f, 37.13f, -125.05f),
            new P("Prop_Lamp_On", -74.654f, 35.212f, 8.75f),
            new P("Prop_Bench", -77.68f, 35.272f, 8.75f),
            new P("Prop_Lamp_On", -96.0f, 45.3f, -180.0f),
            new P("Prop_Bench", -99.0f, 45.7f, -180.0f),
            new P("Prop_Lamp_Half", -118.0f, 36.7f, 0.0f),
            new P("Prop_Bench", -121.0f, 36.3f, 0.0f),
            new P("Prop_Lamp_On", -141.769f, 43.923f, 153.43f),
            new P("Prop_Bench", -144.631f, 42.939f, 153.43f),
            new P("Prop_Lamp_Half", -147.2f, -24.0f, -90.0f),
            new P("Prop_Bench", -146.8f, -21.0f, -90.0f),
            new P("Prop_Lamp_On", -147.2f, -2.0f, -90.0f),
            new P("Prop_Bench", -146.8f, 1.0f, -90.0f),
            new P("Prop_Lamp_On", -147.2f, 20.0f, -90.0f),
            new P("Prop_Bench", -146.8f, 23.0f, -90.0f),
            new P("Prop_Lamp_Half", -147.2f, 42.0f, -90.0f),
            new P("Prop_Bench", -146.8f, 45.0f, -90.0f),
            new P("Prop_Lamp_Half", -63.142f, -18.01f, 156.52f),
            new P("Prop_Bench", -66.053f, -18.838f, 156.52f),
            new P("Prop_Lamp_On", -83.306f, -29.8f, 0.0f),
            new P("Prop_Bench", -86.306f, -30.2f, 0.0f),
            new P("Prop_Lamp_Half", -112.215f, -38.294f, 129.56f),
            new P("Prop_Bench", -114.434f, -40.352f, 129.56f),
            new P("Prop_Lamp_On", -130.0f, -47.8f, 0.0f),
            new P("Prop_Bench", -133.0f, -48.2f, 0.0f),
            new P("Prop_Lamp_On", -147.321f, -32.673f, -144.46f),
            new P("Prop_Bench", -149.529f, -30.603f, -144.46f),
            new P("Prop_Lamp_Half", 61.996f, -24.04f, 12.72f),
            new P("Prop_Bench", 64.835f, -25.091f, 12.72f),

        };
        // 위치 검사용: 오브젝트 이름 -> 예상 월드 (x, z)
        static readonly C[] Checks =
        {
            new C("Deco_Tree_000_dead", -120.0f, 55.0f),
            new C("Deco_Bed_00_flower", -145.4f, -16.0f),
            new C("Deco_Lawn_W_Garden", -132.75f, 13.5f),
            new C("Deco_Lawn_E_North", 130.0f, 20.75f),
        };

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

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        static System.Type FlickerType()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type t = asm.GetType("LotteWorld.Props.LightFlicker");
                if (t != null) return t;
            }
            return null;
        }

        static readonly string[,] Buildings =
        {
            { "BldTimber", "LotteWorld_BldTimber", "Bld_TimberRestaurant" },
            { "BldMedCafe", "LotteWorld_BldMedCafe", "Bld_MedCafe" },
            { "BldRoundTower", "LotteWorld_BldRoundTower", "Bld_RoundTowerShop" },
            { "BldTownhouses", "LotteWorld_BldTownhouses", "Bld_Townhouses" },
            { "BldShopHall", "LotteWorld_BldShopHall", "Bld_ShopHall" },
            { "NWCastleHalls", "LotteWorld_NWCastleHalls", "Bld_NWCastleHalls" },
        };

        public static void BuildBuildingPrefabs()
        {
            EditorApplication.ExecuteMenuItem("Tools/LotteWorld/Setup Ride Materials");
            EnsureFolder(PrefabDir);
            int made = 0; var lines = new List<string>();
            for (int i = 0; i < Buildings.GetLength(0); i++)
            {
                string fbx = "Assets/Maps/LotteWorld/Rides/" + Buildings[i, 0] + "/" + Buildings[i, 1] + ".fbx";
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                if (asset == null) { lines.Add("  없음: " + fbx); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var holder = new GameObject(Buildings[i, 2]);
                inst.transform.SetParent(holder.transform, false);
                inst.transform.localPosition = Vector3.zero;
                // 정문(문 경첩들의 가운데)이 +Z 쪽을 보게 Y 회전
                var doors = inst.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Door_")).ToList();
                if (doors.Count > 0)
                {
                    Vector3 c = Vector3.zero; foreach (var d in doors) c += d.position; c /= doors.Count;
                    Vector3 f = holder.transform.InverseTransformPoint(c); f.y = 0f;
                    if (f.sqrMagnitude < 0.01f) f = new Vector3(0f, 0f, -1f);
                    float alpha = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                    inst.transform.localRotation = Quaternion.Euler(0f, -alpha, 0f) * inst.transform.localRotation;
                }
                int mc = 0;
                foreach (MeshFilter mf in holder.GetComponentsInChildren<MeshFilter>(true))
                {
                    bool isDoor = mf.transform.parent != null && mf.transform.parent.name.StartsWith("Door_");
                    if (!isDoor) GameObjectUtility.SetStaticEditorFlags(mf.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic);
                    if (mf.sharedMesh == null) continue;
                    if (isDoor) mf.gameObject.AddComponent<BoxCollider>();
                    else { var col = mf.gameObject.AddComponent<MeshCollider>(); col.sharedMesh = mf.sharedMesh; col.convex = false; }
                    mc++;
                }
                PrefabUtility.SaveAsPrefabAsset(holder, PrefabDir + "/" + Buildings[i, 2] + ".prefab");
                Bounds b = new Bounds(); bool first = true;
                foreach (Renderer r in holder.GetComponentsInChildren<Renderer>(true)) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                lines.Add(string.Format("  {0}: 문 {1}개 ({2}), 충돌체 {3}, 크기 {4:F1} x {5:F1} x {6:F1} m", Buildings[i, 2], doors.Count, string.Join(",", doors.Select(d => d.name)), mc, b.size.x, b.size.y, b.size.z));
                Object.DestroyImmediate(holder); made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Deco] 건물 프리팹 " + made + "개 -> " + PrefabDir + "\n" + string.Join("\n", lines));
        }

        public static void BuildSinglePrefabs(string fbxPath, float intensity, float range)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (asset == null) { Debug.LogError("[Deco] FBX가 없어: " + fbxPath); return; }
            EditorApplication.ExecuteMenuItem("Tools/LotteWorld/Setup Ride Materials");
            asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var glass = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/LW_Prop_Glass.mat");
            if (glass != null)
            {
                glass.SetColor("_BaseColor", new Color(0.75f, 0.8f, 0.78f, 0.3f));
                glass.SetFloat("_Surface", 1f); glass.SetFloat("_Blend", 0f);
                glass.SetFloat("_SrcBlend", 5f); glass.SetFloat("_DstBlend", 10f); glass.SetFloat("_ZWrite", 0f);
                glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glass.renderQueue = 3000; glass.SetOverrideTag("RenderType", "Transparent");
                EditorUtility.SetDirty(glass);
            }
            EnsureFolder(PrefabDir);
            var names = asset.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Deco_") && !t.name.Contains("_LightPt") && !t.name.EndsWith("_Fwd") && !t.name.Contains("__")).Select(t => t.name).Distinct().ToList();
            System.Type flicker = FlickerType();
            int made = 0, lights = 0; var sizes = new List<string>();
            foreach (string n in names)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Transform src = inst.GetComponentsInChildren<Transform>(true).First(t => t.name == n);
                Quaternion r0 = src.rotation; Vector3 s0 = src.lossyScale;
                var holder = new GameObject(n);
                src.SetParent(holder.transform, false);
                src.localPosition = Vector3.zero; src.localRotation = r0; src.localScale = s0;
                Transform fwd = holder.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("_Fwd"));
                if (fwd != null)                          // 앞(블렌더 -Y, *_Fwd 표시)이 프리팹 +Z 를 보게
                {
                    Vector3 f = holder.transform.InverseTransformPoint(fwd.position);
                    float alpha = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                    src.localRotation = Quaternion.Euler(0f, -alpha, 0f) * r0;
                    Object.DestroyImmediate(fwd.gameObject);
                }
                bool isTree = n.Contains("_Tree_");
                foreach (MeshFilter mf in holder.GetComponentsInChildren<MeshFilter>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(mf.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic);
                    if (mf.name.Contains("Rock_Grass") || mf.name.Contains("Rock_DryGrass")) continue;      // 바위 옆 풀은 충돌 없음
                    if (!isTree && mf.sharedMesh != null) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; mc.convex = false; }
                }
                if (isTree)
                {
                    var trunk = new GameObject("Trunk_Collider"); trunk.transform.SetParent(holder.transform, false);
                    trunk.transform.localPosition = new Vector3(0f, 1.75f, 0f);
                    var cc = trunk.AddComponent<CapsuleCollider>(); cc.radius = 0.45f; cc.height = 3.5f; cc.direction = 1;
                }
                foreach (Transform lp in holder.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("_LightPt")).ToList())
                {
                    var L = lp.gameObject.AddComponent<Light>();
                    L.type = LightType.Point; L.color = new Color(1f, 0.78f, 0.5f); L.intensity = intensity; L.range = range; L.shadows = LightShadows.None;
                    if (flicker != null && (n.Contains("Popcorn") || n.Contains("Headbands"))) lp.gameObject.AddComponent(flicker);
                    lights++;
                }
                PrefabUtility.SaveAsPrefabAsset(holder, PrefabDir + "/" + n + ".prefab");
                Bounds b = new Bounds(); bool first = true;
                foreach (Renderer r in holder.GetComponentsInChildren<Renderer>(true)) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                sizes.Add(string.Format("  {0}: {1:F1} x {2:F1} x {3:F1} m, 바닥 y {4:F2}", n, b.size.x, b.size.y, b.size.z, b.min.y));
                Object.DestroyImmediate(holder); Object.DestroyImmediate(inst); made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Deco] 낱개 프리팹 " + made + "개, 점광원 " + lights + "개 -> " + PrefabDir + "\n" + string.Join("\n", sizes));
        }

        public static void Build(bool colliders, bool lamps)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (asset == null) { Debug.LogError("[Deco] FBX가 없어: " + FbxPath); return; }
            EditorApplication.ExecuteMenuItem("Tools/LotteWorld/Setup Ride Materials");
            asset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            Clear();
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            inst.transform.localPosition = Vector3.zero;          // 회전은 FBX 기본값 그대로 (모서리 건물과 같은 방식)

            int nTree = 0, nBed = 0, nMesh = 0;
            foreach (Transform t in inst.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<MeshFilter>() != null)
                {
                    nMesh++;
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic);
                }
                if (!colliders) continue;
                if (t.name.StartsWith("Deco_Tree_") || t.name == "Deco_RoundBed_Tree")
                {
                    var trunk = new GameObject("Trunk_Collider");                  // 밑동 기둥 (지름 0.9 m, 높이 3.5 m)
                    trunk.transform.SetParent(t, true);
                    trunk.transform.position = new Vector3(t.position.x, 1.75f, t.position.z);
                    trunk.transform.rotation = Quaternion.identity;
                    var cc = trunk.AddComponent<CapsuleCollider>(); cc.radius = 0.45f; cc.height = 3.5f; cc.direction = 1;
                    nTree++;
                }
                else if (t.name.StartsWith("Deco_Bed_") || t.name == "Deco_RoundBed")
                {
                    var r = t.GetComponent<Renderer>();
                    if (r == null) continue;
                    Bounds b = r.bounds;
                    var box = new GameObject("Bed_Collider");
                    box.transform.SetParent(t, true);
                    box.transform.position = new Vector3(b.center.x, 0.5f, b.center.z);
                    box.transform.rotation = Quaternion.identity;
                    var bc = box.AddComponent<BoxCollider>(); bc.size = new Vector3(b.size.x, 1.0f, b.size.z);
                    nBed++;
                }
            }

            int nProp = 0, missing = 0;
            if (lamps)
            {
                var holder = new GameObject("Props"); holder.transform.SetParent(root.transform, false);
                foreach (P p in Props)
                {
                    var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PropPrefabDir + "/" + p.n + ".prefab");
                    if (pf == null) { missing++; continue; }
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(pf, holder.transform);
                    g.transform.position = new Vector3(p.x, 0f, p.z);
                    g.transform.rotation = Quaternion.Euler(0f, p.yaw, 0f);
                    nProp++;
                }
                if (missing > 0) Debug.LogWarning("[Deco] 소품 프리팹 " + missing + "개를 못 찾음 (" + PropPrefabDir + "). Props 창의 [1. 프리팹 만들기]를 먼저 누르세요.");
            }

            // 위치 검사
            int ok = 0; var lines = new List<string>();
            foreach (C c in Checks)
            {
                Transform t = inst.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == c.n);
                if (t == null) { lines.Add("  " + c.n + " 없음"); continue; }
                float dx = t.position.x - c.x, dz = t.position.z - c.z;
                bool good = dx * dx + dz * dz < 0.25f;
                if (good) ok++;
                lines.Add(string.Format("  {0}: 실제 ({1:F2}, {2:F2}, {3:F2}) / 예상 ({4:F2}, 0, {5:F2}) {6}", c.n, t.position.x, t.position.y, t.position.z, c.x, c.z, good ? "OK" : "어긋남"));
            }
            Bounds all = new Bounds(); bool first = true;
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true)) { if (first) { all = r.bounds; first = false; } else all.Encapsulate(r.bounds); }
            Debug.Log(string.Format("[Deco] 메시 {0}개, 나무 충돌체 {1}, 화단 충돌체 {2}, 가로등·벤치 {3}개. 전체 바운즈 center ({4:F1}, {5:F1}, {6:F1}) size ({7:F1}, {8:F1}, {9:F1})\n위치 검사 {10}/{11}\n{12}",
                nMesh, nTree, nBed, nProp, all.center.x, all.center.y, all.center.z, all.size.x, all.size.y, all.size.z, ok, Checks.Length, string.Join("\n", lines)));
            if (ok < Checks.Length) Debug.LogWarning("[Deco] 위치가 예상과 달라. FBX 회전·축 설정을 확인해야 해 (LotteWorld_Deco 아래 FBX 인스턴스의 Rotation 값을 알려줘).");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
#endif
