using UnityEngine;
using UnityEngine.EventSystems;

namespace CarCrash
{
    public sealed class CarCrashCameraFollow : MonoBehaviour
    {
        public enum DrivingView { Chase, Low, Bonnet }

        public Transform target;
        public CrashScoring scoring;
        public Vector3 offset = new(0f, 4.2f, -8.2f);
        public float followSharpness = 7f;
        public float lookAhead = 5f;

        public bool Showcasing { get; private set; }
        public DrivingView View { get; private set; }
        public bool HomeScreen { get; set; }

        private Camera _camera;
        private ArcadeVehicleController _vehicle;
        private float _orbit = 142f;
        private Vector3 _pointer;
        private bool _orbitDragging;
        private float _transitionUntil;
        private float _orbitDirection = 1f;
        private bool _autoOrbit = true;

        private void Awake() => _camera = GetComponent<Camera>();

        public void SetShowcase(bool enabled)
        {
            Showcasing = enabled;
            if (!enabled) HomeScreen = false;
            _orbitDragging = false;
            _transitionUntil = Time.unscaledTime + 2f;
            if (target != null) _vehicle = target.GetComponent<ArcadeVehicleController>();
        }

        public void RotateShowcase(float degrees)
        {
            _autoOrbit = false;
            _orbit += degrees;
        }
        public void CycleView() => View = (DrivingView)(((int)View + 1) % 3);

        private void LateUpdate()
        {
            if (target == null || (!Showcasing && Time.timeScale <= 0f && Time.unscaledTime > _transitionUntil)) return;
            var dt = Time.unscaledDeltaTime;
            var blend = 1f - Mathf.Exp(-followSharpness * dt);
            if (Showcasing)
            {
                UpdateShowcase(dt, blend);
                return;
            }

            var localOffset = View == DrivingView.Low ? new Vector3(0f, 1.4f, -5.8f)
                : View == DrivingView.Bonnet ? new Vector3(0f, 1.4f, 1.15f) : offset;
            if (_camera.aspect < 1.4f && View != DrivingView.Bonnet)
            {
                // Preserve the coupe's screen width when the horizontal FOV narrows.
                localOffset.z *= Mathf.Clamp(0.8f / _camera.aspect, 1f, 2f);
                localOffset.y *= 1.25f;
            }
            transform.position = Vector3.Lerp(transform.position, target.TransformPoint(localOffset), blend);
            LookAt(target.position + target.forward * lookAhead + Vector3.up * 1.1f, blend);
            var speed = _vehicle != null ? Mathf.Clamp01(_vehicle.SpeedKmh / 200f) : 0f;
            var boost = _vehicle != null && _vehicle.BoostActive ? 6f : 0f;
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, 54f + speed * 12f + boost, blend * 0.6f);

            var sinceImpact = scoring != null ? Time.realtimeSinceStartup - scoring.LastImpactRealtime : 10f;
            if (sinceImpact < 0f || sinceImpact > 0.52f) return;
            var decay = 1f - sinceImpact / 0.52f;
            var strength = Mathf.Clamp01(scoring.LastImpactKmh / 150f) * 0.16f * decay;
            transform.position += new Vector3(Mathf.Sin(Time.unscaledTime * 67f), Mathf.Cos(Time.unscaledTime * 83f) * 0.55f, 0f) * strength;
        }

        private void UpdateShowcase(float dt, float blend)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (Input.touchCount > 0 && EventSystem.current != null)
                    overUi |= EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
                _orbitDragging = !overUi;
                _pointer = Input.mousePosition;
            }
            if (!Input.GetMouseButton(0)) _orbitDragging = false;
            if (_orbitDragging)
            {
                _autoOrbit = false;
                _orbit += (Input.mousePosition.x - _pointer.x) * 0.22f;
                _pointer = Input.mousePosition;
            }
            else if (_autoOrbit)
            {
                if (_orbit > 214f) _orbitDirection = -1f;
                if (_orbit < 136f) _orbitDirection = 1f;
                _orbit += dt * 1.5f * _orbitDirection;
            }

            var portrait = _camera.aspect < 1.4f;
            var angle = _orbit * Mathf.Deg2Rad;
            // Keep the whole body in the much narrower horizontal field of view on phones.
            var radius = portrait ? Mathf.Clamp(6.6f / _camera.aspect, 8.5f, 20f) : HomeScreen ? 8.6f : 7.4f;
            var radial = new Vector3(Mathf.Sin(angle) * radius, portrait ? 2.5f : 1.95f, Mathf.Cos(angle) * radius);
            transform.position = Vector3.Lerp(transform.position, target.position + radial, blend * 0.45f);
            var focus = target.position + Vector3.up * (portrait ? -0.10f : HomeScreen ? 0.35f : 0.75f);
            // Place the live car beside the menu on wide screens.
            focus -= transform.right * (portrait ? 0f : 1.65f);
            LookAt(focus, blend * 0.5f);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, portrait ? 46f : 43f, blend);
        }

        private void LookAt(Vector3 point, float blend)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(point - transform.position, Vector3.up), blend);
        }
    }
}
