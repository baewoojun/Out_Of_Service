using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class MarketPrototypeBuilderV7
{
    // Called once on newly generated fixtures, before probes are baked. No runtime
    // light scans or repeated multiplication of saved scene intensities.
    static void ApplyAfterHoursLightBalance()
    {
        foreach(var lamp in root.GetComponentsInChildren<Light>(true))
        {
            if(lamp.type==LightType.Directional)continue;
            bool route=lamp.name.Contains("stair lamp") || lamp.name.Contains("landing") || lamp.name.Contains("emergency");
            lamp.intensity*=route?.88f:.70f;
        }
        // Player is a separate root: torch intensity is intentionally unchanged.
    }

    static void MarketNightGrade()
    {
        const string path="Assets/OutOfService/Generated/MarketAfterHours.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(!profile)
        {
            profile=ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile,path);
        }
        T Setting<T>() where T:VolumeComponent
        {
            if(profile.TryGet<T>(out var setting))return setting;
            setting=profile.Add<T>(false);
            AssetDatabase.AddObjectToAsset(setting,profile);
            return setting;
        }
        var color=Setting<ColorAdjustments>();
        color.contrast.Override(6f);color.saturation.Override(-8f);
        var vignette=Setting<Vignette>();
        vignette.intensity.Override(.16f);vignette.smoothness.Override(.58f);
        var tonemap=Setting<Tonemapping>();
        tonemap.mode.Override(TonemappingMode.Neutral);
        foreach(var item in profile.components)EditorUtility.SetDirty(item);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        var obj=new GameObject("영업 종료 후 매장 색보정");
        obj.transform.SetParent(root);
        var volume=obj.AddComponent<Volume>();
        volume.isGlobal=true;volume.sharedProfile=profile;
    }
}
