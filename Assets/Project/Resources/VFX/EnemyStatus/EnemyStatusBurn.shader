Shader "Hidden/Game/Enemy Status Burn"
{
    Properties
    {
        _Motion ("Time, growth, opacity, pulse", Vector) = (0, 1, 1, 0)
        [HideInInspector] _SrcBlend ("Source blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination blend", Float) = 10
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Cull Off ZWrite Off
        Blend [_SrcBlend] [_DstBlend], One [_DstBlend]

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Fragment
            #include "UnityCG.cginc"

            float4 _Motion;
            const float FlameRootHeight = 0.094;

            half3 Palette(half3 color)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return color;
                #else
                    return GammaToLinearSpace(color);
                #endif
            }

            float SmoothUnion(float a, float b, float blend)
            {
                float weight = saturate(0.5 + 0.5 * (b - a) / blend);
                return lerp(b, a, weight) - blend * weight * (1.0 - weight);
            }

            float TongueDistance(float2 uv, float x, float width, float height, float offset, float lean)
            {
                float time = _Motion.x;
                float life = frac(time * 2.28 + offset + 0.043 * sin(time * 4.1 + offset * 11.0));
                float stretch = smoothstep(0.0, 0.60, life) * (1.0 - smoothstep(0.68, 1.0, life));
                height *= lerp(0.29, 1.0, stretch) * _Motion.y * (1.0 + 0.07 * _Motion.w);
                float rise = uv.y - FlameRootHeight;
                float along = saturate(rise / max(height, 0.001));

                float flow = uv.y * 12.0 - time * 15.0 + offset * 12.0;
                float curl = sin(flow) * 0.088 + sin(flow * 1.8) * 0.022;
                float center = 0.50 + (x - 0.50) * smoothstep(0.0, 0.45, along) * _Motion.y;
                center += along * along * (lean + curl) * stretch * _Motion.y;

                float taper = pow(saturate(1.0 - along), 0.68);
                float shoulder = 0.80 + along * (0.85 - along) + sin(flow) * along * 0.13;
                float footWidth = 0.067 * (1.0 - smoothstep(0.0, 0.35, along)) * lerp(0.65, 1.0, _Motion.y);
                float heel = saturate(-rise / 0.043);
                float halfWidth = width * taper * shoulder + footWidth - 0.23 * heel * heel;
                float side = abs(uv.x - center) - halfWidth;
                return max(side, max(-rise - 0.043, rise - height));
            }

            half4 Fragment(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                // Join the branches before coloring so the core and edge run through the whole flame.
                float distance = TongueDistance(uv, 0.36, 0.142, 0.60, 0.17, -0.14);
                distance = SmoothUnion(distance, TongueDistance(uv, 0.63, 0.133, 0.52, 0.42, 0.14), 0.057);
                distance = SmoothUnion(distance, TongueDistance(uv, 0.50, 0.167, 0.78, 0.92, 0.10), 0.054);

                float antialias = max(fwidth(distance), 0.005);
                float coverage = 1.0 - smoothstep(-antialias, antialias, distance);
                coverage *= smoothstep(0.053, 0.122, uv.y);
                float edge = smoothstep(0.007, 0.029, -distance);
                float rise = smoothstep(FlameRootHeight, 0.78, uv.y);
                float stream = sin(uv.y * 19.0 - _Motion.x * 19.0 + uv.x * 5.5);
                float depth = saturate(-distance * 8.2);
                float heat = depth * (0.96 - rise * 0.30 + 0.13 * _Motion.w + stream * 0.08);

                half3 color = lerp(half3(0.38, 0.052, 0.021), half3(0.98, 0.26, 0.031), edge);
                color = lerp(color, half3(1.0, 0.61, 0.035), smoothstep(0.18, 0.53, heat));
                color = lerp(color, half3(1.0, 0.91, 0.31), smoothstep(0.52, 0.91, heat));
                return half4(Palette(color), coverage * _Motion.z);
            }
            ENDCG
        }
    }
}
