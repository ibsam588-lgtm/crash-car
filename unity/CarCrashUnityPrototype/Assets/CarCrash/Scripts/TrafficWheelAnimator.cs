using UnityEngine;

namespace CarCrash
{
    public sealed class TrafficWheelAnimator : MonoBehaviour
    {
        public Transform[] wheels = System.Array.Empty<Transform>();
        public float speedMetersPerSecond = 6f;
        public float wheelRadius = 0.38f;

        private float _spin;
        private Rigidbody _body;
        private MovingTrafficObstacle _mover;
        private Quaternion[] _baseRotations = System.Array.Empty<Quaternion>();

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _mover = GetComponent<MovingTrafficObstacle>();
            _baseRotations = new Quaternion[wheels.Length];
            for (var i = 0; i < wheels.Length; i++)
            {
                _baseRotations[i] = wheels[i] != null ? wheels[i].localRotation : Quaternion.identity;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            var currentSpeed = _body != null && !_body.isKinematic
                ? _body.linearVelocity.magnitude
                : _mover != null ? _mover.CurrentCruiseSpeed : speedMetersPerSecond;
            var angularDegrees = Mathf.Rad2Deg * currentSpeed / Mathf.Max(0.05f, wheelRadius);
            _spin -= angularDegrees * Time.deltaTime;
            for (var i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                {
                    wheels[i].localRotation = _baseRotations[i] * Quaternion.Euler(_spin, 0f, 0f);
                }
            }
        }
    }
}
