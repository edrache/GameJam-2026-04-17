Shader "TSF/PortalClippedBase1"
{
    Properties
    {
        [MaterialToggle(_TEX_ON)] _DetailTex ("Enable Detail texture", Float) = 0
        _MainTex ("Detail", 2D) = "white" {}
        _ToonShade ("Shade", 2D) = "white" {}
        [MaterialToggle(_COLOR_ON)] _TintColor ("Enable Color Tint", Float) = 0
        _Color ("Base Color", Color) = (1,1,1,1)
        _GradientTopColor ("Gradient Top Color", Color) = (1,0.9,0.75,1)
        _GradientBottomColor ("Gradient Bottom Color", Color) = (0.85,0.45,0.35,1)
        _GradientOffset ("Gradient Offset", Float) = 0
        _GradientScale ("Gradient Scale", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0.08,0.025,0.03,1)
        _Outline ("Outline Width", Float) = 0.01
        [MaterialToggle(_VCOLOR_ON)] _VertexColor ("Enable Vertex Color", Float) = 0
        _Brightness ("Brightness 1 = neutral", Float) = 1.0
        _ClipPlanePosition ("Clip Plane Position", Vector) = (0,0,0,0)
        _ClipPlaneNormal ("Clip Plane Normal", Vector) = (0,0,1,0)
        _ClipSide ("Clip Side", Float) = 1
        _ClipEnabled ("Clip Enabled", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        ZWrite On
        Cull Back
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            Name "BASE"
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
                #pragma glsl_no_auto_normalization
                #pragma multi_compile _TEX_OFF _TEX_ON
                #pragma multi_compile _COLOR_OFF _COLOR_ON

                #if _TEX_ON
                sampler2D _MainTex;
                half4 _MainTex_ST;
                #endif

                struct appdata_base0
                {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float4 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float3 worldPos : TEXCOORD2;
                    #if _TEX_ON
                    half2 uv : TEXCOORD0;
                    #endif
                    half2 uvn : TEXCOORD1;
                };

                v2f vert(appdata_base0 v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    float3 normalP = normalize(v.normal);
                    float3 n = UnityObjectToWorldNormal(normalP);
                    float2 uvM = mul((float3x3)UNITY_MATRIX_V, n).xy;
                    uvM = (uvM * float2(0.5, 0.5)) + float2(0.5, 0.5);
                    o.uvn = uvM;
                    #if _TEX_ON
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    #endif
                    return o;
                }

                sampler2D _ToonShade;
                fixed _Brightness;
                float4 _ClipPlanePosition;
                float4 _ClipPlaneNormal;
                float _ClipSide;
                float _ClipEnabled;

                fixed4 _Color;
                fixed4 _GradientTopColor;
                fixed4 _GradientBottomColor;
                float _GradientOffset;
                float _GradientScale;

                fixed4 frag(v2f i) : COLOR
                {
                    if (_ClipEnabled > 0.5)
                    {
                        float3 clipNormal = normalize(_ClipPlaneNormal.xyz);
                        float clipDistance = dot(i.worldPos - _ClipPlanePosition.xyz, clipNormal) * _ClipSide;
                        clip(clipDistance);
                    }

                    float gradientAmount = saturate((i.worldPos.y + _GradientOffset) * max(_GradientScale, 0.0001));
                    fixed4 gradientColor = lerp(_GradientBottomColor, _GradientTopColor, gradientAmount) * _Color;
                    fixed4 toonShade = tex2D(_ToonShade, i.uvn) * gradientColor;

                    #if _TEX_ON
                    fixed4 detail = tex2D(_MainTex, i.uv);
                    return toonShade * detail * _Brightness;
                    #else
                    return toonShade * _Brightness;
                    #endif
                }
            ENDCG
        }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            CGPROGRAM
                #include "UnityCG.cginc"
                #pragma fragmentoption ARB_precision_hint_fastest
                #pragma glsl_no_auto_normalization
                #pragma vertex vert
                #pragma fragment frag

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float3 worldPos : TEXCOORD0;
                };

                float _Outline;
                fixed4 _OutlineColor;
                float4 _ClipPlanePosition;
                float4 _ClipPlaneNormal;
                float _ClipSide;
                float _ClipEnabled;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    float4 expanded = v.vertex;
                    expanded.xyz += normalize(v.normal.xyz) * _Outline * 0.01;
                    o.pos = UnityObjectToClipPos(expanded);
                    o.worldPos = mul(unity_ObjectToWorld, expanded).xyz;
                    return o;
                }

                fixed4 frag(v2f i) : COLOR
                {
                    if (_ClipEnabled > 0.5)
                    {
                        float3 clipNormal = normalize(_ClipPlaneNormal.xyz);
                        float clipDistance = dot(i.worldPos - _ClipPlanePosition.xyz, clipNormal) * _ClipSide;
                        clip(clipDistance);
                    }

                    return _OutlineColor;
                }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Diffuse"
}
