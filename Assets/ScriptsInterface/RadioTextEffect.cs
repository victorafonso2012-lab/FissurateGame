using UnityEngine;
using TMPro;

public class RadioTextEffect : MonoBehaviour
{
    [Header("Configuração do Sinal")]
    [Tooltip("O quanto as letras se separam (em pixels virtuais)")]
    public float intensidadeTremor = 5.0f;

    [Tooltip("Velocidade da troca de posição (stutter)")]
    public float velocidadeRuido = 12.0f;

    [Tooltip("Chance da letra sumir (0.0 a 1.0)")]
    [Range(0f, 1f)]
    public float falhaSinal = 0.2f;

    [Header("Pixel Perfect")]
    [Tooltip("Se marcado, o tremor 'trava' em posições fixas, evitando borrão")]
    public bool travarNosPixels = true;

    [Tooltip("Tamanho do 'pixel' do tremor. Aumente se o canvas for muito grande.")]
    public float tamanhoDoPixel = 1.0f;

    private TMP_Text textoTMP;

    void Awake()
    {
        textoTMP = GetComponent<TMP_Text>();
    }

    void Update()
    {
        textoTMP.ForceMeshUpdate();
        var textInfo = textoTMP.textInfo;

        // Se o texto estiver vazio, não faz nada
        if (textInfo.characterCount == 0) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            var verts = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
            var colors = textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;

            // --- CÁLCULO DO RUÍDO ---
            // Usamos um 'Step' no tempo para o ruído não ser contínuo, mas 'framerate baixo'
            float tempoTravado = Mathf.Floor(Time.time * velocidadeRuido);

            float ruidoX = Mathf.PerlinNoise(tempoTravado, i * 1.5f) - 0.5f;
            float ruidoY = Mathf.PerlinNoise(tempoTravado + 50f, i * 1.5f) - 0.5f;

            Vector3 offset = new Vector3(ruidoX, ruidoY, 0) * intensidadeTremor;

            // --- PIXEL SNAP (O Segredo da Legibilidade) ---
            if (travarNosPixels)
            {
                // Arredonda o movimento para o múltiplo mais próximo do 'tamanhoDoPixel'
                offset.x = Mathf.Round(offset.x / tamanhoDoPixel) * tamanhoDoPixel;
                offset.y = Mathf.Round(offset.y / tamanhoDoPixel) * tamanhoDoPixel;
            }

            // --- FALHA DE SINAL (Alpha) ---
            // Usa o mesmo tempo travado para piscar em sincronia com o movimento
            float sinalRuim = Mathf.PerlinNoise(Time.time * 5.0f, i * 0.2f);
            byte alpha = 255;

            if (sinalRuim < falhaSinal)
            {
                // Em vez de transparência suave, usamos 0 ou 255 (ligado/desligado) para ficar nítido
                alpha = (Random.value > 0.5f) ? (byte)0 : (byte)255;
            }

            // Aplica nos vértices
            for (int j = 0; j < 4; j++)
            {
                var orig = verts[charInfo.vertexIndex + j];
                verts[charInfo.vertexIndex + j] = orig + offset;

                // Aplica cor
                Color32 corOriginal = colors[charInfo.vertexIndex + j];
                // Mantém a cor original do texto, muda só o Alpha
                colors[charInfo.vertexIndex + j] = new Color32(corOriginal.r, corOriginal.g, corOriginal.b, alpha);
            }
        }

        // Atualiza a malha
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            meshInfo.mesh.colors32 = meshInfo.colors32;
            textoTMP.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}