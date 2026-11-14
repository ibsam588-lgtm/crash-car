using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(MovingTrafficObstacle))]
    public sealed class TrafficImpactTarget : MonoBehaviour
    {
        public CrashScoring scoring;
        public Transform visualRoot;
        public float minimumImpactKmh = 16f;

        public bool IsReleased => _mover != null && _mover.IsReleased;
        public int ChainDepth => _mover != null ? _mover.ChainDepth : 0;

        private MovingTrafficObstacle _mover;
        private TrafficCrashVisual _crashVisual;
        private float _nudgeSeconds;

        private void OnCollisionStay(Collision collision)
        {
            if(_mover==null||_mover.IsReleased||collision.rigidbody==null)return;
            var player=collision.rigidbody.GetComponent<ArcadeVehicleController>();
            if(player==null)return;
            var direction=player.transform.forward*player.CurrentThrottle;
            var toTraffic=transform.position-player.transform.position;toTraffic.y=0;
            var pushing=Mathf.Abs(player.CurrentThrottle)>.2f&&player.CurrentBrake<.1f&&player.SpeedKmh<14f&&Vector3.Dot(direction.normalized,toTraffic.normalized)>.35f;
            _nudgeSeconds=pushing ? _nudgeSeconds+Time.fixedDeltaTime : 0;
            if(_nudgeSeconds>.3f)_mover.ReleaseForNudge(collision.rigidbody);
        }
        private void OnCollisionExit(Collision collision)=>_nudgeSeconds=0;

        private void Awake()
        {
            _mover = GetComponent<MovingTrafficObstacle>();
            _crashVisual = GetComponent<TrafficCrashVisual>();
            if (_crashVisual != null && _crashVisual.visualRoot == null)
            {
                _crashVisual.visualRoot = visualRoot;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_mover == null || _mover.IsReleased || collision.rigidbody == null)
            {
                return;
            }

            var player = collision.rigidbody.GetComponent<ArcadeVehicleController>();
            var sourceTraffic = collision.rigidbody.GetComponent<TrafficImpactTarget>();
            if (player == null && (sourceTraffic == null || !sourceTraffic.IsReleased))
            {
                return;
            }

            var impactKmh = Mathf.Min(collision.relativeVelocity.magnitude * 3.6f, 240f);
            if(player!=null&&impactKmh<(scoring!=null ? scoring.minimumImpactKmh : 28f)&&Mathf.Abs(player.CurrentThrottle)>.2f&&player.CurrentBrake<.1f)
            {
                _mover.ReleaseForNudge(collision.rigidbody);
                return;
            }
            if (impactKmh < minimumImpactKmh || collision.contactCount == 0)
            {
                return;
            }

            var contact = collision.GetContact(0);
            var chainDepth = player != null ? 0 : sourceTraffic.ChainDepth + 1;
            if (!_mover.ReleaseFromImpact(
                    collision.rigidbody,
                    contact.point,
                    contact.normal,
                    impactKmh,
                    chainDepth))
            {
                return;
            }

            _crashVisual?.ApplyImpact(contact.point, collision.relativeVelocity, impactKmh, chainDepth);
            scoring?.RegisterTrafficImpact(this, contact.point, collision.relativeVelocity, impactKmh, chainDepth);
        }
    }
}
