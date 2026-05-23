using UnityEngine;

public class HealingFlower : MonoBehaviour, IDamageable
{
    public float cura = 20f;
    public GameObject efeitoQuebrar;

    private bool quebrada = false;

    public void TakeDamage(float damageAmount)
    {
        if (quebrada) return;
        quebrada = true;

        CurarPlayer();

        if (efeitoQuebrar != null)
            Instantiate(efeitoQuebrar, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    private void CurarPlayer()
    {
        if (PlayerMove.instance == null) return;

        PlayerHealth playerHealth = PlayerMove.instance.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Curar(cura);
        }
    }
}