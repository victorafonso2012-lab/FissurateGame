using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneTrigger : MonoBehaviour
{
    [Header("Configuração da Transição")]
    public string sceneToLoad;
    public string sceneToUnload;
    public string targetSpawnPointName;

    [Header("Visual")]
    public bool useFade = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || hasTriggered)
            return;

        hasTriggered = true;

        GameLoader.LoadScene(
            sceneToLoad,
            targetSpawnPointName,
            sceneToUnload,
            useFade ? SceneTransitionVisual.Fade : SceneTransitionVisual.None
        );
    }
}
