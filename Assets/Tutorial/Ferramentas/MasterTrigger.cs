using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider2D))]
public class MasterTrigger : MonoBehaviour
{
    [Header("Câmera")]
    public CinemachineCamera cameraToActivate;

    [Header("Objetos que vão emergir")]
    public FloatingObject[] objectsToActivate;

    [Header("Prioridade da câmera")]
    public int priorityOnEnter = 11;
    public int priorityOnExit = 9;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (cameraToActivate != null)
            {
                if (cameraToActivate.Follow == null)
                    cameraToActivate.Follow = other.transform;

                cameraToActivate.Priority = priorityOnEnter;
            }

            // 🔼 Ativa as casas (subida)
            foreach (FloatingObject obj in objectsToActivate)
                if (obj != null)
                    obj.Activate();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (cameraToActivate != null)
                cameraToActivate.Priority = priorityOnExit;

            // 🔽 Desativa as casas (descida)
            foreach (FloatingObject obj in objectsToActivate)
                if (obj != null)
                    obj.Deactivate();
        }
    }
}
