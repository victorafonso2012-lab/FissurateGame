using UnityEngine;

public class EnemySpotlight : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste o objeto FILHO que contém a luz aqui.")]
    public GameObject luzSpotlight;

    [Tooltip("O Animator que vai tocar a animação no futuro.")]
    public Animator anim;

    [Header("Configurações")]
    [Tooltip("Qual a distância que o player precisa chegar para a luz apagar?")]
    public float distanciaParaApagar = 4f;

    [Tooltip("O nome do gatilho que criaremos no Animator no futuro.")]
    public string nomeTriggerAnimacao = "ApagarLuz";

    private Transform player;
    private bool luzEstaAcesa = true;

    void Start()
    {
        // Tenta achar o player automaticamente pela Tag (certifique-se que seu player tem a tag "Player")
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Pega o Animator do próprio objeto se você não tiver arrastado manualmente
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
    }

    void Update()
    {
        // Se não achou o player ou a luz já apagou, não faz mais nada
        if (player == null || !luzEstaAcesa) return;

        // Calcula a distância do inimigo até o player
        float distancia = Vector2.Distance(transform.position, player.position);

        // Se o player entrar no raio de distância
        if (distancia <= distanciaParaApagar)
        {
            ApagarLuz();
        }
    }

    private void ApagarLuz()
    {
        luzEstaAcesa = false;

        // 1. Apaga a luz desativando o objeto filho (funciona para luz 2D ou 3D)
        if (luzSpotlight != null)
        {
            luzSpotlight.SetActive(false);
        }

        // 2. Aciona o gatilho da animação que você vai criar no futuro
        if (anim != null)
        {
            anim.SetTrigger(nomeTriggerAnimacao);
        }
    }

    // Isso desenha um círculo amarelo no Unity para você visualizar o tamanho da área de detecção
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaParaApagar);
    }
}