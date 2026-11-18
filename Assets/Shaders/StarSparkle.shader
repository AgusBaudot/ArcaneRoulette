Shader "UI/StarSparkle"
{
    Properties
    {
        _Intensity ("Intensity", Float) = 2
        _StarPower ("Star Power", Float) = 1.2
        _GlowSize ("Glow Falloff", Float) = 20
        _GlowStrength ("Glow Strength", Float) = 0.9
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            float _Intensity, _StarPower, _GlowSize, _GlowStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2;
                float2 a = sqrt(abs(p));
                float star = pow(saturate(1 - (a.x + a.y)), _StarPower);
                float glow = exp(-_GlowSize * dot(p, p)) * _GlowStrength;
                float mask = saturate(star + glow);
                return fixed4(i.color.rgb * _Intensity, i.color.a * mask);
            }
            ENDCG
        }
    }
}