using UnityEngine;
using UnityEngine.UI;

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager instance;

    [Header("Experience")]
    [SerializeField] private bool useFormulaProgression = true;
    [SerializeField] private int baseExperienceToNextLevel = 25;
    [UnityEngine.Serialization.FormerlySerializedAs("growthPercentPerLevel")]
    [SerializeField] private int additionalExperiencePerLevel = 10;
    [SerializeField] private AnimationCurve experienceCurve;

    private int currentLevel;
    private int totalExperience;
    private int previousLevelsExperience;
    private int nextLevelsExperience;
    private PhoneSystemController phoneSystem;

    [Header("Interface")]
    [SerializeField] private Image experienceFill;

    public int CurrentLevel => currentLevel;
    public int TotalExperience => totalExperience;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void Start()
    {
        TryFindPhoneSystem();
        UpdateLevel();
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        totalExperience += amount;
        CheckForLevelUp();
        UpdateInterface();
    }

    void CheckForLevelUp()
    {
        bool leveledUp = false;
        int gainedLevels = 0;
        bool queuedAttributeChoices = false;

        while (totalExperience >= nextLevelsExperience)
        {
            currentLevel++;
            gainedLevels++;
            UpdateLevel();
            leveledUp = true;

            // Aqui depois você pode tocar VFX, som, popup etc.
            Debug.Log($"Level Up! Novo nível: {currentLevel}");

            if (PlayerMove.instance != null)
            {
                int displayedPlayerLevel = PlayerMove.instance.PlayerLevel + gainedLevels;
                if (PlayerMove.instance.QueueAttributeLevelUpChoices(displayedPlayerLevel))
                    queuedAttributeChoices = true;
            }
        }

        if (leveledUp)
        {
            PlayerMove.instance?.IncreasePlayerLevel(gainedLevels);

            if (queuedAttributeChoices)
                TriggerLevelUpFeedback();
        }
    }

    void UpdateLevel()
    {
        if (useFormulaProgression)
        {
            previousLevelsExperience = GetTotalExperienceRequiredForLevel(currentLevel);
            nextLevelsExperience = GetTotalExperienceRequiredForLevel(currentLevel + 1);
        }
        else
        {
            previousLevelsExperience = Mathf.RoundToInt(experienceCurve.Evaluate(currentLevel));
            nextLevelsExperience = Mathf.RoundToInt(experienceCurve.Evaluate(currentLevel + 1));
        }

        UpdateInterface();
    }

    int GetTotalExperienceRequiredForLevel(int level)
    {
        if (level <= 0)
            return 0;

        int total = 0;

        for (int i = 0; i < level; i++)
        {
            total += baseExperienceToNextLevel + Mathf.Max(0, additionalExperiencePerLevel) * i;
        }

        return total;
    }

    void UpdateInterface()
    {
        if (experienceFill == null) return;

        int start = totalExperience - previousLevelsExperience;
        int end = nextLevelsExperience - previousLevelsExperience;

        if (end <= 0)
        {
            experienceFill.fillAmount = 1f;
            return;
        }

        experienceFill.fillAmount = Mathf.Clamp01((float)start / end);
    }

    void TriggerLevelUpFeedback()
    {
        if (phoneSystem == null)
            TryFindPhoneSystem();

        if (phoneSystem != null)
        {
            phoneSystem.TriggerLevelUpSequence();
        }
        else
        {
            Debug.LogWarning("ExperienceManager: PhoneSystemController nao encontrado para o efeito de level up.");
        }
    }

    void TryFindPhoneSystem()
    {
        if (phoneSystem == null)
            phoneSystem = FindAnyObjectByType<PhoneSystemController>();
    }
}
