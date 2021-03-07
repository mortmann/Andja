// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Sprites/RadialGradient"
{
    Properties
    {
        //      [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _ColorA("Color A", Color) = (1, 1, 1, 1)
        _ColorB("Color B", Color) = (0, 0, 0, 1)
        _Slide("Slide", Range(0, 1)) = 0.5
        _MainTex("Texture", 2D) = "white" {}

    }

        SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
    LOD 100
    Cull Off
    Lighting Off
    ZWrite Off

    Fog { Mode Off }
    ColorMask RGB
    Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"
              struct appdata_t {
                 float4 vertex : POSITION;
                 float2 texcoord : TEXCOORD0;
                 float4 color    : COLOR;
             };

             struct v2f {
                 float4 vertex : SV_POSITION;
                 half2 texcoord : TEXCOORD0;
                  half4 color : COLOR;
             };

            fixed4 _Color;
             v2f vert(appdata_t v)
             {
                 v2f o;
                 o.vertex = UnityObjectToClipPos(v.vertex);
                 o.texcoord = v.texcoord;
                   o.color = _Color;
                 return o;
             }


             fixed4 _ColorA, _ColorB;
             float _Slide;




            sampler2D _MainTex;
            sampler2D _AlphaTex;



            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

#if ETC1_EXTERNAL_ALPHA
                // get the color from an external texture (usecase: Alpha support for ETC1 on android)
                color.a = tex2D(_AlphaTex, uv).r;
#endif //ETC1_EXTERNAL_ALPHA

                return color;
            }



             fixed4 frag(v2f i) : SV_Target
             {
                float t = length(i.texcoord - float2(0.5, 0.5)) * 1.41421356237; // 1.141... = sqrt(2)
                fixed4 c = SampleSpriteTexture(i.texcoord) * i.color;
                c.rgb *= lerp(_ColorA, _ColorB, t + (_Slide - 0.5) * 2);
                c.rgb *= c.a;
                return c;
             }

        ENDCG
        }
    }
}
