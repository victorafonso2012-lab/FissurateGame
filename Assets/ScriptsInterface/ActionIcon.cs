using UnityEngine;
using System.Collections;

public class ActionIcon : MonoBehaviour
{
    [Header("Configuração Flutuar")]
    public bool flutuar = true; // O PlayerMove precisa acessar isso!
    public float velocidade = 5f;
    public float altura = 0.1f;

    private Vector3 escalaBase;
    private float fatorPunch = 1.0f;
    private Vector3 posInicial;
    private Coroutine punchRoutine;

    void Start()
    {
        escalaBase = transform.localScale;
        posInicial = transform.localPosition;
    }

    void Update()
    {
        // Se a variável 'flutuar' for verdadeira, ele sobe e desce
        if (flutuar)
        {
            float novoY = posInicial.y + Mathf.Sin(Time.time * velocidade) * altura;
            transform.localPosition = new Vector3(posInicial.x, novoY, posInicial.z);
        }
        else
        {
            // Se for falsa (quando preso na lama), ele fica parado na posição original
            transform.localPosition = posInicial;
        }
    }

    // Esta é a função que estava faltando ou diferente
    public void Punch(float intensidade = 1.3f, float duracao = 0.15f)
    {
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchRoutine(intensidade, duracao));
    }

    private IEnumerator PunchRoutine(float intensidade, float duracao)
    {
        float timer = 0f;
        // Incha
        while (timer < duracao / 2)
        {
            timer += Time.deltaTime;
            fatorPunch = Mathf.Lerp(1.0f, intensidade, timer / (duracao / 2));
            yield return null;
        }
        timer = 0f;
        // Desincha
        while (timer < duracao / 2)
        {
            timer += Time.deltaTime;
            fatorPunch = Mathf.Lerp(intensidade, 1.0f, timer / (duracao / 2));
            yield return null;
        }
        fatorPunch = 1.0f;
    }

    void LateUpdate()
    {
        transform.rotation = Quaternion.identity;

        if (transform.parent != null)
        {
            float parentX = transform.parent.localScale.x;
            Vector3 newScale = escalaBase;

            // Corrige espelhamento
            newScale.x = Mathf.Abs(escalaBase.x) * Mathf.Sign(parentX);

            // Aplica o soco
            newScale *= fatorPunch;

            transform.localScale = newScale;
        }
    }
}