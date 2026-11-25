#if UNITY_EDITOR
// BldParkSetup.cs - 4묶음 건물 고퀄(The Park 단계, 11/25 클라우드에서 구움) 재질 연결
// 대상: Assets/Maps/LotteWorld/Rides/<건물 폴더>/ 의 FBX 14개 (이름·빈 오브젝트·문 피벗·메시 이름은 예전 것과 같음)
// 메뉴 Tools > LotteWorld > Look > 건물 고퀄 재질 연결
//   - <폴더>/Textures/<재질>_BaseColor / _Normal / _MS  (구운 재질, 이름 끝 _P)
//   - Rides/BldShared/Textures/Tile_<이름>_*  (넓은 벽·지붕에 반복되는 공용 실사 질감, 재질도 BldShared/Materials 에 하나씩)
//   - 이름이 On / Lit 로 끝나거나 Bulb·Neon 이 들어간 재질은 발광 (이미 있는 재질이면 그대로 둠)
//   - 끝나면 콘솔에 건물마다 재질 수, 텍스처 붙은 수, 삼각형 수
// 씬 저장·커밋은 직접. 새 장식 메시(<구역>_Detail_*)에는 콜라이더를 안 붙임(얇은 장식이라 걸리적거림).
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LotteWorldLook
{
    public static class BldParkSetup
    {
        const string Root = "Assets/Maps/LotteWorld/Rides";
        static readonly string[] Blds = {
            "NorthGate/LotteWorld_NorthGate", "Castle/Castle_A12", "GardenStage/GardenStage", "A15/LotteWorld_A15", "A11/LotteWorld_A11",
            "SouthTerrace/LotteWorld_SouthTerrace", "NWCastleHalls/LotteWorld_NWCastleHalls", "BldShopHall/LotteWorld_BldShopHall",
            "Facade/LotteWorld_Facade", "Rink/LotteWorld_Rink", "BldTimber/LotteWorld_BldTimber", "BldMedCafe/LotteWorld_BldMedCafe",
            "BldRoundTower/LotteWorld_BldRoundTower", "BldTownhouses/LotteWorld_BldTownhouses" };

        static void FixTextures(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var p in Directory.GetFiles(dir, "*.png"))
            {
                string path = p.Replace('\\', '/');
                var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) continue;
                bool dirty = false;
                if (path.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
                if (dirty) ti.SaveAndReimport();
            }
        }
        static void EnsureFolder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }

        [MenuItem("Tools/LotteWorld/Look/건물 고퀄 재질 연결")]
        public static void Setup()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            string shared = Root + "/BldShared";
            EnsureFolder(Root, "BldShared"); EnsureFolder(shared, "Materials");
            AssetDatabase.Refresh();
            FixTextures(shared + "/Textures");
            int okB = 0;
            foreach (var b in Blds)
            {
                string dir = Root + "/" + Path.GetDirectoryName(b).Replace('\\', '/'), fbx = Root + "/" + b + ".fbx";
                if (!File.Exists(fbx)) { Debug.LogWarning("[BldPark] FBX 없음: " + fbx); continue; }
                string tex = dir + "/Textures"; EnsureFolder(dir, "Materials"); string matDir = dir + "/Materials";
                FixTextures(tex);
                var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
                mi.globalScale = 1f; mi.useFileScale = true; mi.bakeAxisConversion = true;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                mi.SaveAndReimport();
                var names = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Material>().Select(m => m.name).Distinct().ToList();
                int withTex = 0;
                foreach (var n in names)
                {
                    bool tile = n.StartsWith("Tile_");
                    bool emit = n.EndsWith("On") || n.EndsWith("Lit") || n.Contains("Bulb") || n.Contains("Neon") || n.Contains("_LightW") || n.Contains("_LightR");
                    string td = tile ? shared + "/Textures" : tex;
                    string mp = (tile ? shared + "/Materials" : matDir) + "/" + n + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                    if (emit && mat != null) { mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat); continue; }
                    if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, mp); }
                    mat.shader = shader; mat.SetColor("_BaseColor", Color.white);
                    var bc = AssetDatabase.LoadAssetAtPath<Texture2D>($"{td}/{n}_BaseColor.png");
                    var nm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{td}/{n}_Normal.png");
                    var ms = AssetDatabase.LoadAssetAtPath<Texture2D>($"{td}/{n}_MS.png");
                    mat.SetTexture("_BaseMap", bc);
                    if (nm) { mat.SetTexture("_BumpMap", nm); mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); }
                    else { mat.SetTexture("_BumpMap", null); mat.DisableKeyword("_NORMALMAP"); }
                    if (ms) { mat.SetTexture("_MetallicGlossMap", ms); mat.SetFloat("_Smoothness", n.Contains("Gold") || n.Contains("Brass") ? 0.5f : 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                    if (bc) withTex++;
                    if (emit)
                    {
                        mat.SetColor("_BaseColor", new Color(1f, .86f, .58f));
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", new Color(1f, .8f, .45f) * 3f);
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                    }
                    EditorUtility.SetDirty(mat);
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
                }
                mi.SaveAndReimport();
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                long tris = go.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh).Sum(f => { long t = 0; for (int i = 0; i < f.sharedMesh.subMeshCount; i++) t += (long)f.sharedMesh.GetIndexCount(i) / 3; return t; });
                int miss = names.Count(n => !(n.EndsWith("On") || n.EndsWith("Lit") || n.Contains("Bulb") || n.Contains("Neon") || n.Contains("_LightW") || n.Contains("_LightR"))) - withTex;
                Debug.Log($"[BldPark] {Path.GetFileName(fbx)}: 재질 {names.Count}개(텍스처 {withTex}{(miss > 0 ? $", 텍스처 없음 {miss}" : "")}), 삼각형 {tris:N0}");
                okB++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[BldPark] 끝 ({okB}/{Blds.Length}). 씬에서 정문·성 문 열리는지, 벽 콜라이더 그대로인지 한 번 확인해 줘");
        }
    }
}
#endif
