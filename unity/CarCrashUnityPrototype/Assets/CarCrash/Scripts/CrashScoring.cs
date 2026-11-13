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
        public bool SimulationPaused { get; private set; }

        private Coroutine _slowMotionRoutine;
        private float _activeSlowMotionScale = 1f;

        // Pause owns the clock even when an impact coroutine is running.
        public void SetSimulationPaused(bool paused)
        {
            SimulationPaused = paused;
            ApplyTimeScale();
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = SimulationPaused ? 0f : (_slowMotionRoutine != null ? _activeSlowMotionScale : 1f);
            Time.fixedDeltaTime = 0.02f * (Time.timeScale > 0f ? Time.timeScale : 1f);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _slowMotionRoutine = null;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
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
            Damage = Mathf.Clamp(Damage + impactKmh * 0.08f * (1f + LastChainDepth * 0.10f), 0f, 100f);
            LastImpactKmh = impactKmh;
            LastImpactScore = Mathf.RoundToInt(impactKmh * scorePerKmh * LastMultiplier + LastChainDepth * 520f);
            Score += LastImpactScore;
            LastImpactRealtime = Time.realtimeSinceStartup;
            GetComponent<VehicleImpactEffects>()?.EmitImpact(point, -relativeVelocity.normalized, relativeVelocity, impactKmh);

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
            // Fender bumps can score and scuff the car without shedding a whole panel.
            if (impactKmh < 65f) return;
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
                    // Imported panels can carry a centimetre-to-metre transform.
                    // Build the shape in the part's local coordinates rather than
                    // turning a one-unit box into a vehicle-sized (or larger) wall.
                    var hasBounds = false;
                    var localBounds = new Bounds();
                    foreach (var renderer in part.GetComponentsInChildren<Renderer>())
                    {
                        var world = renderer.bounds;
                        for (var corner = 0; corner < 8; corner++)
                        {
                            var worldCorner = world.center + Vector3.Scale(world.extents, new Vector3(
                                (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                            var local = part.InverseTransformPoint(worldCorner);
                            if (!hasBounds) { localBounds = new Bounds(local, Vector3.zero); hasBounds = true; }
                            else localBounds.Encapsulate(local);
                        }
                    }
                    collider.center = localBounds.center;
                    collider.size = hasBounds ? localBounds.size : Vector3.one * 0.01f;
                    foreach (var ownCollider in GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(collider, ownCollider, true);
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
            _activeSlowMotionScale = depthScale;
            // StartCoroutine executes up to the first yield before assigning its handle.
            if (!SimulationPaused)
            {
                Time.timeScale = depthScale;
                Time.fixedDeltaTime = 0.02f * depthScale;
            }
            while (depthDuration > 0f)
            {
                yield return null;
                if (!SimulationPaused)
                {
                    depthDuration -= Time.unscaledDeltaTime;
                }
            }
            _slowMotionRoutine = null;
            ApplyTimeScale();
        }

    }
}
