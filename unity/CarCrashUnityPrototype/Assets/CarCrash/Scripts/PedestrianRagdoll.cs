using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    internal sealed class PedestrianRagdoll
    {
        private readonly List<Limb> _limbs = new();
        private readonly Transform _root;
        private readonly Dictionary<string, Transform> _bones = new(StringComparer.OrdinalIgnoreCase);

        private sealed class Limb
        {
            public Transform Bone;
            public Rigidbody Body;
            public CapsuleCollider Shape;
            public Vector3 Offset;
            public Quaternion Rotation;
        }

        public PedestrianRagdoll(Transform owner, Animation animation, Vector3 push)
        {
            foreach (var bone in animation.GetComponentsInChildren<Transform>(true))
            {
                var name = bone.name;
                var separator = name.LastIndexOf(':');
                _bones.TryAdd(separator >= 0 ? name[(separator + 1)..] : name, bone);
            }
            _root = new GameObject("Fallen pedestrian physics").transform;
            _root.SetParent(owner, false);

            var hips = Add("pelvis", "spine_01", .138f, 19f);
            var torso = Add("spine_01", "neck_01", .149f, 21f, hips, 31f);
            Add("head", null, .105f, 4f, torso, 29f);
            foreach (var side in new[] { "_l", "_r" })
            {
                var thigh = Add("thigh" + side, "calf" + side, .086f, 7f, hips, 56f);
                Add("calf" + side, "foot" + side, .068f, 4f, thigh, 52f);
                var upper = Add("upperarm" + side, "lowerarm" + side, .063f, 3f, torso, 63f);
                Add("lowerarm" + side, "hand" + side, .05f, 2f, upper, 53f);
            }

            for (var i = 0; i < _limbs.Count; i++)
            {
                var limb = _limbs[i];
                for (var j = 0; j < i; j++) Physics.IgnoreCollision(limb.Shape, _limbs[j].Shape);
                limb.Body.linearVelocity = push;
            }
            if (hips != null)
                hips.Body.angularVelocity = Vector3.Cross(Vector3.up, push.normalized) * 2.1f;
        }

        public bool HasBody => _limbs.Count >= 6;

        public void ApplyPose()
        {
            foreach (var limb in _limbs)
            {
                var pose = limb.Body.transform;
                limb.Bone.SetPositionAndRotation(pose.TransformPoint(limb.Offset), pose.rotation * limb.Rotation);
            }
        }

        private Limb Add(string name, string endpoint, float radius, float mass, Limb parent = null, float swing = 40f)
        {
            if (!_bones.TryGetValue(name, out var bone)) return null;
            var end = endpoint != null && _bones.TryGetValue(endpoint, out var next) ? next.position : bone.position + Vector3.up * .15f;
            var direction = end - bone.position;
            if (direction.sqrMagnitude < .0004f) direction = Vector3.up * .1f;
            var proxy = new GameObject(name + " impact body");
            proxy.transform.SetParent(_root, false);
            proxy.transform.SetPositionAndRotation(Vector3.Lerp(bone.position, end, .49f), Quaternion.FromToRotation(Vector3.up, direction));

            var collider = proxy.AddComponent<CapsuleCollider>();
            collider.radius = radius;
            collider.height = Mathf.Max(radius * 2f, direction.magnitude + radius * .32f);
            var body = proxy.AddComponent<Rigidbody>();
            body.mass = mass;
            body.linearDamping = .48f;
            body.angularDamping = 1.3f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.solverIterations = 8;

            if (parent != null)
            {
                var joint = proxy.AddComponent<CharacterJoint>();
                joint.connectedBody = parent.Body;
                joint.anchor = proxy.transform.InverseTransformPoint(bone.position);
                joint.axis = Vector3.forward;
                joint.swingAxis = Vector3.right;
                joint.lowTwistLimit = new SoftJointLimit { limit = -25f };
                joint.highTwistLimit = new SoftJointLimit { limit = 25f };
                joint.swing1Limit = new SoftJointLimit { limit = swing };
                joint.swing2Limit = new SoftJointLimit { limit = swing * .74f };
                joint.enableProjection = true;
                joint.projectionDistance = .08f;
                joint.projectionAngle = 30f;
            }

            var limb = new Limb
            {
                Bone = bone, Body = body, Shape = collider,
                Offset = proxy.transform.InverseTransformPoint(bone.position),
                Rotation = Quaternion.Inverse(proxy.transform.rotation) * bone.rotation,
            };
            _limbs.Add(limb);
            return limb;
        }
    }
}
