using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameLanguage
{
    English = 0,
    Indonesian = 1
}

public class MainMenuManager : MonoBehaviour
{
    [Header("Gameplay Scene")]
    [Tooltip("Target scene to load when Play is clicked")]
    public string gameplaySceneName = "LV 1";

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject tutorialPanel;

    [Header("Audio Settings")]
    public Slider volumeSlider;
    public TMP_Text volumePercentText;

    [Header("Language Buttons")]
    public Button englishButton;
    public Button indonesianButton;
    public Image englishBtnBg;
    public Image indonesianBtnBg;

    [Header("Localized Text Elements")]
    public TMP_Text titleText;
    [Tooltip("Customizable Game Title text")]
    public string gameTitle = "WHAT IS UP THERE";
    public TMP_Text subtitleText;
    public TMP_Text playBtnText;
    public TMP_Text settingsBtnText;
    public TMP_Text quitBtnText;
    public TMP_Text settingsHeaderText;
    public TMP_Text audioLabelText;
    public TMP_Text languageLabelText;
    public TMP_Text backBtnText;
    public TMP_Text englishBtnText;
    public TMP_Text indonesianBtnText;

    [Header("Tutorial Localized Texts")]
    public TMP_Text tutorialHeaderText;
    public TMP_Text tutorialSubheaderText;
    public TMP_Text moveLabelText;
    public TMP_Text moveDescText;
    public TMP_Text jumpLabelText;
    public TMP_Text jumpDescText;
    public TMP_Text slideLabelText;
    public TMP_Text slideDescText;
    public TMP_Text grappleLabelText;
    public TMP_Text grappleDescText;
    public TMP_Text wallRunLabelText;
    public TMP_Text wallRunDescText;
    public TMP_Text pauseLabelText;
    public TMP_Text pauseDescText;
    public TMP_Text objectiveTitleText;
    public TMP_Text objectiveDescText;
    public TMP_Text startLevelBtnText;
    public TMP_Text tutorialBackBtnText;

    [Header("Tutorial Buttons")]
    public Button startLevelButton;
    public Button tutorialBackButton;

    [Header("Tutorial Badges")]
    public TMP_Text moveBadgeText;
    public TMP_Text jumpBadgeText;
    public TMP_Text slideBadgeText;
    public TMP_Text grappleBadgeText;
    public TMP_Text wallRunBadgeText;
    public TMP_Text pauseBadgeText;

    [Header("Styling Colors")]
    public Color activeLangColor = new Color(0.12f, 0.82f, 0.72f, 1f);   // Vibrant cyan/mint active
    public Color inactiveLangColor = new Color(0.18f, 0.22f, 0.28f, 0.75f); // Subtle dark inactive

    private const string PREF_VOLUME = "MasterVolume";
    private const string PREF_LANGUAGE = "GameLanguage";

    private GameLanguage currentLanguage = GameLanguage.English;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 1. Audio volume setup (Default: 0.75 = 75%)
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

        // 2. Language setup (Default: English)
        int savedLang = PlayerPrefs.GetInt(PREF_LANGUAGE, (int)GameLanguage.English);
        SetLanguage((GameLanguage)savedLang);

        // 3. Hook Tutorial Buttons
        if (startLevelButton != null)
        {
            startLevelButton.onClick.RemoveListener(PlayGame);
            startLevelButton.onClick.AddListener(PlayGame);
        }
        if (tutorialBackButton != null)
        {
            tutorialBackButton.onClick.RemoveListener(ShowMainPanel);
            tutorialBackButton.onClick.AddListener(ShowMainPanel);
        }
    }

    private void Start()
    {
        ShowMainPanel();
    }

    // --- Panel Navigation ---

    public void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    public void ShowSettingsPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    public void ShowTutorialPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
    }

    private void Update()
    {
        // Keyboard navigation when tutorial panel is open
        if (tutorialPanel != null && tutorialPanel.activeSelf)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                {
                    PlayGame();
                }
                else if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    ShowMainPanel();
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    PlayGame();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ShowMainPanel();
                }
            }
            catch { }
