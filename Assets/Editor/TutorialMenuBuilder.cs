#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class TutorialMenuBuilder
{
    [MenuItem("Tools/Build Tutorial Menu")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().name != "main menu")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/main menu.unity");
        }

        var menuCanvas = GameObject.Find("MenuCanvas");
        if (menuCanvas == null)
        {
            Debug.LogError("MenuCanvas not found in main menu scene!");
            return;
        }

        var mmm = menuCanvas.GetComponent<MainMenuManager>();
        if (mmm == null)
        {
            Debug.LogError("MainMenuManager not found on MenuCanvas!");
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

        // Remove old TutorialPanel if present
        Transform existingTutorial = menuCanvas.transform.Find("TutorialPanel");
        if (existingTutorial != null)
        {
            Undo.DestroyObjectImmediate(existingTutorial.gameObject);
        }

        // 1. Fullscreen dark backdrop
        GameObject tutorialPanel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tutorialPanel.transform.SetParent(menuCanvas.transform, false);
        RectTransform panelRt = tutorialPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        panelRt.anchoredPosition = Vector2.zero;
        Image panelBg = tutorialPanel.GetComponent<Image>();
        panelBg.color = new Color(0.02f, 0.04f, 0.07f, 0.90f);
        panelBg.raycastTarget = true; // Blocks clicks behind the tutorial

        // 2. Centered Modal Card (980 x 750)
        GameObject card = new GameObject("TutorialCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(tutorialPanel.transform, false);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(980f, 750f);
        cardRt.anchoredPosition = Vector2.zero;
        Image cardBg = card.GetComponent<Image>();
        cardBg.color = new Color(0.06f, 0.09f, 0.14f, 0.98f);
        cardBg.raycastTarget = false;

        var cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.12f, 0.82f, 0.72f, 0.45f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // 3. Header Title
        GameObject headerObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerObj.transform.SetParent(card.transform, false);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(-40f, 38f);
        headerRt.anchoredPosition = new Vector2(0f, -22f);
        var headerTmp = headerObj.GetComponent<TextMeshProUGUI>();
        headerTmp.text = "PANDUAN KONTROL & CARA BERMAIN";
        if (boldFont != null) headerTmp.font = boldFont;
        headerTmp.fontSize = 26f;
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.color = Color.white;
        headerTmp.raycastTarget = false;

        // Header Accent Divider
        GameObject div1 = new GameObject("HeaderDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        div1.transform.SetParent(card.transform, false);
        RectTransform div1Rt = div1.GetComponent<RectTransform>();
        div1Rt.anchorMin = new Vector2(0.5f, 1f);
        div1Rt.anchorMax = new Vector2(0.5f, 1f);
        div1Rt.pivot = new Vector2(0.5f, 1f);
        div1Rt.sizeDelta = new Vector2(900f, 2f);
        div1Rt.anchoredPosition = new Vector2(0f, -66f);
        var div1Img = div1.GetComponent<Image>();
        div1Img.color = new Color(0.12f, 0.82f, 0.72f, 0.85f);
        div1Img.raycastTarget = false;

        // Subheader
        GameObject subheaderObj = new GameObject("Subheader", typeof(RectTransform), typeof(TextMeshProUGUI));
        subheaderObj.transform.SetParent(card.transform, false);
        RectTransform subheaderRt = subheaderObj.GetComponent<RectTransform>();
        subheaderRt.anchorMin = new Vector2(0f, 1f);
        subheaderRt.anchorMax = new Vector2(1f, 1f);
        subheaderRt.pivot = new Vector2(0.5f, 1f);
        subheaderRt.sizeDelta = new Vector2(-60f, 24f);
        subheaderRt.anchoredPosition = new Vector2(0f, -74f);
        var subheaderTmp = subheaderObj.GetComponent<TextMeshProUGUI>();
        subheaderTmp.text = "Kuasai manuver parkour dan grappling untuk bertahan hidup!";
        if (regFont != null) subheaderTmp.font = regFont;
        subheaderTmp.fontSize = 14.5f;
        subheaderTmp.alignment = TextAlignmentOptions.Center;
        subheaderTmp.color = new Color(0.72f, 0.82f, 0.90f, 0.9f);
        subheaderTmp.raycastTarget = false;

        // 4. Control Item Rows (2 Columns x 3 Rows)
        // Left column: X center = -235, Right column: X center = 235
        // Row 1: Y = -108, Row 2: Y = -220, Row 3: Y = -332 (Height 102 each, Width 435)

        System.Func<string, string, string, Vector2, (TextMeshProUGUI badge, TextMeshProUGUI title, TextMeshProUGUI desc)> createControlCard = (name, badgeDefault, defaultDesc, pos) => {
            GameObject itemCard = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            itemCard.transform.SetParent(card.transform, false);
            RectTransform iRt = itemCard.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.5f, 1f);
            iRt.anchorMax = new Vector2(0.5f, 1f);
            iRt.pivot = new Vector2(0.5f, 1f);
            iRt.sizeDelta = new Vector2(435f, 102f);
            iRt.anchoredPosition = pos;
            Image iBg = itemCard.GetComponent<Image>();
            iBg.color = new Color(0.10f, 0.14f, 0.20f, 0.95f);
            iBg.raycastTarget = false;

            var outline = itemCard.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.26f, 0.36f, 0.5f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Key Text (clean without green background box)
            GameObject badgeObj = new GameObject("KeyBadge", typeof(RectTransform));
            badgeObj.transform.SetParent(itemCard.transform, false);
            RectTransform bRt = badgeObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 1f);
            bRt.anchorMax = new Vector2(0f, 1f);
            bRt.pivot = new Vector2(0f, 1f);
            bRt.sizeDelta = new Vector2(175f, 30f);
            bRt.anchoredPosition = new Vector2(12f, -10f);

            GameObject bTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bTxtObj.transform.SetParent(badgeObj.transform, false);
            RectTransform bTxtRt = bTxtObj.GetComponent<RectTransform>();
            bTxtRt.anchorMin = Vector2.zero;
            bTxtRt.anchorMax = Vector2.one;
            bTxtRt.sizeDelta = Vector2.zero;
            var bTmp = bTxtObj.GetComponent<TextMeshProUGUI>();
            bTmp.text = badgeDefault;
            if (boldFont != null) bTmp.font = boldFont;
            bTmp.fontSize = 14f;
            bTmp.textWrappingMode = TextWrappingModes.NoWrap;
            bTmp.alignment = TextAlignmentOptions.Left;
            bTmp.color = new Color(0.18f, 0.90f, 0.80f, 1f);
            bTmp.raycastTarget = false;

            // Action Title (Width 235)
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(itemCard.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0f, 1f);
            tRt.sizeDelta = new Vector2(-200f, 30f);
            tRt.anchoredPosition = new Vector2(192f, -10f);
            var tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = name;
            if (boldFont != null) tTmp.font = boldFont;
            tTmp.fontSize = 15f;
            tTmp.textWrappingMode = TextWrappingModes.NoWrap;
            tTmp.color = Color.white;
            tTmp.alignment = TextAlignmentOptions.Left;
            tTmp.raycastTarget = false;

            // Action Description (Width 410)
            GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(itemCard.transform, false);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 1f);
            dRt.anchorMax = new Vector2(1f, 1f);
            dRt.pivot = new Vector2(0f, 1f);
            dRt.sizeDelta = new Vector2(-24f, 50f);
            dRt.anchoredPosition = new Vector2(12f, -46f);
            var dTmp = descObj.GetComponent<TextMeshProUGUI>();
            dTmp.text = defaultDesc;
            if (regFont != null) dTmp.font = regFont;
            dTmp.fontSize = 13f;
            dTmp.lineSpacing = 3f;
            dTmp.color = new Color(0.78f, 0.84f, 0.90f, 0.95f);
            dTmp.raycastTarget = false;

            return (bTmp, tTmp, dTmp);
        };

        // Left Column (Controls 1 - 3)
        var moveCard = createControlCard("Gerakan Dasar", "[ W A S D ]", "Jalan & Berlari mengelilingi level", new Vector2(-235f, -108f));
        var jumpCard = createControlCard("Lompat & Bhop", "[ SPASI ]", "Lompat berantai untuk mengakumulasi kecepatan", new Vector2(-235f, -220f));
        var slideCard = createControlCard("Meluncur (Slide)", "[ CTRL / SHIFT ]", "Meluncur saat bergerak atau di turunan untuk dorongan akselerasi", new Vector2(-235f, -332f));

        // Right Column (Controls 4 - 6)
        var grappleCard = createControlCard("Grappling Hook", "[ KLIK KIRI ]", "Tembak ke permukaan untuk berayun dan melontar tinggi", new Vector2(235f, -108f));
        var wallRunCard = createControlCard("Wall Run", "[ W + TEMBOK ]", "Lompat mendekati dinding dan tahan W untuk berlari di tembok", new Vector2(235f, -108f + -112f));
        var pauseCard = createControlCard("Menu Jeda & Seting", "[ P / ESC ]", "Buka pengaturan audio dan ganti bahasa di tengah game", new Vector2(235f, -108f + -224f));

        // 5. Objective Warning Banner (Full width across bottom of grid)
        GameObject objBanner = new GameObject("ObjectiveBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objBanner.transform.SetParent(card.transform, false);
        RectTransform obRt = objBanner.GetComponent<RectTransform>();
        obRt.anchorMin = new Vector2(0.5f, 1f);
        obRt.anchorMax = new Vector2(0.5f, 1f);
        obRt.pivot = new Vector2(0.5f, 1f);
        obRt.sizeDelta = new Vector2(900f, 85f);
        obRt.anchoredPosition = new Vector2(0f, -450f);
        Image obImg = objBanner.GetComponent<Image>();
        obImg.color = new Color(0.24f, 0.08f, 0.09f, 0.92f);
        obImg.raycastTarget = false;

        var obOutline = objBanner.AddComponent<Outline>();
        obOutline.effectColor = new Color(0.85f, 0.25f, 0.22f, 0.7f);
        obOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject obTitleObj = new GameObject("ObjTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        obTitleObj.transform.SetParent(objBanner.transform, false);
        RectTransform obtRt = obTitleObj.GetComponent<RectTransform>();
        obtRt.anchorMin = new Vector2(0f, 1f);
        obtRt.anchorMax = new Vector2(1f, 1f);
        obtRt.pivot = new Vector2(0f, 1f);
        obtRt.sizeDelta = new Vector2(-28f, 26f);
        obtRt.anchoredPosition = new Vector2(14f, -8f);
        var obtTmp = obTitleObj.GetComponent<TextMeshProUGUI>();
        obtTmp.text = "TUJUAN UTAMA:";
        if (boldFont != null) obtTmp.font = boldFont;
        obtTmp.fontSize = 15f;
        obtTmp.color = new Color(1f, 0.35f, 0.30f, 1f);
        obtTmp.raycastTarget = false;

        GameObject obDescObj = new GameObject("ObjDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
        obDescObj.transform.SetParent(objBanner.transform, false);
        RectTransform obdRt = obDescObj.GetComponent<RectTransform>();
        obdRt.anchorMin = new Vector2(0f, 1f);
        obdRt.anchorMax = new Vector2(1f, 1f);
        obdRt.pivot = new Vector2(0f, 1f);
        obdRt.sizeDelta = new Vector2(-28f, 44f);
        obdRt.anchoredPosition = new Vector2(14f, -32f);
        var obdTmp = obDescObj.GetComponent<TextMeshProUGUI>();
        obdTmp.text = "WASPADA: Pumpkin Merah akan mengejarmu! Jangan biarkan ia menangkapmu sebelum mencapai Checkpoint Akhir!";
        if (regFont != null) obdTmp.font = regFont;
        obdTmp.fontSize = 13.5f;
        obdTmp.lineSpacing = 3f;
        obdTmp.color = new Color(0.95f, 0.88f, 0.88f, 0.95f);
        obdTmp.raycastTarget = false;

        // Bottom Divider
        GameObject div2 = new GameObject("BottomDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        div2.transform.SetParent(card.transform, false);
        RectTransform div2Rt = div2.GetComponent<RectTransform>();
        div2Rt.anchorMin = new Vector2(0.5f, 1f);
        div2Rt.anchorMax = new Vector2(0.5f, 1f);
        div2Rt.pivot = new Vector2(0.5f, 1f);
        div2Rt.sizeDelta = new Vector2(900f, 1f);
        div2Rt.anchoredPosition = new Vector2(0f, -550f);
        div2.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.32f, 0.6f);
        div2.GetComponent<Image>().raycastTarget = false;

        // 6. Action Navigation Buttons
        // Back to Main Menu Button
        GameObject backBtnObj = new GameObject("TutorialBackBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(card.transform, false);
        RectTransform backRt = backBtnObj.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 1f);
        backRt.anchorMax = new Vector2(0.5f, 1f);
        backRt.pivot = new Vector2(0.5f, 1f);
        backRt.sizeDelta = new Vector2(240f, 52f);
        backRt.anchoredPosition = new Vector2(-280f, -572f);

        Image backImg = backBtnObj.GetComponent<Image>();
        backImg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
        backImg.raycastTarget = true;

        Button backBtn = backBtnObj.GetComponent<Button>();
        ColorBlock bcb = backBtn.colors;
        bcb.normalColor = new Color(0.18f, 0.22f, 0.28f, 0.95f);
        bcb.highlightedColor = new Color(0.28f, 0.34f, 0.42f, 1f);
        bcb.pressedColor = new Color(0.12f, 0.15f, 0.20f, 1f);
        bcb.selectedColor = bcb.normalColor;
        backBtn.colors = bcb;

        GameObject backTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        backTxtObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform btRt = backTxtObj.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.sizeDelta = Vector2.zero;
        var backTmp = backTxtObj.GetComponent<TextMeshProUGUI>();
        backTmp.text = "Kembali";
        if (boldFont != null) backTmp.font = boldFont;
        backTmp.fontSize = 17f;
        backTmp.alignment = TextAlignmentOptions.Center;
        backTmp.color = Color.white;
        backTmp.raycastTarget = false;

        // Add persistent listener for Back Button
        UnityEventTools.AddPersistentListener(backBtn.onClick, mmm.ShowMainPanel);

        // Start / Skip Button
        GameObject startBtnObj = new GameObject("StartGameBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        startBtnObj.transform.SetParent(card.transform, false);
        RectTransform startRt = startBtnObj.GetComponent<RectTransform>();
        startRt.anchorMin = new Vector2(0.5f, 1f);
        startRt.anchorMax = new Vector2(0.5f, 1f);
        startRt.pivot = new Vector2(0.5f, 1f);
        startRt.sizeDelta = new Vector2(520f, 52f);
        startRt.anchoredPosition = new Vector2(130f, -572f);

        Image startImg = startBtnObj.GetComponent<Image>();
        startImg.color = new Color(0.12f, 0.72f, 0.62f, 1f);
        startImg.raycastTarget = true;

        Button startBtn = startBtnObj.GetComponent<Button>();
        ColorBlock scb = startBtn.colors;
        scb.normalColor = new Color(0.12f, 0.72f, 0.62f, 1f);
        scb.highlightedColor = new Color(0.20f, 0.88f, 0.76f, 1f);
        scb.pressedColor = new Color(0.08f, 0.52f, 0.44f, 1f);
        scb.selectedColor = scb.normalColor;
        startBtn.colors = scb;

        GameObject startTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        startTxtObj.transform.SetParent(startBtnObj.transform, false);
        RectTransform stRt = startTxtObj.GetComponent<RectTransform>();
        stRt.anchorMin = Vector2.zero;
        stRt.anchorMax = Vector2.one;
        stRt.sizeDelta = Vector2.zero;
        var startTmp = startTxtObj.GetComponent<TextMeshProUGUI>();
        startTmp.text = "LEWATI & MASUK LV 1 [SPASI / ENTER]";
        if (boldFont != null) startTmp.font = boldFont;
        startTmp.fontSize = 16f;
        startTmp.textWrappingMode = TextWrappingModes.NoWrap;
        startTmp.alignment = TextAlignmentOptions.Center;
        startTmp.color = Color.white;
        startTmp.raycastTarget = false;

        // Add persistent listener for Start Button
        UnityEventTools.AddPersistentListener(startBtn.onClick, mmm.PlayGame);

        // 7. Update MainPanel PLAY_Btn to open TutorialPanel instead of immediately loading scene
        var playBtn = menuCanvas.transform.Find("MainPanel/ButtonContainer/PLAY_Btn")?.GetComponent<Button>();
        if (playBtn != null)
        {
            for (int i = playBtn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(playBtn.onClick, i);
            }
            UnityEventTools.AddPersistentListener(playBtn.onClick, mmm.ShowTutorialPanel);
        }

        // 8. Wire MainMenuManager references
        mmm.tutorialPanel = tutorialPanel;
        mmm.tutorialHeaderText = headerTmp;
        mmm.tutorialSubheaderText = subheaderTmp;

        mmm.moveBadgeText = moveCard.badge;
        mmm.moveLabelText = moveCard.title;
        mmm.moveDescText = moveCard.desc;

        mmm.jumpBadgeText = jumpCard.badge;
        mmm.jumpLabelText = jumpCard.title;
        mmm.jumpDescText = jumpCard.desc;

        mmm.slideBadgeText = slideCard.badge;
        mmm.slideLabelText = slideCard.title;
        mmm.slideDescText = slideCard.desc;

        mmm.grappleBadgeText = grappleCard.badge;
        mmm.grappleLabelText = grappleCard.title;
        mmm.grappleDescText = grappleCard.desc;

        mmm.wallRunBadgeText = wallRunCard.badge;
        mmm.wallRunLabelText = wallRunCard.title;
        mmm.wallRunDescText = wallRunCard.desc;

        mmm.pauseBadgeText = pauseCard.badge;
        mmm.pauseLabelText = pauseCard.title;
        mmm.pauseDescText = pauseCard.desc;

        mmm.objectiveTitleText = obtTmp;
        mmm.objectiveDescText = obdTmp;

        mmm.startLevelBtnText = startTmp;
        mmm.tutorialBackBtnText = backTmp;

        mmm.startLevelButton = startBtn;
        mmm.tutorialBackButton = backBtn;

        // Hide tutorial panel initially
        tutorialPanel.SetActive(false);

        EditorUtility.SetDirty(menuCanvas);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[TutorialMenuBuilder] Tutorial Menu rebuilt with persistent listeners and polished layout successfully!");
    }
}
#endif
