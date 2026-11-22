Shader "OutOfService/WorldText"
{
    Properties { _MainTex("Font Atlas", 2D) = "white" {} _Color("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            struct A { float4 positionOS: POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float3 positionWS:TEXCOORD1; float3 normalWS:TEXCOORD2; half fog:TEXCOORD3; };
            V vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color*_Color; o.positionWS=TransformObjectToWorld(i.positionOS.xyz); o.normalWS=TransformObjectToWorldNormal(float3(0,0,-1)); o.fog=ComputeFogFactor(o.positionCS.z); return o; }
            half4 frag(V i):SV_Target {
                half a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                half3 normal=normalize(i.normalWS);
                half3 light=max(0,SampleSH(normal))+.035;
                Light main=GetMainLight();light+=main.color*saturate(dot(normal,main.direction));
                #ifdef _ADDITIONAL_LIGHTS
                uint count=GetAdditionalLightsCount();
                for(uint n=0;n<count;n++){
                    Light lamp=GetAdditionalLight(n,i.positionWS);
                    light+=lamp.color*lamp.distanceAttenuation*saturate(dot(normal,lamp.direction));
                }
                #endif
                return half4(MixFog(i.color.rgb*light,i.fog),i.color.a*a);
            }
            ENDHLSL
        }
    }
}
