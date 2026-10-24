using UnityEngine;
using DG.Tweening;

namespace HillClimbGame
{
    /// <summary>
    /// Central game state machine. Coordinates all controllers.
    /// No GameObjects are created at runtime.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("Controller References")]
        [SerializeField] private VehicleController  vehicleController;
        [SerializeField] private TerrainController  terrainController;
        [SerializeField] private FuelController     fuelController;
        [SerializeField] private CoinController     coinController;
        [SerializeField] private WeatherController  weatherController;
        [SerializeField] private DayNightController dayNightController;
        [SerializeField] private AudioController    audioController;
        [SerializeField] private SaveController     saveController;
        [SerializeField] private UIController       uiController;

        [Header("Spawn")]
        [SerializeField] private Vector3 vehicleSpawnPos = new Vector3(-3f, 0.7f, 0f);

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        void Awake()
        {
            Application.runInBackground = true;
            AutoWire();
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
        }

        public void AutoWire()
        {
            if (saveController == null)     saveController = FindAnyObjectByType<SaveController>();
            if (vehicleController == null)  vehicleController = FindAnyObjectByType<VehicleController>();
            if (terrainController == null)  terrainController = FindAnyObjectByType<TerrainController>();
            if (fuelController == null)     fuelController = FindAnyObjectByType<FuelController>();
            if (coinController == null)     coinController = FindAnyObjectByType<CoinController>();
            if (weatherController == null)  weatherController = FindAnyObjectByType<WeatherController>();
            if (dayNightController == null) dayNightController = FindAnyObjectByType<DayNightController>();
            if (audioController == null)    audioController = FindAnyObjectByType<AudioController>();
            if (uiController == null)       uiController = FindAnyObjectByType<UIController>();
        }

        void Start()
        {
            // Connect events
            if (fuelController != null) fuelController.OnFuelEmpty += OnFuelEmpty;
            if (weatherController != null && audioController != null)
                weatherController.OnWeatherChanged += (w) => audioController.OnWeatherChanged(w);
            if (dayNightController != null && audioController != null)
                dayNightController.OnTimeChanged += (t) => audioController.OnTimeChanged(t);

            EnterMainMenu();
        }

        void Update()
        {
            if (CurrentState != GameState.Playing) return;

            // Crash detection
            if (vehicleController != null && vehicleController.IsCrashed)
            {
                OnVehicleCrashed();
                return;
            }

            float dist  = terrainController  != null ? terrainController.Distance  : 0f;
            float speed = vehicleController  != null ? vehicleController.SpeedKmh  : 0f;
            int coins   = coinController     != null ? coinController.RunCoins     : 0;
            string tier = terrainController != null ? terrainController.CurrentTier.ToString().ToUpper() : "";
            uiController?.UpdateHUD(speed, dist, coins, tier);

            // Pause input
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        void OnVehicleCrashed()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.GameOver;
            SetAllControllersActive(false);

            float dist  = terrainController  != null ? terrainController.Distance  : 0f;
            int   coins = coinController     != null ? coinController.RunCoins     : 0;
            saveController?.TryUpdateBestDistance(dist);
            saveController?.AddCoins(coins % 5);

            float best = saveController != null ? saveController.BestDistance : dist;
            uiController?.ShowGameOver(dist, coins, best, "DRIVER DOWN!");
        }

        // ── STATE TRANSITIONS ────────────────────────────────────────────────

        public void EnterMainMenu()
        {
            CurrentState = GameState.MainMenu;
            SetAllControllersActive(false);
            uiController?.ShowMainMenu(
                saveController != null ? saveController.BestDistance : 0f,
                saveController != null ? saveController.TotalCoins   : 0);
        }

        public void StartGame()
        {
            audioController?.PlayUIClick();
            CurrentState = GameState.Loading;
            uiController?.ShowLoading(OnLoadingComplete);
        }

        void OnLoadingComplete()
        {
            ResetRun();
            CurrentState = GameState.Playing;
            uiController?.ShowGameplay();
            SetAllControllersActive(true);
        }

        void ResetRun()
        {
            vehicleController?.ResetVehicle(vehicleSpawnPos);
            terrainController?.ResetTerrain();
            fuelController?.ResetFuel();
            coinController?.ResetCoins();
            weatherController?.ResetWeather();
            dayNightController?.ResetCycle();
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                vehicleController?.SetActive(false);
                fuelController?.SetActive(false);
                weatherController?.SetActive(false);
                dayNightController?.SetActive(false);
                audioController?.SetPaused(true);
                Time.timeScale = 0f;
                uiController?.ShowPause(true);
            }
            else if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
                vehicleController?.SetActive(true);
                fuelController?.SetActive(true);
                weatherController?.SetActive(true);
                dayNightController?.SetActive(true);
                audioController?.SetPaused(false);
                Time.timeScale = 1f;
                uiController?.ShowPause(false);
            }
        }

        void OnFuelEmpty()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.GameOver;
            SetAllControllersActive(false);

            float dist  = terrainController  != null ? terrainController.Distance  : 0f;
            int   coins = coinController     != null ? coinController.RunCoins     : 0;
            saveController?.TryUpdateBestDistance(dist);
            saveController?.AddCoins(coins % 5);   // remainder not yet saved at milestones

            float best = saveController != null ? saveController.BestDistance : dist;
            uiController?.ShowGameOver(dist, coins, best, "OUT OF FUEL");
        }

        public void Restart()
        {
            audioController?.PlayUIClick();
            Time.timeScale = 1f;
            uiController?.HideAllPanelsExcept(null);
            StartGame();
        }

        public void ShowHowToPlay()
        {
            audioController?.PlayUIClick();
            uiController?.ShowHowToPlay(true);
        }

        public void HideHowToPlay()
        {
            uiController?.ShowHowToPlay(false);
        }

        public void Quit()
        {
            audioController?.PlayUIClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── HELPERS ──────────────────────────────────────────────────────────

        void SetAllControllersActive(bool active)
        {
            vehicleController?.SetActive(active);
            fuelController?.SetActive(active);
            weatherController?.SetActive(active);
            dayNightController?.SetActive(active);
        }
    }
}
