using UnityEngine;
using UnityEngine.EventSystems;

namespace HillClimbGame
{
    /// <summary>
    /// Attach to on-screen Gas and Brake pedals.
    /// Uses IPointerDownHandler and IPointerUpHandler for continuous hold driving.
    /// Works with mouse, touchscreen, and controller raycasts.
    /// </summary>
    public class PedalButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool isGas;

        private VehicleController _vehicle;

        void Start()
        {
            _vehicle = Object.FindAnyObjectByType<VehicleController>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_vehicle == null) _vehicle = Object.FindAnyObjectByType<VehicleController>();
            if (_vehicle != null)
            {
                if (isGas) _vehicle.OnAccelPressed();
                else _vehicle.OnBrakePressed();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_vehicle == null) _vehicle = Object.FindAnyObjectByType<VehicleController>();
            if (_vehicle != null)
            {
                if (isGas) _vehicle.OnAccelReleased();
                else _vehicle.OnBrakeReleased();
            }
        }

        void OnDisable()
        {
            if (_vehicle != null)
            {
                if (isGas) _vehicle.OnAccelReleased();
                else _vehicle.OnBrakeReleased();
            }
        }
    }
}
