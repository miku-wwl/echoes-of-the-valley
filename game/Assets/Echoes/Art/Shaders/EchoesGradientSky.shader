// A soft three-colour sky for the valley: zenith, horizon and ground colours, a sun halo and a sun disc.
// ValleyLighting moves all of them with the restoration stages (muted -> golden hour), so the colours are art-directed
// instead of coming from atmospheric scattering. Original shader written for this project.
Shader "Echoes/GradientSky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.45, 0.60, 0.85, 1)
        _HorizonColor ("Horizon", Color) = (0.85, 0.90, 0.95, 1)
        _GroundColor ("Ground", Color) = (0.45, 0.50, 0.42, 1)
        _SunColor ("Sun", Color) = (1, 0.90, 0.65, 1)
        _SunDir ("Sun direction (towards the sun)", Vector) = (0.3, 0.6, 0.7, 0)
        _SunSize ("Sun size", Range(0.9, 0.9999)) = 0.9985
        _Exposure ("Exposure", Range(0, 3)) = 1
        _Falloff ("Horizon falloff", Range(0.2, 3)) = 0.7
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _ZenithColor;
            half4 _HorizonColor;
            half4 _GroundColor;
            half4 _SunColor;
            float4 _SunDir;
            half _SunSize;
            half _Exposure;
            half _Falloff;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = d.y;

                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(saturate(up), _Falloff));
                half3 ground = lerp(_HorizonColor.rgb, _GroundColor.rgb, saturate(-up * 5.0));
                half3 col = up >= 0.0 ? sky : ground;

                float3 sunDir = normalize(_SunDir.xyz);
                float s = saturate(dot(d, sunDir));
                float halo = pow(s, 6.0) * 0.30 + pow(s, 48.0) * 0.45;
                float disc = smoothstep(_SunSize, _SunSize + 0.0010, s);
                col += _SunColor.rgb * (halo + disc * 2.5);

                return half4(col * _Exposure, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
