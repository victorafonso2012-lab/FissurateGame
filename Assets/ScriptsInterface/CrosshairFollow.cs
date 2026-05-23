using UnityEngine;
using UnityEngine.InputSystem;

public class CrosshairFollow : MonoBehaviour
{
    public bool hideDefaultCursor = true;
    public Canvas targetCanvas;

    [SerializeField] private PlayerMove player;
    private Animator anim;
    private RectTransform rectTransform;
    [SerializeField] private string currentWeaponMode = "";

    void Start()
    {
        player = FindFirstObjectByType<PlayerMove>();
        anim = GetComponent<Animator>();
        rectTransform = GetComponent<RectTransform>();

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (hideDefaultCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }

        if (rectTransform == null)
            Debug.LogError("CrosshairFollow: este objeto precisa estar em UI e ter RectTransform.");

        if (targetCanvas == null)
            Debug.LogError("CrosshairFollow: nenhum Canvas encontrado.");
    }

    void Update()
    {
        if (player != null && anim != null)
        {
            string newWeaponMode = player.currentWeaponMode.ToString();
            if (newWeaponMode != currentWeaponMode)
            {
                currentWeaponMode = newWeaponMode;
                anim.Play(currentWeaponMode);
            }
        }

        if (rectTransform == null || targetCanvas == null)
            return;

        Vector2 mousePos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        RectTransform canvasRect = targetCanvas.transform as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mousePos,
            null,
            out Vector2 localPoint))
        {
            rectTransform.localPosition = localPoint;
        }
    }
}
