using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class MarketPlayerBuildReview
{
    const string Request="Temp/pepper-build.request";
    const string SceneRequest="Temp/market-scene-build.request";
    const string Refreshed="Market.SceneBuildRefreshed";
    static MarketPlayerBuildReview(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(File.Exists("Temp/market-recover-build.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating){
            File.Delete("Temp/market-recover-build.request");
            var partial=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(string.IsNullOrEmpty(partial.path))EditorSceneManager.OpenScene("Assets/Scenes/MarketPrototype_v7.unity");
        }
        bool rebuild=File.Exists(SceneRequest);
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || (!rebuild && !File.Exists(Request)))return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        if(!SessionState.GetBool(Refreshed,false))
        {
            SessionState.SetBool(Refreshed,true);
            AssetDatabase.Refresh();
            return;
        }
        SessionState.SetBool(Refreshed,false);
        if(EditorUtility.scriptCompilationFailed){
            if(rebuild)File.Delete(SceneRequest);else File.Delete(Request);
            Debug.LogError("Market build request stopped: fix compilation before regenerating the scene. The last compiled generator must not overwrite current work.");
            return;
        }
        if(rebuild)
        {
            File.Delete(SceneRequest);
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("Save the current scene before rebuilding the market.");
                return;
            }
            RebuildSceneAndPlayer();
        }
        else {File.Delete(Request);BuildPlayer();}
    }
    public static void RebuildSceneAndPlayer()
    {
        try{
            MarketPrototypeBuilderV7.BuildWithCatalogViews();
            BuildPlayer();
        }catch(System.Exception error){
            Directory.CreateDirectory("Builds/MarketReview");
            File.WriteAllText("Builds/MarketReview/build-result.txt","Failed: "+error);
            // A failed generator must not leave an unsaved, camera-less partial
            // scene in the editor. Restore the last scene saved by the builder.
            if(string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/MarketPrototype_v7.unity");
            throw;
        }
    }
    public static void BuildPlayer()
    {
        string folder="Builds/MarketReview";
        Directory.CreateDirectory(folder);
        BuildReport report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/MarketPrototype_v7.unity"},folder+"/Market.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        File.WriteAllText(folder+"/build-result.txt",report.summary.result.ToString());
        if(report.summary.result!=BuildResult.Succeeded)
            throw new System.InvalidOperationException("Market build: "+report.summary.result);
        // Optional clean exit permits standalone performance tests without editor memory/GPU contention.
        const string closeRequest="Temp/market-close-after-build.request";
        if(File.Exists(closeRequest)){
            File.Delete(closeRequest);
            if(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
        }
    }
}
