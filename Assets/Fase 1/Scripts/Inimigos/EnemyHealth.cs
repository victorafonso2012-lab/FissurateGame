using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Configurações de Vida")]
    public float vidaMaxima = 30f;
    [SerializeField] private float vidaAtual;

    [Header("Referências Visuais")]
    public SpriteRenderer spriteRenderer;

    [Header("Configurações de Morte")]
    public string deathTriggerName = "Death";
    public string deathStateName = "Death";
    public bool esperarAnimacaoDeMorte = true;
    public float tempoFallbackDestruicao = 1.2f;
    public float tempoMaximoEsperandoAnimacao = 2f;

    [Header("XP")]
    public int experienciaAoMorrer = 5;
    public GameObject experienceOrbPrefab;
    public bool droparOrbDeXP = true;

    public bool IsDead { get; private set; }

    private Animator anim;
    private IEnemyAI enemyAI;
    private Coroutine deathCoroutine;
    private Coroutine poisonCoroutine;
    private readonly Dictionary<string, float> activePoisons = new Dictionary<string, float>();
    private bool recompensaEntregue = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        enemyAI = GetComponent<IEnemyAI>();
        vidaAtual = vidaMaxima;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(float damageAmount)
    {
        TomarDano(damageAmount);
    }

    public void TomarDano(float dano)
    {
        if (IsDead || dano <= 0f) return;

        if (enemyAI != null && !enemyAI.IsVulnerableToDamage())
            return;

        vidaAtual -= dano;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        if (vidaAtual <= 0f)
        {
            Morrer();
        }
        else
        {
            // Se não morreu, toca a animação de Dano
            TocarAnimacaoDeHit();
        }
    }

    public void TakeCounterDamage(float damageAmount, float stunDuration)
    {
        if (IsDead || damageAmount <= 0f) return;

        vidaAtual -= damageAmount;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        if (vidaAtual <= 0f)
        {
            Morrer();
            return;
        }

        // Se não morreu, toca a animação de Dano
        TocarAnimacaoDeHit();

        if (enemyAI != null && stunDuration > 0f)
            enemyAI.StartStun(stunDuration);
    }

    public void ApplyPoison(string sourceId, float damagePerSecond)
    {
        if (IsDead || damagePerSecond <= 0f)
            return;

        if (string.IsNullOrEmpty(sourceId))
            sourceId = "default";

        activePoisons[sourceId] = damagePerSecond;

        if (poisonCoroutine == null)
            poisonCoroutine = StartCoroutine(PoisonRoutine());
    }

    private IEnumerator PoisonRoutine()
    {
        WaitForSeconds tickDelay = new WaitForSeconds(1f);

        while (!IsDead && activePoisons.Count > 0)
        {
            yield return tickDelay;

            if (IsDead)
                break;

            float totalPoisonDamage = 0f;
            foreach (float damagePerSecond in activePoisons.Values)
                totalPoisonDamage += damagePerSecond;

            TomarDano(totalPoisonDamage);
        }

        poisonCoroutine = null;
    }

    private void TocarAnimacaoDeHit()
    {
        // Busca o script específico do Espantalho para pausar o Idle/Run e tocar o Hit
        EspantalhoAI espantalho = GetComponent<EspantalhoAI>();
        if (espantalho != null)
        {
            espantalho.TriggerHitAnimation();
        }
        else if (anim != null)
        {
            // Fallback genérico caso outro inimigo use esse script
            anim.Play("Hit");
        }
    }

    private void Morrer()
    {
        if (IsDead) return;
        IsDead = true;

        RecarregarArmaDoPlayer();
        EntregarRecompensaDeXP();
        PlayerMove.instance?.ApplyKillAreaDamage(transform.position, this);

        // O AI já dá "anim.Play("Death")". Usar o SetTrigger ao mesmo tempo pode bugar a transição.
        // Se o enemyAI for nulo, usamos o Trigger por segurança.
        if (enemyAI != null)
            enemyAI.SetDeadState();
        else if (anim != null && !string.IsNullOrEmpty(deathTriggerName))
            anim.SetTrigger(deathTriggerName);

        if (deathCoroutine != null)
            StopCoroutine(deathCoroutine);

        deathCoroutine = StartCoroutine(RotinaDeMorte());
    }

    private void RecarregarArmaDoPlayer()
    {
        if (PlayerMove.instance == null) return;
        if (PlayerMove.instance.gun == null) return;

        PlayerMove.instance.gun.ReloadAmmo();
    }

    private void EntregarRecompensaDeXP()
    {
        if (recompensaEntregue) return;
        recompensaEntregue = true;

        if (experienciaAoMorrer <= 0)
            return;

        if (droparOrbDeXP && experienceOrbPrefab != null)
        {
            GameObject orb = Instantiate(experienceOrbPrefab, transform.position, Quaternion.identity);
            ExperienceOrb experienceOrb = orb.GetComponent<ExperienceOrb>();
            if (experienceOrb == null)
                experienceOrb = orb.AddComponent<ExperienceOrb>();

            experienceOrb.SetExperienceAmount(experienciaAoMorrer);
            return;
        }

        if (ExperienceManager.instance != null)
            ExperienceManager.instance.AddExperience(experienciaAoMorrer);
    }

    private IEnumerator RotinaDeMorte()
    {
        if (anim == null || !esperarAnimacaoDeMorte)
        {
            yield return new WaitForSeconds(tempoFallbackDestruicao);

            if (this != null && gameObject != null)
                Destroy(gameObject);

            yield break;
        }

        float tempo = 0f;
        bool entrouNoStateDeMorte = false;

        while (tempo < tempoMaximoEsperandoAnimacao)
        {
            if (anim == null)
                yield break;

            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            if (!anim.IsInTransition(0) && stateInfo.IsName(deathStateName))
            {
                entrouNoStateDeMorte = true;

                if (stateInfo.normalizedTime >= 1f)
                    break;
            }

            tempo += Time.deltaTime;
            yield return null;
        }

        if (!entrouNoStateDeMorte)
            yield return new WaitForSeconds(tempoFallbackDestruicao);

        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    public float GetCurrentHealth()
    {
        return vidaAtual;
    }

    public void ResetHealth()
    {
        IsDead = false;
        recompensaEntregue = false;
        activePoisons.Clear();
        vidaAtual = vidaMaxima;
    }
}
