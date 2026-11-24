using UnityEngine;

namespace CarCrash
{
    public sealed class ElysianSoundtrack : MonoBehaviour
    {
        public const string PreferenceKey = "CarCrash.MusicEnabled";
        public const string EffectsPreferenceKey = "CarCrash.EffectsEnabled";

        private static ElysianSoundtrack _instance;
        private AudioSource _source;
        private bool _started;
        private bool _appPaused;

        public bool MusicEnabled { get; private set; }
        public bool EffectsEnabled { get; private set; }
        internal bool GameplayEffectsAllowed => EffectsEnabled && !_appPaused && Time.timeScale > 0f;

        public static ElysianSoundtrack GetOrCreate()
        {
            if (_instance == null)
                _instance = new GameObject("Elysian soundtrack").AddComponent<ElysianSoundtrack>();
            return _instance;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            MusicEnabled = PlayerPrefs.GetInt(PreferenceKey, 1) == 1;
            EffectsEnabled = PlayerPrefs.GetInt(EffectsPreferenceKey, 1) == 1;
            _source = gameObject.AddComponent<AudioSource>();
            _source.clip = Resources.Load<AudioClip>("Audio/ArcadeRush");
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.loop = true;
            _source.volume = .43f;
            RefreshPlayback();
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (MusicEnabled == enabled) return;
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            RefreshPlayback();
        }

        public void SetEffectsEnabled(bool enabled)
        {
            if (EffectsEnabled == enabled) return;
            EffectsEnabled = enabled;
            PlayerPrefs.SetInt(EffectsPreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            _appPaused = paused;
            RefreshPlayback();
        }

        private void RefreshPlayback()
        {
            if (_source == null || _source.clip == null) return;
            if (!MusicEnabled || _appPaused)
            {
                if (_started) _source.Pause();
            }
            else if (_started)
            {
                _source.UnPause();
            }
            else
            {
                _source.Play();
                _started = true;
            }
        }
    }
}
