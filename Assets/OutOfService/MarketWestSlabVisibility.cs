using System.Collections.Generic;
using UnityEngine;

namespace OutOfService
{
    // Conservative slab occlusion. The full object footprint and eye must be
    // inside the same authored solid slab; atria and stair holes remain visible.
    public sealed class MarketWestSlabVisibility : MonoBehaviour
    {
        public Bounds[] slabs;
        public Renderer[] staticStock;
        public Light[] staticLights;
        sealed class Sector
        {
            public uint above,below;
            public Renderer[] renderers;
            public bool hidden;
        }
        Sector[] sectors;
        uint[] lightAbove,lightBelow;
        bool[] lightHidden;
        Camera view;
        uint lastAbove=uint.MaxValue,lastBelow=uint.MaxValue;
        public int LastRendererChanges { get; private set; }
        public int TotalRendererChanges { get; private set; }
        public int SectorCount=>sectors==null?0:sectors.Length;
        void Start()
        {
            var grouped=new Dictionary<ulong,List<Renderer>>();
            foreach(var renderer in staticStock)if(renderer) {
                Masks(renderer.bounds,out uint a,out uint b);
                // Unoccludable props never need a per-transition native setter.
                if(a==0&&b==0)continue;
                ulong key=((ulong)a<<32)|b;
                if(!grouped.TryGetValue(key,out var list)){list=new List<Renderer>();grouped.Add(key,list);}
                list.Add(renderer);
            }
            sectors=new Sector[grouped.Count];int index=0;
            foreach(var pair in grouped)sectors[index++]=new Sector{above=(uint)(pair.Key>>32),below=(uint)pair.Key,renderers=pair.Value.ToArray()};
            lightAbove=new uint[staticLights.Length];lightBelow=new uint[staticLights.Length];lightHidden=new bool[staticLights.Length];
            for(int i=0;i<staticLights.Length;i++)if(staticLights[i]) {
                var light=staticLights[i];
                if(light.type!=LightType.Spot||Vector3.Angle(light.transform.forward,Vector3.down)+light.spotAngle*.5f>=89)continue;
                var bounds=new Bounds(light.transform.position-Vector3.up*light.range*.5f,new Vector3(light.range*2,light.range,light.range*2));
                Masks(bounds,out lightAbove[i],out lightBelow[i]);
                for(int j=0;j<slabs.Length&&j<32;j++) {
                    var s=slabs[j];
                    if(bounds.min.x>=s.min.x&&bounds.max.x<=s.max.x&&bounds.min.z>=s.min.z&&bounds.max.z<=s.max.z&&light.transform.position.y>s.max.y+.1f)lightAbove[i]|=1u<<j;
                }
            }
        }
        void Masks(Bounds b,out uint up,out uint down)
        {
            up=down=0;
            for(int j=0;j<slabs.Length&&j<32;j++) {
                var s=slabs[j];
                if(b.min.x<s.min.x||b.max.x>s.max.x||b.min.z<s.min.z||b.max.z>s.max.z)continue;
                if(b.min.y>=s.max.y+.001f)up|=1u<<j;
                if(b.max.y<=s.min.y-.001f)down|=1u<<j;
            }
        }
        void LateUpdate()
        {
            LastRendererChanges=0;
            if(!view)view=Camera.main;
            if(!view||sectors==null)return;
            Vector3 eye=view.transform.position;uint up=0,down=0;
            for(int i=0;i<slabs.Length&&i<32;i++) {
                var s=slabs[i];
                if(eye.x<=s.min.x+.1f||eye.x>=s.max.x-.1f||eye.z<=s.min.z+.1f||eye.z>=s.max.z-.1f)continue;
                if(eye.y>s.max.y+.1f)up|=1u<<i;
                if(eye.y<s.min.y-.1f)down|=1u<<i;
            }
            if(up==lastAbove&&down==lastBelow)return;
            foreach(var sector in sectors) {
                bool hidden=((sector.above&down)|(sector.below&up))!=0;
                if(sector.hidden==hidden)continue;
                foreach(var renderer in sector.renderers)if(renderer){renderer.forceRenderingOff=hidden;LastRendererChanges++;}
                sector.hidden=hidden;
            }
            for(int i=0;i<staticLights.Length;i++)if(staticLights[i]) {
                bool hidden=((lightAbove[i]&down)|(lightBelow[i]&up))!=0;
                if(lightHidden[i]==hidden)continue;
                staticLights[i].enabled=!hidden;lightHidden[i]=hidden;
            }
            TotalRendererChanges+=LastRendererChanges;lastAbove=up;lastBelow=down;
        }
        void OnDisable()
        {
            if(sectors!=null)foreach(var sector in sectors){if(sector.hidden)foreach(var renderer in sector.renderers)if(renderer)renderer.forceRenderingOff=false;sector.hidden=false;}
            if(staticLights!=null&&lightHidden!=null)for(int i=0;i<staticLights.Length;i++)if(lightHidden[i]){if(staticLights[i])staticLights[i].enabled=true;lightHidden[i]=false;}
            lastAbove=lastBelow=uint.MaxValue;
        }
    }
}
