using UnityEngine;
using System.Collections;

public class ChatMove : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [Tooltip("Tempo para a caixa chegar na posição final")]
    public float duracaoAnimacao = 0.5f;

    [Tooltip("A distância que a caixa vai andar para cima (em pixels)")]
    public float distanciaDeslize = 100f;

    [Header("Suavização")]
    [Tooltip("Use uma curva suave, sem passar do limite (recomendo EaseOutCubic)")]
    public AnimationCurve curvaMovimento = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine rotinaAtual;
    private Vector2 posicaoOriginal;
    private Vector2 posicaoEscondida;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Salva onde você colocou a caixa na cena (essa é a posição "Aberta")
        posicaoOriginal = rectTransform.anchoredPosition;

        // Calcula a posição "Fechada" (Original - Distância para baixo)
        posicaoEscondida = posicaoOriginal - new Vector2(0, distanciaDeslize);

        // Começa escondido
        canvasGroup.alpha = 0;
        rectTransform.anchoredPosition = posicaoEscondida;
    }

    // --- Conexão Automática com o Trigger ---
    void OnEnable()
    {
        PauseTrigger2D.OnPlayerEntrouNoTrigger += PlayChat;
        PauseTrigger2D.OnPlayerSaiuDoTrigger += StopChat;
    }

    void OnDisable()
    {
        PauseTrigger2D.OnPlayerEntrouNoTrigger -= PlayChat;
        PauseTrigger2D.OnPlayerSaiuDoTrigger -= StopChat;
    }

    public void PlayChat()
    {
        if (rotinaAtual != null) StopCoroutine(rotinaAtual);
        rotinaAtual = StartCoroutine(AnimarSlide(true));
    }

    public void StopChat()
    {
        if (rotinaAtual != null) StopCoroutine(rotinaAtual);
        rotinaAtual = StartCoroutine(AnimarSlide(false));
    }

    IEnumerator AnimarSlide(bool mostrando)
    {
        float tempoDecorrido = 0f;

        // Define origem e destino baseado se está abrindo ou fechando
        Vector2 inicioPos = rectTransform.anchoredPosition;
        Vector2 fimPos = mostrando ? posicaoOriginal : posicaoEscondida;

        float inicioAlpha = canvasGroup.alpha;
        float fimAlpha = mostrando ? 1f : 0f;

        while (tempoDecorrido < duracaoAnimacao)
        {
            tempoDecorrido += Time.deltaTime;
            float progresso = tempoDecorrido / duracaoAnimacao;

            // Avalia a curva para suavidade
            float valorCurva = curvaMovimento.Evaluate(progresso);

            // Move a posição (Lerp)
            rectTransform.anchoredPosition = Vector2.Lerp(inicioPos, fimPos, valorCurva);

            // Fade suave
            canvasGroup.alpha = Mathf.Lerp(inicioAlpha, fimAlpha, progresso);

            yield return null;
        }

        // Garante valores finais exatos
        rectTransform.anchoredPosition = fimPos;
        canvasGroup.alpha = fimAlpha;
    }
}