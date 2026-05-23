using UnityEngine;
using System.Collections;

public class FloatingIcon : MonoBehaviour
{
    public float velocidade = 5f;
    public float altura = 0.2f;

    private Vector3 posInicial;
    private Vector3 escalaOriginal;

    // Variável extra para controlar o "inchaço" do soco
    private float escalaPunch = 1.0f;
    private bool floatingIcon;

    public bool GetFloatingIcon()
    {
        return floatingIcon;
    }

    internal void SetFloatingIcon(bool value)
    {
        floatingIcon = value;
    }

    void Start()
    {
        posInicial = transform.localPosition;
        escalaOriginal = transform.localScale;
    }

    void Update()
    {
        // Efeito "Bobbing" (Sobe e desce)
        float novoY = posInicial.y + Mathf.Sin(Time.time * velocidade) * altura;
        transform.localPosition = new Vector3(posInicial.x, novoY, posInicial.z);
    }

    // --- NOVA FUNÇÃO PÚBLICA PARA O PLAYER CHAMAR ---
    public void Punch(float intensidade = 1.3f, float duracao = 0.15f)
    {
        StopAllCoroutines(); // Para animações anteriores se apertar rápido
        StartCoroutine(PunchRoutine(intensidade, duracao));
    }

    private IEnumerator PunchRoutine(float intensidade, float duracao)
    {
        float timer = 0f;

        // Vai de 1.0 até 1.3 (incha)
        while (timer < duracao / 2)
        {
            timer += Time.deltaTime;
            escalaPunch = Mathf.Lerp(1.0f, intensidade, timer / (duracao / 2));
            yield return null;
        }

        // Vai de 1.3 até 1.0 (desincha)
        timer = 0f;
        while (timer < duracao / 2)
        {
            timer += Time.deltaTime;
            escalaPunch = Mathf.Lerp(intensidade, 1.0f, timer / (duracao / 2));
            yield return null;
        }

        escalaPunch = 1.0f; // Garante final limpo
    }

    void LateUpdate()
    {
        transform.rotation = Quaternion.identity;

        if (transform.parent != null)
        {
            float parentX = transform.parent.localScale.x;
            Vector3 newScale = escalaOriginal;

            // 1. Corrige a direção (Anti-Flip)
            newScale.x = Mathf.Abs(escalaOriginal.x) * Mathf.Sign(parentX);

            // 2. APLICA O PUNCH EM CIMA DA CORREÇÃO
            // Multiplicamos tudo pelo valor do punch atual
            newScale *= escalaPunch;

            transform.localScale = newScale;
        }
    }
}