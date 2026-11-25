#if UNITY_EDITOR
// BldParkUnzip.cs - 11/25 구운 건물 14곳 zip 풀고 재질 연결까지 한 번에
// zip 4개는 Assets/Maps/LotteWorld/_bld_incoming~/ 에 있음 (~ 로 끝나는 폴더는 Unity 가 안 읽음)
// 메뉴 Tools > LotteWorld > Look > 건물 고퀄 0. 받은 zip 풀고 연결
//   1) zip 안의 Assets/... 파일을 프로젝트에 덮어씀 (.cs 는 건너뜀, 이미 들어가 있음)
//   2) AssetDatabase.Refresh (FBX·텍스처 다시 읽기, 시간 좀 걸림)
//   3) BldParkSetup.Setup() = "건물 고퀄 재질 연결"
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace LotteWorldLook
{
    public static class BldParkUnzip
    {
        const string Incoming = "Assets/Maps/LotteWorld/_bld_incoming~";

        [MenuItem("Tools/LotteWorld/Look/건물 고퀄 0. 받은 zip 풀고 연결")]
        public static void Run()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string inc = Path.Combine(root, Incoming);
            if (!Directory.Exists(inc)) { Debug.LogError("[BldPark] zip 폴더 없음: " + inc); return; }
            var zips = Directory.GetFiles(inc, "*.zip");
            if (zips.Length == 0) { Debug.LogError("[BldPark] zip 없음: " + inc); return; }
            int n = 0, k = 0;
            try
            {
                foreach (var z in zips)
                {
                    k++;
                    using (var fs = File.OpenRead(z))
                    using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        int i = 0;
                        foreach (var e in za.Entries)
                        {
                            i++;
                            if (string.IsNullOrEmpty(e.Name) || e.FullName.EndsWith(".cs")) continue;
                            if (!e.FullName.StartsWith("Assets/")) continue;
                            if (i % 20 == 0) EditorUtility.DisplayProgressBar("건물 zip 풀기", $"{k}/{zips.Length} {Path.GetFileName(z)}  {e.Name}", (k - 1 + (float)i / za.Entries.Count) / zips.Length);
                            string dst = Path.Combine(root, e.FullName.Replace('/', Path.DirectorySeparatorChar));
                            Directory.CreateDirectory(Path.GetDirectoryName(dst));
                            using (var src = e.Open()) using (var o = File.Create(dst)) src.CopyTo(o);
                            n++;
                        }
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[BldPark] zip {zips.Length}개에서 파일 {n}개 풀었음. 다시 읽는 중...");
            AssetDatabase.Refresh();
            BldParkSetup.Setup();
            Debug.Log("[BldPark] zip 풀기 + 재질 연결 끝. 확인되면 Assets/Maps/LotteWorld/_bld_incoming~ 폴더는 지워도 됨(860MB)");
        }
    }
}
#endif
