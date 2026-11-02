using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Finish line for the level. When the player touches/lands on this object:
/// - Player movement, audio, and all pumpkin hazards are stopped
/// - Screen fades to black
/// - Ending credits are shown one by one with fade-in / fade-out in the active GameLanguage (Indonesian / English)
/// - Returns to the main menu when the credits finish
/// </summary>
[DisallowMultipleComponent]
public class FinishLine : MonoBehaviour
{
    public enum LanguageMode
    {
        FollowGameSettings,
        ForceIndonesian,
        ForceEnglish
    }

    [System.Serializable]
    public class LocalizedCreditEntry
    {
        [Header("Indonesian (Bahasa Indonesia)")]
        [TextArea(1, 3)] public string titleID;
        [TextArea(1, 3)] public string subtitleID;

        [Header("English")]
        [TextArea(1, 3)] public string titleEN;
        [TextArea(1, 3)] public string subtitleEN;

        public LocalizedCreditEntry() { }

        public LocalizedCreditEntry(string titleID, string subtitleID, string titleEN, string subtitleEN)
        {
            this.titleID = titleID;
            this.subtitleID = subtitleID;
            this.titleEN = titleEN;
            this.subtitleEN = subtitleEN;
        }

        public string GetTitle(GameLanguage lang)
        {
            if (lang == GameLanguage.Indonesian)
                return !string.IsNullOrEmpty(titleID) ? titleID : titleEN;
            return !string.IsNullOrEmpty(titleEN) ? titleEN : titleID;
        }

        public string GetSubtitle(GameLanguage lang)
        {
            if (lang == GameLanguage.Indonesian)
                return !string.IsNullOrEmpty(subtitleID) ? subtitleID : subtitleEN;
            return !string.IsNullOrEmpty(subtitleEN) ? subtitleEN : subtitleID;
        }
    }

    /// <summary>True once the player has reached the finish line (blocks jumpscares/pause).</summary>
    public static bool IsGameFinished { get; private set; } = false;

    private const string PREF_LANGUAGE = "GameLanguage";

    [Header("Language Mode")]
    [Tooltip("Follow the game's active language setting (from MainMenu / Pause menu) or force a specific language for testing.")]
    public LanguageMode languageMode = LanguageMode.FollowGameSettings;

    [Header("Detection")]
    [Tooltip("Extra height above the finish line collider that still counts as reaching the finish (for grappling/jumping over it)")]
    public float detectionHeightAbove = 8f;

    [Header("Credits Content (Bilingual)")]
    public LocalizedCreditEntry[] credits = new LocalizedCreditEntry[]
    {
        new LocalizedCreditEntry(
            titleID: "Terima kasih telah memainkan game yang aku buat",
            subtitleID: "",
            titleEN: "Thank you for playing my game",
            subtitleEN: ""
        ),
        new LocalizedCreditEntry(
            titleID: "Credit",
            subtitleID: "Dikembangkan oleh Rein",
            titleEN: "Credits",
            subtitleEN: "Developed by Rein"
        ),
        new LocalizedCreditEntry(
            titleID: "Credit to Salmon",
            subtitleID: "untuk font",
            titleEN: "Credit to Salmon",
            subtitleEN: "for the font"
        )
    };

    [Header("Fonts")]
    public TMP_FontAsset titleFont;
    public TMP_FontAsset subtitleFont;
    public float titleFontSize = 64f;
    public float subtitleFontSize = 42f;
    public Color titleColor = new Color(1f, 0.62f, 0.18f, 1f);   // Warm pumpkin orange
    public Color subtitleColor = new Color(0.92f, 0.9f, 0.86f, 1f);

    [Header("Timing (seconds, unscaled)")]
    public float screenFadeInDuration = 1.5f;
    public float delayBeforeCredits = 0.6f;
    public float textFadeInDuration = 1.2f;
    public float textHoldDuration = 2.8f;
    public float textFadeOutDuration = 1.2f;
    public float gapBetweenEntries = 0.5f;
    public float delayAfterCredits = 1.0f;

    [Header("After Credits")]
    public bool returnToMainMenu = true;
    public string mainMenuSceneName = "main menu";

    private Collider _collider;
    private bool _triggered = false;
    private Transform _player;

    private Canvas _canvas;
    private Image _blackScreen;
    private CanvasGroup _textGroup;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _subtitleText;

    private void Awake()
    {
        IsGameFinished = false;
        _collider = GetComponent<Collider>();
    }

    public GameLanguage GetActiveLanguage()
    {
        if (languageMode == LanguageMode.ForceIndonesian) return GameLanguage.Indonesian;
        if (languageMode == LanguageMode.ForceEnglish) return GameLanguage.English;

        if (PauseManager.Instance != null)
        {
            return PauseManager.Instance.CurrentLanguage;
        }

        return (GameLanguage)PlayerPrefs.GetInt(PREF_LANGUAGE, (int)GameLanguage.English);
    }

    private void Update()
    {
        if (_triggered || _collider == null) return;

        if (_player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null)
            {
                var pm = FindAnyObjectByType<PlayerMovement>();
                if (pm != null) p = pm.gameObject;
            }
            if (p != null) _player = p.transform;
            if (_player == null) return;
        }

