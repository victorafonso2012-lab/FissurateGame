using UnityEngine;
using System.Collections;

public class EnemyParryWarningIcon : MonoBehaviour
{
    [Header("Icone")]
    public GameObject prefabIconeParry;
    public float alturaIcone = 1.2f;

    [Header("Tempo")]
    public float tempoVisivel = 0.7f;

    [Header("Render")]
    public int ordemAcimaDoInimigo = 20;

    private GameObject iconeAtual;
    private Coroutine rotinaAtual;

    public void MostrarAvisoParry()
    {
        if (rotinaAtual != null)
            StopCoroutine(rotinaAtual);

        rotinaAtual = StartCoroutine(MostrarRotina());
    }

    private IEnumerator MostrarRotina()
    {
        if (iconeAtual != null)
            Destroy(iconeAtual);

        if (prefabIconeParry == null)
        {
            Debug.LogWarning($"{gameObject.name}: prefabIconeParry nao foi configurado.");
            yield break;
        }

        iconeAtual = Instantiate(prefabIconeParry, transform);
        iconeAtual.transform.localPosition = new Vector3(0f, alturaIcone, 0f);
        iconeAtual.transform.localRotation = Quaternion.identity;

        AjustarOrdemDoIcone();

        Animator anim = iconeAtual.GetComponent<Animator>();
        if (anim != null)
            anim.Play(0, 0, 0f);

        FloatingIcon floating = iconeAtual.GetComponent<FloatingIcon>();
        if (floating != null)
            floating.SetFloatingIcon(true);

        yield return new WaitForSeconds(tempoVisivel);

        if (iconeAtual != null)
            Destroy(iconeAtual);

        rotinaAtual = null;
    }

    private void AjustarOrdemDoIcone()
    {
        SpriteRenderer enemyRenderer = GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer[] renderersIcone = iconeAtual.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer sr in renderersIcone)
        {
            if (sr == null)
                continue;

            if (enemyRenderer != null)
            {
                sr.sortingLayerID = enemyRenderer.sortingLayerID;
                sr.sortingOrder = enemyRenderer.sortingOrder + ordemAcimaDoInimigo;
            }
            else
            {
                sr.sortingOrder = ordemAcimaDoInimigo;
            }
        }
    }

    private void OnDisable()
    {
        if (iconeAtual != null)
            Destroy(iconeAtual);
    }

    private void OnDestroy()
    {
        if (iconeAtual != null)
            Destroy(iconeAtual);
    }
}