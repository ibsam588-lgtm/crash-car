using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    public sealed class TrafficCrashVisual : MonoBehaviour
    {
        public Transform visualRoot;
        public int maximumDetachedParts = 3;

        private readonly List<Transform> _detachable = new();
        private readonly HashSet<int> _detachedIds = new();
        private Renderer[] _renderers = Array.Empty<Renderer>();

        private void Awake()
        {
            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }

            if (visualRoot == null)
            {
                return;
            }

            _renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var candidate in visualRoot.GetComponentsInChildren<Transform>(true))
            {
                if (candidate == visualRoot ||
                    !IsDetachableName(candidate.name) ||
                    (candidate.parent != null && IsDetachableName(candidate.parent.name)))
                {
                    continue;
                }

                _detachable.Add(candidate);
            }
        }

        public void ApplyImpact(Vector3 point, Vector3 relativeVelocity, float impactKmh, int chainDepth)
        {
            DarkenNearestPanel(point, impactKmh);

            var partCount = Mathf.Clamp(1 + Mathf.FloorToInt((impactKmh - 58f) / 48f), 1, maximumDetachedParts);
            _detachable.Sort((left, right) =>
                DistanceToPoint(left, point).CompareTo(DistanceToPoint(right, point)));

            var detached = 0;
            foreach (var part in _detachable)
            {
                if (detached >= partCount || part == null || _detachedIds.Contains(part.GetInstanceID()))
                {
                    continue;
                }

                DetachPart(part, point, relativeVelocity, impactKmh, chainDepth);
                detached += 1;
            }
        }

        private void DarkenNearestPanel(Vector3 point, float impactKmh)
        {
            Renderer nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var renderer in _renderers)
            {
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                var distance = Vector3.Distance(renderer.bounds.ClosestPoint(point), point);
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = renderer;
                nearestDistance = distance;
            }

            if (nearest == null)
            {
                return;
            }

            var materials = nearest.materials;
            var damageAmount = Mathf.InverseLerp(35f, 180f, impactKmh) * 0.58f;
            foreach (var material in materials)
            {
                if (material != null && material.HasProperty("_Color"))
                {
                    material.color = Color.Lerp(material.color, new Color(0.045f, 0.035f, 0.03f, 1f), damageAmount);
                }
            }
        }

        private void DetachPart(Transform part, Vector3 point, Vector3 relativeVelocity, float impactKmh, int chainDepth)
        {
            _detachedIds.Add(part.GetInstanceID());
            var bounds = CalculateBounds(part);
            part.SetParent(null, true);

            var collider = part.GetComponent<Collider>();
            if (collider == null)
            {
                var box = part.gameObject.AddComponent<BoxCollider>();
                box.center = part.InverseTransformPoint(bounds.center);
                var scale = part.lossyScale;
                box.size = new Vector3(
                    bounds.size.x / Mathf.Max(0.01f, Mathf.Abs(scale.x)),
                    bounds.size.y / Mathf.Max(0.01f, Mathf.Abs(scale.y)),
                    bounds.size.z / Mathf.Max(0.01f, Mathf.Abs(scale.z)));
                collider = box;
            }

            var body = part.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = part.gameObject.AddComponent<Rigidbody>();
            }

            body.mass = Mathf.Clamp(bounds.size.magnitude * 7f, 3f, 38f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var velocityDirection = relativeVelocity.sqrMagnitude > 0.01f ? relativeVelocity.normalized : transform.forward;
            var chainLift = 0.18f + Mathf.Min(chainDepth, 4) * 0.035f;
            var forceDirection = (velocityDirection + Vector3.up * chainLift).normalized;
            body.AddForceAtPosition(forceDirection * Mathf.Clamp(impactKmh * 0.11f, 5f, 24f), point, ForceMode.Impulse);
            body.AddTorque(new Vector3(0.7f, 1.1f, 0.8f) * body.mass, ForceMode.Impulse);
            Destroy(part.gameObject, 7f);
        }

        private static Bounds CalculateBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.position, Vector3.one * 0.25f);
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static float DistanceToPoint(Transform candidate, Vector3 point)
        {
            var renderer = candidate.GetComponentInChildren<Renderer>(true);
            return renderer != null
                ? Vector3.Distance(renderer.bounds.ClosestPoint(point), point)
                : Vector3.Distance(candidate.position, point);
        }

        private static bool IsDetachableName(string name)
        {
            if (name.StartsWith("Damage_Windshield_Crack", StringComparison.Ordinal))
            {
                return false;
            }

            return name.StartsWith("Damage_", StringComparison.Ordinal) ||
                   name.Contains("Mirror", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Bumper", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Splitter", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Diffuser", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Headlamp", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("TailLamp", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Endplate", StringComparison.OrdinalIgnoreCase);
        }
    }
}
