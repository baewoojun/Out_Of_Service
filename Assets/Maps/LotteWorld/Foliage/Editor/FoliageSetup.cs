// FoliageSetup.cs - 새 나무·식물 에셋(Foliage 폴더) 재질 연결 + LOD 프리팹 만들기
// 메뉴: Tools > LotteWorld > Look > 새 나무 에셋 설정   (Foliage 폴더를 Assets/Maps/LotteWorld/ 아래에 넣은 뒤 한 번)
// 씬은 건드리지 않음. 만드는 것: Foliage/Materials/NT_*.mat, Foliage/Prefabs/*.prefab
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LotteWorld.Look
{
    public static class FoliageSetup
    {
        const string Root = "Assets/Maps/LotteWorld/Foliage";
        // 잎 카드(알파 잘라내기 + 양면) 재질
        static readonly HashSet<string> Cards = new HashSet<string> {
            "NT_Leaf1", "NT_Leaf2", "NT_Aut1", "NT_Aut2", "NT_Dry", "NT_Blossom", "NT_Needle", "NT_Frond", "NT_FrondDry", "NT_Violet", "NT_Lilac", "NT_Flowers" };

        [MenuItem("Tools/LotteWorld/Look/새 나무 에셋 설정")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Root)) { Debug.LogError("[Foliage] 폴더 없음: " + Root); return; }
            if (!AssetDatabase.IsValidFolder(Root + "/Materials")) AssetDatabase.CreateFolder(Root, "Materials");
            if (!AssetDatabase.IsValidFolder(Root + "/Prefabs")) AssetDatabase.CreateFolder(Root, "Prefabs");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) { Debug.LogError("[Foliage] URP Lit 셰이더를 못 찾음"); return; }

            // 1) 텍스처 설정 + 재질
            var mats = new Dictionary<string, Material>();
            foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Textures" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g); string n = Path.GetFileNameWithoutExtension(p);
                var ti = AssetImporter.GetAtPath(p) as TextureImporter; if (ti == null) continue;
                bool changed = false;
                if (n.EndsWith("_Normal") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; changed = true; }
                if (n.EndsWith("_BaseColor"))
                {
                    string m = n.Substring(0, n.Length - "_BaseColor".Length);
                    if (Cards.Contains(m) && !ti.mipMapsPreserveCoverage)
                    { ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = true; ti.mipMapsPreserveCoverage = true; ti.alphaTestReferenceValue = 0.5f; ti.wrapMode = TextureWrapMode.Clamp; changed = true; }
                }
                if (changed) ti.SaveAndReimport();
            }
            foreach (string g in AssetDatabase.FindAssets("_BaseColor t:Texture2D", new[] { Root + "/Textures" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g); string n = Path.GetFileNameWithoutExtension(p);
                string name = n.Substring(0, n.Length - "_BaseColor".Length);
                string mp = Root + "/Materials/" + name + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, mp); }
                m.shader = lit;
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(p)); m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", 0.1f); m.SetFloat("_Metallic", 0f);
                var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + name + "_Normal.png");
                if (nrm != null) { m.SetTexture("_BumpMap", nrm); m.EnableKeyword("_NORMALMAP"); }
                if (Cards.Contains(name))
                {
                    m.SetFloat("_Surface", 0f); m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cull", 0f); m.doubleSidedGI = true; m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = 2450;
                }
                m.enableInstancing = true;
                EditorUtility.SetDirty(m); mats[name] = m;
            }
            AssetDatabase.SaveAssets();

            // 2) FBX 임포트 설정 + 재질 연결
            var fbxs = new List<string>();
            foreach (string g in AssetDatabase.FindAssets("t:Model", new[] { Root }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g); if (!p.ToLower().EndsWith(".fbx")) continue;
                var mi = AssetImporter.GetAtPath(p) as ModelImporter; if (mi == null) continue;
                mi.bakeAxisConversion = true; mi.importCameras = false; mi.importLights = false; mi.importAnimation = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                foreach (var kv in mats) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                mi.SaveAndReimport(); fbxs.Add(p);
            }

            // 3) 프리팹 (LODGroup)
            int made = 0;
            foreach (string p in fbxs)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(p); if (model == null) continue;
                string name = Path.GetFileNameWithoutExtension(p);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model); go.name = name;
                Transform l0 = go.transform.Find(name + "_LOD0"), l1 = go.transform.Find(name + "_LOD1");
                if (l0 != null && l1 != null)
                {
                    var lg = go.GetComponent<LODGroup>(); if (lg == null) lg = go.AddComponent<LODGroup>();   // _LOD0/_LOD1 이름이면 Unity 가 임포트 때 이미 만들어 둠
                    lg.fadeMode = LODFadeMode.CrossFade; lg.animateCrossFading = true;
                    lg.SetLODs(new[] {
                        new LOD(0.22f, l0.GetComponentsInChildren<Renderer>()),
                        new LOD(0.012f, l1.GetComponentsInChildren<Renderer>()) });
                    lg.RecalculateBounds();
                }
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + name + ".prefab"); Object.DestroyImmediate(go); made++;
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log(string.Format("[Foliage] 재질 {0}개, FBX {1}개, 프리팹 {2}개 만듦 → {3}/Prefabs", mats.Count, fbxs.Count, made, Root));
        }
    }
}
#endif
