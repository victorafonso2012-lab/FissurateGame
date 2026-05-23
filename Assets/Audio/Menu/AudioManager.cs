using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioMixer audioMixer;
    public AudioSource musicSource;

    private void Awake()
    {
        // Garante que só exista um AudioManager no jogo inteiro
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // mantém o objeto ao trocar de cena
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 1. Carrega o volume salvo em PlayerPrefs, usando 1.0f como valor padrão
        float savedVolume = PlayerPrefs.GetFloat("volume", 1.0f);

        // 2. Aplica o volume carregado ao AudioMixer
        SetVolume(savedVolume);

        // Começa a música se ainda não estiver tocando
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void SetVolume(float volume)
    {
        // volume vem de um slider (0.0001 a 1)
        // converte pra decibéis (escala logarítmica)
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
    }
}
