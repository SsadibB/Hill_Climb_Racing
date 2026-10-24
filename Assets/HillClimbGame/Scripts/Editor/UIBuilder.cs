using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;

namespace HillClimbGame
{
    public static class UIBuilder
    {
        [MenuItem("HillClimbGame/1 - Build UI Canvas")]
        public static void BuildUI()
        {
            var uiRoot = GameObject.Find("UI");
            if (uiRoot == null)
            {
                uiRoot = new GameObject("UI");
            }

            // Ensure EventSystem exists
            SetupEventSystem(uiRoot.transform);

            // Remove existing MainCanvas if present
            var existing = uiRoot.transform.Find("MainCanvas");
            if (existing != null) GameObject.DestroyImmediate(existing.gameObject);

            var mainCanvasGo = new GameObject("MainCanvas");
            mainCanvasGo.transform.SetParent(uiRoot.transform, false);

            var canvas = mainCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 2f;
            canvas.sortingOrder = 100;

            var scaler = mainCanvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            mainCanvasGo.AddComponent<GraphicRaycaster>();

            // 1. MAIN MENU PANEL
            BuildMainMenu(mainCanvasGo.transform);

            // 2. LOADING PANEL
            BuildLoadingScreen(mainCanvasGo.transform);

            // 3. GAMEPLAY HUD
            BuildGameplayHUD(mainCanvasGo.transform);

            // 4. PAUSE PANEL
            BuildPauseMenu(mainCanvasGo.transform);

            // 5. HOW TO PLAY PANEL
            BuildHowToPlay(mainCanvasGo.transform);

            // 6. GAME OVER PANEL
            BuildGameOver(mainCanvasGo.transform);

            EditorUtility.SetDirty(mainCanvasGo);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mainCanvasGo.scene);
            Debug.Log("[HillClimbGame] UI Canvas built successfully with responsive, aesthetic layout!");
        }

        private static void SetupEventSystem(Transform parent)
        {
            var esGo = GameObject.Find("EventSystem");
            if (esGo == null)
            {
                esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(parent, false);
            }

            var es = esGo.GetComponent<EventSystem>();
            if (es == null) es = esGo.AddComponent<EventSystem>();

            // Use InputSystemUIInputModule for new Input System
            var oldModule = esGo.GetComponent<StandaloneInputModule>();
            if (oldModule != null) GameObject.DestroyImmediate(oldModule);

            var inputModule = esGo.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null) esGo.AddComponent<InputSystemUIInputModule>();

