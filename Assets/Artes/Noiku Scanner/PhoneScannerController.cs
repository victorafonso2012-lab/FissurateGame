using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class PhoneSystemController : MonoBehaviour
{
    static readonly int LevelUpCameraGlitchIntensityProperty = Shader.PropertyToID("_LevelUpCameraGlitchIntensity");
    static readonly int LevelUpCameraGlitchTimeProperty = Shader.PropertyToID("_LevelUpCameraGlitchTime");

    [Header("DEBUG")]
    public bool enablePauseSystem = true;

    [Header("Referencias Visuais")]
    public RectTransform phoneRectTransform;

    [Header("Telas do Celular")]
    public GameObject scannerScreenContent;
    public GameObject pauseMenuContent;
    public GameObject gameOverContent;
    public GameObject levelUpContent;

    public RawImage scannerImage;
    public GameObject scannerCameraObject;

    [Header("Integracao com Mira")]
    public GameObject customCrosshairObject;

    [Header("Posicoes")]
    public Vector2 hiddenPosition = new Vector2(400f, 50f);
    public Vector2 visiblePosition = new Vector2(-200f, 50f);

    [Header("Animacao")]
    public float animationDuration = 0.5f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Level Up")]
    public float levelUpPreFreezeGlitchDuration = 0.18f;
    [Range(0f, 1f)] public float levelUpPreFreezeNoiseAmount = 0.55f;
    public float levelUpGlitchDuration = 0.2f;
    [Range(0f, 1f)] public float levelUpGlitchNoiseAmount = 0.8f;

    [Header("Level Up Overlay")]
    public RawImage levelUpFullscreenOverlay;
    public bool autoFindLevelUpFullscreenOverlay = true;
    [Range(0f, 1f)] public float levelUpFullscreenOverlayMaxAlpha = 0.38f;
    public float levelUpFullscreenOverlayFlickerSpeed = 10f;

    [Header("Level Up Shake")]
    public bool enableLevelUpPreFreezeShake = true;
    [Range(0f, 1f)] public float levelUpPreFreezeShakeForce = 0.12f;
    public float levelUpPreFreezeShakeDuration = 0.1f;

    private bool isPhoneVisible;
    private bool isGamePaused;
    public bool isGameOver { get; private set; }
    public bool IsLevelUpMenuOpen { get; private set; }

    private bool isTransitioning;
    private Coroutine currentAnimationRoutine;
    private Coroutine levelUpRoutine;
    private float originalNoiseAmount = 0.05f;
    private PlayerMove playerMove;
    private PlayerParry playerParry;
    private Material levelUpFullscreenOverlayMaterial;
    private CinemachineImpulseSource levelUpImpulseSource;

    void Start()
    {
        TryFindLevelUpContent();
        TryFindLevelUpFullscreenOverlay();
        PrepareLevelUpFullscreenOverlay();
        EnsureLevelUpImpulseSource();
        ResetLevelUpCameraGlitch();

        if (phoneRectTransform != null)
            phoneRectTransform.anchoredPosition = hiddenPosition;

        if (scannerCameraObject != null) scannerCameraObject.SetActive(false);
        if (scannerScreenContent != null) scannerScreenContent.SetActive(false);
        if (pauseMenuContent != null) pauseMenuContent.SetActive(false);
        if (gameOverContent != null) gameOverContent.SetActive(false);
        if (levelUpContent != null) levelUpContent.SetActive(false);

        if (scannerImage != null && scannerImage.material != null)
            originalNoiseAmount = scannerImage.material.GetFloat("_NoiseAmount");

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        if (customCrosshairObject != null)
            customCrosshairObject.SetActive(true);

        HideLevelUpFullscreenOverlay();
    }

    void Update()
    {
        if (isTransitioning) return;
        if (isGameOver) return;
        if (IsLevelUpMenuOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGamePaused)
            {
                TogglePauseMode(false);
            }
            else
            {
                if (isPhoneVisible && scannerScreenContent != null && scannerScreenContent.activeSelf)
                    StartCoroutine(GlitchTransitionToPause());
                else if (isPhoneVisible && pauseMenuContent != null && pauseMenuContent.activeSelf)
                    TogglePauseMode(false);
                else
                    TogglePauseMode(true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            if (isGamePaused) return;
            ToggleScannerMode();
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        IsLevelUpMenuOpen = false;
        SetPlayerControlsLocked(true);

        if (enablePauseSystem)
            Time.timeScale = 0f;

        if (scannerScreenContent != null) scannerScreenContent.SetActive(false);
        if (pauseMenuContent != null) pauseMenuContent.SetActive(false);
        if (scannerCameraObject != null) scannerCameraObject.SetActive(false);
        if (gameOverContent != null) gameOverContent.SetActive(true);
        if (levelUpContent != null) levelUpContent.SetActive(false);

        if (customCrosshairObject != null)
            customCrosshairObject.SetActive(false);

        AnimatePhoneToState(true);
    }

    public void ResetAfterRestart()
    {
        if (levelUpRoutine != null)
        {
            StopCoroutine(levelUpRoutine);
            levelUpRoutine = null;
        }

        if (currentAnimationRoutine != null)
        {
            StopCoroutine(currentAnimationRoutine);
            currentAnimationRoutine = null;
        }

        isPhoneVisible = false;
        isGamePaused = false;
        isGameOver = false;
        IsLevelUpMenuOpen = false;
        isTransitioning = false;

        if (phoneRectTransform != null)
            phoneRectTransform.anchoredPosition = hiddenPosition;

        if (scannerCameraObject != null) scannerCameraObject.SetActive(false);
        if (scannerScreenContent != null) scannerScreenContent.SetActive(false);
        if (pauseMenuContent != null) pauseMenuContent.SetActive(false);
        if (gameOverContent != null) gameOverContent.SetActive(false);
        if (levelUpContent != null) levelUpContent.SetActive(false);
        HideLevelUpFullscreenOverlay();

        if (scannerImage != null && scannerImage.material != null)
            scannerImage.material.SetFloat("_NoiseAmount", originalNoiseAmount);

        ResetLevelUpCameraGlitch();

        if (customCrosshairObject != null)
            customCrosshairObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        SetPlayerControlsLocked(false);
        Time.timeScale = 1f;
    }

    public void TriggerLevelUpSequence()
    {
        if (isGameOver) return;

        TryFindLevelUpContent();
        TryFindLevelUpFullscreenOverlay();
        PrepareLevelUpFullscreenOverlay();
        EnsureLevelUpImpulseSource();
        ResetLevelUpCameraGlitch();

        if (levelUpRoutine != null)
        {
            StopCoroutine(levelUpRoutine);
            levelUpRoutine = null;
        }

        if (currentAnimationRoutine != null)
        {
            StopCoroutine(currentAnimationRoutine);
            currentAnimationRoutine = null;
        }

        levelUpRoutine = StartCoroutine(LevelUpSequenceRoutine());
    }

    public void CompleteLevelUpSelection()
    {
        if (!IsLevelUpMenuOpen) return;

        bool hasQueuedAttributeChoices = PlayerMove.instance != null && PlayerMove.instance.HasPendingAttributeLevelUpChoices;

        IsLevelUpMenuOpen = false;
        HideLevelUpFullscreenOverlay();

        if (levelUpContent != null)
            levelUpContent.SetActive(false);

        TogglePauseMode(false);

        if (hasQueuedAttributeChoices)
            StartCoroutine(TriggerQueuedLevelUpAfterClose());
    }

    IEnumerator TriggerQueuedLevelUpAfterClose()
    {
        yield return new WaitForSecondsRealtime(animationDuration + 0.05f);

        if (isGameOver)
            yield break;

        if (PlayerMove.instance != null && PlayerMove.instance.HasPendingAttributeLevelUpChoices)
            TriggerLevelUpSequence();
    }

    IEnumerator GlitchTransitionToPause()
    {
        isTransitioning = true;

        SetScannerNoise(levelUpGlitchNoiseAmount);

        yield return new WaitForSecondsRealtime(0.15f);

        if (scannerScreenContent != null) scannerScreenContent.SetActive(false);
        if (scannerCameraObject != null) scannerCameraObject.SetActive(false);

        SetScannerNoise(originalNoiseAmount);

        TogglePauseMode(true);
        isTransitioning = false;
    }

    IEnumerator LevelUpSequenceRoutine()
    {
        isTransitioning = true;

        yield return PlayLevelUpFullscreenEffect();

        isGamePaused = true;
        IsLevelUpMenuOpen = true;
        SetPlayerControlsLocked(true);

        if (enablePauseSystem)
            Time.timeScale = 0f;

        if (pauseMenuContent != null) pauseMenuContent.SetActive(false);
        if (gameOverContent != null) gameOverContent.SetActive(false);
        if (scannerScreenContent != null) scannerScreenContent.SetActive(false);
        if (levelUpContent != null) levelUpContent.SetActive(true);
        if (scannerCameraObject != null) scannerCameraObject.SetActive(false);
        if (customCrosshairObject != null) customCrosshairObject.SetActive(false);

        if (levelUpContent == null)
            Debug.LogWarning("PhoneSystemController: levelUpContent nao foi configurado.");

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        if (!isPhoneVisible)
        {
            isPhoneVisible = true;

            if (phoneRectTransform != null)
            {
                currentAnimationRoutine = StartCoroutine(AnimatePhone(visiblePosition, true));

                while (currentAnimationRoutine != null)
                    yield return null;
            }
            else
            {
                Debug.LogError("PhoneSystemController: phoneRectTransform nao foi configurado para o level up.");
            }
        }

        isTransitioning = false;
        levelUpRoutine = null;
    }

    public void ToggleScannerMode()
    {
        bool shouldOpen = !isPhoneVisible;
        IsLevelUpMenuOpen = false;

        if (shouldOpen)
        {
            if (scannerScreenContent != null) scannerScreenContent.SetActive(true);
            if (pauseMenuContent != null) pauseMenuContent.SetActive(false);
            if (gameOverContent != null) gameOverContent.SetActive(false);
            if (levelUpContent != null) levelUpContent.SetActive(false);
            if (scannerCameraObject != null) scannerCameraObject.SetActive(true);
            if (customCrosshairObject != null) customCrosshairObject.SetActive(false);
        }

        AnimatePhoneToState(shouldOpen);
    }

    public void TogglePauseMode(bool active)
    {
        isGamePaused = active;

        if (isGamePaused)
        {
            IsLevelUpMenuOpen = false;
            SetPlayerControlsLocked(true);

            if (enablePauseSystem)
                Time.timeScale = 0f;

            if (scannerScreenContent != null) scannerScreenContent.SetActive(false);
            if (pauseMenuContent != null) pauseMenuContent.SetActive(true);
            if (gameOverContent != null) gameOverContent.SetActive(false);
            if (levelUpContent != null) levelUpContent.SetActive(false);
            if (scannerCameraObject != null) scannerCameraObject.SetActive(false);
            if (customCrosshairObject != null) customCrosshairObject.SetActive(false);

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;

            if (!isPhoneVisible)
                AnimatePhoneToState(true);
        }
        else
        {
            IsLevelUpMenuOpen = false;
            HideLevelUpFullscreenOverlay();

            if (enablePauseSystem)
                Time.timeScale = 1f;

            AnimatePhoneToState(false);
        }
    }

    private void AnimatePhoneToState(bool show)
    {
        if (phoneRectTransform == null)
        {
            Debug.LogError("PhoneSystemController: phoneRectTransform nao foi configurado.");
            return;
        }

        isPhoneVisible = show;

        if (currentAnimationRoutine != null)
            StopCoroutine(currentAnimationRoutine);

        Vector2 targetPosition = show ? visiblePosition : hiddenPosition;
        currentAnimationRoutine = StartCoroutine(AnimatePhone(targetPosition, show));
    }

    IEnumerator AnimatePhone(Vector2 target, bool show)
    {
        isTransitioning = true;

        Vector2 startPos = phoneRectTransform.anchoredPosition;
        float timeElapsed = 0f;

        while (timeElapsed < animationDuration)
        {
            float delta = Time.timeScale == 0f ? Time.unscaledDeltaTime : Time.deltaTime;
            timeElapsed += delta;

            float percentage = Mathf.Clamp01(timeElapsed / animationDuration);
            float curveValue = animationCurve.Evaluate(percentage);

            phoneRectTransform.anchoredPosition = Vector2.Lerp(startPos, target, curveValue);
            yield return null;
        }

        phoneRectTransform.anchoredPosition = target;

        if (!show)
        {
            if (scannerCameraObject != null) scannerCameraObject.SetActive(false);
            if (scannerScreenContent != null) scannerScreenContent.SetActive(false);
            if (pauseMenuContent != null) pauseMenuContent.SetActive(false);
            if (gameOverContent != null) gameOverContent.SetActive(false);
            if (levelUpContent != null) levelUpContent.SetActive(false);

            if (customCrosshairObject != null && !isGameOver)
                customCrosshairObject.SetActive(true);

            isGamePaused = false;

            if (!isGameOver)
                Time.timeScale = 1f;

            if (!isGameOver)
                SetPlayerControlsLocked(false);

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }

        currentAnimationRoutine = null;
        isTransitioning = false;
    }

    void SetScannerNoise(float amount)
    {
        if (scannerImage != null && scannerImage.material != null)
            scannerImage.material.SetFloat("_NoiseAmount", amount);
    }

    void SetPlayerControlsLocked(bool locked)
    {
        if (playerMove == null)
            playerMove = FindAnyObjectByType<PlayerMove>();

        if (playerParry == null)
            playerParry = FindAnyObjectByType<PlayerParry>();

        if (playerMove != null)
            playerMove.SetExternalControlLock(locked);

        if (playerParry != null)
            playerParry.SetExternalControlLock(locked);
    }

    void TryFindLevelUpContent()
    {
        if (levelUpContent != null)
            return;

        LevelUpMenuController levelUpMenu = FindAnyObjectByType<LevelUpMenuController>(FindObjectsInactive.Include);
        if (levelUpMenu != null)
        {
            levelUpContent = levelUpMenu.gameObject;
            return;
        }

        if (phoneRectTransform == null)
            return;

        foreach (Transform child in phoneRectTransform.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "LevelUpContent")
            {
                levelUpContent = child.gameObject;
                return;
            }
        }
    }

    void TryFindLevelUpFullscreenOverlay()
    {
        if (levelUpFullscreenOverlay != null || !autoFindLevelUpFullscreenOverlay)
            return;

        RawImage[] rawImages = FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (RawImage rawImage in rawImages)
        {
            if (rawImage != null && rawImage.name == "LevelUpFullscreenOverlay")
            {
                levelUpFullscreenOverlay = rawImage;
                return;
            }
        }
    }

    void PrepareLevelUpFullscreenOverlay()
    {
        if (levelUpFullscreenOverlay == null)
            return;

        RectTransform overlayRect = levelUpFullscreenOverlay.rectTransform;
        if (overlayRect != null)
        {
            Transform parentTransform = overlayRect.parent;
            if (phoneRectTransform != null && parentTransform == phoneRectTransform && phoneRectTransform.parent != null)
            {
                overlayRect.SetParent(phoneRectTransform.parent, false);
                parentTransform = overlayRect.parent;
            }

            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = Vector2.zero;
            overlayRect.localScale = Vector3.one;

            if (parentTransform != null)
                overlayRect.SetAsLastSibling();
        }

        levelUpFullscreenOverlay.raycastTarget = false;

        UnscaledTimeFeeder timeFeeder = levelUpFullscreenOverlay.GetComponent<UnscaledTimeFeeder>();
        if (timeFeeder == null)
            timeFeeder = levelUpFullscreenOverlay.gameObject.AddComponent<UnscaledTimeFeeder>();

        timeFeeder.RebindTargetMaterial();
        levelUpFullscreenOverlayMaterial = timeFeeder.TargetMaterial;

        if (levelUpFullscreenOverlayMaterial == null)
        {
            Debug.LogWarning("PhoneSystemController: o overlay de level up precisa de um material com shader de TV static.");
            return;
        }

        SetLevelUpOverlayMaterialFloat("_FlickerSpeed", levelUpFullscreenOverlayFlickerSpeed);
        SetLevelUpOverlayMaterialFloat("_Intensity", 0f);
        SetLevelUpOverlayMaterialFloat("_OverlayOpacity", 0f);
        levelUpFullscreenOverlay.color = Color.white;
        levelUpFullscreenOverlay.gameObject.SetActive(false);
    }

    IEnumerator PlayLevelUpFullscreenEffect()
    {
        TriggerLevelUpPreFreezeShake();
        HideLevelUpFullscreenOverlay();

        float duration = Mathf.Max(0.01f, levelUpPreFreezeGlitchDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float burst = 1f - progress;
            float flicker = 0.92f + (Mathf.Sin((elapsed * 29f) + (Time.unscaledTime * 12f)) * 0.08f);
            float intensity = Mathf.Clamp01(levelUpPreFreezeNoiseAmount * (0.62f + (burst * 0.38f)) * flicker);

            SetLevelUpCameraGlitch(intensity, elapsed);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ResetLevelUpCameraGlitch();
    }

    void ApplyLevelUpFullscreenOverlay(float intensity, float alpha)
    {
        if (levelUpFullscreenOverlay == null)
            return;

        levelUpFullscreenOverlay.color = Color.white;
        SetLevelUpOverlayMaterialFloat("_Intensity", intensity);
        SetLevelUpOverlayMaterialFloat("_OverlayOpacity", alpha);
        SetLevelUpOverlayMaterialFloat("_FlickerSpeed", levelUpFullscreenOverlayFlickerSpeed);
    }

    void HideLevelUpFullscreenOverlay()
    {
        ResetLevelUpCameraGlitch();

        if (levelUpFullscreenOverlay == null)
            return;

        ApplyLevelUpFullscreenOverlay(0f, 0f);
        levelUpFullscreenOverlay.gameObject.SetActive(false);
    }

    void SetLevelUpFullscreenOverlayVisible(bool visible)
    {
        if (levelUpFullscreenOverlay == null)
            return;

        levelUpFullscreenOverlay.gameObject.SetActive(visible);

        if (visible && !levelUpFullscreenOverlay.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("PhoneSystemController: o overlay de level up precisa ficar em um Canvas ativo fora da tela do tablet.");
        }
    }

    void SetLevelUpOverlayMaterialFloat(string propertyName, float value)
    {
        if (levelUpFullscreenOverlayMaterial != null && levelUpFullscreenOverlayMaterial.HasProperty(propertyName))
            levelUpFullscreenOverlayMaterial.SetFloat(propertyName, value);
    }

    void SetLevelUpCameraGlitch(float intensity, float elapsedTime)
    {
        Shader.SetGlobalFloat(LevelUpCameraGlitchIntensityProperty, Mathf.Clamp01(intensity));
        Shader.SetGlobalFloat(LevelUpCameraGlitchTimeProperty, Mathf.Max(0f, elapsedTime));
    }

    void ResetLevelUpCameraGlitch()
    {
        Shader.SetGlobalFloat(LevelUpCameraGlitchIntensityProperty, 0f);
        Shader.SetGlobalFloat(LevelUpCameraGlitchTimeProperty, 0f);
    }

    void EnsureLevelUpImpulseSource()
    {
        if (!enableLevelUpPreFreezeShake)
            return;

        if (levelUpImpulseSource == null)
            levelUpImpulseSource = GetComponent<CinemachineImpulseSource>();

        if (levelUpImpulseSource == null)
            levelUpImpulseSource = gameObject.AddComponent<CinemachineImpulseSource>();

        levelUpImpulseSource.ImpulseDefinition.ImpulseChannel = 1;
        levelUpImpulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        levelUpImpulseSource.ImpulseDefinition.ImpulseDuration = Mathf.Max(0.05f, levelUpPreFreezeShakeDuration);
        levelUpImpulseSource.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        levelUpImpulseSource.ImpulseDefinition.DissipationDistance = 100f;
        levelUpImpulseSource.ImpulseDefinition.DissipationRate = 0.1f;
        levelUpImpulseSource.DefaultVelocity = new Vector3(-0.15f, -1f, 0f).normalized;
    }

    void TriggerLevelUpPreFreezeShake()
    {
        if (!enableLevelUpPreFreezeShake)
            return;

        EnsureLevelUpImpulseSource();

        if (levelUpImpulseSource != null)
            levelUpImpulseSource.GenerateImpulseWithForce(levelUpPreFreezeShakeForce);
    }
}
