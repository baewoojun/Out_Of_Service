#if UNITY_EDITOR
// PropsHQSetup.cs - 1묶음 고퀄 소품(LotteWorld_PropsHQ.fbx, LOD0 + LOD1)을 기존 소품 프리팹 안에 넣는다.
// 넣는 곳: Assets/Maps/LotteWorld/Rides/PropsHQ/ (FBX + Textures), 이 파일은 아무 Editor 폴더
// 메뉴 Tools > LotteWorld > Look > 소품 고퀄 ...
//   1. 재질 설정     : PropsHQ FBX만 URP Lit 재질 연결 (옛 소품 재질은 안 건드림)
//   2. 프리팹에 적용 : Rides/Props/Prefabs/Prop_X.prefab 마다 자식 HQ(LODGroup: LOD0 가까이 / LOD1 멀리)를 넣고
//                      옛 렌더러만 끈다. 옛 콜라이더·조명·깜빡임은 그대로. 씬 인스턴스는 프리팹이라 자동 반영
//   3. 옛 모델 보이기·숨기기 : 비교용 (프리팹을 고침)
//   4. 되돌리기      : HQ 지우고 옛 렌더러 다시 켬
//   5. HQ 새로 교체  : 다시 구운 FBX로 HQ를 지우고 새로 넣음 (11/24 The Park 단계 + 데칼)
// 방향: 옛 프리팹 안 FBX 자식(Prop_X)과 같은 회전·배율로 넣음 (두 FBX가 같은 방식으로 내보내져서 맞음)
// LOD 기준(화면 높이 비율): 아래 Lod0Height. 크로스페이드를 쓰려면 URP 에셋에서 LOD Cross Fade 켜기
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LotteWorldLook
{
    public static class PropsHQSetup
    {
        const string Dir = "Assets/Maps/LotteWorld/Rides/PropsHQ";
        const string Fbx = Dir + "/LotteWorld_PropsHQ.fbx";
        const string PrefabDir = "Assets/Maps/LotteWorld/Rides/Props/Prefabs";
        const string Child = "HQ";

        // 화면 높이에서 이 비율 이상 차지하면 LOD0(고퀄). 개수 많은 선반은 조금 빡빡하게, 멀리서도 보이는 건 넉넉하게
        static float Lod0Height(string n)
        {
            if (n.Contains("Shelf")) return .22f;
            if (n.Contains("Lamp") || n.Contains("Bunting")) return .08f;
            return .12f;
        }
        const float CullHeight = .006f;   // 이보다 작으면 안 그림

        [MenuItem("Tools/LotteWorld/Look/소품 고퀄 1. 재질 설정")]
        public static void SetupMaterials()
        {
            if (!File.Exists(Fbx)) { Debug.LogError("[PropsHQ] FBX가 없어: " + Fbx); return; }
            string tex = Dir + "/Textures", matDir = Dir + "/Materials";
            if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(Dir, "Materials");
            if (Directory.Exists(tex))
                foreach (var p in Directory.GetFiles(tex, "*.png"))
                {
                    string path = p.Replace('\\', '/');
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) continue;
                    bool dirty = false;
                    if (path.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                    if (path.EndsWith("_MS.png") && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                    if (dirty) ti.SaveAndReimport();
                }
            var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            mi.globalScale = 1f; mi.useFileScale = true; mi.bakeAxisConversion = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var names = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Material>().Select(m => m.name).Distinct().ToList();
            foreach (var n in names)
            {
                string mp = matDir + "/" + n + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, mp); }
                mat.shader = shader; mat.SetColor("_BaseColor", Color.white);
                var bc = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_BaseColor.png");
                var nm = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_Normal.png");
                var ms = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + "_MS.png");
                if (bc) mat.SetTexture("_BaseMap", bc);
                if (nm) { mat.SetTexture("_BumpMap", nm); mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); }
                if (ms) { mat.SetTexture("_MetallicGlossMap", ms); mat.SetFloat("_Smoothness", 1f); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                if (n.EndsWith("On"))
                {
                    mat.SetColor("_BaseColor", new Color(1f, .86f, .58f));
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1f, .8f, .45f) * (n.Contains("Filament") ? 8f : 4f));
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
                if (n.Contains("Glass")) MakeGlass(mat);
                if (n.StartsWith("Decal_")) MakeDecal(mat, AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/" + n + ".png"));
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
            }
            mi.SaveAndReimport();
            AssetDatabase.SaveAssets();
            Debug.Log("[PropsHQ] 재질 " + names.Count + "개 설정");
        }

        static void MakeGlass(Material mat)
        {
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(.85f, .9f, .88f, .22f));
            mat.SetFloat("_Smoothness", .85f);
            mat.DisableKeyword("_METALLICSPECGLOSSMAP"); mat.SetTexture("_MetallicGlossMap", null);
        }

        static void MakeDecal(Material mat, Texture2D t)
        {
            mat.SetTexture("_BaseMap", t); mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent - 10;
            mat.SetFloat("_Smoothness", .2f); mat.SetFloat("_Metallic", 0f);
            mat.DisableKeyword("_METALLICSPECGLOSSMAP"); mat.SetTexture("_MetallicGlossMap", null);
            mat.DisableKeyword("_NORMALMAP"); mat.SetTexture("_BumpMap", null);
        }

        static List<string> PropNames(GameObject fbx)
        {
            return fbx.transform.Cast<Transform>().Select(t => t.name).Where(n => n.StartsWith("Prop_")).ToList();
        }

        static void SetOldRenderers(GameObject root, bool on)
        {
            var hq = root.transform.Find(Child);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (hq == null || !r.transform.IsChildOf(hq)) r.enabled = on;
        }

        [MenuItem("Tools/LotteWorld/Look/소품 고퀄 2. 프리팹에 적용")]
        public static void Apply() { ApplyImpl(false); }

        // 다시 구운 FBX(The Park 단계, 데칼 추가)로 바꿀 때: 있던 HQ를 지우고 새로 넣는다
        [MenuItem("Tools/LotteWorld/Look/소품 고퀄 5. HQ 새로 교체 (다시 구운 뒤)")]
        public static void Replace() { ApplyImpl(true); }

        static void ApplyImpl(bool replace)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if (fbx == null) { Debug.LogError("[PropsHQ] FBX가 없어: " + Fbx); return; }
            int n = 0, skip = 0, miss = 0;
            var temp = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            PrefabUtility.UnpackPrefabInstance(temp, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            try
            {
                foreach (var name in PropNames(fbx))
                {
                    string pp = PrefabDir + "/" + name + ".prefab";
                    if (!File.Exists(pp)) { Debug.LogWarning("[PropsHQ] 프리팹 없음: " + pp); miss++; continue; }
                    var root = PrefabUtility.LoadPrefabContents(pp);
                    try
                    {
                        var had = root.transform.Find(Child);
                        if (had != null) { if (!replace) { skip++; continue; } Object.DestroyImmediate(had.gameObject); }
                        var src = temp.transform.Find(name);
                        var hq = Object.Instantiate(src.gameObject, root.transform, false);
                        hq.name = Child;
                        var old = root.transform.Cast<Transform>().FirstOrDefault(t => t.name == name);   // 옛 FBX 자식 (회전 = 앞 방향 맞춘 것)
                        if (old != null) { hq.transform.localPosition = old.localPosition; hq.transform.localRotation = old.localRotation; hq.transform.localScale = old.localScale; }
                        else Debug.LogWarning("[PropsHQ] " + name + ": 옛 자식 " + name + " 을 못 찾아 회전 기본값으로 넣음 — 방향 확인", root);
                        foreach (var mf in hq.GetComponentsInChildren<MeshFilter>(true))
                            GameObjectUtility.SetStaticEditorFlags(mf.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                        var l0 = hq.transform.Find(name + "_LOD0"); var l1 = hq.transform.Find(name + "_LOD1");
                        var g = hq.AddComponent<LODGroup>();
                        g.fadeMode = LODFadeMode.CrossFade; g.animateCrossFading = true;
                        g.SetLODs(new[] {
                            new LOD(Lod0Height(name), l0.GetComponentsInChildren<Renderer>(true)),
                            new LOD(CullHeight, l1.GetComponentsInChildren<Renderer>(true)) });
                        g.RecalculateBounds();
                        foreach (var r in hq.GetComponentsInChildren<Renderer>(true))
                            r.shadowCastingMode = r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("Decal_") ? ShadowCastingMode.Off : ShadowCastingMode.On;
                        SetOldRenderers(root, false);
                        PrefabUtility.SaveAsPrefabAsset(root, pp);
                        n++;
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
            }
            finally { Object.DestroyImmediate(temp); }
            AssetDatabase.SaveAssets();
            Debug.Log("[PropsHQ] 프리팹 " + n + "개 적용, " + skip + "개는 이미 있음, " + miss + "개는 프리팹 없음. 옛 콜라이더·조명은 그대로.");
        }

        static bool showOld;
        [MenuItem("Tools/LotteWorld/Look/소품 고퀄 3. 옛 모델 보이기·숨기기")]
        public static void ToggleOld()
        {
            showOld = !showOld; ForEachHQPrefab(root => SetOldRenderers(root, showOld), false);
            Debug.Log("[PropsHQ] 옛 모델 " + (showOld ? "보임" : "숨김"));
        }

        [MenuItem("Tools/LotteWorld/Look/소품 고퀄 4. 되돌리기")]
        public static void Revert()
        {
            int n = ForEachHQPrefab(root => SetOldRenderers(root, true), true);
            Debug.Log("[PropsHQ] 프리팹 " + n + "개 되돌림");
        }

        static int ForEachHQPrefab(System.Action<GameObject> act, bool removeHQ)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx); if (fbx == null) return 0;
            int n = 0;
            foreach (var name in PropNames(fbx))
            {
                string pp = PrefabDir + "/" + name + ".prefab"; if (!File.Exists(pp)) continue;
                var root = PrefabUtility.LoadPrefabContents(pp);
                try
                {
                    var hq = root.transform.Find(Child); if (hq == null) continue;
                    act(root);
                    if (removeHQ) Object.DestroyImmediate(hq.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(root, pp); n++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets(); return n;
        }
    }
}
#endif
