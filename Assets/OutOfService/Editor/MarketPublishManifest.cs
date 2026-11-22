using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Export the saved scene's real dependencies without rebuilding or changing it.
public static class MarketPublishManifest
{
    [Serializable] public class Manifest { public string scene; public string unity; public string[] dependencies; }
    public static void Export()
    {
        const string scene="Assets/Scenes/MarketPrototype_v7.unity";
        var entries=AssetDatabase.GetDependencies(new[]{scene,
            "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/PC_Renderer.asset",
            "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset"},true)
            .Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        Directory.CreateDirectory("Review");
        File.WriteAllText("Review/market-publish-dependencies.json",JsonUtility.ToJson(new Manifest{
            scene=scene,unity=Application.unityVersion,dependencies=entries},true));
        Debug.Log("Market publish dependency manifest: "+entries.Length+" assets; saved scene unchanged.");
    }
}
