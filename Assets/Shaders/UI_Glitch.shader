Shader "UI/URP/DigitalGlitchUI"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _GlitchIntensity ("Glitch Intensity", Range(0,1)) = 0.3
        _GlitchSpeed ("Glitch Speed", Range(0,10)) = 3
        _ColorOffset ("Color Separation", Range(0,0.02)) = 0.005
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            float _GlitchIntensity;
            float _GlitchSpeed;
            float _ColorOffset;
            float _NoiseStrength;

            // Simple random function
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float time = _Time.y * _GlitchSpeed;

                // Create intermittent glitch bursts
                float glitchBurst = step(0.85, frac(time));

                // Horizontal displacement
                float scanline = sin(i.uv.y * 400 + time * 10) * 0.002;
                float noise = (rand(i.uv * time) - 0.5) * _NoiseStrength * 0.02;

                float displacement = (scanline + noise) * glitchBurst * _GlitchIntensity;

                float2 uvR = i.uv + float2(_ColorOffset * glitchBurst, displacement);
                float2 uvG = i.uv + float2(0, displacement);
                float2 uvB = i.uv - float2(_ColorOffset * glitchBurst, displacement);

                float r = tex2D(_MainTex, uvR).r;
                float g = tex2D(_MainTex, uvG).g;
                float b = tex2D(_MainTex, uvB).b;
                float a = tex2D(_MainTex, i.uv).a;

                // Flicker noise
                float flicker = rand(float2(time, i.uv.y)) * glitchBurst * _NoiseStrength;

                half4 col = half4(r, g, b, a);
                col.rgb += flicker;

                return col * i.color;
            }
            ENDHLSL
        }
    }
}
