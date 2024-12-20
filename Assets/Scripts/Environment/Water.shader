Shader "Custom/WaterWaveShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _Width ("Width", Float) = 10
        _Height ("Height", Float) = 1
        _Segments ("Segments", Float) = 50
        _WaterTimer ("Water Timer", Float) = 0.02
        _WaterColor ("Water Color", Color) = (0.5, 0.7, 1.0, 0.5)
        _WaveAmplitude ("Wave Amplitude", Float) = 0.5
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _ReflectionBlend ("Reflection Blend", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        // Enable both sides rendering
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _ReflectionTex;
            float _Width;
            float _Height;
            float _Segments;
            float _WaterTimer;
            float4 _WaterColor;
            float _WaveAmplitude;
            float _WaveSpeed;
            float _ReflectionBlend;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float x = i.uv.x * _Width;
                float y = _Height + sin(i.uv.y * _Segments * _WaterTimer * _WaveSpeed) * _WaveAmplitude;
                float4 pos = float4(x, y, 0, 1);

                // Get the base water color
                fixed4 waterColor = _WaterColor;

                // Blend with reflection texture
                fixed4 reflectionColor = tex2D(_ReflectionTex, i.uv);
                fixed4 finalColor = lerp(waterColor, reflectionColor, _ReflectionBlend);

                return finalColor;
            }
            ENDCG
        }
    }
}
