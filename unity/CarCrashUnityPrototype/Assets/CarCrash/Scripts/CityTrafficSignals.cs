using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    public enum CitySignalAspect { Red, Amber, Green, Unlit }

    [DisallowMultipleComponent]
    public sealed class CityTrafficSignals : MonoBehaviour
    {
        private const float SignalRange = 155f;
        private const float AllRedSeconds = 1.25f;
        private const float AmberSeconds = 2.5f;
        private const float AvenueGreenSeconds = 13f;
        private const float StreetGreenSeconds = 9f;
        private const float AvenuePhaseSeconds = AvenueGreenSeconds + AmberSeconds + AllRedSeconds;
        private const float CycleSeconds = AvenuePhaseSeconds + StreetGreenSeconds + AmberSeconds + AllRedSeconds;
        private const string LegacyLensMaterial = "M_Signal_Green";

        private static CityTrafficSignals _active;
        private readonly List<SignalPole> _poles = new();
        private readonly Material[] _signalMaterials = new Material[6];
        private readonly Material[][] _aspectMaterials = new Material[4][];
        private Mesh _headMesh, _armMesh;
        private Transform _player;
        private float _nextRefresh;
        public bool HasSignalHeads => _poles.Count > 0;

        private sealed class SignalPole
        {
            public BreakableCityProp fixture;
            public float avenue, street, side;
            public GameObject visual;
            public MeshRenderer avenueHead, streetHead;
            public CitySignalAspect previousAvenue = (CitySignalAspect)(-1);
            public CitySignalAspect previousStreet = (CitySignalAspect)(-1);
        }

        public void Initialize(Transform player)
        {
            if (HasSignalHeads) return;
            _player = player;
            var shader = Resources.Load<Shader>("RoadTurnPaint");
            if (shader == null || !shader.isSupported) return;
            Material housing = null;
            foreach (var fixture in GetComponentsInChildren<BreakableCityProp>())
            {
                if (!fixture.name.StartsWith("Breakaway traffic signal", StringComparison.Ordinal)) continue;
                var position = fixture.transform.position;
                var avenue = CityRoadNetwork.Nearest(CityRoadNetwork.AvenueX, position.x);
                var street = CityRoadNetwork.Nearest(CityRoadNetwork.CrossStreetZ, position.z);
                _poles.Add(new SignalPole { fixture = fixture, avenue = avenue, street = street, side = Mathf.Sign(position.x - avenue) });
                foreach (var renderer in fixture.GetComponentsInChildren<Renderer>())
                {
                    var material = renderer.sharedMaterial;
                    if (IsLegacyLens(material)) renderer.gameObject.SetActive(false);
                    else if (material != null && material.name == "M_City_Powdercoat") housing = material;
                }
            }
            if (!HasSignalHeads) return;

            _signalMaterials[0] = Lens(shader, "Signal red active", new Color(1f, .045f, .026f));
            _signalMaterials[1] = Lens(shader, "Signal amber active", new Color(1f, .62f, .045f));
            _signalMaterials[2] = Lens(shader, "Signal green active", new Color(.045f, .91f, .35f));
            _signalMaterials[3] = Lens(shader, "Signal red inactive", new Color(.16f, .027f, .024f));
            _signalMaterials[4] = Lens(shader, "Signal amber inactive", new Color(.15f, .082f, .024f));
            _signalMaterials[5] = Lens(shader, "Signal green inactive", new Color(.018f, .13f, .046f));
            for (var aspect = 0; aspect < _aspectMaterials.Length; aspect++)
            {
                var materials = new Material[4];
                materials[0] = housing != null ? housing : _signalMaterials[5];
                for (var lens = 0; lens < 3; lens++) materials[lens + 1] = _signalMaterials[lens == aspect ? lens : lens + 3];
                _aspectMaterials[aspect] = materials;
            }
            _headMesh = MakeHeadMesh();
            _armMesh = MakeArmMesh();
            _active = this;
            Refresh();
        }

        internal static bool IsLegacyLens(Material material) => material != null && material.name == LegacyLensMaterial;

        public static CitySignalAspect AspectAt(float time, float avenue, float street, bool alongAvenue)
        {
            // Neighboring intersections are offset to avoid citywide pulses while opposing faces agree.
            var elapsed = Mathf.Repeat(time + avenue * .036f + (street - 35f) * .047f, CycleSeconds);
            if (alongAvenue)
            {
                if (elapsed < AvenueGreenSeconds) return CitySignalAspect.Green;
                if (elapsed < AvenueGreenSeconds + AmberSeconds) return CitySignalAspect.Amber;
            }
            else if (elapsed >= AvenuePhaseSeconds)
            {
                elapsed -= AvenuePhaseSeconds;
                if (elapsed < StreetGreenSeconds) return CitySignalAspect.Green;
                if (elapsed < StreetGreenSeconds + AmberSeconds) return CitySignalAspect.Amber;
            }
            return CitySignalAspect.Red;
        }

        public static float LimitAvenueSpeed(Vector3 position, Vector3 heading, float halfLength, float speed, float desired)
        {
            if (_active == null || Mathf.Abs(heading.z) < .85f) return desired;
            return AvenueApproachSpeed(position, heading.z, halfLength, speed, desired, Time.time);
        }

        public static float AvenueApproachSpeed(Vector3 position, float directionZ, float halfLength, float speed, float desired, float time)
        {
            var avenue = CityRoadNetwork.Nearest(CityRoadNetwork.AvenueX, position.x);
            if (Mathf.Abs(position.x - avenue) > CityRoadNetwork.AvenueHalfWidth) return desired;
            var direction = Mathf.Sign(directionZ);
            var closest = float.PositiveInfinity;
            var crossing = 0f;
            foreach (var street in CityRoadNetwork.CrossStreetZ)
            {
                var stopLine = street - direction * (CityRoadNetwork.CrossStreetHalfWidth + 1.05f);
                var distance = (stopLine - position.z) * direction - halfLength;
                if (distance < -.15f || distance >= closest) continue;
                closest = distance;
                crossing = street;
            }
            if (closest > Mathf.Max(13f, desired * 2.4f)) return desired;
            var light = AspectAt(time, avenue, crossing, true);
            if (light == CitySignalAspect.Green) return desired;
            // Let drivers already too close for a comfortable amber stop clear the crossing.
            if (light == CitySignalAspect.Amber && speed > 3f && closest < speed * speed / 15f + .65f) return desired;
            return Mathf.Min(desired, Mathf.Max(0f, closest - .46f) * 1.48f);
        }

        private void LateUpdate()
        {
            if (!HasSignalHeads || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .12f;
            Refresh();
        }

        private void Refresh()
        {
            var observer = _player != null ? _player.position : Vector3.zero;
            foreach (var pole in _poles)
            {
                if (pole.fixture == null) continue;
                var visible = (observer - pole.fixture.transform.position).sqrMagnitude <= SignalRange * SignalRange;
                if (!visible)
                {
                    if (pole.visual != null && pole.visual.activeSelf) pole.visual.SetActive(false);
                    continue;
                }
                if (pole.visual == null) BuildPole(pole);
                if (!pole.visual.activeSelf) pole.visual.SetActive(true);
                var avenue = pole.fixture.Broken ? CitySignalAspect.Unlit : AspectAt(Time.time, pole.avenue, pole.street, true);
                var street = pole.fixture.Broken ? CitySignalAspect.Unlit : AspectAt(Time.time, pole.avenue, pole.street, false);
                if (avenue != pole.previousAvenue)
                {
                    pole.avenueHead.sharedMaterials = _aspectMaterials[(int)avenue];
                    pole.previousAvenue = avenue;
                }
                if (street != pole.previousStreet)
                {
                    pole.streetHead.sharedMaterials = _aspectMaterials[(int)street];
                    pole.previousStreet = street;
                }
            }
        }

        private void BuildPole(SignalPole pole)
        {
            // The signal heads follow the rigidbody and remain part of the breakaway pole after a crash.
            pole.visual = new GameObject("Working signal heads");
            pole.visual.transform.SetParent(pole.fixture.transform, false);
            var root = pole.visual.transform;
            pole.avenueHead = Part(root, "Avenue signal", _headMesh,
                new Vector3(-pole.side * 4.8f, 5.05f, 0f), Quaternion.LookRotation(Vector3.forward * pole.side));
            pole.streetHead = Part(root, "Cross-street signal", _headMesh,
                new Vector3(0f, 5.05f, pole.side * 4.55f), Quaternion.LookRotation(Vector3.left * pole.side));
            var arm = Part(root, "Cross-street signal arm", _armMesh, new Vector3(0f, 5.6f, pole.side * 2.28f), Quaternion.identity);
            arm.sharedMaterial = _aspectMaterials[0][0];
        }

        private static MeshRenderer Part(Transform parent, string name, Mesh mesh, Vector3 position, Quaternion rotation)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return renderer;
        }

        private static Material Lens(Shader shader, string name, Color color) => new(shader) { name = name, color = color };

        private static Mesh MakeHeadMesh()
        {
            var vertices = new List<Vector3>();
            var shell = new List<int>();
            Box(vertices, shell, Vector3.zero, new Vector3(.43f, 1.04f, .30f));
            var mesh = new Mesh { name = "Three-aspect traffic signal", subMeshCount = 4 };
            var lenses = new List<int>[3];
            for (var lamp = 0; lamp < lenses.Length; lamp++)
            {
                var y = .28f - lamp * .28f;
                Box(vertices, shell, new Vector3(0f, y + .126f, .205f), new Vector3(.27f, .042f, .18f));
                lenses[lamp] = new List<int>();
                Disc(vertices, lenses[lamp], y);
            }
            mesh.SetVertices(vertices);
            mesh.SetTriangles(shell, 0, false);
            for (var lamp = 0; lamp < lenses.Length; lamp++) mesh.SetTriangles(lenses[lamp], lamp + 1, false);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh MakeArmMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Box(vertices, triangles, Vector3.zero, new Vector3(.13f, .13f, 4.55f));
            var mesh = new Mesh { name = "Signal mounting arm" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void Disc(List<Vector3> vertices, List<int> triangles, float y)
        {
            const int sides = 12;
            var center = vertices.Count;
            vertices.Add(new Vector3(0f, y, .195f));
            for (var edge = 0; edge < sides; edge++)
            {
                var angle = edge * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(.111f * Mathf.Cos(angle), y + .111f * Mathf.Sin(angle), .171f));
                triangles.Add(center);
                triangles.Add(center + 1 + edge);
                triangles.Add(center + 1 + (edge + 1) % sides);
            }
        }

        private static void Box(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size)
        {
            var half = size * .5f;
            var min = center - half;
            var max = center + half;
            var first = vertices.Count;
            vertices.AddRange(new[]
            {
                new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z)
            });
            foreach (var index in BoxTriangles) triangles.Add(first + index);
        }

        private static readonly int[] BoxTriangles =
        {
            0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4,
            3,7,6, 3,6,2, 0,4,7, 0,7,3, 1,2,6, 1,6,5
        };

        private void OnDestroy()
        {
            if (_active == this) _active = null;
            if (_headMesh != null) Destroy(_headMesh);
            if (_armMesh != null) Destroy(_armMesh);
            foreach (var material in _signalMaterials) if (material != null) Destroy(material);
        }
    }
}
