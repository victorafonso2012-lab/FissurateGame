using UnityEngine;
using TMPro;

public class GhostTextEffect : MonoBehaviour
{
    [Header("Movimento (Fumaça)")]
    [Tooltip("Velocidade da ondulação.")]
    public float velocidadeOnda = 2.0f;

    [Tooltip("Altura da onda (o quanto sobe e desce).")]
    public float alturaOnda = 5.0f;

    [Tooltip("Frequência da onda (quanto menor, mais larga a curva).")]
    public float frequenciaOnda = 0.1f;

    [Header("Opacidade (Fantasma)")]
    [Tooltip("Velocidade do piscar (Fade In/Out).")]
    public float velocidadeFade = 1.0f;

    [Tooltip("Opacidade mínima (0 = invisível, 255 = sólido).")]
    [Range(0, 255)]
    public float alphaMinimo = 50.0f;

    [Tooltip("Opacidade máxima.")]
    [Range(0, 255)]
    public float alphaMaximo = 255.0f;

    [Tooltip("Se marcado, cada letra pisca em tempos diferentes.")]
    public bool fadeAssincrono = true;

    private TMP_Text textoTMP;

    void Awake()
    {
        textoTMP = GetComponent<TMP_Text>();
    }

    void Update()
    {
        textoTMP.ForceMeshUpdate();
        var textInfo = textoTMP.textInfo;

        // Se não tiver texto, retorna para não dar erro
        if (textInfo.characterCount == 0) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            // Se o caractere for invisível (espaço, quebra de linha), pula
            if (!charInfo.isVisible) continue;

            var verts = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
            var colors = textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;

            // --- CÁLCULO DA FUMAÇA (MOVIMENTO) ---
            // Usamos Seno (Sin) + Cosseno (Cos) para criar um movimento circular/ondulatório orgânico
            // Usamos 'i' (índice da letra) para que cada letra se mova levemente desencontrada da outra
            float offsetY = Mathf.Sin(Time.time * velocidadeOnda + i * frequenciaOnda) * alturaOnda;
            float offsetX = Mathf.Cos(Time.time * velocidadeOnda * 0.5f + i * frequenciaOnda) * (alturaOnda * 0.5f);

            // --- CÁLCULO DA OPACIDADE (FADE) ---
            // Mathf.PingPong cria um valor que vai e volta (0 -> 1 -> 0)
            float tempoFade = Time.time * velocidadeFade;

            // Se for assíncrono, adicionamos o índice 'i' para cada letra piscar em momentos diferentes
            if (fadeAssincrono) tempoFade += i * 0.2f;

            float lerpVal = Mathf.PingPong(tempoFade, 1.0f);
            byte alphaFinal = (byte)Mathf.Lerp(alphaMinimo, alphaMaximo, lerpVal);

            // Aplica as mudanças nos 4 vértices de cada letra
            for (int j = 0; j < 4; j++)
            {
                var orig = verts[charInfo.vertexIndex + j];

                // Aplica o movimento
                verts[charInfo.vertexIndex + j] = orig + new Vector3(offsetX, offsetY, 0);

                // Aplica a cor (mantém a cor original, muda só o Alpha)
                Color32 corOriginal = colors[charInfo.vertexIndex + j];
                colors[charInfo.vertexIndex + j] = new Color32(corOriginal.r, corOriginal.g, corOriginal.b, alphaFinal);
            }
        }

        // Atualiza a malha do texto para o Unity renderizar
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            meshInfo.mesh.colors32 = meshInfo.colors32;
            textoTMP.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}