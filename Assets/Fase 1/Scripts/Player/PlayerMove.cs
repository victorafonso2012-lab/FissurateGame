using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    public static PlayerMove instance;

    public enum WeaponMode { Sword, Gun }
    public enum AttributeUpgradeType
    {
        SwordDamage,
        MovementSpeed,
        GunDamage,
        GunProjectileSpeed,
        ParryDamage,
        MaxHealth,
        SwordPoison,
        BulletPoison,
        DoubleHealing,
        KillAreaDamage
    }

    public struct AttributeLevelUpChoice
    {
        public AttributeUpgradeType Type { get; }
        public string AttributeName { get; }
        public string BonusText { get; }
        public string DisplayText => $"{AttributeName} {BonusText}";

        public AttributeLevelUpChoice(AttributeUpgradeType type, string attributeName, string bonusText)
        {
            Type = type;
            AttributeName = attributeName;
            BonusText = bonusText;
        }
    }

    private class AttributeLevelUpChoiceSet
    {
        public int PlayerLevel { get; }
        public List<AttributeLevelUpChoice> Choices { get; }

        public AttributeLevelUpChoiceSet(int playerLevel, List<AttributeLevelUpChoice> choices)
        {
            PlayerLevel = playerLevel;
            Choices = choices;
        }
    }

    [Header("Armas")]
    public WeaponMode currentWeaponMode = WeaponMode.Sword;

    [Header("Referências")]
    public SwordAttack sword;
    public GunAttack gun;
    public Transform swordTransform;
    public Transform attackPoint;

    [Header("Nível do Player")]
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private TMP_Text playerLevelText;

    [Header("Bonus Aleatorio por Level Up")]
    [SerializeField] private float swordDamageLevelUpBonus = 5f;
    [SerializeField] private float movementSpeedLevelUpBonus = 1f;
    [SerializeField] private float gunDamageLevelUpBonus = 5f;
    [SerializeField] private float gunProjectileSpeedLevelUpBonus = 1f;
    [SerializeField] private float parryDamageLevelUpBonus = 5f;
    [SerializeField] private float healthLevelUpBonus = 5f;

    [Header("Bonus Especial por Level Up Multiplo de 5")]
    [SerializeField] private float poisonDamagePerUpgrade = 5f;
    [SerializeField] private float healingMultiplierUpgrade = 2f;
    [SerializeField] private float killAreaDamagePerUpgrade = 10f;
    [SerializeField] private float killAreaDamageRadius = 5f;
    [SerializeField] private LayerMask killAreaDamageLayers = ~0;

    [Header("Movimento")]
    public float speed = 5f;
    public float stamina = 100f;
    public float staminaRegenRate = 15f;

    [Header("Areia Movediça")]
    public float quicksandGraceTime = 20f;
    public float quicksandSlowdownDuration = 10f;
    public int quicksandEscapeClicks = 10;

    [Header("Efeitos Visuais (Mãos na Lama)")]
    public GameObject maosVisual;
    public float atrasoParaAparecerMaos = 2.0f;
    public float duracaoOriginalClipAnimacao = 10f;

    [Header("Efeitos Visuais (Afundar na Água/Pântano)")]
    [Tooltip("O objeto que contém o Sprite Mask (Filho do Player).")]
    public GameObject mascaraPernas;
    [Tooltip("O objeto que contém o Sprite Renderer do Player (Filho do Player).")]
    public Transform visualPlayer;

    [Tooltip("Posição Y inicial da Máscara (abaixo dos pés, sem cortar nada).")]
    public float alturaMascaraInicial = -1.5f;
    [Tooltip("Posição Y final da Máscara (na cintura, cortando as pernas).")]
    public float alturaMascaraFinal = -0.5f;

    [Tooltip("O quanto o sprite do player desce no eixo Y quando afunda.")]
    public float afundamentoMaximoY = -0.3f;
    [Tooltip("Tempo para o afundamento acontecer no pântano.")]
    public float duracaoAnimacaoAfundar = 1.2f;

    [Header("Visual Escape (Tecla E)")]
    public GameObject prefabIconeE;
    public float alturaIconeE = 1.8f;

    [Header("Efeito de Espasmo")]
    public float velocidadeEspasmo = 15.0f;
    public float intensidadeEspasmo = 0.3f;

    public Vector2 LookDirection { get; private set; } = Vector2.down;
    public Vector2 MovementInput => movement;
    public bool IsTryingToMove => movement.sqrMagnitude > 0.01f;
    public PlayerHealth health { get; private set; }
    public int PlayerLevel => playerLevel;
    public float SwordPoisonDamagePerSecond => swordPoisonDamagePerSecond;
    public float BulletPoisonDamagePerSecond => bulletPoisonDamagePerSecond;
    public bool isStunned { get; private set; } = false;
    public bool IsFacingRight => facingRight;

    private Animator anim;
    private Rigidbody2D rb;
    private PlayerParry parry;
    private Vector2 movement;
    private bool facingRight = true;
    private float baseSpeed;
    private Camera mainCamera;
    private bool isExternallyLocked = false;

    private bool isInQuicksand = false;
    private bool isStuckInQuicksand = false;
    private Collider2D currentWaterCollider;
    private int currentEscapeClicks = 0;
    private Coroutine quicksandCoroutine;
    private Coroutine quicksandWaterCoroutine;

    private Vector3 escalaOriginalMaos;
    private Animator maosAnimator;
    private GameObject iconeEInstanciado;
    private readonly Queue<AttributeLevelUpChoiceSet> pendingAttributeChoiceSets = new Queue<AttributeLevelUpChoiceSet>();
    private float swordPoisonDamagePerSecond;
    private float bulletPoisonDamagePerSecond;
    private bool doubleHealingEnabled;
    private float killAreaDamage;
    private bool applyingKillAreaDamage;

    public bool HasPendingAttributeLevelUpChoices => pendingAttributeChoiceSets.Count > 0;

    // Guarda a posição original do visual para ele voltar ao normal quando sair da água
    private float posYOriginalVisual = 0f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        health = GetComponent<PlayerHealth>();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        parry = GetComponent<PlayerParry>();
        baseSpeed = speed;

        // Pega o Animator no filho (Visual) ou no pai, dependendo de onde estiver
        anim = GetComponentInChildren<Animator>();

        if (sword == null || gun == null) Debug.LogWarning("Armas não configuradas no PlayerMove");
        currentWeaponMode = WeaponMode.Sword;
        playerLevel = Mathf.Max(1, playerLevel);
        UpdatePlayerLevelUI();

        if (maosVisual != null)
        {
            maosVisual.SetActive(false);
            escalaOriginalMaos = maosVisual.transform.localScale;
            maosAnimator = maosVisual.GetComponent<Animator>();
        }

        if (visualPlayer != null)
            posYOriginalVisual = visualPlayer.localPosition.y;

        if (mascaraPernas != null)
        {
            SetSinkingVisuals(alturaMascaraInicial, posYOriginalVisual);
            mascaraPernas.SetActive(false);
        }
    }

    public void IncreasePlayerLevel(int amount = 1)
    {
        if (amount <= 0) return;

        playerLevel += amount;
        UpdatePlayerLevelUI();
    }

    public void SetPlayerLevel(int level)
    {
        playerLevel = Mathf.Max(1, level);
        UpdatePlayerLevelUI();
    }

    public bool QueueAttributeLevelUpChoices(int playerLevel)
    {
        EnsureStatReferences();

        bool isSpecialLevel = playerLevel > 0 && playerLevel % 5 == 0;
        List<AttributeUpgradeType> availableAttributes = isSpecialLevel
            ? GetAvailableSpecialAttributeTypes()
            : GetAvailableAttributeTypes();
        if (availableAttributes.Count == 0)
        {
            Debug.LogWarning("Level Up: nenhum atributo valido encontrado para gerar escolhas.");
            return false;
        }

        ShuffleAttributes(availableAttributes);

        int choiceCount = Mathf.Min(3, availableAttributes.Count);
        List<AttributeLevelUpChoice> choices = new List<AttributeLevelUpChoice>(choiceCount);

        for (int i = 0; i < choiceCount; i++)
            choices.Add(CreateAttributeChoice(availableAttributes[i]));

        pendingAttributeChoiceSets.Enqueue(new AttributeLevelUpChoiceSet(playerLevel, choices));
        Debug.Log($"Level Up: {choiceCount} escolhas de atributo preparadas para o nivel {playerLevel}.");
        return true;
    }

    public bool TryGetCurrentAttributeLevelUpChoices(out List<AttributeLevelUpChoice> choices)
    {
        choices = null;

        if (pendingAttributeChoiceSets.Count == 0)
            return false;

        choices = pendingAttributeChoiceSets.Peek().Choices;
        return true;
    }

    public bool ApplyQueuedAttributeLevelUpChoice(int optionIndex)
    {
        if (pendingAttributeChoiceSets.Count == 0)
            return false;

        AttributeLevelUpChoiceSet choiceSet = pendingAttributeChoiceSets.Peek();
        if (optionIndex < 0 || optionIndex >= choiceSet.Choices.Count)
            return false;

        AttributeLevelUpChoice choice = choiceSet.Choices[optionIndex];
        if (!ApplyAttributeUpgrade(choice.Type))
            return false;

        pendingAttributeChoiceSets.Dequeue();
        Debug.Log($"Level Up: atributo escolhido no nivel {choiceSet.PlayerLevel}: {choice.AttributeName} ({choice.BonusText}).");
        return true;
    }

    private List<AttributeUpgradeType> GetAvailableAttributeTypes()
    {
        List<AttributeUpgradeType> attributes = new List<AttributeUpgradeType>();

        if (sword != null)
            attributes.Add(AttributeUpgradeType.SwordDamage);

        attributes.Add(AttributeUpgradeType.MovementSpeed);

        if (gun != null)
        {
            attributes.Add(AttributeUpgradeType.GunDamage);
            attributes.Add(AttributeUpgradeType.GunProjectileSpeed);
        }

        if (parry != null)
            attributes.Add(AttributeUpgradeType.ParryDamage);

        if (health != null)
            attributes.Add(AttributeUpgradeType.MaxHealth);

        return attributes;
    }

    private List<AttributeUpgradeType> GetAvailableSpecialAttributeTypes()
    {
        List<AttributeUpgradeType> attributes = new List<AttributeUpgradeType>();

        if (sword != null)
            attributes.Add(AttributeUpgradeType.SwordPoison);

        if (gun != null)
            attributes.Add(AttributeUpgradeType.BulletPoison);

        if (health != null && !doubleHealingEnabled)
            attributes.Add(AttributeUpgradeType.DoubleHealing);

        attributes.Add(AttributeUpgradeType.KillAreaDamage);

        return attributes;
    }

    private void ShuffleAttributes(List<AttributeUpgradeType> attributes)
    {
        for (int i = attributes.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            AttributeUpgradeType temporary = attributes[i];
            attributes[i] = attributes[randomIndex];
            attributes[randomIndex] = temporary;
        }
    }

    private AttributeLevelUpChoice CreateAttributeChoice(AttributeUpgradeType attribute)
    {
        return new AttributeLevelUpChoice(attribute, GetAttributeName(attribute), GetAttributeBonusText(attribute));
    }

    private bool ApplyAttributeUpgrade(AttributeUpgradeType attribute)
    {
        EnsureStatReferences();

        switch (attribute)
        {
            case AttributeUpgradeType.SwordDamage:
                if (sword == null) return false;
                sword.damage += swordDamageLevelUpBonus;
                return true;

            case AttributeUpgradeType.MovementSpeed:
                IncreaseBaseMovementSpeed(movementSpeedLevelUpBonus);
                return true;

            case AttributeUpgradeType.GunDamage:
                if (gun == null) return false;
                gun.damage += gunDamageLevelUpBonus;
                return true;

            case AttributeUpgradeType.GunProjectileSpeed:
                if (gun == null) return false;
                gun.bulletSpeed += gunProjectileSpeedLevelUpBonus;
                return true;

            case AttributeUpgradeType.ParryDamage:
                if (parry == null) return false;
                parry.IncreaseCounterDamageBonus(parryDamageLevelUpBonus);
                return true;

            case AttributeUpgradeType.MaxHealth:
                if (health == null) return false;
                health.IncreaseMaxHealth(healthLevelUpBonus);
                return true;

            case AttributeUpgradeType.SwordPoison:
                if (sword == null) return false;
                swordPoisonDamagePerSecond += poisonDamagePerUpgrade;
                return true;

            case AttributeUpgradeType.BulletPoison:
                if (gun == null) return false;
                bulletPoisonDamagePerSecond += poisonDamagePerUpgrade;
                return true;

            case AttributeUpgradeType.DoubleHealing:
                if (health == null || doubleHealingEnabled) return false;
                doubleHealingEnabled = true;
                health.SetHealingMultiplier(healingMultiplierUpgrade);
                return true;

            case AttributeUpgradeType.KillAreaDamage:
                killAreaDamage += killAreaDamagePerUpgrade;
                return true;
        }

        return false;
    }

    public void ApplyKillAreaDamage(Vector3 center, EnemyHealth killedEnemy)
    {
        if (killAreaDamage <= 0f || killAreaDamageRadius <= 0f || applyingKillAreaDamage)
            return;

        applyingKillAreaDamage = true;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, killAreaDamageRadius, killAreaDamageLayers);
        HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null || enemyHealth == killedEnemy || enemyHealth.IsDead || damagedEnemies.Contains(enemyHealth))
                continue;

            damagedEnemies.Add(enemyHealth);
            enemyHealth.TakeDamage(killAreaDamage);
        }

        applyingKillAreaDamage = false;
    }

    private string GetAttributeName(AttributeUpgradeType attribute)
    {
        switch (attribute)
        {
            case AttributeUpgradeType.SwordDamage:
                return "Dano da espada";
            case AttributeUpgradeType.MovementSpeed:
                return "Velocidade de movimento";
            case AttributeUpgradeType.GunDamage:
                return "Dano do projetil";
            case AttributeUpgradeType.GunProjectileSpeed:
                return "Velocidade do projetil";
            case AttributeUpgradeType.ParryDamage:
                return "Dano do parry";
            case AttributeUpgradeType.MaxHealth:
                return "Vida maxima";
            case AttributeUpgradeType.SwordPoison:
                return "Veneno da espada";
            case AttributeUpgradeType.BulletPoison:
                return "Veneno da bala";
            case AttributeUpgradeType.DoubleHealing:
                return "Curas dobradas";
            case AttributeUpgradeType.KillAreaDamage:
                return "Dano em area ao matar";
        }

        return "Atributo";
    }

    private string GetAttributeBonusText(AttributeUpgradeType attribute)
    {
        switch (attribute)
        {
            case AttributeUpgradeType.SwordDamage:
                return $"+{swordDamageLevelUpBonus}";
            case AttributeUpgradeType.MovementSpeed:
                return $"+{movementSpeedLevelUpBonus}";
            case AttributeUpgradeType.GunDamage:
                return $"+{gunDamageLevelUpBonus}";
            case AttributeUpgradeType.GunProjectileSpeed:
                return $"+{gunProjectileSpeedLevelUpBonus}";
            case AttributeUpgradeType.ParryDamage:
                return $"+{parryDamageLevelUpBonus}";
            case AttributeUpgradeType.MaxHealth:
                return $"+{healthLevelUpBonus}";
            case AttributeUpgradeType.SwordPoison:
                return $"+{poisonDamagePerUpgrade}/s";
            case AttributeUpgradeType.BulletPoison:
                return $"+{poisonDamagePerUpgrade}/s";
            case AttributeUpgradeType.DoubleHealing:
                return $"x{healingMultiplierUpgrade}";
            case AttributeUpgradeType.KillAreaDamage:
                return $"+{killAreaDamagePerUpgrade}";
        }

        return string.Empty;
    }

    private void EnsureStatReferences()
    {
        if (sword == null)
            sword = GetComponentInChildren<SwordAttack>();

        if (gun == null)
            gun = GetComponentInChildren<GunAttack>();

        if (parry == null)
            parry = GetComponent<PlayerParry>();

        if (health == null)
            health = GetComponent<PlayerHealth>();
    }

    private void IncreaseBaseMovementSpeed(float amount)
    {
        if (amount <= 0f) return;

        if (baseSpeed <= 0f)
            baseSpeed = speed;

        float currentMultiplier = baseSpeed > 0f ? speed / baseSpeed : 1f;
        baseSpeed += amount;
        speed = baseSpeed * Mathf.Max(0f, currentMultiplier);
    }

    private void UpdatePlayerLevelUI()
    {
        if (playerLevelText == null)
            playerLevelText = FindPlayerLevelText();

        if (playerLevelText != null)
            playerLevelText.text = playerLevel.ToString("00");
    }

    private TMP_Text FindPlayerLevelText()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);

        foreach (TMP_Text text in texts)
        {
            if (text == null || text.name != "LVL")
                continue;

            Transform parent = text.transform.parent;
            if (parent != null && parent.name == "PlayerLVL")
                return text;
        }

        foreach (TMP_Text text in texts)
        {
            if (text != null && text.name == "LVL")
                return text;
        }

        return null;
    }

    public void StartStun(float duration)
    {
        if (isStunned) return;
        if (quicksandCoroutine != null) StopCoroutine(quicksandCoroutine);
        StopAllCoroutines();
        StartCoroutine(StunSequence(duration));
    }

    IEnumerator StunSequence(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    void Update()
    {
        if (isExternallyLocked)
        {
            movement = Vector2.zero;
            if (anim != null) anim.SetBool("Run", false);
            return;
        }

        if (isStunned)
        {
            movement = Vector2.zero;
            UpdateAnimations();
            return;
        }

        if (isStuckInQuicksand)
        {
            movement = Vector2.zero;
            HandleEscapeInput();
            UpdateAnimations();

            if (maosVisual != null && maosVisual.activeSelf)
                AplicarEspasmoNasMaos();

            return;
        }

        HandleInput();
        HandleRotationAndAim();
        HandleWeaponSwitch();
        HandleAttackInput();
        HandleStamina();
        UpdateAnimations();

        if (!isInQuicksand)
            HandleWaterDepth();
    }

    void FixedUpdate()
    {
        // Se estiver atordoado ou preso, zera a velocidade
        if (isStuckInQuicksand || isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Se estiver travado externamente (ex: pelo Dash), NÃO zera a velocidade.
        // Apenas interrompe o movimento padrão, deixando o Dash usar o Rigidbody livremente.
        if (isExternallyLocked)
        {
            return;
        }

        // Movimento padrão
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    private void HandleWaterDepth()
    {
        if (mascaraPernas == null || visualPlayer == null) return;

        if (currentWaterCollider != null)
        {
            if (!mascaraPernas.activeSelf) mascaraPernas.SetActive(true);

            Bounds bounds = currentWaterCollider.bounds;
            float waterTop = bounds.max.y;
            float waterCenter = bounds.center.y;

            float depthPercentage = Mathf.InverseLerp(waterTop, waterCenter, transform.position.y);

            // Calcula as novas posições: a máscara sobe, o corpo desce
            float targetMaskY = Mathf.Lerp(alturaMascaraInicial, alturaMascaraFinal, depthPercentage);
            float targetVisualY = Mathf.Lerp(posYOriginalVisual, afundamentoMaximoY, depthPercentage);

            // Suaviza o movimento da máscara
            Vector3 maskPos = mascaraPernas.transform.localPosition;
            maskPos.y = Mathf.Lerp(maskPos.y, targetMaskY, Time.deltaTime * 5f);
            mascaraPernas.transform.localPosition = maskPos;

            // Suaviza o movimento do corpo do player
            Vector3 visualPos = visualPlayer.localPosition;
            visualPos.y = Mathf.Lerp(visualPos.y, targetVisualY, Time.deltaTime * 5f);
            visualPlayer.localPosition = visualPos;
        }
        else if (mascaraPernas.activeSelf && !isInQuicksand)
        {
            // Volta as posições ao normal suavemente antes de desativar
            Vector3 visualPos = visualPlayer.localPosition;
            visualPos.y = Mathf.Lerp(visualPos.y, posYOriginalVisual, Time.deltaTime * 10f);
            visualPlayer.localPosition = visualPos;

            if (Mathf.Abs(visualPos.y - posYOriginalVisual) < 0.05f)
            {
                visualPlayer.localPosition = new Vector3(visualPlayer.localPosition.x, posYOriginalVisual, visualPlayer.localPosition.z);
                mascaraPernas.SetActive(false);
            }
        }
    }

    private void SetSinkingVisuals(float maskY, float visualY)
    {
        if (mascaraPernas != null)
        {
            Vector3 maskPos = mascaraPernas.transform.localPosition;
            maskPos.y = maskY;
            mascaraPernas.transform.localPosition = maskPos;
        }

        if (visualPlayer != null)
        {
            Vector3 visualPos = visualPlayer.localPosition;
            visualPos.y = visualY;
            visualPlayer.localPosition = visualPos;
        }
    }

    private void StartQuicksandWaterRise()
    {
        if (mascaraPernas == null) return;

        if (quicksandWaterCoroutine != null)
            StopCoroutine(quicksandWaterCoroutine);

        SetSinkingVisuals(alturaMascaraInicial, posYOriginalVisual);
        mascaraPernas.SetActive(true);
        quicksandWaterCoroutine = StartCoroutine(AnimateQuicksandWaterRise());
    }

    private IEnumerator AnimateQuicksandWaterRise()
    {
        float duration = Mathf.Max(0.01f, duracaoAnimacaoAfundar);
        float elapsed = 0f;

        while (elapsed < duration && isInQuicksand)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            float currentMaskY = Mathf.Lerp(alturaMascaraInicial, alturaMascaraFinal, progress);
            float currentVisualY = Mathf.Lerp(posYOriginalVisual, afundamentoMaximoY, progress);

            SetSinkingVisuals(currentMaskY, currentVisualY);
            yield return null;
        }

        if (isInQuicksand)
            SetSinkingVisuals(alturaMascaraFinal, afundamentoMaximoY);

        quicksandWaterCoroutine = null;
    }

    private void StopQuicksandWaterRise(bool resetToInitialHeight)
    {
        if (quicksandWaterCoroutine != null)
        {
            StopCoroutine(quicksandWaterCoroutine);
            quicksandWaterCoroutine = null;
        }

        if (resetToInitialHeight)
            SetSinkingVisuals(alturaMascaraInicial, posYOriginalVisual);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
            currentWaterCollider = other;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            if (currentWaterCollider == other)
                currentWaterCollider = null;
        }
    }

    void HandleInput()
    {
        Vector2 inputDirection = Vector2.zero;
        var kb = Keyboard.current;

        if (kb.wKey.isPressed && !IsAttacking()) inputDirection.y = 1f;
        if (kb.sKey.isPressed && !IsAttacking()) inputDirection.y = -1f;
        if (kb.aKey.isPressed && !IsAttacking()) inputDirection.x = -1f;
        if (kb.dKey.isPressed && !IsAttacking()) inputDirection.x = 1f;

        movement = inputDirection.normalized;
    }

    void HandleWeaponSwitch()
    {
        if (gun != null && gun.currentAmmo < 1f)
            currentWeaponMode = WeaponMode.Sword;

        if (Keyboard.current.qKey.wasPressedThisFrame && gun != null && gun.currentAmmo > 0f)
            currentWeaponMode = (currentWeaponMode == WeaponMode.Sword) ? WeaponMode.Gun : WeaponMode.Sword;
    }

    void HandleAttackInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            switch (currentWeaponMode)
            {
                case WeaponMode.Sword: if (sword != null && !IsAttacking()) sword.Attack(); break;
                case WeaponMode.Gun: if (gun != null && !IsAttacking()) gun.Attack(); break;
            }
        }
    }

    void HandleStamina()
    {
        stamina = Mathf.Clamp(stamina + staminaRegenRate * Time.deltaTime, 0f, 100f);
    }

    private void HandleRotationAndAim()
    {
        if (mainCamera == null) return;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorld.z = 0f;

        Vector2 direction = (mouseWorld - transform.position).normalized;

        if (direction != Vector2.zero) LookDirection = direction;

        if (swordTransform != null && attackPoint != null)
        {
            float angle = Mathf.Atan2(LookDirection.y, LookDirection.x) * Mathf.Rad2Deg;
            swordTransform.rotation = Quaternion.Euler(0, 0, angle);
            float attackOffset = 0.8f;
            attackPoint.position = transform.position + (Vector3)(LookDirection * attackOffset);
        }

        if (movement.x < 0f)
        {
            if (facingRight) Flip();
        }
        else if (movement.x > 0f)
        {
            if (!facingRight) Flip();
        }
        else
        {
            if (LookDirection.x < 0 && facingRight) Flip();
            else if (LookDirection.x > 0 && !facingRight) Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        // Flip aplicado no transform principal afeta todo o objeto (incluindo Visual e Armas)
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        if (currentWeaponMode == WeaponMode.Sword)
        {
            anim.SetBool("Sword", true);
            anim.SetBool("Gun", false);
        }
        else if (currentWeaponMode == WeaponMode.Gun)
        {
            anim.SetBool("Sword", false);
            anim.SetBool("Gun", true);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && !IsAttacking())
            anim.SetTrigger("Attack");

        if (movement.magnitude > 0.1f && !isStunned && !isStuckInQuicksand && !sword.isAttacking)
            anim.SetBool("Run", true);
        else
            anim.SetBool("Run", false);

        if (Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed)
        {
            anim.SetBool("Down", false);
            anim.SetBool("Top", false);
        }

        if (movement.y > 0)
        {
            anim.SetBool("Down", false);
            anim.SetBool("Top", true);
        }
        else if (movement.y < 0)
        {
            anim.SetBool("Down", true);
            anim.SetBool("Top", false);
        }
    }

    public void EnterQuicksand()
    {
        if (isInQuicksand) return;
        isInQuicksand = true;

        currentWaterCollider = null;

        if (!isStunned && quicksandCoroutine != null) StopCoroutine(quicksandCoroutine);
        if (!isStunned) quicksandCoroutine = StartCoroutine(QuicksandSequence());
    }

    public void ExitQuicksand()
    {
        if (!isInQuicksand) return;
        isInQuicksand = false;

        ResetarVisuais();

        if (quicksandCoroutine != null) StopCoroutine(quicksandCoroutine);
        isStuckInQuicksand = false;
        ModifySpeed(1f);
    }

    private IEnumerator QuicksandSequence()
    {
        while (isInQuicksand)
        {
            StartQuicksandWaterRise();
            yield return new WaitForSeconds(quicksandGraceTime);
            if (!isInQuicksand) yield break;

            float timer = 0f;
            float startSpeed = baseSpeed;
            float endSpeed = baseSpeed * 0.5f;
            bool maosAtivadas = false;

            while (timer < quicksandSlowdownDuration)
            {
                if (!isInQuicksand)
                {
                    ModifySpeed(1f);
                    ResetarVisuais();
                    yield break;
                }
                if (isStunned) yield break;

                timer += Time.deltaTime;
                float progress = timer / quicksandSlowdownDuration;

                if (maosVisual != null)
                {
                    if (!maosAtivadas && timer >= atrasoParaAparecerMaos)
                    {
                        maosVisual.SetActive(true);
                        maosAtivadas = true;

                        if (maosAnimator != null)
                        {
                            maosAnimator.speed = 1;
                            float tempoRestante = quicksandSlowdownDuration - atrasoParaAparecerMaos;
                            if (tempoRestante > 0.1f)
                                maosAnimator.speed = duracaoOriginalClipAnimacao / tempoRestante;
                        }
                    }
                    if (maosAtivadas) AplicarEspasmoNasMaos();
                }

                speed = Mathf.Lerp(startSpeed, endSpeed, progress);
                yield return null;
            }

            speed = endSpeed;
            isStuckInQuicksand = true;
            currentEscapeClicks = 0;

            MostrarIconeEscape();

            if (maosAnimator != null)
            {
                maosAnimator.speed = 0;
                AnimatorStateInfo estadoAtual = maosAnimator.GetCurrentAnimatorStateInfo(0);
                maosAnimator.Play(estadoAtual.fullPathHash, 0, 1.0f);
            }

            SetSinkingVisuals(alturaMascaraFinal, afundamentoMaximoY);

            yield return new WaitUntil(() => !isStuckInQuicksand || !isInQuicksand);

            ResetarVisuais();

            if (!isInQuicksand) yield break;
            ModifySpeed(1f);
        }
    }

    private void ResetarVisuais()
    {
        DestruirIconeEscape();
        if (maosVisual != null) maosVisual.SetActive(false);
        if (maosAnimator != null) maosAnimator.speed = 1;
        StopQuicksandWaterRise(true);
        if (mascaraPernas != null) mascaraPernas.SetActive(false);
    }

    private void MostrarIconeEscape()
    {
        if (iconeEInstanciado != null) Destroy(iconeEInstanciado);

        if (prefabIconeE != null)
        {
            iconeEInstanciado = Instantiate(prefabIconeE, transform);
            iconeEInstanciado.transform.localPosition = new Vector3(0, alturaIconeE, 0);

            FloatingIcon script = iconeEInstanciado.GetComponent<FloatingIcon>();
            if (script != null)
                script.SetFloatingIcon(false);
        }
    }

    private void DestruirIconeEscape()
    {
        if (iconeEInstanciado != null) Destroy(iconeEInstanciado);
    }

    private void AnimarIconePulo()
    {
        if (iconeEInstanciado != null)
        {
            ActionIcon scriptIcone = iconeEInstanciado.GetComponent<ActionIcon>();
            if (scriptIcone != null)
                scriptIcone.Punch(1.4f, 0.1f);
        }
    }

    private void AplicarEspasmoNasMaos()
    {
        float ruidoX = Mathf.PerlinNoise(Time.time * velocidadeEspasmo, 0f);
        float ruidoY = Mathf.PerlinNoise(Time.time * velocidadeEspasmo, 50f);
        float deformacaoX = (ruidoX - 0.5f) * 2f;
        float deformacaoY = (ruidoY - 0.5f) * 2f;

        Vector3 novaEscala = escalaOriginalMaos;
        novaEscala.x += deformacaoX * intensidadeEspasmo;
        novaEscala.y += deformacaoY * intensidadeEspasmo;
        maosVisual.transform.localScale = novaEscala;
    }

    private void HandleEscapeInput()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            currentEscapeClicks++;
            AnimarIconePulo();

            if (maosAnimator != null)
            {
                AnimatorStateInfo estadoAtual = maosAnimator.GetCurrentAnimatorStateInfo(0);
                float progressoPreso = 1.0f - ((float)currentEscapeClicks / (float)quicksandEscapeClicks);
                progressoPreso = Mathf.Clamp01(progressoPreso);
                maosAnimator.Play(estadoAtual.fullPathHash, 0, progressoPreso);
                maosAnimator.Update(0f);
            }

            // Move o corpo e a máscara baseado nos cliques de fuga
            float progresso = 1.0f - ((float)currentEscapeClicks / (float)quicksandEscapeClicks);
            float currentMaskY = Mathf.Lerp(alturaMascaraInicial, alturaMascaraFinal, progresso);
            float currentVisualY = Mathf.Lerp(posYOriginalVisual, afundamentoMaximoY, progresso);

            SetSinkingVisuals(currentMaskY, currentVisualY);

            if (currentEscapeClicks >= quicksandEscapeClicks)
                isStuckInQuicksand = false;
        }
    }

    public void ModifySpeed(float multiplier)
    {
        speed = baseSpeed * multiplier;
    }

    public void SetExternalControlLock(bool locked)
    {
        isExternallyLocked = locked;
        movement = Vector2.zero;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (anim != null && locked)
            anim.SetBool("Run", false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            stamina -= 10f;
            stamina = Mathf.Clamp(stamina, 0f, 100f);
        }
    }

    bool IsPlaying(string animationName)
    {
        if (anim == null) return false;
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName);
    }

    bool IsAttacking()
    {
        if (IsPlaying("SashaAtack") || IsPlaying("AttackF") || IsPlaying("TiroTop") || IsPlaying("TiroSide") || IsPlaying("TiroDown"))
            return true;
        return false;
    }
}
