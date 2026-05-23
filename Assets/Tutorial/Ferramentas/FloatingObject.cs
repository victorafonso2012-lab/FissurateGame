using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Movimento vertical")]
    [Tooltip("Quanto o objeto fica abaixo da posição original ao começar escondido")]
    public float hiddenOffsetY = -10f;

    [Tooltip("Velocidade de subida")]
    public float riseSpeed = 3f;

    [Tooltip("Velocidade de descida")]
    public float fallSpeed = 2f;

    [Tooltip("Amplitude da flutuação")]
    public float floatAmplitude = 0.5f;

    [Tooltip("Velocidade da flutuação")]
    public float floatSpeed = 1.5f;

    [Header("Parallax opcional")]
    public bool useParallax = false;
    public Transform player;
    public float parallaxMultiplier = -0.05f;

    private bool canFloat = false;
    private bool isReturning = false;
    private float riseProgress = 0f;
    private float fallProgress = 0f;
    private float phaseOffset;

    private Vector3 basePosition;
    private Vector3 hiddenPosition;
    private Vector3 returnStartPosition;
    private float playerStartX;

    void Start()
    {
        phaseOffset = Random.Range(0f, 2f * Mathf.PI);

        basePosition = transform.position;
        hiddenPosition = basePosition + new Vector3(0f, hiddenOffsetY, 0f);

        transform.position = hiddenPosition;

        if (player == null && PlayerMove.instance != null)
            player = PlayerMove.instance.transform;

        if (player != null)
            playerStartX = player.position.x;
    }

    void LateUpdate()
    {
        if (canFloat && !isReturning)
        {
            if (riseProgress < 1f)
            {
                riseProgress += Time.deltaTime * riseSpeed;
                riseProgress = Mathf.Clamp01(riseProgress);

                // Sobe até a posição ORIGINAL do editor
                transform.position = Vector3.Lerp(hiddenPosition, basePosition, riseProgress);
            }
            else
            {
                float floatY = Mathf.Sin(Time.time * floatSpeed + phaseOffset) * floatAmplitude;

                float currentX = basePosition.x;

                if (useParallax && player != null)
                {
                    float deltaPlayerX = player.position.x - playerStartX;
                    currentX += deltaPlayerX * parallaxMultiplier;
                }

                transform.position = new Vector3(
                    currentX,
                    basePosition.y + floatY,
                    basePosition.z
                );
            }
        }
        else if (isReturning)
        {
            if (fallProgress < 1f)
            {
                fallProgress += Time.deltaTime * fallSpeed;
                fallProgress = Mathf.Clamp01(fallProgress);

                // Volta suavemente da posição atual para escondido, sem "snap"
                transform.position = Vector3.Lerp(returnStartPosition, hiddenPosition, fallProgress);
            }
            else
            {
                isReturning = false;
                canFloat = false;
                riseProgress = 0f;
                fallProgress = 0f;
                transform.position = hiddenPosition;
            }
        }
    }

    public void Activate()
    {
        canFloat = true;
        isReturning = false;
        riseProgress = 0f;
        fallProgress = 0f;

        if (player == null && PlayerMove.instance != null)
            player = PlayerMove.instance.transform;

        if (player != null)
            playerStartX = player.position.x;
    }

    public void Deactivate()
    {
        isReturning = true;
        canFloat = false;
        fallProgress = 0f;
        returnStartPosition = transform.position;
    }
}