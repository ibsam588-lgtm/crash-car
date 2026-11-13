using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MovingTrafficObstacle : MonoBehaviour
    {
        public Vector3 velocity = new(0f, 0f, -5f);
        public float resetBehindZ = -24f;
        public float resetAheadZ = 92f;
        public float laneJitter = 0.25f;
        public float swaySpeed = 0.8f;
        public float impactVelocityTransfer = 0.68f;
        public float chainLateralBias;
        public float launchDelaySeconds = 5.6f;
        public Transform observer;

        public bool IsReleased => _released;
        public bool IsCruising => !_released && Time.time >= _launchAt;
        public float CurrentCruiseSpeed { get; private set; }
        public int ChainDepth { get; private set; }
        public Rigidbody Body => _body;

        private Rigidbody _body;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private bool _released;
        private float _phase;
        private float _launchAt;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            _phase = Random.value * Mathf.PI * 2f;
            _launchAt = Time.time + launchDelaySeconds;
        }

        private void FixedUpdate()
        {
            if (_released)
            {
                return;
            }

            if (Time.time < _launchAt)
            {
                _body.MovePosition(_startPosition);
                _body.MoveRotation(_startRotation);
                return;
            }

            var heading = velocity.sqrMagnitude > 0.01f ? velocity.normalized : transform.forward;
            var targetSpeed = velocity.magnitude;
            var box = GetComponent<BoxCollider>();
            var halfLength = box != null ? box.size.z * 0.5f : 2.4f;
            var halfWidth = box != null ? box.size.x * 0.4f : 0.85f;
            var lookDistance = Mathf.Max(5f, targetSpeed * 1.4f);
            var origin = _body.position + Vector3.up * 0.7f + heading * (halfLength + 0.15f);
            foreach (var hit in Physics.BoxCastAll(origin, new Vector3(halfWidth, 0.4f, 0.1f), heading,
                Quaternion.LookRotation(heading), lookDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.attachedRigidbody == _body || hit.collider is MeshCollider) continue;
                // Kinematic traffic must brake for a stopped player or another car,
                // rather than driving through it with effectively infinite mass.
                targetSpeed = Mathf.Min(targetSpeed, Mathf.Max(0, hit.distance - 2.2f) * 0.7f);
            }
            CurrentCruiseSpeed = Mathf.MoveTowards(CurrentCruiseSpeed, targetSpeed,
                (targetSpeed < CurrentCruiseSpeed ? 14f : 2.8f) * Time.fixedDeltaTime);
            var next = _body.position + heading * (CurrentCruiseSpeed * Time.fixedDeltaTime);
            next.x = _startPosition.x + Mathf.Sin(Time.time * swaySpeed + _phase) * laneJitter;

            var behind = observer != null ? Mathf.Max(-115f, observer.position.z - 85f) : resetBehindZ;
            var ahead = observer != null ? Mathf.Min(320f, observer.position.z + 150f) : resetAheadZ;
            if (next.z < behind)
            {
                next.z = ahead - Random.Range(0f, 16f);
                RecycleAt(next);
                return;
            }
            else if (next.z > ahead)
            {
                next.z = behind + Random.Range(0f, 12f);
                RecycleAt(next);
                return;
            }

            _body.MovePosition(next);
        }

        private void RecycleAt(Vector3 position)
        {
            // MovePosition performs a continuous sweep. A long pool relocation
            // must teleport off camera instead, or it creates phantom road impacts.
            _body.position = position;
            CurrentCruiseSpeed = 0f;
        }

        public bool ReleaseFromImpact(
            Rigidbody striker,
            Vector3 contactPoint,
            Vector3 collisionNormal,
            float impactKmh,
            int chainDepth)
        {
            if (_released)
            {
                return false;
            }

            _released = true;
            ChainDepth = Mathf.Max(0, chainDepth);
            _body.isKinematic = false;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var strikerVelocity = striker != null
                ? striker.linearVelocity
                : -collisionNormal * (impactKmh / 3.6f);
            var chainTransfer = impactVelocityTransfer * (ChainDepth > 0 ? 0.88f : 1f);
            var inheritedVelocity = Vector3.Lerp(velocity, strikerVelocity, Mathf.Clamp01(chainTransfer));
            var impactMetersPerSecond = Mathf.Clamp(impactKmh / 3.6f, 3f, 48f);
            inheritedVelocity.x += chainLateralBias * impactMetersPerSecond * 0.34f;
            inheritedVelocity.y = Mathf.Clamp(inheritedVelocity.y, -0.3f, 0.4f);
            _body.linearVelocity = Vector3.ClampMagnitude(inheritedVelocity, 48f);

            var impulseDirection = (-collisionNormal + Vector3.right * chainLateralBias * 0.62f + Vector3.up * 0.08f).normalized;
            var impulse = impulseDirection * (_body.mass * impactMetersPerSecond * 0.065f);
            _body.AddForceAtPosition(impulse, contactPoint, ForceMode.Impulse);

            var spinSign = Mathf.Abs(chainLateralBias) > 0.05f
                ? Mathf.Sign(chainLateralBias)
                : (GetInstanceID() % 2 == 0 ? 1f : -1f);
            _body.AddTorque(
                new Vector3(impactMetersPerSecond * 0.008f, spinSign * impactMetersPerSecond * 0.08f, spinSign * impactMetersPerSecond * 0.018f) * _body.mass,
                ForceMode.Impulse);
            return true;
        }

        public void ResetTraffic()
        {
            _released = false;
            CurrentCruiseSpeed = 0f;
            ChainDepth = 0;
            _body.isKinematic = true;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_startPosition, _startRotation);
        }
    }
}
