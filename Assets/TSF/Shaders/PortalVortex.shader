Shader "TSF/PortalVortex"
{
    Properties
    {
        [HDR] _ColorCenter ("Center Color", Color) = (0.02, 0.05, 0.02, 1)
        [HDR] _ColorMid ("Mid Color", Color) = (0.1, 0.6, 0.15, 1)
        [HDR] _ColorEdge ("Edge Color", Color) = (0.5, 2.0, 0.5, 1)
        _TwirlStrength ("Twirl Strength", Float) = 3.0
        _TwirlOffset ("Twirl Offset", Float) = 0.0
        _ScrollSpeed ("Scroll Speed", Float) = 0.3
        _SparkleThreshold ("Sparkle Threshold", Range(0, 1)) = 0.15
        _GlowIntensity ("Glow Intensity", Float) = 2.0
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.05
        _OpenAmount ("Open Amount", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PortalVortex"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                half4 _ColorCenter;
                half4 _ColorMid;
                half4 _ColorEdge;
                float _TwirlStrength;
                float _TwirlOffset;
                float _ScrollSpeed;
                float _SparkleThreshold;
                float _GlowIntensity;
                float _EdgeSoftness;
                float _OpenAmount;
            CBUFFER_END

            static const float TAU = 6.28318530718;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
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

            float voronoiCell(float2 uv)
            {
                float2 grid = floor(uv);
                float2 local = frac(uv);
                float nearest = 8.0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 cell = float2(x, y);
                        float2 randomPoint = float2(
                            hash21(grid + cell),
                            hash21(grid + cell + 19.19)
                        );
                        randomPoint = 0.5 + 0.5 * sin(_Time.y * 0.9 + TAU * randomPoint);
                        nearest = min(nearest, length(cell + randomPoint - local));
                    }
                }

                return saturate(1.0 - nearest);
            }

            float2 twirlUv(float2 uv, float strength, float offset)
            {
                float2 centered = uv - 0.5;
                float radius = length(centered);
                float angle = atan2(centered.y, centered.x);
                angle += strength * (1.0 - saturate(radius * 2.0)) + offset * TAU;
                return 0.5 + float2(cos(angle), sin(angle)) * radius;
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
                float2 uv = input.uv;
                float2 centeredUv = uv - 0.5;
                float sourceRadius = length(centeredUv) * 2.0;

                float openAmount = saturate(_OpenAmount);
                float visibleRadius = max(openAmount, 0.001);
                float edgeSoftness = max(_EdgeSoftness, 0.001);
                float alpha = 1.0 - smoothstep(visibleRadius - edgeSoftness, visibleRadius, sourceRadius);
                alpha *= smoothstep(0.0, 0.02, openAmount);

                float2 twistedUv = twirlUv(uv, _TwirlStrength, _TwirlOffset);
                float2 twistedCentered = twistedUv - 0.5;
                float radius = saturate(length(twistedCentered) * 2.0 / visibleRadius);
                float angle01 = frac(atan2(twistedCentered.y, twistedCentered.x) / TAU + 0.5);

                float timeOffset = _Time.y * _ScrollSpeed;
                float2 polarUv = float2(angle01 * 3.0 + timeOffset, radius * 4.0 - timeOffset);
                float noiseValue = valueNoise(polarUv);
                noiseValue += valueNoise(polarUv * 2.0 + float2(4.7, -2.3)) * 0.5;
                noiseValue /= 1.5;

                half3 innerGradient = lerp(_ColorCenter.rgb, _ColorMid.rgb, smoothstep(0.0, 0.55, radius));
                half3 outerGradient = lerp(_ColorMid.rgb, _ColorEdge.rgb, smoothstep(0.35, 1.0, radius));
                half3 baseColor = lerp(innerGradient, outerGradient, smoothstep(0.45, 0.85, radius));
                baseColor *= lerp(0.75, 1.25, noiseValue);

                float sparkleField = voronoiCell(twistedUv * 20.0 + float2(timeOffset, -timeOffset * 0.5));
                float sparkleDots = step(1.0 - _SparkleThreshold, sparkleField);
                sparkleDots *= smoothstep(0.5, 0.65, radius);
                sparkleDots *= alpha;

                float edgeMask = smoothstep(0.55, 0.7, radius) * (1.0 - smoothstep(0.7, 0.85, radius));
                half3 glow = _ColorEdge.rgb * _GlowIntensity * edgeMask;
                half3 finalColor = baseColor + glow + half3(sparkleDots, sparkleDots, sparkleDots);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
