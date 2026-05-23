using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialMovementPrompt : MonoBehaviour
{
    [Header("Cena")]
    public string nomeCenaTutorial = "Game_Tutorial";
    public string nomeCenaLoading = "LoadingScreen";
    public string nomeCenaPersistente = "PersistentScene";

    [Header("Configurações")]
    public float tempoParaSumir = 1.5f;
    public float velocidadeFlutuar = 5f;
    public float alturaFlutuar = 0.1f;

    private bool jaComecouAndar = false;
    private bool estaSumindo = false;

    private Vector3 posicaoInicialLocal;
    private float escalaOriginalX;

    private SpriteRenderer[] spriteRenderers;
    private Color[] coresOriginais;

    void Start()
    {
        posicaoInicialLocal = transform.localPosition;
        escalaOriginalX = transform.localScale.x;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        coresOriginais = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            coresOriginais[i] = spriteRenderers[i].color;
        }

        SetSpritesVisiveis(false);
    }

    void Update()
    {
        if (!PodeRodarNaCenaAtual())
            return;

        float novoY = posicaoInicialLocal.y + Mathf.Sin(Time.time * velocidadeFlutuar) * alturaFlutuar;
        transform.localPosition = new Vector3(posicaoInicialLocal.x, novoY, posicaoInicialLocal.z);

        if (!jaComecouAndar && DetectouMovimento())
        {
            jaComecouAndar = true;
            StartCoroutine(SumirSuavemente());
        }
    }

    void LateUpdate()
    {
        if (!PodeFicarVisivelNaCenaAtual())
            return;

        transform.rotation = Quaternion.identity;

        if (transform.parent != null)
        {
            float paiScaleX = transform.parent.localScale.x;
            float direcao = Mathf.Sign(paiScaleX);

            if (direcao == 0f)
                direcao = 1f;

            Vector3 escalaAtual = transform.localScale;
            escalaAtual.x = direcao * Mathf.Abs(escalaOriginalX);
            transform.localScale = escalaAtual;
        }
    }

    private bool PodeRodarNaCenaAtual()
    {
        string cenaAtual = SceneManager.GetActiveScene().name;

        if (cenaAtual == nomeCenaTutorial)
        {
            if (!estaSumindo)
                SetSpritesVisiveis(true);

            return true;
        }

        SetSpritesVisiveis(false);

        if (cenaAtual != nomeCenaLoading && cenaAtual != nomeCenaPersistente)
            Destroy(gameObject);

        return false;
    }

    private bool PodeFicarVisivelNaCenaAtual()
    {
        return SceneManager.GetActiveScene().name == nomeCenaTutorial;
    }

    private bool DetectouMovimento()
    {
        Keyboard teclado = Keyboard.current;

        if (teclado != null)
        {
            if (teclado.wKey.isPressed || teclado.aKey.isPressed ||
                teclado.sKey.isPressed || teclado.dKey.isPressed ||
                teclado.upArrowKey.isPressed || teclado.downArrowKey.isPressed ||
                teclado.leftArrowKey.isPressed || teclado.rightArrowKey.isPressed)
            {
                return true;
            }
        }

        Gamepad controle = Gamepad.current;

        if (controle != null)
        {
            if (controle.leftStick.ReadValue().sqrMagnitude > 0.01f ||
                controle.dpad.ReadValue().sqrMagnitude > 0.01f)
            {
                return true;
            }
        }

        return false;
    }

    private void SetSpritesVisiveis(bool visivel)
    {
        if (spriteRenderers == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].enabled = visivel;
        }
    }

    IEnumerator SumirSuavemente()
    {
        estaSumindo = true;

        yield return new WaitForSeconds(tempoParaSumir);

        float duracaoFade = 1.0f;
        float tempo = 0f;

        while (tempo < duracaoFade)
        {
            tempo += Time.deltaTime;
            float progresso = Mathf.Clamp01(tempo / duracaoFade);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                    continue;

                Color corInicial = coresOriginais[i];
                float alpha = Mathf.Lerp(corInicial.a, 0f, progresso);

                spriteRenderers[i].color = new Color(
                    corInicial.r,
                    corInicial.g,
                    corInicial.b,
                    alpha
                );
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}