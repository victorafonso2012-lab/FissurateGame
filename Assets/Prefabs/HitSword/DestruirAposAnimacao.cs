using UnityEngine;

public class DestruirAposAnimacao : MonoBehaviour
{
    // Define o tempo que o objeto existirá (ex: 0.5 segundos, o tempo da sua animação)
    public float tempoDeVida = 0.5f;

    void Start()
    {
        // O objeto se destrói sozinho após o tempo definido.
        Destroy(gameObject, tempoDeVida);
    }
}