using UnityEngine;
using DG.Tweening;

namespace HillClimbGame
{
    public enum WeatherType { Clear, Fog, Snow, LightRain, HeavyRain, Thunderstorm }

    public class WeatherController : MonoBehaviour
    {
        [Header("Weather Effect GameObjects (pre-created)")]
        [SerializeField] private ParticleSystem rainLightParticles;
        [SerializeField] private ParticleSystem rainHeavyParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private SpriteRenderer fogOverlay;
        [SerializeField] private SpriteRenderer lightningFlash;

        [Header("Sky Color Tint (DayNight SkyBackground)")]
        [SerializeField] private SpriteRenderer skyBackground;

        [Header("Weather Colors")]
        [SerializeField] private Color clearSkyColor      = new Color(0.4f, 0.7f, 1f);
        [SerializeField] private Color fogSkyColor        = new Color(0.6f, 0.65f, 0.7f);
        [SerializeField] private Color snowSkyColor       = new Color(0.75f, 0.80f, 0.90f);
        [SerializeField] private Color lightRainSkyColor  = new Color(0.3f, 0.4f, 0.55f);
        [SerializeField] private Color heavyRainSkyColor  = new Color(0.15f, 0.2f, 0.35f);
        [SerializeField] private Color stormSkyColor      = new Color(0.08f, 0.10f, 0.20f);

        [Header("Timing")]
        [SerializeField] private float minWeatherDuration = 30f;
        [SerializeField] private float maxWeatherDuration = 90f;
        [SerializeField] private float transitionDuration = 3f;
        [SerializeField] private bool  autoChange         = true;

        [Header("Lightning")]
        [SerializeField] private float minLightningInterval = 5f;
        [SerializeField] private float maxLightningInterval = 15f;
        [SerializeField] private float lightningDuration    = 0.12f;

        public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;

        private float _weatherTimer;
        private float _weatherDuration;
        private float _lightningTimer;
        private Tween _skyTween;
        private Tween _lightningTween;
        private bool  _active;

        public event System.Action<WeatherType> OnWeatherChanged;

        void Awake()
        {
            AutoWire();
            _weatherDuration = Random.Range(minWeatherDuration, maxWeatherDuration);
            _lightningTimer  = Random.Range(minLightningInterval, maxLightningInterval);

            // Ensure fog/flash start invisible
            if (fogOverlay    != null) SetAlpha(fogOverlay,    0f);
            if (lightningFlash!= null) SetAlpha(lightningFlash, 0f);
        }

        public void AutoWire()
        {
            if (rainLightParticles == null)
            {
                var go = GameObject.Find("RainLight");
                if (go != null) rainLightParticles = go.GetComponent<ParticleSystem>();
            }
            if (rainHeavyParticles == null)
            {
                var go = GameObject.Find("RainHeavy");
                if (go != null) rainHeavyParticles = go.GetComponent<ParticleSystem>();
            }
            if (snowParticles == null)
            {
                var go = GameObject.Find("Snow");
                if (go != null) snowParticles = go.GetComponent<ParticleSystem>();
            }
            if (fogOverlay == null)
            {
                var go = GameObject.Find("FogOverlay");
                if (go != null) fogOverlay = go.GetComponent<SpriteRenderer>();
            }
            if (lightningFlash == null)
            {
                var go = GameObject.Find("LightningFlash");
                if (go != null) lightningFlash = go.GetComponent<SpriteRenderer>();
            }
            if (skyBackground == null)
            {
                var go = GameObject.Find("SkyBackground");
                if (go != null) skyBackground = go.GetComponent<SpriteRenderer>();
            }
        }

        public void SetActive(bool active) => _active = active;

        void Update()
        {
            if (!_active) return;

            if (autoChange)
            {
                _weatherTimer += Time.deltaTime;
                if (_weatherTimer >= _weatherDuration)
                {
                    _weatherTimer    = 0f;
                    _weatherDuration = Random.Range(minWeatherDuration, maxWeatherDuration);
                    TransitionToRandom();
                }
            }

            if (CurrentWeather == WeatherType.Thunderstorm)
            {
                _lightningTimer -= Time.deltaTime;
                if (_lightningTimer <= 0f)
                {
                    TriggerLightning();
                    _lightningTimer = Random.Range(minLightningInterval, maxLightningInterval);
                }
            }
        }

        void TransitionToRandom()
        {
            WeatherType next = (WeatherType)Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length);
            if (next == CurrentWeather) return;
            SetWeather(next);
        }

        public void SetWeather(WeatherType weather)
        {
            CurrentWeather = weather;

            // Particle systems
            ControlParticles(rainLightParticles, weather == WeatherType.LightRain || weather == WeatherType.Thunderstorm);
            ControlParticles(rainHeavyParticles, weather == WeatherType.HeavyRain || weather == WeatherType.Thunderstorm);
            ControlParticles(snowParticles,      weather == WeatherType.Snow);

            // Fog overlay
            float fogAlpha = weather == WeatherType.Fog ? 0.45f : 0f;
            FadeSpriteAlpha(fogOverlay, fogAlpha, transitionDuration);

            // Sky tint
            Color targetColor = GetSkyColor(weather);
            _skyTween?.Kill();
            if (skyBackground != null)
                _skyTween = DOTween.To(() => skyBackground.color, c => skyBackground.color = c, targetColor, transitionDuration);

            OnWeatherChanged?.Invoke(weather);
        }

        Color GetSkyColor(WeatherType w) => w switch
        {
            WeatherType.Fog         => fogSkyColor,
            WeatherType.Snow        => snowSkyColor,
            WeatherType.LightRain   => lightRainSkyColor,
            WeatherType.HeavyRain   => heavyRainSkyColor,
            WeatherType.Thunderstorm=> stormSkyColor,
            _                       => clearSkyColor,
        };

        void ControlParticles(ParticleSystem ps, bool enable)
        {
            if (ps == null) return;
            if (enable && !ps.isPlaying) ps.Play();
            if (!enable && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void FadeSpriteAlpha(SpriteRenderer sr, float target, float duration)
        {
            if (sr == null) return;
            DOTween.To(() => sr.color.a, a => SetAlpha(sr, a), target, duration);
        }

        void SetAlpha(SpriteRenderer sr, float a)
        {
            Color c = sr.color; c.a = a; sr.color = c;
        }

        void TriggerLightning()
        {
            if (lightningFlash == null) return;
            _lightningTween?.Kill();
            SetAlpha(lightningFlash, 0.8f);
            _lightningTween = DOTween.To(() => lightningFlash.color.a, a => SetAlpha(lightningFlash, a),
                                          0f, lightningDuration);
        }

        public void ResetWeather()
        {
            SetWeather(WeatherType.Clear);
            _weatherTimer = 0f;
        }
    }
}
