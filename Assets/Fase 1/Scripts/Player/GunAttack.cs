using UnityEngine;
using System.Collections;

public class GunAttack : MonoBehaviour
{
    [Header("Configurações da Arma")]
    public GameObject bulletPrefab; // Arraste aqui o Prefab da sua Bala
    public float bulletSpeed = 20f;
    public float attackCooldown = 0.5f;
    public float damage = 15f; // Dano da bala

    [Header("Munição")]
    public int maxAmmo = 3;
    public int currentAmmo;

    [Header("Referências")]
    public Transform firePoint; // Ponto de onde a bala sai (pode ser o mesmo 'AttackPoint')

    [HideInInspector] public bool isAttacking = false; // (Para cooldown)

    void Start()
    {
        currentAmmo = maxAmmo;
        UpdateAmmoUI(); // (Opcional: atualiza a UI)
    }

    /// <summary>
    /// Método público chamado pelo PlayerMove para tentar atirar.
    /// </summary>
    public void Attack()
    {
        // Só atira se não estiver em cooldown E tiver munição
        if (isAttacking || currentAmmo <= 0) return;

        StartCoroutine(PerformAttack());
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;

        // --- Lógica de Atirar ---
        currentAmmo--;
        Debug.Log($"Atirou! Munição restante: {currentAmmo}");
        UpdateAmmoUI(); // (Opcional: atualiza a UI)

        // Cria a bala
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript == null)
                bulletScript = bullet.AddComponent<Bullet>();

            // Configura a bala
            float poisonDamagePerSecond = PlayerMove.instance != null ? PlayerMove.instance.BulletPoisonDamagePerSecond : 0f;
            bulletScript.Initialize(firePoint.right, bulletSpeed, damage, poisonDamagePerSecond);
        }
        else
        {
            Debug.LogError("Prefab da Bala ou FirePoint não estão configurados no GunAttack!");
        }

        // Inicia o cooldown
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    /// <summary>
    /// Chamado pelo Player (quando um inimigo morre) para recarregar 1 bala.
    /// </summary>
    public void ReloadAmmo()
    {
        // --- LÓGICA MODIFICADA AQUI ---

        // Só recarrega se a munição não estiver cheia
        if (currentAmmo < maxAmmo)
        {
            currentAmmo++; // Adiciona 1 bala
            Debug.Log($"Recarregou 1 bala! Munição atual: {currentAmmo}");
            UpdateAmmoUI(); // (Opcional: atualiza a UI)
        }
        else
        {
            Debug.Log("Munição já estava cheia!");
        }
    }

    // Método simples para UI (opcional)
    void UpdateAmmoUI()
    {
        // Você pode conectar isso ao seu sistema de UI
        // Ex: UIManager.instance.SetAmmo(currentAmmo, maxAmmo);
    }
}
