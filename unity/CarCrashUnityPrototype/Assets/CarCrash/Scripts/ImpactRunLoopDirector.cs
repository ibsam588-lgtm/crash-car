using UnityEngine;

namespace CarCrash
{
    public sealed class ImpactRunLoopDirector : MonoBehaviour
    {
        public ArcadeVehicleController vehicle;
        public Transform player;
        public Rigidbody playerBody;
        public bool RecoverySuggested { get; private set; }
        private Vector3 _lastPosition;
        private float _blockedSeconds;

        private void LateUpdate()
        {
            if (player == null) return;
            var position = player.position;
            var trying=vehicle!=null&&Mathf.Abs(vehicle.CurrentThrottle)>.2f&&vehicle.CurrentBrake<.1f;
            var travel=Vector3.Distance(position,_lastPosition);
            _blockedSeconds=Time.timeScale>0&&trying&&travel<.35f*Time.deltaTime ? _blockedSeconds+Time.deltaTime : Mathf.Max(0,_blockedSeconds-Time.deltaTime*2f);
            _lastPosition=position;
            RecoverySuggested=position.y < -2f||Vector3.Dot(player.up,Vector3.up)<.3f||!CityRoadNetwork.InsideWorld(position)||_blockedSeconds>2.5f;
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
                if (!CityRoadNetwork.InsideWorld(position,12f)) continue;
                if (TryRecover(position, rotation)) return;
            }
            RecoverySuggested = true;
        }

        public bool RecoverFromCheckpoint(Vector3 position, Quaternion rotation)
        {
            var right = rotation * Vector3.right;
            foreach (var side in new[] { 0f, -2.5f, 2.5f, -4.5f, 4.5f })
                if (TryRecover(position + right * side, rotation)) return true;
            RecoverySuggested = true;
            return false;
        }

        private bool TryRecover(Vector3 position, Quaternion rotation)
        {
            var chassis = vehicle.GetComponent<BoxCollider>();
            var halfSize = chassis.size * .5f + new Vector3(.3f, .08f, .6f);
            foreach (var collider in Physics.OverlapBox(position + rotation * chassis.center, halfSize, rotation, ~0, QueryTriggerInteraction.Ignore))
                if (collider.attachedRigidbody != playerBody && collider.bounds.max.y > position.y + .28f) return false;
            vehicle.RecoverAt(position, rotation);
            RecoverySuggested = false;
            _blockedSeconds = 0f;
            _lastPosition = position;
            return true;
        }
    }
}
