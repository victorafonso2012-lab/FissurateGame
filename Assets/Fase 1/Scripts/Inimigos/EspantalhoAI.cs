using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(EnemyHealth))]
public class EspantalhoAI : MonoBehaviour, IEnemyAI
{
    public enum ScarecrowState { Idle, Reacting, Active, Petrified, Stunned, Dead }

    [Header("Movimento e ataque")]
    public float speed = 5f;
    public float detectionRange = 6f;
    public float stopDistance = 0.8f;
    public float reactionTime = 1.5f;
    public bool onCollision;

    [Header("Janela de Vulnerabilidade")]
    private Coroutine invulnerabilityCoroutine;
    public float vulnerabilityDelay = 0.2f;
    public float vulnerabilityDuration = 2f;

    [Header("Campo de visão")]
    public float visionAngle = 60f;
    public float chaseMemoryTime = 2f;

    [Header("Dano ao Player")]
    public float danoAoPlayer = 10f;
    [Tooltip("Tempo entre iniciar a animacao de ataque e aplicar o dano.")]
    public float damageInterval = 1f;

    [Header("Parry")]
    [Tooltip("Mostra o aviso antes de cada tentativa de ataque.")]
    public bool mostrarAvisoParryAntesDeCadaAtaque = true;

    private Animator anim;
    private Transform player;
    private PlayerMove playerMoveScript;
    private Rigidbody2D rb;
    private ScarecrowState currentState = ScarecrowState.Petrified;

    private float reactionTimer = 0f;
    private float chaseMemoryTimer = 0f;

    private Coroutine damageCoroutine;
    private Coroutine stunCoroutine;

    private bool isInvulnerable = true;
    private bool wasMoving = false;
    private float stationaryTime = 0f;

    private Vector3 originalScale;
    private EnemyHealth enemyHealth;
    private EnemyParryWarningIcon avisoParry;

    private bool isTakingHit = false;
    private bool isAttackSequencePlaying = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        enemyHealth = GetComponent<EnemyHealth>();
        anim = GetComponent<Animator>();
        avisoParry = GetComponent<EnemyParryWarningIcon>();

        originalScale = transform.localScale;

        if (PlayerMove.instance != null)
        {
            player = PlayerMove.instance.transform;
            playerMoveScript = PlayerMove.instance;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerMoveScript = playerObj.GetComponent<PlayerMove>();
            }

