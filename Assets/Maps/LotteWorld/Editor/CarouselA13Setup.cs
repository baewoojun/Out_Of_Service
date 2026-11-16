#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 회전목마(A13) FBX의 재질을 URP Lit 재질로 만들고 구운 텍스처를 연결한다.
// 메뉴: Tools/LotteWorld/Setup Carousel A13
public static class CarouselA13Setup
{
    const string Root = "Assets/Maps/LotteWorld/Rides/Carousel";
    const string Fbx = Root + "/Carousel_A13.fbx";

    [MenuItem("Tools/LotteWorld/Setup Carousel A13")]
    public static void Setup()
    {
        string tex = Root + "/Textures";
        string matDir = Root + "/Materials";
        if (!File.Exists(Fbx)) { Debug.LogError($"FBX를 찾을 수 없어: {Fbx}"); return; }
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(Root, "Materials");

        // 텍스처 임포트 설정: 노멀맵 / 메탈릭-스무스니스(선형)
        foreach (var p in Directory.GetFiles(tex, "*.png"))
        {
            string path = p.Replace('\\', '/');
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            bool dirty = false;
            if (path.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
            if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
            if (dirty) ti.SaveAndReimport();
        }

        // FBX 스케일·재질 설정
        var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        mi.globalScale = 1f; mi.useFileScale = true;
        mi.bakeAxisConversion = true;   // 블렌더 축 변환을 메시에 굽기 → Rotor를 Y축으로 돌리면 그대로 회전
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var names = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Material>().Select(m => m.name).Distinct().ToList();
        foreach (var n in names)
        {
            string mp = $"{matDir}/{n}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, mp); }
            mat.shader = shader;

            var bc = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_BaseColor.png");
            var nm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_Normal.png");
            var ms = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_MS.png");
            mat.SetColor("_BaseColor", Color.white);
            if (bc) mat.SetTexture("_BaseMap", bc);
            if (nm) { mat.SetTexture("_BumpMap", nm); mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); }
            if (ms) { mat.SetTexture("_MetallicGlossMap", ms); mat.SetFloat("_Smoothness", 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }

            if (n.Contains("BulbOn"))   // 켜진 전구: 발광
            {
                mat.SetColor("_BaseColor", new Color(1f, .86f, .58f));
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, .8f, .45f) * 4f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            EditorUtility.SetDirty(mat);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
        }
        mi.SaveAndReimport();
        AssetDatabase.SaveAssets();

        // 크기·방향 확인 (정상: 가로세로 약 21m, 높이 약 12m)
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var rs = inst.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Debug.Log($"[Carousel A13] 크기 {b.size.x:F1} x {b.size.y:F1} x {b.size.z:F1} m (가로 x 높이 x 세로)");
            if (b.size.y > 15f) Debug.LogWarning("[Carousel A13] 회전목마가 누워 있어. 씬에 놓은 뒤 Rotation X를 -90 또는 90으로 바꿔 줘.");
        }
        Object.DestroyImmediate(inst);
        Debug.Log($"[Carousel A13] 재질 {names.Count}개 설정 완료. {Fbx}를 씬에 끌어다 놓으면 돼.");
    }
}
#endif
