Shader "UI/VortexWipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Cover Color", Color) = (0.03, 0.0, 0.06, 1)
        _GlowColor ("Glow Color (deep)", Color) = (0.45, 0.1, 0.9, 1)
        _RimColor ("Glow Color (bright)", Color) = (0.96, 0.6, 1, 1)
        _Progress ("Progress (0 = open, 1 = closed)", Range(0, 1)) = 0
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)

        [Header(Streaks)]
        _AngularCells ("Streaks Around (integer)", Range(4, 24)) = 12
        _RadialFreq ("Rings Per Unit", Range(4, 40)) = 14
        _Threshold ("Streak Threshold", Range(0.3, 0.8)) = 0.56
        _Edge ("Streak Edge Softness", Range(0.01, 0.3)) = 0.07
        _Intensity ("Glow Intensity", Range(0, 3)) = 1.6

        [Header(Swirl)]
        _Twist ("Spiral Curl (turns, center to edge)", Range(0, 1.5)) = 0.45
        _Spin ("Spin (turns over the transition)", Range(-3, 3)) = 0.8

        [Header(Front)]
        _BandWidth ("Glow Band Width", Range(0.1, 0.8)) = 0.35
        _ShadeStrength ("Inner Shadow", Range(0, 1)) = 0.6
        _Ragged ("Ragged Edge", Range(0, 0.5)) = 0.2
        _Softness ("Edge Softness", Range(0.001, 0.2)) = 0.03

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]

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
            float4 _GlowColor;
            float4 _RimColor;
            float _Progress;
            float4 _Center;
            float _AngularCells;
            float _RadialFreq;
            float _Threshold;
            float _Edge;
            float _Intensity;
            float _Twist;
            float _Spin;
            float _BandWidth;
            float _ShadeStrength;
            float _Ragged;
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Value noise that tiles every `period` cells along x (the angular axis),
            // so there is no visible seam where the angle wraps around.
            float tiledNoise(float2 x, float period)
            {
                float2 i = floor(x);
                float2 f = frac(x);
                float2 u = f * f * (3.0 - 2.0 * f);

                float x0 = i.x - period * floor(i.x / period);
                float x1 = x0 + 1.0;
                x1 = x1 - period * floor(x1 / period);

                float na = hash12(float2(x0, i.y));
                float nb = hash12(float2(x1, i.y));
                float nc = hash12(float2(x0, i.y + 1.0));
                float nd = hash12(float2(x1, i.y + 1.0));

                return lerp(lerp(na, nb, u.x), lerp(nc, nd, u.x), u.y);
            }

            // Streaks: long along the angle, thin along the radius. Each octave
            // rotates at a slightly different speed, so the streaks shear and shimmer.
            float streakField(float a, float r, float rel)
            {
                float period = floor(_AngularCells + 0.5);
                float radial = _RadialFreq;
                float amp = 0.5;
                float sum = 0.0;
                float norm = 0.0;

                [unroll]
                for (int o = 0; o < 3; o++)
                {
                    float turns = _Twist * r + _Spin * _Progress * (1.0 + 0.5 * o);
                    float2 x = float2((a + turns) * period, rel * radial + o * 17.0);
                    sum += tiledNoise(x, period) * amp;
                    norm += amp;
                    amp *= 0.5;
                    period *= 2.0;
                    radial *= 2.0;
                }
                return sum / norm;
            }

            float4 frag(v2f i) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = (i.uv - _Center.xy) * float2(aspect, 1.0);

                // r = 0 at the center, 1 at the farthest screen corner.
                float2 farCorner = max(_Center.xy, 1.0 - _Center.xy) * float2(aspect, 1.0);
                float r = length(p) / length(farCorner);
                float a = atan2(p.y, p.x) / 6.28318530718 + 0.5;

                // The front sweeps from just off-screen (everything clear) to just past
                // the center (everything flat cover color). Both ends are exact.
                float reach = _BandWidth * 1.2;
                float startFront = 1.0 + _Ragged * 0.5 + reach;
                float endFront = -_Ragged * 0.5 - _Softness;
                float front0 = lerp(startFront, endFront, _Progress);

                // Noise is anchored to the front, so streaks travel in with it.
                float n = streakField(a, r, r - front0);

                // Ragged, tendril-like leading edge.
                float front = front0 + (n - 0.5) * _Ragged;
                float depth = front - r; // > 0 = inside the vortex, < 0 = covered

                float cover = smoothstep(0.0, _Softness, -depth);
                float shade = (1.0 - smoothstep(0.0, reach, depth)) * _ShadeStrength;
                float dark = max(cover, shade);

                // Glow lives only in a band just inside the front.
                float env = smoothstep(0.0, _BandWidth * 0.2, depth)
                          * (1.0 - smoothstep(_BandWidth * 0.35, _BandWidth, depth));
                float core = smoothstep(_Threshold, _Threshold + _Edge, n);
                float halo = smoothstep(_Threshold - 0.2, _Threshold + 0.02, n) * 0.4;
                float glow = saturate(max(core, halo) * env * _Intensity);

                float3 glowCol = lerp(_GlowColor.rgb, _RimColor.rgb, core);
                float3 col = lerp(_Color.rgb, glowCol, glow);

                return float4(col, max(dark, glow) * _Color.a);
            }
            ENDCG
        }
    }
}