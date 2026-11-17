#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Rides 폴더 안의 모든 놀이기구 FBX에 URP Lit 재질을 만들고 구운 텍스처를 연결한다.
// 폴더 구조: Assets/Maps/LotteWorld/Rides/<놀이기구>/<이름>.fbx + Textures/
// 메뉴: Tools/LotteWorld/Setup Ride Materials (새 놀이기구를 넣을 때마다 다시 실행)
public static class RideMaterialSetup
{
    const string RidesRoot = "Assets/Maps/LotteWorld/Rides";

    [MenuItem("Tools/LotteWorld/Setup Ride Materials")]
    public static void SetupAll()
    {
        if (!AssetDatabase.IsValidFolder(RidesRoot)) { Debug.LogError($"폴더가 없어: {RidesRoot}"); return; }
        int count = 0;
        foreach (var dir in AssetDatabase.GetSubFolders(RidesRoot))
            foreach (var fbx in Directory.GetFiles(dir, "*.fbx"))
            { SetupOne(dir, fbx.Replace('\\', '/')); count++; }
        AssetDatabase.SaveAssets();
        Debug.Log($"[Rides] 놀이기구 {count}개 재질 설정 완료");
    }

    static void SetupOne(string dir, string fbx)
    {
        string tex = dir + "/Textures", matDir = dir + "/Materials";
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(dir, "Materials");
        if (Directory.Exists(tex))
            foreach (var p in Directory.GetFiles(tex, "*.png"))
            {
                string path = p.Replace('\\', '/');
                var ti = (TextureImporter)AssetImporter.GetAtPath(path); bool dirty = false;
                if (path.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                if (dirty) ti.SaveAndReimport();
            }

        var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
        mi.globalScale = 1f; mi.useFileScale = true;
        mi.bakeAxisConversion = true;   // 축 변환을 메시에 굽기 → 그룹을 Y/X축으로 돌리면 그대로 회전
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var names = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Material>().Select(m => m.name).Distinct().ToList();
        foreach (var n in names)
        {
            string mp = $"{matDir}/{n}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, mp); }
            mat.shader = shader; mat.SetColor("_BaseColor", Color.white);
            var bc = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_BaseColor.png");
            var nm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_Normal.png");
            var ms = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_MS.png");
            if (bc) mat.SetTexture("_BaseMap", bc);
            if (nm) { mat.SetTexture("_BumpMap", nm); mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); }
            if (ms) { mat.SetTexture("_MetallicGlossMap", ms); mat.SetFloat("_Smoothness", 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
            if (n.EndsWith("On"))   // BulbOn, LanternOn 등 발광 재질
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

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        var rs = inst.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Debug.Log($"[Rides] {Path.GetFileName(fbx)}: 재질 {names.Count}개, 크기 {b.size.x:F1} x {b.size.y:F1} x {b.size.z:F1} m");
        }
        Object.DestroyImmediate(inst);
    }
}
#endif
