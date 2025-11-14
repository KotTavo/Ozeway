// SlimeMetaballShader.shader
Shader "Custom/SlimeMetaballShader"
{
    Properties
    {
        _BaseColor("Slime Color", Color) = (0, 1, 0, 0.5)
        _Threshold("Edge Threshold", Range(0.1, 2.0)) = 1.0
        _Smoothness("Edge Smoothness", Range(0.01, 1.0)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

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
                float4 worldPos : TEXCOORD1;
            };

            // Данные, получаемые из C# скрипта
            uniform float4 _NodePositions[256];
            uniform int _NodeCount;
            uniform float4 _SlimeCenter;
            uniform float _MaxRadius;
            
            // Настройки из инспектора
            fixed4 _BaseColor;
            float _Threshold;
            float _Smoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float totalInfluence = 0.0;
                
                // Проходим по всем узлам (метаболам)
                for (int j = 0; j < _NodeCount; j++)
                {
                    float3 nodePos = _NodePositions[j].xyz;
                    float radius = _NodePositions[j].w;
                    
                    float distSq = dot(i.worldPos.xyz - nodePos, i.worldPos.xyz - nodePos);
                    float radiusSq = radius * radius;

                    // Используем полиномиальную функцию для плавного и ограниченного влияния.
                    // Это решает проблему "оторванных" узлов.
                    // Влияние = (1 - (dist/radius)^2)^2
                    if (distSq < radiusSq)
                    {
                        float normalizedDistSq = distSq / radiusSq;
                        float influence = (1.0 - normalizedDistSq);
                        totalInfluence += influence * influence;
                    }
                }

                // Создаем маску формы с гладкими краями
                float mask = smoothstep(_Threshold - _Smoothness, _Threshold + _Smoothness, totalInfluence);
                
                // Если пиксель полностью вне формы, отбрасываем его
                clip(mask - 0.001);

                // Вычисляем градиент прозрачности для 3D эффекта
                // Расстояние от текущего пикселя до центра слизи
                float distFromCenter = distance(i.worldPos.xyz, _SlimeCenter.xyz);
                // Нормализуем расстояние, чтобы получить значение от 0 до 1
                float normalizedDist = saturate(distFromCenter / _MaxRadius);

                // Собираем итоговый цвет
                fixed4 finalColor = _BaseColor;
                // Прозрачность зависит от маски И от расстояния до центра
                finalColor.a *= mask * normalizedDist;

                return finalColor;
            }
            ENDCG
        }
    }
}