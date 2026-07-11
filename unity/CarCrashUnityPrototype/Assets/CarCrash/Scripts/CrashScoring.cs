using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CrashScoring : MonoBehaviour
    {
        public int scorePerKmh = 42;
        public float slowMotionScale = 0.24f;
        public float slowMotionSeconds = 0.62f;
        public float minimumImpactKmh = 28f;
        public float impactCooldownSeconds = 0.14f;

        public int Score { get; private set; }
        public int Hits { get; private set; }
        public float Damage { get; private set; }
        public int LastImpactScore { get; private set; }
        public float LastImpactKmh { get; private set; }
        public int LastChainDepth { get; private set; }
        public int PeakChainDepth { get; private set; }
        public float LastMultiplier { get; private set; } = 1f;
        public float LastImpactRealtime { get; private set; } = -10f;
        public bool SlowMotionActive => _slowMotionRoutine != null;

        private Coroutine _slowMotionRoutine;
        public void RegisterTrafficImpact(
            TrafficImpactTarget target,
            Vector3 point,
            Vector3 relativeVelocity,
            float impactKmh,
            int chainDepth)
        {
            if (target == null || impactKmh < minimumImpactKmh)
            {
                return;
            }

            Hits += 1;
            LastChainDepth = Mathf.Clamp(chainDepth, 0, 8);
            PeakChainDepth = Mathf.Max(PeakChainDepth, LastChainDepth);
            LastMultiplier = 1f + Mathf.Min(LastChainDepth, 6) * 0.48f + Mathf.Min(Hits - 1, 8) * 0.10f;
            Damage = Mathf.Clamp(Damage + impactKmh * 0.011f * (1f + LastChainDepth * 0.10f), 0f, 100f);
            LastImpactKmh = impactKmh;
            LastImpactScore = Mathf.RoundToInt(impactKmh * scorePerKmh * LastMultiplier + LastChainDepth * 520f);
            Score += LastImpactScore;
            LastImpactRealtime = Time.realtimeSinceStartup;

            if (LastChainDepth == 0)
            {
                var direction = relativeVelocity.sqrMagnitude > 0.01f ? relativeVelocity.normalized : transform.forward;
                KickLooseParts(point, direction, impactKmh);
            }

            if (_slowMotionRoutine != null)
            {
                StopCoroutine(_slowMotionRoutine);
            }
            _slowMotionRoutine = StartCoroutine(SlowMotionBurst(LastChainDepth));

            Debug.Log($"[CrashChain] vehicle={target.name} depth={LastChainDepth} impact={impactKmh:F1} score={LastImpactScore} total={Score}");
        }

        private void KickLooseParts(Vector3 point, Vector3 direction, float impactKmh)
        {
            var detachable = new List<Transform>();
            foreach (var part in GetComponentsInChildren<Transform>(true))
            {
                if (!part.name.StartsWith("Damage_", System.StringComparison.Ordinal) ||
                    part.name.StartsWith("Damage_Windshield_Crack", System.StringComparison.Ordinal))
                {
                    continue;
                }

                detachable.Add(part);
            }

            detachable.Sort((left, right) =>
                Vector3.Distance(left.position, point).CompareTo(Vector3.Distance(right.position, point)));
            var detachCount = Mathf.Clamp(1 + Mathf.FloorToInt((impactKmh - 65f) / 55f), 1, 3);
            for (var index = 0; index < Mathf.Min(detachCount, detachable.Count); index++)
            {
                var part = detachable[index];

                part.SetParent(null, true);
                if (part.GetComponent<Collider>() == null)
                {
                    var collider = part.gameObject.AddComponent<BoxCollider>();
                    collider.size = Vector3.one;
                }

                var loose = part.GetComponent<Rigidbody>();
                if (loose == null)
                {
                    loose = part.gameObject.AddComponent<Rigidbody>();
                    loose.mass = 14f;
                    loose.interpolation = RigidbodyInterpolation.Interpolate;
                    loose.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }

                loose.AddExplosionForce(impactKmh * 2.4f, point - direction * 0.35f, 3.2f, 0.3f, ForceMode.Impulse);
                Destroy(part.gameObject, 7f);
            }
        }

        private IEnumerator SlowMotionBurst(int chainDepth)
        {
            var depthScale = Mathf.Lerp(slowMotionScale, 0.34f, Mathf.Clamp01(chainDepth / 5f));
            var depthDuration = Mathf.Lerp(slowMotionSeconds, slowMotionSeconds * 0.72f, Mathf.Clamp01(chainDepth / 5f));
            Time.timeScale = depthScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return new WaitForSecondsRealtime(depthDuration);
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            _slowMotionRoutine = null;
        }

    }
}
