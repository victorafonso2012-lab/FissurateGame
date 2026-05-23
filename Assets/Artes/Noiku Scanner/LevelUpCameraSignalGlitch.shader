Shader "Hidden/Fissurate/LevelUpCameraSignalGlitch"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.96, 0.28, 0.34, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.18
        _DistortionStrength ("Distortion Strength", Range(0, 0.12)) = 0.032
        _WaveStrength ("Wave Strength", Range(0, 0.08)) = 0.018
        _WaveDensity ("Wave Density", Range(1, 120)) = 34
        _WaveScrollSpeed ("Wave Scroll Speed", Range(0, 40)) = 16
        _CurveScrollSpeed ("Curve Scroll Speed", Range(0, 40)) = 11
        _BandFrequency ("Band Frequency", Range(1, 40)) = 14
        _BandScrollSpeed ("Band Scroll Speed", Range(0, 30)) = 7
        _BandShift ("Band Shift", Range(0, 0.12)) = 0.022
        _BlockStrength ("Block Strength", Range(0, 0.12)) = 0.016
        _RgbSplit ("RGB Split", Range(0, 0.03)) = 0.0035
        _NoiseStrength ("Noise Strength", Range(0, 0.12)) = 0.018
        _PixelMix ("Pixel Mix", Range(0, 1)) = 0.18
        _PixelDensity ("Pixel Density", Range(24, 260)) = 110
        _PixelTintStrength ("Pixel Tint Strength", Range(0, 1)) = 0.32
        _PixelJitter ("Pixel Jitter", Range(0, 0.03)) = 0.006
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend One Zero

        Pass
        {
            Name "LevelUpCameraSignalGlitch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 _TintColor;
            float _TintStrength;
            float _DistortionStrength;
            float _WaveStrength;
            float _WaveDensity;
            float _WaveScrollSpeed;
            float _CurveScrollSpeed;
            float _BandFrequency;
            float _BandScrollSpeed;
            float _BandShift;
            float _BlockStrength;
            float _RgbSplit;
            float _NoiseStrength;
            float _PixelMix;
            float _PixelDensity;
            float _PixelTintStrength;
            float _PixelJitter;

            float _LevelUpCameraGlitchIntensity;
            float _LevelUpCameraGlitchTime;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float intensity = saturate(_LevelUpCameraGlitchIntensity);
                float2 uv = input.texcoord.xy;

                if (intensity <= 0.0001)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float t = _LevelUpCameraGlitchTime;

                float baseWave = sin((uv.y * _WaveDensity) + (t * _WaveScrollSpeed));
                float curveWave = sin(((uv.y * (_WaveDensity * 0.55)) - (t * _CurveScrollSpeed)) + sin((uv.x * 6.0) + (t * 3.0)) * 2.4);
                float bandIndex = floor((uv.y * _BandFrequency) + (t * _BandScrollSpeed));
                float bandRandom = Hash21(float2(bandIndex, floor(t * 10.0) + 3.7));
                float bandMask = smoothstep(0.74, 1.0, bandRandom);
                float bandShift = (bandRandom - 0.5) * _BandShift * intensity * bandMask;

                float tearIndex = floor((uv.y + (t * 0.07)) * (_BandFrequency * 1.9));
                float tearMask = step(0.91, Hash21(float2(tearIndex + 17.0, floor(t * 13.0))));
                float tearShift = (Hash21(float2(tearIndex + 5.1, floor(t * 21.0))) - 0.5) * _BlockStrength * intensity * tearMask;

                float2 blockUv = float2(floor(uv.x * 38.0) / 38.0, floor(uv.y * 22.0) / 22.0);
                float blockRandom = Hash21(blockUv * 41.0 + float2(floor(t * 12.0), 8.3));
                float blockMask = step(0.955, blockRandom);
                float blockShift = (blockRandom - 0.5) * _BlockStrength * 2.0 * intensity * blockMask;

                float xOffset = ((baseWave * 0.65) + (curveWave * 0.35)) * _DistortionStrength * intensity;
                float yOffset = sin((uv.x * 18.0) - (t * 9.0)) * (_WaveStrength * 0.18) * intensity;

                float2 warpedUv = uv;
                warpedUv.x += xOffset + bandShift + tearShift + blockShift;
                warpedUv.y += yOffset + sin((uv.x * 5.5) + (t * 4.0)) * _WaveStrength * 0.1 * intensity;
                warpedUv = clamp(warpedUv, 0.001, 0.999);

                float split = (_RgbSplit + (abs(tearShift) * 0.35)) * intensity;
                float2 chromaOffset = float2(split, 0.0);

                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, clamp(warpedUv + chromaOffset, 0.001, 0.999)).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warpedUv).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, clamp(warpedUv - chromaOffset, 0.001, 0.999)).b;
                half3 color = half3(r, g, b);

                float pixelMix = saturate(_PixelMix) * intensity;
                float aspectRatio = max(_ScreenParams.x / max(_ScreenParams.y, 1.0), 0.0001);
                float2 pixelGrid = float2(_PixelDensity, max(1.0, _PixelDensity / aspectRatio));
                float2 pixelUv = floor(warpedUv * pixelGrid) / pixelGrid;
                float pixelNoise = Hash21((pixelUv * pixelGrid) + float2(floor(t * 18.0), 4.6));
                float2 pixelOffset = float2((pixelNoise - 0.5) * _PixelJitter, (0.5 - pixelNoise) * _PixelJitter) * intensity;
                float2 shiftedPixelUv = clamp(pixelUv + pixelOffset, 0.001, 0.999);

                half pixelR = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, clamp(shiftedPixelUv + chromaOffset * 1.8, 0.001, 0.999)).r;
                half pixelG = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, shiftedPixelUv).g;
                half pixelB = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, clamp(shiftedPixelUv - chromaOffset * 1.8, 0.001, 0.999)).b;
                half3 pixelColor = half3(pixelR, pixelG, pixelB);
                half3 pixelTint = lerp(pixelColor, pixelColor + (_TintColor.rgb * (0.25 + pixelNoise * 0.35)), _PixelTintStrength);
                color = lerp(color, pixelTint, pixelMix * (0.55 + bandMask * 0.25 + tearMask * 0.2));

                float grain = Hash21((uv + t) * float2(_ScreenParams.x * 0.14, _ScreenParams.y * 0.08) + float2(t * 29.0, -t * 11.0));
                color += (grain - 0.5) * (_NoiseStrength * intensity);
                color *= 0.98 - (abs(baseWave) * 0.03 * intensity);

                half luminance = dot(color, half3(0.299h, 0.587h, 0.114h));
                half3 tinted = color + (_TintColor.rgb * luminance * _TintStrength * intensity);
                color = lerp(color, tinted, saturate((_TintStrength * 0.75) + (bandMask * 0.18)) * intensity);

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }
}
