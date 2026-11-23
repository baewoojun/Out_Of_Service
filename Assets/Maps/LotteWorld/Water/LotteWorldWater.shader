// LotteWorldWater.shader  (v1, 11/20) - realistic water for the Lotte World map (flume ride A08, north-gate waterfall, ponds)
// URP (Unity 6.x). Opaque-in-transparent-queue water: it reads the camera Opaque + Depth textures for refraction, depth colour
// and shoreline foam, then lights the surface with URP's own PBR (sun, extra lights, shadows, reflection probes, fog).
//
// Mesh data it uses (all optional - without it the water is still water, just without the baked extras):
//   TEXCOORD3.xyz = flow direction (object space, unit or zero)  -> water on flat parts moves that way
//   TEXCOORD3.w   = impact foam 0..1                              -> white churn where a fall/drop lands
// Steep faces always flow straight down the slope (waterfall sheets, flume drops) - no data needed.
// LotteWorldWaterTool.cs (Tools > LotteWorld > Water) bakes TEXCOORD3, swaps materials and adds splash particles.
// Needs "Depth Texture" + "Opaque Texture" on the URP asset (the tool switches both on).

Shader "LotteWorld/Water"
{
    Properties
    {
        [Header(Colour)]
        _DeepColor     ("Deep colour (scattered light)", Color) = (0.04, 0.13, 0.13, 1)
        _Tint          ("See-through tint", Color) = (0.75, 0.92, 0.88, 1)
        _Absorption    ("Absorption per metre (RGB)", Vector) = (0.55, 0.22, 0.18, 0)
        _Turbidity     ("Murk (lower = clearer)", Range(0, 4)) = 0.9

        [Header(Surface)]
        _Smoothness    ("Smoothness", Range(0, 1)) = 0.95
        _WaveScale     ("Wave size (m)", Range(0.1, 4)) = 0.7
        _WaveStrength  ("Wave strength", Range(0, 2)) = 0.55
        _Refraction    ("Refraction", Range(0, 0.2)) = 0.04

        [Header(Flow)]
        _FlowSpeed     ("Flow speed on flat parts (m per s)", Range(0, 4)) = 1.0
        _FallSpeed     ("Fall speed on slopes (m per s)", Range(0, 12)) = 5.0
        _FlowCycle     ("Flow cycle (per s)", Range(0.1, 2)) = 0.6
        _Ripple        ("Still-water ripple", Range(0, 1)) = 0.35

        [Header(Foam)]
        _FoamColor     ("Foam colour", Color) = (0.86, 0.88, 0.86, 1)
        _EdgeFoam      ("Edge foam width (m)", Range(0, 1.5)) = 0.3
        _ImpactFoam    ("Impact foam (baked)", Range(0, 2)) = 1.0
        _FallWhite     ("White water on falls", Range(0, 1)) = 0.75
        _FoamScale     ("Foam grain (m)", Range(0.05, 2)) = 0.35

        [Header(Neglect)]
        _ScumColor     ("Scum colour", Color) = (0.11, 0.12, 0.07, 1)
        _Scum          ("Floating scum", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent-10" "IgnoreProjector" = "True" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend Off
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // transparent-queue surface: never read the screen-space shadow map (that one is for opaques)
            #define _SURFACE_TYPE_TRANSPARENT 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _DeepColor, _Tint, _FoamColor, _ScumColor;
                float4 _Absorption;
                half   _Turbidity, _Smoothness, _WaveStrength, _Refraction, _Ripple;
                half   _EdgeFoam, _ImpactFoam, _FallWhite, _Scum;
                float  _WaveScale, _FlowSpeed, _FallSpeed, _FlowCycle, _FoamScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 flowOS     : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 flowWS     : TEXCOORD2;   // xyz flow dir (world), w impact foam
                float  fogCoord   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.flowWS = float4(TransformObjectToWorldDir(v.flowOS.xyz, false), v.flowOS.w);
                o.fogCoord = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // ---------------------------------------------------------------- noise
            float2 Hash2(float2 p)
            {
                p = fmod(p, 289.0);
                float3 q = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                q += dot(q, q.yzx + 33.33);
                return frac((q.xx + q.yz) * q.zy) * 2.0 - 1.0;
            }

            // gradient noise with analytic derivatives: x = value (-1..1), yz = d/dp
            float3 NoiseD(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float2 du = 30.0 * f * f * (f * (f - 2.0) + 1.0);
                float2 ga = Hash2(i), gb = Hash2(i + float2(1, 0)), gc = Hash2(i + float2(0, 1)), gd = Hash2(i + float2(1, 1));
                float va = dot(ga, f), vb = dot(gb, f - float2(1, 0)), vc = dot(gc, f - float2(0, 1)), vd = dot(gd, f - float2(1, 1));
                float k = va - vb - vc + vd;
                float v = va + u.x * (vb - va) + u.y * (vc - va) + u.x * u.y * k;
                float2 d = ga + u.x * (gb - ga) + u.y * (gc - ga) + u.x * u.y * (ga - gb - gc + gd) + du * (u.yx * k + float2(vb, vc) - va);
                return float3(v, d);
            }

            // two octaves; uv in metres, returns (height, dh/du, dh/dv) per metre. aniso stretches the pattern (u, v)
            float3 Waves(float2 uv, float2 aniso)
            {
                float2 s = aniso / _WaveScale;
                float3 a = NoiseD(uv * s);
                float3 b = NoiseD(uv * s * 2.13 + float2(17.3, 5.1));
                return float3(a.x + 0.45 * b.x, a.yz * s + 0.45 * 2.13 * b.yz * s);
            }

            // flow-map waves: two phases half a cycle apart, cross-faded so the pattern never stretches out
            float3 FlowWaves(float2 uv, float2 flow, float2 aniso)
            {
                float t = _Time.y * _FlowCycle;
                float p0 = frac(t), p1 = frac(t + 0.5);
                float w0 = 1.0 - abs(1.0 - 2.0 * p0), w1 = 1.0 - abs(1.0 - 2.0 * p1);
                float2 step_ = flow / _FlowCycle;                       // metres travelled in one cycle
                float3 a = Waves(uv - step_ * p0, aniso);
                float3 b = Waves(uv - step_ * p1 + float2(0.37, 0.71) * _WaveScale, aniso);
                return a * w0 + b * w1;
            }

            float Foamy(float2 uv, float2 flow)
            {
                float t = _Time.y * _FlowCycle;
                float p0 = frac(t), p1 = frac(t + 0.5);
                float w0 = 1.0 - abs(1.0 - 2.0 * p0), w1 = 1.0 - abs(1.0 - 2.0 * p1);
                float2 step_ = flow / _FlowCycle;
                float2 q0 = (uv - step_ * p0) / _FoamScale, q1 = (uv - step_ * p1) / _FoamScale + 7.7;
                float a = NoiseD(q0).x + 0.5 * NoiseD(q0 * 2.7).x;
                float b = NoiseD(q1).x + 0.5 * NoiseD(q1 * 2.7).x;
                return 0.5 + 0.5 * (a * w0 + b * w1);                   // ~0..1
            }

            half3 LightPBR(InputData inputData, half3 albedo, half gloss)
            {
                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.specular = half3(0, 0, 0);
                s.metallic = 0;
                s.smoothness = gloss;
                s.normalTS = half3(0, 0, 1);
                s.occlusion = 1;
                s.alpha = 1;
                return UniversalFragmentPBR(inputData, s).rgb;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 P = i.positionWS;
                float3 n0 = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(P);
                float upness = abs(n0.y);
                float slope = 1.0 - upness;                             // 0 flat .. 1 vertical

                // ---------------- flow direction: baked on flat parts, straight downhill on slopes
                float3 downhill = float3(0, -1, 0) + n0 * n0.y;
                float dl = length(downhill);
                downhill = dl > 1e-3 ? downhill / dl : float3(0, 0, 0);
                float3 baked = i.flowWS.xyz - n0 * dot(i.flowWS.xyz, n0);
                float bl = length(baked);
                baked = bl > 0.2 ? baked / bl : float3(0, 0, 0);
                float onSlope = smoothstep(0.08, 0.22, slope);
                float3 dir = lerp(baked, downhill, onSlope);
                float speed = lerp(_FlowSpeed, _FallSpeed, smoothstep(0.08, 0.6, slope));
                float3 flowW = dir * speed;

                // ---------------- surface frames: flat (world XZ) and steep (horizontal x vertical in the face)
                float steep = smoothstep(0.45, 0.8, slope);
                float3 gradW = float3(0, 0, 0);
                float streak = 0.5;
                float foamN = 0.5;
                float3 Ta = float3(1, 0, 0), Tb = float3(0, 0, 1);
                float3 Sa = normalize(cross(float3(0, 1, 0), n0) + float3(1e-4, 0, 0));
                float3 Sb = cross(n0, Sa);
                if (steep < 0.999)
                {
                    float2 uv = P.xz, f2 = flowW.xz;
                    float3 w = FlowWaves(uv, f2, float2(1, 1));
                    // still water: slow crossing ripples so a pond is never glassy
                    float3 r = Waves(uv * 1.7 + _Time.y * float2(0.05, 0.03), float2(1, 1)) * (1.0 - saturate(length(f2)));
                    float3 g = w * (1.0 + 0.6 * saturate(length(f2) * 0.5)) + r * _Ripple;
                    gradW += (1.0 - steep) * (g.y * Ta + g.z * Tb);
                    foamN = lerp(foamN, Foamy(uv, f2), 1.0 - steep);
                }
                if (steep > 0.001)
                {
                    float2 uv = float2(dot(P, Sa), dot(P, Sb)), f2 = float2(dot(flowW, Sa), dot(flowW, Sb));
                    float3 w = FlowWaves(uv, f2, float2(2.2, 0.35));     // long vertical streaks
                    gradW += steep * (w.y * Sa + w.z * Sb);
                    streak = 0.5 + 0.5 * FlowWaves(uv * float2(1.0, 0.5), f2 * float2(1.0, 0.5), float2(3.0, 0.25)).x;
                    foamN = lerp(foamN, Foamy(uv * float2(1.0, 0.4), f2 * float2(1.0, 0.4)), steep);
                }
                float3 nW = normalize(n0 - gradW * _WaveStrength * 0.35);

                // ---------------- depth behind the surface + refraction
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                float surfEye = LinearEyeDepth(i.positionCS.z, _ZBufferParams);
                float sceneEye = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float thick = max(0.0, sceneEye - surfEye);
                float2 off = (nW.xz - n0.xz) * _Refraction * saturate(thick * 1.5) / max(1.0, surfEye * 0.08);
                float2 ruv = suv + off;
                float rEye = LinearEyeDepth(SampleSceneDepth(ruv), _ZBufferParams);
                if (rEye < surfEye) { ruv = suv; rEye = sceneEye; }
                float rThick = max(0.0, rEye - surfEye);
                half3 behind = SampleSceneColor(ruv);

                // ---------------- lighting: one PBR call with white albedo, one with black -> diffuse light + specular
                InputData inputData = (InputData)0;
                inputData.positionWS = P;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = nW;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(P);
                inputData.fogCoord = i.fogCoord;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(nW);
                inputData.normalizedScreenSpaceUV = suv;
                inputData.shadowMask = half4(1, 1, 1, 1);

                float t = _Time.y;
                // neglect: drifting scum on slow water
                float2 su = P.xz - flowW.xz * t * 0.15;
                float scumN = 0.5 + 0.5 * (NoiseD(su * 0.35).x + 0.5 * NoiseD(su * 0.9 + 3.1).x);
                half scum = smoothstep(0.62, 0.75, scumN) * _Scum * (1.0 - onSlope);

                // foam: shoreline (depth), baked impact zones, white water on falls
                half edge = _EdgeFoam > 0.001 ? 1.0 - saturate(thick / _EdgeFoam) : 0;
                half impact = saturate(i.flowWS.w * _ImpactFoam);
                half fall = steep * _FallWhite * saturate(streak * 1.3);
                half foamAmt = saturate(edge * 0.9 + impact + fall);
                half foam = saturate((foamAmt - (1.0 - foamN)) * 3.0 + fall * 0.35);

                half gloss = lerp(_Smoothness, 0.35, saturate(foam + scum));
                half3 spec = LightPBR(inputData, half3(0, 0, 0), gloss);
                half3 litW = LightPBR(inputData, half3(1, 1, 1), gloss);
                half3 diffL = max(litW - spec, 0);

                half NdotV = saturate(dot(nW, V));
                half F = 0.02 + 0.98 * pow(1.0 - NdotV, 5.0);
                float3 absorb = exp(-rThick * _Absorption.xyz * _Turbidity * 2.0);
                half3 through = behind * _Tint.rgb * absorb;
                half3 scatter = diffL * _DeepColor.rgb * (1.0 - absorb);
                half3 water = spec + (1.0 - F) * (through + scatter);
                water = lerp(water, diffL * _ScumColor.rgb, scum);
                half3 col = lerp(water, diffL * _FoamColor.rgb + spec * 0.15, foam);

                col = MixFog(col, i.fogCoord);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
