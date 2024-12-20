Shader "Custom/SmokeTracer"
{
    Properties
    {
        _Color ("Smoke Color", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.5
        _Speed ("Speed", Range(0.1, 1.0)) = 0.5
        _Size ("Size", Range(0.1, 2.0)) = 1.0
        _TimeMultiplier ("Time Multiplier", Range(0.1, 10.0)) = 1.0
        _MainTex ("Base (RGB)", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" }
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
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Speed;
            float _Opacity;
            float _TimeMultiplier;
            float _Size;
            float4 _Color;
            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float time = _Time.y * _TimeMultiplier;  // Время анимации
                float randomNoise = frac(sin(i.uv.x * 1000.0) * 43758.5453); // Случайный эффект для дыма

                // Смесь прозрачности и случайности для имитации дыма
                float alpha = smoothstep(0.4, 0.6, randomNoise + time * _Speed);
                alpha = lerp(alpha, 0.0, randomNoise); // Модификация альфа-канала для дымового эффекта

                // Итоговый цвет (цвет дыма и его прозрачность)
                half4 col = _Color;
                col.a = alpha * _Opacity; // Установим прозрачность

                return col;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
