using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BreakableCityProp : MonoBehaviour
    {
        public float breakSpeedKmh=17f;
        public bool Broken { get; private set; }
        public float ImpactKmh { get; private set; }
        public Renderer[] visibleParts;
        private Rigidbody _body;
        private Transform _player;
        private float _nextVisibility;
        private Renderer[] _lettering;
        public bool Instanced { get; set; }
        private void Awake()
        {
            _body=GetComponent<Rigidbody>();
            var vehicle=FindFirstObjectByType<ArcadeVehicleController>();
            if(vehicle!=null)_player=vehicle.transform;
            if(visibleParts==null||visibleParts.Length==0)visibleParts=GetComponentsInChildren<Renderer>();
            var lettering=new System.Collections.Generic.List<Renderer>();
            foreach(var renderer in visibleParts)if(renderer.GetComponent<TextMesh>()!=null)lettering.Add(renderer);
            _lettering=lettering.ToArray();
        }
        private void OnCollisionEnter(Collision collision)=>HandleContact(collision);
        private void OnCollisionStay(Collision collision)=>HandleContact(collision);
        private void HandleContact(Collision collision)
        {
            if(Broken||collision.rigidbody==null)return;
            var vehicle=collision.rigidbody.GetComponent<ArcadeVehicleController>();
            var traffic=collision.rigidbody.GetComponent<MovingTrafficObstacle>();
            if(vehicle==null&&traffic==null)return;
            var speed=collision.relativeVelocity.magnitude*3.6f;
            // A truck can also push a weakened base out of the way at low speed.
            var force=collision.impulse.magnitude/Mathf.Max(Time.fixedDeltaTime,.005f);
            if(speed<breakSpeedKmh&&force<45000f)return;
            var point=collision.contactCount>0 ? collision.GetContact(0).point : transform.position+Vector3.up;
            var direction=collision.rigidbody.linearVelocity;
            if(direction.sqrMagnitude<.05f)direction=transform.position-collision.rigidbody.position;
            Release(direction,speed,point);
            if(vehicle!=null)vehicle.GetComponent<VehicleImpactEffects>()?.EmitImpact(point,Vector3.up,direction,speed);
        }
        public void Release(Vector3 direction,float impactKmh,Vector3 point)
        {
            if(Broken)return;
            Broken=true;ImpactKmh=impactKmh;
            direction.y=0;if(direction.sqrMagnitude<.01f)direction=Vector3.forward;direction.Normalize();
            _body.isKinematic=false;_body.useGravity=true;
            _body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            _body.linearVelocity=direction*Mathf.Clamp(impactKmh*.035f,1.4f,4f)+Vector3.up*.35f;
            _body.angularVelocity=Vector3.Cross(Vector3.up,direction)*2.1f;
            foreach(var renderer in visibleParts)
            {
                if(renderer==null)continue;renderer.forceRenderingOff=false;renderer.enabled=true;
                if(renderer.sharedMaterial!=null&&renderer.sharedMaterial.name.Contains("Lamp_Lens"))
                {var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetColor("_EmissionColor",Color.black);renderer.SetPropertyBlock(block);}
            }
        }
        private void LateUpdate()
        {
            if(!Broken&&Instanced&&_lettering.Length==0)return;
            if(Time.unscaledTime<_nextVisibility)return;_nextVisibility=Time.unscaledTime+.35f;
            var near=_player==null||(transform.position-_player.position).sqrMagnitude<150f*150f;
            foreach(var renderer in !Broken&&Instanced ? _lettering : visibleParts)if(renderer!=null)renderer.enabled=near;
        }
    }
}
