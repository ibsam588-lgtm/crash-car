using UnityEngine;

namespace CarCrash
{
    public sealed class CrashDamageVisual : MonoBehaviour
    {
        public CrashScoring scoring;
        public Renderer[] windshieldCracks = System.Array.Empty<Renderer>();

        private int _observedHits;

        private void Update()
        {
            if (scoring == null || scoring.Hits <= _observedHits)
            {
                return;
            }

            _observedHits = scoring.Hits;
            if (_observedHits == 1)
            {
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
