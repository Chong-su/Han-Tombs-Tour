Shader "UI/EdgeBlur RawImage"
{
    Properties
    {
        _MainTex ("视频纹理", 2D) = "white" {}
        _BlurRange ("虚化范围", Range(0, 0.5)) = 0.12
        _BlurPower ("模糊强度", Range(0, 10)) = 4
        _TintColor ("颜色叠加", Color) = (1,1,1,1)
        
        // UGUI 内置模板兼容参数
        _StencilComp ("模板比较", Float) = 8
        _Stencil ("模板ID", Float) = 0
        _StencilOp ("模板操作", Float) = 0
        _StencilWriteMask ("模板写入掩码", Float) = 255
        _StencilReadMask ("模板读取掩码", Float) = 255
        _ColorMask ("颜色掩码", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BlurRange;
            float _BlurPower;
            fixed4 _TintColor;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPos);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _TintColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                // 计算像素到四边的距离
                float distX = min(uv.x, 1 - uv.x);
                float distY = min(uv.y, 1 - uv.y);
                float edgeDist = min(distX, distY);
                
                // 边缘模糊系数：越靠近边缘模糊越强
                float blurWeight = smoothstep(_BlurRange, 0, edgeDist) * _BlurPower * 0.01;
                
                // 8方向多重采样实现模糊
                fixed4 finalCol = tex2D(_MainTex, uv);
                finalCol += tex2D(_MainTex, uv + float2(blurWeight, 0));
                finalCol += tex2D(_MainTex, uv - float2(blurWeight, 0));
                finalCol += tex2D(_MainTex, uv + float2(0, blurWeight));
                finalCol += tex2D(_MainTex, uv - float2(0, blurWeight));
                finalCol += tex2D(_MainTex, uv + float2(blurWeight, blurWeight));
                finalCol += tex2D(_MainTex, uv - float2(blurWeight, blurWeight));
                finalCol += tex2D(_MainTex, uv + float2(blurWeight, -blurWeight));
                finalCol += tex2D(_MainTex, uv - float2(blurWeight, -blurWeight));
                finalCol /= 9;
                
                // 边缘渐隐
                float alpha = smoothstep(0, _BlurRange, edgeDist);
                finalCol.a *= alpha * i.color.a;
                
                // 适配 UI 矩形裁剪
                finalCol.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                
                return finalCol;
            }
            ENDCG
        }
    }
}
