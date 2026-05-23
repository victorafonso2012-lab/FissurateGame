using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class ShiftPromptTrigger2D : MonoBehaviour
{
    [Header("Icone")]
    [Tooltip("Arraste aqui o prefab do icone de Shift.")]
    public GameObject prefabIconeShift;

    [Tooltip("Altura do icone acima da personagem.")]
    public float alturaIcone = 1.5f;

    [Header("Comportamento")]
    [Tooltip("Se ligado, o trigger some depois que o jogador apertar Shift.")]
    public bool destruirDepoisDeApertarShift = false;

    private GameObject iconeAtual;
    private Transform playerAtual;
    private FloatingIcon floatingIcon;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (playerAtual == null)
            return;

        if (Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.wasPressedThisFrame ||
             Keyboard.current.rightShiftKey.wasPressedThisFrame))
        {
            if (floatingIcon != null)
                floatingIcon.Punch();

            if (destruirDepoisDeApertarShift)
            {
                RemoverIcone();
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerAtual = other.transform;
        MostrarIcone();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        RemoverIcone();
        playerAtual = null;
    }

    private void MostrarIcone()
    {
        if (iconeAtual != null)
            Destroy(iconeAtual);

        if (prefabIconeShift == null || playerAtual == null)
            return;

        iconeAtual = Instantiate(prefabIconeShift, playerAtual);
        iconeAtual.transform.localPosition = new Vector3(0f, alturaIcone, 0f);

        floatingIcon = iconeAtual.GetComponent<FloatingIcon>();

        if (floatingIcon != null)
            floatingIcon.SetFloatingIcon(true);
    }

    private void RemoverIcone()
    {
        if (iconeAtual != null)
            Destroy(iconeAtual);

        iconeAtual = null;
        floatingIcon = null;
    }

    private void OnDisable()
    {
        RemoverIcone();
    }

    private void OnDestroy()
    {
        RemoverIcone();
    }
}