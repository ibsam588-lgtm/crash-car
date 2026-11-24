using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    internal sealed class GarageUpgradeVisuals : MonoBehaviour
    {
        private sealed class Slot
        {
            public readonly Renderer Renderer;
            public readonly int Index;
            public readonly MaterialPropertyBlock Original = new();

            public Slot(Renderer renderer, int index)
            {
                Renderer = renderer;
                Index = index;
                renderer.GetPropertyBlock(Original, index);
            }

            public void Restore() => Renderer.SetPropertyBlock(Original, Index);
        }

        private sealed class Wheel
        {
            public Transform Pivot;
            public Vector3 Point;
            public Quaternion Rotation;
            public Vector3 Outward;
            public Vector3 ModelScale;
            public MeshFilter Rim, Sidewall;
            public MeshRenderer SidewallRenderer;
        }

        private readonly List<Slot> _alloy = new(), _lamps = new(), _windows = new();
        private readonly List<Wheel> _wheels = new();
        private readonly List<Material> _materials = new();
        private readonly Dictionary<Material, Material> _lampCopies = new();
        private readonly Mesh[] _rimMeshes = new Mesh[4], _sidewallMeshes = new Mesh[3];
        private MaterialPropertyBlock _block;
        private int _colorProperty, _emission, _metal, _gloss;

        private GarageVehicle _spec;
        private Transform _chassis;
        private GameObject _bodyKit, _auxiliary;
        private Material _rim, _barrel, _bolts, _whitewall, _pinstripe, _guard, _recovery, _trim, _auxLens;
        private Bounds? _leftLamp, _rightLamp;

        internal void Bind(Transform chassis, GarageVehicle spec, Transform[] wheels)
        {
            _block = new MaterialPropertyBlock();
            _colorProperty = Shader.PropertyToID("_Color");
            _emission = Shader.PropertyToID("_EmissionColor");
            _metal = Shader.PropertyToID("_Metallic");
            _gloss = Shader.PropertyToID("_Glossiness");
            _chassis = chassis;
            _spec = spec;
            foreach (var pivot in wheels)
            {
                if (pivot == null) continue;
                _wheels.Add(CreateWheelBinding(pivot));
            }

            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                var mesh = filter.sharedMesh;
                if (renderer == null || mesh == null) continue;
                var materials = renderer.sharedMaterials;
                var lensesChanged = false;
                for (var i = 0; i < Mathf.Min(materials.Length, mesh.subMeshCount); i++)
                {
                    var material = materials[i];
                    if (material == null) continue;
                    var name = material.name;
                    if (name.Contains("Headlamp", StringComparison.OrdinalIgnoreCase) && material.HasProperty(_emission))
                    {
                        var bounds = InChassisSpace(filter.transform, mesh.GetSubMesh(i).bounds);
                        if (bounds.center.z < _spec.size.z * .24f) continue;
                        BindLampBounds(bounds);
                        _lamps.Add(new Slot(renderer, i));
                        if (!material.IsKeywordEnabled("_EMISSION"))
                        {
                            if (!_lampCopies.TryGetValue(material, out var duplicate))
                            {
                                duplicate = new Material(material);
                                duplicate.EnableKeyword("_EMISSION");
                                _lampCopies.Add(material, duplicate);
                                _materials.Add(duplicate);
                            }
                            materials[i] = duplicate;
                            lensesChanged = true;
                        }
                    }
                    if (name.Contains("Glass", StringComparison.OrdinalIgnoreCase) && material.HasProperty(_colorProperty))
                        _windows.Add(new Slot(renderer, i));
                }
                if (lensesChanged) renderer.sharedMaterials = materials;
            }
        }

        internal void LampPositions(out Vector3 left, out Vector3 right)
        {
            left = LampPosition(_leftLamp, -1);
            right = LampPosition(_rightLamp, 1);
        }

        private Vector3 LampPosition(Bounds? lens, int side)
        {
            if (lens.HasValue)
            {
                var bounds = lens.Value;
                return new Vector3(bounds.center.x, bounds.center.y, bounds.max.z + .045f);
            }
            var height = _spec.id == "monster" ? _spec.size.y * .60f : _spec.size.y * .44f;
            return new Vector3(side * _spec.size.x * .34f, Mathf.Max(.46f, height), _spec.size.z * .49f);
        }

        private void BindLampBounds(Bounds bounds)
        {
            if (bounds.min.x < -.12f)
            {
                var left = bounds;
                if (left.max.x > 0f) left.max = new Vector3(-.12f, left.max.y, left.max.z);
                if (_leftLamp.HasValue) left.Encapsulate(_leftLamp.Value);
                _leftLamp = left;
            }
            if (bounds.max.x > .12f)
            {
                var right = bounds;
                if (right.min.x < 0f) right.min = new Vector3(.12f, right.min.y, right.min.z);
                if (_rightLamp.HasValue) right.Encapsulate(_rightLamp.Value);
                _rightLamp = right;
            }
        }

        private Wheel CreateWheelBinding(Transform pivot)
        {
            var center = _chassis.InverseTransformPoint(pivot.position);
            var side = Mathf.Sign(center.x);
            var outer = side * center.x + _spec.wheelRadius * .31f;
            foreach (var filter in pivot.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null) continue;
                var bounds = InChassisSpace(filter.transform, filter.sharedMesh.bounds);
                outer = Mathf.Max(outer, side > 0f ? bounds.max.x : -bounds.min.x);
                var slots = renderer.sharedMaterials;
                for (var i = 0; i < slots.Length; i++)
                {
                    var material = slots[i];
                    if (material == null || !material.HasProperty(_colorProperty)) continue;
                    if (material.name.Contains("Alloy", StringComparison.OrdinalIgnoreCase) ||
                        material.name.Contains("Rim", StringComparison.OrdinalIgnoreCase))
                        _alloy.Add(new Slot(renderer, i));
                }
            }
            // Position the face from the actual model's tire width. The imported axle can
            // use a different local basis; parenting in world space follows its steering.
            center.x = side * (outer + .008f);
            var outward = _chassis.TransformDirection(Vector3.right * side);
            var rotation = Quaternion.Inverse(pivot.rotation) * Quaternion.LookRotation(outward, _chassis.up);
            return new Wheel
            {
                Pivot = pivot,
                Point = pivot.InverseTransformPoint(_chassis.TransformPoint(center)),
                Rotation = rotation,
                Outward = pivot.InverseTransformVector(outward),
                ModelScale = new Vector3(
                    Mathf.Max(.001f, pivot.TransformVector(rotation * Vector3.right).magnitude),
                    Mathf.Max(.001f, pivot.TransformVector(rotation * Vector3.up).magnitude),
                    Mathf.Max(.001f, pivot.TransformVector(rotation * Vector3.forward).magnitude))
            };
        }

        internal void ApplyWheels(int style, int finish, int sidewall)
        {
            foreach (var slot in _alloy)
            {
                slot.Restore();
                if (finish == 0) continue;
                slot.Renderer.GetPropertyBlock(_block, slot.Index);
                _block.SetColor(_colorProperty, GarageCustomization.RimColors[finish]);
                _block.SetFloat(_metal, finish == 1 ? .58f : .79f);
                _block.SetFloat(_gloss, finish == 1 ? .40f : .66f);
                slot.Renderer.SetPropertyBlock(_block, slot.Index);
                _block.Clear();
            }

            if (style > 0)
            {
                _rimMeshes[style] ??= GarageRimMesh.Face(style, GarageCustomization.Options(_spec.id, GarageUpgrade.Wheels)[style]);
                _rim ??= MakeMaterial("Custom alloy face", GarageCustomization.RimColors[finish], .79f, .63f);
                _barrel ??= MakeMaterial("Dark wheel barrel", new Color(.06f,.075f,.077f), .40f, .32f);
                _bolts ??= MakeMaterial("Machined lug bolts", new Color(.51f,.53f,.55f), .85f, .63f);
                _rim.color = GarageCustomization.RimColors[finish];
                _rim.SetFloat(_gloss, finish == 1 ? .38f : .63f);
            }
            if (sidewall > 0)
            {
                _sidewallMeshes[sidewall] ??= GarageRimMesh.Sidewall(sidewall);
                if (sidewall == 1) _whitewall ??= MakeMaterial("Whitewall rubber", new Color(.82f,.78f,.66f), .04f, .31f);
                else _pinstripe ??= MakeMaterial("Coral rubber pinstripe", new Color(.74f,.18f,.095f), .03f, .32f);
            }
            foreach (var wheel in _wheels)
            {
                if (style > 0 && wheel.Rim == null)
                {
                    wheel.Rim = WheelPart(wheel, "Custom rim face", 0f, out var renderer);
                    renderer.sharedMaterials = new[] { _rim, _barrel, _bolts };
                }
                if (wheel.Rim != null)
                {
                    wheel.Rim.sharedMesh = _rimMeshes[style];
                    wheel.Rim.gameObject.SetActive(style > 0);
                }
                if (sidewall > 0 && wheel.Sidewall == null)
                    wheel.Sidewall = WheelPart(wheel, "Custom tyre sidewall", .003f, out wheel.SidewallRenderer);
                if (wheel.Sidewall == null) continue;
                wheel.Sidewall.sharedMesh = _sidewallMeshes[sidewall];
                wheel.SidewallRenderer.sharedMaterial = sidewall == 1 ? _whitewall : _pinstripe;
                wheel.Sidewall.gameObject.SetActive(sidewall > 0);
            }
        }

        private MeshFilter WheelPart(Wheel wheel, string name, float offset, out MeshRenderer renderer)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(wheel.Pivot, false);
            part.transform.SetLocalPositionAndRotation(wheel.Point + wheel.Outward * offset, wheel.Rotation);
            var radius = _spec.wheelRadius;
            part.transform.localScale = new Vector3(radius / wheel.ModelScale.x, radius / wheel.ModelScale.y, radius * .43f / wheel.ModelScale.z);
            renderer = part.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return part.GetComponent<MeshFilter>();
        }

        internal void ApplyHeadlights(int option)
        {
            foreach (var slot in _lamps)
            {
                slot.Restore();
                if (option == 0) continue;
                slot.Renderer.GetPropertyBlock(_block, slot.Index);
                var color = GarageCustomization.Lamps[option].Color;
                _block.SetColor(_colorProperty, color);
                _block.SetColor(_emission, color * (option == 3 ? .93f : .64f));
                slot.Renderer.SetPropertyBlock(_block, slot.Index);
                _block.Clear();
            }
        }

        internal void ApplyWindows(int option)
        {
            foreach (var slot in _windows)
            {
                slot.Restore();
                if (option == 0) continue;
                slot.Renderer.GetPropertyBlock(_block, slot.Index);
                _block.SetColor(_colorProperty, GarageCustomization.WindowColors[option]);
                slot.Renderer.SetPropertyBlock(_block, slot.Index);
                _block.Clear();
            }
        }

        internal void ApplyAuxiliary(int option, int headlight)
        {
            ReplaceAccessory(ref _auxiliary, "Garage auxiliary lights", option);
            if (option == 0) return;
            LampPositions(out var left, out var right);
            var width = _spec.size.x;
            var bumper = (left + right) * .5f;
            bumper.y -= Mathf.Min(.33f, _spec.size.y * .13f);
            bumper.z = Mathf.Max(bumper.z, _spec.size.z * .5f) + .034f;
            _guard ??= MakeMaterial("Powder-coated accessory steel", new Color(.055f, .077f, .080f), .52f, .38f);
            _auxLens ??= MakeMaterial("Auxiliary lamp lens", new Color(.88f,.92f,1f), .26f, .78f);
            _auxLens.EnableKeyword("_EMISSION");
            _auxLens.color = GarageCustomization.Lamps[headlight].Color;
            _auxLens.SetColor(_emission, _auxLens.color * .70f);
            if (option == 1)
            {
                foreach (var side in new[] { -1, 1 })
                {
                    var point = bumper + Vector3.right * (width * .22f * side);
                    Part(_auxiliary.transform, "Fog pod housing", point, new Vector3(.23f,.137f,.084f), _guard);
                    Part(_auxiliary.transform, "Fog pod lens", point + Vector3.forward * .046f, new Vector3(.173f,.091f,.018f), _auxLens);
                }
                return;
            }

            Part(_auxiliary.transform, "LED bar housing", bumper, new Vector3(width * .49f,.105f,.093f), _guard);
            for (var i = -4; i <= 4; i++)
                Part(_auxiliary.transform, "LED bar emitter", bumper + new Vector3(i * width * .046f,0,.051f),
                    new Vector3(width * .032f,.064f,.012f), _auxLens);
        }

        internal void ApplyBodyKit(int option)
        {
            ReplaceAccessory(ref _bodyKit, "Garage body kit", option);
            if (option == 0) return;
            _guard ??= MakeMaterial("Powder-coated accessory steel", new Color(.055f, .077f, .080f), .52f, .38f);
            _trim ??= MakeMaterial("Brushed body kit insert", new Color(.48f,.51f,.48f), .72f, .48f);
            var size = _spec.size;
            if (GarageCustomization.IsUtility(_spec.id))
            {
                LampPositions(out var left, out var right);
                var height = (left.y + right.y) * .5f - Mathf.Min(.25f, size.y * .11f);
                var depth = Mathf.Max((left.z + right.z) * .5f + .038f, size.z * .5f + .024f);
                if (option == 1)
                {
                    Part(_bodyKit.transform,"Front guard beam",new Vector3(0,height,depth),new Vector3(size.x*.70f,.082f,.090f),_guard);
                    Part(_bodyKit.transform,"Front guard face",new Vector3(0,height,depth+.047f),new Vector3(size.x*.57f,.037f,.009f),_trim);
                    foreach (var side in new[] { -1, 1 })
                        Part(_bodyKit.transform,"Guard stanchion",new Vector3(side*size.x*.21f,height-.034f,depth-.019f),new Vector3(.088f,.22f,.107f),_guard);
                    return;
                }

                _recovery ??= MakeMaterial("Recovery hardware enamel", new Color(.67f,.15f,.061f), .55f, .53f);
                Part(_bodyKit.transform,"Front skid plate",new Vector3(0,height-.06f,depth-.021f),new Vector3(size.x*.54f,.175f,.08f),_guard);
                foreach (var side in new[] { -1, 1 })
                {
                    var hook = new Vector3(side*size.x*.19f,height-.13f,depth+.031f);
                    Part(_bodyKit.transform,"Recovery mount",hook,new Vector3(.132f,.055f,.103f),_trim);
                    Part(_bodyKit.transform,"Recovery eye",hook + Vector3.down * .048f,new Vector3(.083f,.065f,.023f),_recovery);
                }
                return;
            }

            if (option == 1)
            {
                Part(_bodyKit.transform,"Front splitter",new Vector3(0,size.y*.27f,size.z*.493f),new Vector3(size.x*.89f,.042f,.18f),_guard);
                foreach (var side in new[] { -1, 1 })
                    Part(_bodyKit.transform,"Splitter end plate",new Vector3(side*size.x*.436f,size.y*.28f,size.z*.473f),new Vector3(.032f,.083f,.173f),_guard);
            }
            else
            {
                var height = size.y * (_spec.id is "apex" or "velocity" ? .68f : .79f);
                Part(_bodyKit.transform,"Rear lip spoiler",new Vector3(0,height,-size.z*.46f),new Vector3(size.x*.74f,.057f,.190f),_guard);
                Part(_bodyKit.transform,"Rear lip edge",new Vector3(0,height+.027f,-size.z*.51f),new Vector3(size.x*.71f,.020f,.019f),_trim);
            }
        }

        private void ReplaceAccessory(ref GameObject accessory, string name, int option)
        {
            if (accessory != null)
            {
                accessory.SetActive(false);
                Destroy(accessory);
                accessory = null;
            }
            if (option == 0) return;
            accessory = new GameObject(name);
            accessory.transform.SetParent(transform, false);
        }

        private void Part(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = name;
            var collider = primitive.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            primitive.transform.SetPositionAndRotation(_chassis.TransformPoint(position), _chassis.rotation);
            primitive.transform.localScale = size;
            primitive.transform.SetParent(parent, true);
            primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private Material MakeMaterial(string name, Color color, float metallic, float glossiness)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat(_metal, metallic);
            material.SetFloat(_gloss, glossiness);
            _materials.Add(material);
            return material;
        }

        private Bounds InChassisSpace(Transform source, Bounds bounds)
        {
            var result = new Bounds(_chassis.InverseTransformPoint(source.TransformPoint(bounds.center)), Vector3.zero);
            for (var corner = 0; corner < 8; corner++)
            {
                var direction = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                result.Encapsulate(_chassis.InverseTransformPoint(source.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, direction))));
            }
            return result;
        }

        private void OnDestroy()
        {
            foreach (var material in _materials) Destroy(material);
            foreach (var mesh in _rimMeshes) if (mesh != null) Destroy(mesh);
            foreach (var mesh in _sidewallMeshes) if (mesh != null) Destroy(mesh);
        }
    }
}
