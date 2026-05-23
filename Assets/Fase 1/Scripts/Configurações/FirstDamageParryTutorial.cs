using UnityEngine;
using System.Collections;

public class FirstDamageParryTutorial : MonoBehaviour
{
    public static FirstDamageParryTutorial Instance;

    [Header("Icone")]
    public GameObject prefabIconeMouseDireito;
    public float alturaIcone = 1.8f;

    [Header("Slow Motion")]
    public bool usarSlowMotion = true;

    [Range(0.01f, 1f)]
    public float escalaTempoTutorial = 0.15f;

    [Tooltip("Tempo real que o slow motion fica ativo.")]
    public float duracaoSlowMotion = 0.75f;

    private bool tutorialJaMostrado = false;
    private bool tutorialAtivo = false;

    private float timeScaleAnterior = 1f;
    private float fixedDeltaTimeAnterior;
    private GameObject iconeAtual;
    private Coroutine rotinaAtual;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TentarIniciarNoAvisoDeParry(PlayerHealth player)
    {
        if (tutorialJaMostrado || tutorialAtivo)
            return false;

        if (player == null)
            return false;

        tutorialJaMostrado = true;
        tutorialAtivo = true;

        rotinaAtual = StartCoroutine(RotinaTutorial(player));
        return true;
    }

    private IEnumerator RotinaTutorial(PlayerHealth player)
    {
        MostrarIconeNoPlayer(player.transform);
        AplicarSlowMotion();

        yield return new WaitForSecondsRealtime(duracaoSlowMotion);

        FinalizarTutorial();
    }

    public void FinalizarAoApertarParry()
    {
        if (!tutorialAtivo)
            return;

        if (rotinaAtual != null)
        {
            StopCoroutine(rotinaAtual);
            rotinaAtual = null;
        }

        FinalizarTutorial();
    }

    private void MostrarIconeNoPlayer(Transform player)
    {
        if (prefabIconeMouseDireito == null || player == null)
            return;

        if (iconeAtual != null)
            Destroy(iconeAtual);

        iconeAtual = Instantiate(prefabIconeMouseDireito, player);
        iconeAtual.transform.localPosition = new Vector3(0f, alturaIcone, 0f);

        FloatingIcon floating = iconeAtual.GetComponent<FloatingIcon>();
        if (floating != null)
            floating.SetFloatingIcon(true);
    }

    private void AplicarSlowMotion()
    {
        if (!usarSlowMotion)
            return;

        timeScaleAnterior = Time.timeScale;
        fixedDeltaTimeAnterior = Time.fixedDeltaTime;

        Time.timeScale = escalaTempoTutorial;
        Time.fixedDeltaTime = fixedDeltaTimeAnterior * escalaTempoTutorial;
    }

    private void FinalizarTutorial()
    {
        tutorialAtivo = false;
        rotinaAtual = null;

        if (iconeAtual != null)
            Destroy(iconeAtual);

        RestaurarTempo();
    }

    private void RestaurarTempo()
    {
        if (!usarSlowMotion)
            return;

        Time.timeScale = timeScaleAnterior <= 0f ? 1f : timeScaleAnterior;
        Time.fixedDeltaTime = fixedDeltaTimeAnterior;
    }

    // Mantem compatibilidade caso seu PlayerHealth ainda chame esse metodo antigo.
    public bool TentarCriarOportunidadeDeParry(
        PlayerHealth player,
        float dano,
        float stunDuration,
        GameObject atacante = null)
    {
        return false;
    }

    private void OnDisable()
    {
        if (rotinaAtual != null)
            StopCoroutine(rotinaAtual);

        RestaurarTempo();
    }

    private void OnDestroy()
    {
        RestaurarTempo();
    }
}
