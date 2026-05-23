using UnityEngine;
using System;
using System.Collections;
using Unity.Cinemachine;

public class PauseTrigger2D : MonoBehaviour
{
    [Header("Configurações de Visual")]
    [Tooltip("Arraste o PREFAB da Tecla 'E' aqui.")]
    public GameObject prefabIconeInteracao;
    public float alturaIcone = 1.5f;

    [Header("Configurações de Cena")]
    [Tooltip("Objeto vazio onde a personagem deve parar.")]
    public Transform pontoDeParada;
    public float velocidadeAutoWalk = 3f;
    public string nomeAnimacaoAndar = "SashaRun";
    public string nomeAnimacaoIdle = "SashaIddleF";

    [Header("Configurações de Diálogo")]
    [TextArea(3, 10)]
    public string[] falasDesteTrigger;

    [Header("Zoom (Cinemachine) no Tutorial")]
    [Tooltip("Se vazio, o script tentará encontrar pelo nome abaixo.")]
    public CinemachineCamera vcamParaZoom;

    [Tooltip("Nome exato do GameObject da VCam (ex: VCam_FollowPlayer).")]
    public string nomeVCamParaZoom = "VCam_FollowPlayer";

    [Tooltip("Tamanho ortográfico durante o tutorial (menor = mais zoom in). Ex: 5.")]
    public float orthoSizeTutorial = 5f;

    [Tooltip("Tempo (segundos) para interpolar o zoom.")]
    public float duracaoZoom = 0.25f;

    [Header("Reframe (mais central) no Tutorial")]
    [Tooltip("Deixa a câmera mais rápida pra centralizar durante o tutorial.")]
    public bool reforcarCentralizacao = true;

    [Tooltip("Quanto menor, mais rápido centraliza. Ex: 0.05 ~ 0.2")]
    public float dampingTutorial = 0.08f;

    // Variáveis de controle
    private int indiceDaFalaAtual = 0;
    private bool isPlayerInside = false;
    public bool isPaused = false;
    private GameObject iconeAtual;

    // Referências
    PlayerMove playerMove;
    Animator playerAnimator;
    Rigidbody2D playerRb;

    // Eventos
    public static event Action OnPlayerEntrouNoTrigger;
    public static event Action OnPlayerSaiuDoTrigger;
    public static event Action<string> OnAtualizarTexto;

    // Zoom internals
    private float orthoOriginal;
    private bool orthoOriginalCapturado = false;
    private Coroutine rotinaZoom;
    private CinemachineConfiner2D confiner;

    // “Reframe” internals (genérico)
    private bool framingSalvo = false;
    private float savedXDamping, savedYDamping;
    private float savedDeadW, savedDeadH, savedSoftW, savedSoftH;

    // Referência ao componente de framing (pode variar por versão)
    // Se o seu Body for Framing Transposer, ele estará nesse componente.
    private Component framingComponent;

    private void Start()
    {
        AutoAssignVCamSePreciso();
        CachearComponentesDaVCam();

        playerMove = FindFirstObjectByType<PlayerMove>();
        if (playerMove != null)
        {
            playerAnimator = playerMove.GetComponent<Animator>();
            playerRb = playerMove.GetComponent<Rigidbody2D>();

            if (playerAnimator == null)
                playerAnimator = playerMove.GetComponentInChildren<Animator>();
        }
    }

