using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameOverMenuController : MonoBehaviour
{
    [Header("Opções do Game Over")]
    public List<TextMeshProUGUI> menuOptions;

    [Header("Configuração Visual")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color selectedColor = new Color(0f, 0f, 0f, 1f);
    public string selectorPrefix = "> ";

    [Header("Restart")]
    public RestartButton restartButton;

    private int selectedIndex = 0;
    private readonly List<string> originalTexts = new List<string>();
    private bool initialized = false;

    void Awake()
    {
        originalTexts.Clear();

        if (menuOptions != null)
        {
            foreach (var option in menuOptions)
            {
                originalTexts.Add(option != null ? option.text : string.Empty);
            }
        }

        initialized = true;
    }

    void OnEnable()
    {
        if (!initialized) return;
        if (menuOptions == null || menuOptions.Count == 0) return;

        selectedIndex = 0;
        UpdateVisuals();
    }

    void Update()
    {
        if (!initialized) return;
        if (menuOptions == null || menuOptions.Count == 0) return;

        HandleNavigation();
        HandleSelection();
    }

    void HandleNavigation()
    {
        bool moved = false;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex--;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex++;
            moved = true;
        }

        if (moved)
        {
            if (selectedIndex < 0) selectedIndex = menuOptions.Count - 1;
            if (selectedIndex >= menuOptions.Count) selectedIndex = 0;
            UpdateVisuals();
        }
    }

    void HandleSelection()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ExecuteAction();
        }
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < menuOptions.Count; i++)
        {
            if (menuOptions[i] == null) continue;

            if (i == selectedIndex)
            {
                menuOptions[i].color = selectedColor;
                menuOptions[i].text = selectorPrefix + originalTexts[i];
                menuOptions[i].fontStyle = FontStyles.Bold;
            }
            else
            {
                menuOptions[i].color = normalColor;
                menuOptions[i].text = originalTexts[i];
                menuOptions[i].fontStyle = FontStyles.Normal;
            }
        }
    }

    void ExecuteAction()
    {
        switch (selectedIndex)
        {
            case 0:
                if (restartButton == null)
                    restartButton = Object.FindFirstObjectByType<RestartButton>();

                if (restartButton != null)
                {
                    restartButton.RestartLevel();
                }
                else
                {
                    Debug.LogError("GameOverMenuController: RestartButton não encontrado.");
                }
                break;

            case 1:
                Debug.Log("Saindo do Jogo...");
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;

                #else
                    Application.Quit();
                #endif
                break;
        }
    }
}
