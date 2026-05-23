using UnityEngine;

public class ScannerCameraSync : MonoBehaviour
{
    [Header("Referência")]
    public Transform targetCamera;

    [Header("2D")]
    public bool copyRotation = false;
    public float fixedZ = -10f;

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            if (Camera.main != null)
                targetCamera = Camera.main.transform;
            else
                return;
        }

        Vector3 p = targetCamera.position;
        transform.position = new Vector3(p.x, p.y, fixedZ);

        if (copyRotation)
            transform.rotation = targetCamera.rotation;
    }
}
