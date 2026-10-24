using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace HillClimbGame
{
    public enum GameState { MainMenu, Loading, Playing, Paused, GameOver }

    public class UIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private CanvasGroup mainMenuPanel;
        [SerializeField] private CanvasGroup loadingPanel;
        [SerializeField] private CanvasGroup gameplayHUD;
        [SerializeField] private CanvasGroup pausePanel;
        [SerializeField] private CanvasGroup howToPlayPanel;
        [SerializeField] private CanvasGroup gameOverPanel;

        [Header("Main Menu")]
        [SerializeField] private Text mainMenuBestDistanceText;
        [SerializeField] private Text mainMenuTotalCoinsText;

        [Header("Loading")]
        [SerializeField] private Image loadingBarFill;
        [SerializeField] private Text  loadingPercentageText;
        [SerializeField] private Text  loadingTipText;

        [Header("Gameplay HUD")]
        [SerializeField] private Text speedText;
        [SerializeField] private Text distanceText;
        [SerializeField] private Text hudCoinText;

        [Header("Game Over")]
        [SerializeField] private Text gameOverDistanceText;
        [SerializeField] private Text gameOverCoinsText;
        [SerializeField] private Text gameOverBestText;
        [SerializeField] private Text gameOverCauseText;

        [Header("Transition")]
        [SerializeField] private float panelFadeDuration = 0.4f;
        [SerializeField] private float loadingDuration   = 1.5f;

        [Header("Loading Tips")]
        [SerializeField] private string[] tips = {
            "Collect fuel cans to keep driving!",
            "Accelerate into hills for momentum.",
            "Coins appear on hilltops - grab them!",
            "Bad weather reduces visibility.",
            "The longer you drive, the harder it gets."
        };

        private Tween _loadingTween;
        private Tween _gameOverFade;

        // Active tweens per panel
        private Tween _menuFade, _loadFade, _hudFade, _pauseFade, _howFade, _goFade;

        void Awake()
        {
            AutoWire();
            // All panels start hidden except main menu
            SetGroupImmediate(mainMenuPanel, true);
            SetGroupImmediate(loadingPanel,  false);
            SetGroupImmediate(gameplayHUD,   false);
            SetGroupImmediate(pausePanel,    false);
            SetGroupImmediate(howToPlayPanel,false);
            SetGroupImmediate(gameOverPanel, false);
        }

        public void AutoWire()
        {
            var canvas = GameObject.Find("MainCanvas")?.transform;
            if (canvas == null) return;

            Transform FindChildRecursive(Transform parent, string name)
            {
                if (parent.name == name) return parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    var found = FindChildRecursive(parent.GetChild(i), name);
                    if (found != null) return found;
                }
                return null;
            }

            if (mainMenuPanel == null)  mainMenuPanel = canvas.Find("MainMenuPanel")?.GetComponent<CanvasGroup>();
            if (loadingPanel == null)   loadingPanel = canvas.Find("LoadingPanel")?.GetComponent<CanvasGroup>();
            if (gameplayHUD == null)    gameplayHUD = canvas.Find("GameplayHUD")?.GetComponent<CanvasGroup>();
            if (pausePanel == null)     pausePanel = canvas.Find("PausePanel")?.GetComponent<CanvasGroup>();
            if (howToPlayPanel == null) howToPlayPanel = canvas.Find("HowToPlayPanel")?.GetComponent<CanvasGroup>();
            if (gameOverPanel == null)  gameOverPanel = canvas.Find("GameOverPanel")?.GetComponent<CanvasGroup>();

            if (mainMenuBestDistanceText == null) mainMenuBestDistanceText = FindChildRecursive(canvas, "BestDistanceText")?.GetComponent<Text>();
            if (mainMenuTotalCoinsText == null)   mainMenuTotalCoinsText = FindChildRecursive(canvas, "TotalCoinsText")?.GetComponent<Text>();

            if (loadingBarFill == null)        loadingBarFill = FindChildRecursive(canvas, "LoadingBarFill")?.GetComponent<Image>();
            if (loadingPercentageText == null) loadingPercentageText = FindChildRecursive(canvas, "LoadingPercentageText")?.GetComponent<Text>();
            if (loadingTipText == null)        loadingTipText = FindChildRecursive(canvas, "LoadingTipText")?.GetComponent<Text>();

            if (speedText == null)    speedText = FindChildRecursive(canvas, "SpeedText")?.GetComponent<Text>();
            if (distanceText == null) distanceText = FindChildRecursive(canvas, "DistanceText")?.GetComponent<Text>();
            if (hudCoinText == null)  hudCoinText = FindChildRecursive(canvas, "CoinCounter")?.GetComponent<Text>();

            if (gameOverDistanceText == null) gameOverDistanceText = FindChildRecursive(canvas, "FinalDistanceText")?.GetComponent<Text>();
            if (gameOverCoinsText == null)    gameOverCoinsText = FindChildRecursive(canvas, "RunCoinsText")?.GetComponent<Text>();
            if (gameOverBestText == null)     gameOverBestText = FindChildRecursive(canvas, "GameOverBestText")?.GetComponent<Text>();
            if (gameOverCauseText == null)    gameOverCauseText = FindChildRecursive(canvas, "OutOfFuelLabel")?.GetComponent<Text>();

            // Wire Buttons
            var gc = FindAnyObjectByType<GameController>();
            if (gc != null)
            {
                BindButton(canvas, "PlayButton",      () => gc.StartGame());
                BindButton(canvas, "HowToPlayButton", () => gc.ShowHowToPlay());
                BindButton(canvas, "QuitButton",       () => gc.Quit());
                BindButton(canvas, "PauseButton",      () => gc.TogglePause());
                BindButton(canvas, "ResumeButton",     () => gc.TogglePause());
                BindButton(canvas, "MenuButton",       () => gc.EnterMainMenu());
                BindButton(canvas, "RestartButton",    () => gc.Restart());
                BindButton(canvas, "HowToCloseBtn",    () => gc.HideHowToPlay());
            }
        }

        void BindButton(Transform root, string btnName, UnityEngine.Events.UnityAction action)
        {
            var tr = root.GetComponentsInChildren<Button>(true);
            foreach (var b in tr)
            {
                if (b.name == btnName)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(action);
                }
            }
        }

        public void HideAllPanelsExcept(CanvasGroup keep)
        {
            SetGroupImmediate(mainMenuPanel,  mainMenuPanel  == keep);
            SetGroupImmediate(loadingPanel,   loadingPanel   == keep);
            SetGroupImmediate(gameplayHUD,    gameplayHUD    == keep);
            SetGroupImmediate(pausePanel,     pausePanel     == keep);
            SetGroupImmediate(howToPlayPanel, howToPlayPanel == keep);
            SetGroupImmediate(gameOverPanel,  gameOverPanel  == keep);
        }

        public void ShowMainMenu(float bestDist, int totalCoins)
        {
            if (mainMenuBestDistanceText != null)
                mainMenuBestDistanceText.text = $"Best: {Mathf.RoundToInt(bestDist)} m";
            if (mainMenuTotalCoinsText != null)
                mainMenuTotalCoinsText.text   = $"Coins: {totalCoins}";

            FadeGroup(ref _menuFade, mainMenuPanel,  true);
            FadeGroup(ref _loadFade, loadingPanel,   false);
            FadeGroup(ref _hudFade,  gameplayHUD,    false);
            FadeGroup(ref _goFade,   gameOverPanel,  false);
            FadeGroup(ref _pauseFade,pausePanel,     false);
            FadeGroup(ref _howFade,  howToPlayPanel, false);
        }

        public void ShowLoading(System.Action onComplete)
        {
            FadeGroup(ref _menuFade, mainMenuPanel,  false);
            FadeGroup(ref _goFade,   gameOverPanel,  false);
            FadeGroup(ref _pauseFade,pausePanel,     false);
            FadeGroup(ref _howFade,  howToPlayPanel, false);
            FadeGroup(ref _hudFade,  gameplayHUD,    false);
            FadeGroup(ref _loadFade, loadingPanel,   true);

            // Random tip
            if (loadingTipText != null && tips.Length > 0)
                loadingTipText.text = tips[Random.Range(0, tips.Length)];

            // Animate bar
            if (loadingBarFill != null) loadingBarFill.fillAmount = 0f;
            if (loadingPercentageText != null) loadingPercentageText.text = "0%";

            _loadingTween?.Kill();
            float progress = 0f;
            _loadingTween = DOTween.To(() => progress, v =>
            {
                progress = v;
                if (loadingBarFill       != null) loadingBarFill.fillAmount = v;
                if (loadingPercentageText!= null) loadingPercentageText.text= Mathf.RoundToInt(v * 100f) + "%";
            }, 1f, loadingDuration)
            .OnComplete(() =>
            {
                FadeGroup(ref _loadFade, loadingPanel, false);
                onComplete?.Invoke();
            });
        }

        public void ShowGameplay()
        {
            FadeGroup(ref _menuFade, mainMenuPanel,  false);
            FadeGroup(ref _loadFade, loadingPanel,   false);
            FadeGroup(ref _goFade,   gameOverPanel,  false);
            FadeGroup(ref _pauseFade,pausePanel,     false);
            FadeGroup(ref _howFade,  howToPlayPanel, false);
            FadeGroup(ref _hudFade,  gameplayHUD,    true);
        }

        public void ShowPause(bool show)
        {
            FadeGroup(ref _pauseFade, pausePanel, show);
        }

        public void ShowHowToPlay(bool show)
        {
            FadeGroup(ref _howFade, howToPlayPanel, show);
        }

        public void ShowGameOver(float distance, int coins, float bestDist, string cause)
        {
            if (gameOverDistanceText != null) gameOverDistanceText.text = $"{Mathf.RoundToInt(distance)} m";
            if (gameOverCoinsText    != null) gameOverCoinsText.text    = coins.ToString();
            if (gameOverBestText     != null) gameOverBestText.text     = $"{Mathf.RoundToInt(bestDist)} m";
            if (gameOverCauseText    != null) gameOverCauseText.text    = cause;

            FadeGroup(ref _goFade, gameOverPanel, true);
        }

        public void UpdateHUD(float speedKmh, float distanceM, int coins, string tierName = null)
        {
            if (speedText    != null) speedText.text    = $"{Mathf.RoundToInt(speedKmh)} km/h";
            if (distanceText != null)
            {
                if (!string.IsNullOrEmpty(tierName))
                    distanceText.text = $"{Mathf.RoundToInt(distanceM)} m [{tierName}]";
                else
                    distanceText.text = $"{Mathf.RoundToInt(distanceM)} m";
            }
            if (hudCoinText  != null) hudCoinText.text  = coins.ToString();
        }

        void FadeGroup(ref Tween tween, CanvasGroup group, bool show)
        {
            if (group == null) return;
            tween?.Kill();
            float target = show ? 1f : 0f;
            group.interactable    = show;
            group.blocksRaycasts  = show;
            tween = group.DOFade(target, panelFadeDuration);
        }

        void SetGroupImmediate(CanvasGroup group, bool show)
        {
            if (group == null) return;
            group.alpha           = show ? 1f : 0f;
            group.interactable    = show;
            group.blocksRaycasts  = show;
        }
    }
}
