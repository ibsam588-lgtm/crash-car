using System.Collections;
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
        public float LastImpactRealtime { get; private set; } = -10f;
        public bool SlowMotionActive => _slowMotionRoutine != null;

        private Coroutine _slowMotionRoutine;
        private float _nextImpactRealtime;

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.realtimeSinceStartup < _nextImpactRealtime ||
                collision.collider.name.StartsWith("Damage_", System.StringComparison.Ordinal))
            {
                return;
            }

            var impactKmh = Mathf.Min(collision.relativeVelocity.magnitude * 3.6f, 220f);
            if (impactKmh < minimumImpactKmh)
            {
                return;
            }

            _nextImpactRealtime = Time.realtimeSinceStartup + impactCooldownSeconds;
            Hits += 1;
            Damage = Mathf.Clamp(Damage + impactKmh * 0.0125f, 0f, 100f);
            LastImpactKmh = impactKmh;
            LastImpactScore = Mathf.RoundToInt(impactKmh * scorePerKmh * (1f + Mathf.Min(Hits - 1, 8) * 0.16f));
            Score += LastImpactScore;
            LastImpactRealtime = Time.realtimeSinceStartup;

            var contact = collision.GetContact(0);
            KickLooseParts(contact.point, collision.relativeVelocity.normalized, impactKmh);

            if (_slowMotionRoutine != null)
            {
                StopCoroutine(_slowMotionRoutine);
            }
            _slowMotionRoutine = StartCoroutine(SlowMotionBurst());
        }

        private void KickLooseParts(Vector3 point, Vector3 direction, float impactKmh)
        {
            var detachable = GetComponentsInChildren<Transform>(true);
            foreach (var part in detachable)
            {
                if (!part.name.StartsWith("Damage_", System.StringComparison.Ordinal) ||
                    part.name.StartsWith("Damage_Windshield_Crack", System.StringComparison.Ordinal))
                {
                    continue;
                }

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
            }
        }

        private IEnumerator SlowMotionBurst()
        {
            Time.timeScale = slowMotionScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return new WaitForSecondsRealtime(slowMotionSeconds);
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            _slowMotionRoutine = null;
        }

    }
}
