Shader "TSF/InteriorMapping"
{
    Properties
    {
        _BackTex   ("Back Wall",   2D) = "white" {}
        _LeftTex   ("Left Wall",   2D) = "white" {}
        _RightTex  ("Right Wall",  2D) = "white" {}
        _TopTex    ("Ceiling",     2D) = "white" {}
        _BottomTex ("Floor",       2D) = "white" {}
        _FrontTex  ("Front Overlay (window frame)", 2D) = "black" {}
        _RoomDepth ("Room Depth",  Float) = 1.0
        _RoomWidth ("Room Width",  Float) = 1.0
        _RoomHeight("Room Height", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Opaque"
            "Queue"           = "Geometry"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Pass
        {
            Name "InteriorMapping"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BackTex);   SAMPLER(sampler_BackTex);
            TEXTURE2D(_LeftTex);   SAMPLER(sampler_LeftTex);
            TEXTURE2D(_RightTex);  SAMPLER(sampler_RightTex);
            TEXTURE2D(_TopTex);    SAMPLER(sampler_TopTex);
            TEXTURE2D(_BottomTex); SAMPLER(sampler_BottomTex);
            TEXTURE2D(_FrontTex);  SAMPLER(sampler_FrontTex);

            CBUFFER_START(UnityPerMaterial)
                float _RoomDepth;
                float _RoomWidth;
                float _RoomHeight;
                float4 _BackTex_ST;
                float4 _LeftTex_ST;
                float4 _RightTex_ST;
                float4 _TopTex_ST;
                float4 _BottomTex_ST;
                float4 _FrontTex_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 viewDirTS   : TEXCOORD0;
                float2 uv          : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs    = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.uv          = IN.uv;

                float3 viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);

                // World-to-tangent matrix (rows = tangent, bitangent, normal)
                float3x3 worldToTangent = float3x3(
                    normalInputs.tangentWS,
                    normalInputs.bitangentWS,
                    normalInputs.normalWS
                );
                OUT.viewDirTS = mul(worldToTangent, viewDirWS);

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Fragment position on the quad surface, centered at (0,0)
                // Scaled by room dimensions so the virtual box has correct proportions
                float2 pos = (IN.uv - 0.5) * float2(_RoomWidth, _RoomHeight);

                // Ray goes INTO the room (negative Z in tangent space)
                // viewDirTS points surface→camera, so -viewDirTS points into the room
                float3 dir = -normalize(IN.viewDirTS);

                // Safe-divide: avoid NaN when dir component is ~0
                const float EPS = 1e-6;
                dir.x = (abs(dir.x) < EPS) ? EPS : dir.x;
                dir.y = (abs(dir.y) < EPS) ? EPS : dir.y;
                dir.z = (abs(dir.z) < EPS) ? EPS : dir.z;

                // Ray origin: on the front face of the virtual room box
                float3 ro = float3(pos.x, pos.y, 0.0);

                // Virtual room AABB
                float hw = _RoomWidth  * 0.5;
                float hh = _RoomHeight * 0.5;
                float3 boxMin = float3(-hw, -hh, -_RoomDepth);
                float3 boxMax = float3( hw,  hh,  0.0);

                // Slab-method ray/AABB — find first exit face
                float3 tA = (boxMin - ro) / dir;
                float3 tB = (boxMax - ro) / dir;

                // Per-axis: tNear = entry, tFar = exit (we start inside, so tNear <= 0 <= tFar)
                float3 tFar = max(tA, tB);

                // Exit t is the minimum of all tFar values
                float tExit = min(min(tFar.x, tFar.y), tFar.z);
                float3 hit  = ro + dir * tExit;

                // ---------------------------------------------------------------
                // UV conventions (all walls, from outside the room looking in):
                //   Side walls  — U: 0=front(window), 1=back wall   V: 0=bottom, 1=top
                //   Floor/Ceil  — U: 0=left, 1=right                V: 0=front,  1=back
                //   Back wall   — U: 0=left, 1=right                V: 0=bottom, 1=top
                // ---------------------------------------------------------------

                float4 color;

                if (tFar.z <= tFar.x && tFar.z <= tFar.y)
                {
                    // Back wall  (z = -_RoomDepth)
                    float2 uv = float2((hit.x + hw) / _RoomWidth,
                                       (hit.y + hh) / _RoomHeight);
                    color = SAMPLE_TEXTURE2D(_BackTex, sampler_BackTex, uv);
                }
                else if (tFar.x <= tFar.y)
                {
                    float depth01 = -hit.z / _RoomDepth; // 0=front, 1=back
                    float v       = (hit.y + hh) / _RoomHeight;

                    if (hit.x < 0.0)
                        // Left wall
                        color = SAMPLE_TEXTURE2D(_LeftTex, sampler_LeftTex,   float2(depth01, v));
                    else
                        // Right wall (U flipped so texture reads naturally for both walls)
                        color = SAMPLE_TEXTURE2D(_RightTex, sampler_RightTex, float2(1.0 - depth01, v));
                }
                else
                {
                    float u      = (hit.x + hw) / _RoomWidth;
                    float depth01 = -hit.z / _RoomDepth;

                    if (hit.y < 0.0)
                        // Floor
                        color = SAMPLE_TEXTURE2D(_BottomTex, sampler_BottomTex, float2(u, depth01));
                    else
                        // Ceiling
                        color = SAMPLE_TEXTURE2D(_TopTex, sampler_TopTex, float2(u, depth01));
                }

                // Front overlay (window frame / decal) — alpha-blended on top
                float4 front = SAMPLE_TEXTURE2D(_FrontTex, sampler_FrontTex, IN.uv);
                color.rgb = lerp(color.rgb, front.rgb, front.a);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
