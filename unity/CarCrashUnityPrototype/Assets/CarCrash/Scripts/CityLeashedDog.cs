using UnityEngine;

namespace CarCrash
{
    public sealed class CityLeashedDog : MonoBehaviour
    {
        public Rigidbody owner;
        public Transform[] legs;
        public Transform tail;
        public LineRenderer leash;
        private Transform _hand;
        private Vector3 _offset, _position, _velocity;
        private Quaternion _rotation;
        private float _step, _stride;

        private void Awake()
        {
            if (owner == null) return;
            _offset = owner.transform.InverseTransformPoint(transform.position);
            _position = transform.position;
            _rotation = transform.rotation;
            var model = owner.GetComponentInChildren<Animation>();
            if (model == null) return;
            foreach (var bone in model.GetComponentsInChildren<Transform>())
                if (bone.name == "hand_r") { _hand = bone; break; }
        }

        private void LateUpdate()
        {
            var deltaTime = Time.deltaTime;
            if (owner == null || leash == null || !leash.enabled || deltaTime <= 0f) return;
            var target = owner.transform.TransformPoint(_offset);
            var previous = _position;
            if ((_position - target).sqrMagnitude > 2.25f)
            {
                _position = target;
                _velocity = Vector3.zero;
                previous = target;
            }
            _position = Vector3.SmoothDamp(_position, target, ref _velocity, .16f, 7f, deltaTime);
            _position.y = target.y;
            var movement = Vector3.ProjectOnPlane(_position - previous, Vector3.up) / deltaTime;
            var pace = movement.magnitude;
            var facing = pace > .18f ? movement : Vector3.ProjectOnPlane(owner.transform.forward, Vector3.up);
            if (facing.sqrMagnitude > .01f)
                _rotation = Quaternion.RotateTowards(_rotation, Quaternion.LookRotation(facing), 380f * deltaTime);
            transform.SetPositionAndRotation(_position, _rotation);
            _step += deltaTime * Mathf.Clamp(pace * 3.4f, 0f, 18f);
            _stride = Mathf.MoveTowards(_stride, Mathf.Clamp(pace * 10f, 0f, 39f), 105f * deltaTime);
            for(var i=0;i<legs.Length;i++)
            {
                var phase=_step+((i==0||i==3)?0f:Mathf.PI);
                legs[i].localRotation=Quaternion.Euler(Mathf.Sin(phase)*_stride,0f,0f);
            }
            tail.localRotation=Quaternion.Euler(0f,Mathf.Sin(Time.time*9.3f)*24f,0f);
            var hand = _hand == null ? owner.transform.TransformPoint(new Vector3(.30f,.92f,.03f)) : _hand.position;
            var collar=transform.TransformPoint(new Vector3(0f,.49f,.27f));
            leash.SetPosition(0,hand);
            leash.SetPosition(1,Vector3.Lerp(hand,collar,.50f)-Vector3.up*.065f);
            leash.SetPosition(2,collar);
        }
    }
}