#endif
        }
    }

    // --- Gameplay / App Lifecycle ---

    public void PlayGame()
    {
        // Ensure standard timeScale before loading scene
        Time.timeScale = 1f;

        // Load LV 1
        try
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        catch
        {
            SceneManager.LoadScene(1);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- Audio Control ---

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

    // --- Localization ---

    public void SetLanguageEnglish()
    {
        SetLanguage(GameLanguage.English);
    }

    public void SetLanguageIndonesian()
    {
        SetLanguage(GameLanguage.Indonesian);
    }

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

        // Text contrast colors
        if (englishBtnText != null) englishBtnText.color = isEn ? new Color(0.02f, 0.08f, 0.1f, 1f) : Color.white;
        if (indonesianBtnText != null) indonesianBtnText.color = !isEn ? new Color(0.02f, 0.08f, 0.1f, 1f) : Color.white;
    }

    private void UpdateLocalizedTexts()
    {
        bool isID = (currentLanguage == GameLanguage.Indonesian);

        if (titleText != null) titleText.text = !string.IsNullOrEmpty(gameTitle) ? gameTitle : "WHAT IS UP THERE";
        if (subtitleText != null) subtitleText.text = isID ? "PETUALANGAN PARKOUR & GRAPPLING" : "PARKOUR & GRAPPLING ADVENTURE";
        if (playBtnText != null) playBtnText.text = isID ? "Main" : "Play";
        if (settingsBtnText != null) settingsBtnText.text = isID ? "Pengaturan" : "Settings";
        if (quitBtnText != null) quitBtnText.text = isID ? "Keluar" : "Quit";

        if (settingsHeaderText != null) settingsHeaderText.text = isID ? "Pengaturan" : "Settings";
        if (audioLabelText != null) audioLabelText.text = isID ? "Volume Audio" : "Audio Volume";
        if (languageLabelText != null) languageLabelText.text = isID ? "Bahasa" : "Language";
        if (backBtnText != null) backBtnText.text = isID ? "Kembali" : "Back";

        if (englishBtnText != null) englishBtnText.text = isID ? "Inggris" : "English";
        if (indonesianBtnText != null) indonesianBtnText.text = isID ? "Indonesia" : "Indonesian";

        // Tutorial Translations
        if (tutorialHeaderText != null) tutorialHeaderText.text = isID ? "PANDUAN KONTROL & CARA BERMAIN" : "CONTROLS & HOW TO PLAY";
        if (tutorialSubheaderText != null) tutorialSubheaderText.text = isID ? "Kuasai manuver parkour dan grappling untuk bertahan hidup!" : "Master parkour maneuvers and grappling to survive!";

        if (moveLabelText != null) moveLabelText.text = isID ? "Gerakan Dasar" : "Basic Movement";
        if (moveDescText != null) moveDescText.text = isID ? "Jalan & Berlari mengelilingi level" : "Walk & run across the level";

        if (jumpLabelText != null) jumpLabelText.text = isID ? "Lompat & Bunny Hop" : "Jump & Bunny Hop";
        if (jumpDescText != null) jumpDescText.text = isID ? "Lompat berantai untuk mengakumulasi kecepatan" : "Chain jumps to accumulate high speed";

        if (slideLabelText != null) slideLabelText.text = isID ? "Meluncur (Slide)" : "Slide";
        if (slideDescText != null) slideDescText.text = isID ? "Meluncur saat bergerak atau di turunan untuk dorongan akselerasi" : "Slide while sprinting or on ramps for burst velocity";

        if (grappleLabelText != null) grappleLabelText.text = isID ? "Grappling Hook" : "Grappling Hook";
        if (grappleDescText != null) grappleDescText.text = isID ? "Tembak ke permukaan untuk berayun dan melontar tinggi" : "Shoot surfaces to swing and launch forward";

        if (wallRunLabelText != null) wallRunLabelText.text = isID ? "Wall Run" : "Wall Run";
        if (wallRunDescText != null) wallRunDescText.text = isID ? "Lompat mendekati dinding dan tahan W untuk berlari di tembok" : "Jump towards vertical walls and hold W to run along walls";

        if (pauseLabelText != null) pauseLabelText.text = isID ? "Menu Jeda & Seting" : "Pause & Settings";
        if (pauseDescText != null) pauseDescText.text = isID ? "Buka pengaturan audio dan ganti bahasa di tengah game" : "Adjust volume & language settings in-game";

        if (objectiveTitleText != null) objectiveTitleText.text = isID ? "TUJUAN UTAMA:" : "MAIN OBJECTIVE:";
        if (objectiveDescText != null) objectiveDescText.text = isID 
            ? "WASPADA: Pumpkin Merah akan mengejarmu! Jangan biarkan ia menangkapmu sebelum mencapai Checkpoint Akhir!" 
            : "WARNING: The Red Pumpkin will chase you! Don't let it catch you before reaching the Final Checkpoint!";

        if (startLevelBtnText != null) startLevelBtnText.text = isID ? "LEWATI & MASUK LV 1 [SPASI / ENTER]" : "SKIP & START LV 1 [SPACE / ENTER]";
        if (tutorialBackBtnText != null) tutorialBackBtnText.text = isID ? "Kembali" : "Back";

        // Badges
        if (moveBadgeText != null) moveBadgeText.text = "[ W A S D ]";
        if (jumpBadgeText != null) jumpBadgeText.text = isID ? "[ SPASI ]" : "[ SPACE ]";
        if (slideBadgeText != null) slideBadgeText.text = "[ CTRL / SHIFT ]";
        if (grappleBadgeText != null) grappleBadgeText.text = isID ? "[ KLIK KIRI ]" : "[ MOUSE 1 ]";
        if (wallRunBadgeText != null) wallRunBadgeText.text = isID ? "[ W + TEMBOK ]" : "[ W + WALL ]";
        if (pauseBadgeText != null) pauseBadgeText.text = "[ P / ESC ]";
    }
}
