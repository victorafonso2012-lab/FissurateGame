using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class SceneFader : MonoBehaviour
{
    public static SceneFader instance;

    private CanvasGroup canvasGroup;
    private Coroutine currentFadeRoutine;

    [Header("Configurações")]
    public float fadeDuration = 1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public void StartFadeIn()
    {
        if (currentFadeRoutine != null)
            StopCoroutine(currentFadeRoutine);

        currentFadeRoutine = StartCoroutine(FadeRoutine(0f, 1f, true));
    }

    public void StartFadeOut()
    {
        if (currentFadeRoutine != null)
            StopCoroutine(currentFadeRoutine);

        currentFadeRoutine = StartCoroutine(FadeRoutine(1f, 0f, false));
    }

    public IEnumerator FadeInAndWait()
    {
        if (currentFadeRoutine != null)
            StopCoroutine(currentFadeRoutine);

        yield return StartCoroutine(FadeRoutine(0f, 1f, true));
        currentFadeRoutine = null;
    }

    public IEnumerator FadeOutAndWait()
    {
        if (currentFadeRoutine != null)
            StopCoroutine(currentFadeRoutine);

        yield return StartCoroutine(FadeRoutine(1f, 0f, false));
        currentFadeRoutine = null;
    }

    public void UnloadLoadingSceneAndFadeOut(string loadingSceneName)
    {
        StartCoroutine(UnloadLoadingSceneAndFadeOutRoutine(loadingSceneName));
    }

    private IEnumerator UnloadLoadingSceneAndFadeOutRoutine(string loadingSceneName)
    {
        if (!string.IsNullOrEmpty(loadingSceneName))
        {
            Scene loadingScene = SceneManager.GetSceneByName(loadingSceneName);
            if (loadingScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(loadingScene);
                while (unload != null && !unload.isDone)
                    yield return null;
            }
        }

        yield return StartCoroutine(FadeOutAndWait());
    }

    private IEnumerator FadeRoutine(float from, float to, bool blockRaycasts)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = from;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.blocksRaycasts = blockRaycasts;
    }
}
