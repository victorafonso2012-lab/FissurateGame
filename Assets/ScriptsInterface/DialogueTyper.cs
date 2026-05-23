using UnityEngine;
using TMPro; // Importante para mexer com texto
using System.Collections;

public class DialogueTyper : MonoBehaviour
{
    public float velocidadeDigitacao = 0.05f; // Quanto menor, mais rápido
    private TextMeshProUGUI textoUI;
    private Coroutine rotinaDigitacao;

    void Awake()
    {
        textoUI = GetComponent<TextMeshProUGUI>();
        textoUI.text = ""; // Começa vazio
    }

    void OnEnable()
    {
        // Se inscreve para ouvir quando o Trigger mandar um texto novo
        PauseTrigger2D.OnAtualizarTexto += EscreverTexto;
        PauseTrigger2D.OnPlayerSaiuDoTrigger += LimparTexto;
    }

    void OnDisable()
    {
        PauseTrigger2D.OnAtualizarTexto -= EscreverTexto;
        PauseTrigger2D.OnPlayerSaiuDoTrigger -= LimparTexto;
    }

    void LimparTexto()
    {
        textoUI.text = "";
    }

    // Essa função recebe a frase e começa a digitar
    void EscreverTexto(string fraseNova)
    {
        if (rotinaDigitacao != null) StopCoroutine(rotinaDigitacao);
        rotinaDigitacao = StartCoroutine(TypewriterEffect(fraseNova));
    }

    IEnumerator TypewriterEffect(string frase)
    {
        textoUI.text = ""; // Limpa antes de começar

        foreach (char letra in frase.ToCharArray())
        {
            textoUI.text += letra; // Adiciona uma letra
            yield return new WaitForSeconds(velocidadeDigitacao); // Espera um pouquinho
        }
    }
}