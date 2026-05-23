Shader "Custom/NokiaAdvanced"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Cor do Scanner", Color) = (0.0, 1.0, 0.2, 1.0)
        _PixelCount ("Resolucao Pixels", Float) = 128
        
        [Header(Color Keying)]
        _TargetColor ("Cor Alvo Fantasma", Color) = (0, 0.847, 1, 1)
        _ColorTolerance ("Tolerancia de Cor", Range(0, 1)) = 0.2
        _SpectralIntensity ("Intensidade do Brilho", Range(0, 2)) = 1.2

        [Header(Old TV Effects)]
        _ScanlineIntensity ("Intensidade das Linhas", Range(0,1)) = 0.5
        _ScanlineCount ("Quantidade de Linhas", Float) = 200
        _ScanlineSpeed ("Velocidade das Linhas", Float) = 2.0
        
        [Header(Distortion)]
        _NoiseAmount ("Chiado Noise", Range(0,0.2)) = 0.05
        _VignettePower ("Escurecer Bordas", Range(0, 3)) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "PreviewType"="Plane" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Tint;
            float _PixelCount;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _ScanlineSpeed;
            float _NoiseAmount;
            float _VignettePower;
            
            float4 _TargetColor;
            float _ColorTolerance;
            float _SpectralIntensity;

            float random (float2 uv)
            {
                return frac(sin(dot(uv,float2(12.9898,78.233)))*43758.5453123);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 pixelUV = floor(i.uv * _PixelCount) / _PixelCount;
                fixed4 col = tex2D(_MainTex, pixelUV);

                // --- FILTRO DE COR ---
                float diferencaCor = distance(col.rgb, _TargetColor.rgb);
                float isSpectral = 1.0 - smoothstep(_ColorTolerance, _ColorTolerance + 0.1, diferencaCor);

                // Processo Nokia
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float noise = random(pixelUV + _Time.y) * _NoiseAmount;
                lum += noise;

                float scanline = sin((i.uv.y * _ScanlineCount) - (_Time.y * _ScanlineSpeed));
                scanline = 1.0 - (_ScanlineIntensity * (scanline * 0.5 + 0.5));
                lum *= scanline;

                float2 center = i.uv - 0.5;
                float dist = length(center);
                float vignette = 1.0 - (dist * _VignettePower);
                lum *= clamp(vignette, 0.0, 1.0);

                float3 corNokia = lum * _Tint.rgb;

                // Mix final
                float3 corFinal = lerp(corNokia, col.rgb * _SpectralIntensity, isSpectral);

                return float4(corFinal, 1.0);
            }
            ENDCG
        }
    }
}