            EditorUtility.SetDirty(esGo);
        }

        private static void BuildMainMenu(Transform root)
        {
            var panel = MakePanel("MainMenuPanel", root);
            var cg = panel.GetComponent<CanvasGroup>();
            cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
            panel.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.22f, 0.90f);

            // Title banner
            MakeText("TitleText", panel.transform, "HILL CLIMB RACING", 64, new Color(1f, 0.82f, 0.15f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(900, 90), FontStyle.Bold);

            MakeText("SubtitleText", panel.transform, "ENDLESS OFF-ROAD ADVENTURE", 24, new Color(0.85f, 0.9f, 1f, 0.9f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(700, 40), FontStyle.Normal);

            // Buttons
            MakeButton("PlayButton", panel.transform, "PLAY GAME", new Color(0.18f, 0.72f, 0.25f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(360, 75), 32);

            MakeButton("HowToPlayButton", panel.transform, "HOW TO PLAY", new Color(0.20f, 0.48f, 0.85f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -65), new Vector2(360, 65), 26);

            MakeButton("QuitButton", panel.transform, "QUIT", new Color(0.82f, 0.22f, 0.20f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(360, 65), 26);

            // Bottom stats bar
            var statsBar = MakeImageRect("StatsBar", panel.transform, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(600, 50));
            statsBar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            MakeText("BestDistanceText", statsBar.transform, "BEST: 0 m", 22, Color.white,
                new Vector2(0.3f, 0.5f), Vector2.zero, new Vector2(250, 40), FontStyle.Bold);

            MakeText("TotalCoinsText", statsBar.transform, "COINS: 0", 22, new Color(1f, 0.88f, 0.2f),
                new Vector2(0.7f, 0.5f), Vector2.zero, new Vector2(250, 40), FontStyle.Bold);
        }

        private static void BuildLoadingScreen(Transform root)
        {
            var panel = MakePanel("LoadingPanel", root);
            panel.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.22f, 0.96f);

            MakeText("LoadingTitle", panel.transform, "GETTING READY...", 44, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(600, 60), FontStyle.Bold);

            // Bar background
            var barBg = MakeImageRect("LoadingBarBackground", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(600, 40));
            barBg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f);

            // Bar fill
            var barFill = MakeImageRect("LoadingBarFill", barBg.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(592, 32));
            var fillImg = barFill.GetComponent<Image>();
            fillImg.color = new Color(0.2f, 0.85f, 0.35f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            MakeText("LoadingPercentageText", panel.transform, "0%", 30, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0, -45), new Vector2(200, 40), FontStyle.Bold);

            MakeText("LoadingTipText", panel.transform, "Tip: Accelerate before steep hills to gain momentum!", 22,
                new Color(0.8f, 0.9f, 1f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0, -130), new Vector2(800, 50), FontStyle.Normal);
        }

        private static void BuildGameplayHUD(Transform root)
        {
            var panel = MakePanel("GameplayHUD", root);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            // Top-Left Coins
            var coinCard = MakeImageRect("CoinCard", panel.transform, new Vector2(0f, 1f), new Vector2(140, -50), new Vector2(180, 50));
            coinCard.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            MakeText("CoinIconLabel", coinCard.transform, "$", 26, new Color(1f, 0.88f, 0.2f),
                new Vector2(0.2f, 0.5f), Vector2.zero, new Vector2(40, 40), FontStyle.Bold);
            MakeText("CoinCounter", coinCard.transform, "0", 24, new Color(1f, 0.95f, 0.4f),
                new Vector2(0.65f, 0.5f), Vector2.zero, new Vector2(110, 40), FontStyle.Bold);

            // Top-Center Distance
            var distCard = MakeImageRect("DistanceCard", panel.transform, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(240, 50));
            distCard.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            MakeText("DistanceText", distCard.transform, "0 m", 26, Color.white,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 40), FontStyle.Bold);

            // Top-Right Pause Button
            MakeButton("PauseButton", panel.transform, "II", new Color(0.2f, 0.2f, 0.25f, 0.8f),
                new Vector2(1f, 1f), new Vector2(-60, -50), new Vector2(55, 50), 22);

            // Bottom-Center Speed & Fuel
            var hudBottom = MakeImageRect("BottomHUDCard", panel.transform, new Vector2(0.5f, 0f), new Vector2(0, 75), new Vector2(460, 95));
            hudBottom.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.60f);

            MakeText("SpeedText", hudBottom.transform, "0 km/h", 24, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(200, 35), FontStyle.Bold);

            // Fuel Bar Background
            var fuelBg = MakeImageRect("FuelBarBackground", hudBottom.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(400, 26));
            fuelBg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f);

            var fuelFill = MakeImageRect("FuelBarFill", fuelBg.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(394, 20));
            var fillImg = fuelFill.GetComponent<Image>();
            fillImg.color = new Color(0.15f, 0.85f, 0.35f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            MakeText("FuelPercentageText", hudBottom.transform, "100%", 18, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(100, 24), FontStyle.Bold);

            // Mobile Gas and Brake pedals
            var brakeBtn = MakeButton("BrakePedal", panel.transform, "BRAKE", new Color(0.78f, 0.20f, 0.18f, 0.85f),
                new Vector2(0f, 0f), new Vector2(130, 95), new Vector2(170, 105), 26);
            var gasBtn = MakeButton("GasPedal", panel.transform, "GAS", new Color(0.18f, 0.72f, 0.22f, 0.85f),
                new Vector2(1f, 0f), new Vector2(-130, 95), new Vector2(170, 105), 28);

            // Attach persistent PedalButton runtime components
            var brakePedal = brakeBtn.AddComponent<PedalButton>();
            brakePedal.isGas = false;

            var gasPedal = gasBtn.AddComponent<PedalButton>();
            gasPedal.isGas = true;
        }

        private static void BuildPauseMenu(Transform root)
        {
            var panel = MakePanel("PausePanel", root);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.85f);

            var dialog = MakeImageRect("PauseDialog", panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 380));
            dialog.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.24f, 0.98f);

            MakeText("PauseTitle", dialog.transform, "GAME PAUSED", 44, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(400, 60), FontStyle.Bold);

            MakeButton("ResumeButton", dialog.transform, "RESUME", new Color(0.18f, 0.72f, 0.25f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(320, 65), 28);

            MakeButton("MenuButton", dialog.transform, "MAIN MENU", new Color(0.70f, 0.35f, 0.12f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -75), new Vector2(320, 65), 26);
        }

        private static void BuildHowToPlay(Transform root)
        {
            var panel = MakePanel("HowToPlayPanel", root);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.88f);

            var dialog = MakeImageRect("HowToDialog", panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660, 500));
            dialog.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.28f, 0.98f);

            MakeText("HowToTitle", dialog.transform, "HOW TO PLAY", 40, new Color(1f, 0.85f, 0.2f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(500, 60), FontStyle.Bold);

            string body = "<b>[D] / [Right Arrow] / [GAS]</b>: Accelerate forward\n\n" +
                          "<b>[A] / [Left Arrow] / [BRAKE]</b>: Brake and tilt backward\n\n" +
                          "<b>[ESC]</b>: Pause the game\n\n" +
                          "- Collect <b>COINS</b> to increase your high score\n" +
                          "- Grab <b>FUEL CANS</b> before your tank runs empty\n" +
                          "- Watch out for changing weather and steep hills!";

            MakeText("HowToBody", dialog.transform, body, 22, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(580, 240), FontStyle.Normal);

            MakeButton("HowToCloseBtn", dialog.transform, "GOT IT!", new Color(0.82f, 0.24f, 0.20f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(260, 60), 26);
        }

        private static void BuildGameOver(Transform root)
        {
            var panel = MakePanel("GameOverPanel", root);
            panel.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.10f, 0.94f);

            var dialog = MakeImageRect("GameOverDialog", panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 540));
            dialog.GetComponent<Image>().color = new Color(0.16f, 0.14f, 0.25f, 0.98f);

            MakeText("GameOverTitle", dialog.transform, "GAME OVER", 52, new Color(1f, 0.78f, 0.15f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(460, 65), FontStyle.Bold);

            MakeText("OutOfFuelLabel", dialog.transform, "OUT OF FUEL", 26, new Color(1f, 0.40f, 0.35f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(400, 40), FontStyle.Bold);

            // Stats Card
            var statsBox = MakeImageRect("StatsBox", dialog.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 25), new Vector2(420, 150));
            statsBox.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            MakeText("DistLabel", statsBox.transform, "DISTANCE TRAVELED:", 20, new Color(0.8f, 0.85f, 0.9f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 45), new Vector2(380, 30), FontStyle.Normal);
            MakeText("FinalDistanceText", statsBox.transform, "0 m", 32, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(380, 40), FontStyle.Bold);

            MakeText("RunCoinsText", statsBox.transform, "Coins Collected: 0", 22, new Color(1f, 0.88f, 0.2f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(380, 30), FontStyle.Bold);

            MakeText("GameOverBestText", statsBox.transform, "All-Time Best: 0 m", 20, new Color(0.7f, 0.9f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(380, 30), FontStyle.Normal);

            // Action Buttons
            MakeButton("RestartButton", dialog.transform, "PLAY AGAIN", new Color(0.18f, 0.72f, 0.25f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -115), new Vector2(340, 65), 28);

            MakeButton("MenuButton", dialog.transform, "MAIN MENU", new Color(0.65f, 0.30f, 0.15f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -195), new Vector2(340, 55), 24);
        }

        private static Font GetFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static GameObject MakePanel(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
            return go;
        }

        private static GameObject MakeText(string name, Transform parent, string txt, int size, Color color,
            Vector2 anchor, Vector2 pos, Vector2 dim, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dim;

            var t = go.AddComponent<Text>();
            t.text = txt; t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = style;
            t.font = GetFont();
            t.raycastTarget = false;
            return go;
        }

        private static GameObject MakeButton(string name, Transform parent, string label, Color bgColor,
            Vector2 anchor, Vector2 pos, Vector2 dim, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dim;

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();

            var colors = btn.colors;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            colors.pressedColor     = Color.Lerp(bgColor, Color.black, 0.35f);
            btn.colors = colors;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            var t = labelGo.AddComponent<Text>();
            t.text = label; t.fontSize = fontSize; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;
            t.font = GetFont();
            t.raycastTarget = false;
            return go;
        }

        private static GameObject MakeImageRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dim;

            go.AddComponent<Image>();
            return go;
        }
    }
}
