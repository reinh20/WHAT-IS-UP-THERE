#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class PauseMenuBuilder
{
    [MenuItem("Tools/Build Pause Menu")]
    public static void Build()
    {
        var hudCanvas = GameObject.Find("HUD_Canvas");
        if (hudCanvas == null)
        {
            Debug.LogError("HUD_Canvas not found in current scene!");
            return;
        }

        // Ensure EventSystem with InputSystemUIInputModule exists in scene
        var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }
        else if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            var oldModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null) Object.DestroyImmediate(oldModule);
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Load font assets
        var boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/asset/font/Salmon Typewriter fonts/SalmonTypewriter_Bold_FontAsset.asset");
        var regFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/asset/font/Salmon Typewriter fonts/SalmonTypewriter_Regular_FontAsset.asset");

        // Remove old PausePanel if present
        Transform existingPanel = hudCanvas.transform.Find("PausePanel");
        if (existingPanel != null)
        {
            Undo.DestroyObjectImmediate(existingPanel.gameObject);
        }

        // 1. Fullscreen dark backdrop
        GameObject pausePanel = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pausePanel.transform.SetParent(hudCanvas.transform, false);
        RectTransform panelRt = pausePanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        panelRt.anchoredPosition = Vector2.zero;
        Image panelBg = pausePanel.GetComponent<Image>();
        panelBg.color = new Color(0.03f, 0.05f, 0.08f, 0.88f);

        // 2. Centered Modal Card
        GameObject card = new GameObject("PauseCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(pausePanel.transform, false);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(460f, 520f);
        cardRt.anchoredPosition = Vector2.zero;
        Image cardBg = card.GetComponent<Image>();
        cardBg.color = new Color(0.08f, 0.11f, 0.16f, 0.96f);

        var cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.12f, 0.82f, 0.72f, 0.4f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // 3. Header Title
        GameObject headerObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerObj.transform.SetParent(card.transform, false);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(-40f, 45f);
        headerRt.anchoredPosition = new Vector2(0f, -24f);
        var headerTmp = headerObj.GetComponent<TextMeshProUGUI>();
        headerTmp.text = "PAUSED / SETTINGS";
        if (boldFont != null) headerTmp.font = boldFont;
        headerTmp.fontSize = 26f;
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.color = Color.white;

        // Header Accent Divider
        GameObject div1 = new GameObject("HeaderDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        div1.transform.SetParent(card.transform, false);
        RectTransform div1Rt = div1.GetComponent<RectTransform>();
        div1Rt.anchorMin = new Vector2(0.5f, 1f);
        div1Rt.anchorMax = new Vector2(0.5f, 1f);
        div1Rt.pivot = new Vector2(0.5f, 1f);
        div1Rt.sizeDelta = new Vector2(380f, 2f);
        div1Rt.anchoredPosition = new Vector2(0f, -74f);
        div1.GetComponent<Image>().color = new Color(0.12f, 0.82f, 0.72f, 0.85f);

        // 4. Audio Section
        GameObject audioLblObj = new GameObject("AudioLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        audioLblObj.transform.SetParent(card.transform, false);
        RectTransform audioLblRt = audioLblObj.GetComponent<RectTransform>();
        audioLblRt.anchorMin = new Vector2(0f, 1f);
        audioLblRt.anchorMax = new Vector2(1f, 1f);
        audioLblRt.pivot = new Vector2(0f, 1f);
        audioLblRt.sizeDelta = new Vector2(-70f, 26f);
        audioLblRt.anchoredPosition = new Vector2(38f, -92f);
        var audioLblTmp = audioLblObj.GetComponent<TextMeshProUGUI>();
        audioLblTmp.text = "Audio Volume";
        if (regFont != null) audioLblTmp.font = regFont;
        audioLblTmp.fontSize = 17f;
        audioLblTmp.color = new Color(0.8f, 0.86f, 0.92f, 1f);

        // Volume Slider
        GameObject sliderObj = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(card.transform, false);
        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0f, 1f);
        sliderRt.anchorMax = new Vector2(1f, 1f);
        sliderRt.pivot = new Vector2(0f, 1f);
        sliderRt.sizeDelta = new Vector2(-155f, 22f);
        sliderRt.anchoredPosition = new Vector2(38f, -122f);

        // Slider Background
        GameObject sBg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sBg.transform.SetParent(sliderObj.transform, false);
        RectTransform sBgRt = sBg.GetComponent<RectTransform>();
        sBgRt.anchorMin = Vector2.zero;
        sBgRt.anchorMax = Vector2.one;
        sBgRt.sizeDelta = Vector2.zero;
        sBg.GetComponent<Image>().color = new Color(0.16f, 0.20f, 0.26f, 1f);

        // Slider Fill Area
        GameObject sFillArea = new GameObject("Fill Area", typeof(RectTransform));
        sFillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform sFaRt = sFillArea.GetComponent<RectTransform>();
        sFaRt.anchorMin = Vector2.zero;
        sFaRt.anchorMax = Vector2.one;
        sFaRt.sizeDelta = new Vector2(-12f, 0f);
        sFaRt.anchoredPosition = Vector2.zero;

        GameObject sFill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sFill.transform.SetParent(sFillArea.transform, false);
        RectTransform sFillRt = sFill.GetComponent<RectTransform>();
        sFillRt.anchorMin = Vector2.zero;
        sFillRt.anchorMax = Vector2.one;
        sFillRt.sizeDelta = Vector2.zero;
        Image sFillImg = sFill.GetComponent<Image>();
        sFillImg.color = new Color(0.12f, 0.82f, 0.72f, 1f);

        // Slider Handle Slide Area
        GameObject sHandleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        sHandleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform sHaRt = sHandleArea.GetComponent<RectTransform>();
        sHaRt.anchorMin = Vector2.zero;
        sHaRt.anchorMax = Vector2.one;
        sHaRt.sizeDelta = new Vector2(-12f, 0f);

        GameObject sHandle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sHandle.transform.SetParent(sHandleArea.transform, false);
        RectTransform sHandleRt = sHandle.GetComponent<RectTransform>();
        sHandleRt.sizeDelta = new Vector2(16f, 22f);
        Image sHandleImg = sHandle.GetComponent<Image>();
        sHandleImg.color = Color.white;

        Slider sliderComp = sliderObj.GetComponent<Slider>();
        sliderComp.fillRect = sFillRt;
        sliderComp.handleRect = sHandleRt;
        sliderComp.targetGraphic = sHandleImg;
        sliderComp.direction = Slider.Direction.LeftToRight;
        sliderComp.minValue = 0f;
        sliderComp.maxValue = 1f;
        sliderComp.value = 0.75f;

        // Percent Text
        GameObject volPctObj = new GameObject("VolumePercent", typeof(RectTransform), typeof(TextMeshProUGUI));
        volPctObj.transform.SetParent(card.transform, false);
        RectTransform volPctRt = volPctObj.GetComponent<RectTransform>();
        volPctRt.anchorMin = new Vector2(1f, 1f);
        volPctRt.anchorMax = new Vector2(1f, 1f);
        volPctRt.pivot = new Vector2(1f, 1f);
        volPctRt.sizeDelta = new Vector2(65f, 26f);
        volPctRt.anchoredPosition = new Vector2(-38f, -121f);
        var volPctTmp = volPctObj.GetComponent<TextMeshProUGUI>();
        volPctTmp.text = "75%";
        if (boldFont != null) volPctTmp.font = boldFont;
        volPctTmp.fontSize = 17f;
        volPctTmp.alignment = TextAlignmentOptions.Right;
        volPctTmp.color = new Color(0.12f, 0.82f, 0.72f, 1f);

        // 5. Language Section
        GameObject langLblObj = new GameObject("LanguageLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        langLblObj.transform.SetParent(card.transform, false);
        RectTransform langLblRt = langLblObj.GetComponent<RectTransform>();
        langLblRt.anchorMin = new Vector2(0f, 1f);
        langLblRt.anchorMax = new Vector2(1f, 1f);
        langLblRt.pivot = new Vector2(0f, 1f);
        langLblRt.sizeDelta = new Vector2(-70f, 26f);
        langLblRt.anchoredPosition = new Vector2(38f, -165f);
        var langLblTmp = langLblObj.GetComponent<TextMeshProUGUI>();
        langLblTmp.text = "Language";
        if (regFont != null) langLblTmp.font = regFont;
        langLblTmp.fontSize = 17f;
        langLblTmp.color = new Color(0.8f, 0.86f, 0.92f, 1f);

        // English Button
        GameObject enBtnObj = new GameObject("EnglishBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        enBtnObj.transform.SetParent(card.transform, false);
        RectTransform enRt = enBtnObj.GetComponent<RectTransform>();
        enRt.anchorMin = new Vector2(0.5f, 1f);
        enRt.anchorMax = new Vector2(0.5f, 1f);
        enRt.pivot = new Vector2(1f, 1f);
        enRt.sizeDelta = new Vector2(188f, 38f);
        enRt.anchoredPosition = new Vector2(-5f, -195f);
        Image enImg = enBtnObj.GetComponent<Image>();
        enImg.color = new Color(0.12f, 0.82f, 0.72f, 1f);

        GameObject enTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        enTxtObj.transform.SetParent(enBtnObj.transform, false);
        RectTransform enTxtRt = enTxtObj.GetComponent<RectTransform>();
        enTxtRt.anchorMin = Vector2.zero;
        enTxtRt.anchorMax = Vector2.one;
        enTxtRt.sizeDelta = Vector2.zero;
        var enTmp = enTxtObj.GetComponent<TextMeshProUGUI>();
        enTmp.text = "English";
        if (boldFont != null) enTmp.font = boldFont;
        enTmp.fontSize = 16f;
        enTmp.alignment = TextAlignmentOptions.Center;
        enTmp.color = new Color(0.02f, 0.08f, 0.1f, 1f);

        // Indonesian Button
        GameObject idBtnObj = new GameObject("IndonesianBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        idBtnObj.transform.SetParent(card.transform, false);
        RectTransform idRt = idBtnObj.GetComponent<RectTransform>();
        idRt.anchorMin = new Vector2(0.5f, 1f);
        idRt.anchorMax = new Vector2(0.5f, 1f);
        idRt.pivot = new Vector2(0f, 1f);
        idRt.sizeDelta = new Vector2(188f, 38f);
        idRt.anchoredPosition = new Vector2(5f, -195f);
        Image idImg = idBtnObj.GetComponent<Image>();
        idImg.color = new Color(0.18f, 0.22f, 0.28f, 0.75f);

        GameObject idTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        idTxtObj.transform.SetParent(idBtnObj.transform, false);
        RectTransform idTxtRt = idTxtObj.GetComponent<RectTransform>();
        idTxtRt.anchorMin = Vector2.zero;
        idTxtRt.anchorMax = Vector2.one;
        idTxtRt.sizeDelta = Vector2.zero;
        var idTmp = idTxtObj.GetComponent<TextMeshProUGUI>();
        idTmp.text = "Indonesia";
        if (boldFont != null) idTmp.font = boldFont;
        idTmp.fontSize = 16f;
        idTmp.alignment = TextAlignmentOptions.Center;
        idTmp.color = Color.white;

        // Divider 2
        GameObject div2 = new GameObject("Divider2", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        div2.transform.SetParent(card.transform, false);
        RectTransform div2Rt = div2.GetComponent<RectTransform>();
        div2Rt.anchorMin = new Vector2(0.5f, 1f);
        div2Rt.anchorMax = new Vector2(0.5f, 1f);
        div2Rt.pivot = new Vector2(0.5f, 1f);
        div2Rt.sizeDelta = new Vector2(380f, 1f);
        div2Rt.anchoredPosition = new Vector2(0f, -250f);
        div2.GetComponent<Image>().color = new Color(0.22f, 0.28f, 0.36f, 0.6f);

        // 6. Action Navigation Buttons
        System.Func<string, float, Color, GameObject> createActionButton = (name, yPos, bgColor) => {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(card.transform, false);
            RectTransform bRt = btnObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 1f);
            bRt.anchorMax = new Vector2(0.5f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f);
            bRt.sizeDelta = new Vector2(380f, 46f);
            bRt.anchoredPosition = new Vector2(0f, yPos);

            Image bImg = btnObj.GetComponent<Image>();
            bImg.color = bgColor;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.2f);
            cb.selectedColor = cb.normalColor;
            btn.colors = cb;

            GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRt = tObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;
            var tmp = tObj.GetComponent<TextMeshProUGUI>();
            if (boldFont != null) tmp.font = boldFont;
            tmp.fontSize = 17f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        };

        // Resume Button
        GameObject resumeBtnObj = createActionButton("ResumeBtn", -272f, new Color(0.12f, 0.68f, 0.60f, 1f));
        var resumeTmp = resumeBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        resumeTmp.text = "Resume [P]";

        // Restart Checkpoint Button
        GameObject restartBtnObj = createActionButton("RestartBtn", -330f, new Color(0.18f, 0.23f, 0.30f, 0.95f));
        var restartTmp = restartBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        restartTmp.text = "Restart Checkpoint";

        // Main Menu Button
        GameObject menuBtnObj = createActionButton("MainMenuBtn", -388f, new Color(0.24f, 0.16f, 0.20f, 0.95f));
        var menuTmp = menuBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        menuTmp.text = "Main Menu";

        // 7. Attach and wire PauseManager component
        var pauseMgr = hudCanvas.GetComponent<PauseManager>();
        if (pauseMgr == null) pauseMgr = hudCanvas.AddComponent<PauseManager>();

        pauseMgr.pausePanel = pausePanel;
        pauseMgr.volumeSlider = sliderComp;
        pauseMgr.volumePercentText = volPctTmp;

        pauseMgr.englishButton = enBtnObj.GetComponent<Button>();
        pauseMgr.indonesianButton = idBtnObj.GetComponent<Button>();
        pauseMgr.englishBtnBg = enImg;
        pauseMgr.indonesianBtnBg = idImg;

        pauseMgr.resumeButton = resumeBtnObj.GetComponent<Button>();
        pauseMgr.restartCheckpointButton = restartBtnObj.GetComponent<Button>();
        pauseMgr.mainMenuButton = menuBtnObj.GetComponent<Button>();

        pauseMgr.pauseHeaderText = headerTmp;
        pauseMgr.audioLabelText = audioLblTmp;
        pauseMgr.languageLabelText = langLblTmp;
        pauseMgr.resumeBtnText = resumeTmp;
        pauseMgr.restartBtnText = restartTmp;
        pauseMgr.mainMenuBtnText = menuTmp;
        pauseMgr.englishBtnText = enTmp;
        pauseMgr.indonesianBtnText = idTmp;

        // Hide initially
        pausePanel.SetActive(false);

        EditorUtility.SetDirty(hudCanvas);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[PauseMenuBuilder] Pause & Settings menu built and wired successfully!");
    }
}
#endif
