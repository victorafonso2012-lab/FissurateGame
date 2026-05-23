using UnityEngine;
using Unity.Cinemachine; // Não se esqueça de adicionar esta linha!

// Coloque este script no seu objeto VCam_FollowPlayer
[RequireComponent(typeof(CinemachineCamera))]
public class VCamTargetAssigner : MonoBehaviour
{
    void Start()
    {
        var vcam = GetComponent<CinemachineCamera>();

        // O seu PlayerMove.instance é definido no Awake(),
        // então no Start() deste script ele JÁ DEVE existir.
        if (PlayerMove.instance != null)
        {
            // Se o 'Follow' estiver vazio, preenche.
            if (vcam.Follow == null)
            {
                vcam.Follow = PlayerMove.instance.transform;
                Debug.Log("Referência 'Follow' da VCam atribuída ao Player (via script).");
            }

            // Se o 'LookAt' estiver vazio, preenche também.
            // (Muitas câmeras 2D usam o Follow e o LookAt no mesmo objeto)
            if (vcam.LookAt == null)
            {
                vcam.LookAt = PlayerMove.instance.transform;
                Debug.Log("Referência 'LookAt' da VCam atribuída ao Player (via script).");
            }
        }
        else
        {
            // Este erro é grave e indica um problema de ordem de inicialização
            Debug.LogError("VCamTargetAssigner: Não foi possível encontrar PlayerMove.instance no Start()!");
        }
    }
}