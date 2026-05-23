Shader "UI/LevelUpTVStatic"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 0.45, 0.5, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0
        _OverlayOpacity ("Overlay Opacity", Range(0, 1)) = 0
        _NoiseContrast ("Noise Contrast", Range(0.2, 4)) = 0.85
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.12
        _LineDensity ("Line Density", Range(20, 500)) = 110
        _FlickerSpeed ("Flicker Speed", Range(0, 40)) = 10
        _PixelBlocks ("Pixel Blocks", Range(48, 280)) = 136
        _BandJitter ("Band Jitter", Range(0, 0.15)) = 0.02
        _RgbSplit ("RGB Split", Range(0, 0.08)) = 0.004
        _AccentMix ("Accent Mix", Range(0, 1)) = 0.26
        _ManualTime ("Manual Time", Float) = 0
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
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Intensity;
            float _OverlayOpacity;
            float _NoiseContrast;
            float _ScanlineStrength;
            float _LineDensity;
            float _FlickerSpeed;
            float _PixelBlocks;
            float _BandJitter;
            float _RgbSplit;
            float _AccentMix;
            float _ManualTime;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 grid = float2(_PixelBlocks, _PixelBlocks * 0.5625);

                float bandSeed = floor(i.uv.y * max(10.0, _LineDensity * 0.18));
                float bandMask = step(0.86, Hash21(float2(bandSeed, floor(_ManualTime * (_FlickerSpeed * 1.1)))));
                float bandShift = (Hash21(float2(bandSeed + 4.7, floor(_ManualTime * (_FlickerSpeed * 1.6)))) - 0.5) * _BandJitter * _Intensity;

                float tearMask = step(0.94, Hash21(float2(floor(_ManualTime * 7.0), floor(i.uv.y * 18.0))));
                float tearShift = (Hash21(float2(floor(_ManualTime * 9.0), 3.1)) - 0.5) * (_BandJitter * 2.0) * tearMask * _Intensity;

                float2 glitchUv = i.uv;
                glitchUv.x = frac(glitchUv.x + bandShift * bandMask + tearShift);

                float2 pixelUv = floor(glitchUv * grid) / grid;
                float split = _RgbSplit * (0.35 + _Intensity);

                float r = Hash21(frac(pixelUv + float2(split, 0)) * (3.2 + _NoiseContrast) + float2(_ManualTime * 8.0, 1.3));
                float g = Hash21(pixelUv * (3.5 + _NoiseContrast) + float2(_ManualTime * 9.5, 6.2));
                float b = Hash21(frac(pixelUv - float2(split, 0)) * (3.8 + _NoiseContrast) + float2(_ManualTime * 11.0, 9.7));

                float mono = dot(float3(r, g, b), float3(0.3333, 0.3333, 0.3333));
                mono = floor(mono * 4.0) / 3.0;

                float signalLoss = step(0.965, Hash21(pixelUv * 27.0 + float2(floor(_ManualTime * 10.0), 2.2)));
                float whiteBand = step(0.9, Hash21(float2(floor(i.uv.y * 220.0), floor(_ManualTime * 14.0)))) * bandMask;

                float3 accentA = float3(1.0, 0.92, 0.94);
                float3 accentB = float3(1.0, 0.24, 0.34);
                float3 accent = lerp(accentA, accentB, _AccentMix);

                float3 glitchColor = mono.xxx * accent;
                glitchColor = lerp(glitchColor, float3(1.0, 0.82, 0.86), whiteBand * 0.55);
                glitchColor = lerp(glitchColor, float3(0.12, 0.02, 0.04), signalLoss * 0.55);

                float scanline = sin((i.uv.y * _LineDensity) + (_ManualTime * _FlickerSpeed * 6.28318));
                scanline = 1.0 - (_ScanlineStrength * (scanline * 0.5 + 0.5));
                glitchColor *= scanline;
                glitchColor *= i.color.rgb;

                float flicker = 0.92 + 0.08 * sin(_ManualTime * _FlickerSpeed * 3.14159);
                float alphaBase = 0.22 + bandMask * 0.24 + tearMask * 0.18 + signalLoss * 0.16 + mono * 0.12;
                float alpha = saturate(_OverlayOpacity * (0.55 + alphaBase * 0.85)) * flicker * i.color.a;

                return float4(glitchColor, alpha);
            }
            ENDCG
        }
    }
}
