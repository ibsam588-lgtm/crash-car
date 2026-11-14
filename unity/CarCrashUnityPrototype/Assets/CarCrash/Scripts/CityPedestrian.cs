using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class CityPedestrian : MonoBehaviour
    {
        public Vector3 pointA, pointB;
        public float walkSpeed = 1.3f;
        public float sourceWalkSpeed = 1.3f;
        public AnimationClip walkClip;
        public Transform player;
        public float avenueCenterX;
        public bool Yielding { get; private set; }
        public float DistanceWalked { get; private set; }
        public string LastObstacle { get; private set; }

        private Rigidbody _body;
        private Animation _animation;
        private Renderer[] _renderers;
        private Vector3 _destination, _previous;
        private float _stumbleUntil, _nextVisibility;
        private readonly RaycastHit[] _obstacles = new RaycastHit[12];

        private void Start()
        {
            _body = GetComponent<Rigidbody>();
            _animation = GetComponentInChildren<Animation>();
            _renderers = GetComponentsInChildren<Renderer>();
            _destination = Vector3.Dot(transform.forward, pointB - pointA) > 0 ? pointB : pointA;
            _previous = transform.position;
            if (_animation != null && walkClip != null)
            {
                // Legacy clip aliases are runtime state and do not survive prefab saving.
                _animation.AddClip(walkClip, "Walk");
                _animation["Walk"].wrapMode = WrapMode.Loop;
                _animation.Play("Walk");
            }
        }

        private void FixedUpdate()
        {
            DistanceWalked += Vector3.Distance(_body.position, _previous);
            _previous = _body.position;
            if (Time.time < _stumbleUntil) return;

            var position = _body.position;
            var delta = _destination - position;
            delta.y = 0;
            if (delta.magnitude < 0.35f)
            {
                _destination = Vector3.Distance(_destination, pointA) < 1f ? pointB : pointA;
                delta = _destination - position;
                delta.y = 0;
            }
            Yielding = false;
            if (player != null)
            {
                var toCar = player.position - position;
                toCar.y = 0;
                var car = player.GetComponent<ArcadeVehicleController>();
                var carRoadX = player.position.x - avenueCenterX;
                if (toCar.magnitude < 5.5f && Mathf.Abs(carRoadX) > 8f && car != null && car.SpeedKmh > 5f)
                {
                    // Step aside within the sidewalk, rather than walking into traffic.
                    var side = Mathf.Sign(pointA.x-avenueCenterX);
                    var safeX = avenueCenterX + (Mathf.Abs(carRoadX) < 10.7f ? side * 11.8f : side * 9.5f);
                    delta = new Vector3(safeX - position.x, 0, Mathf.Clamp(position.z - player.position.z, -1f, 1f));
                    Yielding = true;
                }
            }
            var desired = delta.normalized * (Yielding ? 2.2f : walkSpeed);
            LastObstacle = "";
            var count = Physics.SphereCastNonAlloc(position + Vector3.up * 0.85f, 0.23f, delta.normalized,
                _obstacles, 0.65f, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var collider = _obstacles[i].collider;
                if (collider.attachedRigidbody == _body || collider.transform.IsChildOf(transform)) continue;
                if (collider.bounds.max.y < position.y + 0.4f) continue;
                desired = Vector3.zero;
                LastObstacle = collider.name;
                if (collider.attachedRigidbody == null) _destination = _destination == pointA ? pointB : pointA;
                break;
            }
            var velocity = _body.linearVelocity;
            var planar = Vector3.MoveTowards(new Vector3(velocity.x, 0, velocity.z), desired, 4f * Time.fixedDeltaTime);
            _body.linearVelocity = new Vector3(planar.x, velocity.y, planar.z);
            if (planar.sqrMagnitude > 0.05f)
                _body.MoveRotation(Quaternion.Slerp(_body.rotation, Quaternion.LookRotation(planar), Time.fixedDeltaTime * 7f));
            if (_animation != null && _animation["Walk"] != null)
                _animation["Walk"].speed = planar.magnitude / sourceWalkSpeed;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < _nextVisibility || _renderers == null) return;
            _nextVisibility = Time.unscaledTime + 0.3f;
            var distance = player != null ? Vector3.Distance(transform.position, player.position) : 0f;
            foreach (var renderer in _renderers)
            {
                renderer.enabled = distance < 100f;
                renderer.shadowCastingMode = distance < 38f ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (_animation != null) _animation.enabled = distance < 100f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody != null && collision.rigidbody.GetComponent<ArcadeVehicleController>() != null)
                _stumbleUntil = Time.time + 1.2f;
        }
    }
}
