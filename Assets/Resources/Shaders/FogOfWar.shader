Shader "Hidden/FogOfWar"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _SecondaryTex("Secondary Texture", 2D) = "white" {}
        _FallbackTex("Fallback Texture", 2D) = "white" {}
        _TopTex("Top Texture", 2D) = "white" {}
        _Color("Main Color", COLOR) = (1,1,1,1)
        _Spaceing("Spacing", Vector) = (1,1,0,0)
        _RedWeight("Red Weight", Float) = 1.5
        _BlueWeight("Blue Weight", Float) = 0.5
        _TransparencyRatio("Transparency Ratio", Float) = 0.5

    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+1"
        }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                fixed4 color : COLOR0;
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR0;
            };

            sampler2D _MainTex;
            sampler2D _SecondaryTex;
            sampler2D _FallbackTex;
            sampler2D _TopTex;
            float4 _MainTex_TexelSize;
            float4 _TopTex_TexelSize;
            float4 _FallbackTex_ST;
            float4 _TopTex_ST;
            float4 _Spaceing;
            float4 _Color;

            float _RedWeight;
            float _BlueWeight;
            float _TransparencyRatio;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {

                fixed4 col = tex2D(_MainTex, i.uv) + tex2D(_SecondaryTex, i.uv);
                fixed4 col2 = tex2D(_TopTex, TRANSFORM_TEX(i.uv, _TopTex));
                float r = 0;
                for (int x = -3; x < 4; x++)
                {
                    for (int y = -3; y < 4; y++)
                    {
                        fixed4 nC = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x * x, _MainTex_TexelSize.y * y));
                        r += nC.r;
                        col2 += tex2D(_TopTex, TRANSFORM_TEX(i.uv, _TopTex) + float2((_TopTex_TexelSize.x) * x,
                            (_TopTex_TexelSize.y) * y));
                    }
                }
                col.r = clamp(col.r + (r / 16), 0, 1);
                col2 /= 37;
                // red = 1, blue = 1 , we want alpha 0;
                // red = 1, blue = 0, we want 0.5;
                // red = 0, blue = 0, we want alpha 1;
                fixed4 col1 = tex2D(_FallbackTex, TRANSFORM_TEX(i.uv, _FallbackTex));

                col.a = 2.0f - col.r * _RedWeight - col.b * _BlueWeight + col.g;
                if (col.a > 0.6f && col2.a > 0) {
                    col1.rgb = lerp(col1.rgb, col1.rgb * col2.rgb, col2.a);
                    return col1;
                }
                //return col;
                return fixed4(
                    (col1.r * col1.a) - (col.r - col.b) * 0.5f * (1 - col.a),
                    (col1.g * col1.a) - (col.r - col.b) * 0.5f * (1 - col.a),
                    (col1.b * col1.a) - (col.r - col.b) * 0.5f * (1 - col.a),
                    col.a
                );
            }

        ENDCG
        }
    }
}
