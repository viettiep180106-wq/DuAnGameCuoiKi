Shader "Game/FX/Earth Impact Smoke"
{
    Properties
    {
        _SmokeMotion ("Age, seed, and spin direction", Vector) = (0, 0, 1, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SmokeMotion;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            float Hash(float2 cell)
            {
                float3 value = frac(cell.xyx * 0.1031);
                value += dot(value, value.yzx + 33.33);
                return frac((value.x + value.y) * value.z);
            }

            float Noise(float2 samplePosition)
            {
                float2 cell = floor(samplePosition);
                float2 blend = frac(samplePosition);
                blend = blend * blend * (3.0 - 2.0 * blend);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), blend.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + 1.0), blend.x), blend.y);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * 2.0;
                float age = _SmokeMotion.x;
                float seed = _SmokeMotion.y;
                float roll = _SmokeMotion.z * (0.2 + age * 1.3) * (1.1 - 0.55 * length(p));
                float sine;
                float cosine;
                sincos(roll, sine, cosine);
                float2 flow = mul(float2x2(cosine, -sine, sine, cosine), p) * 2.2;
                flow += float2(seed, seed * 0.61 - age * 1.2);
                float low = Noise(flow);
                float fine = Noise(flow * 2.4 + float2(low, -age));
                float detail = low * 0.73 + fine * 0.27;
                float body = 1.0 - dot(p, p);
                float rim = smoothstep(0.01, 0.38, body);
                float density = smoothstep(0.16, 0.78, body * 0.69 + detail * 0.67 - 0.19);
                half shading = lerp(0.57h, 1.13h, saturate(detail + p.y * 0.22));
                return half4(input.color.rgb * shading, input.color.a * density * rim);
            }
            ENDHLSL
        }
    }
}
