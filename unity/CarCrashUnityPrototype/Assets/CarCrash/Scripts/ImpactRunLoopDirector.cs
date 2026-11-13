using UnityEngine;

namespace CarCrash
{
    public sealed class ImpactRunLoopDirector : MonoBehaviour
    {
        public ArcadeVehicleController vehicle;
        public Transform player;
        public Rigidbody playerBody;
        public bool RecoverySuggested { get; private set; }

        private void LateUpdate()
        {
            if (player == null) return;
            var position = player.position;
            RecoverySuggested = position.y < -2f || Vector3.Dot(player.up, Vector3.up) < 0.3f ||
                Mathf.Abs(position.x) > 145f || position.z < -150f || position.z > 350f;
        }

        // There are no automatic shot-boundary resets. Recovery is an explicit
        // player action and keeps the current score and elapsed driving time.
        public void RecoverVehicle()
        {
            if (vehicle == null || player == null) return;
            CityRoadNetwork.NearestRoadPose(player.position, player.forward, out var position, out var rotation);
            var roadPosition = position;
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var offset = attempt == 0 ? 0 : ((attempt + 1) / 2) * 6f * (attempt % 2 == 0 ? 1f : -1f);
                position = roadPosition + rotation * Vector3.forward * offset;
                if (Mathf.Abs((rotation * Vector3.forward).z) > 0.5f && (position.z < -112f || position.z > 310f)) continue;
                if (Mathf.Abs(position.x) > 86f) continue;
                var occupied = false;
                foreach (var collider in Physics.OverlapBox(position + Vector3.up * 0.7f,
                    new Vector3(1.25f, 0.55f, 5f), rotation, ~0, QueryTriggerInteraction.Ignore))
                    if (collider.attachedRigidbody != playerBody && collider.bounds.max.y > position.y + 0.3f)
                        occupied = true;
                if (occupied) continue;
                vehicle.RecoverAt(position, rotation);
                RecoverySuggested = false;
                return;
            }
            RecoverySuggested = true;
        }
    }
}
