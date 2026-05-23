using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExperienceOrb : MonoBehaviour
{
    [Header("XP")]
    public int experienceAmount = 5;

    [Header("Movimento")]
    public float moveToPlayerSpeed = 8f;
    public float detectionRange = 2.5f;

    [Header("Delay")]
    public float pickupDelay = 0.35f;

    private Transform player;
    private bool followingPlayer = false;
    private bool collected = false;
    private float timer = 0f;
    private Collider2D orbCollider;

    void Awake()
    {
        orbCollider = GetComponent<Collider2D>();
        if (orbCollider != null)
            orbCollider.isTrigger = true;
    }

    void Start()
    {
        TryFindPlayer();
    }

    void Update()
    {
        if (!TryFindPlayer()) return;

        timer += Time.deltaTime;
        if (timer < pickupDelay) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= detectionRange)
            followingPlayer = true;

        if (followingPlayer)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                moveToPlayerSpeed * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, player.position) <= 0.15f)
                Collect();
        }
    }

    public void SetExperienceAmount(int amount)
    {
        experienceAmount = amount;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (timer < pickupDelay) return;
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    private bool TryFindPlayer()
    {
        if (player != null)
            return true;

        if (PlayerMove.instance != null)
        {
            player = PlayerMove.instance.transform;
            return true;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return false;

        player = playerObj.transform;
        return true;
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;

        if (ExperienceManager.instance != null)
            ExperienceManager.instance.AddExperience(experienceAmount);
        else
            Debug.LogWarning("ExperienceOrb: ExperienceManager.instance não encontrado ao coletar XP.");

        Destroy(gameObject);
    }
}
