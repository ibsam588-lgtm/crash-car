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
        public bool IsDown { get; private set; }
        public bool IsLimping => !IsDown && Time.time < _limpUntil;
        public float DistanceWalked { get; private set; }
        public string LastObstacle { get; private set; }

        private Rigidbody _body;
        private Animation _animation;
        private PedestrianGait _gait;
        private Renderer[] _renderers;
        private Rigidbody _carBody;
        private PedestrianRagdoll _ragdoll;
        private Vector3 _destination, _previous, _escape;
        private float _stumbleUntil, _limpUntil, _nextVisibility, _runUntil, _nextEscape, _nextRouteChange;
        private readonly RaycastHit[] _obstacles = new RaycastHit[12];

        private void Start()
        {
            _body = GetComponent<Rigidbody>();
            _animation = GetComponentInChildren<Animation>();
            _renderers = GetComponentsInChildren<Renderer>();
            if (player != null) _carBody = player.GetComponent<Rigidbody>();
            _destination = Vector3.Dot(transform.forward, pointB - pointA) > 0 ? pointB : pointA;
            _previous = transform.position;
            if (_animation != null && walkClip != null)
                _gait = new PedestrianGait(_animation, walkClip, sourceWalkSpeed);
        }

        private void FixedUpdate()
        {
            if (IsDown) return;
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
            if (VehicleApproaching(_carBody, position))
            {
                if (Time.time >= _runUntil || (Time.time >= _nextEscape && (_escape - position).sqrMagnitude < .65f))
                {
                    _escape = EscapePoint(position);
                    _nextEscape = Time.time + .85f;
                }
                _runUntil = Time.time + 1.8f;
            }
            Yielding = Time.time < _runUntil;
            if (Yielding)
            {
                delta = _escape - position;
                delta.y = 0;
            }
            LastObstacle = "";
            var blocked = Blocked(position, delta, Yielding ? .95f : .65f, out var obstacle);
            if (blocked)
            {
                LastObstacle = obstacle.name;
                if (!Yielding && obstacle.attachedRigidbody == null && Time.time >= _nextRouteChange)
                {
                    _destination = _destination == pointA ? pointB : pointA;
                    _nextRouteChange = Time.time + .55f;
                }
                else if (Yielding && Time.time >= _nextEscape)
                {
                    _escape = EscapePoint(position);
                    _nextEscape = Time.time + .55f;
                }
            }
            var heading = _body.rotation;
            var pace = (Yielding ? 4.25f : walkSpeed) * (IsLimping ? .46f : 1f);
            var speed = blocked ? 0f : PedestrianGait.FacingSpeed(ref heading, delta, pace, Yielding ? 330f : 250f, Time.fixedDeltaTime);
            var desired = (heading * Vector3.forward) * speed;
            var velocity = _body.linearVelocity;
            var planar = Vector3.MoveTowards(new Vector3(velocity.x, 0, velocity.z), desired, (Yielding ? 9f : 4f) * Time.fixedDeltaTime);
            _body.linearVelocity = new Vector3(planar.x, velocity.y, planar.z);
            _body.MoveRotation(heading);
            _gait?.SetSpeed(planar.magnitude);
        }

        private Vector3 EscapePoint(Vector3 position)
        {
            if (_carBody == null) return position;
            var start = Mathf.Min(pointA.z, pointB.z);
            var end = Mathf.Max(pointA.z, pointB.z);
            var threat = _carBody.position + Vector3.ProjectOnPlane(_carBody.linearVelocity, Vector3.up) * .38f;
            var side = Mathf.Sign(pointA.x - avenueCenterX);
            var away = Mathf.Sign(position.z - threat.z);
            var lane = Mathf.Abs(threat.x - avenueCenterX) < 11.25f ? 11.8f : 10.9f;
            var safest = position;
            var best = float.NegativeInfinity;
            for (var direction = -1; direction <= 1; direction += 2)
            {
                var z = Mathf.Clamp(position.z + direction * 7f, start, end);
                var point = new Vector3(avenueCenterX + side * lane, position.y, z);
                var delta = point - position;
                if (delta.sqrMagnitude < .65f || Blocked(position, delta, .7f, out _)) continue;
                var score = Vector3.Distance(threat, point) + (direction == away ? 1f : 0f)
                    + Vector3.Dot(transform.forward, delta.normalized) * .7f;
                if (score <= best) continue;
                best = score;
                safest = point;
            }
            return safest;
        }

        internal static bool VehicleApproaching(Rigidbody car,Vector3 position)
        {
            if(car==null)return false;
            var delta=Vector3.ProjectOnPlane(position-car.position,Vector3.up);
            var movement=Vector3.ProjectOnPlane(car.linearVelocity,Vector3.up);
            var speed=movement.magnitude;
            if(speed<1.6f)return false;
            var radius=Mathf.Min(22f,6.5f+speed*.40f);
            if(delta.sqrMagnitude>radius*radius||Vector3.Dot(delta,movement)<-.1f)return false;
            var reach=Mathf.Clamp(Vector3.Dot(delta,movement)/movement.sqrMagnitude,0f,1.25f);
            return (delta-movement*reach).sqrMagnitude<28f;
        }

        private bool Blocked(Vector3 position, Vector3 delta, float distance, out Collider obstacle)
        {
            obstacle = null;
            if (delta.sqrMagnitude < .06f) return false;
            var count = Physics.SphereCastNonAlloc(position + Vector3.up * 0.85f, 0.23f, delta.normalized,
                _obstacles, Mathf.Min(distance, delta.magnitude), ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var collider = _obstacles[i].collider;
                if (collider.attachedRigidbody == _body || collider.transform.IsChildOf(transform)) continue;
                if (collider.bounds.max.y < position.y + 0.4f) continue;
                obstacle = collider;
                return true;
            }
            return false;
        }

        private void LateUpdate()
        {
            if (IsDown && _ragdoll != null && _ragdoll.HasBody) _ragdoll.ApplyPose();
            if (Time.unscaledTime < _nextVisibility || _renderers == null) return;
            _nextVisibility = Time.unscaledTime + 0.3f;
            var distance = player != null ? Vector3.Distance(transform.position, player.position) : 0f;
            foreach (var renderer in _renderers)
            {
                renderer.enabled = distance < 100f;
                renderer.shadowCastingMode = distance < 38f ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _gait?.SetVisible(!IsDown && distance < 100f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsDown || collision.rigidbody == null || collision.rigidbody.GetComponent<ArcadeVehicleController>() == null) return;
            var speed = collision.relativeVelocity.magnitude * 3.6f;
            if (speed < 3f) return;
            if (speed < 25f)
            {
                _stumbleUntil = Time.time + .48f;
                _limpUntil = Time.time + 5f;
                _gait?.SetSpeed(.2f);
                return;
            }

            IsDown = true;
            Yielding = false;
            var direction = Vector3.ProjectOnPlane(collision.rigidbody.linearVelocity, Vector3.up).normalized;
            if (direction.sqrMagnitude < .1f) direction = (transform.position - collision.rigidbody.position).normalized;
            var push = direction * Mathf.Clamp(speed * .07f, 1.5f, 6f) + Vector3.up * .45f;
            if (_animation != null)
            {
                _animation.enabled = false;
                _ragdoll = new PedestrianRagdoll(transform, _animation, push);
            }
            if (_ragdoll != null && _ragdoll.HasBody)
            {
                GetComponent<CapsuleCollider>().enabled = false;
                _body.isKinematic = true;
                return;
            }
            _body.constraints = RigidbodyConstraints.None;
            _body.linearVelocity = push;
            _body.angularVelocity = Vector3.Cross(Vector3.up, direction) * 2.6f;
        }
    }
}
