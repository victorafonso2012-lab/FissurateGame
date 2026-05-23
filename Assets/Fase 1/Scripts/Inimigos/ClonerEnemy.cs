using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class ClonerEnemy : MonoBehaviour, IDamageable
{
    enum ClonerState { Disguised, Chasing, Clone, Dormant }

    [Header("Disfarce")]
    [SerializeField] private Sprite[] disguiseSprites;
    [SerializeField] private bool chooseRandomDisguise = true;
    [SerializeField] private Color disguiseColor = Color.white;

    [Header("Deteccao")]
    [SerializeField] private float detectionRadius = 2.5f;
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float contactDistance = 0.8f;
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float contactStunTime = 0.1f;

    [Header("Clone")]
    [SerializeField] private float cloneDuration = 60f;
    [SerializeField] private float cloneFollowDistance = 3f;
    [SerializeField] private float clonePathSampleDistance = 0.08f;
    [SerializeField] private float damageToPlayerMultiplier = 2f;
    [SerializeField] private Color cloneColor = Color.white;

    [Header("Recarga")]
    [SerializeField] private float dormantDuration = 60f;

    [Header("Buff dos inimigos")]
    [SerializeField] private float enemyBuffRadius = 12f;
    [SerializeField] private float enemySpeedMultiplier = 2f;
    [SerializeField] private float buffRefreshInterval = 0.5f;
    [SerializeField] private LayerMask enemyBuffLayers = ~0;

    [Header("Fisica")]
    [SerializeField] private bool forceTriggerCollider = true;

    private const BindingFlags SpeedFieldFlags = BindingFlags.Instance | BindingFlags.Public;

    private readonly List<Vector3> playerPath = new List<Vector3>();
    private readonly Dictionary<MonoBehaviour, float> buffedSpeeds = new Dictionary<MonoBehaviour, float>();

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private Transform player;
    private PlayerHealth playerHealth;
    private SpriteRenderer playerSpriteRenderer;

    private ClonerState state = ClonerState.Disguised;
    private Sprite initialSprite;
    private Color initialColor;
    private Vector3 initialScale;
    private string originalTag;
    private int originalLayer;
    private float cloneTimer;
    private float dormantTimer;
    private float buffRefreshTimer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();

        initialSprite = spriteRenderer.sprite;
        initialColor = spriteRenderer.color;
        initialScale = transform.localScale;
        originalTag = gameObject.tag;
        originalLayer = gameObject.layer;

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (forceTriggerCollider)
            circleCollider.isTrigger = true;
    }

    void Start()
    {
        CachePlayer();
        BecomeDisguise();
    }

    void OnDisable()
    {
        RestoreBuffedEnemies();
        RestoreIdentity();
    }

    void Update()
    {
        if (player == null)
            CachePlayer();

        switch (state)
        {
            case ClonerState.Disguised:
                if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRadius)
                    StartChasing();
                break;

            case ClonerState.Clone:
                cloneTimer -= Time.deltaTime;
                CopyPlayerVisual();
                RefreshEnemyBuffAura();

                if (cloneTimer <= 0f)
                    BecomeDormant();
                break;

            case ClonerState.Dormant:
                dormantTimer -= Time.deltaTime;

                if (dormantTimer <= 0f)
                    BecomeDisguise();
                break;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (state == ClonerState.Chasing)
        {
            ChasePlayer();
        }
        else if (state == ClonerState.Clone)
        {
            FollowPlayerPath();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (state == ClonerState.Clone)
        {
            DamagePlayerFromClone(damageAmount * damageToPlayerMultiplier);
            return;
        }

        if (state == ClonerState.Disguised || state == ClonerState.Dormant)
            StartChasing();
    }

    private void CachePlayer()
    {
        if (PlayerMove.instance == null)
            return;

        player = PlayerMove.instance.transform;
        playerHealth = PlayerMove.instance.health;

        if (playerHealth == null)
            playerHealth = player.GetComponent<PlayerHealth>();

        playerSpriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
    }

    private void BecomeDisguise()
    {
        state = ClonerState.Disguised;
        rb.linearVelocity = Vector2.zero;
        RestoreBuffedEnemies();
        RestoreIdentity();
        ApplyRandomDisguiseSprite();
        spriteRenderer.color = disguiseColor;
        transform.localScale = initialScale;
    }

    private void StartChasing()
    {
        if (state == ClonerState.Clone)
            return;

        state = ClonerState.Chasing;
        RestoreIdentity();
        spriteRenderer.color = initialColor;
    }

    private void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;

        if (Vector2.Distance(transform.position, player.position) <= contactDistance)
            HitPlayerAndBecomeClone();
    }

    private void HitPlayerAndBecomeClone()
    {
        if (state != ClonerState.Chasing)
            return;

        if (playerHealth != null)
            playerHealth.TomarDano(contactDamage, contactStunTime, gameObject);

        BecomeClone();
    }

    private void BecomeClone()
    {
        state = ClonerState.Clone;
        cloneTimer = cloneDuration;
        buffRefreshTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        playerPath.Clear();

        if (player != null)
        {
            playerPath.Add(player.position);
            Vector2 offsetDirection = PlayerMove.instance != null && PlayerMove.instance.LookDirection != Vector2.zero
                ? -PlayerMove.instance.LookDirection.normalized
                : Vector2.left;

            transform.position = player.position + (Vector3)(offsetDirection * cloneFollowDistance);
            gameObject.layer = player.gameObject.layer;
        }

        TrySetTag("Player");
        CopyPlayerVisual();
        RefreshEnemyBuffAura();
    }

    private void BecomeDormant()
    {
        state = ClonerState.Dormant;
        dormantTimer = dormantDuration;
        rb.linearVelocity = Vector2.zero;
        RestoreBuffedEnemies();
        RestoreIdentity();
        ApplyRandomDisguiseSprite();
        spriteRenderer.color = disguiseColor;
        transform.localScale = initialScale;
    }

    private void FollowPlayerPath()
    {
        RecordPlayerPosition();
        Vector3 target = GetFollowTarget();
        rb.MovePosition(target);
    }

    private void RecordPlayerPosition()
    {
        Vector3 playerPosition = player.position;

        if (playerPath.Count == 0 || Vector3.Distance(playerPath[playerPath.Count - 1], playerPosition) >= clonePathSampleDistance)
            playerPath.Add(playerPosition);

        float maxStoredDistance = Mathf.Max(10f, cloneFollowDistance * 3f);
        float storedDistance = 0f;

        for (int i = playerPath.Count - 1; i > 0; i--)
        {
            storedDistance += Vector3.Distance(playerPath[i], playerPath[i - 1]);

            if (storedDistance > maxStoredDistance)
            {
                playerPath.RemoveRange(0, i - 1);
                break;
            }
        }
    }

    private Vector3 GetFollowTarget()
    {
        if (playerPath.Count == 0)
            return transform.position;

        float distance = 0f;

        for (int i = playerPath.Count - 1; i > 0; i--)
        {
            distance += Vector3.Distance(playerPath[i], playerPath[i - 1]);

            if (distance >= cloneFollowDistance)
                return playerPath[i - 1];
        }

        Vector2 fallbackDirection = PlayerMove.instance != null && PlayerMove.instance.LookDirection != Vector2.zero
            ? -PlayerMove.instance.LookDirection.normalized
            : Vector2.left;

        return player.position + (Vector3)(fallbackDirection * cloneFollowDistance);
    }

    private void CopyPlayerVisual()
    {
        if (playerSpriteRenderer == null)
            return;

        spriteRenderer.sprite = playerSpriteRenderer.sprite;
        spriteRenderer.flipX = playerSpriteRenderer.flipX;
        spriteRenderer.color = cloneColor;

        float direction = player.lossyScale.x < 0f ? -1f : 1f;
        transform.localScale = new Vector3(Mathf.Abs(initialScale.x) * direction, initialScale.y, initialScale.z);
    }

    private void ApplyRandomDisguiseSprite()
    {
        if (disguiseSprites != null && disguiseSprites.Length > 0)
        {
            int index = chooseRandomDisguise ? Random.Range(0, disguiseSprites.Length) : 0;
            spriteRenderer.sprite = disguiseSprites[index];
            return;
        }

        spriteRenderer.sprite = initialSprite;
    }

    private void RefreshEnemyBuffAura()
    {
        buffRefreshTimer -= Time.deltaTime;

        if (buffRefreshTimer > 0f)
            return;

        buffRefreshTimer = buffRefreshInterval;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, enemyBuffRadius, enemyBuffLayers);
        HashSet<MonoBehaviour> stillInRange = new HashSet<MonoBehaviour>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject || hit.CompareTag("Player"))
                continue;

            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || behaviour is ClonerEnemy || behaviour is PlayerMove)
                    continue;

                FieldInfo speedField = behaviour.GetType().GetField("speed", SpeedFieldFlags);
                if (speedField == null || speedField.FieldType != typeof(float))
                    continue;

                stillInRange.Add(behaviour);

                if (!buffedSpeeds.ContainsKey(behaviour))
                    buffedSpeeds.Add(behaviour, (float)speedField.GetValue(behaviour));

                speedField.SetValue(behaviour, buffedSpeeds[behaviour] * enemySpeedMultiplier);
            }
        }

        RestoreEnemiesOutsideAura(stillInRange);
    }

    private void RestoreEnemiesOutsideAura(HashSet<MonoBehaviour> stillInRange)
    {
        List<MonoBehaviour> toRestore = new List<MonoBehaviour>();

        foreach (KeyValuePair<MonoBehaviour, float> entry in buffedSpeeds)
        {
            if (entry.Key == null || !stillInRange.Contains(entry.Key))
                toRestore.Add(entry.Key);
        }

        foreach (MonoBehaviour behaviour in toRestore)
            RestoreEnemySpeed(behaviour);
    }

    private void RestoreBuffedEnemies()
    {
        List<MonoBehaviour> toRestore = new List<MonoBehaviour>(buffedSpeeds.Keys);

        foreach (MonoBehaviour behaviour in toRestore)
            RestoreEnemySpeed(behaviour);
    }

    private void RestoreEnemySpeed(MonoBehaviour behaviour)
    {
        if (behaviour != null && buffedSpeeds.TryGetValue(behaviour, out float originalSpeed))
        {
            FieldInfo speedField = behaviour.GetType().GetField("speed", SpeedFieldFlags);
            if (speedField != null && speedField.FieldType == typeof(float))
                speedField.SetValue(behaviour, originalSpeed);
        }

        buffedSpeeds.Remove(behaviour);
    }

    private void DamagePlayerFromClone(float damage)
    {
        if (damage <= 0f)
            return;

        if (playerHealth == null)
            CachePlayer();

        if (playerHealth != null)
            playerHealth.AplicarDanoSemTutorial(damage);
    }

    private void RestoreIdentity()
    {
        TrySetTag(originalTag);
        gameObject.layer = originalLayer;
    }

    private void TrySetTag(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return;

        try
        {
            gameObject.tag = tagName;
        }
        catch
        {
            Debug.LogWarning($"ClonerEnemy: a tag '{tagName}' nao existe no projeto.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandlePlayerContact(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHandlePlayerContact(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHandlePlayerContact(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryHandlePlayerContact(collision.gameObject);
    }

    private void TryHandlePlayerContact(GameObject other)
    {
        if (state != ClonerState.Chasing || other == null || !other.CompareTag("Player"))
            return;

        HitPlayerAndBecomeClone();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, enemyBuffRadius);
    }
}
