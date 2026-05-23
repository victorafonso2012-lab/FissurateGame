using UnityEngine;
using System.Collections;

public class SwordAttack : MonoBehaviour
{
    [Header("Configurações de Ataque")]
    public float attackRange = 1.2f;    // raio do alcance
    public float attackCooldown = 0.3f;   // tempo entre ataques
    public float attackDelay = 0.1f;    // atraso até o impacto
    public float damage = 20f;          // dano do golpe
    public LayerMask enemyLayers;         // camada dos inimigos

    [Header("Referências")]
    public Transform attackPoint;         // ponto de origem do ataque (lido pelo PlayerMove)
    public GameObject hitAnimationPrefab; //Prefab da animação do hit

    public bool isAttacking = false;

    /// <summary>
    /// Método público chamado pelo PlayerMove para iniciar o ataque.
    /// </summary>
    public void Attack()
    {
        if (isAttacking) return; // Impede ataque durante o cooldown
        StartCoroutine(PerformAttack());
    }

    // =========================================
    // =========== SISTEMA DE ATAQUE ============
    // =========================================
    private IEnumerator PerformAttack()
    {
        isAttacking = true;

        // pequeno atraso pra sincronizar com animação
        yield return new WaitForSeconds(attackDelay);

        // detecta inimigos no alcance do ataque
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hits)
        {
            // Tenta pegar o "contrato" IDamageable do inimigo
            IDamageable damageable = enemy.GetComponent<IDamageable>();

            // Se o inimigo tiver o "contrato" (não for nulo), aplica o dano
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                ApplyPoison(enemy);
                Debug.Log($"⚔️ {enemy.name} recebeu {damage} de dano!");

                if (hitAnimationPrefab != null)
                {
                    // Instancia o prefab da animação na posição do inimigo atingido.
                    // Quaternion.identity = sem rotação.
                    Vector3 pontoIntermediario = Vector3.Lerp(attackPoint.position, enemy.transform.position, 0.75f);
                    Instantiate(hitAnimationPrefab, pontoIntermediario, Quaternion.identity);
                }
            }
        }

        // cooldown antes do próximo golpe
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private void ApplyPoison(Collider2D enemy)
    {
        if (PlayerMove.instance == null || PlayerMove.instance.SwordPoisonDamagePerSecond <= 0f)
            return;

        EnemyHealth enemyHealth = enemy.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.ApplyPoison("sword", PlayerMove.instance.SwordPoisonDamagePerSecond);
    }

    // =========================================
    // =========== GIZMOS (EDITOR) ==============
    // =========================================
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