    private void OnValidate()
    {
        AutoAssignVCamSePreciso();
        CachearComponentesDaVCam();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isPaused)
        {
            isPlayerInside = true;
            StartCoroutine(CutsceneEntrada(other.transform));
        }
    }

    IEnumerator CutsceneEntrada(Transform playerTransform)
    {
        isPaused = true;

        // Garante refs
        AutoAssignVCamSePreciso();
        CachearComponentesDaVCam();

        // Captura o zoom original (uma vez) para restaurar no final
        CapturarOrthoOriginalSePreciso();

        // === COMEÇA O ZOOM + CENTRALIZAÇÃO JÁ NO INÍCIO ===
        if (reforcarCentralizacao)
            AplicarReframeTutorial();

        StartZoom(orthoSizeTutorial);

        // --- FASE 1: DESLIGA O PLAYER PARA CAMINHAR SOZINHO ---
        if (playerMove != null)
        {
            playerMove.enabled = false;
            if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
        }

        // Caminhar automaticamente (Cutscene)
        if (pontoDeParada != null)
        {
            if (playerAnimator != null && !string.IsNullOrEmpty(nomeAnimacaoAndar))
                playerAnimator.Play(nomeAnimacaoAndar);

            while (Vector3.Distance(playerTransform.position, pontoDeParada.position) > 0.05f)
            {
                Vector3 direcao = (pontoDeParada.position - playerTransform.position).normalized;

                if (playerAnimator != null)
                {
                    playerAnimator.SetFloat("Horizontal", direcao.x);
                    playerAnimator.SetFloat("Vertical", direcao.y);
                    playerAnimator.SetFloat("Speed", 1f);
                }

                playerTransform.position = Vector3.MoveTowards(
                    playerTransform.position,
                    pontoDeParada.position,
                    velocidadeAutoWalk * Time.deltaTime
                );

                yield return null;
            }
            playerTransform.position = pontoDeParada.position;
        }

        // --- FASE 2: CHEGOU NO PONTO ---

        // 1. Zera animações de movimento
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Horizontal", 0f);
            playerAnimator.SetFloat("Vertical", 0f);
            playerAnimator.SetFloat("Speed", 0f);

            if (!string.IsNullOrEmpty(nomeAnimacaoIdle))
                playerAnimator.Play(nomeAnimacaoIdle);
        }

        // 2. Liga o cérebro de volta (poder atacar/trocar arma)
        if (playerMove != null) playerMove.enabled = true;

        // 3. Trava o corpo (para não sair andando)
        if (playerRb != null)
            playerRb.constraints = RigidbodyConstraints2D.FreezeAll;

        IniciarDialogo();
    }

    void IniciarDialogo()
    {
        indiceDaFalaAtual = 0;
        MostrarIconeInteracao();
        OnPlayerEntrouNoTrigger?.Invoke();
        MandarFraseAtual();
    }

    void MostrarIconeInteracao()
    {
        if (iconeAtual != null) Destroy(iconeAtual);
        if (prefabIconeInteracao != null && playerMove != null)
        {
            iconeAtual = Instantiate(prefabIconeInteracao, playerMove.transform);
            iconeAtual.transform.localPosition = new Vector3(0, alturaIcone, 0);
        }
    }

    void MandarFraseAtual()
    {
        if (falasDesteTrigger != null && falasDesteTrigger.Length > indiceDaFalaAtual)
            OnAtualizarTexto?.Invoke(falasDesteTrigger[indiceDaFalaAtual]);
    }

    void Update()
    {
        if (isPaused && isPlayerInside)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (indiceDaFalaAtual < falasDesteTrigger.Length)
                    AvancarDialogo();
            }
        }
    }

    void AvancarDialogo()
    {
        if (indiceDaFalaAtual < falasDesteTrigger.Length - 1)
        {
            indiceDaFalaAtual++;
            MandarFraseAtual();
        }
        else
        {
            FinalizarDialogo();
        }
    }

    void FinalizarDialogo()
    {
        if (playerMove != null) playerMove.enabled = true;

        if (playerRb != null)
            playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Volta o zoom ao normal
        RestaurarZoomOriginal();

        // Restaura comportamento normal de framing
        RestaurarReframe();

        isPaused = false;
        if (iconeAtual != null) Destroy(iconeAtual);
        OnPlayerSaiuDoTrigger?.Invoke();
        if (pontoDeParada != null) Destroy(pontoDeParada.gameObject);
        Destroy(gameObject);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerInside = false;
    }

    // =========================
    // AUTO ASSIGN VCam
    // =========================

    void AutoAssignVCamSePreciso()
    {
        if (vcamParaZoom != null) return;
        if (string.IsNullOrEmpty(nomeVCamParaZoom)) return;

        GameObject go = GameObject.Find(nomeVCamParaZoom);
        if (go != null && go.TryGetComponent(out CinemachineCamera cam))
        {
            vcamParaZoom = cam;
            return;
        }

        var cams = FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var c in cams)
        {
            if (c != null && c.name == nomeVCamParaZoom)
            {
                vcamParaZoom = c;
                return;
            }
        }
    }

    void CachearComponentesDaVCam()
    {
        confiner = null;
        framingComponent = null;

        if (vcamParaZoom == null) return;

        confiner = vcamParaZoom.GetComponent<CinemachineConfiner2D>();

        // Tenta achar um Framing Transposer de forma “genérica”
        // (se não encontrar, tudo bem: a parte de reframe não será aplicada)
        // Em várias versões ele é um componente separado no GameObject da VCam.
        var all = vcamParaZoom.GetComponents<Component>();
        foreach (var c in all)
        {
            if (c == null) continue;
            string typeName = c.GetType().Name;
            if (typeName.Contains("FramingTransposer"))
            {
                framingComponent = c;
                break;
            }
        }
    }

    // =========================
    // ZOOM HELPERS
    // =========================

    void CapturarOrthoOriginalSePreciso()
    {
        if (orthoOriginalCapturado) return;
        if (vcamParaZoom == null) return;

        var lens = vcamParaZoom.Lens;
        orthoOriginal = lens.OrthographicSize;
        orthoOriginalCapturado = true;
    }

    void RestaurarZoomOriginal()
    {
        if (!orthoOriginalCapturado) return;
        StartZoom(orthoOriginal);
    }

    void StartZoom(float orthoTarget)
    {
        if (vcamParaZoom == null) return;

        if (rotinaZoom != null) StopCoroutine(rotinaZoom);
        rotinaZoom = StartCoroutine(ZoomCoroutine(orthoTarget));
    }

    IEnumerator ZoomCoroutine(float orthoTarget)
    {
        var lens = vcamParaZoom.Lens;
        float start = lens.OrthographicSize;

        float t = 0f;
        while (t < duracaoZoom)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracaoZoom);
            float v = Mathf.Lerp(start, orthoTarget, p);

            lens = vcamParaZoom.Lens;
            lens.OrthographicSize = v;
            vcamParaZoom.Lens = lens;

            yield return null;
        }

        lens = vcamParaZoom.Lens;
        lens.OrthographicSize = orthoTarget;
        vcamParaZoom.Lens = lens;

        if (confiner != null)
            confiner.InvalidateLensCache();

        rotinaZoom = null;
    }

    // =========================
    // REFRAME HELPERS (best-effort)
    // =========================

    void AplicarReframeTutorial()
    {
        if (framingComponent == null) return;

        // Usa reflection para não quebrar por diferenças de versão de tipo
        // Campos típicos do FramingTransposer:
        // XDamping, YDamping, DeadZoneWidth/Height, SoftZoneWidth/Height
        var t = framingComponent.GetType();

        try
        {
            float GetFloat(string field)
            {
                var f = t.GetField(field);
                if (f != null && f.FieldType == typeof(float))
                    return (float)f.GetValue(framingComponent);

                var p = t.GetProperty(field);
                if (p != null && p.PropertyType == typeof(float))
                    return (float)p.GetValue(framingComponent);

                return float.NaN;
            }

            void SetFloat(string field, float value)
            {
                var f = t.GetField(field);
                if (f != null && f.FieldType == typeof(float))
                {
                    f.SetValue(framingComponent, value);
                    return;
                }

                var p = t.GetProperty(field);
                if (p != null && p.PropertyType == typeof(float) && p.CanWrite)
                {
                    p.SetValue(framingComponent, value);
                    return;
                }
            }

            if (!framingSalvo)
            {
                // Tentativas de nomes comuns (varia entre versões)
                savedXDamping = GetFloat("m_XDamping");
                if (float.IsNaN(savedXDamping)) savedXDamping = GetFloat("XDamping");

                savedYDamping = GetFloat("m_YDamping");
                if (float.IsNaN(savedYDamping)) savedYDamping = GetFloat("YDamping");

                savedDeadW = GetFloat("m_DeadZoneWidth");
                if (float.IsNaN(savedDeadW)) savedDeadW = GetFloat("DeadZoneWidth");

                savedDeadH = GetFloat("m_DeadZoneHeight");
                if (float.IsNaN(savedDeadH)) savedDeadH = GetFloat("DeadZoneHeight");

                savedSoftW = GetFloat("m_SoftZoneWidth");
                if (float.IsNaN(savedSoftW)) savedSoftW = GetFloat("SoftZoneWidth");

                savedSoftH = GetFloat("m_SoftZoneHeight");
                if (float.IsNaN(savedSoftH)) savedSoftH = GetFloat("SoftZoneHeight");

                framingSalvo = true;
            }

            // Aplica valores “rápidos”
            SetFloat("m_XDamping", dampingTutorial);
            SetFloat("XDamping", dampingTutorial);

            SetFloat("m_YDamping", dampingTutorial);
            SetFloat("YDamping", dampingTutorial);

            // Zera zonas para forçar recentralização
            SetFloat("m_DeadZoneWidth", 0f);
            SetFloat("DeadZoneWidth", 0f);

            SetFloat("m_DeadZoneHeight", 0f);
            SetFloat("DeadZoneHeight", 0f);

            SetFloat("m_SoftZoneWidth", 0f);
            SetFloat("SoftZoneWidth", 0f);

            SetFloat("m_SoftZoneHeight", 0f);
            SetFloat("SoftZoneHeight", 0f);
        }
        catch
        {
            // Se falhar, só ignora (zoom ainda funciona)
        }
    }

    void RestaurarReframe()
    {
        if (!framingSalvo) return;
        if (framingComponent == null) return;

        var t = framingComponent.GetType();

        try
        {
            void SetMaybe(string field, float value)
            {
                var f = t.GetField(field);
                if (f != null && f.FieldType == typeof(float))
                {
                    f.SetValue(framingComponent, value);
                    return;
                }

                var p = t.GetProperty(field);
                if (p != null && p.PropertyType == typeof(float) && p.CanWrite)
                {
                    p.SetValue(framingComponent, value);
                    return;
                }
            }

            SetMaybe("m_XDamping", savedXDamping);
            SetMaybe("XDamping", savedXDamping);

            SetMaybe("m_YDamping", savedYDamping);
            SetMaybe("YDamping", savedYDamping);

            SetMaybe("m_DeadZoneWidth", savedDeadW);
            SetMaybe("DeadZoneWidth", savedDeadW);

            SetMaybe("m_DeadZoneHeight", savedDeadH);
            SetMaybe("DeadZoneHeight", savedDeadH);

            SetMaybe("m_SoftZoneWidth", savedSoftW);
            SetMaybe("SoftZoneWidth", savedSoftW);

            SetMaybe("m_SoftZoneHeight", savedSoftH);
            SetMaybe("SoftZoneHeight", savedSoftH);
        }
        catch
        {
            // ignora
        }

        framingSalvo = false;
    }

    private void OnDisable()
    {
        // Segurança: se esse trigger for destruído no meio, tenta restaurar
        if (orthoOriginalCapturado && vcamParaZoom != null)
        {
            var lens = vcamParaZoom.Lens;
            lens.OrthographicSize = orthoOriginal;
            vcamParaZoom.Lens = lens;

            if (confiner != null)
                confiner.InvalidateLensCache();
        }

        RestaurarReframe();
    }
}
