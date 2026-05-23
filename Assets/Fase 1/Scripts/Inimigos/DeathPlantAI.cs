using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public class DeathPlantAI : MonoBehaviour, IEnemyAI
{
    public enum EnemyState { Idle, Chase, Attack, WindUp, Hit, Stunned, Dead }
    [SerializeField] private EnemyState state = EnemyState.Idle;

    [Header("Movimentação")]
    public float velocidade = 2f;
    public float rangeDeteccao = 6f;
    public float distanciaAtaque = 1.5f;
    private Vector2 direcaoMovimento = Vector2.zero;

    private Vector2 lastFacingDirection = Vector2.down;

    [Header("Ataque")]
    public float dano = 40f;
    public float delayAtaque = 2f;
    public float windUp = 0.7f;
    public float hitDuration = 0.1f;

    // Novo: Duração do Stun no Player aplicado por este inimigo
    private const float PLAYER_STUN_DURATION = 1.5f;

    [Header("Hitbox Quadrado de Acerto")]
    public Vector2 hitboxSize = new Vector2(1.5f, 1.5f);
    public float hitboxDistance = 1.0f;
    public float baseVerticalOffset = -0.5f;

    [Header("Visual")]
    public Color corNormal = Color.blue;
    public Color corWindUp = Color.yellow;
    public Color corHit = Color.red;
    public Color corStun = Color.magenta;
    public SpriteRenderer sr;

    private Transform player;
    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    private PlayerParry playerParry;
    private Coroutine attackCoroutine;
    private Coroutine stunCoroutine;

    // ======================================
    // 🧠 INICIALIZAÇÃO
    // ======================================

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        rb.gravityScale = 0;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = corNormal;

        if (PlayerMove.instance != null)
        {
            player = PlayerMove.instance.transform;
            playerHealth = PlayerMove.instance.health;
            playerParry = PlayerMove.instance.GetComponent<PlayerParry>();
        }
        else
        {
            Debug.LogError("Player não encontrado. Desativando AI.");
            this.enabled = false;
        }

        if (enemyHealth != null) enemyHealth.spriteRenderer = sr;
    }

    // ======================================
    // 🔄 UPDATE E FSM
    // ======================================

    void Update()
    {
        if (enemyHealth.IsDead || player == null || state == EnemyState.Stunned || state == EnemyState.WindUp || state == EnemyState.Hit)
        {
            direcaoMovimento = Vector2.zero;
            return;
        }

        float distSqr = (player.position - transform.position).sqrMagnitude;
        float rangeSqr = rangeDeteccao * rangeDeteccao;
        float attackSqr = distanciaAtaque * distanciaAtaque;

        // 1. Prioridade: ATAQUE
        if (distSqr <= attackSqr)
        {
            if (state != EnemyState.Attack)
                SetState(EnemyState.Attack);

            direcaoMovimento = Vector2.zero;
        }
        // 2. CHASE
        else if (distSqr <= rangeSqr)
        {
            SetState(EnemyState.Chase);
            direcaoMovimento = (player.position - transform.position).normalized;
        }
        // 3. IDLE
        else
        {
            SetState(EnemyState.Idle);
            direcaoMovimento = Vector2.zero;
        }

        // NOVO: Atualiza a direção que a planta está "olhando"
        if (direcaoMovimento.sqrMagnitude > 0)
        {
            if (Mathf.Abs(direcaoMovimento.x) > Mathf.Abs(direcaoMovimento.y))
            {
                lastFacingDirection = direcaoMovimento.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                lastFacingDirection = direcaoMovimento.y > 0 ? Vector2.up : Vector2.down;
            }
        }
    }

    void FixedUpdate()
    {
        if (state == EnemyState.Chase)
        {
            rb.MovePosition(rb.position + direcaoMovimento * velocidade * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // ======================================
    // ⚔️ SEQUÊNCIA DE ATAQUE E PARRY
    // ======================================

    public void SetState(EnemyState newState)
    {
        if (state == EnemyState.Dead) return;

        if (state != newState)
        {
            if ((state == EnemyState.Attack || state == EnemyState.WindUp || state == EnemyState.Hit) &&
                (newState == EnemyState.Idle || newState == EnemyState.Chase))
            {
                if (attackCoroutine != null)
                {
                    StopCoroutine(attackCoroutine);
                    attackCoroutine = null;
                    SetCor(corNormal);
                }
            }

            state = newState;

            if (newState == EnemyState.Attack && attackCoroutine == null)
            {
                attackCoroutine = StartCoroutine(AttackSequence());
            }
        }
    }

    IEnumerator AttackSequence()
    {
        // 1. WindUp (Telegraph/Aviso)
        SetState(EnemyState.WindUp);
        SetCor(corWindUp);
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(windUp);

        if (enemyHealth.IsDead) yield break;

        // 2. Janela de HIT (Ataque/Parry)
        SetState(EnemyState.Hit);
        SetCor(corHit);

        yield return StartCoroutine(CheckForParry(hitDuration));

        // 3. Cooldown e Reset
        SetCor(corNormal);
        yield return new WaitForSeconds(delayAtaque);

        attackCoroutine = null;
        SetState(EnemyState.Idle);
    }

    IEnumerator CheckForParry(float duration)
    {
        float timer = 0f;
        bool attackSuccessful = true;

        while (timer < duration)
        {
            if (playerParry != null && playerParry.IsParrying && CheckForPlayerHit())
            {
                playerParry.OnSuccessfulParryAndStun(gameObject);
                attackSuccessful = false;
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (attackSuccessful && CheckForPlayerHit())
        {
            if (playerHealth != null)
            {
                // Dano e Stun são aplicados no PlayerHealth.TomarDano
                playerHealth.TomarDano(dano, PLAYER_STUN_DURATION);
            }
        }
    }

    /// <summary>
    /// Executa a checagem de Box Collider, agora DIREICIONAL e com rotação.
    /// </summary>
    private bool CheckForPlayerHit()
    {
        // 1. Posição Base e Deslocamento
        Vector2 basePosition = (Vector2)transform.position + Vector2.up * baseVerticalOffset;
        Vector2 boxCenter = basePosition + lastFacingDirection * hitboxDistance;

        // 2. Cálculo da Rotação
        float angle = 0f;
        if (lastFacingDirection == Vector2.left || lastFacingDirection == Vector2.right)
        {
            angle = 90f;
        }

        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(
            boxCenter,
            hitboxSize,
            angle, // <== USANDO O ÂNGULO
            LayerMask.GetMask("Player")
        );

        foreach (Collider2D hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }


    // ======================================
    // 🛡️ IMPLEMENTAÇÃO IEnemyAI (Stun e Morte)
    // ======================================

    public void StartStun(float duration)
    {
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunEnemyCoroutine(duration));
    }

    IEnumerator StunEnemyCoroutine(float duration)
    {
        SetCor(corStun);

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        SetState(EnemyState.Stunned);
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(duration);

        if (state != EnemyState.Dead)
        {
            SetCor(corNormal);
            SetState(EnemyState.Idle);
        }
        stunCoroutine = null;
    }

    public bool IsVulnerableToDamage()
    {
        return true;
    }

    public void SetDeadState()
    {
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        state = EnemyState.Dead;
        rb.linearVelocity = Vector2.zero;
    }

    // ======================================
    // 🎨 VISUAL & GIZMOS
    // ======================================

    private void SetCor(Color c)
    {
        if (sr != null) sr.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        // Range de Detecção (Amarelo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangeDeteccao);

        // Hitbox Quadrado (Ciano) - A área de acerto real
        Gizmos.color = Color.cyan;

        // NOVO: Cálculo da rotação para o Gizmo
        float angle = 0f;
        if (lastFacingDirection == Vector2.left || lastFacingDirection == Vector2.right)
        {
            angle = 90f;
        }

        Vector3 boxCenter = transform.position + Vector3.up * baseVerticalOffset + (Vector3)lastFacingDirection * hitboxDistance;

        // Aplica a rotação ao Gizmo
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, Quaternion.Euler(0, 0, angle), Vector3.one);

        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);

        // RESTAURA a matriz original
        Gizmos.matrix = originalMatrix;
    }
}