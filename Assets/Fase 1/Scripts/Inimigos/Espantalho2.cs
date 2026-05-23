using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class Espantalho2 : MonoBehaviour
{
    [Header("Movimento e ataque corpo a corpo")]
    public float speed = 5f;
    public float detectionRange = 5f;
    public float attackChance = 50f;
    public float minFollowDistance = 2f;

    [Header("Atributos de Vida")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDying = false;

    [Header("Ataque à distância")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 8f;
    public float shootDelay = 3f;
    public float shootInterval = 3f;
    public float sampleRate = 0.05f;

    [Header("Dano ao Player")]
    public float danoAoPlayer = 10f;

    private Transform player;
    private PlayerMove playerMove;
    private SpriteRenderer spriteRenderer;

    // histórico de posições do player (para prever movimento)
    private struct TimedPos { public float t; public Vector2 pos; }
    private List<TimedPos> history = new List<TimedPos>();
    private float sampleTimer = 0f;
    private float shootTimer = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerMove = player.GetComponent<PlayerMove>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;

        if (firePoint == null)
            firePoint = transform;
    }

    void Update()
    {
        if (isDying || player == null || playerMove == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > detectionRange) return;

        // chance de perseguir o jogador
        float roll = Random.Range(0f, 100f);
        if (roll <= attackChance && distance > minFollowDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        }

        // registra posição do player
        float now = Time.time;
        sampleTimer += Time.deltaTime;
        if (sampleTimer >= sampleRate)
        {
            sampleTimer = 0f;
            history.Add(new TimedPos { t = now, pos = player.position });
            float cutoff = now - (shootDelay + 1f);
            int removeCount = 0;
            while (removeCount < history.Count && history[removeCount].t < cutoff) removeCount++;
            if (removeCount > 0) history.RemoveRange(0, removeCount);
        }

        // atira projétil após intervalo
        shootTimer += Time.deltaTime;
        if (shootTimer >= shootInterval)
        {
            shootTimer = 0f;
            Vector2 targetPos;
            if (TryGetPositionAtTime(now - shootDelay, out targetPos))
            {
                FireAtPosition(targetPos);
            }
        }
    }

    private bool TryGetPositionAtTime(float targetT, out Vector2 posOut)
    {
        posOut = Vector2.zero;
        if (history.Count == 0) return false;

        if (targetT <= history[0].t)
        {
            posOut = history[0].pos;
            return true;
        }
        if (targetT >= history[history.Count - 1].t)
        {
            posOut = history[history.Count - 1].pos;
            return true;
        }
        for (int i = 0; i < history.Count - 1; i++)
        {
            if (history[i].t <= targetT && targetT <= history[i + 1].t)
            {
                float span = history[i + 1].t - history[i].t;
                float frac = (targetT - history[i].t) / span;
                posOut = Vector2.Lerp(history[i].pos, history[i + 1].pos, frac);
                return true;
            }
        }
        return false;
    }

    private void FireAtPosition(Vector2 targetPosition)
    {
        if (isDying || projectilePrefab == null) return;

        Vector2 dir = (targetPosition - (Vector2)firePoint.position).normalized;
        GameObject p = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = dir * projectileSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDying) return;

        // recebe dano de qualquer coisa com tag "Hit"
        if (collision.CompareTag("Hit"))
        {
            float danoRecebido = 0f;

            // se for uma espada
            if (collision.TryGetComponent<SwordAttack>(out SwordAttack sword))
            {
                if (sword.isAttacking)
                    danoRecebido = sword.damage;
            }


        }

        // dano ao player
        if (collision.CompareTag("Player"))
        {
            PlayerHealth player = collision.GetComponent<PlayerHealth>();
            if (player != null)
                player.TomarDano(danoAoPlayer);
        }
    }

    // ==============================
    // ======= SISTEMA DE VIDA ======
    // ==============================
    private void TakeDamage(float amount)
    {
        if (isDying) return;

        currentHealth -= amount;
        StartCoroutine(FlashRed());

        if (currentHealth <= 0f)
            Die();
    }

    private IEnumerator FlashRed()
    {
        if (spriteRenderer == null) yield break;

        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;

        // aqui pode colocar efeito, animação, som etc.
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minFollowDistance);
    }
}
