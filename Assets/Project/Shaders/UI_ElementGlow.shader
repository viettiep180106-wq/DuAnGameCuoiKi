Shader "Custom/UI_ElementGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,0.8,0.2,1)
        _GlowIntensity ("Glow Intensity", Range(0.5, 3.0)) = 1.5
        _PulseSpeed ("Pulse Speed", Range(0.5, 6.0)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _PulseSpeed;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                // Hiệu ứng nhịp thở viền hào quang
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                float borderDist = min(min(i.texcoord.x, 1.0 - i.texcoord.x), min(i.texcoord.y, 1.0 - i.texcoord.y));
                float glow = (1.0 - smoothstep(0.0, 0.15, borderDist)) * pulse * _GlowIntensity;

                col.rgb += _GlowColor.rgb * glow * col.a;

                return col;
            }
            ENDCG
        }
    }
}

