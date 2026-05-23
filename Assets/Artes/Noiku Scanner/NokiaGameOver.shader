Shader "Custom/NokiaGameOver"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Cor do Game Over", Color) = (1.0, 0.2, 0.2, 1.0) // Vermelho padrão erro
        _PixelCount ("Resolucao Pixels", Float) = 128
        
        // Variável de tempo manual (Obrigatório script UnscaledTimeFeeder)
        _ManualTime ("Tempo Manual", Float) = 0.0
        
        [Header(Glitch Settings)]
        _GlitchIntensity ("Intensidade do Glitch", Range(0, 0.5)) = 0.1
        _GlitchSpeed ("Velocidade do Glitch", Float) = 10.0
        
        [Header(Broken Screen)]
        _CrackSize ("Tamanho da Quebra (Canto esq)", Range(0, 1.0)) = 0.3
        _CrackJitter ("Irregularidade da Quebra", Range(0, 0.5)) = 0.1

        [Header(Old TV Effects)]
        _ScanlineIntensity ("Intensidade das Linhas", Range(0,1)) = 0.5
        _ScanlineCount ("Quantidade de Linhas", Float) = 200
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _Tint;
            float _PixelCount;
            float _ManualTime;
            
            float _GlitchIntensity;
            float _GlitchSpeed;
            
            float _CrackSize;
            float _CrackJitter;

            float _ScanlineIntensity;
            float _ScanlineCount;
            float _VignettePower;

            // Função de ruído simples
            float random (float2 uv) { return frac(sin(dot(uv,float2(12.9898,78.233)))*43758.5453123); }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. EFEITO GLITCH (Deslocamento UV horizontal)
                // Cria faixas horizontais que se movem rapidamente
                float glitchTime = floor(_ManualTime * _GlitchSpeed); // Movimento "travado"
                float noiseLine = random(float2(0, i.uv.y + glitchTime)); // Ruído por linha
                
                // Se o ruído for alto, desloca o pixel para o lado
                float displacement = 0;
                if (noiseLine > 0.95) displacement = (random(float2(i.uv.x, glitchTime)) - 0.5) * _GlitchIntensity;
                
                float2 glitchUV = i.uv;
                glitchUV.x += displacement;

                // 2. PIXELIZAÇÃO
                float2 pixelUV = floor(glitchUV * _PixelCount) / _PixelCount;
                fixed4 col = tex2D(_MainTex, pixelUV);

                // 3. EFEITO NOKIA (Luminância)
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                
                // Adiciona Scanline
                float scanline = sin((i.uv.y * _ScanlineCount) - (_ManualTime * 2.0));
                scanline = 1.0 - (_ScanlineIntensity * (scanline * 0.5 + 0.5));
                lum *= scanline;

                // Adiciona Vignette
                float2 center = i.uv - 0.5;
                float dist = length(center);
                float vignette = 1.0 - (dist * _VignettePower);
                lum *= clamp(vignette, 0.0, 1.0);

                // 4. EFEITO TELA QUEBRADA (Canto Superior Esquerdo)
                // Coordenada do Canto Superior Esquerdo no Unity UV é (0, 1)
                float distToCorner = distance(i.uv, float2(0, 1));
                
                // Adiciona ruído à borda da quebra para parecer vidro lascado
                float crackNoise = random(pixelUV * 10.0) * _CrackJitter;
                
                // Se estiver dentro da área quebrada, pinta de PRETO (Dead Pixels)
                if (distToCorner < (_CrackSize - crackNoise))
                {
                    return float4(0, 0, 0, 1);
                }

                // 5. COR FINAL
                // Aplica a cor do Game Over (Geralmente vermelho ou um verde muito escuro)
                float3 finalColor = lum * _Tint.rgb;
                
                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}