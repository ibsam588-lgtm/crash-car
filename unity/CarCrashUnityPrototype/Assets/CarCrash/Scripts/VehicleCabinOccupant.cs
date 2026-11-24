using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    // One small opaque mesh per car, drawn before the existing transparent window glass.
    public sealed class VehicleCabinOccupant : MonoBehaviour
    {
        private const string DriverName = "Cabin driver";
        private static readonly Mesh[] DriverMeshes = new Mesh[4];
        private static Material _material;
        private ArcadeVehicleController _driverInput;
        private float _turn;

        public static void InstallPlayer(Transform chassis, string vehicleId)
        {
            var oldDriver = chassis.Find(DriverName);
            if (oldDriver != null)
            {
                oldDriver.gameObject.SetActive(false);
                oldDriver.SetParent(null);
                Destroy(oldDriver.gameObject);
            }

            var driver = Create(chassis, Seat(vehicleId), Quaternion.identity, 0);
            driver.gameObject.AddComponent<VehicleCabinOccupant>()._driverInput = chassis.GetComponent<ArcadeVehicleController>();
        }

        public static void InstallTraffic(Transform chassis, Vector3 velocity, float chassisMass)
        {
            if (chassis.Find(DriverName) != null) return;

            // Civilian prefabs each use the same chassis mass when they are placed in a lane.
            var vehicleId = chassisMass >= 2200f ? "pickup" : chassisMass >= 1750f ? "suv" : chassisMass >= 1440f ? "apex" : "roadbreaker";
            var heading = Vector3.ProjectOnPlane(velocity, Vector3.up);
            var localHeading = heading.sqrMagnitude > .01f ? chassis.InverseTransformDirection(heading.normalized) : Vector3.forward;
            var style = Mathf.Abs(Mathf.RoundToInt(chassis.position.z) + Mathf.RoundToInt(chassis.position.x) * 13) % DriverMeshes.Length;
            Create(chassis, Seat(vehicleId), Quaternion.LookRotation(localHeading), style);
        }

        // Measured in the authored vehicle coordinate systems at the front seats. The hauler's
        // cab is ahead of its cargo box, and the monster shares the pickup cab 1.2 m higher.
        private static Vector3 Seat(string vehicleId) => vehicleId switch
        {
            "apex" => new(-.33f, 1.02f, .03f),
            "suv" => new(-.38f, 1.49f, .29f),
            "pickup" => new(-.41f, 1.61f, .38f),
            "hauler" => new(-.49f, 2.14f, 2.19f),
            "monster" => new(-.41f, 2.81f, .38f),
            _ => new(-.35f, 1.15f, .09f)
        };

        private static Transform Create(Transform chassis, Vector3 headPosition, Quaternion yaw, int style)
        {
            var driver = new GameObject(DriverName, typeof(MeshFilter), typeof(MeshRenderer));
            driver.transform.SetParent(chassis, false);
            driver.transform.SetLocalPositionAndRotation(yaw * headPosition, yaw);

            if (DriverMeshes[style] == null) DriverMeshes[style] = BuildDriver(style);
            if (_material == null)
            {
                var shader = Resources.Load<Shader>("CabinDriver") ?? Shader.Find("Standard");
                _material = new Material(shader) { name = "Cabin occupants" };
            }
            driver.GetComponent<MeshFilter>().sharedMesh = DriverMeshes[style];
            var renderer = driver.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return driver.transform;
        }

        private void LateUpdate()
        {
            if (_driverInput == null) return;
            _turn = Mathf.MoveTowards(_turn, _driverInput.CurrentSteer, Time.deltaTime * 3f);
            transform.localRotation = Quaternion.Euler(0f, _turn * 6f, -_turn * 2f);
        }

        private static Mesh BuildDriver(int style)
        {
            var skin = style switch
            {
                0 => new Color(.76f, .51f, .34f), 1 => new Color(.44f, .26f, .17f),
                2 => new Color(.86f, .64f, .51f), _ => new Color(.63f, .42f, .28f)
            };
            var hair = style == 2 ? new Color(.24f, .13f, .07f) : new Color(.065f, .045f, .038f);
            var jacket = style switch
            {
                0 => new Color(.17f, .36f, .37f), 1 => new Color(.44f, .25f, .19f),
                2 => new Color(.39f, .44f, .29f), _ => new Color(.25f, .33f, .46f)
            };
            var jeans = new Color(.12f, .15f, .19f);
            var belt = new Color(.24f, .26f, .27f);
            var inside = new Color(.15f, .092f, .068f);
            var cloth = new Color(.62f, .61f, .53f);
            var mesh = new DriverMesh();

            mesh.Ellipse(new(0, 0, 0), new(.184f, .223f, .199f), skin, 14, 9);
            mesh.Ellipse(new(0, -.041f, .023f), new(.158f, .139f, .177f), skin);
            mesh.Ellipse(new(0, .055f, -.024f), new(.195f, .136f, .202f), hair, 14, 7);
            mesh.Ellipse(new(0, -.005f, .094f), new(.041f, .052f, .036f), skin * .94f);
            mesh.Ellipse(new(0, -.052f, .100f), new(.046f, .008f, .008f), inside);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                mesh.Ellipse(new(side * .086f, -.012f, -.004f), new(.027f, .053f, .032f), skin);
                mesh.Ellipse(new(side * .039f, .025f, .092f), new(.032f, .012f, .010f), inside);
                mesh.Ellipse(new(side * .039f, .043f, .089f), new(.038f, .010f, .012f), hair);
            }
            mesh.Ellipse(new(0, -.147f, -.016f), new(.105f, .18f, .109f), skin);
            mesh.Ellipse(new(0, -.229f, -.016f), new(.193f, .081f, .164f), cloth);
            mesh.Ellipse(new(0, -.352f, -.028f), new(.366f, .364f, .245f), jacket, 14, 9);
            mesh.Ellipse(new(0, -.514f, .007f), new(.316f, .231f, .226f), jacket);
            mesh.Ellipse(new(0, -.594f, .027f), new(.335f, .179f, .248f), jeans);
            mesh.Limb(new(-.17f, -.244f, .070f), new(.13f, -.508f, .104f), .029f, belt, .44f);

            for (var side = -1f; side <= 1f; side += 2f)
            {
                var shoulder = new Vector3(side * .156f, -.284f, -.022f);
                var elbow = new Vector3(side * .211f, -.437f, .148f);
                var hand = new Vector3(side * .126f, -.365f, .372f);
                mesh.Limb(shoulder, elbow, .121f, jacket);
                mesh.Limb(elbow, hand, .098f, jacket);
                mesh.Ellipse(hand, new(.079f, .069f, .099f), skin);
                var hip = new Vector3(side * .092f, -.591f, .039f);
                var knee = new Vector3(side * .114f, -.644f, .385f);
                mesh.Limb(hip, knee, .155f, jeans);
                mesh.Limb(knee, new Vector3(side * .107f, -.869f, .465f), .123f, jeans);
            }
            return mesh.Create();
        }

        private sealed class DriverMesh
        {
            private readonly List<Vector3> _vertices = new();
            private readonly List<Vector3> _normals = new();
            private readonly List<Color> _colors = new();
            private readonly List<int> _triangles = new();

            public void Ellipse(Vector3 center, Vector3 size, Color tint, int sides = 10, int rings = 6)
                => Ellipse(center, size, tint, Quaternion.identity, sides, rings);

            public void Limb(Vector3 from, Vector3 to, float thickness, Color tint, float depth = .85f)
            {
                var axis = to - from;
                Ellipse((from + to) * .5f, new Vector3(thickness, axis.magnitude + thickness * .72f, thickness * depth),
                    tint, Quaternion.FromToRotation(Vector3.up, axis), 10, 6);
            }

            private void Ellipse(Vector3 center, Vector3 size, Color tint, Quaternion rotation, int sides, int rings)
            {
                var start = _vertices.Count;
                for (var ring = 0; ring <= rings; ring++)
                {
                    var polar = Mathf.PI * ring / rings;
                    for (var side = 0; side < sides; side++)
                    {
                        var angle = Mathf.PI * 2f * side / sides;
                        var point = new Vector3(Mathf.Sin(polar) * Mathf.Cos(angle), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(angle));
                        _vertices.Add(center + rotation * Vector3.Scale(point, size * .5f));
                        _normals.Add(rotation * new Vector3(point.x / size.x, point.y / size.y, point.z / size.z).normalized);
                        _colors.Add(tint);
                        if (ring == rings) continue;

                        var current = start + ring * sides + side;
                        var next = start + ring * sides + (side + 1) % sides;
                        _triangles.Add(current);
                        _triangles.Add(next);
                        _triangles.Add(current + sides);
                        _triangles.Add(current + sides);
                        _triangles.Add(next);
                        _triangles.Add(next + sides);
                    }
                }
            }

            public Mesh Create()
            {
                var mesh = new Mesh { name = "Seated driver" };
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetColors(_colors);
                mesh.SetTriangles(_triangles, 0);
                mesh.UploadMeshData(true);
                return mesh;
            }
        }
    }
}
