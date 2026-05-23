using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using System.Collections;

public class IntroVideoPlayer : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Header("Fade")]
    public CanvasGroup fadePreto;
    public float duracaoFadeIn = 1f;
    public float duracaoFadeOut = 1f;

    [Header("Cena")]
    public string cenaDoMenu = "MainMenu";

    [Header("Opcoes")]
    public bool permitirPular = true;

    private bool carregandoMenu = false;

    void Start()
    {
        Time.timeScale = 1f;

        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            CarregarMenuDireto();
            return;
        }

        videoPlayer.loopPointReached += AoTerminarVideo;
        videoPlayer.Play();

        StartCoroutine(Fade(1f, 0f, duracaoFadeIn));
    }

    void Update()
    {
        if (!permitirPular || carregandoMenu)
            return;

        if (ApertouParaPular())
            StartCoroutine(CarregarMenuComFade());
    }

    private void AoTerminarVideo(VideoPlayer vp)
    {
        StartCoroutine(CarregarMenuComFade());
    }

    private IEnumerator CarregarMenuComFade()
    {
        if (carregandoMenu)
            yield break;

        carregandoMenu = true;

        if (videoPlayer != null)
            videoPlayer.loopPointReached -= AoTerminarVideo;

        yield return StartCoroutine(Fade(0f, 1f, duracaoFadeOut));

        SceneManager.LoadScene(cenaDoMenu);
    }

    private IEnumerator Fade(float inicio, float fim, float duracao)
    {
        if (fadePreto == null)
            yield break;

        fadePreto.blocksRaycasts = true;

        float tempo = 0f;
        fadePreto.alpha = inicio;

        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;
            float progresso = Mathf.Clamp01(tempo / duracao);
            fadePreto.alpha = Mathf.Lerp(inicio, fim, progresso);
            yield return null;
        }

        fadePreto.alpha = fim;
        fadePreto.blocksRaycasts = fim > 0f;
    }

    private bool ApertouParaPular()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            return true;

        return false;
    }

    private void CarregarMenuDireto()
    {
        SceneManager.LoadScene(cenaDoMenu);
    }
}