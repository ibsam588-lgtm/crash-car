using UnityEngine;

namespace CarCrash
{
    public sealed class ElysianSoundtrack : MonoBehaviour
    {
        public const string PreferenceKey = "CarCrash.MusicEnabled";

        private static ElysianSoundtrack _instance;
        private AudioSource _source;
        private bool _started;
        private bool _appPaused;

        public bool MusicEnabled { get; private set; }

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
            _source = gameObject.AddComponent<AudioSource>();
            _source.clip = Resources.Load<AudioClip>("Audio/CityAfterlight");
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.loop = true;
            _source.volume = .56f;
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
