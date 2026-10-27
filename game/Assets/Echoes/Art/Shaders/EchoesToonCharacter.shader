// Original stylised character shader for Echoes of the Valley (URP 17).
// Painted albedo + soft two-tone cel shading that follows the main light and shadow, baked vertex AO (alpha), a gentle rim,
// an optional toon highlight and an inverted-hull outline of constant pixel width. Written for this project; no third-party shader code.
Shader "Echoes/ToonCharacter"
{
    Properties
    {
        [MainTexture] _BaseMap ("Painted Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        [NoScaleOffset] _BlinkMap ("Blink Albedo (optional)", 2D) = "black" {}
        _Blink ("Blink", Range(0,1)) = 0
        _ShadeColor ("Shade Tint", Color) = (0.68,0.60,0.78,1)
        _ShadeThreshold ("Shade Threshold", Range(0,1)) = 0.46
        _ShadeSoftness ("Shade Softness", Range(0.001,0.5)) = 0.10
        _AoStrength ("Vertex AO Strength", Range(0,1)) = 0.85
        _VertexTint ("Vertex Tint Strength", Range(0,1)) = 1
        _Ambient ("Ambient Boost", Range(0,2)) = 1.15
        _LightGain ("Light Gain", Range(0,2)) = 0.74
        _CameraFill ("Camera Fill", Range(0,1)) = 0.22
        _RimColor ("Rim Colour", Color) = (1.0,0.94,0.84,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.0
        _RimStrength ("Rim Strength", Range(0,1.5)) = 0.30
        _HighlightColor ("Highlight Colour", Color) = (1,0.96,0.88,1)
        _HighlightPower ("Highlight Power", Range(4,256)) = 40
        _HighlightStrength ("Highlight Strength", Range(0,2)) = 0
        _OutlineColor ("Outline Colour", Color) = (0.13,0.075,0.06,1)
        _OutlineTint ("Outline Albedo Mix", Range(0,1)) = 0.45
        _OutlineWidth ("Outline Width (px at 1080p)", Range(0,6)) = 1.6
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BlinkMap); SAMPLER(sampler_BlinkMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Blink;
            half4 _ShadeColor;
            half _ShadeThreshold;
            half _ShadeSoftness;
            half _AoStrength;
            half _VertexTint;
            half _Ambient;
            half _LightGain;
            half _CameraFill;
            half4 _RimColor;
            half _RimPower;
            half _RimStrength;
            half4 _HighlightColor;
            half _HighlightPower;
            half _HighlightStrength;
            half4 _OutlineColor;
            half _OutlineTint;
            half _OutlineWidth;
            half _Cutoff;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 color : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            half Band(half value)
            {
                return smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, value);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                if (_Blink > 0.001h) tex = lerp(tex, SAMPLE_TEXTURE2D(_BlinkMap, sampler_BlinkMap, IN.uv), _Blink);
                #if defined(_ALPHATEST_ON)
                    clip(tex.a - _Cutoff);
                #endif
                half3 albedo = tex.rgb * _BaseColor.rgb * lerp(half3(1, 1, 1), IN.color.rgb, _VertexTint);
                half ao = lerp(1.0h, IN.color.a, _AoStrength);
                half3 N = normalize(IN.normalWS);
                half3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndl = dot(N, mainLight.direction);
                half lit = saturate(ndl * 0.5h + 0.5h) * mainLight.shadowAttenuation * ao;
                half band = Band(lit);
                half3 ambient = SampleSH(N) * _Ambient + _CameraFill * saturate(dot(N, V)) * (mainLight.color * 0.55h + 0.35h);
                half3 shaded = albedo * _ShadeColor.rgb * ambient * ao;
                half3 sunlit = albedo * (ambient * 0.58h * ao + mainLight.color * _LightGain);
                half3 col = lerp(shaded, sunlit, band);

                half rim = pow(saturate(1.0h - dot(N, V)), _RimPower) * saturate(ndl + 0.4h) * _RimStrength;
                col += rim * _RimColor.rgb * mainLight.color;
                if (_HighlightStrength > 0.001h)
                {
                    half3 H = normalize(mainLight.direction + V);
                    half s = pow(saturate(dot(N, H)), _HighlightPower);
                    col += smoothstep(0.42h, 0.58h, s) * _HighlightStrength * _HighlightColor.rgb * mainLight.color * band;
                }
                #if defined(_ADDITIONAL_LIGHTS)
                    uint extra = GetAdditionalLightsCount();
                    for (uint li = 0u; li < extra; ++li)
                    {
                        Light l = GetAdditionalLight(li, IN.positionWS);
                        half a = saturate(dot(N, l.direction) * 0.5h + 0.5h);
                        col += albedo * l.color * (l.distanceAttenuation * l.shadowAttenuation * Band(a) * 0.55h);
                    }
                #endif
                col = MixFog(col, IN.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // Inverted hull outline: back faces pushed along the normal by a constant number of pixels.
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct OAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 outlineScale : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct OVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            OVaryings OutlineVert(OAttributes IN)
            {
                OVaryings OUT = (OVaryings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformWorldToHClip(positionWS);
                float3 normalVS = mul((float3x3)UNITY_MATRIX_V, normalWS);
                float2 dirPx = normalize(mul((float3x3)UNITY_MATRIX_P, normalVS).xy * _ScreenParams.xy + 1e-5);
                float widthPx = _OutlineWidth * IN.outlineScale.x * (_ScreenParams.y / 1080.0);
                positionCS.xy += dirPx * (widthPx * 2.0 / _ScreenParams.xy) * positionCS.w;
                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(positionCS.z);
                return OUT;
            }

            half4 OutlineFrag(OVaryings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                #if defined(_ALPHATEST_ON)
                    clip(tex.a - _Cutoff);
                #endif
                half3 col = lerp(_OutlineColor.rgb, saturate(tex.rgb * _BaseColor.rgb * _OutlineColor.rgb * 2.4h), _OutlineTint);
                col = MixFog(col, IN.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct SAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct SVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            SVaryings ShadowVert(SAttributes IN)
            {
                SVaryings OUT = (SVaryings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 ShadowFrag(SVaryings IN) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
