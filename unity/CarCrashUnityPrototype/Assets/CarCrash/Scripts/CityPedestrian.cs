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
        private Renderer[] _renderers;
        private Rigidbody _carBody;
        private PedestrianRagdoll _ragdoll;
        private Vector3 _destination, _previous, _escape;
        private float _stumbleUntil, _limpUntil, _nextVisibility, _runUntil;
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
            {
                // Legacy clip aliases are runtime state and do not survive prefab saving.
                _animation.AddClip(walkClip, "Walk");
                _animation["Walk"].wrapMode = WrapMode.Loop;
                _animation.Play("Walk");
            }
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
            if (VehicleApproaching(_carBody,position))
            {
                if (Time.time >= _runUntil || (_escape-position).sqrMagnitude < .65f)
                    _escape = EscapePoint(position,_carBody.position);
                _runUntil = Time.time + 2.1f;
            }
            Yielding = Time.time < _runUntil;
            if(Yielding)
            {
                delta = _escape-position;
                delta.y = 0;
            }
            LastObstacle = "";
            var blocked = Blocked(position,delta);
            if(blocked&&Yielding)
            {
                var safer = new Vector3(avenueCenterX+Mathf.Sign(pointA.x-avenueCenterX)*11.78f,position.y,_escape.z);
                var inside = safer-position;
                if(!Blocked(position,inside))
                {
                    _escape = safer;
                    delta = inside;
                    blocked = false;
                }
            }
            var desired = blocked || delta.sqrMagnitude<.06f ? Vector3.zero : delta.normalized*(Yielding?4.2f:walkSpeed)*(IsLimping?.46f:1f);
            var velocity = _body.linearVelocity;
            var planar = Vector3.MoveTowards(new Vector3(velocity.x, 0, velocity.z), desired, (Yielding?10f:4f)*Time.fixedDeltaTime);
            _body.linearVelocity = new Vector3(planar.x, velocity.y, planar.z);
            if (planar.sqrMagnitude > 0.05f)
                _body.MoveRotation(Quaternion.Slerp(_body.rotation, Quaternion.LookRotation(planar), Time.fixedDeltaTime * 7f));
            if (_animation != null && _animation["Walk"] != null)
                _animation["Walk"].speed = planar.magnitude / Mathf.Max(.1f,sourceWalkSpeed);
        }

        private Vector3 EscapePoint(Vector3 position,Vector3 car)
        {
            var start = Mathf.Min(pointA.z,pointB.z);
            var end = Mathf.Max(pointA.z,pointB.z);
            var z = Mathf.Clamp(position.z+Mathf.Sign(position.z-car.z)*10f,start,end);
            if(Mathf.Abs(z-position.z)<.65f)z=Mathf.Abs(position.z-start)>Mathf.Abs(position.z-end)?start:end;
            var sidewalkSide = Mathf.Sign(pointA.x-avenueCenterX);
            var x = avenueCenterX+sidewalkSide*(Mathf.Abs(car.x-avenueCenterX)<11.05f?11.8f:9.6f);
            return new Vector3(x,position.y,z);
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

        private bool Blocked(Vector3 position,Vector3 delta)
        {
            if(delta.sqrMagnitude<.06f)return false;
            var count = Physics.SphereCastNonAlloc(position + Vector3.up * 0.85f, 0.23f, delta.normalized,
                _obstacles, Yielding?.95f:.65f, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var collider = _obstacles[i].collider;
                if (collider.attachedRigidbody == _body || collider.transform.IsChildOf(transform)) continue;
                if (collider.bounds.max.y < position.y + 0.4f) continue;
                LastObstacle = collider.name;
                if (!Yielding && collider.attachedRigidbody == null) _destination = _destination == pointA ? pointB : pointA;
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
            if (_animation != null) _animation.enabled = !IsDown && distance < 100f;
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
                if (_animation != null && _animation["Walk"] != null) _animation["Walk"].speed = .25f;
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
