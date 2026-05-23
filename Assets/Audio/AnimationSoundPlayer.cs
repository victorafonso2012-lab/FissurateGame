using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class AnimationSoundPlayer : MonoBehaviour
{
    [System.Serializable]
    public class AnimationSound
    {
        public string animationName;    // Nome do estado da animação
        public AudioClip sound;         // Som que vai tocar
        public bool loop;               // Se deve tocar em loop ou apenas uma vez
    }

    public AnimationSound[] sounds;     // Lista configurável no Inspector

    private Animator anim;
    private AudioSource audioSource;

    private string currentAnimation = "";
    private AudioClip currentClip;

    void Start()
    {
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);

        // Nome atual da animação
        string newAnimation = state.IsTag("Any") ? "" : GetCurrentAnimationName(state);

        if (newAnimation != currentAnimation)
        {
            currentAnimation = newAnimation;
            PlaySoundForAnimation(newAnimation);
        }
    }

    private string GetCurrentAnimationName(AnimatorStateInfo state)
    {
        // Lê o nome do state (ex: "Run", "Attack", "Death")
        return anim.GetCurrentAnimatorClipInfo(0)[0].clip.name;
    }

    private void PlaySoundForAnimation(string animationName)
    {
        // Procura se existe um som configurado para essa animação
        foreach (var s in sounds)
        {
            if (s.animationName == animationName)
            {
                // se já está tocando o mesmo som, não toca de novo
                if (currentClip == s.sound) return;

                currentClip = s.sound;

                audioSource.loop = s.loop;
                audioSource.clip = s.sound;
                audioSource.Play();
                return;
            }
        }

        // Se a animação não tem som → para o áudio
        audioSource.Stop();
        currentClip = null;
    }
}
