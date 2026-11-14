using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    public sealed class CityVisibility : MonoBehaviour
    {
        private Transform _player;
        private Renderer[] _renderers;
        private Bounds[] _bounds;
        private float _next;
        private Vector3 _last;
        private bool _ready;
        private void Start()
        {
            var vehicle=FindFirstObjectByType<ArcadeVehicleController>();
            if(vehicle==null){enabled=false;return;}_player=vehicle.transform;
            _renderers=GetComponentsInChildren<Renderer>();_bounds=new Bounds[_renderers.Length];
            for(var i=0;i<_renderers.Length;i++)_bounds[i]=_renderers[i].bounds;
        }
        private void LateUpdate()
        {
            if(_player==null||Time.unscaledTime<_next)return;_next=Time.unscaledTime+.3f;
            var point=_player.position;if(_ready&&(point-_last).sqrMagnitude<9)return;_last=point;_ready=true;
            for(var i=0;i<_renderers.Length;i++)
            {
                var renderer=_renderers[i];if(renderer==null)continue;
                var bounds=_bounds[i];var nearest=bounds.ClosestPoint(point);nearest.y=point.y;
                var distance=(nearest-point).sqrMagnitude;
                var broadSurface=bounds.size.x>400||bounds.size.z>400;
                renderer.enabled=broadSurface||distance<360*360;
                renderer.shadowCastingMode=distance<100*100&&!broadSurface ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }
        public void RefreshNow(){_ready=false;_next=0;LateUpdate();}
    }
}
