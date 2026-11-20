Shader "Game/FX/Shield Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("State Sprite", 2D) = "white" {}
        [NoScaleOffset] _IdleTex ("Idle Sprite Texture", 2D) = "black" {}
        [NoScaleOffset] _ImpactMask ("Impact Mask (R: cracks, G: flash)", 2D) = "black" {}
        [NoScaleOffset] _ShardMask ("Shard Groups (R: left, G: right, B: top)", 2D) = "black" {}
        [Enum(Cast,0,Idle,1,Charge,2,Impact,3,Shatter,4)] _State ("State", Float) = 1
        _Progress ("Progress", Range(0, 1)) = 0
        _Elapsed ("Elapsed Seconds", Float) = 0
        _Glow ("Glow Strength", Range(0, 2)) = 1
    }

    SubShader
    {
        // The masks share the whole, unrotated 512x512 UV rectangle of every sprite.
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "False"
        }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "ShieldUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex ShieldVertex
            #pragma fragment ShieldFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_IdleTex);
            TEXTURE2D(_ImpactMask);
            TEXTURE2D(_ShardMask);

            CBUFFER_START(UnityPerMaterial)
                float _State;
                float _Progress;
                float _Elapsed;
                half _Glow;
            CBUFFER_END

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            Varyings ShieldVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                SetUpSpriteInstanceProperties();
                float3 position = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(position);
                output.uv = input.uv;
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            half4 Premultiply(half4 color)
            {
                return half4(color.rgb * color.a, color.a);
            }

            half4 SampleSprite(float2 uv)
            {
                return Premultiply(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv));
            }

            half4 Over(half4 top, half4 bottom)
            {
                return top + bottom * (1.0h - top.a);
            }

            half4 SampleShardGroup(float2 uv, half3 channel)
            {
                half3 groups = SAMPLE_TEXTURE2D(_ShardMask, sampler_MainTex, uv).rgb;
                return SampleSprite(uv) * saturate(dot(groups, channel) * 2.0h);
            }

            half4 Cast(float2 uv, float progress, half4 color)
            {
                float front = lerp(0.09, 0.88, smoothstep(0.0, 0.88, progress));
                half reveal = 1.0 - smoothstep(front - 0.065, front, uv.y);
                half foot = 1.0 - smoothstep(0.17, 0.35, uv.y);
                half ignition = smoothstep(0.0, 0.11, progress);
                half footFlash = ignition * (1.0 - smoothstep(0.18, 0.61, progress));
                color *= reveal * ignition;
                color.rgb *= 1.0h + foot * footFlash * _Glow;
                return color;
            }

            half4 Shell(float2 uv, float progress, half4 color, bool charged)
            {
                float2 offset = uv - 0.5;
                float radius = length(offset);
                half rim = 1.0 - smoothstep(0.008, 0.065, abs(radius - 0.286));
                float cycle = _Elapsed * 1.3;
                float2 sweepDirection = float2(cos(cycle), sin(cycle));
                half sweep = pow(saturate(dot(offset / max(radius, 0.001), sweepDirection)), 18.0);
                color.rgb *= 1.0h + _Glow * rim * sweep * 0.32h;
                if (charged)
                {
                    half pulse = 1.0 - smoothstep(0.08, 1.0, progress);
                    float angle = atan2(offset.y, offset.x + 0.00001);
                    half twinkle = 0.55 + 0.25 * sin(angle * 17.0 + _Elapsed * 16.0)
                        + 0.20 * sin(angle * 11.0 - _Elapsed * 11.0);
                    half sparks = smoothstep(0.294, 0.32, radius);
                    color.rgb *= 1.0h + _Glow * pulse * (rim * 0.55h + sparks * twinkle * 0.75h);
                    half4 idle = Premultiply(SAMPLE_TEXTURE2D(_IdleTex, sampler_MainTex, uv));
                    idle.rgb *= 1.0h + _Glow * rim * sweep * 0.32h;
                    color = lerp(color, idle, smoothstep(0.65, 1.0, progress));
                }
                return color;
            }

            half4 Impact(float2 uv, float progress, half4 color)
            {
                const float2 impactPoint = float2(345.0 / 512.0, 1.0 - 214.0 / 512.0);
                float distanceFromHit = length(uv - impactPoint);
                half2 mask = SAMPLE_TEXTURE2D(_ImpactMask, sampler_MainTex, uv).rg;
                half crack = saturate(mask.r * 14.0h);
                half flash = saturate(mask.g * 14.0h);
                float edge = lerp(0.012, 0.26, smoothstep(0.0, 0.60, progress));
                half crackReveal = (1.0 - smoothstep(edge, edge + 0.042, distanceFromHit))
                    * (1.0 - smoothstep(0.68, 1.0, progress));
                half flashFalloff = 1.0 - smoothstep(0.03, 0.64, progress);
                half recovery = smoothstep(0.68, 1.0, progress);
                half visibility = max(max(crack * crackReveal, flash * flashFalloff),
                    (1.0h - max(crack, flash)) * (1.0h - recovery) * 0.38h);
                half4 idle = Premultiply(SAMPLE_TEXTURE2D(_IdleTex, sampler_MainTex, uv));
                color.rgb *= 1.0h + _Glow * (1.5h * mask.g * flashFalloff + 0.38h * mask.r * crackReveal);
                return lerp(idle, color, visibility);
            }

            half4 Shatter(float2 uv, float progress)
            {
                float distance = progress * (1.22 - 0.22 * progress);
                float fall = 0.050 * progress * progress;
                half4 left = SampleShardGroup(uv - float2(-0.120 * distance, -0.022 * distance - fall),
                    half3(1, 0, 0));
                half4 right = SampleShardGroup(uv - float2(0.125 * distance, 0.021 * distance - fall),
                    half3(0, 1, 0));
                half4 top = SampleShardGroup(uv - float2(0.022 * distance, 0.132 * distance - fall),
                    half3(0, 0, 1));
                left *= 1.0h - smoothstep(0.38, 0.94, progress);
                right *= 1.0h - smoothstep(0.47, 1.0, progress);
                top *= 1.0h - smoothstep(0.30, 0.89, progress);
                half4 color = Over(top, Over(right, left));
                color.rgb *= 1.0h + _Glow * (1.0 - smoothstep(0.0, 0.38, progress)) * 0.52;
                return color;
            }

            half4 ShieldFragment(Varyings input) : SV_Target
            {
                float progress = saturate(_Progress);
                half4 color;
                if (_State >= 3.5)
                {
                    color = Shatter(input.uv, progress);
                }
                else
                {
                    color = SampleSprite(input.uv);
                    if (_State < 0.5)
                        color = Cast(input.uv, progress, color);
                    else if (_State < 2.5)
                        color = Shell(input.uv, progress, color, _State >= 1.5);
                    else
                        color = Impact(input.uv, progress, color);
                }
                return color * half4(input.color.rgb * input.color.a, input.color.a);
            }
            ENDHLSL
        }
    }
}
