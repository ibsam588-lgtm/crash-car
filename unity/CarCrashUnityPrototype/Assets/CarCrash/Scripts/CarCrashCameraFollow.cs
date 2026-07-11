using UnityEngine;

namespace CarCrash
{
    public sealed class CarCrashCameraFollow : MonoBehaviour
    {
        public Transform target;
        public CrashScoring scoring;
        public Vector3 offset = new(0f, 4.2f, -8.2f);
        public float followSharpness = 7f;
        public float lookAhead = 5f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            var desired = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * dt));
            var lookTarget = target.position + target.forward * lookAhead + Vector3.up * 1.1f;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookTarget - transform.position, Vector3.up),
                1f - Mathf.Exp(-followSharpness * dt)
            );

            if (scoring == null)
            {
                return;
            }

            var sinceImpact = Time.realtimeSinceStartup - scoring.LastImpactRealtime;
            if (sinceImpact < 0f || sinceImpact > 0.52f)
            {
                return;
            }

            var decay = 1f - sinceImpact / 0.52f;
            var strength = Mathf.Clamp01(scoring.LastImpactKmh / 150f) * Mathf.Lerp(0.04f, 0.19f, decay);
            strength *= 1f + Mathf.Min(scoring.LastChainDepth, 4) * 0.06f;
            var noise = new Vector3(
                Mathf.Sin(Time.unscaledTime * 67f),
                Mathf.Cos(Time.unscaledTime * 83f) * 0.55f,
                Mathf.Sin(Time.unscaledTime * 53f) * 0.35f);
            transform.position += noise * strength;
        }
    }
}
