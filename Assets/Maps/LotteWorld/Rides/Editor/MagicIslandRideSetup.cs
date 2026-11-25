#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 매직아일랜드 놀이기구 구운 패키지(MI19, MI33~MI42) 재질 연결.
// 폴더: Assets/Maps/LotteWorld/Rides/MIxx/MIxx.fbx + Textures/<재질>_BaseColor|Normal|MS.png + MIxx_mats.txt
// 메뉴: Tools/LotteWorld/Magic Island/Setup Ride Materials  (RideMaterialSetup 와 같은 방식 + 발광·투명·사진 텍스처·색만 있는 재질)
public static class MagicIslandRideSetup
{
    const string RidesRoot = "Assets/Maps/LotteWorld/Rides";

    [MenuItem("Tools/LotteWorld/Magic Island/Setup Ride Materials")]
    public static void SetupAll()
    {
        int n = 0;
        foreach (var dir in AssetDatabase.GetSubFolders(RidesRoot))
        {
            string name = Path.GetFileName(dir);
            if (!name.StartsWith("MI")) continue;
            string fbx = $"{dir}/{name}.fbx";
            if (!File.Exists(fbx)) continue;
            SetupOne(dir, name, fbx); n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[MagicIsland] 놀이기구 {n}개 재질 설정 완료");
    }

    struct Info { public string kind; public Color baseC; public float metal, rough, alpha; public Color emit; public float estr; }

    static Dictionary<string, Info> ReadTable(string path)
    {
        var d = new Dictionary<string, Info>();
        if (!File.Exists(path)) return d;
        var ci = CultureInfo.InvariantCulture;
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("#") || line.Trim().Length == 0) continue;
            var p = line.Split('|'); if (p.Length < 8) continue;
            float[] V(string s) => s.Split(',').Select(x => float.Parse(x, ci)).ToArray();
            var b = V(p[2]); var e = V(p[6]);
            d[p[0]] = new Info
            {
                kind = p[1], baseC = new Color(b[0], b[1], b[2]), metal = float.Parse(p[3], ci), rough = float.Parse(p[4], ci),
                alpha = float.Parse(p[5], ci), emit = new Color(e[0], e[1], e[2]), estr = float.Parse(p[7], ci)
            };
        }
        return d;
    }

    static void SetupOne(string dir, string name, string fbx)
    {
        string tex = dir + "/Textures", matDir = dir + "/Materials";
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(dir, "Materials");
        if (Directory.Exists(tex))
            foreach (var p in Directory.GetFiles(tex, "*.png"))
            {
                string path = p.Replace('\\', '/');
                var ti = (TextureImporter)AssetImporter.GetAtPath(path); if (ti == null) continue; bool dirty = false;
                if (path.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                if (ti.maxTextureSize < 4096) { ti.maxTextureSize = 4096; dirty = true; }
                if (dirty) ti.SaveAndReimport();
            }
        var table = ReadTable($"{dir}/{name}_mats.txt");

        var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
        mi.globalScale = 1f; mi.useFileScale = true;
        mi.bakeAxisConversion = true;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        mi.isReadable = false;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var names = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Material>().Select(m => m.name).Distinct().ToList();
        foreach (var n in names)
        {
            string mp = $"{matDir}/{n}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, mp); }
            mat.shader = shader;
            table.TryGetValue(n, out var info);
            var bc = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_BaseColor.png");
            var nm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_Normal.png");
            var ms = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}_MS.png");
            mat.SetColor("_BaseColor", bc ? Color.white : (info.kind != null ? info.baseC : Color.gray));
            mat.SetTexture("_BaseMap", bc);
            if (nm) { mat.SetTexture("_BumpMap", nm); mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); }
            if (ms) { mat.SetTexture("_MetallicGlossMap", ms); mat.SetFloat("_Smoothness", 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
            else if (info.kind != null) { mat.SetFloat("_Metallic", Mathf.Min(info.metal, .6f)); mat.SetFloat("_Smoothness", 1f - info.rough); }
            if (info.kind == "emissive")
            {
                mat.EnableKeyword("_EMISSION");
                float k = Mathf.Clamp(info.estr * 0.25f, 1f, 6f);     // 블렌더 세기(밤 기준) → URP HDR 세기, 너무 밝지 않게
                mat.SetColor("_EmissionColor", info.emit * k);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            else { mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.black); }
            if (info.kind != null && info.alpha < 0.99f)   // 유리·물줄기
            {
                mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0);
                mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                var c = mat.GetColor("_BaseColor"); c.a = Mathf.Max(info.alpha, .25f); mat.SetColor("_BaseColor", c);
            }
            EditorUtility.SetDirty(mat);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
        }
        mi.SaveAndReimport();
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        int tris = 0; foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh) tris += mf.sharedMesh.triangles.Length / 3;
        Debug.Log($"[MagicIsland] {name}: 재질 {names.Count}개, 삼각형 {tris:N0}");
    }
}
#endif
