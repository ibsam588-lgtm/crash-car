using UnityEngine;

namespace CarCrash
{
    public sealed class ImpactRunLoopDirector : MonoBehaviour
    {
        public ArcadeVehicleController vehicle;
        public Transform player;
        public Rigidbody playerBody;
        public float resetBeyondZ = 39f;
        public float resetBelowY = -1.5f;
        public float resetAboveY = 5.0f;
        public float resetBeyondX = 11.5f;
        public float relaunchDelaySeconds = 1.15f;

        private float _nextResetTime;

        private void LateUpdate()
        {
            if (vehicle == null || player == null || playerBody == null || Time.time < _nextResetTime)
            {
                return;
            }

            var position = player.position;
            var outOfShot = position.z > resetBeyondZ ||
                            position.y < resetBelowY ||
                            position.y > resetAboveY ||
                            Mathf.Abs(position.x) > resetBeyondX;

            if (!outOfShot)
            {
                return;
            }

            vehicle.ResetForRelaunch(relaunchDelaySeconds);
            _nextResetTime = Time.time + relaunchDelaySeconds + 0.35f;
        }
    }
}
