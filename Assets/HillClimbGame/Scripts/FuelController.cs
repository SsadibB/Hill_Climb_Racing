using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace HillClimbGame
{
    public class FuelController : MonoBehaviour
    {
        [Header("Fuel Settings")]
        [SerializeField] private float startingFuel         = 100f;
        [SerializeField] private float maxFuel              = 100f;
        [SerializeField] private float gasDrainRate         = 5.0f;    // Fuel/sec when pressing gas (normal rate)
        [SerializeField] private float backwardDrainRate    = 2.0f;    // Fuel/sec when moving backward (lower rate)

        [Header("HUD References")]
        [SerializeField] private Image  fuelBarFill;
        [SerializeField] private Text   fuelPercentageText;

        [Header("Fuel Can Pickups (pre-created)")]
        [SerializeField] private List<FuelCanPickup> fuelCans = new List<FuelCanPickup>();

        public float CurrentFuel    { get; private set; }
        public bool  IsEmpty        => CurrentFuel <= 0f;

        private bool          _active;
        private VehicleController _vehicle;

        public event System.Action OnFuelEmpty;

        void Awake()
        {
            AutoWire();
            CurrentFuel  = startingFuel;
            UpdateHUD();
        }

        public void AutoWire()
        {
            if (_vehicle == null) _vehicle = FindAnyObjectByType<VehicleController>();

            var canvas = GameObject.Find("MainCanvas")?.transform;
            if (canvas != null)
            {
                var fills = canvas.GetComponentsInChildren<Image>(true);
                foreach (var img in fills)
                {
                    if (img.name == "FuelBarFill") { fuelBarFill = img; break; }
                }
                var texts = canvas.GetComponentsInChildren<Text>(true);
                foreach (var txt in texts)
                {
                    if (txt.name == "FuelPercentageText") { fuelPercentageText = txt; break; }
                }
            }

            if (fuelCans == null || fuelCans.Count == 0)
            {
                fuelCans = new List<FuelCanPickup>(FindObjectsByType<FuelCanPickup>(FindObjectsInactive.Exclude));
            }
        }

        public void SetActive(bool active) => _active = active;

        void Update()
        {
            if (!_active || IsEmpty) return;

            float drainRate = 0f;
            if (_vehicle != null)
            {
                switch (_vehicle.DriveState)
                {
                    case VehicleDriveState.Gas:
                        drainRate = gasDrainRate;
                        break;
                    case VehicleDriveState.Backward:
                        drainRate = backwardDrainRate;
                        break;
                    case VehicleDriveState.Braking:
                    case VehicleDriveState.Idle:
                    default:
                        drainRate = 0f;
                        break;
                }
            }

            // Immediately stops consumption when gas or backward input is released
            if (drainRate > 0f)
            {
                CurrentFuel = Mathf.Max(0f, CurrentFuel - drainRate * Time.deltaTime);
                UpdateHUD();

                if (IsEmpty)
                {
                    _active = false;
                    OnFuelEmpty?.Invoke();
                }
            }
        }

        public void CollectFuelCan(FuelCanPickup can)
        {
            // Instantly refill to 100% regardless of current level
            CurrentFuel = maxFuel;
            _active     = true;   // Re-activate if fuel was empty
            UpdateHUD();
            can.gameObject.SetActive(false);
            AudioController.Instance?.PlayFuelPickup();
        }

        void UpdateHUD()
        {
            float pct = CurrentFuel / maxFuel;
            if (fuelBarFill != null) fuelBarFill.fillAmount = pct;
            if (fuelPercentageText != null) fuelPercentageText.text = Mathf.RoundToInt(pct * 100f) + "%";
        }

        public void ResetFuel()
        {
            CurrentFuel = startingFuel;
            _active     = false;
            UpdateHUD();
            foreach (var can in fuelCans)
                if (can != null) can.gameObject.SetActive(true);
        }
    }
}
