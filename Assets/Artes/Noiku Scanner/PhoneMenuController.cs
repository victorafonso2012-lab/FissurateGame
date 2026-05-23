using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KeyboardMenuController : MonoBehaviour
{
    [Header("Referências")]
    public PhoneSystemController phoneSystem;

    [Header("Opções do Menu")]
    // Element 0: Resume, Element 1: Quit
    public List<TextMeshProUGUI> menuOptions;

    [Header("Configuração Visual")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color selectedColor = new Color(0f, 0f, 0f, 1f);
    public string selectorPrefix = "> ";

    private int selectedIndex = 0;
    private List<string> originalTexts = new List<string>();
    private bool initialized = false;

    void Awake()
    {
        // Salva os textos originais apenas uma vez
        foreach (var option in menuOptions)
        {
            if (option != null) originalTexts.Add(option.text);
        }
        initialized = true;
    }

    // ESSA É A MÁGICA:
    // Essa função roda sozinha toda vez que o GameObject é ativado (SetActive true)
    void OnEnable()
    {
        if (!initialized) return;

        // Reseta o cursor para o topo sempre que o menu abrir
        selectedIndex = 0;
        UpdateVisuals();
    }

    void Update()
    {
        // Não precisamos mais checar Time.timeScale aqui,
        // porque se este script estiver rodando, é CERTEZA que o menu está aberto.

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
            case 0: // Resume
                if (phoneSystem != null) phoneSystem.TogglePauseMode(false);
                break;
            case 1: // Quit
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