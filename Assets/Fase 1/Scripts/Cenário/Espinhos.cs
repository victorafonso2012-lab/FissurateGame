using UnityEngine;

public class Espinhos : MonoBehaviour
{
    [Header("Configurações do Espinho")]
    public float dano = 15f;
    public float tempoEntreDanos = 1f; // Tempo que demora pra tomar dano de novo se ficar parado em cima
    public float tempoDeStun = 0.2f;   // Dá uma leve travadinha na personagem (puxa da sua função TomarDano)

    private float ultimoDanoTempo;

    // OnTriggerStay2D roda o tempo todo enquanto algo estiver dentro da área
    private void OnTriggerStay2D(Collider2D other)
    {
        // Verifica se quem está pisando é a personagem
        if (other.CompareTag("Player"))
        {
            // Checa se já passou o "cooldown" para não matar a personagem em 1 segundo
            if (Time.time >= ultimoDanoTempo + tempoEntreDanos)
            {
                PlayerHealth saude = other.GetComponent<PlayerHealth>();
                if (saude != null)
                {
                    // Chama o seu método de dano passando o valor do dano e o stun!
                    saude.TomarDano(dano, tempoDeStun);
                    ultimoDanoTempo = Time.time;
                }
            }
        }
    }
}

