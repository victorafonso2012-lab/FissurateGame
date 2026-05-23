using UnityEngine;

public class Projectile2 : MonoBehaviour
{
    [Header("Configuração do projétil")]
    public float lifeTime2 = 5f; // tempo até ser destruído sozinho
    public int damage = 1;       // quanto de dano causa

    void Start()
    {
        // destrói o projétil automaticamente depois de um tempo
        Destroy(gameObject, lifeTime2);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // se bater no player
        if (other.CompareTag("Player"))
        {
            // tenta acessar o script de vida do player
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TomarDano(damage); // <-- usar o método correto
            }
            else
            {
                IDamageable damageable = other.GetComponentInParent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(damage);
            }


            Destroy(gameObject);
        }

        // se bater em uma parede/obstáculo
       /// if (other.CompareTag("Obstacle"))
       // {
      //      Destroy(gameObject);
       // }
    }
}
