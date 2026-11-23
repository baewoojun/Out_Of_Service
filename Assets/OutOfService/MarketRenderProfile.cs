using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OutOfService
{
    // The shared pipeline keeps the team's original default renderer. Only
    // market cameras opt into this renderer, including regenerated scenes.
    public static class MarketRenderProfile
    {
        public const string RendererName="Market_Renderer";
        public static int FindIndex(UniversalRenderPipelineAsset pipeline)
        {
            if(!pipeline)return -1;
            var renderers=pipeline.rendererDataList;
            for(int i=0;i<renderers.Length;i++)if(renderers[i]&&renderers[i].name==RendererName)return i;
            return -1;
        }
        public static bool Select(Camera camera,UniversalRenderPipelineAsset pipeline=null)
        {
            if(!camera)return false;
            int index=FindIndex(pipeline?pipeline:UniversalRenderPipeline.asset);
            if(index<0)return false;
            camera.GetUniversalAdditionalCameraData().SetRenderer(index);return true;
        }
    }
}
