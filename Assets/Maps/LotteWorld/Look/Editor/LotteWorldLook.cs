// LotteWorldLook.cs - 롯데월드 맵 재질에 "디테일 노멀 + 얼룩"을 한 번에 붙이고 떼는 도구
// 메뉴: Tools > LotteWorld > Look (디테일 노멀)
// - URP Lit 재질에만 적용. 전구·유리·잎·물처럼 디테일이 어색한 재질은 이름으로 건너뜀
// - 붙인 재질에는 에셋 라벨 "LW_Detail"을 달아 두고, [떼기]는 그 라벨이 있는 재질만 원래대로 돌림
// - 씬은 건드리지 않음(재질 에셋만 바뀜). Undo 됨. 저장·커밋은 사용자가
// - 맵 작업(매직아일랜드 포함)이 다 끝난 뒤 마지막에 실행하는 도구
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LotteWorld.Look
{
    public class LotteWorldLookWindow : EditorWindow
    {
        const string Label = "LW_Detail";
        const string TexDir = "Assets/Maps/LotteWorld/Look/Textures/";
        const string NormalPath = TexDir + "LW_Detail_Normal.png";
        const string AlbedoPath = TexDir + "LW_Detail_Albedo.png";

        static readonly string[] DefaultFolders =
        {
            "Assets/Maps/LotteWorld/Rides",
            "Assets/Maps/LotteWorld/Shell/Materials",
            "Assets/Maps/LotteWorld/MagicIsland",
        };
        static readonly string[] SkipWords =
        {
            "Bulb", "Lit", "Glow", "LampOn", "LightOn", "CeilLight", "Glass", "Window", "WinDark", "Opening", "Recess",
            "Leaf", "Grass", "Hedge", "Flower", "Fl1", "Fl2", "DryFl", "Aut", "Water", "Pond", "Neon", "Led", "Emiss", "Sky", "Fog",
            "YellowLine", "Mirror", "Eye",
        };

        bool[] useFolder = { true, true, true };
        float normalStrength = 0.4f;
        float tiling = 12f;
        bool useAlbedo = true;
        Vector2 scroll;
        string lastLog = "";

        [MenuItem("Tools/LotteWorld/Look (디테일 노멀)")]
        static void Open() { GetWindow<LotteWorldLookWindow>("LW Look"); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("재질에 디테일 노멀(잔 요철) + 얼룩(x2)을 붙입니다.", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("맵 수정이 다 끝난 뒤 마지막에 실행하세요. 재질 에셋만 바뀌고 씬은 그대로입니다. [떼기]로 원래대로 돌릴 수 있습니다.", MessageType.Info);
            for (int i = 0; i < DefaultFolders.Length; i++)
                useFolder[i] = EditorGUILayout.ToggleLeft(DefaultFolders[i], useFolder[i]);
            normalStrength = EditorGUILayout.Slider("노멀 세기", normalStrength, 0f, 2f);
            tiling = EditorGUILayout.Slider("타일 반복", tiling, 1f, 32f);
            useAlbedo = EditorGUILayout.Toggle("얼룩(디테일 알베도)", useAlbedo);
            EditorGUILayout.Space();
            if (GUILayout.Button("미리 세기 (바꾸지 않음)")) Report(dryRun: true);
            if (GUILayout.Button("붙이기")) Apply();
            if (GUILayout.Button("떼기 (원래대로)")) Remove();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox(lastLog, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        string[] Folders()
        {
            var list = new List<string>();
            for (int i = 0; i < DefaultFolders.Length; i++)
                if (useFolder[i] && AssetDatabase.IsValidFolder(DefaultFolders[i])) list.Add(DefaultFolders[i]);
            return list.ToArray();
        }

        static bool IsLit(Material m)
        {
            return m != null && m.shader != null && m.shader.name == "Universal Render Pipeline/Lit";
        }

        static bool Skip(Material m)
        {
            foreach (var w in SkipWords)
                if (m.name.IndexOf(w, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        List<Material> Targets(out int skipped, out int notLit)
        {
            skipped = 0; notLit = 0;
            var res = new List<Material>();
            var folders = Folders();
            if (folders.Length == 0) return res;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", folders))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!IsLit(m)) { notLit++; continue; }
                if (Skip(m)) { skipped++; continue; }
                res.Add(m);
            }
            return res;
        }

        void Report(bool dryRun)
        {
            int skipped, notLit;
            var t = Targets(out skipped, out notLit);
            lastLog = string.Format("대상 {0}개, 이름으로 건너뜀 {1}개, URP Lit 아님 {2}개", t.Count, skipped, notLit);
            Debug.Log("[LW Look] " + lastLog);
        }

        static void FixImporters()
        {
            var ni = AssetImporter.GetAtPath(NormalPath) as TextureImporter;
            if (ni != null && ni.textureType != TextureImporterType.NormalMap)
            {
                ni.textureType = TextureImporterType.NormalMap; ni.wrapMode = TextureWrapMode.Repeat; ni.SaveAndReimport();
            }
            var ai = AssetImporter.GetAtPath(AlbedoPath) as TextureImporter;
            if (ai != null && ai.wrapMode != TextureWrapMode.Repeat)
            {
                ai.wrapMode = TextureWrapMode.Repeat; ai.SaveAndReimport();
            }
        }

        void Apply()
        {
            FixImporters();
            var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            var alb = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            if (nrm == null) { lastLog = "텍스처가 없습니다: " + NormalPath; Debug.LogError("[LW Look] " + lastLog); return; }
            int skipped, notLit;
            var t = Targets(out skipped, out notLit);
            Undo.RecordObjects(t.ToArray(), "LW Look 디테일 붙이기");
            int n = 0;
            foreach (var m in t)
            {
                m.SetTexture("_DetailNormalMap", nrm);
                m.SetFloat("_DetailNormalMapScale", normalStrength);
                if (useAlbedo && alb != null) { m.SetTexture("_DetailAlbedoMap", alb); m.SetFloat("_DetailAlbedoMapScale", 1f); }
                else m.SetTexture("_DetailAlbedoMap", null);
                m.SetTextureScale("_DetailAlbedoMap", new Vector2(tiling, tiling));
                m.SetTexture("_DetailMask", null);
                m.EnableKeyword("_DETAIL_MULX2");
                var labels = AssetDatabase.GetLabels(m).ToList();
                if (!labels.Contains(Label)) { labels.Add(Label); AssetDatabase.SetLabels(m, labels.ToArray()); }
                EditorUtility.SetDirty(m);
                n++;
            }
            AssetDatabase.SaveAssets();
            lastLog = string.Format("붙임 {0}개 (이름으로 건너뜀 {1}, URP Lit 아님 {2}) / 노멀 {3:0.00}, 반복 {4}, 얼룩 {5}", n, skipped, notLit, normalStrength, tiling, useAlbedo);
            Debug.Log("[LW Look] " + lastLog);
        }

        void Remove()
        {
            var mats = new List<Material>();
            foreach (var guid in AssetDatabase.FindAssets("l:" + Label + " t:Material"))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m != null) mats.Add(m);
            }
            Undo.RecordObjects(mats.ToArray(), "LW Look 디테일 떼기");
            foreach (var m in mats)
            {
                m.SetTexture("_DetailNormalMap", null);
                m.SetTexture("_DetailAlbedoMap", null);
                m.SetTextureScale("_DetailAlbedoMap", Vector2.one);
                m.SetFloat("_DetailNormalMapScale", 1f);
                m.DisableKeyword("_DETAIL_MULX2");
                AssetDatabase.SetLabels(m, AssetDatabase.GetLabels(m).Where(l => l != Label).ToArray());
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
            lastLog = "뗌 " + mats.Count + "개";
            Debug.Log("[LW Look] " + lastLog);
        }
    }
}
#endif
