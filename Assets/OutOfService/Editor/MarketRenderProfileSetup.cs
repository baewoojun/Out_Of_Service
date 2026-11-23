using System;
using System.IO;
using OutOfService;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class MarketRenderProfileSetup
{
    public static void PrepareAndBuild()
    {
        const string scenePath="Assets/Scenes/MarketPrototype_v7.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.isDirty)throw new InvalidOperationException("Save existing scene changes before renderer preparation.");
        if(scene.path!=scenePath)scene=EditorSceneManager.OpenScene(scenePath);
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        var shared=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        var market=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/OutOfService/Rendering/Market_Renderer.asset");
        int marketIndex=MarketRenderProfile.FindIndex(pipeline);
        var serializedPipeline=new SerializedObject(pipeline);
        if(marketIndex<1||pipeline.rendererDataList[0]!=shared||serializedPipeline.FindProperty("m_DefaultRendererIndex").intValue!=0)
            throw new InvalidOperationException("The team renderer must remain default; market renderer must be a separate entry.");
        float Ao(UniversalRendererData renderer){
            foreach(var feature in renderer.rendererFeatures)if(feature&&feature.GetType().Name=="ScreenSpaceAmbientOcclusion")
                return new SerializedObject(feature).FindProperty("m_Settings.Intensity").floatValue;
            return -1;
        }
        if(Mathf.Abs(Ao(shared)-.4f)>.001f||Mathf.Abs(Ao(market)-1.05f)>.001f)
            throw new InvalidOperationException("Unexpected shared/market contact shadow settings.");
        var player=UnityEngine.Object.FindFirstObjectByType<MarketPlayer>();
        if(!player||!MarketRenderProfile.Select(player.viewCamera,pipeline))throw new InvalidOperationException("Market camera missing");
        EditorUtility.SetDirty(player.viewCamera.GetUniversalAdditionalCameraData());
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Review");
        File.WriteAllText("Review/market-renderer-isolation-validation-2026-11-23.json",JsonUtility.ToJson(new Result{
            scene=scenePath,market_renderer_index=marketIndex,shared_default_index=0,shared_ao=Ao(shared),market_ao=Ao(market)},true));
        MarketPublishManifest.ExportAndBuild();
    }
    [Serializable] class Result {public string scene;public int market_renderer_index,shared_default_index;public float shared_ao,market_ao;}
}
