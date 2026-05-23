using UnityEngine;

public class SimpleParallax : MonoBehaviour
{
    [Header("Configurações de Parallax")]
    [Tooltip("Câmera do gameplay. Pode deixar vazio que o script tenta achar sozinho.")]
    public Transform cameraTransform;

    [Tooltip("Quão forte é o efeito no eixo X. 0 = não se move, 1 = acompanha a câmera.")]
    [Range(0f, 1f)]
    public float parallaxFactorX = 0.5f;

    [Tooltip("Quão forte é o efeito no eixo Y. 0 = não se move, 1 = acompanha a câmera.")]
    [Range(0f, 1f)]
    public float parallaxFactorY = 0.5f;

    [Header("Segurança")]
    [Tooltip("Se true, o script tenta se reconectar automaticamente quando a câmera mudar.")]
    public bool autoRebindCamera = true;

    private Vector3 startPosition;
    private Vector3 cameraStartPosition;
    private Camera cachedMainCamera;
    private bool hasInitialized = false;

    void Awake()
    {
        startPosition = transform.position;
    }

    void OnEnable()
    {
        // Quando a cena/objeto reativa, força nova tentativa de bind.
        hasInitialized = false;
        cachedMainCamera = null;
    }

    void LateUpdate()
    {
        if (!EnsureCamera())
            return;

        float deltaX = cameraTransform.position.x - cameraStartPosition.x;
        float deltaY = cameraTransform.position.y - cameraStartPosition.y;

        transform.position = new Vector3(
            startPosition.x + (deltaX * parallaxFactorX),
            startPosition.y + (deltaY * parallaxFactorY),
            startPosition.z
        );
    }

    private bool EnsureCamera()
    {
        if (cameraTransform != null)
        {
            if (!hasInitialized)
            {
                cameraStartPosition = cameraTransform.position;
                hasInitialized = true;
            }
            return true;
        }

        Camera main = Camera.main;
        if (main == null)
            return false;

        bool cameraChanged = cachedMainCamera != main;

        if (cameraChanged || !hasInitialized || autoRebindCamera)
        {
            cachedMainCamera = main;
            cameraTransform = main.transform;
            cameraStartPosition = cameraTransform.position;
            hasInitialized = true;

            // Mantém o objeto exatamente na posição em que foi colocado na cena
            transform.position = startPosition;
        }

        return true;
    }

    public void ForceRebind()
    {
        cameraTransform = null;
        cachedMainCamera = null;
        hasInitialized = false;
    }
}
