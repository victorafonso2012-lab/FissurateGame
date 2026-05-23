using UnityEngine;
using System.Collections;

public class NoikuMove : MonoBehaviour
{
    Vector3 posIni;
    Vector3 posFin;
    public float duracaoMovimento = 1.0f;
    private Coroutine corrotinaMovimento;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        posIni = transform.position;
        posFin = posIni + new Vector3(0,370f,0);
    }

    // Update is called once per frame
    void Update()
    {

    }

    // 1. O que acontece quando o sinal é recebido.
    private void IniciarMinhaAcao()
    {
        Debug.Log("Sinal Recebido! Meu objeto está iniciando a ação de resposta.");
        // Coloque aqui o código da sua ação: abrir uma porta, tocar um som, etc.

        StartCoroutine(MoverParaAlvo(posFin, duracaoMovimento));
        ChatMove chat = FindFirstObjectByType<ChatMove>();
        chat.PlayChat();

        // Exemplo: desabilitar este script após a ação.
        // this.enabled = false;
    }
    private void IniciarMinhaAcao2()
    {
        Debug.Log("Sinal Recebido! Meu objeto está iniciando a ação de resposta.");
        // Coloque aqui o código da sua ação: abrir uma porta, tocar um som, etc.

        StartCoroutine(MoverParaAlvo(posIni, duracaoMovimento));
        ChatMove chat = FindFirstObjectByType<ChatMove>();
        chat.StopChat();
        // Exemplo: desabilitar este script após a ação.
        // this.enabled = false;
    }

    // 2. ASSINAR O EVENTO (LIGAR O RECEPTOR)
    private void OnEnable()
    {
        // O operador '+= 'liga' o método IniciarMinhaAcao ao sinal disparado pelo Trigger.
        PauseTrigger2D.OnPlayerEntrouNoTrigger += IniciarMinhaAcao;
        PauseTrigger2D.OnPlayerSaiuDoTrigger += IniciarMinhaAcao2;
    }

    // 3. DESASSINAR O EVENTO (DESLIGAR O RECEPTOR)
    // ESSENCIAL para evitar memory leaks (scripts que continuam ouvindo mesmo após serem destruídos).
    private void OnDisable()
    {
        // O operador '-=' 'desliga' o método do sinal.
        PauseTrigger2D.OnPlayerEntrouNoTrigger -= IniciarMinhaAcao;
        PauseTrigger2D.OnPlayerSaiuDoTrigger -= IniciarMinhaAcao2;
    }
    IEnumerator MoverParaAlvo(Vector3 alvo, float duracao)
    {
        float tempoDecorrido = 0f;
        Vector3 posicaoInicial = transform.position;

        // Loop principal que roda a cada frame
        while (tempoDecorrido < duracao)
        {
            // 1. Calcula o progresso (fator t) entre 0 (início) e 1 (fim)
            tempoDecorrido += Time.deltaTime;
            float progresso = tempoDecorrido / duracao;

            // 2. Aplica Lerp para calcular a posição atual
            // Mathf.Lerp(a, b, t) retorna 'a' quando t=0 e 'b' quando t=1.
            transform.position = Vector3.Lerp(posicaoInicial, alvo, progresso);

            // Aguarda o próximo frame
            yield return null;
        }

        // 3. Garante que a posição final seja EXATAMENTE o alvo
        transform.position = alvo;
        corrotinaMovimento = null; // Limpa a referência da corrotina
    }
}
