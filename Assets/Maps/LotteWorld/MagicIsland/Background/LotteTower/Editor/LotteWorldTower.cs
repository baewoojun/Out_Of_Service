// LotteWorldTower.cs (v1 - 11/20) 롯데월드타워 배경 (555 m, 낡은 유리, 밤 불 켜진 창 몇 개, 빨간 항공 장애등, 로고 없음)
// 넣는 곳: Assets/Maps/LotteWorld/MagicIsland/Background/LotteTower/Editor/
//          FBX·텍스처: Assets/Maps/LotteWorld/MagicIsland/Background/LotteTower/{LotteTower.fbx, Textures/}
// 메뉴: Tools > LotteWorld > Lotte Tower
//   1) 재질·임포트 설정 : URP Lit 재질을 만들고 텍스처 연결, bakeAxisConversion, 콜라이더 없음(배경 전용)
//   2) 야외맵 씬에 놓기 : Map3_MagicIsland 씬(안 열려 있으면 Additive로 엶)의 루트 'LotteWorld_LotteTower'.
//      표식 Marker_A(타워 중심 바닥) / Marker_B(동쪽 100 m)가 월드 (-370, 0, 20) / (-270, 0, 20)에 오게 Y 회전·위치 자동 맞춤.
//      ※ 매직아일랜드 메뉴 Rebuild Scene/Rebuild All은 씬을 새로 만들기 때문에 타워가 지워짐 → 그 뒤엔 이 메뉴 2를 다시 누르면 됨
//   3) 창문 불빛 켜기·끄기 : 몸통 유리의 불 켜진 창(Emission) 토글
// 부품(하나씩 옮기거나 끌 수 있음): Tower_Body(유리 몸통 0~488 m), Tower_SeamCore(가운데 홈 안쪽), Tower_DeckBands(전망대 띠),
//   Tower_Crown(철골 왕관), Tower_Spire(첨탑), Tower_AviationLights(장애등), Mall_South / Mall_West(+ _Roof, Mall_RoofUnits), Tower_Plaza(바닥판)
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LotteWorldTower
{
    const string Dir = "Assets/Maps/LotteWorld/MagicIsland/Background/LotteTower";
    const string Fbx = Dir + "/LotteTower.fbx";
    const string ScenePath = "Assets/Maps/LotteWorld/Map3_MagicIsland.unity";
    const string RootName = "LotteWorld_LotteTower";
    static readonly Vector3 PA = new Vector3(-370f, 0f, 20f), PB = new Vector3(-270f, 0f, 20f);
    const float EmissionOn = 1.5f;

    // 단색 재질: 색, 금속, 매끈함
    struct Solid { public Color c; public float metal, smooth; public Color emit; public Solid(Color c, float m, float s, Color e) { this.c = c; metal = m; smooth = s; emit = e; } }
    static readonly System.Collections.Generic.Dictionary<string, Solid> Solids = new System.Collections.Generic.Dictionary<string, Solid>
    {
        { "LT_Steel",       new Solid(new Color(0.72f, 0.72f, 0.70f), 0.8f, 0.5f,  new Color(0, 0, 0)) },
        { "LT_Dark",        new Solid(new Color(0.18f, 0.20f, 0.22f), 0.6f, 0.65f, new Color(0, 0, 0)) },
        { "LT_AviationRed", new Solid(new Color(0.30f, 0.02f, 0.02f), 0.0f, 0.4f,  new Color(1f, 0.05f, 0.02f) * 4f) },
        { "LT_Roof",        new Solid(new Color(0.42f, 0.42f, 0.40f), 0.0f, 0.1f,  new Color(0, 0, 0)) },
        { "LT_Plaza",       new Solid(new Color(0.30f, 0.30f, 0.29f), 0.0f, 0.1f,  new Color(0, 0, 0)) },
    };

    [MenuItem("Tools/LotteWorld/Lotte Tower/1 재질·임포트 설정")]
    public static void Setup()
    {
        if (AssetImporter.GetAtPath(Fbx) == null) { Debug.LogError("[LotteTower] FBX 없음: " + Fbx); return; }
        string tex = Dir + "/Textures", matDir = Dir + "/Materials";
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(Dir, "Materials");
        if (Directory.Exists(tex))
            foreach (string p in Directory.GetFiles(tex, "*.png"))
            {
                string path = p.Replace('\\', '/'); var ti = (TextureImporter)AssetImporter.GetAtPath(path); bool dirty = false;
                if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                int want = path.Contains("LT_TowerGlass") ? 4096 : 2048;              // 몸통은 2048x4096 한 장 (반복 없음)
                if (ti.maxTextureSize < want) { ti.maxTextureSize = want; dirty = true; }
                if (path.Contains("LT_TowerGlass") && ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                if (dirty) ti.SaveAndReimport();
            }
        var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        mi.globalScale = 1f; mi.useFileScale = true; mi.bakeAxisConversion = true; mi.addCollider = false;
        mi.importCameras = false; mi.importLights = false; mi.importAnimation = false;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        var names = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Material>().Select(m => m.name).Distinct().ToList();
        foreach (string n in names)
        {
            string mp = matDir + "/" + n + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (mat == null) { mat = new Material(sh); AssetDatabase.CreateAsset(mat, mp); }
            mat.shader = sh;
            Solid s;
            if (Solids.TryGetValue(n, out s))
            {
                mat.SetColor("_BaseColor", s.c); mat.SetFloat("_Metallic", s.metal); mat.SetFloat("_Smoothness", s.smooth);
                SetEmission(mat, s.emit, null);
            }
            else
            {
                mat.SetColor("_BaseColor", Color.white);
                var bc = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_BaseColor.png");
                var ms = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_MS.png");
                var em = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_Emission.png");
                if (bc) mat.SetTexture("_BaseMap", bc);
                if (ms) { mat.SetTexture("_MetallicGlossMap", ms); mat.SetFloat("_Smoothness", 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                SetEmission(mat, em ? Color.white * EmissionOn : new Color(0, 0, 0), em);
            }
            EditorUtility.SetDirty(mat);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
        }
        mi.SaveAndReimport(); AssetDatabase.SaveAssets();
        Debug.Log("[LotteTower] 재질 " + names.Count + "개 연결 (" + string.Join(", ", names) + "), 콜라이더 없음");
    }

    static void SetEmission(Material mat, Color c, Texture tex)
    {
        bool on = c.r + c.g + c.b > 0.001f;
        if (tex != null) mat.SetTexture("_EmissionMap", tex);
        mat.SetColor("_EmissionColor", c);
        if (on) { mat.EnableKeyword("_EMISSION"); mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
        else mat.DisableKeyword("_EMISSION");
    }

    [MenuItem("Tools/LotteWorld/Lotte Tower/2 야외맵 씬에 놓기")]
    public static void Place()
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        if (src == null) { Debug.LogError("[LotteTower] FBX 없음: " + Fbx); return; }
        Scene scene = default(Scene); bool found = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.path == ScenePath && s.isLoaded) { scene = s; found = true; }
        }
        if (!found)
        {
            if (!File.Exists(ScenePath)) { Debug.LogError("[LotteTower] 야외맵 씬 없음: " + ScenePath); return; }
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Debug.Log("[LotteTower] 야외맵 씬을 Additive로 열었음: " + ScenePath);
        }
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (root == null)
        {
            root = (GameObject)PrefabUtility.InstantiatePrefab(src);
            root.name = RootName; SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Place Lotte Tower");
        }
        Transform a = Find(root.transform, "Marker_A"), b = Find(root.transform, "Marker_B");
        if (a == null || b == null) { Debug.LogError("[LotteTower] 표식 Marker_A / Marker_B 를 못 찾음"); return; }
        Undo.RecordObject(root.transform, "Align Lotte Tower");
        root.transform.localScale = Vector3.one;
        // FBX 루트 회전(축 변환)은 그대로 두고 그 앞에 Y 회전만 곱함 -> 표식 A->B 방향이 동쪽(+X)이 되게, A를 PA에
        Vector3 cur = b.position - a.position, want = PB - PA; cur.y = 0; want.y = 0;
        float yaw = Vector3.SignedAngle(cur, want, Vector3.up);
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * root.transform.rotation;
        root.transform.position += PA - a.position;
        float err = Mathf.Max((a.position - PA).magnitude, (b.position - PB).magnitude);
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root.transform || t.name.StartsWith("Marker_")) continue;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        // 뒤집힘 확인: 표식이 맞아도 모델이 좌우로 뒤집혀 들어왔으면 몰 위치(타워 남서쪽)가 어긋남
        var rs = root.GetComponentsInChildren<Renderer>(true); Bounds bb = rs[0].bounds; foreach (Renderer r in rs) bb.Encapsulate(r.bounds);
        Transform mall = Find(root.transform, "Mall_West");
        float bErr = Mathf.Max(Mathf.Abs(bb.min.x - (-460f)), Mathf.Abs(bb.max.x - (-310f)), Mathf.Abs(bb.min.z - (-70f)), Mathf.Abs(bb.max.z - 80f), Mathf.Abs(bb.max.y - 558.8f));
        bool mallOk = mall != null && mall.GetComponent<Renderer>().bounds.center.x < PA.x - 30f;
        string msg = string.Format("[LotteTower] 씬 {0}, 루트 회전 Y {1:F1}°, 위치 {2}, 표식 오차 {3:F2} m, 범위 {4} ~ {5} (예상과 차 {6:F1} m), 몰(서쪽) {7}",
                                   scene.name, root.transform.eulerAngles.y, root.transform.position, err, bb.min, bb.max, bErr, mallOk ? "맞음" : "어긋남");
        if (err > 0.5f || bErr > 2f || !mallOk) Debug.LogWarning(msg + " — 뒤집혔거나 축이 틀렸을 수 있으니 이 로그를 보내 주세요");
        else Debug.Log(msg + " — 맞음. 야외맵 씬 저장은 직접(Ctrl+S). 플레이 카메라 Far Clip이 700 m보다 짧으면 꼭대기가 잘림");
    }

    [MenuItem("Tools/LotteWorld/Lotte Tower/3 창문 불빛 켜기·끄기")]
    public static void ToggleLights()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(Dir + "/Materials/LT_TowerGlass.mat");
        if (mat == null) { Debug.LogError("[LotteTower] 재질 없음 — 메뉴 1을 먼저"); return; }
        bool on = !mat.IsKeywordEnabled("_EMISSION");
        SetEmission(mat, on ? Color.white * EmissionOn : new Color(0, 0, 0), null);
        EditorUtility.SetDirty(mat); AssetDatabase.SaveAssets();
        Debug.Log("[LotteTower] 창문 불빛 " + (on ? "켬" : "끔"));
    }

    static Transform Find(Transform t, string n)
    {
        foreach (Transform c in t.GetComponentsInChildren<Transform>(true)) if (c.name == n) return c;
        return null;
    }
}
#endif
