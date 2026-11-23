using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class DairyPlayReview
{
    const string Key = "Market.DairyReview";
    static int frames;
    static DairyPlayReview() { EditorApplication.update += Tick; }
    public static void Run()
    {
        MarketPrototypeBuilderV7.Build();
        SessionState.SetBool(Key,true);
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (File.Exists("Temp/dairy-review.request") && EditorApplication.isPlaying && !SessionState.GetBool(Key,false)) { EditorApplication.isPlaying = false; return; }
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists("Temp/dairy-review.request"))
        {
            File.Delete("Temp/dairy-review.request");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("Dairy review paused: save open scene before rebuilding.");
                return;
            }
            if (File.Exists("Temp/dairy-capture-only")) {
                File.Delete("Temp/dairy-capture-only");
                SessionState.SetBool(Key,true);
                EditorApplication.isPlaying = true;
            } else Run();
            return;
        }
        if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || !Application.isPlaying) return;
        var p = OutOfService.MarketPlayer.Instance;
        if (!p || ++frames < 35) return;
        if (File.Exists("Temp/market-assortment-review"))
        {
            File.Delete("Temp/market-assortment-review");
            SessionState.SetBool(Key,false);
            MarketPrototypeBuilderV7.RunSceneReview();
            return;
        }
        MarketPrototypeBuilderV7.PreviewDairy();
        p.viewCamera.transform.LookAt(new Vector3(-17,1.45f,42));
        p.bareHandsModel.SetActive(false);
        p.flashlightModel.SetActive(true);
        foreach (var group in Object.FindObjectsByType<OutOfService.MarketDetailVisibility>()) group.Refresh();
        var rt = RenderTexture.GetTemporary(960,540,24);
        p.viewCamera.targetTexture = rt;
        p.viewCamera.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(960,540,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,960,540),0,0);
        File.WriteAllBytes("Review/dairy-revised.png",tex.EncodeToPNG());
        p.viewCamera.transform.position = new Vector3(-16.85f,1.28f,40.3f);
        p.viewCamera.transform.LookAt(new Vector3(-17f,1.2f,42f));
        p.viewCamera.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0,0,960,540),0,0);
        File.WriteAllBytes("Review/dairy-close.png",tex.EncodeToPNG());
        p.viewCamera.targetTexture = null;
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        SessionState.SetBool(Key,false);
        if (Application.isBatchMode) EditorApplication.Exit(0);
        else EditorApplication.isPlaying = false;
    }
}
