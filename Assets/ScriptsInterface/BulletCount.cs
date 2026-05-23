using UnityEngine;

public class BulletCount : MonoBehaviour
{
    Animator anim;
    public GunAttack gun;
    string animNum;

    void Start()
    {
        anim = GetComponent<Animator>();
        // Linha corrigida para usar o método FindFirstObjectByType
        gun = FindFirstObjectByType<GunAttack>();
        // Se você tiver certeza de que só existe um GunAttack, use FindAnyObjectByType para um ganho de performance
        // gun = FindAnyObjectByType<GunAttack>();
    }

    void Update()
    {
        getAmmo();
        anim.Play(animNum);
    }
    void getAmmo()
    {
        // Certifique-se de que 'gun' não é nulo antes de acessar 'currentAmmo'
        if (gun != null)
        {
            animNum = gun.currentAmmo.ToString();
        }
    }
}