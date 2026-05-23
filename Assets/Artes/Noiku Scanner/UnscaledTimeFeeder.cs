using UnityEngine;
using UnityEngine.UI;

public class UnscaledTimeFeeder : MonoBehaviour
{
    private Material targetMaterial;
    private float timeTracker;

    public Material TargetMaterial => targetMaterial;

    void Awake()
    {
        RebindTargetMaterial();
    }

    void OnEnable()
    {
        if (targetMaterial == null)
            RebindTargetMaterial();
    }

    public void RebindTargetMaterial()
    {
        Graphic graphic = GetComponent<Graphic>();
        if (graphic == null || graphic.material == null)
        {
            targetMaterial = null;
            return;
        }

        if (targetMaterial != null && graphic.material == targetMaterial)
            return;

        targetMaterial = new Material(graphic.material);
        graphic.material = targetMaterial;
    }

    void Update()
    {
        if (targetMaterial == null)
            return;

        timeTracker += Time.unscaledDeltaTime;
        targetMaterial.SetFloat("_ManualTime", timeTracker);
    }
}
