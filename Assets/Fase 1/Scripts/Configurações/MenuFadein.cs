using UnityEngine;
using System.Collections;

public class MenuFadeIn : MonoBehaviour
{
    public CanvasGroup fadePreto;
    public float duracaoFade = 1f;

    void Start()
    {
        if (fadePreto == null)
            fadePreto = GetComponent<CanvasGroup>();

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        if (fadePreto == null)
            yield break;

        fadePreto.alpha = 1f;
        fadePreto.blocksRaycasts = true;

        float tempo = 0f;

        while (tempo < duracaoFade)
        {
            tempo += Time.unscaledDeltaTime;
            float progresso = Mathf.Clamp01(tempo / duracaoFade);

            fadePreto.alpha = Mathf.Lerp(1f, 0f, progresso);

            yield return null;
        }

        fadePreto.alpha = 0f;
        fadePreto.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
}