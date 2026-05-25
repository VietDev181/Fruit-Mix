Shader "Custom/LiquidFill"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Fill ("Fill", Range(0,1)) = 0.5
        _TiltSlope ("Tilt Slope", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Fill;
            float _TiltSlope;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Fill line: UV.y threshold shifts left/right based on tilt slope.
                // fillLine = fill + slope * (uv.x - 0.5)
                // Pixels above the fill line are discarded (transparent).
                float fillLine = _Fill + _TiltSlope * (i.uv.x - 0.5);
                clip(fillLine - i.uv.y);
                return i.color * tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
