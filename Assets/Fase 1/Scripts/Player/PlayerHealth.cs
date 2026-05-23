using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Sistema do Celular")]
    public PhoneSystemController phoneSystem;

    [Header("Configurações de Vida")]
    public float vidaMaxima = 100f;
    [SerializeField] private float vidaAtual;

    [Header("Slider da Vida")]
    public Slider sliderVida;

    private PlayerMove playerMove;
    private PlayerDash playerDash;
    private float healingMultiplier = 1f;
    public event Action OnHealthChanged;

    void Awake()
    {
        vidaAtual = vidaMaxima;
        playerMove = GetComponent<PlayerMove>();
        playerDash = GetComponent<PlayerDash>();
    }

    void Start()
    {
        if (phoneSystem == null)
            phoneSystem = FindAnyObjectByType<PhoneSystemController>();

        if (playerMove == null)
            Debug.LogError("PlayerHealth requer PlayerMove.");

        AtualizarSlider();
    }

    public void TakeDamage(float damageAmount)
    {
        TomarDano(damageAmount, 0f);
    }

    public void TomarDano(float dano, float stunDuration = 0f)
    {
        TomarDano(dano, stunDuration, null);
    }

    public void TomarDano(float dano, float stunDuration, GameObject atacante)
    {
        if (vidaAtual <= 0f)
            return;

        if (playerDash != null && playerDash.IsDashing())
            return;

        if (FirstDamageParryTutorial.Instance != null &&
            FirstDamageParryTutorial.Instance.TentarCriarOportunidadeDeParry(this, dano, stunDuration, atacante))
        {
            return;
        }

        AplicarDanoSemTutorial(dano, stunDuration);
    }

    public void AplicarDanoSemTutorial(float dano, float stunDuration = 0f)
    {
        if (vidaAtual <= 0f)
            return;

        vidaAtual -= dano;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        AtualizarSlider();

        if (vidaAtual > 0f && playerMove != null && stunDuration > 0f)
            playerMove.StartStun(stunDuration);

        if (vidaAtual <= 0f)
            Morrer();
    }

    public void Curar(float cura)
    {
        if (vidaAtual <= 0f)
            return;

        vidaAtual += cura * healingMultiplier;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        AtualizarSlider();
    }

    public void SetHealingMultiplier(float multiplier)
    {
        healingMultiplier = Mathf.Max(1f, multiplier);
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (amount <= 0f)
            return;

        vidaMaxima += amount;
        vidaAtual += amount;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        AtualizarSlider();
    }

    public void ResetForRestart()
    {
        vidaAtual = vidaMaxima;
        AtualizarSlider();

        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();

        if (playerDash == null)
            playerDash = GetComponent<PlayerDash>();

        if (playerMove != null)
            playerMove.enabled = true;
    }

    private void AtualizarSlider()
    {
        if (sliderVida != null)
            sliderVida.value = vidaAtual / vidaMaxima;

        OnHealthChanged?.Invoke();
    }

    private void Morrer()
    {
        Debug.Log("Player morreu!");

        if (playerMove != null)
            playerMove.enabled = false;

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
            anim.SetTrigger("Death");

        if (phoneSystem != null)
            phoneSystem.TriggerGameOver();
    }

    public float GetCurrentHealth()
    {
        return vidaAtual;
    }
}