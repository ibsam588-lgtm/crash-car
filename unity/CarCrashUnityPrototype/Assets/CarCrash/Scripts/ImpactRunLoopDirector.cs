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
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var occupied = false;
                foreach (var collider in Physics.OverlapBox(position + Vector3.up * 0.7f,
                    new Vector3(1.2f, 0.65f, 2.5f), rotation, ~0, QueryTriggerInteraction.Ignore))
                    if (collider.attachedRigidbody != playerBody && collider.GetComponent<MeshCollider>() == null)
                        occupied = true;
                if (!occupied) break;
                position -= rotation * Vector3.forward * 6f;
            }
            vehicle.RecoverAt(position, rotation);
            RecoverySuggested = false;
        }
    }
}
