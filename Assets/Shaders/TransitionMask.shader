Shader "UI/TransitionMask"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _Radius ("Radius", Range(0, 1)) = 0
        _Feather ("Feather", Range(0, 0.5)) = 0.08
        _Color ("Mask Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Radius;
            float _Feather;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 当前像素到屏幕中心的距离
                float dist = distance(i.uv, float2(0.5, 0.5));
                // 归一化到 0~1（对角线最大距离约为 0.7071）
                float maxDist = 0.7071;
                float normalizedDist = dist / maxDist;

                // smoothstep 产生柔和边缘：半径内透明，半径外黑色
                float alpha = smoothstep(_Radius - _Feather, _Radius + _Feather, normalizedDist);

                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
