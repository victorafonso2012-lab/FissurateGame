using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    [Header("Referências da UI")]
    public Slider progressBar;

    [Header("Configuração de Cenas")]
    public string persistentSceneName = "PersistentScene";
    public string loadingSceneName = "LoadingScreen";
    public string fallbackSceneName = "MainMenu";

    [Header("Segurança")]
    public float playerWaitTimeout = 3f;

    [Header("Sincronia Visual")]
    [Tooltip("Tempo mínimo que o loading fica visível, mesmo se a cena carregar muito rápido.")]
    public float minimumLoadingScreenTime = 0.35f;

    [Tooltip("Tempo máximo esperando a câmera parar de se mover antes de liberar a cena.")]
    public float cameraSettleTimeout = 1.5f;

    [Tooltip("Quão pequeno deve ser o movimento da câmera para considerarmos que ela estabilizou.")]
    public float cameraMovementThreshold = 0.02f;

    [Tooltip("Quantos frames seguidos a câmera precisa ficar estável.")]
    public int requiredStableFrames = 3;

    [Header("Atraso Extra")]
    [Tooltip("Tempo extra segurando a tela de loading antes de revelar a cena.")]
    public float extraRevealDelay = 3f;

    private void Start()
    {
        string sceneToLoad = GameLoader.nextSceneToLoad;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("[LoadingManager] Nenhuma cena foi solicitada. Voltando ao menu.");
            SceneManager.LoadScene(fallbackSceneName);
            return;
        }

        StartCoroutine(LoadScenesAsync(
            sceneToLoad,
            GameLoader.nextSpawnPointName,
            GameLoader.nextSceneToUnload,
            GameLoader.nextTransitionVisual
        ));
    }

    private IEnumerator LoadScenesAsync(
        string levelSceneName,
        string spawnPointName,
        string sceneToUnload,
        SceneTransitionVisual transitionVisual)
    {
        float loadingStartTime = Time.unscaledTime;
        bool useFade = transitionVisual == SceneTransitionVisual.Fade && SceneFader.instance != null;

        if (progressBar != null)
            progressBar.value = 0f;

        if (useFade)
            yield return StartCoroutine(SceneFader.instance.FadeInAndWait());

        bool hasPersistentRoot = Object.FindFirstObjectByType<PersistentRoot>() != null;
        var loadOperations = new List<AsyncOperation>();

        if (!hasPersistentRoot)
        {
            Scene persistentScene = SceneManager.GetSceneByName(persistentSceneName);
            if (!persistentScene.isLoaded)
            {
                AsyncOperation persistentLoad = SceneManager.LoadSceneAsync(persistentSceneName, LoadSceneMode.Additive);
                if (persistentLoad != null)
                    loadOperations.Add(persistentLoad);
            }
        }

        yield return StartCoroutine(UnloadSceneIfLoaded(levelSceneName));

        if (!string.IsNullOrEmpty(sceneToUnload) &&
            sceneToUnload != loadingSceneName &&
            sceneToUnload != persistentSceneName &&
            sceneToUnload != levelSceneName)
        {
            yield return StartCoroutine(UnloadSceneIfLoaded(sceneToUnload));
        }

        AsyncOperation levelLoad = SceneManager.LoadSceneAsync(levelSceneName, LoadSceneMode.Additive);
        if (levelLoad != null)
            loadOperations.Add(levelLoad);

        yield return StartCoroutine(UpdateProgress(loadOperations));

        Scene levelScene = SceneManager.GetSceneByName(levelSceneName);
        if (levelScene.isLoaded)
            SceneManager.SetActiveScene(levelScene);

        // Deixa Awake/OnEnable/Start da cena nova rodarem
        yield return null;

        yield return StartCoroutine(WaitForPlayer(playerWaitTimeout));

        PlayerMove player = GetPlayerMoveSafe();
        SpawnPoint spawnPoint = FindSpawnPoint(spawnPointName);

        if (player != null && spawnPoint != null)
        {
            MovePlayerToSpawn(player, spawnPoint.transform.position);
            Debug.Log($"[LoadingManager] Player movido para {spawnPoint.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[LoadingManager] Falha ao posicionar Player. Player nulo? {player == null} / Spawn nulo? {spawnPoint == null}");
        }

        if (progressBar != null)
            progressBar.value = 0.95f;

        // Espera a câmera reagir ao novo spawn
        yield return null;
        yield return new WaitForEndOfFrame();

        yield return StartCoroutine(WaitForCameraToSettle());
        yield return StartCoroutine(WaitMinimumLoadingTime(loadingStartTime));

        if (extraRevealDelay > 0f)
            yield return new WaitForSecondsRealtime(extraRevealDelay);

        if (progressBar != null)
            progressBar.value = 1f;

        GameLoader.ClearPendingRequest();

        if (useFade && SceneFader.instance != null)
        {
            SceneFader.instance.UnloadLoadingSceneAndFadeOut(loadingSceneName);
            yield break;
        }

        SceneManager.UnloadSceneAsync(loadingSceneName);
    }

    private IEnumerator UpdateProgress(List<AsyncOperation> operations)
    {
        if (operations == null || operations.Count == 0)
        {
            if (progressBar != null)
                progressBar.value = 1f;

            yield break;
        }

        bool allDone = false;

        while (!allDone)
        {
            allDone = true;
            float totalProgress = 0f;

            foreach (AsyncOperation op in operations)
            {
                if (op == null) continue;

                totalProgress += Mathf.Clamp01(op.progress / 0.9f);

                if (!op.isDone)
                    allDone = false;
            }

            if (progressBar != null)
                progressBar.value = totalProgress / operations.Count;

            yield return null;
        }

        if (progressBar != null)
            progressBar.value = 1f;
    }

    private IEnumerator UnloadSceneIfLoaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            yield break;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
            yield break;

        AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
        while (unload != null && !unload.isDone)
            yield return null;
    }

    private void MovePlayerToSpawn(PlayerMove player, Vector3 spawnPosition)
    {
        player.transform.position = spawnPosition;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private IEnumerator WaitForCameraToSettle()
    {
        float timer = 0f;
        int stableFrames = 0;
        Camera cam = Camera.main;

        while (cam == null && timer < cameraSettleTimeout)
        {
            cam = Camera.main;
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (cam == null)
            yield break;

        Vector3 lastPos = cam.transform.position;
        timer = 0f;

        while (timer < cameraSettleTimeout)
        {
            yield return new WaitForEndOfFrame();

            if (Camera.main != null)
                cam = Camera.main;

            Vector3 currentPos = cam.transform.position;
            float sqrMove = (currentPos - lastPos).sqrMagnitude;

            if (sqrMove <= cameraMovementThreshold * cameraMovementThreshold)
            {
                stableFrames++;
                if (stableFrames >= requiredStableFrames)
                    yield break;
            }
            else
            {
                stableFrames = 0;
            }

            lastPos = currentPos;
            timer += Time.unscaledDeltaTime;
        }
    }

    private IEnumerator WaitMinimumLoadingTime(float loadingStartTime)
    {
        float elapsed = Time.unscaledTime - loadingStartTime;
        float remaining = minimumLoadingScreenTime - elapsed;

        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private PlayerMove GetPlayerMoveSafe()
    {
        if (PlayerMove.instance != null)
            return PlayerMove.instance;

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return null;

        return go.GetComponent<PlayerMove>();
    }

    private IEnumerator WaitForPlayer(float timeout)
    {
        float t = 0f;

        while (t < timeout)
        {
            if (PlayerMove.instance != null)
                yield break;

            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null && go.GetComponent<PlayerMove>() != null)
                yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning($"[LoadingManager] Timeout esperando Player ({timeout:0.0}s).");
    }

    private SpawnPoint FindSpawnPoint(string spawnName)
    {
        if (string.IsNullOrEmpty(spawnName))
        {
            Debug.LogWarning("[LoadingManager] Nome do SpawnPoint vazio.");
            return null;
        }

        SpawnPoint[] allSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        foreach (SpawnPoint sp in allSpawnPoints)
        {
            if (sp != null && sp.gameObject.name == spawnName)
                return sp;
        }

        Debug.LogError($"[LoadingManager] SpawnPoint '{spawnName}' não encontrado.");
        return null;
    }
}
