using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

namespace HillClimbGame
{
    public enum TimeOfDay { Day, Sunset, Night, Sunrise }

    public class DayNightController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private SpriteRenderer skyBackground;
        [SerializeField] private SpriteRenderer sunOrMoon;
        [SerializeField] private SpriteRenderer dayNightOverlay;
        [SerializeField] private Light2D        globalLight;
        [SerializeField] private Sprite         sunSprite;
        [SerializeField] private Sprite         moonSprite;

        [Header("Sky Colors by Time")]
        [SerializeField] private Color dayColor     = new Color(0.4f, 0.75f, 1f);
        [SerializeField] private Color sunsetColor  = new Color(0.85f, 0.45f, 0.25f);
        [SerializeField] private Color nightColor   = new Color(0.05f, 0.05f, 0.18f);
        [SerializeField] private Color sunriseColor = new Color(0.9f, 0.65f, 0.35f);

        [Header("Overlay Colors")]
        [SerializeField] private Color dayOverlayColor     = new Color(1f,1f,1f,0f);
        [SerializeField] private Color nightOverlayColor   = new Color(0.02f,0.02f,0.15f,0.65f);

        [Header("Light Intensity")]
        [SerializeField] private float dayIntensity    = 1.2f;
        [SerializeField] private float nightIntensity  = 0.35f;

        [Header("Cycle Settings")]
        [SerializeField] private float dayDuration    = 120f;    // seconds per full day

        public TimeOfDay CurrentTime { get; private set; } = TimeOfDay.Day;

        private float _cycleTimer;
        private bool  _active;
        private Tween _skyTween;
        private Tween _overlayTween;
        private Tween _lightTween;

        public event System.Action<TimeOfDay> OnTimeChanged;

        void Awake()
        {
            AutoWire();
        }

        public void AutoWire()
        {
            if (skyBackground == null)
            {
                var go = GameObject.Find("SkyBackground");
                if (go != null) skyBackground = go.GetComponent<SpriteRenderer>();
            }
            if (sunOrMoon == null)
            {
                var go = GameObject.Find("SunOrMoon");
                if (go != null) sunOrMoon = go.GetComponent<SpriteRenderer>();
            }
            if (dayNightOverlay == null)
            {
                var go = GameObject.Find("DayNightOverlay");
                if (go != null) dayNightOverlay = go.GetComponent<SpriteRenderer>();
            }
            if (globalLight == null)
            {
                var go = GameObject.Find("GlobalLight2D");
                if (go != null) globalLight = go.GetComponent<Light2D>();
            }
        }

        public void SetActive(bool active) => _active = active;

        void Update()
        {
            if (!_active) return;
            _cycleTimer += Time.deltaTime;
            float t = (_cycleTimer % dayDuration) / dayDuration;   // 0..1

            TimeOfDay newTime;
            if      (t < 0.25f) newTime = TimeOfDay.Day;
            else if (t < 0.45f) newTime = TimeOfDay.Sunset;
            else if (t < 0.75f) newTime = TimeOfDay.Night;
            else                newTime = TimeOfDay.Sunrise;

            if (newTime != CurrentTime)
            {
                CurrentTime = newTime;
                ApplyTimeOfDay(newTime);
                OnTimeChanged?.Invoke(newTime);
            }
        }

        void ApplyTimeOfDay(TimeOfDay time)
        {
            Color skyTarget, overlayTarget;
            float lightTarget;
            bool  showSun;

            switch (time)
            {
                case TimeOfDay.Sunset:
                    skyTarget     = sunsetColor;
                    overlayTarget = new Color(0.5f, 0.2f, 0.1f, 0.3f);
                    lightTarget   = (dayIntensity + nightIntensity) * 0.5f;
                    showSun       = true;
                    break;
                case TimeOfDay.Night:
                    skyTarget     = nightColor;
                    overlayTarget = nightOverlayColor;
                    lightTarget   = nightIntensity;
                    showSun       = false;
                    break;
                case TimeOfDay.Sunrise:
                    skyTarget     = sunriseColor;
                    overlayTarget = new Color(0.6f, 0.3f, 0.1f, 0.2f);
                    lightTarget   = (dayIntensity + nightIntensity) * 0.5f;
                    showSun       = true;
                    break;
                default:   // Day
                    skyTarget     = dayColor;
                    overlayTarget = dayOverlayColor;
                    lightTarget   = dayIntensity;
                    showSun       = true;
                    break;
            }

            float dur = 8f;
            _skyTween?.Kill();
            _overlayTween?.Kill();
            _lightTween?.Kill();

            if (skyBackground != null)
                _skyTween = DOTween.To(() => skyBackground.color, c => skyBackground.color = c, skyTarget, dur);
            if (dayNightOverlay != null)
                _overlayTween = DOTween.To(() => dayNightOverlay.color, c => dayNightOverlay.color = c, overlayTarget, dur);
            if (globalLight != null)
                _lightTween = DOTween.To(() => globalLight.intensity, v => globalLight.intensity = v, lightTarget, dur);

            if (sunOrMoon != null)
            {
                sunOrMoon.sprite = showSun ? sunSprite : moonSprite;
            }
        }

        public void ResetCycle()
        {
            _cycleTimer  = 0f;
            CurrentTime  = TimeOfDay.Day;
            ApplyTimeOfDay(TimeOfDay.Day);
        }
    }
}
