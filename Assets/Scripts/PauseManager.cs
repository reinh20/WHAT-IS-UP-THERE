using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Manages pause state (toggled with 'P' or 'Escape') and the in-game Settings panel:
/// - Pauses time and unlocks mouse cursor
/// - Master volume slider (synced with MainMenu via PlayerPrefs)
/// - Bilingual localization (English / Indonesian)
/// - Resume, Restart Checkpoint, and Main Menu navigation
/// </summary>
public class PauseManager : MonoBehaviour
{
    private static PauseManager _instance;
    public static PauseManager Instance => _instance;

    public static bool IsPaused { get; private set; } = false;
    public GameLanguage CurrentLanguage => currentLanguage;

    [Header("UI Panels")]
    public GameObject pausePanel;

    [Header("Audio Settings")]
    public Slider volumeSlider;
    public TMP_Text volumePercentText;

    [Header("Language Buttons")]
    public Button englishButton;
    public Button indonesianButton;
    public Image englishBtnBg;
    public Image indonesianBtnBg;

    [Header("Buttons")]
    public Button resumeButton;
    public Button restartCheckpointButton;
    public Button mainMenuButton;

    [Header("Localized Text Elements")]
    public TMP_Text pauseHeaderText;
    public TMP_Text audioLabelText;
    public TMP_Text languageLabelText;
    public TMP_Text resumeBtnText;
    public TMP_Text restartBtnText;
    public TMP_Text mainMenuBtnText;
    public TMP_Text englishBtnText;
    public TMP_Text indonesianBtnText;

    [Header("Styling Colors")]
    public Color activeLangColor = new Color(0.12f, 0.82f, 0.72f, 1f);   // Vibrant cyan/mint
    public Color inactiveLangColor = new Color(0.18f, 0.22f, 0.28f, 0.75f); // Sleek dark

    private const string PREF_VOLUME = "MasterVolume";
    private const string PREF_LANGUAGE = "GameLanguage";

    private GameLanguage currentLanguage = GameLanguage.English;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        IsPaused = false;
        Time.timeScale = 1f;

        EnsureEventSystem();

        // 1. Audio volume setup
        float savedVolume = PlayerPrefs.GetFloat(PREF_VOLUME, 0.75f);
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        SetMasterVolume(savedVolume);

        // 2. Language setup
        int savedLang = PlayerPrefs.GetInt(PREF_LANGUAGE, (int)GameLanguage.English);
        SetLanguage((GameLanguage)savedLang);

        // 3. Hook Button Click Listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }
        if (restartCheckpointButton != null)
        {
            restartCheckpointButton.onClick.RemoveListener(RestartCheckpoint);
            restartCheckpointButton.onClick.AddListener(RestartCheckpoint);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(LoadMainMenu);
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
        if (englishButton != null)
        {
            englishButton.onClick.RemoveListener(SetLanguageEnglish);
            englishButton.onClick.AddListener(SetLanguageEnglish);
        }
        if (indonesianButton != null)
        {
            indonesianButton.onClick.RemoveListener(SetLanguageIndonesian);
            indonesianButton.onClick.AddListener(SetLanguageIndonesian);
        }

        // Start hidden
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void Update()
    {
        // No pausing during the ending credits
        if (FinishLine.IsGameFinished) return;

        bool toggleRequested = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                toggleRequested = true;
            }
        }
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            toggleRequested = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
            {
                toggleRequested = true;
            }
        }
        catch { }
#endif

        if (toggleRequested)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        EnsureEventSystem();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // Refresh settings from PlayerPrefs
        float savedVolume = PlayerPrefs.GetFloat(PREF_VOLUME, AudioListener.volume);
        if (volumeSlider != null) volumeSlider.value = savedVolume;
        SetMasterVolume(savedVolume);

        int savedLang = PlayerPrefs.GetInt(PREF_LANGUAGE, (int)currentLanguage);
        SetLanguage((GameLanguage)savedLang);
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void RestartCheckpoint()
    {
        ResumeGame();
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer();
        }
    }

    public void LoadMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

#if UNITY_EDITOR
        UnityEditor.Selection.activeObject = null;
