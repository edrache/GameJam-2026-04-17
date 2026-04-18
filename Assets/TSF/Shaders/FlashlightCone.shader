Shader "TSF/FlashlightCone"
{
    Properties
    {
        [HDR] _Color ("Beam Color", Color) = (1, 0.95, 0.75, 0.12)
        _TipFade ("Tip Fade Length", Range(0.01, 0.5)) = 0.08
        _BaseFadePow ("Base Fade Power", Range(0.5, 4)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+10"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _TipFade;
                float _BaseFadePow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv        : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // uv.y : 0 = tip/disc-centre, 1 = base-perimeter
                float v        = IN.uv.y;
                float tipFade  = smoothstep(0.0, _TipFade, v);
                // Cone sides fade toward the base; disc centre fades to 0 as well.
                float baseFade = pow(v, 0.4) * (1.0 - pow(v, _BaseFadePow));
                float alpha    = tipFade * baseFade * _Color.a;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
