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
        public float releaseImpulse = 0.18f;
        public float launchDelaySeconds = 5.6f;

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

            var next = _body.position + velocity * Time.fixedDeltaTime;
            next.x = _startPosition.x + Mathf.Sin(Time.time * swaySpeed + _phase) * laneJitter;

            if (next.z < resetBehindZ)
            {
                next.z = resetAheadZ + Random.Range(0f, 18f);
            }
            else if (next.z > resetAheadZ)
            {
                next.z = resetBehindZ - Random.Range(0f, 10f);
            }

            _body.MovePosition(next);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_released)
            {
                return;
            }

            if (collision.rigidbody == null || collision.rigidbody.GetComponent<ArcadeVehicleController>() == null)
            {
                return;
            }

            _released = true;
            _body.isKinematic = false;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.AddForce(velocity * _body.mass * releaseImpulse, ForceMode.Impulse);
            _body.AddTorque(Random.insideUnitSphere * _body.mass * 0.08f, ForceMode.Impulse);
        }

        public void ResetTraffic()
        {
            _released = false;
            _body.isKinematic = true;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_startPosition, _startRotation);
        }
    }
}
