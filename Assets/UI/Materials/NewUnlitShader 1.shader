Shader "HDRP/UI/3DDiffusionRing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Float) = 2.0

        _Speed ("Speed", Float) = 1.5
        _MaxRadius ("MaxRadius", Float) = 0.45
        _RingWidth ("RingWidth", Float) = 0.04

        _EnableCenter ("EnableCenter", Int) = 1
        _CenterSize ("CenterSize", Float) = 0.03
        _CenterBrightness ("CenterBrightness", Float) = 3.0

        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Int) = 0
        [Enum(LEqual,4,Always,8)] _ZTest ("ZTest", Int) = 4
        _Billboard ("Billboard", Int) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "HDRenderPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]
            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;
            float _Brightness;
            float _Speed;
            float _MaxRadius;
            float _RingWidth;
            int _EnableCenter;
            float _CenterSize;
            float _CenterBrightness;
            int _Billboard;

            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos;

                if (_Billboard == 1)
                {
                    float3 right = UNITY_MATRIX_V[0].xyz;
                    float3 up = UNITY_MATRIX_V[1].xyz;

                    float3 worldOrigin = mul(UNITY_MATRIX_M, float4(0,0,0,1)).xyz;
                    worldPos = worldOrigin + right * v.vertex.x + up * v.vertex.y;
                }
                else
                {
                    worldPos = mul(UNITY_MATRIX_M, v.vertex).xyz;
                }

                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.uv, float2(0.5, 0.5));
                float progress = frac(_Time.y * _Speed);
                float ringPosition = progress * _MaxRadius;

                float ringInner = smoothstep(ringPosition - _RingWidth, ringPosition, dist);
                float ringOuter = smoothstep(ringPosition + _RingWidth, ringPosition, dist);
                float ringAlpha = ringInner * ringOuter;

                float centerAlpha = 0;
                if (_EnableCenter == 1)
                {
                    centerAlpha = smoothstep(_CenterSize, 0.0, dist) * _CenterBrightness;
                }

                float finalAlpha = (ringAlpha + centerAlpha) * _Color.a;
                float3 finalColor = _Color.rgb * _Brightness;

                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
