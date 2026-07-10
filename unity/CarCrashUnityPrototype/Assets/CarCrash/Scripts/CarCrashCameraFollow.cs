using UnityEngine;

namespace CarCrash
{
    public sealed class CarCrashCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new(0f, 4.2f, -8.2f);
        public float followSharpness = 7f;
        public float lookAhead = 5f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desired = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            var lookTarget = target.position + target.forward * lookAhead + Vector3.up * 1.1f;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookTarget - transform.position, Vector3.up),
                1f - Mathf.Exp(-followSharpness * Time.deltaTime)
            );
        }
    }
}
