using UnityEngine.SceneManagement;

public enum SceneTransitionVisual
{
    None,
    Fade
}

public static class GameLoader
{
    public static string loadingSceneName = "LoadingScreen";

    public static string nextSceneToLoad;
    public static string nextSpawnPointName;
    public static string nextSceneToUnload;
    public static SceneTransitionVisual nextTransitionVisual = SceneTransitionVisual.None;

    public static string currentLevelScene;
    public static string currentSpawnPoint;

    public static void LoadScene(string sceneName, string spawnName)
    {
        LoadScene(sceneName, spawnName, null, SceneTransitionVisual.None);
    }

    public static void LoadScene(
        string sceneName,
        string spawnName,
        string sceneToUnload,
        SceneTransitionVisual transitionVisual)
    {
        nextSceneToLoad = sceneName;
        nextSpawnPointName = spawnName;
        nextSceneToUnload = sceneToUnload;
        nextTransitionVisual = transitionVisual;

        currentLevelScene = sceneName;
        currentSpawnPoint = spawnName;

        SceneManager.LoadScene(loadingSceneName);
    }

    public static void ClearPendingRequest()
    {
        nextSceneToLoad = null;
        nextSpawnPointName = null;
        nextSceneToUnload = null;
        nextTransitionVisual = SceneTransitionVisual.None;
    }
}
