// LotteWorldRink.cs (v1 - 11/19) 아이스링크 2층 구조 (시안 v5 확정): 1층(-6 m) 링크 65 x 36 m + 둘레 통로 + 발코니 아래 가게 줄,
// 2층 발코니 끝(붉은 띠·유리 난간·망루), 링크 왼쪽 끝 섬 + 성 장식, 뒤쪽 1층 -> 섬 -> 앞쪽 1층 붉은 아치 다리 하나
// 넣는 곳: Assets/Maps/LotteWorld/Rides/Rink/Editor/  (FBX·텍스처: Assets/Maps/LotteWorld/Rides/Rink/)
// 메뉴: Tools > LotteWorld > Rink
//   1) 재질·임포트 설정 : URP Lit 재질 + 구운 텍스처, 발광(가게 창·조명 줄), 유리 반투명, 얼음 광택, Mesh Collider 자동 생성
//   2) 씬에 놓기 : 씬 루트 'LotteWorld_Rink' (없으면 만듦), 표식 Marker_E (44.85, 0, 0) / Marker_N (0, 0, 29.9) 로 회전·위치 자동 맞춤,
//      자식 Static. 그리고 겹치는 옛것을 끔(지우지 않음, Undo 가능): 셸 'LotteWorld_Shell/Rink', 블록아웃 'Blockout/B1/IceRink'
// 부품(64): Rink_Ice, Rink_Boards, Rink_LowerFloor, Rink_ShopFronts, Rink_Columns, Rink_Ceiling, Rink_CeilingLights, Rink_BalconyEdge,
//   Rink_Railing, Rink_Turret_00..11, Rink_Lamp_xx, Rink_Cafe_xx, Rink_Escalator_0/1, Rink_Island, Rink_IslandCastle,
//   Rink_Bridge_Back, Rink_Bridge_Front — 하나씩 옮기거나 끌 수 있음 (피벗 = 부품 바닥 가운데)
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LotteWorldRink
{
    const string Dir = "Assets/Maps/LotteWorld/Rides/Rink";
    const string Fbx = Dir + "/LotteWorld_Rink.fbx";
    const string RootName = "LotteWorld_Rink";
    static readonly Vector3 ME = new Vector3(44.85f, 0f, 0f), MN = new Vector3(0f, 0f, 29.9f);
    static readonly Vector3 BMin = new Vector3(-57.4f, -7.5f, -42.4f), BMax = new Vector3(57.4f, 17.7f, 42.4f);

    [MenuItem("Tools/LotteWorld/Rink/1 재질·임포트 설정")]
    public static void Setup()
    {
        if (AssetImporter.GetAtPath(Fbx) == null) { Debug.LogError("[Rink] FBX 없음: " + Fbx); return; }
        string tex = Dir + "/Textures", matDir = Dir + "/Materials";
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(Dir, "Materials");
        if (Directory.Exists(tex))
            foreach (string p in Directory.GetFiles(tex, "*.png"))
            {
                string path = p.Replace('\\', '/'); var ti = (TextureImporter)AssetImporter.GetAtPath(path); bool dirty = false;
                if (path.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
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
            if (n == "Rink_Shop" || n == "Rink_LightW" || n == "Rink_LightR")            // lit shop windows / light lines
            {
                Color c = n == "Rink_Shop" ? new Color(1f, .85f, .64f) : n == "Rink_LightW" ? new Color(1f, .95f, .88f) : new Color(1f, .35f, .32f);
                mat.SetColor("_BaseColor", c); mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", c * (n == "Rink_LightW" ? 3f : 2f));
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            if (n == "Rink_Glass")                                                       // see-through railing
            {
                mat.SetColor("_BaseColor", new Color(.8f, .9f, .92f, .25f)); mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = (int)RenderQueue.Transparent; mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetFloat("_Cull", 0f); mat.SetFloat("_Smoothness", .9f);
            }
            if (n == "Rink_Ice") { mat.SetFloat("_Smoothness", .85f); mat.DisableKeyword("_METALLICSPECGLOSSMAP"); mat.SetTexture("_MetallicGlossMap", null); }
            EditorUtility.SetDirty(mat);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
        }
        mi.SaveAndReimport(); AssetDatabase.SaveAssets();
        Debug.Log("[Rink] 재질 " + names.Count + "개 연결, Mesh Collider 자동 생성 켬");
    }

    [MenuItem("Tools/LotteWorld/Rink/2 씬에 놓기·옛 링크 끄기")]
    public static void Place()
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        if (src == null) { Debug.LogError("[Rink] FBX 없음: " + Fbx); return; }
        Scene scn = SceneManager.GetActiveScene();
        GameObject root = scn.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (root == null) { root = (GameObject)PrefabUtility.InstantiatePrefab(src); root.name = RootName; Undo.RegisterCreatedObjectUndo(root, "Place rink"); }
        Transform a = Find(root.transform, "Marker_E"), b = Find(root.transform, "Marker_N");
        if (a == null || b == null) { Debug.LogError("[Rink] 표식 Marker_E / Marker_N 을 못 찾음"); return; }
        Undo.RecordObject(root.transform, "Align rink");
        root.transform.localScale = Vector3.one;
        Vector3 cur = b.position - a.position, want = MN - ME; cur.y = 0; want.y = 0;
        root.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(cur, want, Vector3.up), 0f) * root.transform.rotation;
        root.transform.position += ME - a.position;
        float err = Mathf.Max((a.position - ME).magnitude, (b.position - MN).magnitude);
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t != root.transform && !t.name.StartsWith("Marker_"))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI |
                                                                     StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.NavigationStatic);
        var rs = root.GetComponentsInChildren<Renderer>(true); Bounds bb = rs[0].bounds; foreach (Renderer r in rs) bb.Encapsulate(r.bounds);
        float bErr = Mathf.Max((bb.min - BMin).magnitude, (bb.max - BMax).magnitude);
        // switch off what the new rink replaces (kept in the scene, undoable)
        string off = "";
        foreach (string path in new[] { "LotteWorld_Shell/Rink", "Blockout/B1/IceRink" })
        {
            GameObject g = FindPath(scn, path);
            if (g != null && g.activeSelf) { Undo.RecordObject(g, "Hide " + path); g.SetActive(false); off += " " + path; }
        }
        EditorSceneManager.MarkSceneDirty(scn); Selection.activeGameObject = root;
        string msg = string.Format("[Rink] 루트 회전 Y {0:F1}°, 위치 {1}, 표식 오차 {2:F2} m, 범위 {3} ~ {4} (예상과 차 {5:F1} m). 끈 것:{6}",
                                   root.transform.eulerAngles.y, root.transform.position, err, bb.min, bb.max, bErr, off == "" ? " 없음" : off);
        if (err > 0.5f || bErr > 2f) Debug.LogWarning(msg + " — 어긋남: 이 로그를 보내 주세요");
        else Debug.Log(msg + " — 맞음. 셸 창에서 Rink 를 다시 [만들기] 하지 마세요. 저장은 Ctrl+S");
    }

    static Transform Find(Transform t, string n)
    {
        foreach (Transform c in t.GetComponentsInChildren<Transform>(true)) if (c.name == n) return c;
        return null;
    }
    static GameObject FindPath(Scene s, string path)
    {
        string[] seg = path.Split('/');
        GameObject r = s.GetRootGameObjects().FirstOrDefault(g => g.name == seg[0]);
        if (r == null) return null;
        Transform t = r.transform;
        for (int i = 1; i < seg.Length && t != null; i++) t = t.Find(seg[i]);
        return t != null ? t.gameObject : null;
    }
}
#endif
