// LotteWorldTextureLimits.cs - 롯데월드 맵 텍스처 최대 크기 규칙 (에디터).
// 그래픽 메모리를 아끼려고 종류별로 상한을 둔다. 색상 맵은 각 도구가 정한 크기를 그대로 쓴다.
//   _MS.png     (금속·거칠기)  -> 512
//   _Normal.png (노멀)         -> 1024
// 놀이기구·건물 설정 도구는 maxTextureSize 를 직접 올리지 말고 Apply() 를 불러야 한다.
// 이미 들어온 텍스처에 한꺼번에 적용: 메뉴 Tools > LotteWorld > 텍스처 크기 제한 다시 적용
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LotteWorldTextureLimits
{
    public const int MetalSmoothMax = 512;
    public const int NormalMax = 1024;
    const string Root = "Assets/Maps/LotteWorld";

    /// <summary>종류별 상한. 상한이 없는 텍스처(색상 맵 등)는 0.</summary>
    public static int CapFor(string path)
    {
        if (path.Contains("LT_TowerGlass")) return 0; // 롯데타워 몸통: 반복 없는 한 장이라 줄이면 창문이 뭉개진다
        if (path.EndsWith("_MS.png", System.StringComparison.OrdinalIgnoreCase)) return MetalSmoothMax;
        if (path.EndsWith("_Normal.png", System.StringComparison.OrdinalIgnoreCase)) return NormalMax;
        return 0;
    }

    /// <summary>
    /// 임포터의 최대 크기를 규칙에 맞춘다. 상한이 있는 종류는 그 값으로, 없으면 minSize 보다 작을 때만 올린다.
    /// 값이 바뀌었으면 true (부른 쪽이 SaveAndReimport 한다).
    /// </summary>
    public static bool Apply(TextureImporter ti, string path, int minSize)
    {
        int cap = CapFor(path);
        if (cap > 0)
        {
            if (ti.maxTextureSize == cap) return false;
            ti.maxTextureSize = cap;
            return true;
        }
        if (ti.maxTextureSize >= minSize) return false;
        ti.maxTextureSize = minSize;
        return true;
    }

    [MenuItem("Tools/LotteWorld/텍스처 크기 제한 다시 적용")]
    public static void ApplyAllMenu() => Debug.Log(ApplyAll());

    /// <summary>롯데월드 폴더의 _MS / _Normal 텍스처 전부에 상한을 적용하고 다시 임포트한다.</summary>
    public static string ApplyAll()
    {
        int changed = 0, total = 0;
        var paths = new System.Collections.Generic.List<string>();
        foreach (var file in Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/');
            if (CapFor(path) == 0) continue;
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            total++;
            if (!Apply(ti, path, 0)) continue;
            EditorUtility.SetDirty(ti);
            paths.Add(path);
            changed++;
        }
        AssetDatabase.StartAssetEditing();
        try { foreach (var p in paths) AssetDatabase.WriteImportSettingsIfDirty(p); }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
        return $"[LotteWorldTextureLimits] 대상 {total}장 중 {changed}장의 최대 크기를 바꿨다 (_MS {MetalSmoothMax}, _Normal {NormalMax}).";
    }
}
