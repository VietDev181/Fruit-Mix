// Liquid sprite that clips itself to a cup-shaped mask texture — no SpriteMask component needed.
// For each pixel we map its WORLD position into the mask transform's local space, sample the mask
// alpha there, and discard the pixel if it's outside the cup shape. Because the mapping uses the
// mask's worldToLocal matrix (set per-frame by LiquidShaderClip), it stays aligned even when the
// cup tilts while drinking.
Shader "FruitMix/LiquidClipped"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _MaskTex ("Cup Mask", 2D) = "white" {}
        _MaskCutoff ("Mask Cutoff", Range(0,1)) = 0.5
        _BottomDarken ("Bottom Darken", Range(0,1)) = 0.18
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 pos   : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv    : TEXCOORD0;
                float2 muv   : TEXCOORD1; // uv into the mask texture
                float  fy    : TEXCOORD2; // 0..1 within mask height, for gradient
            };

            sampler2D _MainTex;
            fixed4 _Color;
            sampler2D _MaskTex;
            float _MaskCutoff;
            float _BottomDarken;
            float4x4 _MaskMatrix; // world -> mask local
            float4 _MaskMin;      // mask local-space min (xy)
            float4 _MaskSize;     // mask local-space size (xy)

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 lp = mul(_MaskMatrix, float4(wp, 1)).xyz;
                o.muv = (lp.xy - _MaskMin.xy) / _MaskSize.xy;
                o.fy = o.muv.y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Clip to the cup shape.
                if (i.muv.x < 0 || i.muv.x > 1 || i.muv.y < 0 || i.muv.y > 1) discard;
                if (tex2D(_MaskTex, i.muv).a < _MaskCutoff) discard;

                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                // Subtle vertical gradient (darker at the bottom of the cup).
                c.rgb *= lerp(1.0 - _BottomDarken, 1.0, saturate(i.fy));
                c.rgb *= c.a; // premultiply for One/OneMinusSrcAlpha
                return c;
            }
            ENDCG
        }
    }
}
