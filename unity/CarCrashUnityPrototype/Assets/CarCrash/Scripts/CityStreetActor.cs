using UnityEngine;

namespace CarCrash
{
    public sealed class CityStreetActor : MonoBehaviour
    {
        public AnimationClip poseClip;
        public Transform player;
        private Transform _head;
        private Quaternion _headRest;
        private Renderer[] _renderers;
        private float _next;
        private void Start()
        {
            var animation=GetComponentInChildren<Animation>();
            if(animation!=null&&poseClip!=null)
            {
                animation.AddClip(poseClip,"Shop idle");animation.Play("Shop idle");
                animation["Shop idle"].time=poseClip.length*.15f;animation["Shop idle"].speed=0;
                animation.Sample();animation.enabled=false;
            }
            foreach(var bone in GetComponentsInChildren<Transform>())
                if(bone.name.ToLowerInvariant()=="head"){_head=bone;_headRest=bone.localRotation;break;}
            _renderers=GetComponentsInChildren<Renderer>();
        }
        private void LateUpdate()
        {
            var distance=player==null ? 0 : (transform.position-player.position).sqrMagnitude;
            if(_head!=null&&distance<45*45&&Time.timeScale>0)
                _head.localRotation=_headRest*Quaternion.Euler(0,Mathf.Sin(Time.time*.5f+transform.position.z)*13f,Mathf.Sin(Time.time*.8f)*1.7f);
            if(Time.unscaledTime<_next)return;_next=Time.unscaledTime+.4f;
            foreach(var renderer in _renderers)
            {renderer.enabled=distance<90*90;renderer.shadowCastingMode=distance<38*38 ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;}
        }
    }
}
