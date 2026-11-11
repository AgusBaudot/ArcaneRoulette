Shader "UI/SpiralWipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Cover Color", Color) = (0.05, 0.0, 0.1, 1)
        _RimColor ("Rim Color", Color) = (0.96, 0.6, 1, 1)
        _Progress ("Progress (0 = open, 1 = closed)", Range(0, 1)) = 0
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Arms ("Arms", Range(1, 8)) = 4
        _Twist ("Twist", Range(0, 8)) = 2.5
        _Spin ("Spin (turns over the transition)", Range(-4, 4)) = 1
        _ArmSpread ("Arm Spread", Range(0, 1)) = 0.6
        _Softness ("Edge Softness", Range(0.001, 0.2)) = 0.02
        _RimWidth ("Rim Width", Range(0, 0.2)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float4 _RimColor;
            float _Progress;
            float4 _Center;
            float _Arms;
            float _Twist;
            float _Spin;
            float _ArmSpread;
            float _Softness;
            float _RimWidth;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Aspect-corrected position relative to the center.
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = (i.uv - _Center.xy) * float2(aspect, 1.0);

                // r = 0 at the center, 1 at the farthest screen corner.
                float2 farCorner = max(_Center.xy, 1.0 - _Center.xy) * float2(aspect, 1.0);
                float r = length(p) / length(farCorner);

                // Angle in 0..1.
                float a = atan2(p.y, p.x) / 6.28318530718 + 0.5;

                // Spiral arm coordinate. Integer arm count keeps the angle seam continuous.
                float arms = floor(_Arms + 0.5);
                float k = frac(a * arms + r * _Twist - _Progress * _Spin);

                // Covered when r is beyond a threshold that shrinks with progress.
                // The extra travel guarantees full coverage (and no rim) at progress = 1.
                float travel = 1.0 + _ArmSpread + _Softness + _RimWidth;
                float threshold = 1.0 - _Progress * travel + k * _ArmSpread;
                float d = r - threshold;

                float cover = smoothstep(0.0, _Softness, d);
                float rim = cover * (1.0 - smoothstep(0.0, _RimWidth, d));

                float3 col = lerp(_Color.rgb, _RimColor.rgb, rim);
                return float4(col, cover * _Color.a);
            }
            ENDCG
        }
    }
}