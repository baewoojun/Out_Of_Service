using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace OutOfService
{
    // Only static decorative batches. Doors, pickups, architecture, signs and collision are never culled.
    public sealed class MarketDetailVisibility : MonoBehaviour
    {
        public Renderer[] details;
        public float visibleDistance=34f;
        Bounds[] bounds;
        Camera view;
        float nextCheck;
        Vector3 lastEye=new Vector3(float.PositiveInfinity,0,0);
        public bool optimized=true;
        public void Configure(bool value)
        {
            optimized=value;
            if(!view)view=Camera.main;
            int layer=LayerMask.NameToLayer("Market Small Props");
            if(view && layer>=0)
            {
                var distances=view.layerCullDistances;
                distances[layer]=value?23:0;
                view.layerCullDistances=distances;
                var player=view.GetComponentInParent<MarketPlayer>();
                var torch=player ? player.flashlight : view.GetComponent<Light>();
                if(torch && torch.type==LightType.Spot)
                {
                    // Bottle caps and package folds cast fine shadows in the close beam.
                    // Doors, the monster and shelf frames keep their full shadow distance.
                    var shadowDistances=torch.layerShadowCullDistances;
                    shadowDistances[layer]=value?9f:0f;
                    torch.layerShadowCullDistances=shadowDistances;
                    var shadows=torch.GetUniversalAdditionalLightData();
                    // Visibility A/B must not switch shadow shader variants or
                    // silently change filtering quality while restoring renderers.
                    shadows.softShadowQuality=SoftShadowQuality.Medium;
                    shadows.additionalLightsShadowResolutionTier=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;
                }
            }
            // A/B validation must really restore all decorative sectors when
            // optimization is disabled, including sectors hidden on the last floor.
            Refresh();
        }
        void Start()
        {
            view=Camera.main;bounds=new Bounds[details.Length];
            for(int i=0;i<details.Length;i++)if(details[i]!=null)bounds[i]=details[i].bounds;
            Configure(optimized); Refresh();
        }
        void Update(){if(optimized && view && Time.unscaledTime>=nextCheck && (view.transform.position-lastEye).sqrMagnitude>.16f){nextCheck=Time.unscaledTime+.15f;Refresh();}}
        public void Refresh()
        {
            if(view==null||bounds==null)return;
            Vector3 p=view.transform.position;lastEye=p;
            for(int i=0;i<details.Length;i++)if(details[i]!=null)
            {
                float radius=visibleDistance+(details[i].enabled?3f:0);
                bool show=!optimized || bounds[i].SqrDistance(p)<radius*radius;
                if(details[i].enabled!=show)details[i].enabled=show;
            }
        }
        void OnDisable(){if(details!=null)foreach(var r in details)if(r!=null)r.enabled=true;}
    }
}
