using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerMove), typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Configurações do Dash")]
    public float dashSpeed = 15f;           // A velocidade/força do impulso
    public float dashDuration = 0.2f;       // Tempo que o dash dura
    public float dashCooldown = 1f;         // Tempo de espera para usar o dash de novo

    [Header("Consumo de Stamina")]
    public float dashStaminaCost = 20f;     // Quanto de stamina gasta por dash

    private bool canDash = true;
    private bool isDashing = false;

    private Rigidbody2D rb;
    private PlayerMove playerMove;
    private Animator anim;

    // Componente visual (opcional). O TrailRenderer deixa um "rastro" legal durante o dash.
    private TrailRenderer trail;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMove = GetComponent<PlayerMove>();
        anim = GetComponentInChildren<Animator>(); // Pega o animator se quiser tocar animação
        trail = GetComponent<TrailRenderer>();

        if (trail != null)
            trail.emitting = false;
    }

    void Update()
    {
        // Se já estiver dando dash ou estiver atordoado, ignora
        if (isDashing || playerMove.isStunned) return;

        var kb = Keyboard.current;

        // Verifica se apertou a barra de Espaço (ou mude para shiftKey se preferir)
        if (kb != null && kb.shiftKey.wasPressedThisFrame && canDash)
        {
            // Verifica se tem stamina suficiente
            if (playerMove.stamina >= dashStaminaCost)
            {
                StartDash();
            }
            else
            {
                Debug.Log("Sem stamina para o Dash!");
            }
        }
    }

    private void StartDash()
    {
        // Define a direção do dash. Se ele estiver andando, vai na direção do movimento.
        // Se estiver parado, vai na direção para onde ele está olhando.
        Vector2 dashDirection = playerMove.MovementInput;

        if (dashDirection == Vector2.zero)
            dashDirection = playerMove.LookDirection;

        // Consome a stamina do PlayerMove
        playerMove.stamina -= dashStaminaCost;

        // Inicia a corrotina
        StartCoroutine(DashRoutine(dashDirection.normalized));
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        canDash = false;
        isDashing = true;

        // 1. Trava o script de movimento normal para ele não sobrescrever a velocidade
        playerMove.SetExternalControlLock(true);

        // 2. Opcional: Liga o rastro e toca animação de dash (se você tiver uma)
        if (trail != null) trail.emitting = true;
        if (anim != null) anim.SetTrigger("Dash"); // Crie um trigger "Dash" no Animator depois

        // 3. Aplica a velocidade alta na direção desejada
        rb.linearVelocity = direction * dashSpeed;

        // 4. Espera o tempo de duração do dash
        yield return new WaitForSeconds(dashDuration);

        // 5. Para o personagem, desliga o rastro e destrava o movimento
        rb.linearVelocity = Vector2.zero;
        if (trail != null) trail.emitting = false;

        playerMove.SetExternalControlLock(false);
        isDashing = false;

        // 6. Espera o tempo de recarga (Cooldown)
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // Método público para outros scripts saberem se o player está no meio de um dash
    // (Útil para deixá-lo invulnerável a dano durante o dash, por exemplo)
    public bool IsDashing()
    {
        return isDashing;
    }
}