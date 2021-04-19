Shader "Hidden/FogOfWar"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
		_SecondaryTex("Secondary Texture", 2D) = "white" {}
        _RedWeight("Red Weight", Float) = 1.5 
        _BlueWeight("Blue Weight", Float) = 0.5
    }
    SubShader
    {
		Tags 
		{
			"Queue" = "Transparent+1"
		}
        Pass
        {
			Blend SrcAlpha OneMinusSrcAlpha
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			sampler2D _SecondaryTex;
            float _RedWeight;
            float _BlueWeight;

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = tex2D(_MainTex, i.uv) + tex2D(_SecondaryTex, i.uv);
				// red = 1, blue = 1 , we want alpha 0;
				// red = 1, blue = 0, we want 0.5;
				// red = 0, blue = 0, we want alpha 1;
                col.a = 2.0f - col.r * _RedWeight - col.b * _BlueWeight + col.g;
                return fixed4(0,0,0,col.a);
            }
            ENDCG
        }
    }
}
