Shader "Game/FX/Enemy Fire Advanced Preview"
{
    Properties
    {
        _Atlas ("Atlas", 2D) = "white" {}
        _AtlasRect ("Atlas rectangle", Vector) = (0, 0, 1, 1)
        _AdvancedEffect ("Mode, phase, particle UV, pulse", Vector) = (0, 0, 0, 0)
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

            TEXTURE2D(_Atlas);
            SAMPLER(sampler_Atlas);
            CBUFFER_START(UnityPerMaterial)
                float4 _AtlasRect;
                float4 _AdvancedEffect;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 particleTime : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv.xy;
                output.particleTime = input.uv.zw;
                output.color = _AdvancedEffect.z > 0.5 ? input.color : input.color * unity_SpriteColor;
                return output;
            }

            half4 AtlasSample(float2 uv)
            {
                half4 art = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, _AtlasRect.xy + saturate(uv) * _AtlasRect.zw);
                float2 interior = smoothstep(0.0, 0.022, uv) * (1.0 - smoothstep(0.978, 1.0, uv));
                art.a *= interior.x * interior.y;
                return art;
            }

            half4 AttackTrail(float2 uv, float time, float opacity, bool blue)
            {
                float along = uv.x;
                float tip = smoothstep(0.12, 0.88, along);
                float flow = along * 17.0 - time * 35.0;
                float bend = (sin(flow) * 0.12 + sin(flow * 2.08) * 0.055) * (1.0 - tip * 0.8);
                float across = uv.y * 2.0 - 1.0 - bend;
                float edge = 1.0 - smoothstep(0.53, 1.0, abs(across));
                float core = exp(-abs(across + 0.06 * sin(flow)) * (4.0 + 2.5 * tip));
                float filament = 1.0 - smoothstep(0.028, 0.095, abs(across - 0.37 - sin(flow * 1.44) * 0.13));
                half3 rim = blue ? half3(0.017, 0.32, 0.76) : half3(0.63, 0.063, 0.005);
                half3 hot = blue ? half3(0.58, 1.0, 1.0) : half3(1.0, 0.74, 0.30);
                float heat = saturate(core * 0.92 + filament * 0.45 + tip * 0.28);
                float alpha = edge * smoothstep(0.0, 0.23, along) * (1.0 - smoothstep(0.96, 1.0, along));
                return half4(lerp(rim, hot, heat), alpha * opacity * (0.61 + 0.39 * tip));
            }

            half4 ImpactFlash(float2 uv, float phase, half4 tint)
            {
                half4 art = AtlasSample(uv);
                float radius = length(uv * 2.0 - 1.0);
                float flash = (1.0 - smoothstep(0.02, 0.36, radius)) * (1.0 - smoothstep(0.0, 0.54, phase));
                float veins = smoothstep(0.40, 0.87, art.g) * art.a * (1.0 - phase);
                return half4(half3(0.37, 0.92, 1.0) * tint.rgb, max(flash, veins * 0.65) * tint.a);
            }

            half4 ImpactSpark(float2 uv, half4 tint)
            {
                float2 p = (uv * 2.0 - 1.0) * float2(1.0, 2.9);
                float lengthwise = 1.0 - smoothstep(0.08, 1.0, abs(p.x));
                float sharp = 1.0 - smoothstep(0.13, 0.67, abs(p.y) / max(0.18, lengthwise));
                half3 color = lerp(half3(0.0, 0.45, 1.0), half3(0.79, 1.0, 1.0), sharp);
                return half4(color * tint.rgb, sharp * lengthwise * tint.a);
            }

            half4 MeteorFlame(float2 uv, float time, half4 tint)
            {
                const float2 axis = float2(0.7071068, 0.7071068);
                const float2 across = float2(-0.7071068, 0.7071068);
                float2 relative = uv - float2(0.40, 0.35);
                float along = dot(relative, axis);
                float side = dot(relative, across);
                float tail = smoothstep(0.16, 0.57, along);
                float flow = along * 22.0 - time * 31.0;
                float flicker = sin(flow + side * 12.0) + 0.47 * sin(flow * 1.77 - side * 19.0);
                float2 displaced = uv + across * (flicker * tail * 0.073)
                    + axis * (sin(flow * 0.71) * tail * 0.049);
                half4 art = AtlasSample(displaced);
                float streak = saturate(sin(flow + side * 34.0));
                float brightness = 1.0 + tail * (0.13 + 0.39 * streak);
                return half4(art.rgb * tint.rgb * brightness, art.a * tint.a);
            }

            half4 SwingArtwork(float2 uv, float phase, float pulse, float mode, half4 tint)
            {
                float paintedDistance;
                if (mode > 11.5)
                {
                    paintedDistance = saturate(dot(uv - float2(0.045, 0.065), float2(0.55, 0.55)));
                }
                else
                {
                    bool magma = mode > 10.5;
                    float2 hub = magma ? float2(0.48, 0.51) : float2(0.48, 0.53);
                    float2 radial = uv - hub;
                    float leadAngle = magma ? 1.16 : 0.44;
                    paintedDistance = frac((leadAngle - atan2(radial.y, radial.x)) * 0.15915494 + 1.0);
                }
                float front = 0.11 + 1.05 * phase;
                float reveal = 1.0 - smoothstep(front - 0.095, front, paintedDistance);
                half4 art = AtlasSample(uv);
                float edgeLight = (1.0 - smoothstep(0.025, 0.14, abs(front - paintedDistance))) * 0.25;
                return half4(art.rgb * tint.rgb * (1.0 + pulse * 0.43 + edgeLight),
                    art.a * tint.a * reveal);
            }

            float Lightning(float2 uv, float phase)
            {
                float clock = floor(phase * 40.0);
                float tip = smoothstep(0.38, 0.57, uv.y) * (1.0 - smoothstep(0.87, 0.99, uv.y));
                float branches = 0.0;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    float jitter = frac(sin(clock * 123.2 + i * 74.9) * 1583.19);
                    float stem = 0.23 + i * 0.27 + (jitter - 0.5) * 0.12;
                    float jag = abs(frac(uv.y * (4.8 + i * 0.65) + jitter) - 0.5);
                    float path = stem + (0.08 + jitter * 0.10) * jag + (uv.y - 0.45) * (i - 1) * 0.16;
                    branches = max(branches, (1.0 - smoothstep(0.004, 0.015, abs(uv.x - path))) * jitter);
                }
                return branches * tip * (1.0 - smoothstep(0.09, 0.25, phase));
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = _AdvancedEffect.z > 0.5 ? input.uv : (input.uv - _AtlasRect.xy) / _AtlasRect.zw;
                float mode = _AdvancedEffect.x;
                float phase = _AdvancedEffect.y;
                float pulse = _AdvancedEffect.w;
                float2 sampleUV = uv;
                float light = 1.0 + pulse * 0.72;

                if (mode > 9.5)
                    return SwingArtwork(uv, phase, pulse, mode, input.color);
                if (mode > 8.5)
                    return MeteorFlame(uv, phase + input.particleTime.x * 0.27 + input.particleTime.y * 0.53, input.color);
                if (mode > 7.5)
                    return ImpactSpark(uv, input.color);
                if (mode > 5.5)
                    return AttackTrail(uv, phase, pulse, mode > 6.5);
                if (mode > 4.5)
                    return ImpactFlash(uv, phase, input.color);

                if (mode > 1.5 && mode < 2.5)
                {
                    float airflow = (uv.x - uv.y) * 5.2 - phase * 27.0;
                    float stretch = smoothstep(-0.65, 0.75, uv.x - uv.y);
                    sampleUV.y += (sin(airflow) + 0.52 * sin(uv.x * 21.0 - phase * 38.0))
                        * (0.010 + 0.042 * stretch);
                    sampleUV.x += sin(uv.y * 19.0 + phase * 26.0) * stretch * 0.028;
                    light += 0.44 * saturate(sin(airflow + uv.y * 8.0));
                }
                else if (mode > 2.5)
                {
                    float flicker = sin(uv.x * 20.0 + uv.y * 7.0 - phase * 28.0);
                    sampleUV.x += (flicker + 0.45 * sin(uv.y * 19.0 - phase * 39.0)) * uv.y * 0.033;
                    sampleUV.y += sin(uv.x * 12.0 - phase * 22.0) * uv.y * 0.037;
                    light += 0.24 * saturate(flicker);
                }

                half4 art = AtlasSample(sampleUV);
                if (mode > 3.5)
                {
                    float arcs = Lightning(uv, phase);
                    art.rgb = lerp(art.rgb, half3(0.58, 0.96, 1.0), arcs);
                    art.a = max(art.a, arcs);
                }
                return half4(art.rgb * input.color.rgb * light, art.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
