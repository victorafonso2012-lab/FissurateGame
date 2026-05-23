using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class CameraBoundaryManagerUltimate : MonoBehaviour
{
    public enum CameraMode { Single, Multi, Smart }

    [Header("Modo de Operação")]
    public CameraMode mode = CameraMode.Multi;

    [Header("Configuração Geral")]
    public string boundaryTag = "Limitadores";
    public float prioridadeDistancia = 10f;

    [Header("Auto-reaplicar (pega câmera que liga depois, ex: Scanner)")]
    [Tooltip("Reaplica o limite a cada X segundos enquanto o jogo roda (leve).")]
    public bool reaplicarPeriodicamente = true;

    [Tooltip("Intervalo em segundos para reaplicar limites.")]
    public float intervaloReaplicar = 0.5f;

    [Header("Debug")]
    public bool logDebug = true;

    [Header("Referências Automáticas")]
    public Transform player;

    // Interno
    private readonly List<CinemachineCamera> vCams = new();
    private readonly List<CinemachineConfiner2D> confiners = new();

    private PolygonCollider2D boundaryAtual;
    private Coroutine rotinaReaplicar;

    void Awake()
    {
        EncontrarPlayer();
        AtualizarListas();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Ao ativar (ex.: voltar de pause/persistent), tenta reaplicar limite da cena atual
        AtualizarBoundaryDaCenaAtiva();

        if (reaplicarPeriodicamente && rotinaReaplicar == null)
            rotinaReaplicar = StartCoroutine(ReaplicarLoop());
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (rotinaReaplicar != null)
        {
            StopCoroutine(rotinaReaplicar);
            rotinaReaplicar = null;
        }
    }

    void Update()
    {
        if (mode == CameraMode.Smart && player != null)
            AtualizarPrioridades();
    }

    private void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (logDebug) Debug.Log($"[CameraUltimate] Cena '{loadedScene.name}' carregada. Atualizando câmeras e limites...");

        EncontrarPlayer();
        AtualizarListas();

        boundaryAtual = EncontrarLimiteNaCena(loadedScene);

        if (boundaryAtual != null)
        {
            StartCoroutine(ApplyNewBoundaryToAll(boundaryAtual));
        }
        else if (logDebug)
        {
            Debug.LogWarning($"[CameraUltimate] Nenhum objeto com tag '{boundaryTag}' encontrado na cena '{loadedScene.name}'.");
        }
    }

    private IEnumerator ReaplicarLoop()
    {
        // Pequeno delay inicial
        yield return null;

        while (true)
        {
            if (boundaryAtual == null)
                AtualizarBoundaryDaCenaAtiva();

            if (boundaryAtual != null)
                ApplyBoundaryNow(boundaryAtual);

            yield return new WaitForSeconds(intervaloReaplicar);
        }
    }

    private void AtualizarBoundaryDaCenaAtiva()
    {
        var scene = SceneManager.GetActiveScene();
        boundaryAtual = EncontrarLimiteNaCena(scene);

        if (logDebug && boundaryAtual != null)
            Debug.Log($"[CameraUltimate] Boundary atual (cena ativa): '{boundaryAtual.name}'.");
    }

    private void AtualizarPrioridades()
    {
        AtualizarListas();

        float menorDist = float.MaxValue;
        CinemachineCamera maisProxima = null;

        foreach (var cam in vCams)
        {
            if (cam == null) continue;
            float dist = Vector2.Distance(player.position, cam.transform.position);

            if (dist < menorDist)
            {
                menorDist = dist;
                maisProxima = cam;
            }
        }

        if (maisProxima != null)
        {
            foreach (var cam in vCams)
                if (cam != null) cam.Priority = (cam == maisProxima) ? 20 : 10;
        }
    }

    private PolygonCollider2D EncontrarLimiteNaCena(Scene cena)
    {
        // Procura em TODOS os objetos da cena (root + filhos), inclusive inativos
        var roots = cena.GetRootGameObjects();
        foreach (var root in roots)
        {
            var colliders = root.GetComponentsInChildren<PolygonCollider2D>(true);
            foreach (var col in colliders)
            {
                if (col != null && col.CompareTag(boundaryTag))
                    return col;
            }
        }

        // Fallback
        var go = GameObject.FindGameObjectWithTag(boundaryTag);
        if (go != null)
            return go.GetComponentInChildren<PolygonCollider2D>(true);

        return null;
    }

    public void AtualizarListas()
    {
        vCams.Clear();
        confiners.Clear();

        // Inclui ativas e INATIVAS (scanner desligado)
        var todasCams = FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        ); // [web:333][web:332]

        foreach (var cam in todasCams)
        {
            if (cam == null) continue;

            vCams.Add(cam);

            // Pega Confiner no mesmo GO da CinemachineCamera
            var conf = cam.GetComponent<CinemachineConfiner2D>();
            if (conf != null)
                confiners.Add(conf);
        }

        if (logDebug) Debug.Log($"[CameraUltimate] Registradas: {vCams.Count} VCams, {confiners.Count} Confiners.");
    }

    private IEnumerator ApplyNewBoundaryToAll(PolygonCollider2D newBoundary)
    {
        yield return null;
        ApplyBoundaryNow(newBoundary);
    }

    private void ApplyBoundaryNow(PolygonCollider2D newBoundary)
    {
        AtualizarListas();

        if (confiners.Count == 0)
        {
            if (logDebug) Debug.LogWarning("[CameraUltimate] Nenhum CinemachineConfiner2D encontrado nas VCams (incluindo scanner).");
            return;
        }

        if (mode == CameraMode.Single)
        {
            var conf = confiners[0];
            if (conf != null) AplicarLimite(conf, newBoundary);
        }
        else
        {
            foreach (var conf in confiners)
                if (conf != null) AplicarLimite(conf, newBoundary);
        }
    }

    private void AplicarLimite(CinemachineConfiner2D conf, PolygonCollider2D boundary)
    {
        if (conf.BoundingShape2D == boundary) return;

        conf.BoundingShape2D = boundary;
        conf.InvalidateBoundingShapeCache();
        conf.InvalidateLensCache(); // quando muda zoom/lente [web:93][web:94]

        if (logDebug)
            Debug.Log($"[CameraUltimate] -> {conf.gameObject.name} recebeu boundary '{boundary.name}'.");
    }

    private void EncontrarPlayer()
    {
        if (player != null) return;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null && logDebug)
            Debug.LogWarning("[CameraUltimate] Player não encontrado! Tag 'Player' está correta?");
    }
}
