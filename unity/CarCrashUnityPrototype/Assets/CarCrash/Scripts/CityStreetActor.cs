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

        private Transform _head;
        private Quaternion _headRest;
        private Renderer[] _renderers;
        private Animation _animation;
        private Rigidbody _car;
        private Vector3[] _escapeRoute = System.Array.Empty<Vector3>();
        private int _waypoint = -1;
        private float _next;
        private bool _visible = true;
        private bool IsPerformer => performanceProps != null && performanceProps.Length > 0;

        private void Start()
        {
            _car=player!=null ? player.GetComponent<Rigidbody>() : null;
            _animation=GetComponentInChildren<Animation>();
            if(_animation!=null&&poseClip!=null)
            {
                _animation.AddClip(poseClip,"Street movement");
                _animation.Play("Street movement");
                var state=_animation["Street movement"];
                state.time=poseClip.length*.15f;
                state.speed=0f;
                state.wrapMode=WrapMode.Loop;
                _animation.Sample();
                _animation.enabled=false;
            }
            foreach(var bone in GetComponentsInChildren<Transform>())
                if(bone.name.ToLowerInvariant()=="head"){_head=bone;_headRest=bone.localRotation;break;}
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
                    BeginFleeing();
                }
                else if(IsPerformer&&CityPedestrian.VehicleApproaching(_car,transform.position))
                {
                    _escapeRoute=new[] {escapePoint};
                    foreach(var prop in performanceProps)if(prop!=null)prop.gameObject.SetActive(false);
                    BeginFleeing();
                }
            }
            if(!IsFleeing)return;
            var direction=_escapeRoute[_waypoint]-transform.position;
            direction.y=0f;
            if(direction.sqrMagnitude<.025f)
            {
                _waypoint++;
                if(HasEvacuated&&_animation!=null)_animation.enabled=false;
                return;
            }
            var turn=Quaternion.LookRotation(direction);
            transform.rotation=Quaternion.RotateTowards(transform.rotation,turn,620f*Time.deltaTime);
            transform.position=Vector3.MoveTowards(transform.position,_escapeRoute[_waypoint],3.85f*Time.deltaTime);
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

        private void BeginFleeing()
        {
            _waypoint=0;
            if(_animation==null||poseClip==null)return;
            _animation.enabled=_visible;
            _animation["Street movement"].speed=2.5f;
            _animation.Play("Street movement");
        }

        private void LateUpdate()
        {
            var distance=player==null ? 0 : (transform.position-player.position).sqrMagnitude;
            if(_head!=null&&_waypoint<0&&distance<45*45&&Time.timeScale>0)
                _head.localRotation=_headRest*Quaternion.Euler(0,Mathf.Sin(Time.time*.5f+transform.position.z)*13f,Mathf.Sin(Time.time*.8f)*1.7f);
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
            _visible=distance<90*90;
            foreach(var renderer in _renderers)
            {renderer.enabled=_visible;renderer.shadowCastingMode=distance<38*38 ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;}
            if(_animation!=null)
            {
                var animate=_visible&&IsFleeing;
                if(animate&&!_animation.enabled)
                {
                    _animation.enabled=true;
                    _animation.Play("Street movement");
                }
                else if(!animate)_animation.enabled=false;
            }
        }
    }
}
