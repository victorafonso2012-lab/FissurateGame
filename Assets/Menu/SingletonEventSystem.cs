using UnityEngine;

public class SingletonEventSystem : MonoBehaviour
{
    private static SingletonEventSystem instance;

    void Awake()
    {
        // Se já existe uma instância (de uma Scene anterior), destrua este novo EventSystem.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            // Senão, defina esta como a instância única e a preserve entre Scenes.
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}