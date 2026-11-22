using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using OutOfService;

public static partial class MarketPrototypeBuilderV7
{
    static void BuildIndirectBounce()
    {
        var sources=root.GetComponentsInChildren<Light>();var links=new List<MarketBounceLighting.Link>();
        void Bounce(string sourceName,Vector3 p,Vector3 aim,float range,float gain,Color surface){
            Light source=null;float distance=float.PositiveInfinity;
            foreach(var candidate in sources)if(candidate.name==sourceName && (candidate.transform.position-p).sqrMagnitude<distance){source=candidate;distance=(candidate.transform.position-p).sqrMagnitude;}
            if(!source)throw new System.InvalidOperationException("Missing bounce source "+sourceName);
            var lamp=new GameObject("surface bounce - "+sourceName).AddComponent<Light>();lamp.transform.SetParent(root);
            lamp.transform.position=p;lamp.transform.LookAt(aim);lamp.type=LightType.Spot;
            lamp.spotAngle=115;lamp.innerSpotAngle=75;lamp.range=range;
            lamp.intensity=Mathf.Min(.65f,source.intensity*gain);lamp.color=source.color*surface;
            lamp.shadows=LightShadows.Soft;lamp.shadowBias=.02f;lamp.shadowNormalBias=.035f;
            lamp.GetUniversalAdditionalLightData().softShadowQuality=SoftShadowQuality.Low;
            var data=new SerializedObject(lamp.GetUniversalAdditionalLightData());data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=0;data.ApplyModifiedPropertiesWithoutUndo();
            links.Add(new MarketBounceLighting.Link{source=source,bounce=lamp,gain=gain,maximum=.65f,surface=surface});
        }
        Bounce("cafe counter task light",new(-21.4f,-3.70f,-14),new(-23.35f,-1.30f,-14),4.6f,.17f,new(1,.91f,.80f));
        Bounce("beauty counter task light",new(-27,-3.80f,-32.7f),new(-29,-1.3f,-34),4.4f,.14f,new(.94f,.96f,.91f));
        Bounce("checkout remaining work light",new(7.5f,.18f,-30),new(7.5f,3.8f,-31.8f),4.5f,.12f,new(1,.94f,.85f));
        Bounce("pepper track lamp",new(-28.7f,.25f,-8),new(-28.7f,4,-8),4.3f,.10f,new(.90f,.92f,.87f));
        Bounce("upper floor night lamp",new(-31.5f,5.18f,-18),new(-31.5f,9,-18),4.4f,.12f,new(.94f,.89f,.81f));
        foreach(float z in new[]{-22f,18f})Bounce("B1 concourse light",new(0,-4.8f,z),new(0,-.85f,z),4.5f,.11f,new(.94f,.90f,.82f));
        // Keep runtime controllers out of static renderers' ancestry: the prop batcher
        // intentionally leaves anything beneath a MonoBehaviour independently movable.
        var controllerObject=new GameObject("Source linked surface bounce controller");
        controllerObject.transform.SetParent(root,false);
        var controller=controllerObject.AddComponent<MarketBounceLighting>();controller.links=links.ToArray();
    }
}