            if (player == null)
            {
                Debug.LogError($"Espantalho ({gameObject.name}): Player nao encontrado. Desativando.");
                enabled = false;
                return;
            }
        }

        isInvulnerable = true;
        currentState = ScarecrowState.Petrified;
    }

    void Update()
    {
        Animations();

        if (player == null || (enemyHealth != null && enemyHealth.IsDead) || playerMoveScript == null)
            return;

        if (currentState == ScarecrowState.Stunned)
            return;

        float distance = Vector2.Distance(transform.position, player.position);

        Vector2 playerLookDir = playerMoveScript.LookDirection.normalized;
        Vector2 dirToEnemy = (transform.position - player.position).normalized;

        if (playerLookDir == Vector2.zero)
            playerLookDir = Vector2.down;

        float angle = Vector2.Angle(playerLookDir, dirToEnemy);
        bool playerAimAtEnemy = angle < visionAngle;
        bool playerBodyFacingEnemy = IsPlayerBodyFacingEnemy();
        bool playerLookingAtEnemy = playerAimAtEnemy && playerBodyFacingEnemy;

        switch (currentState)
        {
            case ScarecrowState.Petrified:
                if (!playerLookingAtEnemy && distance <= detectionRange)
                {
                    currentState = ScarecrowState.Reacting;
                    reactionTimer = reactionTime;
                }
                break;

            case ScarecrowState.Reacting:
                if (playerLookingAtEnemy)
                {
                    currentState = ScarecrowState.Petrified;
                }
                else
                {
                    reactionTimer -= Time.deltaTime;
                    if (reactionTimer <= 0)
                    {
                        currentState = ScarecrowState.Active;
                        chaseMemoryTimer = chaseMemoryTime;
                    }
                }
                break;

            case ScarecrowState.Active:
                if (playerLookingAtEnemy)
                {
                    currentState = ScarecrowState.Petrified;
                    StopDamage();
                }
                else
                {
                    if (distance > detectionRange && chaseMemoryTimer <= 0)
                    {
                        currentState = ScarecrowState.Idle;
                        StopDamage();
                    }
                    else if (distance <= detectionRange || chaseMemoryTimer > 0)
                    {
                        chaseMemoryTimer -= Time.deltaTime;
                    }
                }
                break;

            case ScarecrowState.Idle:
                if (distance <= detectionRange)
                    currentState = ScarecrowState.Petrified;
                break;
        }

        UpdateInvulnerability();
    }

    void FixedUpdate()
    {
        if (player == null || (enemyHealth != null && enemyHealth.IsDead) || playerMoveScript == null)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (currentState == ScarecrowState.Active)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            Vector2 playerLookDir = playerMoveScript.LookDirection.normalized;
            Vector2 dirToEnemy = (transform.position - player.position).normalized;

            if (playerLookDir == Vector2.zero)
                playerLookDir = Vector2.down;

            float angle = Vector2.Angle(playerLookDir, dirToEnemy);
            bool playerAimAtEnemy = angle < visionAngle;
            bool playerBodyFacingEnemy = IsPlayerBodyFacingEnemy();
            bool playerLookingAtEnemy = playerAimAtEnemy && playerBodyFacingEnemy;

            if (!playerLookingAtEnemy && (distance <= detectionRange || chaseMemoryTimer > 0))
                MoveTowardsPlayer(distance);
            else
                rb.linearVelocity = Vector2.zero;
        }
        else
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    void MoveTowardsPlayer(float distance)
    {
        if (distance > stopDistance && !onCollision)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = dir * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private bool IsPlayerBodyFacingEnemy()
    {
        if (player == null || playerMoveScript == null)
            return false;

        bool enemyIsToRight = transform.position.x > player.position.x;
        return enemyIsToRight ? playerMoveScript.IsFacingRight : !playerMoveScript.IsFacingRight;
    }

    void UpdateInvulnerability()
    {
        bool isMoving = rb.linearVelocity.magnitude > 0.05f;

        if (isMoving)
        {
            stationaryTime = 0f;
            wasMoving = true;

            if (invulnerabilityCoroutine != null)
            {
                StopCoroutine(invulnerabilityCoroutine);
                invulnerabilityCoroutine = null;
            }

            isInvulnerable = false;
        }
        else
        {
            if (wasMoving)
            {
                stationaryTime += Time.deltaTime;

                if (!isInvulnerable && stationaryTime >= vulnerabilityDelay)
                    isInvulnerable = false;

                if (stationaryTime >= vulnerabilityDuration)
                {
                    isInvulnerable = true;
                    wasMoving = false;
                }
            }
        }
    }

    private void Animations()
    {
        if (anim == null) return;
        if (currentState == ScarecrowState.Dead) return;
        if (isTakingHit) return;

        if (onCollision)
        {
            HandleFlip();
            return;
        }

        if (currentState == ScarecrowState.Petrified)
        {
            anim.Play("Idle");
            return;
        }

        if (rb.linearVelocity.magnitude > 0.1f)
        {
            anim.Play("Run");
            HandleFlip();
        }
        else
        {
            anim.Play("Idle");
        }
    }

    private void HandleFlip()
    {
        float sizeX = Mathf.Abs(originalScale.x);
        float sizeY = originalScale.y;
        float sizeZ = originalScale.z;

        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            transform.localScale = rb.linearVelocity.x < 0
                ? new Vector3(-sizeX, sizeY, sizeZ)
                : new Vector3(sizeX, sizeY, sizeZ);
        }
        else if (player != null)
        {
            transform.localScale = player.position.x < transform.position.x
                ? new Vector3(-sizeX, sizeY, sizeZ)
                : new Vector3(sizeX, sizeY, sizeZ);
        }
    }

    public bool IsVulnerableToDamage()
    {
        return !isInvulnerable;
    }

    public void StartStun(float duration)
    {
        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunSequence(duration));
    }

    IEnumerator StunSequence(float duration)
    {
        currentState = ScarecrowState.Stunned;
        StopDamage();

        yield return new WaitForSeconds(duration);

        if (currentState != ScarecrowState.Dead)
            currentState = ScarecrowState.Petrified;

        stunCoroutine = null;
    }

    public void TriggerHitAnimation()
    {
        if (currentState == ScarecrowState.Dead)
            return;

        StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        isTakingHit = true;

        if (anim != null)
            anim.Play("Hit");

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.3f);

        isTakingHit = false;
    }

    public void SetDeadState()
    {
        currentState = ScarecrowState.Dead;

        if (anim != null)
            anim.Play("Death");

        StopDamage();

        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (collision.CompareTag("Player") && currentState == ScarecrowState.Active)
        {
            onCollision = true;

            if (anim != null && !isAttackSequencePlaying)
                anim.Play("Attack", 0, 0f);

            isAttackSequencePlaying = true;

            if (damageCoroutine == null)
                damageCoroutine = StartCoroutine(DamageOverTime(collision));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StopDamage();
            onCollision = false;
        }
    }

    private IEnumerator DamageOverTime(Collider2D targetCollider)
    {
        PlayerHealth playerHealth = targetCollider != null ? targetCollider.GetComponentInParent<PlayerHealth>() : null;
        IDamageable damageTarget = targetCollider != null ? targetCollider.GetComponentInParent<IDamageable>() : null;

        while (damageTarget != null && currentState == ScarecrowState.Active && onCollision)
        {
            if (mostrarAvisoParryAntesDeCadaAtaque)
                MostrarAvisoDeParry();

            float tempo = 0f;
            bool parryAcertado = false;

            while (tempo < damageInterval)
            {
                if (damageTarget == null || currentState != ScarecrowState.Active || !onCollision)
                {
                    isAttackSequencePlaying = false;
                    damageCoroutine = null;
                    yield break;
                }

                PlayerParry parry = playerHealth != null ? playerHealth.GetComponent<PlayerParry>() : null;

                if (parry != null && parry.TryParryAttack(gameObject, danoAoPlayer))
                {
                    parryAcertado = true;
                    break;
                }

                tempo += Time.deltaTime;
                yield return null;
            }

            if (parryAcertado)
            {
                isAttackSequencePlaying = false;
                damageCoroutine = null;
                yield break;
            }

            if (damageTarget == null || currentState != ScarecrowState.Active || !onCollision)
                break;

            if (playerHealth != null)
                playerHealth.TomarDano(danoAoPlayer, 0f, gameObject);
            else
                damageTarget.TakeDamage(danoAoPlayer);

            yield return null;
        }

        isAttackSequencePlaying = false;
        damageCoroutine = null;
    }

    private void MostrarAvisoDeParry()
    {
        if (avisoParry == null)
            avisoParry = GetComponent<EnemyParryWarningIcon>();

        if (avisoParry != null)
        {
            avisoParry.MostrarAvisoParry();
        }
        else
        {
            Debug.LogWarning(gameObject.name + " nao tem EnemyParryWarningIcon.");
        }

        if (FirstDamageParryTutorial.Instance != null && player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth != null)
                FirstDamageParryTutorial.Instance.TentarIniciarNoAvisoDeParry(playerHealth);
        }
    }

    private void StopDamage()
    {
        isAttackSequencePlaying = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
