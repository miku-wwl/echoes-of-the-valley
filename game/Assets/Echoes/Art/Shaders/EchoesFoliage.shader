// Original stylised foliage shader for Echoes of the Valley (URP 17).
// Alpha-cutout leaf clusters with painted albedo, wrap lighting plus back-light translucency, vertex-colour volume shading
// and a camera-aware dithered fade: leaves close to the camera dissolve, and leaves that sit between the camera and the
// player (the global _EchoesFocus point) open a cone so the hero stays readable under the canopy. Written for this project.
Shader "Echoes/Foliage"
{
    Properties
    {
        [MainTexture] _BaseMap ("Leaf Albedo (alpha = shape)", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.45
        _AoStrength ("Vertex AO Strength", Range(0,1)) = 0.9
        _Ambient ("Ambient Boost", Range(0,2)) = 1.1
        _LightGain ("Light Gain", Range(0,2)) = 0.95
        _Translucency ("Back-light Glow", Range(0,2)) = 0.6
        _ShadeColor ("Shade Tint", Color) = (0.72,0.80,0.74,1)
        _Saturation ("Saturation", Range(0,2)) = 1.12
        _NearFadeStart ("Near Fade Start (m)", Float) = 0.8
        _NearFadeEnd ("Near Fade End (m)", Float) = 3.6
        _OccluderInnerTan ("Sight Cone Inner (tan)", Float) = 0.17
        _OccluderOuterTan ("Sight Cone Outer (tan)", Float) = 0.36
        _OccluderStrength ("Sight Cone Strength", Range(0,1)) = 1
        _WindStrength ("Wind Sway (m)", Range(0,0.2)) = 0.035
        _WindSpeed ("Wind Speed", Range(0,4)) = 1.4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        float4 _EchoesFocus;                         // xyz = point to keep visible (the player), w = 1 when active
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _AoStrength;
            half _Ambient;
            half _LightGain;
            half _Translucency;
            half4 _ShadeColor;
            half _Saturation;
            float _NearFadeStart;
            float _NearFadeEnd;
            float _OccluderInnerTan;
            float _OccluderOuterTan;
            half _OccluderStrength;
            half _WindStrength;
            half _WindSpeed;
        CBUFFER_END

        float3 Sway(float3 positionOS, float weight)
        {
            float3 w = TransformObjectToWorld(positionOS);
            float t = _Time.y * _WindSpeed;
            float s = sin(t + w.x * 0.37 + w.z * 0.29) + 0.5 * sin(t * 1.9 + w.x * 0.9 + w.z * 0.7);
            return positionOS + float3(s, 0.25 * s, 0.7 * s) * (_WindStrength * weight);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            AlphaToMask On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 sway : TEXCOORD1;
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
                float3 pos = Sway(IN.positionOS.xyz, IN.sway.x);
                VertexPositionInputs p = GetVertexPositionInputs(pos);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            // Interleaved gradient noise: a stable screen-space dither threshold in [0,1).
            float Dither(float2 pixel)
            {
                return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
            }

            half Visibility(float3 positionWS)
            {
                float3 camera = _WorldSpaceCameraPos;
                float fragDistance = distance(positionWS, camera);
                half nearVisible = saturate((fragDistance - _NearFadeStart) / max(_NearFadeEnd - _NearFadeStart, 0.01));
                half visible = nearVisible;
                if (_EchoesFocus.w > 0.5)
                {
                    float3 toFocus = _EchoesFocus.xyz - camera;
                    float focusDistance = length(toFocus);
                    float3 dir = toFocus / max(focusDistance, 0.01);
                    float3 toFrag = positionWS - camera;
                    float along = dot(toFrag, dir);
                    float lateral = length(toFrag - dir * along) / max(along, 0.6);
                    half inFront = (along > 0.2 && along < focusDistance + 0.4) ? 1.0h : 0.0h;
                    half cone = smoothstep(_OccluderInnerTan, _OccluderOuterTan, lateral);
                    visible = min(visible, lerp(1.0h, cone, inFront * _OccluderStrength));
                }
                return visible;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half visible = Visibility(IN.positionWS);
                #if defined(_ALPHATEST_ON)
                    clip(tex.a - _Cutoff);
                #endif
                clip(visible - Dither(IN.positionCS.xy) * 0.999h);

                half3 albedo = tex.rgb * _BaseColor.rgb * IN.color.rgb;
                half grey = dot(albedo, half3(0.3h, 0.59h, 0.11h));
                albedo = lerp(grey.xxx, albedo, _Saturation);
                half ao = lerp(1.0h, IN.color.a, _AoStrength);
                half3 N = normalize(IN.normalWS);
                half3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half wrap = saturate(dot(N, mainLight.direction) * 0.5h + 0.5h);
                half lit = smoothstep(0.30h, 0.72h, wrap) * mainLight.shadowAttenuation;
                half3 ambient = SampleSH(N) * _Ambient;
                half glow = pow(saturate(dot(-mainLight.direction, V)), 3.0h) * _Translucency * mainLight.shadowAttenuation * saturate(1.0h - wrap * 0.4h);
                half3 shaded = albedo * _ShadeColor.rgb * ambient;
                half3 sunlit = albedo * (ambient * 0.75h + mainLight.color * _LightGain);
                half3 col = lerp(shaded, sunlit, lit) * ao;
                col += albedo * mainLight.color * glow * half3(0.9h, 1.0h, 0.45h);
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
            Cull Off

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
                float2 sway : TEXCOORD1;
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
                float3 positionWS = TransformObjectToWorld(Sway(IN.positionOS.xyz, IN.sway.x));
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
