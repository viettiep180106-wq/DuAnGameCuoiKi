Shader "Game/FX/Enemy Water"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _Atlas ("Atlas", 2D) = "white" {}
        _WaterRect ("Atlas rectangle", Vector) = (0, 0, 1, 1)
        _WaterFx ("Shape, reveal, time, deformation", Vector) = (0, 1, 0, 0)
        _WaterJet ("Jet start and tip", Vector) = (0.055, 0.12, 0.91, 0.88)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_Atlas);
            SAMPLER(sampler_Atlas);
            CBUFFER_START(UnityPerMaterial)
                float4 _WaterRect;
                float4 _WaterFx;
                float4 _WaterJet;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = _WaterFx.x >= 4.5 && _WaterFx.x < 6.5 ? input.color : input.color * unity_SpriteColor;
                return output;
            }

            float EdgeDistance(float2 a, float2 b, float2 pixel)
            {
                float2 edge = b - a;
                float2 delta = pixel - a;
                return (edge.x * delta.y - edge.y * delta.x) / max(1.0, length(edge));
            }

            float TalismanPaper(float2 uv)
            {
                float2 pixel = float2(166.0 + uv.x * 153.0, 499.0 - uv.y * 139.0);
                float edge = EdgeDistance(float2(244, 361), float2(318, 382), pixel);
                edge = min(edge, EdgeDistance(float2(318, 382), float2(304, 420), pixel));
                edge = min(edge, EdgeDistance(float2(304, 420), float2(272, 465), pixel));
                edge = min(edge, EdgeDistance(float2(272, 465), float2(234, 498), pixel));
                edge = min(edge, EdgeDistance(float2(234, 498), float2(167, 453), pixel));
                // The inner (curved) edge of the paper is concave, so trace it by height.
                float rise = clamp(453.0 - pixel.y, 0.0, 92.0);
                float t = (sqrt(3136.0 + 144.0 * rise) - 56.0) / 72.0;
                float left = 167.0 + t * (108.0 - 31.0 * t);
                return smoothstep(-0.5, 1.5, edge) * smoothstep(-1.5, 1.3, pixel.x - left);
            }

            half4 TalismanWake(float2 uv, float phase, half alpha)
            {
                float aft = 1.0 - uv.x;
                float crest = sin(phase * 36.0 - aft * 17.0);
                float y = uv.y * 2.0 - 1.0 - crest * aft * 0.16;
                float width = pow(saturate(1.0 - aft), 0.46) * smoothstep(0.0, 0.22, aft);
                float body = (1.0 - smoothstep(width * 0.21, width + 0.04, abs(y))) *
                    (1.0 - smoothstep(0.57, 1.0, aft));
                float upper = width * (0.49 + 0.19 * crest);
                float lower = -width * (0.55 + 0.15 * sin(phase * 36.0 - aft * 21.0 + 1.6));
                float threads = (exp(-abs(y - upper) * 15.0) + exp(-abs(y - lower) * 19.0)) *
                    (0.64 + 0.36 * sin(phase * 27.0 - aft * 24.0));
                float light = saturate(body * 0.63 + threads * 0.38);
                half3 tint = lerp(half3(0.12, 0.60, 0.31), half3(0.80, 1.0, 0.55), light);
                float bounds = smoothstep(0.0, 0.07, aft) * (1.0 - smoothstep(0.71, 1.0, aft));
                return half4(tint, saturate(body + threads * width * 0.46) * alpha * bounds);
            }

            float TitanPistonMask(float2 uv)
            {
                float2 pixel = float2(uv.x, 1.0 - uv.y) * float2(330.0, 290.0);
                float y = pixel.y;
                float crown = 1.0 - smoothstep(0.94, 1.04, length((pixel - float2(155.0, 113.0)) / float2(36.0, 40.0)));
                float barrel = smoothstep(96.0, 101.0, y) * (1.0 - smoothstep(162.0, 170.0, y)) *
                    smoothstep(113.0, 119.0, pixel.x) * (1.0 - smoothstep(212.0, 217.0, pixel.x));
                float shoulder = smoothstep(147.0, 166.0, y);
                float edge = lerp(117.0, 89.0, shoulder);
                float right = lerp(216.0, 243.0, shoulder);
                float bevel = smoothstep(208.0, 237.0, y);
                edge += 62.0 * bevel;
                right -= 62.0 * bevel;
                float baseShape = shoulder * (1.0 - smoothstep(233.0, 239.0, y)) *
                    smoothstep(edge - 1.0, edge + 1.0, pixel.x) * (1.0 - smoothstep(right - 1.0, right + 1.0, pixel.x));
                return max(crown, max(barrel, baseShape));
            }

            half4 MetalDagger(float2 uv, float phase, half4 inputColor)
            {
                float2 pixel = float2(uv.x, 1.0 - uv.y) * float2(265.0, 251.0);
                const float2 axis = float2(0.692, -0.722);
                float2 delta = pixel - float2(82.0, 183.0);
                float along = dot(delta, axis);
                float side = dot(delta, float2(-axis.y, axis.x));
                float bladeWidth = lerp(23.0, 1.5, smoothstep(86.0, 172.0, along));
                float width = lerp(10.0, bladeWidth, smoothstep(53.0, 70.0, along));
                float metal = smoothstep(-10.0, -6.0, along) * (1.0 - smoothstep(170.0, 174.0, along)) *
                    (1.0 - smoothstep(width - 2.0, width + 2.0, abs(side)));
                float handle = 1.0 - smoothstep(19.0, 22.0, length(delta));
                half4 art = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, _WaterRect.xy + uv * _WaterRect.zw);
                float flow = 0.50 + 0.50 * sin(along * 0.16 - phase * 48.0);
                float bend = sin(along * 0.087 - phase * 34.0) * 5.4;
                float threads = exp(-abs(side - bend) * 0.20) + exp(-abs(side + bend + 9.0) * 0.20) * 0.53;
                float liquid = saturate(threads * (0.32 + flow * 0.60)) * smoothstep(59.0, 85.0, along);
                half3 poison = lerp(half3(0.24, 0.64, 0.04), half3(0.84, 1.0, 0.58), flow);
                return half4(lerp(art.rgb * inputColor.rgb, poison, liquid * 0.77),
                    art.a * max(metal, handle) * inputColor.a);
            }

            half4 MetalEnergy(float2 uv, float phase, half4 tint, float laser)
            {
                float x = uv.x;
                float y = uv.y * 2.0 - 1.0;
                float flare = sin(x * 34.0 - phase * 29.0);
                float distanceToAxis = abs(y - flare * (0.072 - laser * 0.035));
                float width = lerp(0.10 + x * 0.77, 0.85, laser);
                float glow = 1.0 - smoothstep(0.06, width, distanceToAxis);
                float core = exp(-distanceToAxis * lerp(7.0, 10.0, laser));
                float filaments = exp(-abs(y - 0.25 * flare) * 33.0) *
                    (0.54 + 0.46 * sin(x * 72.0 - phase * 42.0));
                float edge = smoothstep(0.0, lerp(0.40, 0.035, laser), x) * (1.0 - smoothstep(0.95, 1.0, x));
                half3 cold = lerp(tint.rgb * 0.68, half3(0.94, 1.0, 1.0), saturate(core + filaments * 0.55));
                half4 art = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, _WaterRect.xy + float2(x, saturate(uv.y + flare * 0.026)) * _WaterRect.zw);
                half artwork = art.a * laser * 0.64;
                return half4(lerp(cold, art.rgb, artwork),
                    max(artwork, saturate(glow * 0.49 + core * 0.72 + filaments * 0.43)) * edge * tint.a);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 centered = uv * 2.0 - 1.0;
                float radius = length(centered);
                float mode = _WaterFx.x;
                float phase = _WaterFx.z;
                if (mode >= 8.5 && mode < 9.5)
                    return TalismanWake(uv, phase, input.color.a);
                if (mode >= 13.5)
                    return MetalEnergy(uv, phase, input.color, step(14.5, mode));
                if (mode >= 12.5)
                    return MetalDagger(uv, phase, input.color);
                if (mode >= 10.5)
                {
                    half4 art = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, _WaterRect.xy + uv * _WaterRect.zw);
                    float mask = TitanPistonMask(uv);
                    mask = lerp(mask, 1.0 - mask, step(11.5, mode));
                    return half4(art.rgb * input.color.rgb, art.a * mask * input.color.a);
                }
                if (mode >= 9.5)
                {
                    half4 art = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, _WaterRect.xy + uv * _WaterRect.zw);
                    return half4(art.rgb * input.color.rgb, art.a * TalismanPaper(uv) * input.color.a);
                }
                if (mode >= 4.5 && mode < 6.5)
                {
                    float shape = abs(centered.x) + abs(centered.y) * 0.8;
                    float foam = 1.0 - smoothstep(0.65, 1.0, radius);
                    float crystal = 1.0 - smoothstep(0.65, 0.90, shape);
                    float edge = lerp(foam, crystal, step(5.5, mode));
                    half3 light = lerp(input.color.rgb, half3(0.85, 0.97, 1.0), exp(-radius * 4.0) * 0.7);
                    return half4(light, edge * input.color.a);
                }
                if (mode >= 3.5 && mode < 4.5)
                {
                    float ring = exp(-abs(radius - 0.79) * 64.0) + 0.25 * exp(-abs(radius - 0.79) * 14.0);
                    return half4(input.color.rgb, saturate(ring) * input.color.a);
                }

                float mask = 1.0;
                if (mode >= 6.5 && mode < 7.5)
                {
                    float2 aspect = float2(1.0, _WaterFx.y);
                    float2 start = _WaterJet.xy * aspect;
                    float2 axis = _WaterJet.zw * aspect - start;
                    float2 normal = normalize(float2(-axis.y, axis.x));
                    float bend = sin(uv.x * 16.0 - phase * 31.0) * sin(uv.x * 3.14159) * _WaterFx.w;
                    uv = (start + axis * ((uv.x - 0.07) / 0.86) + normal * (centered.y * 0.61 + bend) * aspect.y) / aspect;
                }
                else if (_WaterFx.y < 0.999)
                {
                    float sweep = mode >= 7.5 ? uv.y : mode > 2.5 ? frac(atan2(centered.x, centered.y) * 0.15915494 + 1.0) : uv.x;
                    float front = saturate(_WaterFx.y) * 1.10;
                    mask = 1.0 - smoothstep(front - 0.09, front, sweep);
                }

                if ((mode > 0.5 && mode < 3.5) || mode >= 7.5)
                {
                    float flow = uv.x * 19.0 - phase * 24.0;
                    float envelope = (1.0 - abs(centered.y)) * smoothstep(-0.05, 0.16, uv.x);
                    uv.y += (sin(flow) + 0.34 * sin(flow * 2.17 + uv.y * 13.0)) * _WaterFx.w * envelope;
                    uv.x += sin(phase * 14.0 - uv.y * 18.0) * _WaterFx.w * 0.24;
                }
                float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                half4 art = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, _WaterRect.xy + saturate(uv) * _WaterRect.zw);
                float shimmer = 1.0 + _WaterFx.w * 3.0 * sin(uv.x * 20.0 + uv.y * 11.0 - phase * 19.0);
                return half4(art.rgb * input.color.rgb * shimmer, art.a * input.color.a * mask * inside);
            }
            ENDHLSL
        }
    }
}
