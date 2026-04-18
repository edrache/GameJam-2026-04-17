Shader "TSF/Layered Space Sky"
{
    Properties
    {
        [HDR] _BaseColor ("Base Space Color", Color) = (0.005, 0.007, 0.018, 1)
        _Exposure ("Exposure", Range(0, 4)) = 1.35
        _CameraParallaxStrength ("Sky Parallax Strength", Range(0, 2)) = 1.0
        _InvertParallaxX ("Invert Parallax X", Range(0, 1)) = 1.0
        _InvertParallaxZ ("Invert Parallax Z", Range(0, 1)) = 0.0
        _ProceduralStars ("Procedural Stars", Range(0, 2)) = 0.65
        _NebulaSoftness ("Nebula Softness", Range(0, 2)) = 0.45

        _Layer1Tex ("Layer 1 Texture", 2D) = "black" {}
        [HDR] _Layer1Tint ("Layer 1 Tint", Color) = (0.55, 0.78, 1.35, 1)
        _Layer1Opacity ("Layer 1 Opacity", Range(0, 2)) = 0.45
        _Layer1Distance ("Layer 1 Distance", Float) = 35
        _Layer1Scroll ("Layer 1 Scroll XY", Vector) = (0.005, 0.002, 0, 0)

        _Layer2Tex ("Layer 2 Texture", 2D) = "black" {}
        [HDR] _Layer2Tint ("Layer 2 Tint", Color) = (1.0, 0.65, 1.4, 1)
        _Layer2Opacity ("Layer 2 Opacity", Range(0, 2)) = 0.35
        _Layer2Distance ("Layer 2 Distance", Float) = 70
        _Layer2Scroll ("Layer 2 Scroll XY", Vector) = (-0.002, 0.004, 0, 0)

        _Layer3Tex ("Layer 3 Texture", 2D) = "black" {}
        [HDR] _Layer3Tint ("Layer 3 Tint", Color) = (0.55, 1.1, 0.95, 1)
        _Layer3Opacity ("Layer 3 Opacity", Range(0, 2)) = 0.3
        _Layer3Distance ("Layer 3 Distance", Float) = 140
        _Layer3Scroll ("Layer 3 Scroll XY", Vector) = (0.0015, -0.002, 0, 0)

        _Layer4Tex ("Layer 4 Texture", 2D) = "black" {}
        [HDR] _Layer4Tint ("Layer 4 Tint", Color) = (1.45, 1.1, 0.7, 1)
        _Layer4Opacity ("Layer 4 Opacity", Range(0, 2)) = 0.25
        _Layer4Distance ("Layer 4 Distance", Float) = 280
        _Layer4Scroll ("Layer 4 Scroll XY", Vector) = (-0.001, -0.001, 0, 0)

        _Layer5Tex ("Layer 5 Texture", 2D) = "black" {}
        [HDR] _Layer5Tint ("Layer 5 Tint", Color) = (0.75, 0.85, 1.65, 1)
        _Layer5Opacity ("Layer 5 Opacity", Range(0, 2)) = 0.22
        _Layer5Distance ("Layer 5 Distance", Float) = 560
        _Layer5Scroll ("Layer 5 Scroll XY", Vector) = (0.0005, 0.001, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "LayeredSpaceSky"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Layer1Tex);
            SAMPLER(sampler_Layer1Tex);
            TEXTURE2D(_Layer2Tex);
            SAMPLER(sampler_Layer2Tex);
            TEXTURE2D(_Layer3Tex);
            SAMPLER(sampler_Layer3Tex);
            TEXTURE2D(_Layer4Tex);
            SAMPLER(sampler_Layer4Tex);
            TEXTURE2D(_Layer5Tex);
            SAMPLER(sampler_Layer5Tex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Exposure;
                float _CameraParallaxStrength;
                float _InvertParallaxX;
                float _InvertParallaxZ;
                float _ProceduralStars;
                float _NebulaSoftness;

                float4 _Layer1Tex_ST;
                half4 _Layer1Tint;
                float _Layer1Opacity;
                float _Layer1Distance;
                float4 _Layer1Scroll;

                float4 _Layer2Tex_ST;
                half4 _Layer2Tint;
                float _Layer2Opacity;
                float _Layer2Distance;
                float4 _Layer2Scroll;

                float4 _Layer3Tex_ST;
                half4 _Layer3Tint;
                float _Layer3Opacity;
                float _Layer3Distance;
                float4 _Layer3Scroll;

                float4 _Layer4Tex_ST;
                half4 _Layer4Tint;
                float _Layer4Opacity;
                float _Layer4Distance;
                float4 _Layer4Scroll;

                float4 _Layer5Tex_ST;
                half4 _Layer5Tint;
                float _Layer5Opacity;
                float _Layer5Distance;
                float4 _Layer5Scroll;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float starField(float2 uv, float density, float threshold)
            {
                float2 cell = floor(uv * density);
                float2 local = frac(uv * density) - 0.5;
                float star = step(threshold, hash21(cell));
                float shape = smoothstep(0.045, 0.0, length(local));
                return star * shape;
            }

            float nebulaField(float2 uv, float scale, float seed)
            {
                float cloud =
                    valueNoise(uv * scale + seed) * 0.55 +
                    valueNoise(uv * scale * 2.17 + seed * 1.73) * 0.3 +
                    valueNoise(uv * scale * 4.31 - seed * 0.61) * 0.15;
                return smoothstep(0.36, 0.92, cloud);
            }

            float2 layerUv(float2 uv, float4 textureST, float distance, float4 scroll)
            {
                float safeDistance = max(abs(distance), 0.001);
                float2 parallaxSign = lerp(float2(1.0, 1.0), float2(-1.0, -1.0), round(float2(_InvertParallaxX, _InvertParallaxZ)));
                float2 cameraOffset = _WorldSpaceCameraPos.xz * parallaxSign / safeDistance;
                float2 animatedOffset = scroll.xy * _Time.y;
                return uv * textureST.xy + textureST.zw + cameraOffset * _CameraParallaxStrength + animatedOffset;
            }

            half3 sampleSpaceLayer(TEXTURE2D_PARAM(layerTex, sampler_layerTex), float2 uv, half4 tint, float opacity)
            {
                half4 tex = SAMPLE_TEXTURE2D(layerTex, sampler_layerTex, uv);
                return tex.rgb * tint.rgb * opacity;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv1 = layerUv(input.uv, _Layer1Tex_ST, _Layer1Distance, _Layer1Scroll);
                float2 uv2 = layerUv(input.uv, _Layer2Tex_ST, _Layer2Distance, _Layer2Scroll);
                float2 uv3 = layerUv(input.uv, _Layer3Tex_ST, _Layer3Distance, _Layer3Scroll);
                float2 uv4 = layerUv(input.uv, _Layer4Tex_ST, _Layer4Distance, _Layer4Scroll);
                float2 uv5 = layerUv(input.uv, _Layer5Tex_ST, _Layer5Distance, _Layer5Scroll);

                half3 color = _BaseColor.rgb;
                color += sampleSpaceLayer(TEXTURE2D_ARGS(_Layer5Tex, sampler_Layer5Tex), uv5, _Layer5Tint, _Layer5Opacity);
                color += sampleSpaceLayer(TEXTURE2D_ARGS(_Layer4Tex, sampler_Layer4Tex), uv4, _Layer4Tint, _Layer4Opacity);
                color += sampleSpaceLayer(TEXTURE2D_ARGS(_Layer3Tex, sampler_Layer3Tex), uv3, _Layer3Tint, _Layer3Opacity);
                color += sampleSpaceLayer(TEXTURE2D_ARGS(_Layer2Tex, sampler_Layer2Tex), uv2, _Layer2Tint, _Layer2Opacity);
                color += sampleSpaceLayer(TEXTURE2D_ARGS(_Layer1Tex, sampler_Layer1Tex), uv1, _Layer1Tint, _Layer1Opacity);

                float nebula5 = nebulaField(uv5, 2.15, 11.3) * 0.5;
                float nebula4 = nebulaField(uv4, 2.8, 24.7) * 0.42;
                float nebula3 = nebulaField(uv3, 3.55, 39.1) * 0.34;
                float nebula2 = nebulaField(uv2, 4.45, 51.6) * 0.26;
                float nebula1 = nebulaField(uv1, 5.75, 67.2) * 0.18;
                half3 layeredNebula =
                    nebula5 * half3(0.08, 0.11, 0.28) +
                    nebula4 * half3(0.16, 0.08, 0.25) +
                    nebula3 * half3(0.08, 0.20, 0.18) +
                    nebula2 * half3(0.22, 0.13, 0.08) +
                    nebula1 * half3(0.12, 0.17, 0.24);
                color += layeredNebula * _NebulaSoftness;

                float stars =
                    starField(uv1, 95.0, 0.992) * 1.4 +
                    starField(uv2, 70.0, 0.989) * 0.9 +
                    starField(uv3, 45.0, 0.985) * 0.55 +
                    starField(uv5, 28.0, 0.98) * 0.35;
                color += stars * _ProceduralStars;

                color *= _Exposure;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
