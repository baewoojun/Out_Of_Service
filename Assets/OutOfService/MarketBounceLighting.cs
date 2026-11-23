using UnityEngine;

namespace OutOfService
{
    // A small authored diffuse-bounce approximation, not global illumination.
    // Follow sources after slab culling and flicker so blackout states agree.
    [DefaultExecutionOrder(200)]
    public sealed class MarketBounceLighting:MonoBehaviour
    {
        [System.Serializable]public struct Link{public Light source,bounce;public float gain,maximum;public Color surface;}
        public Link[] links;
        void LateUpdate(){Refresh();}
        public void Refresh(){
            if(links==null)return;
            foreach(var link in links){
                if(!link.bounce)continue;
                bool on=link.source && link.source.isActiveAndEnabled && link.source.intensity>.001f;
                if(link.bounce.enabled!=on)link.bounce.enabled=on;
                if(!on)continue;
                float value=Mathf.Min(link.maximum,link.source.intensity*link.gain);
                if(Mathf.Abs(link.bounce.intensity-value)>.0001f)link.bounce.intensity=value;
                Color color=link.source.color*link.surface;
                if(link.bounce.color!=color)link.bounce.color=color;
            }
        }
        void OnDisable(){if(links!=null)foreach(var link in links)if(link.bounce)link.bounce.enabled=false;}
    }
}
