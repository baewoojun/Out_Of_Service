Shader "OutOfService/Thin PET"
{
    Properties
    {
        [MainColor] _BaseColor("Transmission tint", Color) = (0.985,0.995,1,1)
        _Smoothness("Surface smoothness", Range(0,1)) = 0.88
        _RefractionPixels("Small refraction at 1080p", Range(0,16)) = 8
        _EdgeOpacity("Edge coverage", Range(0,1)) = 0.26
        _EdgeLoss("Grazing transmission loss", Range(0,1)) = 0.55
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
        Pass
        {
            Name "ThinPETForward"
            Tags {"LightMode"="UniversalForward"}
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _ALPHAPREMULTIPLY_ON 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness;
                float _RefractionPixels;
                half _EdgeOpacity;
                half _EdgeLoss;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes v)
            {
                Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o;
            }
            half4 Frag(Varyings v):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(v);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                InputData input=(InputData)0;
                input.positionWS=v.positionWS;input.positionCS=v.positionCS;
                input.normalWS=NormalizeNormalPerPixel(v.normalWS);input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(v.positionWS);
                input.shadowCoord=TransformWorldToShadowCoord(v.positionWS);
                input.bakedGI=SampleSH(input.normalWS);input.shadowMask=half4(1,1,1,1);
                input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(v.positionCS);
                SurfaceData surface=(SurfaceData)0;
                // The plastic contributes dielectric specular, not a milky diffuse coat.
                surface.albedo=0;surface.alpha=0;surface.metallic=0;surface.smoothness=_Smoothness;surface.occlusion=1;
                half3 specular=UniversalFragmentPBR(input,surface).rgb;
                half edge=pow(1-saturate(dot(input.normalWS,input.viewDirectionWS)),2);
                float2 normalVS=TransformWorldToViewDir(input.normalWS).xy;
                float2 offset=normalVS*(_RefractionPixels/1080.0)*(1-edge);
                offset.x*=_ScaledScreenParams.y/_ScaledScreenParams.x;
                float2 uv=input.normalizedScreenSpaceUV;
                // One shared camera color copy. It includes labels and opaque liquid,
                // avoiding white diffuse accumulation through several bottles.
                half3 transmitted=SampleSceneColor(uv+offset)*_BaseColor.rgb*(.96-edge*_EdgeLoss);
                half coverage=saturate(.70+edge*_EdgeOpacity);
                return half4(transmitted*coverage+specular,coverage);
            }
            ENDHLSL
        }
    }
}
