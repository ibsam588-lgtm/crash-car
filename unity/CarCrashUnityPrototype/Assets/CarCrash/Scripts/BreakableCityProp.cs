using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BreakableCityProp : MonoBehaviour
    {
        public float breakSpeedKmh=17f;
        public float minimumBreakImpulse;
        public bool shatterGlass;
        public CityBuildingProfile shop;
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
        public bool MeetsBreakThreshold(float closingSpeedKmh,float impulse)
        {
            if(minimumBreakImpulse>0f)return closingSpeedKmh>=breakSpeedKmh&&impulse>=minimumBreakImpulse;
            // A truck can push a weaker fixture over without a high-speed strike.
            return closingSpeedKmh>=breakSpeedKmh||(!shatterGlass&&impulse>=45000f*Mathf.Max(Time.fixedDeltaTime,.005f));
        }
        private void HandleContact(Collision collision)
        {
            if(Broken||collision.rigidbody==null)return;
            var vehicle=collision.rigidbody.GetComponent<ArcadeVehicleController>();
            var traffic=collision.rigidbody.GetComponent<MovingTrafficObstacle>();
            if(vehicle==null&&traffic==null)return;
            var relative=collision.relativeVelocity;
            var contact=collision.contactCount>0 ? collision.GetContact(0) : default;
            var speed=(minimumBreakImpulse>0f||shatterGlass) && collision.contactCount>0
                ? Mathf.Abs(Vector3.Dot(contact.normal,relative))*3.6f : relative.magnitude*3.6f;
            if(!MeetsBreakThreshold(speed,collision.impulse.magnitude))return;
            var point=collision.contactCount>0 ? contact.point : transform.position+Vector3.up;
            var direction=collision.rigidbody.linearVelocity;
            if(shatterGlass)
            {
                // The pane has already absorbed a physics impulse this step. Retain
                // some forward motion so a sheet of glass doesn't stop the car.
                var inward=collision.rigidbody.position-transform.position;
                var approach=Vector3.Dot(relative,inward)<0f ? relative : -relative;
                approach=Vector3.ProjectOnPlane(approach,Vector3.up).normalized;
                var carry=speed/3.6f*.63f-Vector3.Dot(direction,approach);
                if(carry>0f)collision.rigidbody.linearVelocity+=approach*carry;
                direction=approach;
            }
            if(direction.sqrMagnitude<.05f)direction=transform.position-collision.rigidbody.position;
            Release(direction,speed,point);
            if(vehicle!=null)
            {
                var effects=vehicle.GetComponent<VehicleImpactEffects>();
                var surface=GetComponent<VehicleImpactMaterial>()?.surface ?? VehicleImpactSurface.Default;
                effects?.EmitImpact(point,Vector3.up,direction,speed,surface);
                vehicle.GetComponent<CrashScoring>()?.RegisterCityPropBreak(surface,speed);
            }
        }
        public void Release(Vector3 direction,float impactKmh,Vector3 point)
        {
            if(Broken)return;
            Broken=true;ImpactKmh=impactKmh;
            direction.y=0;if(direction.sqrMagnitude<.01f)direction=Vector3.forward;direction.Normalize();
            if(shatterGlass)
            {
                Shatter(direction,impactKmh);
                return;
            }
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
            if(Broken&&shatterGlass)return;
            if(!Broken&&Instanced&&_lettering.Length==0)return;
            if(Time.unscaledTime<_nextVisibility)return;_nextVisibility=Time.unscaledTime+.35f;
            var near=_player==null||(transform.position-_player.position).sqrMagnitude<150f*150f;
            foreach(var renderer in !Broken&&Instanced ? _lettering : visibleParts)if(renderer!=null)renderer.enabled=near;
        }

        private void Shatter(Vector3 direction,float speedKmh)
        {
            var pane=GetComponentInChildren<BoxCollider>();
            var bounds=pane!=null ? pane.bounds : new Bounds(transform.position,new Vector3(.07f,2.5f,3.6f));
            foreach(var collider in GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var renderer in visibleParts)if(renderer!=null)renderer.enabled=false;
            if(shop!=null)shop.AlertOccupants();

            var burst=new GameObject("Storefront glass fragments");
            burst.transform.SetParent(transform,false);
            var shards=burst.AddComponent<ParticleSystem>();
            shards.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=shards.main;
            main.duration=.1f;
            main.loop=false;
            main.playOnAwake=false;
            main.maxParticles=56;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.gravityModifier=1.35f;
            var emission=shards.emission;emission.enabled=false;
            var shape=shards.shape;shape.enabled=false;
            var render=shards.GetComponent<ParticleSystemRenderer>();
            render.renderMode=ParticleSystemRenderMode.Stretch;
            render.lengthScale=1.7f;
            render.velocityScale=.045f;
            var vehicle=_player!=null?_player.GetComponent<VehicleImpactEffects>():null;
            if(vehicle!=null&&vehicle.particleMaterial!=null)render.sharedMaterial=vehicle.particleMaterial;
            shards.Play();
            for(var i=0;i<56;i++)
            {
                var offset=new Vector3(0f,Random.Range(-.94f,.94f),Random.Range(-.96f,.96f));
                var color=Color.Lerp(new Color(.31f,.65f,.78f,.80f),new Color(.94f,.99f,1f),Random.value);
                var fragment=new ParticleSystem.EmitParams
                {
                    position=bounds.center+Vector3.Scale(offset,bounds.extents),
                    velocity=(direction*.76f+Random.insideUnitSphere*.80f)*Mathf.Clamp(speedKmh*.15f,2.8f,10f)+Vector3.up*Random.Range(.7f,3.7f),
                    startLifetime=Random.Range(.28f,1.15f),
                    startSize=Random.Range(.032f,.115f),
                    startColor=color,
                    rotation=Random.Range(0f,360f),
                    angularVelocity=Random.Range(-340f,340f)
                };
                shards.Emit(fragment,1);
            }
            Destroy(burst,1.2f);
        }
    }
}
