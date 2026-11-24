using UnityEngine;

namespace CarCrash
{
    public sealed class CityLeashedDog : MonoBehaviour
    {
        public Rigidbody owner;
        public Transform[] legs;
        public Transform tail;
        public LineRenderer leash;
        private float _step;

        private void LateUpdate()
        {
            if(owner==null||leash==null||!leash.enabled)return;
            var movement=Vector3.ProjectOnPlane(owner.linearVelocity,Vector3.up).magnitude;
            _step+=Time.deltaTime*Mathf.Clamp(movement*2.85f,0f,13f);
            var stride=Mathf.Clamp(movement*11f,0f,36f);
            for(var i=0;i<legs.Length;i++)
            {
                var phase=_step+((i==0||i==3)?0f:Mathf.PI);
                legs[i].localRotation=Quaternion.Euler(Mathf.Sin(phase)*stride,0f,0f);
            }
            tail.localRotation=Quaternion.Euler(0f,Mathf.Sin(Time.time*9.3f)*24f,0f);
            var hand=owner.transform.TransformPoint(new Vector3(.30f,.92f,.03f));
            var collar=transform.TransformPoint(new Vector3(0f,.49f,.27f));
            leash.SetPosition(0,hand);
            leash.SetPosition(1,Vector3.Lerp(hand,collar,.50f)-Vector3.up*.065f);
            leash.SetPosition(2,collar);
        }
    }
}
