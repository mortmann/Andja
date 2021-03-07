// Code originaly from Unity's built-in Sprites-Default.shader
Shader "TileMapShader" // Name changed
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
		[HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
		[PerRendererData] _Saturations("Saturations", 2D) = "white" {}

		// Custom properties
		//_TransparencyRatio("Transparency Ratio", Range(0, 0.5)) = 0.1
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

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex SpriteVert
			#pragma fragment CustomSpriteFrag // Originally SpriteFrag
			#pragma multi_compile_instancing
			#pragma multi_compile_local _ PIXELSNAP_ON
			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
			#pragma target 2.0
			#include "UnitySprites.cginc"
			#include "UnityCG.cginc"
			// Custom properties
			float2 _startPosition;
			int _Width;
			int _Height;
			sampler2D _Saturations;
			void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
			{
				float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
				Out = luma.xxx + Saturation.xxx * (In - luma.xxx);
			}
			fixed4 CustomSpriteFrag(v2f IN) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, IN.texcoord);
				//c.rgb *= c.a;
				//float2 worldXY = mul(unity_ObjectToWorld, IN.texcoord).xy;
				//float2 localXY = worldXY - _startPosition;
				//float2 pos = float2(floor(worldXY.x), floor(worldXY.y));
				//Unity_Saturation_float(c.rgb, tex2D(_Saturations, pos).a* tex2D(_Saturations, pos).a, c.rgb);

				return c;
			}	
		ENDCG
		}
	}
	Fallback "Unlit/Color"
}
//fixed4 CustomSpriteFrag(v2f IN) : SV_Target // Copy of SpriteFrag from UnitySprites.cginc with modification
//{
//	fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
//	//c.rgb *= c.a;
//	//float2 worldXY = mul(unity_ObjectToWorld, IN.texcoord).xy;
//	//float2 localXY = worldXY - _startPosition;
//	//float2 pos = float2(floor(worldXY.x), floor(worldXY.y));
//	//Unity_Saturation_float(c.rgb, tex2D(_Saturations, pos).a * tex2D(_Saturations, pos).a, c.rgb);

//	// Apply transparency to edges
//	//float multiplier = 1 / _TransparencyRatio;
//	//if (IN.texcoord.x > 1 - _TransparencyRatio) c *= (1 - IN.texcoord.x) * multiplier;
//	//if (IN.texcoord.x < _TransparencyRatio) c *= IN.texcoord.x * multiplier;
//	//if (IN.texcoord.y > 1 - _TransparencyRatio) c *= (1 - IN.texcoord.y) * multiplier;
//	//if (IN.texcoord.y < _TransparencyRatio) c *= IN.texcoord.y * multiplier;
//	//c *= sin(IN.texcoord.x + IN.texcoord.y * 3.14) * sin(IN.texcoord.y * 3.14);  // Could be used for hex?

//	return c;
//}

//Shader "TileMapShader"
//{
//	Properties
//	{
//
//		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
//		_Color("Tint", Color) = (1,1,1,1)
//		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
//	}
//
//		SubShader
//		{
//			Tags
//			{
//				"Queue" = "Transparent"
//				"IgnoreProjector" = "True"
//				"RenderType" = "Transparent"
//				"PreviewType" = "Plane"
//				"CanUseSpriteAtlas" = "True"
//			}
//
//			Cull Off
//			Lighting Off
//			ZWrite Off
//			Blend One OneMinusSrcAlpha
//
//			Pass
//			{
//			CGPROGRAM
//				#pragma vertex vert
//				#pragma fragment frag
//				#pragma multi_compile _ PIXELSNAP_ON
//				#include "UnityCG.cginc"
//
//				struct appdata_t
//				{
//					float4 vertex   : POSITION;
//					float4 color    : COLOR;
//					float2 texcoord : TEXCOORD0;
//				};
//
//				struct v2f
//				{
//					float4 vertex   : SV_POSITION;
//					fixed4 color : COLOR;
//					float2 texcoord  : TEXCOORD0;
//				};
//				float3 applyHue(float3 aColor, float aHue)
//				{
//					float angle = radians(aHue);
//					float3 k = float3(0.57735, 0.57735, 0.57735);
//					float cosAngle = cos(angle);
//					//Rodrigues' rotation formula
//					return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
//				}
//				fixed4 _Color;
//
//				v2f vert(appdata_t IN)
//				{
//					v2f OUT;
//					OUT.vertex = UnityObjectToClipPos(IN.vertex);
//					OUT.texcoord = IN.texcoord;
//					OUT.color = IN.color * _Color;
//					OUT.color.rgb = applyHue(OUT.color.rgb, 0.5f);
//
//					#ifdef PIXELSNAP_ON
//					OUT.vertex = UnityPixelSnap(OUT.vertex);
//					#endif
//
//					return OUT;
//				}
//
//				sampler2D _MainTex;
//				sampler2D _AlphaTex;
//				float _AlphaSplitEnabled;
//				
//				fixed4 SampleSpriteTexture(float2 uv)
//				{
//					fixed4 color = tex2D(_MainTex, uv);
//
//	#if UNITY_TEXTURE_ALPHASPLIT_ALLOWED
//					if (_AlphaSplitEnabled)
//						color.a = tex2D(_AlphaTex, uv).r;
//	#endif //UNITY_TEXTURE_ALPHASPLIT_ALLOWED
//					color.rgb = applyHue(color.rgb, 0.5f);
//					return color;
//				}
//
//				fixed4 frag(v2f IN) : SV_Target
//				{
//					fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
//					c.rgb *= c.a;
//					c.rgb = applyHue(c.rgb, 0.5f);
//
//					return c;
//				}
//				
//			ENDCG
//			}
//		}
//}