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

            if (player != null && player.CurrentThrottle >= 0f && player.CurrentBrake < .2f)
                PushTraffic(collision.rigidbody, contact, collision.relativeVelocity);
            _crashVisual?.ApplyImpact(contact.point, collision.relativeVelocity, impactKmh, chainDepth);
            scoring?.RegisterTrafficImpact(this, contact.point, collision.relativeVelocity, impactKmh, chainDepth);
        }

        private void PushTraffic(Rigidbody striker, ContactPoint contact, Vector3 relativeVelocity)
        {
            var traffic = _mover.Body;
            var spec = striker.GetComponent<VehicleGarage>()?.Current;
            var push = spec?.trafficPush ?? 0f;
            var monster = spec?.id == "monster";
            if (push < .4f || traffic.mass > (monster ? 2450f : 1850f) || striker.mass < traffic.mass * 1.48f) return;

            var forward = Vector3.ProjectOnPlane(striker.transform.forward, Vector3.up).normalized;
            var toTraffic = Vector3.ProjectOnPlane(traffic.worldCenterOfMass - striker.worldCenterOfMass, Vector3.up);
            if (Vector3.Dot(forward, toTraffic.normalized) < .72f || Mathf.Abs(Vector3.Dot(forward, contact.normal)) < .62f)
                return;

            // Cruising traffic is kinematic until first impact. Use its speed and the
            // collision's approach speed to estimate how two moving bodies would share momentum.
            var closingSpeed = Mathf.Abs(Vector3.Dot(relativeVelocity, forward));
            var cruise = Vector3.Dot(_mover.velocity.normalized * _mover.CurrentCruiseSpeed, forward);
            var approaching = Mathf.Min(48f, closingSpeed + cruise);
            if (approaching < 3f) return;
            var sharedSpeed = (striker.mass * approaching + traffic.mass * cruise) / (striker.mass + traffic.mass);
            if (monster && closingSpeed * 3.6f > 43f)
                _crashVisual?.CrushShell(contact.point, Mathf.Lerp(.55f, 1f, Mathf.InverseLerp(43f, 122f, closingSpeed * 3.6f)));

            var outgoing = striker.linearVelocity;
            var retained = sharedSpeed * Mathf.Lerp(.52f, .91f, push) - Vector3.Dot(outgoing, forward);
            if (retained <= 0f) return;
            striker.linearVelocity = outgoing + forward * retained;

            // Let the heavy nose shoulder the car to the side instead of re-catching it next step.
            var right = Vector3.Cross(Vector3.up, forward);
            var offset = Vector3.Dot(toTraffic, right);
            var side = Mathf.Abs(offset) > .08f ? Mathf.Sign(offset) : (GetInstanceID() % 2 == 0 ? 1f : -1f);
            var sideways = side * Mathf.Min(4f, closingSpeed * .22f) * push;
            var trafficVelocity = traffic.linearVelocity;
            var forwardTransfer = Mathf.Max(0f, sharedSpeed - Vector3.Dot(trafficVelocity, forward)) * push;
            traffic.linearVelocity = Vector3.ClampMagnitude(trafficVelocity + forward * forwardTransfer + right * sideways, 48f);
        }
    }
}
