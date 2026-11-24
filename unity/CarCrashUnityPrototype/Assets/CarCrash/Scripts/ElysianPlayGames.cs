using UnityEngine;

namespace CarCrash
{
    public sealed class ElysianPlayGames : MonoBehaviour
    {
        public bool Available { get; private set; }
        public string Message { get; private set; } = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaClass _bridge;
        private AndroidJavaObject _activity;
        private float _nextPoll;
        private bool _syncOnSignIn = true;

        private void Awake()
        {
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                _activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                _bridge = new AndroidJavaClass("com.corsairlabs.elysian.games.PlayGamesBridge");
                Available = _bridge.CallStatic<bool>("isConfigured", _activity);
                if (Available) _bridge.CallStatic("refresh", _activity);
            }
            catch (AndroidJavaException error)
            {
                Available = false;
                Debug.LogWarning("Google Play Games unavailable: " + error.Message);
            }
        }

        private void Update()
        {
            if (!Available || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + .5f;
            Message = _bridge.CallStatic<string>("getMessage");
            SyncProgress();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused || !Available) return;
            _syncOnSignIn = true;
            _bridge.CallStatic("refresh", _activity);
        }

        private void SyncProgress()
        {
            if (!_syncOnSignIn || !_bridge.CallStatic<bool>("isAuthenticated")) return;
            _syncOnSignIn = false;
            var finished = false;
            foreach (var course in ImpactCourse.Catalog)
            {
                if (!ImpactCareer.Finished(course)) continue;
                finished = true;
                _bridge.CallStatic("submit", _activity, course.Id, (long)ImpactCareer.Best(course));
            }
            if (finished) _bridge.CallStatic("unlock", _activity, "first_finish");
            if (PlayerPrefs.GetInt(ImpactCareer.TargetsKey, 0) >= 25) _bridge.CallStatic("unlock", _activity, "break_25");
            if (ImpactCareer.GoldPaintUnlocked) _bridge.CallStatic("unlock", _activity, "earn_gold");
        }

        private void OnDestroy()
        {
            _bridge?.Dispose();
            _activity?.Dispose();
        }
#endif

        public void RecordCompletion()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Available) return;
            _syncOnSignIn = true;
            SyncProgress();
#endif
        }

        public void Show(ImpactCourse course, bool achievements)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Available) return;
            _bridge.CallStatic("show", _activity, course.Id, achievements);
#endif
        }
    }
}
