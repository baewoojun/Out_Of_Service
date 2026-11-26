// LotteWorldSurfaceMaps.cs - 바닥·벽·천장처럼 색상 텍스처만 있는 반복 재질에 요철(_Normal)·광택(_MS) 텍스처를 만들어 붙인다
// 메뉴: Tools > LotteWorld > Look > 바닥·벽 요철·광택 만들기 / 바닥·벽 요철·광택 떼기
// - 대상: 실내 껍데기(Shell/Textures/LW_Shell_*.png)와 야외 땅(MagicIsland/Textures/MI_*.png). 아래 Targets 표
// - 색상 텍스처에서 만든다: 큰 얼룩은 빼고(하이패스) 줄눈·잔결만 높이로 삼아 노멀을 만들고,
//   광택은 재질별 기준값에서 줄눈·얼룩 자리를 덜 반짝이게 한다 (_MS: RGB = 금속 0, A = 매끄러움. 놀이기구 _MS 와 같은 규칙)
// - 이미 _Normal.png 가 있는 텍스처(야외 포장 등)는 노멀을 다시 만들지 않는다
// - 만든 뒤 Shell/Materials, MagicIsland/Materials 에서 그 색상 텍스처를 쓰는 재질에 연결한다 (_Smoothness = 1)
// - 껍데기·야외맵 생성 도구의 Mat() 도 같은 파일을 찾아 붙이므로 다시 만들어도 풀리지 않는다
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LotteWorld.Look
{
    public static class LotteWorldSurfaceMaps
    {
        struct Target
        {
            public string path; public bool groutIsLight; public float normalStrength, smoothness;
            public Target(string p, bool light, float n, float s) { path = p; groutIsLight = light; normalStrength = n; smoothness = s; }
        }

        const string Shell = "Assets/Maps/LotteWorld/Shell/Textures/LW_Shell_";
        const string Mi = "Assets/Maps/LotteWorld/MagicIsland/Textures/MI_";
        static readonly string[] MaterialFolders =
        {
            "Assets/Maps/LotteWorld/Shell/Materials",
            "Assets/Maps/LotteWorld/MagicIsland/Materials",
        };

        //                       색상 텍스처          줄눈이 밝은가  노멀 세기  기준 매끄러움
        static readonly Target[] Targets =
        {
            new Target(Shell + "FloorBrick.png",  true,  6f, 0.30f),
            new Target(Shell + "ParadeBrick.png", false, 6f, 0.34f),
            new Target(Shell + "StoneBand.png",   false, 5f, 0.38f),
            new Target(Shell + "Plinth.png",      false, 5f, 0.22f),
            new Target(Shell + "Plaster.png",     false, 6f, 0.12f),
            new Target(Shell + "White.png",       false, 6f, 0.12f),
            new Target(Shell + "Teal.png",        false, 6f, 0.18f),
            new Target(Shell + "Ceiling.png",     false, 6f, 0.08f),
            new Target(Mi + "Paving.png",         false, 5f, 0.30f),
            new Target(Mi + "PavingDark.png",     false, 5f, 0.30f),
            new Target(Mi + "StoneGrey.png",      false, 5f, 0.28f),
            new Target(Mi + "StoneDark.png",      false, 5f, 0.28f),
            new Target(Mi + "Asphalt.png",        false, 5f, 0.20f),
            new Target(Mi + "LakeBed.png",        false, 5f, 0.10f),
        };

        const int StainRadius = 32;      // 이보다 큰 얼룩은 높이로 치지 않는다 (px)
        const float GroutDepth = 0.035f; // 높이가 이보다 낮으면 줄눈으로 본다

        public static string NormalPathOf(string basePath) => basePath.Substring(0, basePath.Length - 4) + "_Normal.png";
        public static string MsPathOf(string basePath) => basePath.Substring(0, basePath.Length - 4) + "_MS.png";

        [MenuItem("Tools/LotteWorld/Look/바닥·벽 요철·광택 만들기", false, 40)]
        static void BuildMenu() => Debug.Log(Build());

        [MenuItem("Tools/LotteWorld/Look/바닥·벽 요철·광택 떼기", false, 41)]
        static void DetachMenu() => Debug.Log(Detach());

        public static string Build()
        {
            int madeN = 0, madeMs = 0, keptN = 0, missing = 0;
            foreach (var t in Targets)
            {
                if (!File.Exists(t.path)) { missing++; continue; }
                var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                src.LoadImage(File.ReadAllBytes(t.path));
                int w = src.width, h = src.height;
                var px = src.GetPixels();
                Object.DestroyImmediate(src);

                var lum = new float[w * h];
                for (int i = 0; i < lum.Length; i++) lum[i] = px[i].r * 0.299f + px[i].g * 0.587f + px[i].b * 0.114f;
                var low = Blur(Blur(lum, w, h, StainRadius), w, h, StainRadius);   // 큰 얼룩
                float lowMean = 0f; for (int i = 0; i < low.Length; i++) lowMean += low[i]; lowMean /= low.Length;

                var height = new float[w * h];
                for (int i = 0; i < height.Length; i++) height[i] = (lum[i] - low[i]) * (t.groutIsLight ? -1f : 1f);
                height = Blur(height, w, h, 1);   // 픽셀 잡음 줄이기

                string np = NormalPathOf(t.path);
                if (File.Exists(np)) keptN++;
                else
                {
                    var n = new Color[w * h];
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            float dx = (height[y * w + (x + 1) % w] - height[y * w + (x + w - 1) % w]) * 0.5f;
                            float dy = (height[((y + 1) % h) * w + x] - height[((y + h - 1) % h) * w + x]) * 0.5f;
                            var v = new Vector3(-dx * t.normalStrength * 8f, -dy * t.normalStrength * 8f, 1f).normalized;
                            n[y * w + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                        }
                    WritePng(np, n, w, h);
                    madeN++;
                }

                var ms = new Color[w * h];
                for (int i = 0; i < ms.Length; i++)
                {
                    float s = t.smoothness;
                    s -= Mathf.Max(0f, lowMean - low[i]) * 1.2f;                               // 어두운 얼룩 = 때 = 덜 반짝임
                    s += Mathf.Clamp(height[i], -0.05f, 0.05f) * 0.8f;                         // 잔결
                    if (height[i] < -GroutDepth) s *= 0.45f;                                   // 줄눈
                    ms[i] = new Color(0f, 0f, 0f, Mathf.Clamp(s, 0.02f, 0.95f));
                }
                WritePng(MsPathOf(t.path), ms, w, h);
                madeMs++;
            }

            AssetDatabase.Refresh();
            foreach (var t in Targets)
            {
                FixImporter(NormalPathOf(t.path), true);
                FixImporter(MsPathOf(t.path), false);
            }
            int linked = Link();
            AssetDatabase.SaveAssets();
            return string.Format("[LW SurfaceMaps] 노멀 {0}장 새로 만듦(이미 있어 둔 것 {1}), 광택 {2}장 만듦, 재질 {3}개에 연결{4}",
                madeN, keptN, madeMs, linked, missing > 0 ? ", 색상 텍스처 없음 " + missing : "");
        }

        /// <summary>색상 텍스처 옆의 _Normal / _MS 를 재질에 붙인다. 생성 도구의 Mat() 도 이걸 부른다. 바뀌었으면 true.</summary>
        public static bool Attach(Material mat)
        {
            if (mat == null || !mat.HasProperty("_BaseMap")) return false;
            string bp = AssetDatabase.GetAssetPath(mat.GetTexture("_BaseMap"));
            if (string.IsNullOrEmpty(bp) || !bp.EndsWith(".png") || !IsTarget(bp)) return false;
            bool changed = false;
            var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPathOf(bp));
            if (nrm != null && mat.GetTexture("_BumpMap") == null)
            {
                mat.SetTexture("_BumpMap", nrm); mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); changed = true;
            }
            var ms = AssetDatabase.LoadAssetAtPath<Texture2D>(MsPathOf(bp));
            if (ms != null && (mat.GetTexture("_MetallicGlossMap") != ms || mat.GetFloat("_Smoothness") != 1f))
            {
                mat.SetTexture("_MetallicGlossMap", ms);
                mat.SetFloat("_Smoothness", 1f);              // 매끄러움은 텍스처 알파가 정한다
                mat.SetFloat("_SmoothnessTextureChannel", 0f);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                changed = true;
            }
            if (changed) EditorUtility.SetDirty(mat);
            return changed;
        }

        static int Link()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", MaterialFolders))
                if (Attach(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)))) n++;
            return n;
        }

        public static string Detach()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", MaterialFolders))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat == null || !mat.HasProperty("_BaseMap")) continue;
                string bp = AssetDatabase.GetAssetPath(mat.GetTexture("_BaseMap"));
                if (string.IsNullOrEmpty(bp) || !IsTarget(bp)) continue;
                if (mat.GetTexture("_MetallicGlossMap") == null) continue;
                Undo.RecordObject(mat, "LW SurfaceMaps 떼기");
                mat.SetTexture("_MetallicGlossMap", null);
                mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Smoothness", 0.15f);           // 두 생성 도구의 원래 기본값
                if (bp.StartsWith(Shell)) { mat.SetTexture("_BumpMap", null); mat.DisableKeyword("_NORMALMAP"); }   // 야외 노멀은 원래 있던 것
                EditorUtility.SetDirty(mat);
                n++;
            }
            AssetDatabase.SaveAssets();
            return "[LW SurfaceMaps] 재질 " + n + "개에서 뗌 (텍스처 파일은 남겨 둠. 지우면 생성 도구도 다시 붙이지 않는다)";
        }

        static bool IsTarget(string basePath)
        {
            foreach (var t in Targets) if (t.path == basePath) return true;
            return false;
        }

        // 이어지는(타일) 텍스처라서 가장자리는 반대편으로 넘어가며 평균낸다
        static float[] Blur(float[] src, int w, int h, int r)
        {
            var tmp = new float[src.Length]; var dst = new float[src.Length];
            float inv = 1f / (2 * r + 1);
            for (int y = 0; y < h; y++)
            {
                float sum = 0f;
                for (int k = -r; k <= r; k++) sum += src[y * w + ((k % w) + w) % w];
                for (int x = 0; x < w; x++)
                {
                    tmp[y * w + x] = sum * inv;
                    sum += src[y * w + (x + r + 1) % w] - src[y * w + ((x - r) % w + w) % w];
                }
            }
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int k = -r; k <= r; k++) sum += tmp[(((k % h) + h) % h) * w + x];
                for (int y = 0; y < h; y++)
                {
                    dst[y * w + x] = sum * inv;
                    sum += tmp[((y + r + 1) % h) * w + x] - tmp[(((y - r) % h + h) % h) * w + x];
                }
            }
            return dst;
        }

        static void WritePng(string path, Color[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            tex.SetPixels(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void FixImporter(string path, bool normal)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            bool dirty = false;
            if (normal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
            if (!normal)
            {
                if (ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                if (ti.alphaIsTransparency) { ti.alphaIsTransparency = false; dirty = true; }
                if (ti.alphaSource != TextureImporterAlphaSource.FromInput) { ti.alphaSource = TextureImporterAlphaSource.FromInput; dirty = true; }
            }
            if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
            if (LotteWorldTextureLimits.Apply(ti, path, 0)) dirty = true;   // _MS 512, _Normal 1024
            if (dirty) ti.SaveAndReimport();
        }
    }
}
#endif
