// Soft contact shadow under a 2.5D character (URP 17). Written for this project.
// A radial falloff computed in the shader (no texture), drawn flat on the ground just before the character sprite.
Shader "Echoes/ContactShadow"
{
    Properties
    {
        _Color ("Shadow colour (a = darkness)", Color) = (0.08, 0.07, 0.06, 0.45)
        _Softness ("Edge softness", Range(0.05, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "ContactShadow"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Softness;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv * 2 - 1;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half r = length(i.uv);
                half a = _Color.a * (1 - smoothstep(1 - _Softness, 1, r));
                half3 c = MixFog(_Color.rgb, i.fog);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
