using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    private Rigidbody2D rb;
    private float bulletDamage = 0;
    private float poisonDamagePerSecond = 0f;
    public GameObject hitAnimationPrefab;

    /// <summary>
    /// Configura a bala antes de ela ser disparada.
    /// </summary>
    public void Initialize(Vector2 direction, float speed, float damage)
    {
        Initialize(direction, speed, damage, 0f);
    }

    public void Initialize(Vector2 direction, float speed, float damage, float poisonDamage)
    {
        rb = GetComponent<Rigidbody2D>();
        bulletDamage = damage;
        poisonDamagePerSecond = poisonDamage;

        // Define a velocidade da bala
        rb.linearVelocity = direction.normalized * speed;

        // Destroi a bala depois de 5 segundos (para n�o poluir a cena)
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Tenta aplicar dano a qualquer coisa que tenha a interface
        IDamageable target = other.GetComponent<IDamageable>();

        if (target != null)
        {
            // Evita que a bala acerte o pr�prio Player
            if (other.CompareTag("Player")) return;

            target.TakeDamage(bulletDamage);
            ApplyPoison(other);
            Debug.Log($"Bala acertou {other.name} e causou {bulletDamage} de dano.");
            if (hitAnimationPrefab != null)
            {
                // Instancia o prefab da animação na posição do inimigo atingido.
                // Quaternion.identity = sem rotação.
                Instantiate(hitAnimationPrefab, transform.position, transform.rotation);
            }

            // Destroi a bala ao acertar
            Destroy(gameObject);
        }
        else if (!other.CompareTag("Player") && !other.isTrigger)
        {
            // Se acertar uma parede (que n�o � trigger e n�o � o Player)
            Destroy(gameObject);
        }
    }

    private void ApplyPoison(Collider2D other)
    {
        if (poisonDamagePerSecond <= 0f)
            return;

        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.ApplyPoison("bullet", poisonDamagePerSecond);
    }
}
