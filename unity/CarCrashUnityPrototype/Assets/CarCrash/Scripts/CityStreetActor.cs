using UnityEngine;

namespace CarCrash
{
    public sealed class CityStreetActor : MonoBehaviour
    {
        public AnimationClip poseClip;
        public Transform player;
        public CityBuildingProfile shop;
        public Vector3 escapePoint;
        public Transform[] performanceProps;
        public bool IsFleeing => _waypoint >= 0 && _waypoint < _escapeRoute.Length;
        public bool HasEvacuated => _waypoint >= 0 && _waypoint == _escapeRoute.Length;

        private Renderer[] _renderers;
        private PedestrianGait _gait;
        private Rigidbody _car;
        private Vector3[] _escapeRoute = System.Array.Empty<Vector3>();
        private int _waypoint = -1;
        private float _next, _fleeSpeed;
        private bool IsPerformer => performanceProps != null && performanceProps.Length > 0;

        private void Start()
        {
            _car=player!=null ? player.GetComponent<Rigidbody>() : null;
            var animation = GetComponentInChildren<Animation>();
            if (animation != null && poseClip != null) _gait = new PedestrianGait(animation, poseClip, 1.3f);
            _renderers=GetComponentsInChildren<Renderer>();
        }

        private void Update()
        {
            if(Time.deltaTime<=0f)return;
            if(_waypoint<0)
            {
                if(shop!=null && (shop.Evacuating||CarInShop()))
                {
                    var door=shop.transform.TransformPoint(shop.egressLocalPosition);
                    var passage=new Vector3(transform.position.x,transform.position.y,door.z);
                    _escapeRoute=new[] {passage,door,escapePoint};
                    _waypoint = 0;
                }
                else if(IsPerformer&&CityPedestrian.VehicleApproaching(_car,transform.position))
                {
                    _escapeRoute=new[] {escapePoint};
                    foreach(var prop in performanceProps)if(prop!=null)prop.gameObject.SetActive(false);
                    _waypoint = 0;
                }
            }
            if(!IsFleeing)return;
            var step = Time.deltaTime;
            var position = transform.position;
            var direction = _escapeRoute[_waypoint] - position;
            direction.y = 0f;
            if (direction.sqrMagnitude < .05f)
            {
                _waypoint++;
                if (HasEvacuated)
                {
                    _gait?.SetSpeed(0f);
                    return;
                }
                direction = _escapeRoute[_waypoint] - position;
                direction.y = 0f;
            }
            var heading = transform.rotation;
            var pace = PedestrianGait.FacingSpeed(ref heading, direction, 4.1f, 440f, step);
            _fleeSpeed = Mathf.MoveTowards(_fleeSpeed, pace, 12f * step);
            var travel = Mathf.Min(_fleeSpeed, pace) * step;
            var next = Vector3.MoveTowards(position, position + direction, travel);
            transform.SetPositionAndRotation(next, heading);
            _gait?.SetSpeed(Vector3.Distance(position, next) / step);
        }

        private bool CarInShop()
        {
            if(_car==null)return false;
            var car=shop.transform.InverseTransformPoint(_car.position);
            var door=shop.egressLocalPosition;
            var depth=(car.x-door.x)*shop.side;
            var centerZ=shop.attendantLocalPosition.z-1.15f;
            return depth>.32f&&depth<7.55f&&Mathf.Abs(car.z-centerZ)<shop.size.z*.5f&&car.y<3f;
        }

        private void LateUpdate()
        {
            var distance=player==null ? 0 : (transform.position-player.position).sqrMagnitude;
            if(IsPerformer&&_waypoint<0&&distance<60*60)
            {
                for(var i=0;i<performanceProps.Length;i++)
                {
                    var cycle=Mathf.Repeat(Time.time*.88f+i/(float)performanceProps.Length,1f);
                    var x=Mathf.Lerp(-.38f,.38f,cycle);
                    if(i%2!=0)x=-x;
                    var rise=4f*cycle*(1f-cycle);
                    performanceProps[i].localPosition=new Vector3(x,1.06f+rise*.89f,.46f);
                }
            }
            if(Time.unscaledTime<_next)return;_next=Time.unscaledTime+.4f;
            var visible = distance < 90 * 90;
            foreach(var renderer in _renderers)
            {renderer.enabled=visible;renderer.shadowCastingMode=distance<38*38 ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;}
            _gait?.SetVisible(visible && (IsFleeing || distance < 48 * 48));
        }
    }
}
