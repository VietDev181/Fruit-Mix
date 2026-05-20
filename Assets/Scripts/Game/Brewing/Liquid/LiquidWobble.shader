// Optional polish shader for the liquid body / surface.
// - Animated sine ripples across the surface, amplified by "_Splash" (driven by LiquidWobble).
// - Soft vertical gradient (darker at the bottom, glossy near the top).
// - A moving specular streak for that "juicy" candy-liquid look.
//
// It is written as a plain sprite shader (like Sprites/Default), so a SpriteRenderer using it
// still responds to a SpriteMask via Unity's internal stencil handling. The gameplay does NOT
// depend on this shader — the transform-based LiquidWobble works with the default sprite material
// too. If masking misbehaves in your URP version, just use the default sprite material on the body.
Shader "FruitMix/LiquidWobble"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BottomDarken ("Bottom Darken", Range(0,1)) = 0.25
        _TopGloss ("Top Gloss", Range(0,1)) = 0.35
        _Splash ("Splash Amount", Range(0,1)) = 0
        _WaveAmp ("Wave Amplitude", Range(0,0.1)) = 0.02
        _WaveFreq ("Wave Frequency", Range(0,40)) = 14
        _WaveSpeed ("Wave Speed", Range(0,10)) = 3
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
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

            fixed4 _Color;
            float _BottomDarken;
            float _TopGloss;
            float _Splash;
            float _WaveAmp;
            float _WaveFreq;
            float _WaveSpeed;
            sampler2D _MainTex;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // Ripple the sampled X coord near the top of the sprite; strongest at the surface.
                float surfaceMask = smoothstep(0.65, 1.0, uv.y);
                float wave = sin(uv.x * _WaveFreq + _Time.y * _WaveSpeed)
                           * _WaveAmp * (0.3 + _Splash) * surfaceMask;
                uv.x += wave;

                fixed4 c = tex2D(_MainTex, uv) * IN.color;

                // Vertical gradient: darker at bottom, glossier toward the top.
                float grad = lerp(1.0 - _BottomDarken, 1.0, uv.y);
                c.rgb *= grad;

                // Soft moving gloss streak near the surface.
                float gloss = surfaceMask * _TopGloss
                            * saturate(sin(uv.x * 6.0 + _Time.y * (_WaveSpeed * 0.5)) * 0.5 + 0.5);
                c.rgb += gloss * c.a;

                c.rgb *= c.a; // premultiply for the One/OneMinusSrcAlpha blend
                return c;
            }
            ENDCG
        }
    }
}
