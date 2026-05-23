using UnityEngine;
using System.Collections;

public class BarreiraMadeira : MonoBehaviour, IDamageable
{
    [Header("Configuração")]
    public int hitsParaQuebrar = 3;
    private int hitsAtuais = 0;

    [Header("Efeitos Visuais")]
    public GameObject particulaQuebrar; // Opcional: efeito quando quebra de vez

    private SpriteRenderer spriteRenderer;
    private Color corOriginal;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            corOriginal = spriteRenderer.color;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        // Conta 1 hit toda vez que a espada acerta, independente da força do dano
        hitsAtuais++;

        if (hitsAtuais >= hitsParaQuebrar)
        {
            Quebrar();
        }
        else
        {
            // Toca o efeito de piscar indicando que a madeira rachou/tomou hit
            if (spriteRenderer != null)
            {
                StopAllCoroutines();
                StartCoroutine(PiscarDano());
            }
        }
    }

    private void Quebrar()
    {
        // Se você tiver uma partícula de madeira voando, ela aparece aqui
        if (particulaQuebrar != null)
        {
            Instantiate(particulaQuebrar, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private IEnumerator PiscarDano()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = corOriginal;
    }
}

