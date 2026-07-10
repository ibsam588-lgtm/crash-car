using UnityEngine;

namespace CarCrash
{
    public sealed class TrafficWheelAnimator : MonoBehaviour
    {
        public Transform[] wheels = System.Array.Empty<Transform>();
        public float speedMetersPerSecond = 6f;
        public float wheelRadius = 0.38f;

        private float _spin;
        private Quaternion[] _baseRotations = System.Array.Empty<Quaternion>();

        private void Awake()
        {
            _baseRotations = new Quaternion[wheels.Length];
            for (var i = 0; i < wheels.Length; i++)
            {
                _baseRotations[i] = wheels[i] != null ? wheels[i].localRotation : Quaternion.identity;
            }
        }

        private void Update()
        {
            var angularDegrees = Mathf.Rad2Deg * speedMetersPerSecond / Mathf.Max(0.05f, wheelRadius);
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
