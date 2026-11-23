// LotteWorldFoliage.cs - 바뀐 나무 잎(잎 카드 아틀라스)용 재질 설정: 알파 잘라내기 + 양면
// 메뉴: Tools > LotteWorld > Look > 잎 재질 알파 설정 (나무 FBX·잎 텍스처를 넣은 뒤 한 번, Setup Ride Materials 를 다시 돌렸으면 또 한 번)
// 잎 텍스처 = 왼쪽 1/4 불투명(속 덩어리) + 오른쪽 3/4 잎 카드(알파). 재질 에셋만 바꿈, Undo 됨
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LotteWorld.Look
{
    public static class LotteWorldFoliage
    {
        static readonly string[] Names =
        {
            "LW_Deco_Leaf1", "LW_Deco_Leaf2", "LW_Deco_Aut1", "LW_Deco_Aut2",
            "LW_Prop_DT_Leaf1", "LW_Prop_DT_Leaf2", "LW_Prop_DT_Aut1", "LW_Prop_DT_Aut2",
        };
        const string Root = "Assets/Maps/LotteWorld";

        [MenuItem("Tools/LotteWorld/Look/잎 재질 알파 설정")]
        public static void Apply()
        {
            int mats = 0, texs = 0; var missing = new List<string>();
            foreach (var n in Names)
            {
                // 텍스처: 알파 = 투명도, 밉맵에서 잎이 사라지지 않게 커버리지 유지
                foreach (var g in AssetDatabase.FindAssets(n + "_BaseColor t:Texture2D", new[] { Root }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                    if (ti == null) continue;
                    ti.alphaSource = TextureImporterAlphaSource.FromInput;
                    ti.alphaIsTransparency = true;
                    ti.mipMapsPreserveCoverage = true;
                    ti.alphaTestReferenceValue = 0.5f;
                    ti.SaveAndReimport(); texs++;
                }
                bool found = false;
                foreach (var g in AssetDatabase.FindAssets(n + " t:Material", new[] { Root }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (m == null || m.name != n) continue;
                    found = true;
                    Undo.RecordObject(m, "잎 재질 알파");
                    m.SetFloat("_Surface", 0f);
                    m.SetFloat("_AlphaClip", 1f);
                    m.SetFloat("_Cutoff", 0.5f);
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cull", 0f);                     // 양면
                    m.doubleSidedGI = true;
                    m.SetOverrideTag("RenderType", "TransparentCutout");
                    m.renderQueue = 2450;                         // AlphaTest
                    EditorUtility.SetDirty(m); mats++;
                }
                if (!found) missing.Add(n);
            }
            AssetDatabase.SaveAssets();
            Debug.Log(string.Format("[LW Foliage] 재질 {0}개, 텍스처 {1}개 설정{2}", mats, texs,
                missing.Count > 0 ? " / 재질 못 찾음: " + string.Join(", ", missing) + " (Tools/LotteWorld/Setup Ride Materials 를 먼저 실행)" : ""));
        }
    }
}
#endif
