// LotteWorldFacade.cs (v1 - 11/19) 롯데월드 어드벤처 외벽 (시안 D: 흰 벽 + 자주 세로판 + 민트 지붕 테두리)
// 넣는 곳: Assets/Maps/LotteWorld/Rides/Facade/Editor/  (FBX·텍스처: Assets/Maps/LotteWorld/Rides/Facade/)
// 메뉴: Tools > LotteWorld > Facade
//   1) 재질·임포트 설정 : URP Lit 재질 만들고 구운 텍스처 연결, bakeAxisConversion, Generate Colliders(Mesh Collider)
//   2) 씬에 놓기·위치 맞추기 : 씬 루트 'LotteWorld_Facade' (없으면 만듦). 표식 Marker_V00 / Marker_V08 이 벽 꼭짓점
//      (-128.92, 0, 91.92) / (151.78, 0, 23.03) 에 오도록 루트의 Y 회전·위치를 자동으로 맞춤. 자식은 Static
// 부품: Facade_Wall_00..19 (벽면마다 하나, 피벗 = 벽 가운데 바닥), Facade_GatePortal (매직아일랜드 정문), Facade_Sign (빈 간판),
//       Facade_Roof (민트 평지붕, 유리 돔 자리는 비어 있음). 하나씩 옮기거나 끌 수 있음
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LotteWorldFacade
{
    const string Dir = "Assets/Maps/LotteWorld/Rides/Facade";
    const string Fbx = Dir + "/LotteWorld_Facade.fbx";
    const string RootName = "LotteWorld_Facade";
    static readonly Vector3 V00 = new Vector3(-128.92f, 0f, 91.92f), V08 = new Vector3(151.78f, 0f, 23.03f);

    [MenuItem("Tools/LotteWorld/Facade/1 재질·임포트 설정")]
    public static void Setup()
    {
        if (AssetImporter.GetAtPath(Fbx) == null) { Debug.LogError("[Facade] FBX 없음: " + Fbx); return; }
        string tex = Dir + "/Textures", matDir = Dir + "/Materials";
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(Dir, "Materials");
        if (Directory.Exists(tex))
            foreach (string p in Directory.GetFiles(tex, "*.png"))
            {
                string path = p.Replace('\\', '/'); var ti = (TextureImporter)AssetImporter.GetAtPath(path); bool dirty = false;
                if (path.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                if (ti.maxTextureSize < 2048) { ti.maxTextureSize = 2048; dirty = true; }
                if (dirty) ti.SaveAndReimport();
            }
        var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        mi.globalScale = 1f; mi.useFileScale = true; mi.bakeAxisConversion = true; mi.addCollider = true;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        var names = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Material>().Select(m => m.name).Distinct().ToList();
        foreach (string n in names)
        {
            string mp = matDir + "/" + n + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (mat == null) { mat = new Material(sh); AssetDatabase.CreateAsset(mat, mp); }
            mat.shader = sh; mat.SetColor("_BaseColor", Color.white);
            var bc = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_BaseColor.png");
            var nm = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_Normal.png");
            var ms = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_MS.png");
            if (bc) mat.SetTexture("_BaseMap", bc);
            if (nm) { mat.SetTexture("_BumpMap", nm); mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); }
            if (ms) { mat.SetTexture("_MetallicGlossMap", ms); mat.SetFloat("_Smoothness", 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
            EditorUtility.SetDirty(mat);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
        }
        mi.SaveAndReimport(); AssetDatabase.SaveAssets();
        Debug.Log("[Facade] 재질 " + names.Count + "개 연결, Mesh Collider 자동 생성 켬");
    }

    [MenuItem("Tools/LotteWorld/Facade/2 씬에 놓기·위치 맞추기")]
    public static void Place()
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        if (src == null) { Debug.LogError("[Facade] FBX 없음: " + Fbx); return; }
        GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (root == null)
        {
            root = (GameObject)PrefabUtility.InstantiatePrefab(src);
            root.name = RootName; Undo.RegisterCreatedObjectUndo(root, "Place facade");
        }
        Transform a = Find(root.transform, "Marker_V00"), b = Find(root.transform, "Marker_V08");
        if (a == null || b == null) { Debug.LogError("[Facade] 표식 Marker_V00 / Marker_V08 을 못 찾음"); return; }
        Undo.RecordObject(root.transform, "Align facade");
        root.transform.localScale = Vector3.one;
        // yaw: rotate so that (b - a) points like (V08 - V00) in XZ, then translate a onto V00
        Vector3 cur = b.position - a.position, want = V08 - V00; cur.y = 0; want.y = 0;
        float yaw = Vector3.SignedAngle(cur, want, Vector3.up);
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * root.transform.rotation;
        root.transform.position += V00 - a.position;
        float err = Mathf.Max((a.position - V00).magnitude, (b.position - V08).magnitude);
        float lenErr = Mathf.Abs(cur.magnitude - want.magnitude);
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t != root.transform && !t.name.StartsWith("Marker_"))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI |
                                                                     StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.NavigationStatic);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        // mirror check: the markers can line up even if the model came in mirrored, the overall bounds cannot
        var rs = root.GetComponentsInChildren<Renderer>(true); Bounds bb = rs[0].bounds; foreach (Renderer r in rs) bb.Encapsulate(r.bounds);
        float bErr = Mathf.Max(Mathf.Abs(bb.min.x - (-189.8f)), Mathf.Abs(bb.max.x - 159.9f), Mathf.Abs(bb.min.z - (-126.5f)), Mathf.Abs(bb.max.z - 144.9f));
        string msg = string.Format("[Facade] 루트 회전 Y {0:F1}°, 위치 {1}, 표식 오차 {2:F2} m (길이 차 {3:F2} m), 범위 {4} ~ {5} (예상과 차 {6:F1} m)",
                                   root.transform.eulerAngles.y, root.transform.position, err, lenErr, bb.min, bb.max, bErr);
        if (err > 0.5f || lenErr > 0.5f || bErr > 2f) Debug.LogWarning(msg + " — 어긋남: 좌우가 뒤집혔을 수 있으니 이 로그를 보내 주세요");
        else Debug.Log(msg + " — 맞음. 저장은 Ctrl+S");
    }

    static Transform Find(Transform t, string n)
    {
        foreach (Transform c in t.GetComponentsInChildren<Transform>(true)) if (c.name == n) return c;
        return null;
    }
}
#endif
