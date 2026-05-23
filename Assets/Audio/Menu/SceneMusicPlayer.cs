using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMusicPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] musicas; // Coloque todas as músicas aqui

    private static SceneMusicPlayer instance;

    private void Awake()
    {
        // Singleton para não duplicar nas cenas seguintes
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene cena, LoadSceneMode modo)
    {
        TocarMusicaDaCena(cena.name);
    }

    // Escolhe a música conforme a cena
    private void TocarMusicaDaCena(string nomeCena)
    {
        AudioClip musica = EscolherMusicaPorCena(nomeCena);

        if (musica != null && musica != audioSource.clip)
        {
            audioSource.clip = musica;
            audioSource.Play();
        }
    }


    private AudioClip EscolherMusicaPorCena(string cena)
    {
        switch (cena)
        {
            case "MainMenu":
                return musicas.Length > 0 ? musicas[0] : null;

            case "Game_Tutorial":
                return musicas.Length > 1 ? musicas[1] : null;

            case "PreFase":
                return musicas.Length > 2 ? musicas[2] : null;

            case "Fase1":
                return musicas.Length > 3 ? musicas[3] : null;

            case "Boss":
                return musicas.Length > 4 ? musicas[4] : null;

            default:
                return null;
        }
    }
}
