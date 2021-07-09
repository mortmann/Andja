Shader "Custom/Ocean"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap("BumpMain", 2D) = "bump" {}
        _SecondaryMap("BumpSecondary", 2D) = "bump" {}

        _CellSize("Cell Size", Range(0, 10)) = 2
        _Persistance("Persistance Size", Range(1, 10)) = 2
        _Roughness("Roughness Size", Range(1, 10)) = 2

        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _TimeScale("Time Scaler", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #include "Random.cginc"
        #include "UnityCG.cginc"
        //global shader variables
        #define OCTAVES 4 
        float _Roughness;
        float _Persistance;

        sampler2D _MainTex;
        sampler2D _BumpMap; 
        sampler2D _SecondaryMap;
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldPos;
        };
        float2 _TimeScale;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

            float easeIn(float interpolator) {
            return interpolator * interpolator;
        }

        float easeOut(float interpolator) {
            return 1 - easeIn(1 - interpolator);
        }

        float easeInOut(float interpolator) {
            float easeInValue = easeIn(interpolator);
            float easeOutValue = easeOut(interpolator);
            return lerp(easeInValue, easeOutValue, interpolator);
        }
        // noise by Ronja Böhringer (https://github.com/ronja-tutorials/ShaderTutorials)
        float perlinNoise(float2 value) {
            //generate random directions
            float2 lowerLeftDirection = rand2dTo2d(float2(floor(value.x), floor(value.y))) * 2 - 1;
            float2 lowerRightDirection = rand2dTo2d(float2(ceil(value.x), floor(value.y))) * 2 - 1;
            float2 upperLeftDirection = rand2dTo2d(float2(floor(value.x), ceil(value.y))) * 2 - 1;
            float2 upperRightDirection = rand2dTo2d(float2(ceil(value.x), ceil(value.y))) * 2 - 1;

            float2 fraction = frac(value);

            //get values of cells based on fraction and cell directions
            float lowerLeftFunctionValue = dot(lowerLeftDirection, fraction - float2(0, 0));
            float lowerRightFunctionValue = dot(lowerRightDirection, fraction - float2(1, 0));
            float upperLeftFunctionValue = dot(upperLeftDirection, fraction - float2(0, 1));
            float upperRightFunctionValue = dot(upperRightDirection, fraction - float2(1, 1));

            float interpolatorX = easeInOut(fraction.x);
            float interpolatorY = easeInOut(fraction.y);

            //interpolate between values
            float lowerCells = lerp(lowerLeftFunctionValue, lowerRightFunctionValue, interpolatorX);
            float upperCells = lerp(upperLeftFunctionValue, upperRightFunctionValue, interpolatorX);

            float noise = lerp(lowerCells, upperCells, interpolatorY);
            return noise;
        }
        float sampleLayeredNoise(float2 value) {
            float noise = 0;
            float frequency = 1;
            float factor = 1;

            [unroll]
            for (int i = 0; i < OCTAVES; i++) {
                noise = noise + perlinNoise(value * frequency + i * 0.72354) * factor;
                factor *= _Persistance;
                frequency *= _Roughness;
            }

            return noise;
        }

        float _CellSize;
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed2 offsetUV = _TimeScale.xy * _Time;

            float2 value = (IN.worldPos.xy) / _CellSize;
            float3 noise = sampleLayeredNoise(value + _TimeScale.xy * (sin(_Time)/4.0));
            // Albedo comes from a texture tinted by color
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap + offsetUV * 0.1));
                        //+ UnpackNormal(tex2D(_SecondaryMap, IN.uv_BumpMap + offsetUV * 0.1)) 
                        //+ (sin(_Time)*0.5+0.5) * noise * 0.1;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            float2 uv = IN.uv_MainTex;
            uv *= 1.0; //Scaling amount (larger number more cells can be seen)
            float2 iuv = floor(uv); //gets integer values no floating point
            float2 fuv = frac(uv); // gets only the fractional part
            float minDist = 1.0;  // minimun distance
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    // Position of neighbour on the grid
                    float2 neighbour = float2(float(x), float(y));
                    // Random position from current + neighbour place in the grid
                    float2 pointv = random2(iuv + neighbour);
                    // Move the point with time
                    pointv = 0.5 + 0.1 * sin(_Time.z * _TimeScale.x + 6.2236 * pointv);//each point moves in a certain way
                                                                    // Vector between the pixel and the point
                    float2 diff = neighbour + pointv - fuv;
                    // Distance to the point
                    float dist = length(diff);
                    // Keep the closer distance
                    minDist = min(minDist, dist);
                }
            }
//voronoiNoise(value + _TimeScale.xy * sin(_Time)).y
            fixed4 col = _Color;
            col.b += minDist * minDist;
            o.Albedo = (_Color) +col.b*0.1 + fixed4(1,1,1,perlinNoise(value + _TimeScale.xy * (sin(_Time) / 4.0)))/6;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
