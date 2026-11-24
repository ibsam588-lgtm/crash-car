using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    [DefaultExecutionOrder(250)]
    internal sealed class GarageShowroom : MonoBehaviour
    {
        // Keep the paused showroom vehicle, lighting and meshes outside the driving world.
        internal static readonly Vector3 FloorPosition = new(0f, -400f, 0f);
        private const float WallDistance = 24f;

        private readonly List<Material> _materials = new();
        private Rigidbody _vehicleBody;
        private ArcadeVehicleController _controller;
        private bool _settleOnPlatform;
        private Vector3 _roadPosition;
        private Quaternion _roadRotation;
        private RigidbodyInterpolation _roadInterpolation;

        internal static GarageShowroom Create()
        {
            var room = new GameObject("Elysian workshop").AddComponent<GarageShowroom>();
            room.transform.position = FloorPosition;
            room.Build();
            return room;
        }

        internal void Enter(Rigidbody body, GarageVehicle vehicle)
        {
            _vehicleBody = body;
            _controller = body.GetComponent<ArcadeVehicleController>();
            _roadPosition = body.position;
            _roadRotation = body.rotation;
            _roadInterpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            gameObject.SetActive(true);
            PlaceVehicle(vehicle);
        }

        internal void PlaceVehicle(GarageVehicle vehicle)
        {
            if (_vehicleBody == null) return;
            var tireBottom = 0f;
            if (vehicle?.wheelCenters != null)
            {
                foreach (var center in vehicle.wheelCenters)
                    tireBottom = Mathf.Min(tireBottom, center.y - vehicle.wheelRadius);
            }
            PlaceBody(FloorPosition + Vector3.up * -tireBottom, Quaternion.identity);
            _settleOnPlatform = true;
        }

        internal void Leave()
        {
            if (_vehicleBody == null) return;
            PlaceBody(_roadPosition, _roadRotation);
            _vehicleBody.interpolation = _roadInterpolation;
            _vehicleBody = null;
            _controller = null;
            _settleOnPlatform = false;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_settleOnPlatform || _controller == null) return;
            _settleOnPlatform = false;
            // Wheel meshes and an unloaded suspension can differ from the spec's
            // nominal radius. Seat the visible tires after the controller syncs them.
            var bottom = Mathf.Min(WheelBottom(_controller.frontLeftVisual), WheelBottom(_controller.frontRightVisual),
                WheelBottom(_controller.rearLeftVisual), WheelBottom(_controller.rearRightVisual));
            if (float.IsPositiveInfinity(bottom)) return;
            PlaceBody(_vehicleBody.position + Vector3.up * (FloorPosition.y - bottom - .004f), _vehicleBody.rotation);
        }

        private static float WheelBottom(Transform wheel)
        {
            var bottom = float.PositiveInfinity;
            if (wheel == null) return bottom;
            foreach (var mesh in wheel.GetComponentsInChildren<MeshRenderer>())
                if (mesh.enabled) bottom = Mathf.Min(bottom, mesh.bounds.min.y);
            return bottom;
        }

        private void PlaceBody(Vector3 position, Quaternion rotation)
        {
            _vehicleBody.transform.SetPositionAndRotation(position, rotation);
            _vehicleBody.position = position;
            _vehicleBody.rotation = rotation;
            Physics.SyncTransforms();
        }

        private void Build()
        {
            var concrete = Surface("Sealed concrete", 0x253638, .29f);
            var joints = Surface("Expansion joints", 0x243437, .16f);
            var dark = Surface("Blackened steel", 0x142A2E, .40f, .30f);
            var teal = Surface("Petrol enamel", 0x245357, .55f, .28f);
            var stone = Surface("Coastal limestone", 0x9C8C72, .19f);
            var wood = Surface("Smoked oak", 0x745237, .20f);
            var brass = Surface("Satin brass", 0xC49055, .56f, .45f);
            var pad = Surface("Turntable finish", 0x35454A, .51f, .18f);
            var ice = Surface("Frosted window glass", 0x47747A, .52f);
            var light = Surface("Warm light strip", 0xFFE2B6, .28f, emission: .85f);
            var blueLight = Surface("Service bay light", 0xA4E8E9, .36f, emission: .60f);

            Box(transform, "Workshop floor", new Vector3(0f, -.25f, 0f), new Vector3(49f, .32f, 49f), concrete, true);
            for (var i = -20; i <= 20; i += 5)
            {
                Box(transform, "Concrete control joint", new Vector3(i, -.087f, 0f), new Vector3(.024f, .006f, 48f), joints);
                Box(transform, "Concrete control joint", new Vector3(0f, -.086f, i), new Vector3(48f, .006f, .024f), joints);
            }

            Cylinder("Turntable edge", -.096f, 11.75f, .070f, dark);
            Cylinder("Machined trim", -.052f, 11.66f, .033f, brass);
            Cylinder("Vehicle platform", -.033f, 11.48f, .033f, pad, true);
            for (var angle = 0; angle < 360; angle += 30)
            {
                var rotation = Quaternion.Euler(0f, angle, 0f);
                var index = Box(transform, "Turntable marker", rotation * new Vector3(0f, .006f, 5.55f),
                    new Vector3(.34f, .012f, .035f), brass);
                index.localRotation = rotation;
            }

            for (var side = -1; side <= 1; side += 2)
            {
                Box(transform, "Flush alignment rail", new Vector3(side * 7.15f, -.086f, 0f),
                    new Vector3(.12f, .010f, 15.8f), dark);
                for (var end = -1; end <= 1; end += 2)
                    Box(transform, "Alignment rail reflector", new Vector3(side * 7.15f, -.078f, end * 6.05f),
                        new Vector3(.055f, .008f, 2.55f), brass);
            }

            for (var side = 0; side < 4; side++)
            {
                var wall = new GameObject("Workshop wall").transform;
                wall.SetParent(transform, false);
                wall.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
                Box(wall, "Limestone workshop wall", new Vector3(0f, 3.8f, WallDistance), new Vector3(49f, 7.8f, .45f), stone);
                Box(wall, "Lower steel cladding", new Vector3(0f, 1.05f, WallDistance - .32f), new Vector3(48f, 2.2f, .34f), dark);
                Box(wall, "Brass wall channel", new Vector3(0f, 2.20f, WallDistance - .53f), new Vector3(48f, .11f, .15f), brass);
                Box(wall, "Indirect light", new Vector3(0f, 2.28f, WallDistance - .45f), new Vector3(47f, .035f, .08f), light);
                for (var pier = -1; pier <= 1; pier += 2)
                    Box(wall, "Limestone pier", new Vector3(pier * 21.4f, 3.7f, WallDistance - .52f),
                        new Vector3(.75f, 7.4f, .54f), stone);

                if (side % 2 == 0)
                {
                    Box(wall, "Coastal window casing", new Vector3(0f, 4.20f, WallDistance - .28f), new Vector3(21f, 3.5f, .32f), dark);
                    for (var panel = -2; panel <= 2; panel++)
                    {
                        Box(wall, "Frosted coastal window", new Vector3(panel * 4.14f, 4.20f, WallDistance - .48f),
                            new Vector3(3.94f, 3.0f, .12f), ice);
                        Box(wall, "Window sill", new Vector3(panel * 4.14f, 2.74f, WallDistance - .61f),
                            new Vector3(3.96f, .045f, .12f), blueLight);
                    }
                }
                else
                {
                    Box(wall, "Slatted acoustic backing", new Vector3(0f, 4.18f, WallDistance - .27f), new Vector3(19f, 3.7f, .22f), dark);
                    for (var slat = -8; slat <= 8; slat++)
                        Box(wall, "Smoked oak slat", new Vector3(slat * 1.04f, 4.18f, WallDistance - .44f),
                            new Vector3(.84f, 3.45f, .16f), wood);
                    Box(wall, "Backing top light", new Vector3(0f, 6.05f, WallDistance - .57f), new Vector3(18f, .07f, .10f), light);
                }

                for (var bay = -1; bay <= 1; bay += 2)
                {
                    var x = bay * 12.7f;
                    Box(wall, "Cabinet body", new Vector3(x, .62f, WallDistance - 1.06f), new Vector3(3.7f, 1.45f, 1.32f), teal);
                    Box(wall, "Worktop", new Vector3(x, 1.39f, WallDistance - 1.06f), new Vector3(3.89f, .12f, 1.48f), wood);
                    Box(wall, "Tool chest", new Vector3(x + .85f, 1.67f, WallDistance - 1.00f),
                        new Vector3(1.38f, .47f, .62f), dark);
                    Box(wall, "Tool chest latch", new Vector3(x + .85f, 1.71f, WallDistance - 1.33f),
                        new Vector3(.27f, .05f, .04f), brass);
                    for (var drawer = 0; drawer < 3; drawer++)
                    {
                        var y = .26f + drawer * .37f;
                        Box(wall, "Drawer face", new Vector3(x, y, WallDistance - 1.76f), new Vector3(3.5f, .31f, .12f), dark);
                        Box(wall, "Drawer pull", new Vector3(x, y + .07f, WallDistance - 1.84f), new Vector3(1.0f, .04f, .05f), brass);
                    }
                }
            }

            Box(transform, "Workshop ceiling", new Vector3(0f, 7.5f, 0f), new Vector3(49f, .42f, 49f), dark);
            for (var row = -1; row <= 1; row++)
            {
                var x = row * 6.9f;
                Box(transform, "Overhead steel fixture", new Vector3(x, 7.04f, 0f), new Vector3(.32f, .20f, 18f), dark);
                Box(transform, "Overhead light", new Vector3(x, 6.91f, 0f), new Vector3(.19f, .06f, 17.7f), light);
            }
            AddLight("Studio key", new Vector3(0f, 6.6f, 4.0f), new Color(1f, .91f, .79f), 2.65f, 18f);
            AddLight("Studio fill", new Vector3(-5.5f, 4.4f, -3.0f), new Color(.65f, .89f, 1f), 1.95f, 16f);
            AddLight("Studio rim", new Vector3(5.5f, 4.2f, -2.6f), new Color(1f, .75f, .52f), 1.45f, 15f);
        }

        private Material Surface(string name, int rgb, float smoothness, float metal = 0f, float emission = 0f)
        {
            var color = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metal);
            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", (Color)color * emission);
            }
            _materials.Add(material);
            return material;
        }

        private static Transform Box(Transform parent, string name, Vector3 position, Vector3 size,
            Material surface, bool receiveShadows = false) =>
            Piece(PrimitiveType.Cube, parent, name, position, size, surface, receiveShadows);

        private void Cylinder(string name, float y, float width, float halfHeight, Material surface, bool receiveShadows = false) =>
            Piece(PrimitiveType.Cylinder, transform, name, new Vector3(0f, y, 0f),
                new Vector3(width, halfHeight, width), surface, receiveShadows);

        private static Transform Piece(PrimitiveType primitive, Transform parent, string name, Vector3 position,
            Vector3 size, Material surface, bool receiveShadows)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = size;
            var collider = piece.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            var renderer = piece.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = surface;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
            return piece.transform;
        }

        private void AddLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var fixture = new GameObject(name);
            fixture.transform.SetParent(transform, false);
            fixture.transform.localPosition = position;
            var light = fixture.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = range;
            light.color = color;
            light.shadows = LightShadows.None;
        }

        private void OnDestroy()
        {
            foreach (var material in _materials) Destroy(material);
        }
    }
}
