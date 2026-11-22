Shader "Game/FX/Enemy Fire Smoke Preview"
{
    Properties
    {
        _SmokeTime ("Smoke time", Float) = 0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _SmokeTime;
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
                output.color = input.color;
                return output;
            }

            float Hash(float2 cell)
            {
                float3 q = frac(cell.xyx * 0.1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }

            float Noise(float2 samplePosition)
            {
                float2 cell = floor(samplePosition);
                float2 f = frac(samplePosition);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + 1.0), f.x), f.y);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(p);
                float phase = _SmokeTime * (2.4 - radius);
                float sine;
                float cosine;
                sincos(phase, sine, cosine);
                float2 q = mul(float2x2(cosine, -sine, sine, cosine), p) * 2.55;
                q += float2(14.2 + _SmokeTime * 0.58, 9.7 - _SmokeTime * 1.25);
                float body = 1.0 - radius * radius;
                float detail = Noise(q) * 0.72 + Noise(q * 2.15 + 0.53) * 0.28;
                half density = smoothstep(0.08, 0.72, body * 0.69 + detail * 0.68 - 0.29);
                half edge = smoothstep(0.0, 0.33, body);
                half shading = lerp(0.61h, 1.15h, saturate(detail + p.y * 0.22));
                return half4(input.color.rgb * shading, input.color.a * density * edge);
            }
            ENDHLSL
        }
    }
}
