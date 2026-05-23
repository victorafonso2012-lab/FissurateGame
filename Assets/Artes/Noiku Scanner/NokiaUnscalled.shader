Shader "Custom/NokiaAdvancedUnscaled"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Cor do Scanner", Color) = (0.0, 1.0, 0.2, 1.0)
        _PixelCount ("Resolucao Pixels", Float) = 128
        
        // Esta é a variável mágica que o script vai controlar
        _ManualTime ("Tempo Manual", Float) = 0.0
        
        _ScanlineIntensity ("Intensidade Scanline", Range(0,1)) = 0.5
        _ScanlineCount ("Qtd Scanlines", Float) = 200
        _ScanlineSpeed ("Velocidade Scanlines", Float) = 2.0
        _NoiseAmount ("Chiado Noise", Range(0, 1.0)) = 0.05
        _VignettePower ("Vignette", Range(0, 3)) = 1.2
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _Tint;
            float _PixelCount;
            float _ManualTime; // Recebe o tempo do script
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _ScanlineSpeed;
            float _NoiseAmount;
            float _VignettePower;

            float random (float2 uv) { return frac(sin(dot(uv,float2(12.9898,78.233)))*43758.5453123); }

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 pixelUV = floor(i.uv * _PixelCount) / _PixelCount;
                fixed4 col = tex2D(_MainTex, pixelUV);
                
                // Conversão para monocromático Nokia
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                
                // ANIMAÇÃO: Usamos _ManualTime em vez de _Time.y
                float noise = random(pixelUV + _ManualTime) * _NoiseAmount;
                lum += noise;

                float scanline = sin((i.uv.y * _ScanlineCount) - (_ManualTime * _ScanlineSpeed));
                scanline = 1.0 - (_ScanlineIntensity * (scanline * 0.5 + 0.5));
                lum *= scanline;

                float2 center = i.uv - 0.5;
                float dist = length(center);
                float vignette = 1.0 - (dist * _VignettePower);
                lum *= clamp(vignette, 0.0, 1.0);

                return float4(lum * _Tint.rgb, 1.0);
            }
            ENDCG
        }
    }
}