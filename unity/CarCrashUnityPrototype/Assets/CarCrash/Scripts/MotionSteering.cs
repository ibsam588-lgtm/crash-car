using UnityEngine;

namespace CarCrash
{
    internal sealed class MotionSteering
    {
        private const string PreferenceKey = "CarCrash.MotionSteering";
        private const float CalibrationSeconds = 0.24f;
        private const float DeadZone = 0.045f;
        private const float FullLock = 0.46f;
        private ScreenOrientation _orientation;
        private float _calibrationTime;
        private float _neutral;
        private float _filtered;
        private bool _hasReading;

        public bool Available { get; } = Application.isMobilePlatform && SystemInfo.supportsAccelerometer;
        public bool Enabled { get; private set; }
        public bool Calibrating => Enabled && (_calibrationTime > 0f || !_hasReading);
        public float Value { get; private set; }

        public MotionSteering()
        {
            Enabled = Available && PlayerPrefs.GetInt(PreferenceKey, 0) == 1;
            if (Enabled) ConfigureSensor();
        }

        public void SetEnabled(bool enabled)
        {
            if (!Available || Enabled == enabled) return;
            Enabled = enabled;
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (enabled) ConfigureSensor();
            else Value = 0f;
        }

        private void ConfigureSensor()
        {
            // Unity's legacy input already rotates acceleration into screen coordinates.
            Input.compensateSensors = true;
            _orientation = ResolveOrientation();
            Center();
        }

        public void Center()
        {
            if (!Enabled) return;
            _calibrationTime = CalibrationSeconds;
            _hasReading = false;
            Value = 0f;
        }

        public void Step(float deltaTime)
        {
            if (!Enabled) return;
            var orientation = ResolveOrientation();
            if (orientation != _orientation)
            {
                _orientation = orientation;
                Center();
            }

            var acceleration = Input.acceleration;
            var magnitude = acceleration.magnitude;
            if (!float.IsFinite(magnitude) || magnitude < 0.2f)
            {
                // Sensor data can be unavailable while the app is resuming or on an emulator.
                Value = 0f;
                return;
            }

            var lateralGravity = acceleration.x / magnitude;
            if (!_hasReading)
            {
                _filtered = lateralGravity;
                _hasReading = true;
            }
            else
            {
                _filtered = Mathf.Lerp(_filtered, lateralGravity, 1f - Mathf.Exp(-12f * deltaTime));
            }

            if (_calibrationTime > 0f)
            {
                _neutral = Mathf.Clamp(_filtered, -0.5f, 0.5f);
                _calibrationTime = Mathf.Max(0f, _calibrationTime - deltaTime);
                return;
            }

            var tilt = _filtered - _neutral;
            var range = Mathf.InverseLerp(DeadZone, FullLock, Mathf.Abs(tilt));
            Value = Mathf.Sign(tilt) * range * (0.65f + 0.35f * range);
        }

        private ScreenOrientation ResolveOrientation()
        {
            var screen = Screen.orientation;
            if (screen != ScreenOrientation.AutoRotation && screen != ScreenOrientation.Unknown) return screen;

            // With auto-rotation enabled, keep the last resolved direction when the device is face-up.
            if (Screen.width > Screen.height)
            {
                return Input.deviceOrientation switch
                {
                    DeviceOrientation.LandscapeLeft => ScreenOrientation.LandscapeLeft,
                    DeviceOrientation.LandscapeRight => ScreenOrientation.LandscapeRight,
                    _ => _orientation == ScreenOrientation.LandscapeRight
                        ? ScreenOrientation.LandscapeRight : ScreenOrientation.LandscapeLeft
                };
            }
            return Input.deviceOrientation switch
            {
                DeviceOrientation.Portrait => ScreenOrientation.Portrait,
                DeviceOrientation.PortraitUpsideDown => ScreenOrientation.PortraitUpsideDown,
                _ => _orientation == ScreenOrientation.PortraitUpsideDown
                    ? ScreenOrientation.PortraitUpsideDown : ScreenOrientation.Portrait
            };
        }
    }
}
