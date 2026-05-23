using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelUpMenuController : MonoBehaviour
{
    [Header("References")]
    public PhoneSystemController phoneSystem;
    public List<Button> optionButtons = new List<Button>();

    [Header("Visual")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color selectedColor = new Color(0f, 0f, 0f, 1f);
    public string selectorPrefix = "> ";

    private readonly List<TMP_Text> optionLabels = new List<TMP_Text>();
    private readonly List<string> originalTexts = new List<string>();
    private readonly List<PlayerMove.AttributeLevelUpChoice> currentChoices = new List<PlayerMove.AttributeLevelUpChoice>();
    private int selectedIndex;

    void Awake()
    {
        CacheButtons();
        RegisterButtonCallbacks();
        TryFindPhoneSystem();
    }

    void OnEnable()
    {
        if (optionButtons == null || optionButtons.Count == 0)
            CacheButtons();

        TryFindPhoneSystem();
        selectedIndex = 0;
        RefreshAttributeChoices();
        UpdateVisuals();
    }

    void Update()
    {
        if (optionButtons == null || GetSelectableOptionCount() == 0)
            return;

        HandleNavigation();
        HandleSelection();
    }

    void CacheButtons()
    {
        if (optionButtons == null)
            optionButtons = new List<Button>();

        if (optionButtons.Count == 0)
            optionButtons.AddRange(GetComponentsInChildren<Button>(true));

        optionButtons.RemoveAll(button => button == null);
        optionLabels.Clear();
        originalTexts.Clear();

        foreach (Button button in optionButtons)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            optionLabels.Add(label);
            originalTexts.Add(label != null ? label.text : (button != null ? button.name : string.Empty));
        }
    }

    void RegisterButtonCallbacks()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (optionButtons[i] == null)
                continue;

            int capturedIndex = i;
            optionButtons[i].onClick.AddListener(() => OnOptionChosen(capturedIndex));
        }
    }

    void HandleNavigation()
    {
        bool moved = false;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedIndex--;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedIndex++;
            moved = true;
        }

        if (!moved)
            return;

        int optionCount = GetSelectableOptionCount();

        if (selectedIndex < 0) selectedIndex = optionCount - 1;
        if (selectedIndex >= optionCount) selectedIndex = 0;

        UpdateVisuals();
    }

    void HandleSelection()
    {
        if (!Input.GetKeyDown(KeyCode.E) && !Input.GetKeyDown(KeyCode.Return))
            return;

        if (selectedIndex < 0 || selectedIndex >= GetSelectableOptionCount())
            return;

        if (optionButtons[selectedIndex] != null)
            optionButtons[selectedIndex].onClick.Invoke();
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            TMP_Text label = i < optionLabels.Count ? optionLabels[i] : null;
            bool isAvailable = i < GetSelectableOptionCount();

            if (optionButtons[i] != null)
                optionButtons[i].gameObject.SetActive(isAvailable);

            if (label == null)
                continue;

            if (!isAvailable)
                continue;

            if (i == selectedIndex)
            {
                label.color = selectedColor;
                label.fontStyle = FontStyles.Bold;
                label.text = selectorPrefix + originalTexts[i];

                if (EventSystem.current != null && optionButtons[i] != null)
                    EventSystem.current.SetSelectedGameObject(optionButtons[i].gameObject);
            }
            else
            {
                label.color = normalColor;
                label.fontStyle = FontStyles.Normal;
                label.text = originalTexts[i];
            }
        }
    }

    void OnOptionChosen(int optionIndex)
    {
        if (PlayerMove.instance == null || !PlayerMove.instance.ApplyQueuedAttributeLevelUpChoice(optionIndex))
        {
            Debug.LogWarning($"LevelUpMenuController: nao foi possivel aplicar a opcao {optionIndex + 1}.");
            return;
        }

        selectedIndex = optionIndex;
        UpdateVisuals();

        Debug.Log($"LevelUpMenuController: opcao {optionIndex + 1} selecionada.");

        TryFindPhoneSystem();

        if (phoneSystem != null)
        {
            phoneSystem.CompleteLevelUpSelection();
        }
        else
        {
            Debug.LogWarning("LevelUpMenuController: PhoneSystemController nao encontrado.");
        }
    }

    void TryFindPhoneSystem()
    {
        if (phoneSystem == null)
            phoneSystem = FindAnyObjectByType<PhoneSystemController>();
    }

    void RefreshAttributeChoices()
    {
        currentChoices.Clear();

        if (PlayerMove.instance == null)
        {
            Debug.LogWarning("LevelUpMenuController: PlayerMove.instance nao encontrado para montar escolhas.");
            return;
        }

        if (!PlayerMove.instance.TryGetCurrentAttributeLevelUpChoices(out List<PlayerMove.AttributeLevelUpChoice> choices))
        {
            Debug.LogWarning("LevelUpMenuController: nenhuma escolha de atributo pendente.");
            return;
        }

        int choiceCount = Mathf.Min(3, choices.Count, optionButtons.Count);

        for (int i = 0; i < choiceCount; i++)
        {
            currentChoices.Add(choices[i]);

            if (i < originalTexts.Count)
                originalTexts[i] = choices[i].DisplayText;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, GetSelectableOptionCount() - 1));
    }

    int GetSelectableOptionCount()
    {
        if (currentChoices.Count > 0)
            return Mathf.Min(currentChoices.Count, optionButtons.Count);

        return optionButtons != null ? optionButtons.Count : 0;
    }
}
