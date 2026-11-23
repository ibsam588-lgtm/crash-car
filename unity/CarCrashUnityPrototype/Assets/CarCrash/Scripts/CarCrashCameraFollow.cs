using System.Collections.Generic;
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
        public int LastOccludedVehicleCount { get; private set; }

        private Camera _camera;
        private ArcadeVehicleController _vehicle;
        private VehicleGarage _garage;
        private VehicleImpactEffects _effects;
        private Vector3 VehicleSize => _garage != null && _garage.Current != null ? _garage.Current.size : new Vector3(2.2f,1.3f,4.65f);
        private float _orbit = 38f;
        private Vector3 _pointer;
        private bool _orbitDragging;
        private float _transitionUntil;
        private float _orbitDirection = 1f;
        private bool _autoOrbit = true;
        private readonly RaycastHit[] _obstructions = new RaycastHit[64];
        private readonly List<Renderer> _showcaseHiddenTraffic = new();

        private readonly List<(Collider collider,Renderer[] renderers)> _trafficBodies=new();
        private readonly List<(Renderer renderer,bool forced)> _nearCameraHidden=new();
        private void Awake()
        {
            _camera=GetComponent<Camera>();
            foreach(var traffic in FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))
                _trafficBodies.Add((traffic.GetComponent<Collider>(),traffic.GetComponentsInChildren<Renderer>(true)));
        }

        private void OnPreCull()
        {
            RestoreNearCameraTraffic();
            LastOccludedVehicleCount=0;
            foreach(var traffic in _trafficBodies)
            {
                if(traffic.collider==null||!traffic.collider.gameObject.activeInHierarchy||traffic.collider.bounds.SqrDistance(transform.position)>1.2f*1.2f)continue;
                LastOccludedVehicleCount++;
                foreach(var renderer in traffic.renderers)
                {
                    if(renderer==null)continue;
                    _nearCameraHidden.Add((renderer,renderer.forceRenderingOff));renderer.forceRenderingOff=true;
                }
            }
        }
        private void OnPostRender()=>RestoreNearCameraTraffic();
        private void OnDisable()=>RestoreNearCameraTraffic();
        private void RestoreNearCameraTraffic()
        {
            foreach(var hidden in _nearCameraHidden)if(hidden.renderer!=null)hidden.renderer.forceRenderingOff=hidden.forced;
            _nearCameraHidden.Clear();
        }

        public void SetShowcase(bool enabled)
        {
            foreach (var renderer in _showcaseHiddenTraffic) if (renderer != null) renderer.enabled = true;
            _showcaseHiddenTraffic.Clear();
            if (enabled && target != null)
            {
                foreach (var traffic in FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))
                {
                    if (Vector3.Distance(traffic.transform.position, target.position) > 12f) continue;
                    foreach (var renderer in traffic.GetComponentsInChildren<Renderer>())
                    {
                        if (!renderer.enabled) continue;
                        _showcaseHiddenTraffic.Add(renderer);
                        renderer.enabled = false;
                    }
                }
            }
            Showcasing = enabled;
            if (!enabled) HomeScreen = false;
            _orbitDragging = false;
            _transitionUntil = Time.unscaledTime + 2f;
            if (target != null) _vehicle = target.GetComponent<ArcadeVehicleController>();
            if (target != null) { _garage = target.GetComponent<VehicleGarage>(); _effects = target.GetComponent<VehicleImpactEffects>(); }
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
            var size = VehicleSize;
            if (size.y > 1.6f || size.z > 5f)
            {
                localOffset.y += Mathf.Max(0,size.y-1.3f)*.65f;
                if (View != DrivingView.Bonnet) localOffset.z -= Mathf.Max(0,size.z-4.65f)*1.2f;
                else localOffset.z = size.z*.32f;
            }
            if (_camera.aspect < 1.4f && View != DrivingView.Bonnet)
            {
                // Preserve the coupe's screen width when the horizontal FOV narrows.
                localOffset.z *= Mathf.Clamp(0.8f / _camera.aspect, 1f, 2f);
                localOffset.y *= 1.25f;
            }
            var desired = ClearCameraPosition(target.TransformPoint(localOffset));
            var relocated = (transform.position - desired).sqrMagnitude > 55f * 55f;
            transform.position = relocated ? desired : ClearCameraPosition(Vector3.Lerp(transform.position, desired, blend));
            LookAt(target.position + target.forward * lookAhead + Vector3.up * 1.1f, relocated ? 1f : blend);
            var speed = _vehicle != null ? Mathf.Clamp01(_vehicle.SpeedKmh / 200f) : 0f;
            var boost = _vehicle != null && _vehicle.BoostActive ? 6f : 0f;
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, 54f + speed * 12f + boost, blend * 0.6f);

            var sinceImpact = scoring != null ? Time.realtimeSinceStartup - scoring.LastImpactRealtime : 10f;
            if (_effects != null) sinceImpact = Mathf.Min(sinceImpact,Time.unscaledTime-_effects.LastImpactRealtime);
            if (sinceImpact < 0f || sinceImpact > 0.52f) return;
            var decay = 1f - sinceImpact / 0.52f;
            var impactSpeed = Mathf.Max(scoring != null ? scoring.LastImpactKmh : 0f,_effects != null ? _effects.LastImpactKmh : 0f);
            var strength = Mathf.Clamp01(impactSpeed / 150f) * 0.22f * decay;
            transform.position += new Vector3(Mathf.Sin(Time.unscaledTime * 67f), Mathf.Cos(Time.unscaledTime * 83f) * 0.55f, 0f) * strength;
            transform.position = ClearCameraPosition(transform.position);
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
                if (_orbit > 68f) _orbitDirection = -1f;
                if (_orbit < 24f) _orbitDirection = 1f;
                _orbit += dt * 1.5f * _orbitDirection;
            }

            var portrait = _camera.aspect < 1.4f;
            var angle = _orbit * Mathf.Deg2Rad;
            // Keep the whole body in the much narrower horizontal field of view on phones.
            var radius = portrait ? Mathf.Clamp(6.6f / _camera.aspect, 8.5f, 20f) : HomeScreen ? 8.6f : 7.4f;
            var size = VehicleSize;
            radius *= Mathf.Max(1f,size.z/4.65f,size.y/2.1f);
            // Large trucks still need a street-sized orbit. Pulling far outside the
            // road can put a building between the lens and the vehicle on phones.
            if (portrait) radius = Mathf.Min(radius,12f);
            var radial = new Vector3(Mathf.Sin(angle) * radius, (portrait ? 2.5f : 1.95f)+Mathf.Max(0,size.y-1.3f)*.6f, Mathf.Cos(angle) * radius);
            var desired = ClearCameraPosition(target.position + radial);
            transform.position = ClearCameraPosition(Vector3.Lerp(transform.position, desired, blend * 0.45f));
            var focusHeight = portrait ? HomeScreen ? -.1f : -1.45f : HomeScreen ? .35f : .1f;
            focusHeight += Mathf.Max(0,size.y-1.3f)*(!HomeScreen ? .1f : .45f);
            var focus = target.position + Vector3.up * focusHeight;
            // Place the live car beside the menu on wide screens.
            var framingRight = Vector3.Cross(Vector3.up, (focus - transform.position).normalized).normalized;
            focus -= framingRight * (portrait ? 0f : 1.65f);
            // Position already eases around the orbit; aim at the car throughout
            // fast drags so it cannot leave the frame while rotation catches up.
            LookAt(focus, 1f);
            var clearDistance = Vector3.Distance(transform.position, target.position);
            var baseFov = portrait ? 46f : 43f;
            var framingFov = 2f * Mathf.Atan(Mathf.Tan(baseFov * Mathf.Deg2Rad * 0.5f) * radius / Mathf.Max(3f, clearDistance)) * Mathf.Rad2Deg;
            if (portrait)
            {
                var bodyWidth = Mathf.Abs(Mathf.Sin(angle))*size.z+Mathf.Abs(Mathf.Cos(angle))*size.x;
                framingFov = Mathf.Max(framingFov,2f*Mathf.Atan(bodyWidth/(2f*Mathf.Max(3f,clearDistance)*_camera.aspect*.85f))*Mathf.Rad2Deg);
            }
            if (portrait && !HomeScreen)
                framingFov = Mathf.Max(framingFov,2f*Mathf.Atan((size.y+size.z*.25f)/(2f*Mathf.Max(3f,clearDistance)*.23f))*Mathf.Rad2Deg);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, Mathf.Clamp(framingFov, baseFov, portrait ? 95f : 75f), blend);
        }

        private Vector3 ClearCameraPosition(Vector3 desired)
        {
            var origin = target.position + Vector3.up * Mathf.Max(1.05f,VehicleSize.y*.6f);
            var delta = desired - origin;
            var distance = delta.magnitude;
            if (distance < 0.05f) return desired;
            var count = Physics.SphereCastNonAlloc(origin, 0.28f, delta / distance, _obstructions,
                distance, ~0, QueryTriggerInteraction.Ignore);
            var clearance = distance;
            for (var i = 0; i < count; i++)
            {
                var hit = _obstructions[i];
                // Player, traffic and detached bodywork should not pull the camera in.
                if (hit.collider.attachedRigidbody != null || hit.collider.transform.IsChildOf(target)) continue;
                clearance = Mathf.Min(clearance, Mathf.Max(0.55f, hit.distance - 0.1f));
            }
            return origin + delta / distance * clearance;
        }

        private void LookAt(Vector3 point, float blend)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(point - transform.position, Vector3.up), blend);
        }
    }
}