        // Fallback detection: player is above/inside the finish line area
        Bounds b = _collider.bounds;
        b.Encapsulate(new Vector3(b.center.x, b.max.y + detectionHeightAbove, b.center.z));
        if (b.Contains(_player.position))
        {
            TryFinish(_player.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryFinish(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryFinish(other.gameObject);
    }

    private void TryFinish(GameObject other)
    {
        if (_triggered || other == null) return;

        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null;
        if (!isPlayer) return;

        // Don't finish in the middle of a jumpscare
        if (PumpkinJumpscareManager.Instance != null && PumpkinJumpscareManager.Instance.IsJumpscareActive) return;

        _triggered = true;
        IsGameFinished = true;
        StartCoroutine(FinishSequence(other));
    }

    private IEnumerator FinishSequence(GameObject playerObj)
    {
        GameLanguage activeLang = GetActiveLanguage();
        Debug.Log($"[FinishLine] Player reached the finish line! Playing credits in language: {activeLang}...");

        // Make sure the game isn't paused
        if (PauseManager.IsPaused && PauseManager.Instance != null)
        {
            PauseManager.Instance.ResumeGame();
        }
        Time.timeScale = 1f;

        // 1. Lock the player
        PlayerMovement pm = playerObj.GetComponentInParent<PlayerMovement>();
        if (pm != null)
        {
            pm.StopAllAudio();
            pm.enabled = false;
            Rigidbody rb = pm.GetComponent<Rigidbody>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        // Stop grappling if any
        foreach (var gun in FindObjectsByType<GrapplingGun>())
        {
            gun.enabled = false;
        }

        // 2. Disable all pumpkin hazards
        foreach (var chaser in FindObjectsByType<PumpkinChaser>())
        {
            chaser.enabled = false;
        }
        foreach (var watcher in FindObjectsByType<PumpkinWatcher>())
        {
            watcher.enabled = false;
        }

        // 3. Build credits UI
        BuildCreditsUI();

        // 4. Fade screen to black + fade game audio
        float startVolume = AudioListener.volume;
        float t = 0f;
        while (t < screenFadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / screenFadeInDuration));
            _blackScreen.color = new Color(0f, 0f, 0f, k);
            AudioListener.volume = Mathf.Lerp(startVolume, 0f, k);
            yield return null;
        }
        _blackScreen.color = Color.black;

        // Stop any remaining world audio, then restore listener volume (credits are silent)
        foreach (var src in FindObjectsByType<AudioSource>())
        {
            if (src != null) src.Stop();
        }
        AudioListener.volume = startVolume;

        yield return new WaitForSecondsRealtime(delayBeforeCredits);

        // 5. Show each credit entry with fade in / fade out
        foreach (LocalizedCreditEntry entry in credits)
        {
            if (entry == null) continue;

            string title = entry.GetTitle(activeLang);
            string subtitle = entry.GetSubtitle(activeLang);

            _titleText.text = title;
            _subtitleText.text = subtitle;
            _subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));

            yield return FadeText(0f, 1f, textFadeInDuration, 1.06f, 1.0f);
            yield return new WaitForSecondsRealtime(textHoldDuration);
            yield return FadeText(1f, 0f, textFadeOutDuration, 1.0f, 0.97f);
            yield return new WaitForSecondsRealtime(gapBetweenEntries);
        }

        yield return new WaitForSecondsRealtime(delayAfterCredits);

        // 6. Return to main menu
        if (returnToMainMenu)
        {
            IsGameFinished = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
                _canvas = null;
            }

#if UNITY_EDITOR
            // Deselect any selected object in the Unity Inspector before scene load
            // to prevent Unity's VolumeEditor from throwing a MissingReferenceException on destroyed Volumes
            UnityEditor.Selection.activeObject = null;
#endif

            if (!string.IsNullOrEmpty(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }
    }

    private IEnumerator FadeText(float fromAlpha, float toAlpha, float duration, float fromScale, float toScale)
    {
        RectTransform rt = _textGroup.GetComponent<RectTransform>();
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            _textGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, k);
            rt.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, k);
            yield return null;
        }
        _textGroup.alpha = toAlpha;
        rt.localScale = Vector3.one * toScale;
    }

    private void BuildCreditsUI()
    {
        // Canvas (top-most overlay)
        GameObject canvasGo = new GameObject("FinishCredits_Canvas");
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Black background
        GameObject bgGo = new GameObject("BlackScreen");
        bgGo.transform.SetParent(canvasGo.transform, false);
        _blackScreen = bgGo.AddComponent<Image>();
        _blackScreen.color = new Color(0f, 0f, 0f, 0f);
        _blackScreen.raycastTarget = false;
        StretchFull(bgGo.GetComponent<RectTransform>());

        // Text group (fades as a whole)
        GameObject groupGo = new GameObject("CreditsText", typeof(RectTransform));
        groupGo.transform.SetParent(canvasGo.transform, false);
        _textGroup = groupGo.AddComponent<CanvasGroup>();
        _textGroup.alpha = 0f;
        _textGroup.interactable = false;
        _textGroup.blocksRaycasts = false;
        RectTransform groupRt = groupGo.GetComponent<RectTransform>();
        groupRt.anchorMin = new Vector2(0.1f, 0.25f);
        groupRt.anchorMax = new Vector2(0.9f, 0.75f);
        groupRt.offsetMin = Vector2.zero;
        groupRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = groupGo.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 24f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleText = CreateText(groupGo.transform, "Title", titleFont, titleFontSize, titleColor);
        _subtitleText = CreateText(groupGo.transform, "Subtitle", subtitleFont != null ? subtitleFont : titleFont, subtitleFontSize, subtitleColor);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, TMP_FontAsset font, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (_triggered) IsGameFinished = false;
    }

    private void OnDrawGizmosSelected()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;
        Bounds b = c.bounds;
        b.Encapsulate(new Vector3(b.center.x, b.max.y + detectionHeightAbove, b.center.z));
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
