using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Reflection;

public class PlayerParry : MonoBehaviour
{
    [Header("Configuracoes do Parry")]
    public float parryWindowDuration = 0.45f;
    public float parryCooldown = 0.65f;
    public float counterDamageMultiplier = 1f;
    public float fallbackCounterDamage = 10f;
    public float counterDamageBonus = 0f;
    public float counterStunDuration = 1f;

    [Header("Feedback Visual")]
    public Color corParryAtivo = Color.cyan;
    public Color corParrySucesso = new Color(0.2f, 1f, 0.2f);
    public float duracaoCorSucesso = 0.18f;

    [Header("Referencias")]
    public SwordAttack swordAttack;

    public bool IsParrying { get; private set; } = false;

    private bool isOnCooldown = false;
    private bool isExternallyLocked = false;

    private Coroutine parryCoroutine;
    private Coroutine cooldownCoroutine;

    private SpriteRenderer playerSR;
    private Color originalColor;

    void Start()
    {
        playerSR = GetComponentInChildren<SpriteRenderer>();

        if (playerSR != null)
            originalColor = playerSR.color;

        if (swordAttack == null)
        {
            swordAttack = GetComponentInChildren<SwordAttack>();

            if (swordAttack == null)
                Debug.LogError("PlayerParry: referencia SwordAttack nao configurada.");
        }
    }

    void Update()
    {
        if (isExternallyLocked)
            return;

        if (!ApertouBotaoParry())
            return;

        if (FirstDamageParryTutorial.Instance != null)
            FirstDamageParryTutorial.Instance.FinalizarAoApertarParry();

        if (!isOnCooldown)
        {
            if (parryCoroutine != null)
                StopCoroutine(parryCoroutine);

            parryCoroutine = StartCoroutine(StartParryWindow());
        }
    }

    private bool ApertouBotaoParry()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            return true;

        return Input.GetMouseButtonDown(1);
    }

    private IEnumerator StartParryWindow()
    {
        isOnCooldown = true;
        IsParrying = true;

        if (playerSR != null)
            playerSR.color = corParryAtivo;

        yield return new WaitForSeconds(parryWindowDuration);

        if (IsParrying)
            IsParrying = false;

        if (playerSR != null)
            playerSR.color = originalColor;

        float tempoRestanteCooldown = Mathf.Max(0f, parryCooldown - parryWindowDuration);
        yield return new WaitForSeconds(tempoRestanteCooldown);

        isOnCooldown = false;
        parryCoroutine = null;
    }

    public bool TryParryAttack(GameObject enemyObject, float incomingDamage)
    {
        if (!IsParrying)
            return false;

        OnSuccessfulParryAndStun(enemyObject, incomingDamage);
        return true;
    }

    public void OnSuccessfulParryAndStun(GameObject enemyObject)
    {
        float danoDoInimigo = ObterDanoDoInimigo(enemyObject);
        OnSuccessfulParryAndStun(enemyObject, danoDoInimigo);
    }

    public void OnSuccessfulParryAndStun(GameObject enemyObject, float incomingDamage)
    {
        if (!IsParrying)
            return;

        IsParrying = false;

        if (parryCoroutine != null)
        {
            StopCoroutine(parryCoroutine);
            parryCoroutine = null;
        }

        float danoDevolvido = Mathf.Max(0f, incomingDamage) * counterDamageMultiplier;

        if (danoDevolvido <= 0f)
            danoDevolvido = fallbackCounterDamage;

        danoDevolvido += counterDamageBonus;

        EnemyHealth enemyHealth = null;

        if (enemyObject != null)
            enemyHealth = enemyObject.GetComponentInParent<EnemyHealth>();

        if (enemyHealth != null)
            enemyHealth.TakeCounterDamage(danoDevolvido, counterStunDuration);

        OnSuccessfulParryFeedback();
    }

    public void OnSuccessfulParryFeedback()
    {
        Debug.Log("Parry bem-sucedido!");

        if (playerSR != null)
            playerSR.color = corParrySucesso;

        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);

        cooldownCoroutine = StartCoroutine(ParryCooldown());
    }

    private IEnumerator ParryCooldown()
    {
        isOnCooldown = true;

        yield return new WaitForSeconds(duracaoCorSucesso);

        if (playerSR != null)
            playerSR.color = originalColor;

        float tempoRestante = Mathf.Max(0f, parryCooldown - duracaoCorSucesso);
        yield return new WaitForSeconds(tempoRestante);

        isOnCooldown = false;
        cooldownCoroutine = null;
    }

    private float ObterDanoDoInimigo(GameObject enemyObject)
    {
        if (enemyObject == null)
            return fallbackCounterDamage;

        MonoBehaviour[] componentes = enemyObject.GetComponentsInChildren<MonoBehaviour>(true);

        string[] nomesPossiveis =
        {
            "danoAoPlayer",
            "damage",
            "dano",
            "contactDamage",
            "attackDamage",
            "projectileDamage"
        };

        foreach (MonoBehaviour componente in componentes)
        {
            if (componente == null)
                continue;

            System.Type tipo = componente.GetType();

            foreach (string nome in nomesPossiveis)
            {
                FieldInfo campo = tipo.GetField(
                    nome,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (campo != null)
                {
                    object valor = campo.GetValue(componente);
                    float dano = ConverterParaFloat(valor);

                    if (dano > 0f)
                        return dano;
                }

                PropertyInfo propriedade = tipo.GetProperty(
                    nome,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (propriedade != null && propriedade.CanRead)
                {
                    object valor = propriedade.GetValue(componente);
                    float dano = ConverterParaFloat(valor);

                    if (dano > 0f)
                        return dano;
                }
            }
        }

        return fallbackCounterDamage;
    }

    private float ConverterParaFloat(object valor)
    {
        if (valor is float f)
            return f;

        if (valor is int i)
            return i;

        if (valor is double d)
            return (float)d;

        return 0f;
    }

    public void SetExternalControlLock(bool locked)
    {
        isExternallyLocked = locked;
    }

    public void IncreaseCounterDamageBonus(float amount)
    {
        if (amount <= 0f) return;

        counterDamageBonus += amount;
    }

    private void OnDisable()
    {
        IsParrying = false;

        if (playerSR != null)
            playerSR.color = originalColor;
    }

    private void OnDestroy()
    {
        if (playerSR != null)
            playerSR.color = originalColor;
    }
}
