
Shader "Unlit/SpriteOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 5)) = 1
        _IsActive ("Is Active", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _IsActive;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                if (_IsActive > 0.5 && col.a < 0.1)
                {
                    float2 size = _MainTex_TexelSize.xy * _OutlineWidth;
                    float a = 0;
                    a += tex2D(_MainTex, i.uv + float2(size.x, 0)).a;
                    a += tex2D(_MainTex, i.uv + float2(-size.x, 0)).a;
                    a += tex2D(_MainTex, i.uv + float2(0, size.y)).a;
                    a += tex2D(_MainTex, i.uv + float2(0, -size.y)).a;
                    
                    if (a > 0.1) return _OutlineColor;
                }
                
                return col;
            }
            ENDCG
        }
    }
}