#endif

        try
        {
            SceneManager.LoadScene("main menu");
        }
        catch
        {
            SceneManager.LoadScene(0);
        }
    }

    public void SetMasterVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        AudioListener.volume = clamped;
        PlayerPrefs.SetFloat(PREF_VOLUME, clamped);
        PlayerPrefs.Save();

        if (volumePercentText != null)
        {
            volumePercentText.text = Mathf.RoundToInt(clamped * 100f) + "%";
        }
    }

    public void SetLanguageEnglish() => SetLanguage(GameLanguage.English);
    public void SetLanguageIndonesian() => SetLanguage(GameLanguage.Indonesian);

    public void SetLanguage(GameLanguage lang)
    {
        currentLanguage = lang;
        PlayerPrefs.SetInt(PREF_LANGUAGE, (int)lang);
        PlayerPrefs.Save();

        UpdateLanguageVisuals();
        UpdateLocalizedTexts();
    }

    private void UpdateLanguageVisuals()
    {
        bool isEn = (currentLanguage == GameLanguage.English);

        // English button styling
        if (englishButton != null)
        {
            ColorBlock cb = englishButton.colors;
            cb.normalColor = isEn ? activeLangColor : inactiveLangColor;
            cb.highlightedColor = isEn ? Color.Lerp(activeLangColor, Color.white, 0.2f) : Color.Lerp(inactiveLangColor, Color.white, 0.2f);
            cb.pressedColor = activeLangColor;
            cb.selectedColor = cb.normalColor;
            englishButton.colors = cb;

            if (englishButton.image != null)
            {
                englishButton.image.color = isEn ? activeLangColor : inactiveLangColor;
            }
        }
        else if (englishBtnBg != null)
        {
            englishBtnBg.color = isEn ? activeLangColor : inactiveLangColor;
        }

        // Indonesian button styling
        if (indonesianButton != null)
        {
            ColorBlock cb = indonesianButton.colors;
            cb.normalColor = !isEn ? activeLangColor : inactiveLangColor;
            cb.highlightedColor = !isEn ? Color.Lerp(activeLangColor, Color.white, 0.2f) : Color.Lerp(inactiveLangColor, Color.white, 0.2f);
            cb.pressedColor = activeLangColor;
            cb.selectedColor = cb.normalColor;
            indonesianButton.colors = cb;

            if (indonesianButton.image != null)
            {
                indonesianButton.image.color = !isEn ? activeLangColor : inactiveLangColor;
            }
        }
        else if (indonesianBtnBg != null)
        {
            indonesianBtnBg.color = !isEn ? activeLangColor : inactiveLangColor;
        }

        // Button label contrast
        if (englishBtnText != null) englishBtnText.color = isEn ? new Color(0.02f, 0.08f, 0.1f, 1f) : Color.white;
        if (indonesianBtnText != null) indonesianBtnText.color = !isEn ? new Color(0.02f, 0.08f, 0.1f, 1f) : Color.white;
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var existingEs = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEs == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }
        }
    }

    private void UpdateLocalizedTexts()
    {
        bool isID = (currentLanguage == GameLanguage.Indonesian);

        if (pauseHeaderText != null) pauseHeaderText.text = isID ? "PENGATURAN / JEDA" : "PAUSED / SETTINGS";
        if (audioLabelText != null) audioLabelText.text = isID ? "Volume Audio" : "Audio Volume";
        if (languageLabelText != null) languageLabelText.text = isID ? "Bahasa" : "Language";
        if (resumeBtnText != null) resumeBtnText.text = isID ? "Lanjutkan [P]" : "Resume [P]";
        if (restartBtnText != null) restartBtnText.text = isID ? "Ulangi Checkpoint" : "Restart Checkpoint";
        if (mainMenuBtnText != null) mainMenuBtnText.text = isID ? "Menu Utama" : "Main Menu";

        if (englishBtnText != null) englishBtnText.text = isID ? "Inggris" : "English";
        if (indonesianBtnText != null) indonesianBtnText.text = isID ? "Indonesia" : "Indonesian";
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            IsPaused = false;
            Time.timeScale = 1f;
        }
    }
}
