using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthTextUpdater : MonoBehaviour
{
    public Slider healthSlider;
    public PlayerHealth playerHealth;

    private TextMeshProUGUI healthText;

    void Start()
    {
        healthText = GetComponent<TextMeshProUGUI>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (healthSlider != null)
        {
            // Mantemos o listener do slider por segurança
            healthSlider.onValueChanged.AddListener(UpdateHealthText);
        }

        if (playerHealth != null)
        {
            // Fazemos o texto ESCUTAR o novo evento que criamos no PlayerHealth
            playerHealth.OnHealthChanged += AtualizarTextoManual;

            // Atualiza a vida na tela logo que o jogo começa
            AtualizarTextoManual();
        }
    }

    // Boa prática: Sempre que nos inscrevemos em um evento (+), 
    // devemos nos desinscrever (-) quando o objeto for destruído
    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= AtualizarTextoManual;
        }
    }

    // Método exigido pelo Slider (quando ele mudar, ele chama nosso método manual)
    public void UpdateHealthText(float currentSliderValue)
    {
        AtualizarTextoManual();
    }

    // Nosso método definitivo que pega a vida e atualiza a UI
    public void AtualizarTextoManual()
    {
        if (playerHealth != null && healthText != null)
        {
            float currentHealth = playerHealth.GetCurrentHealth();
            float maxHealth = playerHealth.vidaMaxima;

            int vidaAtualInt = Mathf.CeilToInt(currentHealth);
            int vidaMaximaInt = Mathf.CeilToInt(maxHealth);

            healthText.text = $"{vidaAtualInt} / {vidaMaximaInt}";
        }
    }
}