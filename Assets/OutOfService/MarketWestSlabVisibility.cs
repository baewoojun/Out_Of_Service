using UnityEngine;

namespace OutOfService
{
    // Conservative occlusion by authored solid slab rectangles. Both the camera
    // and the complete object footprint must lie inside the SAME solid rectangle.
    // Stair/escalator holes and the atrium are never used as occluders.
    public sealed class MarketWestSlabVisibility : MonoBehaviour
    {
        public Bounds[] slabs;
        public Renderer[] staticStock;
        public Light[] staticLights;
        uint[] above,below,lightAbove,lightBelow;
        Camera view;
        uint lastAbove=uint.MaxValue,lastBelow=uint.MaxValue;
        void Start()
        {
            above=new uint[staticStock.Length];below=new uint[staticStock.Length];
            for(int i=0;i<staticStock.Length;i++)if(staticStock[i])Masks(staticStock[i].bounds,out above[i],out below[i]);
            lightAbove=new uint[staticLights.Length];lightBelow=new uint[staticLights.Length];
            for(int i=0;i<staticLights.Length;i++)if(staticLights[i]){
                var light=staticLights[i];
                // A downward spot cone fits inside this conservative half-sphere box.
                if(light.type!=LightType.Spot || Vector3.Angle(light.transform.forward,Vector3.down)+light.spotAngle*.5f>=89)continue;
                var bounds=new Bounds(light.transform.position-Vector3.up*light.range*.5f,new Vector3(light.range*2,light.range,light.range*2));
                Masks(bounds,out lightAbove[i],out lightBelow[i]);
                // Even a non-shadowed ceiling lamp must not illuminate a sealed
                // storey below it. Its full projected range must fit in the slab.
                for(int j=0;j<slabs.Length && j<32;j++){
                    var s=slabs[j];
                    if(bounds.min.x>=s.min.x && bounds.max.x<=s.max.x && bounds.min.z>=s.min.z && bounds.max.z<=s.max.z && light.transform.position.y>s.max.y+.1f)
                        lightAbove[i]|=1u<<j;
                }
            }
        }
        void Masks(Bounds b,out uint up,out uint down)
        {
            up=down=0;
            for(int j=0;j<slabs.Length && j<32;j++){
                var s=slabs[j];
                if(b.min.x<s.min.x || b.max.x>s.max.x || b.min.z<s.min.z || b.max.z>s.max.z)continue;
                if(b.min.y>=s.max.y+.001f)up|=1u<<j;
                if(b.max.y<=s.min.y-.001f)down|=1u<<j;
            }
        }
        void LateUpdate()
        {
            if(!view) view=Camera.main;
            if(!view || above==null)return;
            Vector3 eye=view.transform.position;
            uint up=0,down=0;
            for(int i=0;i<slabs.Length && i<32;i++){
                var s=slabs[i];
                if(eye.x<=s.min.x+.1f || eye.x>=s.max.x-.1f || eye.z<=s.min.z+.1f || eye.z>=s.max.z-.1f)continue;
                if(eye.y>s.max.y+.1f)up|=1u<<i;
                if(eye.y<s.min.y-.1f)down|=1u<<i;
            }
            if(up==lastAbove && down==lastBelow)return;
            for(int i=0;i<staticStock.Length;i++)if(staticStock[i])
                staticStock[i].forceRenderingOff=((above[i]&down)|(below[i]&up))!=0;
            for(int i=0;i<staticLights.Length;i++)if(staticLights[i])
                staticLights[i].enabled=((lightAbove[i]&down)|(lightBelow[i]&up))==0;
            lastAbove=up;lastBelow=down;
        }
        void OnDisable()
        {
            if(staticStock!=null)foreach(var r in staticStock)if(r)r.forceRenderingOff=false;
            if(staticLights!=null)foreach(var light in staticLights)if(light)light.enabled=true;
            lastAbove=lastBelow=uint.MaxValue;
        }
    }
}
