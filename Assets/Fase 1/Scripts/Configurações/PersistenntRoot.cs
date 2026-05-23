using UnityEngine;

public class PersistentRoot : MonoBehaviour
{
    static PersistentRoot instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        // REMOVER:
        // DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
