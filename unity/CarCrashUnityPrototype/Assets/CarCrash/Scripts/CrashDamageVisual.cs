using UnityEngine;

namespace CarCrash
{
    public sealed class CrashDamageVisual : MonoBehaviour
    {
        public CrashScoring scoring;
        public Renderer[] windshieldCracks = System.Array.Empty<Renderer>();

        private int _observedHits;
        private bool _cracked;
        private MaterialPropertyBlock _paint;
        private (Renderer renderer, int slot)[] _paintSlots;

        private void Awake()
        {
            _paint = new MaterialPropertyBlock();
            var slots = new System.Collections.Generic.List<(Renderer, int)>();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                    if (materials[i] != null && materials[i].HasProperty("_ImpactMark")) slots.Add((renderer, i));
            }
            _paintSlots = slots.ToArray();
        }

        private void Update()
        {
            if (scoring == null || scoring.DamageRevision == _observedHits)
            {
                return;
            }

            _observedHits = scoring.DamageRevision;
            var strength = Mathf.InverseLerp(22f, 95f, scoring.LastDamageKmh);
            foreach (var (renderer, slot) in _paintSlots)
            {
                if (renderer == null || !renderer.transform.IsChildOf(transform)) continue;
                var point = renderer.transform.InverseTransformPoint(scoring.LastDamagePoint);
                renderer.GetPropertyBlock(_paint, slot);
                _paint.SetVector("_ImpactMark", new Vector4(point.x, point.y, point.z, strength));
                _paint.SetFloat("_BodyWear", scoring.Damage * .01f);
                renderer.SetPropertyBlock(_paint, slot);
            }
            if (!_cracked && scoring.LastDamageKmh >= 55f)
            {
                _cracked = true;
                foreach (var renderer in windshieldCracks)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                }
            }
        }
    }
}
