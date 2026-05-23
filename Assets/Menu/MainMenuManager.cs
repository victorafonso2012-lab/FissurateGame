using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Toggle fullscreenToggle;
    public GameObject Options;
    public GameObject MainMenu;

    [Header("Start Game")]
    [Tooltip("Primeira cena de gameplay do jogo.")]
    public string firstSceneName = "Game_Tutorial";

    [Tooltip("Spawn inicial da primeira cena.")]
    public string firstSpawnPointName = "Spawn_Inicio_Tutorial";

    [Tooltip("Se verdadeiro, a entrada no jogo usa fade.")]
    public bool useFadeOnStart = true;

    private void Start()
    {
        Time.timeScale = 1f;

        if (Options != null) Options.SetActive(false);
        if (MainMenu != null) MainMenu.SetActive(true);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartGame()
    {
        GameLoader.LoadScene(
            firstSceneName,
            firstSpawnPointName,
            null,
            useFadeOnStart ? SceneTransitionVisual.Fade : SceneTransitionVisual.None
        );
    }

    public void OptionsButton()
    {
        if (Options != null) Options.SetActive(true);
        if (MainMenu != null) MainMenu.SetActive(false);
    }

    public void GoBack()
    {
        if (Options != null) Options.SetActive(false);
        if (MainMenu != null) MainMenu.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Fechando o jogo...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}
