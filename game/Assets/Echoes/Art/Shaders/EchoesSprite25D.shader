// Original 2.5D character sprite shader for Echoes of the Valley (URP 17). Written for this project.
// The quad is built in view space around the object's origin (the character's ground point), so the illustrated frame always faces
// the camera at its true size, whatever the camera pitch. Every fragment takes the depth of that ground point pulled a little toward the
// camera: anything in front of the character hides her, nothing behind does, and the ground around the feet never cuts the boots.
// Colour: the painted frame is lit by the scene's ambient and sun, darkened under real shadows (tree canopies), and fogged like the
// world. A second pass draws a faint silhouette where buildings or trees hide her, so the player is never lost behind scenery.
Shader "Echoes/Sprite25D"
{
    Properties
    {
        [MainTexture] _MainTex ("Atlas (straight alpha)", 2D) = "white" {}
        [MainColor] _Tint ("Tint", Color) = (1,1,1,1)
        _DepthPull ("Depth pull toward the camera (m)", Float) = 0.45
        _AmbientWeight ("Ambient weight", Float) = 0.62
        _SunWeight ("Sun weight", Float) = 0.78
        _ShadowFloor ("Brightness of sun light inside shadows", Range(0,1)) = 0.35
        _Exposure ("Exposure", Float) = 1.0
        _OccludedTint ("Hidden silhouette (rgb, a = opacity)", Color) = (0.62,0.80,0.84,0.30)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Tint;
            float _DepthPull;
            half _AmbientWeight;
            half _SunWeight;
            half _ShadowFloor;
            half _Exposure;
            half4 _OccludedTint;
        CBUFFER_END
        // per renderer (MaterialPropertyBlock): the current frame
        float4 _FrameUV;    // atlas rect: x, y, width, height (UV)
        float4 _FrameRect;  // quad in metres around the ground point: left, bottom, width, height (view-space x/y)
        float _Flip;        // 1 = mirror the frame horizontally (E drawn from W)

        struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float fog : TEXCOORD2;
        };

        Varyings VertPulled(Attributes input, float pull)
        {
            Varyings o;
            float3 pivotVS = TransformWorldToView(TransformObjectToWorld(float3(0, 0, 0)));
            float2 corner = _FrameRect.xy + input.uv * _FrameRect.zw;
            float3 positionVS = pivotVS + float3(corner, 0);
            float4 positionCS = TransformWViewToHClip(positionVS);
            float3 pulledVS = float3(positionVS.xy, pivotVS.z + pull);   // view space looks down -z: + moves toward the camera
            float4 pulledCS = TransformWViewToHClip(pulledVS);
            positionCS.z = pulledCS.z / pulledCS.w * positionCS.w;
            o.positionCS = positionCS;
            float u = lerp(input.uv.x, 1.0 - input.uv.x, _Flip);
            o.uv = _FrameUV.xy + float2(u, input.uv.y) * _FrameUV.zw;
            o.positionWS = mul(UNITY_MATRIX_I_V, float4(positionVS, 1)).xyz;
            o.fog = ComputeFogFactor(positionCS.z);
            return o;
        }

        Varyings Vert(Attributes input) { return VertPulled(input, _DepthPull); }
        // only scenery at least two metres in front of the character draws the hidden silhouette (grass at her feet does not)
        Varyings VertHidden(Attributes input) { return VertPulled(input, _DepthPull + 2.0); }
        ENDHLSL

        Pass
        {
            Name "Sprite25D"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            half4 Frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(tex.a - 0.003);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 ambient = SampleSH(half3(0, 1, 0));
                half3 light = ambient * _AmbientWeight + sun.color * (_SunWeight * lerp(_ShadowFloor, 1.0h, sun.shadowAttenuation));
                half3 colour = tex.rgb * _Tint.rgb * light * _Exposure;
                colour = MixFog(colour, i.fog);
                return half4(colour, tex.a * _Tint.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HiddenSilhouette"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VertHidden
            #pragma fragment FragHidden

            half4 FragHidden(Varyings i) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                clip(a - 0.5);
                return half4(_OccludedTint.rgb, _OccludedTint.a);
            }
            ENDHLSL
        }
    }
}
