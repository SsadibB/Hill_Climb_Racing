using UnityEngine;
using DG.Tweening;

namespace HillClimbGame
{
    public class AudioController : MonoBehaviour
    {
        [Header("Audio Sources (pre-created)")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource weatherSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Ambient Clips")]
        [SerializeField] private AudioClip dayAmbient;
        [SerializeField] private AudioClip nightAmbient;
        [SerializeField] private AudioClip windClip;

        [Header("Weather Clips")]
        [SerializeField] private AudioClip rainLightClip;
        [SerializeField] private AudioClip rainHeavyClip;
        [SerializeField] private AudioClip thunderClip;
        [SerializeField] private AudioClip snowWindClip;

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("UI & SFX")]
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip coinClip;
        [SerializeField] private AudioClip fuelClip;
        [SerializeField] private AudioSource uiAudioSource;

        [Header("Volume Settings")]
        [SerializeField] private float masterVolume  = 1f;
        [SerializeField] private float musicVolume   = 0.4f;
        [SerializeField] private float ambientVolume = 0.6f;
        [SerializeField] private float weatherVolume = 0.7f;
        [SerializeField] private float crossfadeDuration = 2f;

        public static AudioController Instance { get; private set; }

        private Tween _ambientFade;
        private Tween _weatherFade;
        private WeatherType  _lastWeather = WeatherType.Clear;
        private TimeOfDay    _lastTime    = TimeOfDay.Day;

        void Awake()
        {
            Instance = this;
            AutoWire();
            SetupSource(ambientSource, true, ambientVolume);
            SetupSource(weatherSource, false, weatherVolume);
            SetupSource(musicSource,   true,  musicVolume);
            SetupSource(uiAudioSource, false, 0.8f);
        }

        public void AutoWire()
        {
            if (ambientSource == null)
            {
                var go = GameObject.Find("AmbientAudioSource");
                if (go != null) ambientSource = go.GetComponent<AudioSource>();
            }
            if (weatherSource == null)
            {
                var go = GameObject.Find("WeatherAudioSource");
                if (go != null) weatherSource = go.GetComponent<AudioSource>();
            }
            if (musicSource == null)
            {
                var go = GameObject.Find("MusicAudioSource");
                if (go != null) musicSource = go.GetComponent<AudioSource>();
            }
            if (uiAudioSource == null)
            {
                var go = GameObject.Find("UIAudioSource");
                if (go != null) uiAudioSource = go.GetComponent<AudioSource>();
            }
#if UNITY_EDITOR
            if (coinClip == null)
                coinClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/HillClimbGame/Audio/coin.wav");
            if (fuelClip == null)
                fuelClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/HillClimbGame/Audio/fuel.wav");
#endif
        }

        void Start()
        {
            if (backgroundMusic != null && musicSource != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
            }
            PlayAmbient(dayAmbient);
        }

        void SetupSource(AudioSource src, bool loop, float vol)
        {
            if (src == null) return;
            src.loop   = loop;
            src.volume = vol * masterVolume;
        }

        public void OnWeatherChanged(WeatherType weather)
        {
            if (weather == _lastWeather) return;
            _lastWeather = weather;

            AudioClip clip = weather switch
            {
                WeatherType.LightRain    => rainLightClip,
                WeatherType.HeavyRain    => rainHeavyClip,
                WeatherType.Thunderstorm => rainHeavyClip,
                WeatherType.Snow         => snowWindClip,
                WeatherType.Fog          => windClip,
                _                        => null,
            };

            CrossfadeWeather(clip);
        }

        public void OnTimeChanged(TimeOfDay time)
        {
            if (time == _lastTime) return;
            _lastTime = time;

            AudioClip clip = (time == TimeOfDay.Night || time == TimeOfDay.Sunrise)
                ? nightAmbient
                : dayAmbient;

            if (clip != null) PlayAmbient(clip);
        }

        void PlayAmbient(AudioClip clip)
        {
            if (ambientSource == null || clip == null) return;
            if (ambientSource.clip == clip && ambientSource.isPlaying) return;
            _ambientFade?.Kill();
            ambientSource.DOFade(0f, crossfadeDuration * 0.5f).OnComplete(() =>
            {
                ambientSource.clip = clip;
                ambientSource.Play();
                ambientSource.DOFade(ambientVolume * masterVolume, crossfadeDuration * 0.5f);
            });
        }

        void CrossfadeWeather(AudioClip clip)
        {
            if (weatherSource == null) return;
            _weatherFade?.Kill();

            if (clip == null)
            {
                _weatherFade = weatherSource.DOFade(0f, crossfadeDuration).OnComplete(() => weatherSource.Stop());
                return;
            }

            _weatherFade = weatherSource.DOFade(0f, crossfadeDuration * 0.4f).OnComplete(() =>
            {
                weatherSource.clip = clip;
                weatherSource.Play();
                _weatherFade = weatherSource.DOFade(weatherVolume * masterVolume, crossfadeDuration * 0.6f);
            });
        }

        public void PlayUIClick()
        {
            if (uiAudioSource != null && uiClickClip != null)
                uiAudioSource.PlayOneShot(uiClickClip);
        }

        public void PlayCoinPickup()
        {
            if (uiAudioSource != null && coinClip != null)
                uiAudioSource.PlayOneShot(coinClip, 0.75f * masterVolume);
        }

        public void PlayFuelPickup()
        {
            if (uiAudioSource != null && fuelClip != null)
                uiAudioSource.PlayOneShot(fuelClip, 0.9f * masterVolume);
        }

        public void PlayThunder()
        {
            if (uiAudioSource != null && thunderClip != null)
                uiAudioSource.PlayOneShot(thunderClip, 0.8f * masterVolume);
        }

        public void SetPaused(bool paused)
        {
            if (paused)
            {
                ambientSource?.Pause();
                weatherSource?.Pause();
            }
            else
            {
                ambientSource?.UnPause();
                weatherSource?.UnPause();
            }
        }
    }
}
