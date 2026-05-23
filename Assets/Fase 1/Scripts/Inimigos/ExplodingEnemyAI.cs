using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(EnemyHealth))]
public class ExplodingEnemyAI : MonoBehaviour, IEnemyAI
{
    [Header("Atributos Gerais")]
    public float speed = 3f;
    public float detectionRange = 6f;
    public float explosionRange = 1.5f;
    public float damage = 50f;
    public GameObject explosionEffect;
    public LayerMask explosionTargetLayers;

    [Header("Parry")]
    [Tooltip("Tempo entre aparecer o aviso de parry e a explosao acontecer.")]
    public float tempoAvisoAntesDaExplosao = 0.65f;

    [Tooltip("Mostra o icone de parry antes da explosao.")]
    public bool mostrarAvisoParry = true;

    private Transform player;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth enemyHealth;
    private EnemyParryWarningIcon avisoParry;

    private bool started = false;
    private bool playingDefault = false;
    private bool hasExploded = false;
    private bool isStunned = false;
    private bool preparingExplosion = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        enemyHealth = GetComponent<EnemyHealth>();
        avisoParry = GetComponent<EnemyParryWarningIcon>();

        if (enemyHealth != null && spriteRenderer != null)
            enemyHealth.spriteRenderer = spriteRenderer;

        if (PlayerMove.instance != null)
        {
            player = PlayerMove.instance.transform;
        }
        else
        {
            Debug.LogError($"ExplodingEnemy ({gameObject.name}): PlayerMove nao encontrado. Desativando AI.");
            enabled = false;
            return;
        }
    }

    void FixedUpdate()
    {
        if (enemyHealth.IsDead || hasExploded || preparingExplosion || player == null || isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        bool inRange = distance <= detectionRange;

        if (!started)
        {
            rb.linearVelocity = Vector2.zero;

            if (inRange && !playingDefault)
            {
                detectionRange = 20f;
                playingDefault = true;

                if (anim != null)
                    anim.Play("default");

                return;
            }

            if (playingDefault)
            {
                if (!IsPlaying("default"))
                {
                    started = true;
                    playingDefault = false;
                }

                return;
            }

            return;
        }

        if (inRange)
        {
            Vector2 direction = (player.position - transform.position).normalized;

            rb.linearVelocity = direction * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rb.rotation = angle;

            if (distance <= explosionRange)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                TryStartExplosion(playerHealth);
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded || preparingExplosion || enemyHealth.IsDead || isStunned)
            return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            TryStartExplosion(playerHealth);
        }
    }

    private void TryStartExplosion(PlayerHealth playerHealth)
    {
        if (hasExploded || preparingExplosion || enemyHealth.IsDead)
            return;

        preparingExplosion = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        StartCoroutine(ExplosionWarningRoutine(playerHealth));
    }

    private IEnumerator ExplosionWarningRoutine(PlayerHealth playerHealth)
    {
        if (mostrarAvisoParry)
            MostrarAvisoDeParry(playerHealth);

        float tempo = 0f;

        while (tempo < tempoAvisoAntesDaExplosao)
        {
            if (hasExploded || enemyHealth.IsDead)
                yield break;

            if (playerHealth != null)
            {
                PlayerParry parry = playerHealth.GetComponent<PlayerParry>();

                if (parry != null && parry.TryParryAttack(gameObject, damage))
                {
                    NeutralizarExplosaoComParry();
                    yield break;
                }
            }

            tempo += Time.deltaTime;
            yield return null;
        }

        Explode();
    }

    private void MostrarAvisoDeParry(PlayerHealth playerHealth)
    {
        if (avisoParry == null)
            avisoParry = GetComponent<EnemyParryWarningIcon>();

        if (avisoParry != null)
            avisoParry.MostrarAvisoParry();
        else
            Debug.LogWarning(gameObject.name + " nao tem EnemyParryWarningIcon.");

        if (FirstDamageParryTutorial.Instance != null && playerHealth != null)
            FirstDamageParryTutorial.Instance.TentarIniciarNoAvisoDeParry(playerHealth);
    }

    private void NeutralizarExplosaoComParry()
    {
        if (hasExploded)
            return;

        hasExploded = true;
        preparingExplosion = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    public void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;
        preparingExplosion = false;

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        if (!enemyHealth.IsDead)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRange, explosionTargetLayers);

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                    continue;

                if (hit.gameObject == gameObject)
                    continue;

                PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();

                if (playerHealth != null)
                {
                    PlayerParry parry = playerHealth.GetComponent<PlayerParry>();

                    if (parry != null && parry.TryParryAttack(gameObject, damage))
                        continue;

                    playerHealth.TomarDano(damage, 0f, gameObject);
                    continue;
                }

                IDamageable target = hit.GetComponentInParent<IDamageable>();

                if (target != null)
                    target.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    public void AttemptParry(PlayerParry playerParryScript)
    {
        if (enemyHealth.IsDead || hasExploded)
            return;

        if (playerParryScript != null && playerParryScript.IsParrying)
        {
            playerParryScript.OnSuccessfulParryAndStun(gameObject, damage);
            NeutralizarExplosaoComParry();
        }
    }

    public bool IsVulnerableToDamage()
    {
        return true;
    }

    public void StartStun(float duration)
    {
        // Stun ignorado para este inimigo.
    }

    public void SetDeadState()
    {
        if (!hasExploded)
            Explode();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    bool IsPlaying(string animationName)
    {
        if (anim == null)
            return false;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName);
    }
}
