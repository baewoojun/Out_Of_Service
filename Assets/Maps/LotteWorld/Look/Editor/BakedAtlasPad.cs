// BakedAtlasPad.cs - 구운 텍스처(조각들 사이 배경이 검정)의 검은 배경을 가장 가까운 조각 색으로 채움
// 증상: 품질 단계가 낮아 텍스처 해상도가 줄면(밉맵 제한) 검은 배경이 조각 안으로 번져 건물에 검은 네모·세모가 생김
// 메뉴: Tools > LotteWorld > Look > 구운 텍스처 검은 배경 채우기   (Rides, MagicIsland/Baked 의 *_BaseColor / _MS / _Normal PNG 를 고쳐 씀)
// 반복 텍스처(Tile_*)·투명 잎 텍스처는 건드리지 않음. 이미 채운 텍스처는 검은 배경이 없어 건너뜀
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LotteWorld.Look
{
    public static class BakedAtlasPad
    {
        static readonly string[] Roots = { "Assets/Maps/LotteWorld/Rides", "Assets/Maps/LotteWorld/MagicIsland/Baked" };

        [MenuItem("Tools/LotteWorld/Look/구운 텍스처 검은 배경 채우기")]
        public static void RunAll() { Run(null); }
        [MenuItem("Tools/LotteWorld/Look/구운 텍스처 검은 배경 채우기 (시험: BldTimber 만)")]
        public static void RunTest() { Run("/BldTimber/"); }

        static void Run(string only)
        {
            Debug.Log("[AtlasPad] 지금 품질 단계: " + QualitySettings.names[QualitySettings.GetQualityLevel()] + ", 텍스처 밉맵 제한: " + QualitySettings.globalTextureMipmapLimit);
            var files = new List<string>();
            foreach (string root in Roots)
                if (Directory.Exists(root))
                    foreach (string f in Directory.GetFiles(root, "*_BaseColor.png", SearchOption.AllDirectories))
                    { string n = Path.GetFileName(f); string p = f.Replace('\\', '/'); if (!n.StartsWith("Tile_") && (only == null || p.Contains(only))) files.Add(p); }
            int done = 0, skipped = 0, written = 0; bool cancel = false;
            try
            {
                for (int i = 0; i < files.Count && !cancel; i++)
                {
                    if (i % 5 == 0) cancel = EditorUtility.DisplayCancelableProgressBar("구운 텍스처 검은 배경 채우기", Path.GetFileName(files[i]) + "  (" + i + "/" + files.Count + ")", (float)i / files.Count);
                    int w = One(files[i]); if (w > 0) { done++; written += w; } else skipped++;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.Refresh();
            Debug.Log("[AtlasPad] " + (cancel ? "중간에 멈춤 — " : "") + "색상 텍스처 " + files.Count + "장 중 " + done + "장 채움 (PNG " + written + "개 고쳐 씀), " + skipped + "장은 검은 배경 없음·제외. 품질 단계: " + QualitySettings.names[QualitySettings.GetQualityLevel()] + ", 밉맵 제한 " + QualitySettings.globalTextureMipmapLimit);
        }

        static Texture2D Load(string path)
        {
            if (!File.Exists(path)) return null;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!t.LoadImage(File.ReadAllBytes(path), false)) { Object.DestroyImmediate(t); return null; }
            return t;
        }

        /// <summary>한 재질(색상 + _MS + _Normal). 고쳐 쓴 PNG 수를 돌려줌</summary>
        static int One(string basePath)
        {
            Texture2D tex = Load(basePath); if (tex == null) return 0;
            int w = tex.width, h = tex.height, n = w * h; Color32[] px = tex.GetPixels32();
            var bg = new bool[n]; int cnt = 0;
            for (int i = 0; i < n; i++)
            {
                if (px[i].a < 250) { Object.DestroyImmediate(tex); return 0; }       // 투명 텍스처(잎 카드 등)는 제외
                if (px[i].r == 0 && px[i].g == 0 && px[i].b == 0) { bg[i] = true; cnt++; }
            }
            if (cnt < n / 200 || cnt > n * 95L / 100) { Object.DestroyImmediate(tex); return 0; }
            // 조각 가장자리의 어두운 번짐 2픽셀도 배경으로 (그 색이 퍼지지 않게)
            for (int pass = 0; pass < 2; pass++)
            {
                var add = new List<int>();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * w + x; if (bg[i]) continue;
                        if ((x > 0 && bg[i - 1]) || (x < w - 1 && bg[i + 1]) || (y > 0 && bg[i - w]) || (y < h - 1 && bg[i + w])) add.Add(i);
                    }
                foreach (int i in add) bg[i] = true;
            }
            // 가장 가까운 조각 픽셀 찾기 (여러 곳에서 동시에 퍼지는 너비 우선)
            var src = new int[n]; var q = new Queue<int>();
            for (int i = 0; i < n; i++) src[i] = bg[i] ? -1 : i;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x; if (bg[i]) continue;
                    if ((x > 0 && bg[i - 1]) || (x < w - 1 && bg[i + 1]) || (y > 0 && bg[i - w]) || (y < h - 1 && bg[i + w])) q.Enqueue(i);
                }
            if (q.Count == 0) { Object.DestroyImmediate(tex); return 0; }
            while (q.Count > 0)
            {
                int i = q.Dequeue(); int x = i % w, y = i / w;
                if (x > 0 && src[i - 1] < 0) { src[i - 1] = src[i]; q.Enqueue(i - 1); }
                if (x < w - 1 && src[i + 1] < 0) { src[i + 1] = src[i]; q.Enqueue(i + 1); }
                if (y > 0 && src[i - w] < 0) { src[i - w] = src[i]; q.Enqueue(i - w); }
                if (y < h - 1 && src[i + w] < 0) { src[i + w] = src[i]; q.Enqueue(i + w); }
            }
            int wrote = 0;
            for (int i = 0; i < n; i++) if (bg[i] && src[i] >= 0) px[i] = px[src[i]];
            tex.SetPixels32(px); File.WriteAllBytes(basePath, tex.EncodeToPNG()); wrote++; Object.DestroyImmediate(tex);
            string stem = basePath.Substring(0, basePath.Length - "_BaseColor.png".Length);
            foreach (string suf in new[] { "_MS.png", "_Normal.png" })
            {
                Texture2D t2 = Load(stem + suf); if (t2 == null) continue;
                if (t2.width == w && t2.height == h)
                {
                    Color32[] p2 = t2.GetPixels32();
                    for (int i = 0; i < n; i++) if (bg[i] && src[i] >= 0) p2[i] = p2[src[i]];
                    t2.SetPixels32(p2); File.WriteAllBytes(stem + suf, t2.EncodeToPNG()); wrote++;
                }
                Object.DestroyImmediate(t2);
            }
            return wrote;
        }
    }
}
#endif
