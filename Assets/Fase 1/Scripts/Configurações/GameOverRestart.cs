using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour
{
    [Header("Cenas ignoradas")]
    public string persistentSceneName = "PersistentScene";
    public string loadingSceneName = "LoadingScreen";
    public string mainMenuSceneName = "MainMenu";
    public string gameOverSceneName = "GameOver";

    [Header("Fallback")]
    [Tooltip("Usada se o script não conseguir descobrir a cena atual de gameplay.")]
    public string fallbackLevelSceneName = "Fase1";

    [Tooltip("Spawn inicial da fase reiniciada.")]
    public string restartSpawnPointName = "Spawn_Inicio_Fase1";

    [Header("Visual")]
    public bool useFadeOnRestart = false;

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        PhoneSystemController phone = Object.FindFirstObjectByType<PhoneSystemController>();
        if (phone != null)
            phone.ResetAfterRestart();

        ResetPlayerState();

        string sceneToRestart = GetCurrentGameplaySceneName();
        if (string.IsNullOrEmpty(sceneToRestart))
            sceneToRestart = fallbackLevelSceneName;

        Debug.Log($"[RestartButton] Reiniciando cena: {sceneToRestart}");

        GameLoader.LoadScene(
            sceneToRestart,
            restartSpawnPointName,
            null,
            useFadeOnRestart ? SceneTransitionVisual.Fade : SceneTransitionVisual.None
        );
    }

    private void ResetPlayerState()
    {
        PlayerMove player = PlayerMove.instance;
        if (player == null)
            player = Object.FindFirstObjectByType<PlayerMove>();

        if (player == null)
        {
            Debug.LogWarning("[RestartButton] PlayerMove não encontrado para reset.");
            return;
        }

        PlayerHealth hp = player.GetComponent<PlayerHealth>();
        if (hp != null)
            hp.ResetForRestart();

        player.enabled = true;
        player.StartStun(0f);

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }

    private string GetCurrentGameplaySceneName()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (IsGameplayScene(activeScene.name))
            return activeScene.name;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;

            if (IsGameplayScene(s.name))
                return s.name;
        }

        return null;
    }

    private bool IsGameplayScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        if (sceneName == persistentSceneName) return false;
        if (sceneName == loadingSceneName) return false;
        if (sceneName == mainMenuSceneName) return false;
        if (sceneName == gameOverSceneName) return false;
        return true;
    }
}
