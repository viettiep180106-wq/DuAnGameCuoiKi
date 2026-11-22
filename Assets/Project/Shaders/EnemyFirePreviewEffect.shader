Shader "Game/FX/Enemy Fire Preview Effect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _PreviewEffect ("Mode and progress", Vector) = (0, 0, 0, 0)
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _PreviewEffect;
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

            float2 StarPoint(int index)
            {
                float theta = 1.5707963 - index * 1.0471976;
                return float2(cos(theta) * 0.74, sin(theta) * 0.80);
            }

            float SegmentDistance(float2 position, float2 start, float2 end)
            {
                float2 span = end - start;
                float t = saturate(dot(position - start, span) / max(dot(span, span), 0.0001));
                return length(position - start - span * t);
            }

            half4 TraceSigil(Varyings input, float progress)
            {
                half4 art = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                if (progress >= 1.0)
                    return art;

                // The ellipse center sits below the center of Sigil.png because of its flames.
                float2 p = (input.uv - float2(0.493, 0.443)) / float2(0.470, 0.353);
                float angle = frac((atan2(p.x, p.y) + 6.2831853) / 6.2831853);
                float radius = length(p);
                float ringProgress = progress * 1.40;
                float ringTip = (1.0 - smoothstep(ringProgress - 0.025, ringProgress, angle));
                float ring = ringTip * smoothstep(0.71, 0.80, radius) * (1.0 - smoothstep(1.03, 1.19, radius));
                float starProgress = saturate((progress - 0.13) / 0.87) * 6.0;
                float starDistance = 10.0;
                [unroll]
                for (int i = 0; i < 6; i++)
                {
                    float amount = saturate(starProgress - i);
                    int vertex = i < 3 ? i * 2 : (i - 3) * 2 + 1;
                    float2 start = StarPoint(vertex);
                    float2 end = lerp(start, StarPoint(vertex + 2), amount);
                    if (amount > 0.0)
                        starDistance = min(starDistance, SegmentDistance(p, start, end));
                }
                float star = (1.0 - smoothstep(0.049, 0.125, starDistance)) * (1.0 - smoothstep(0.79, 0.87, radius));
                art.a *= lerp(max(ring, star), 1.0, smoothstep(0.84, 1.0, progress));
                return art;
            }

            half3 FireColor(float heat)
            {
                half3 red = half3(0.48, 0.037, 0.007);
                half3 orange = half3(1.0, 0.24, 0.018);
                half3 gold = half3(1.0, 0.91, 0.48);
                return lerp(lerp(red, orange, smoothstep(0.02, 0.58, heat)),
                    gold, smoothstep(0.51, 1.0, heat));
            }

            half4 ImpOrb(Varyings input, float time)
            {
                float2 p = (input.uv - 0.5) * 2.0;
                float2 flow = float2(p.x / (1.0 + 0.34 * smoothstep(0.0, 0.8, p.x)), p.y * 1.08);
                float radius = length(flow);
                float angle = atan2(flow.y, flow.x);
                float tail = 0.5 + 0.5 * cos(angle);
                float roll = angle * 3.0 + radius * 11.0 - time * 24.0;
                float flicker = 0.58 * sin(roll) + 0.27 * sin(angle * 5.0 + radius * 8.0 - time * 37.0);
                flicker += 0.14 * sin(angle * 8.0 - radius * 13.0 - time * 31.0);
                float edge = 0.61 + 0.11 * tail + (0.12 + 0.12 * tail) * flicker;
                float depth = edge - radius;
                float flame = smoothstep(-0.032, 0.027, depth);
                float heat = saturate(depth * 3.65 + 0.14 * sin(roll - 0.4));
                half3 color = FireColor(heat);

                float coreRadius = length(p);
                float spin = time * 7.0 + coreRadius * 0.40 * sin(time * 10.0);
                float sine;
                float cosine;
                sincos(spin, sine, cosine);
                float2 coreUV = float2(0.575, 0.432) + mul(float2x2(cosine, -sine, sine, cosine), p) * 0.47;
                half3 coreTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, coreUV).rgb;
                half purple = saturate((coreTex.b - coreTex.g) * 12.0);
                half3 coreColor = lerp(half3(0.15, 0.028, 0.24), coreTex, purple);
                float ripple = 0.012 * sin(10.0 * angle - time * 17.0);
                float coreMask = 1.0 - smoothstep(0.36, 0.434, coreRadius + ripple);
                color = lerp(color, coreColor, coreMask);

                float glow = smoothstep(-0.14, -0.012, depth) * 0.23;
                float frameMask = 1.0 - smoothstep(0.92, 1.0, max(abs(p.x), abs(p.y)));
                return half4(color * input.color.rgb, max(coreMask, max(flame, glow)) * frameMask * input.color.a);
            }

            half4 ImpFlash(Varyings input, float phase)
            {
                float2 p = (input.uv - 0.5) * 2.0;
                float radius = length(p);
                float angle = atan2(p.y, p.x);
                float rays = pow(saturate(0.54 + 0.47 * cos(angle * 7.0 + phase * 2.5 + 0.7 * sin(angle * 3.0))), 6.0);
                float rayReach = 0.17 + (0.66 - 0.22 * phase) * rays;
                float star = (1.0 - smoothstep(rayReach * 0.66, rayReach, radius)) * (1.0 - phase);
                float core = (1.0 - smoothstep(0.045, 0.37 - phase * 0.19, radius)) * (1.0 - phase);
                float ringRadius = 0.36 + phase * 0.47;
                float wave = (1.0 - smoothstep(0.014, 0.07, abs(radius - ringRadius)))
                    * smoothstep(0.09, 0.38, phase) * (1.0 - phase) * 0.48;
                half3 color = lerp(half3(1.0, 0.30, 0.025), half3(1.0, 0.99, 0.79), max(star * 0.8, core));
                return half4(color * input.color.rgb, max(max(star, core), wave) * input.color.a);
            }

            half4 ImpBurstFlame(Varyings input, float age, float seed)
            {
                float2 p = (input.uv - 0.5) * 2.0;
                float phase = age * 14.0 + seed * 6.28;
                float2 flow = p;
                flow.y += (0.17 * sin(p.x * 3.9 - phase) + 0.053 * sin(p.x * 9.0 - phase * 1.3))
                    * smoothstep(-0.75, 0.84, p.x);
                flow.y *= 1.0 + 0.44 * smoothstep(-0.54, 0.87, p.x);
                float radius = length(flow);
                float angle = atan2(flow.y, flow.x);
                float churn = 0.11 * sin(angle * 3.0 + phase) + 0.05 * sin(angle * 6.0 - phase * 1.5);
                float depth = 0.69 + 0.19 * saturate(flow.x + 0.25) + churn - radius;
                float frameMask = 1.0 - smoothstep(0.89, 1.0, max(abs(p.x), abs(p.y)));
                float coverage = max(smoothstep(-0.035, 0.039, depth), 0.19 * smoothstep(-0.17, -0.01, depth))
                    * frameMask;
                float heat = saturate(depth * 2.6 + 0.16 * cos(radius * 12.0 + angle * 3.0 - phase))
                    * (1.0 - age * 0.63);
                half3 color = lerp(FireColor(heat), half3(0.24, 0.072, 0.036), smoothstep(0.65, 1.0, age));
                return half4(color * input.color.rgb, coverage * input.color.a);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half alpha = 1.0h;
                half light = 1.0h;
                float mode = _PreviewEffect.x;
                float progress = _PreviewEffect.y;
                if (mode >= 3.5 && mode < 4.5)
                    return TraceSigil(input, progress);
                if (mode >= 8.5)
                    return ImpBurstFlame(input, progress, _PreviewEffect.z);
                if (mode >= 7.5)
                    return ImpFlash(input, progress);
                if (mode >= 6.5)
                    return ImpOrb(input, progress);

                if (mode > 0.5 && mode < 1.5)
                {
                    float wave = uv.x * 22.0 - progress * 28.0;
                    float front = smoothstep(0.58, 1.0, uv.x);
                    uv.y += sin(wave) * (0.047 + front * 0.075);
                    uv.x += sin(progress * 52.0 + uv.y * 9.0) * front * 0.025;
                    light += 0.32h * saturate(sin(wave * 0.92));
                    alpha *= smoothstep(0.0, 0.04, uv.y) * (1.0 - smoothstep(0.96, 1.0, uv.y));
                }
                else if (mode >= 1.5 && mode < 2.5)
                {
                    alpha = smoothstep(1.0 - progress - 0.13, 1.0 - progress + 0.01, uv.y);
                }
                else if (mode >= 2.5 && mode < 3.5)
                {
                    float wave = uv.y * 13.0 - progress * 32.0;
                    uv.x += sin(wave) * sin(uv.y * 3.14159) * 0.044;
                    light += 0.30h * saturate(sin(wave));
                    alpha *= smoothstep(0.0, 0.04, uv.x) * (1.0 - smoothstep(0.96, 1.0, uv.x));
                }
                else if (mode >= 4.5 && mode < 5.5)
                {
                    float height = uv.y;
                    float wave = uv.y * 24.0 - progress * 39.0;
                    uv.x += (sin(wave) + 0.45 * sin(wave * 1.71 + progress * 11.0))
                        * (0.018 + height * 0.103);
                    uv.y += cos(uv.x * 14.0 - progress * 23.0) * height * height * 0.088;
                    light += 0.71h * saturate(sin(wave + 0.8));
                    alpha *= smoothstep(0.015, 0.075, uv.x) * (1.0 - smoothstep(0.925, 0.985, uv.x));
                }
                else if (mode >= 5.5)
                {
                    uv.y += sin(uv.x * 11.0 - progress * 53.0) * sin(uv.x * 3.14159) * 0.135;
                    uv.x += sin(uv.y * 10.0 + progress * 36.0) * sin(uv.x * 3.14159) * 0.06;
                    light += 0.46h * saturate(sin(uv.x * 19.0 - progress * 34.0));
                }

                half4 art = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * input.color;
                return half4(art.rgb * light, art.a * alpha);
            }
            ENDHLSL
        }
    }
}
