Shader "TSF/PortalVortex"
{
    Properties
    {
        [HDR] _ColorCenter ("Center Color", Color) = (0.02, 0.05, 0.02, 1)
        [HDR] _ColorMid ("Mid Color", Color) = (0.1, 0.6, 0.15, 1)
        [HDR] _ColorEdge ("Edge Color", Color) = (0.5, 2.0, 0.5, 1)
        [HDR] _BackColorCenter ("Back Center Color", Color) = (0.02, 0.03, 0.05, 1)
        [HDR] _BackColorMid ("Back Mid Color", Color) = (0.15, 0.25, 0.8, 1)
        [HDR] _BackColorEdge ("Back Edge Color", Color) = (0.45, 0.75, 2.0, 1)
        _TwirlStrength ("Twirl Strength", Float) = 3.0
        _TwirlOffset ("Twirl Offset", Float) = 0.0
        _ScrollSpeed ("Scroll Speed", Float) = 0.3
        _SparkleThreshold ("Sparkle Threshold", Range(0, 1)) = 0.15
        _GlowIntensity ("Glow Intensity", Float) = 2.0
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.05
        _OpenAmount ("Open Amount", Range(0, 1)) = 1.0
        [HideInInspector] _PortalPermanentReveal ("Portal Permanent Reveal", Float) = 0.0
        _DistortionNoise ("Distortion Noise", 2D) = "gray" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.2)) = 0.035
        _DistortionScale ("Distortion Scale", Float) = 2.0
        _DistortionSpeedX ("Distortion Speed X", Float) = 0.08
        _DistortionSpeedY ("Distortion Speed Y", Float) = 0.05
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0, 0.3)) = 0.08
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

            TEXTURE2D(_DistortionNoise);
            SAMPLER(sampler_DistortionNoise);

            float4 _FlashlightWorldPos;
            float4 _FlashlightWorldDir;
            float _FlashlightCosHalfAngle;
            float _FlashlightRange;
            float _FlashlightEditorReveal;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorCenter;
                half4 _ColorMid;
                half4 _ColorEdge;
                half4 _BackColorCenter;
                half4 _BackColorMid;
                half4 _BackColorEdge;
                float _TwirlStrength;
                float _TwirlOffset;
                float _ScrollSpeed;
                float _SparkleThreshold;
                float _GlowIntensity;
                float _EdgeSoftness;
                float _OpenAmount;
                float _PortalPermanentReveal;
                float4 _DistortionNoise_ST;
                float _DistortionStrength;
                float _DistortionScale;
                float _DistortionSpeedX;
                float _DistortionSpeedY;
                float _EdgeNoiseStrength;
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

            float2 sampleDistortion(float2 uv, float time)
            {
                float2 noiseUv = uv * _DistortionNoise_ST.xy * _DistortionScale + _DistortionNoise_ST.zw;
                float2 speed = float2(_DistortionSpeedX, _DistortionSpeedY);
                float2 pan = speed * time;
                float2 noiseA = SAMPLE_TEXTURE2D(_DistortionNoise, sampler_DistortionNoise, noiseUv + pan).rg;
                float2 noiseB = SAMPLE_TEXTURE2D(_DistortionNoise, sampler_DistortionNoise, noiseUv * 1.73 - pan.yx * 1.37).rg;
                return noiseA + noiseB - 1.0;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                bool isFrontFace = IS_FRONT_VFACE(frontFace, true, false);
                float2 uv = input.uv;
                float2 centeredUv = uv - 0.5;
                float sourceRadius = length(centeredUv) * 2.0;

                float openAmount = saturate(_OpenAmount);
                float visibleRadius = max(openAmount, 0.001);
                float edgeSoftness = max(_EdgeSoftness, 0.001);

                float edgeNoise = dot(sampleDistortion(uv, _Time.y * 0.75), float2(0.5, 0.5));
                float edgeNoiseMask = smoothstep(0.25, 0.9, sourceRadius);
                float shapeRadius = sourceRadius - edgeNoise * _EdgeNoiseStrength * edgeNoiseMask;
                float noisyVisibleRadius = clamp(visibleRadius, 0.001, 1.2);

                float alpha = 1.0 - smoothstep(noisyVisibleRadius - edgeSoftness, noisyVisibleRadius, shapeRadius);
                alpha *= smoothstep(0.0, 0.02, openAmount);

                float distortionMask = smoothstep(0.05, 0.4, shapeRadius) * (1.0 - smoothstep(0.9, 1.0, shapeRadius));
                distortionMask *= alpha;
                float2 distortion = sampleDistortion(uv, _Time.y) * _DistortionStrength * distortionMask;
                float2 distortedUv = uv + distortion;

                float2 twistedUv = twirlUv(distortedUv, _TwirlStrength, _TwirlOffset);
                float2 twistedCentered = twistedUv - 0.5;
                float radius = saturate(length(twistedCentered) * 2.0 / visibleRadius);
                float angle01 = frac(atan2(twistedCentered.y, twistedCentered.x) / TAU + 0.5);

                float timeOffset = _Time.y * _ScrollSpeed;
                float2 polarUv = float2(angle01 * 3.0 + timeOffset, radius * 4.0 - timeOffset);
                float noiseValue = valueNoise(polarUv);
                noiseValue += valueNoise(polarUv * 2.0 + float2(4.7, -2.3)) * 0.5;
                noiseValue /= 1.5;

                half3 colorCenter = isFrontFace ? _ColorCenter.rgb : _BackColorCenter.rgb;
                half3 colorMid = isFrontFace ? _ColorMid.rgb : _BackColorMid.rgb;
                half3 colorEdge = isFrontFace ? _ColorEdge.rgb : _BackColorEdge.rgb;

                half3 innerGradient = lerp(colorCenter, colorMid, smoothstep(0.0, 0.55, radius));
                half3 outerGradient = lerp(colorMid, colorEdge, smoothstep(0.35, 1.0, radius));
                half3 baseColor = lerp(innerGradient, outerGradient, smoothstep(0.45, 0.85, radius));
                baseColor *= lerp(0.75, 1.25, noiseValue);

                float sparkleField = voronoiCell(twistedUv * 20.0 + float2(timeOffset, -timeOffset * 0.5));
                float sparkleDots = step(1.0 - _SparkleThreshold, sparkleField);
                sparkleDots *= smoothstep(0.5, 0.65, radius);
                sparkleDots *= alpha;

                float edgeMask = smoothstep(0.55, 0.7, radius) * (1.0 - smoothstep(0.7, 0.85, radius));
                half3 glow = colorEdge * _GlowIntensity * edgeMask;
                half3 finalColor = baseColor + glow + half3(sparkleDots, sparkleDots, sparkleDots);

                float3 flashlightToFrag = input.positionWS - _FlashlightWorldPos.xyz;
                float distanceToFrag = length(flashlightToFrag);
                float3 toFrag = flashlightToFrag / max(distanceToFrag, 0.0001);
                float cosAngle = dot(toFrag, normalize(_FlashlightWorldDir.xyz));
                float flashlightEdgeSoftness = 0.05;
                float reveal = smoothstep(_FlashlightCosHalfAngle - flashlightEdgeSoftness, _FlashlightCosHalfAngle, cosAngle);
                float rangeSoftness = max(_FlashlightRange * 0.1, 0.25);
                reveal *= 1.0 - smoothstep(_FlashlightRange - rangeSoftness, _FlashlightRange, distanceToFrag);
                reveal *= step(0.0, _FlashlightCosHalfAngle);
                reveal = max(reveal, saturate(_PortalPermanentReveal));
                reveal = max(reveal, saturate(_FlashlightEditorReveal));

                return half4(finalColor, alpha * reveal);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
