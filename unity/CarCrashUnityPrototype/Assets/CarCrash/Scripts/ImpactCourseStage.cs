using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CarCrash
{
    internal sealed class ImpactCourseStage : IDisposable
    {
        private readonly GameObject _root;
        private readonly GameObject[] _gates;
        private readonly List<Material> _materials = new();
        private readonly Mesh _cone, _stripe, _ring, _arrow;
        private readonly Material _orange, _white, _rubber, _asphaltPaint, _blue, _gold, _wood, _woodTrim, _steel, _lens;
        private readonly Font _font;
        private readonly Material _signMaterial;
        private readonly GameObject _wreckModel;

        public ImpactCourseStage(ImpactMission mission, GameObject wreckModel)
        {
            var course = mission.Course;
            _root = new GameObject(course.Name + " / Closed stunt course");
            _gates = new GameObject[course.Gates.Length];
            _wreckModel = wreckModel;
            _font = Resources.Load<Font>("Fonts/Inter-SemiBold");
            _signMaterial = CitySignFont.CreateWorldMaterial(_font, _root);
            if (_signMaterial != null) _materials.Add(_signMaterial);
            _orange = Material("Traffic orange", new Color(.91f, .19f, .035f), 0, .52f);
            _white = Material("Reflective tape", new Color(.89f, .92f, .91f), .05f, .55f, .17f);
            _rubber = Material("Dark rubber and steel", new Color(.045f, .055f, .062f), .3f, .34f);
            _asphaltPaint = Material("Route markings", new Color(.16f, .72f, .86f), .06f, .4f, .42f);
            _blue = Material("Checkpoint teal", new Color(.04f, .42f, .55f), .28f, .56f, .55f);
            _gold = Material("Scoring target paint", new Color(1f, .66f, .14f), .03f, .45f, .48f);
            _wood = Material("Raw crate boards", new Color(.49f, .30f, .16f), 0, .24f);
            _woodTrim = Material("Cut crate edges", new Color(.62f, .45f, .26f), .04f, .36f);
            _steel = Material("Galvanized metal", new Color(.35f, .41f, .45f), .69f, .51f);
            _lens = Material("Lamp lens", new Color(.91f, .79f, .55f), .05f, .76f, .65f);
            _cone = Frustum("Tapered safety cone", .17f, .042f, .48f);
            _stripe = Frustum("Reflective cone band", .115f, .088f, .105f);
            _ring = Ring();
            _arrow = Arrow();

            var previous = course.Start;
            for (var i = 0; i < course.Gates.Length; i++)
            {
                var gate = course.Gates[i];
                foreach (var turn in gate.Turns)
                {
                    DrawRoute(previous, turn);
                    previous = turn;
                }
                DrawRoute(previous, gate.Position);
                previous = gate.Position;
                _gates[i] = CreateGate(gate, i == course.Gates.Length - 1 ? "FINISH" : $"CHECKPOINT {i + 1}");
            }
            foreach (var target in course.Targets) PlaceTarget(mission, target);
            if (course.Id == "freight-yard")
            {
                YardSign("LOADING BAYS", new Vector3(-229, .02f, 208), -27f);
                YardSign("WIDE LANE", new Vector3(-204, .02f, 208), 23f);
            }
        }

        public void ShowGate(int index)
        {
            for (var i = 0; i < _gates.Length; i++) _gates[i].SetActive(i == index);
        }

        private GameObject CreateGate(ImpactGate gate, string title)
        {
            var gateObject = new GameObject(title + " / " + gate.Name);
            var root = gateObject.transform;
            root.SetParent(_root.transform);
            root.SetPositionAndRotation(new Vector3(gate.Position.x, .031f, gate.Position.z), gate.Rotation);
            var span = gate.Width * .5f;
            var finish = title == "FINISH";
            var color = finish ? _gold : _blue;
            if (finish)
            {
                var tiles = Mathf.RoundToInt(gate.Width / 1.25f);
                var size = gate.Width / tiles;
                for (var column = 0; column < tiles; column++)
                    for (var row = 0; row < 2; row++)
                        Box(root, "Checkered finish paint", new Vector3(-span + size * (column + .5f), 0f, (row - .5f) * 1.1f),
                            new Vector3(size, .009f, 1.1f), (row + column) % 2 == 0 ? _white : _rubber);
            }
            else
            {
                Box(root, "Checkpoint stripe", Vector3.zero, new Vector3(gate.Width, .01f, .48f), _asphaltPaint);
            }
            foreach (var side in new[] { -1f, 1f })
            {
                Box(root, "Track beacon pedestal", new Vector3(side * span, .16f, 0), new Vector3(.88f, .32f, .72f), _rubber);
                Box(root, "Track beacon", new Vector3(side * span, 2.35f, 0), new Vector3(.33f, 4.7f, .30f), _steel);
                Box(root, "Track beacon light", new Vector3(side * span, 2.9f, -.19f), new Vector3(.19f, 3.0f, .10f), color);
            }
            var gantryWidth = Mathf.Min(gate.Width, 12f);
            Box(root, "Overhead gantry", new Vector3(0, 4.8f, 0), new Vector3(gantryWidth, .65f, .40f), _rubber);
            foreach (var side in new[] { -1f, 1f })
                if (Mathf.Abs(gantryWidth - gate.Width) > 1f)
                    Box(root, "Gantry post", new Vector3(side * gantryWidth * .5f, 2.33f, 0), new Vector3(.26f, 4.66f, .27f), _steel);
            Box(root, "Checkpoint sign board", new Vector3(0, 4.86f, -.26f), new Vector3(finish ? 6f : 5.25f, .95f, .14f), _rubber);
            Box(root, "Gantry accent", new Vector3(0, 5.29f, -.37f), new Vector3(finish ? 6f : 5.25f, .08f, .09f), color);
            Text(root, title, new Vector3(0, 4.82f, -.36f), finish ? 5.65f : 4.95f, .65f);
            return gateObject;
        }

        private void DrawRoute(Vector3 from, Vector3 to)
        {
            from.y = to.y = .042f;
            var forward = to - from;
            var length = forward.magnitude;
            if (length < 9f) return;
            var heading = Quaternion.LookRotation(forward);
            for (var offset = 6f; offset < length - 2.5f; offset += 24f)
            {
                var arrow = MeshPart(_root.transform, "Course arrow", _arrow, from + forward.normalized * offset, _asphaltPaint);
                arrow.localRotation = heading;
            }
        }

        private void YardSign(string name, Vector3 position, float yaw)
        {
            var root = new GameObject(name).transform;
            root.SetParent(_root.transform);
            root.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            Box(root, "Lane marker post", new Vector3(0, 1.05f, 0), new Vector3(.14f, 2.1f, .16f), _steel);
            Box(root, "Freight route plate", new Vector3(0, 2.12f, 0), new Vector3(3.25f, .64f, .16f), _rubber);
            Box(root, "Freight route plate edge", new Vector3(0, 2.43f, -.11f), new Vector3(3.25f, .07f, .1f), _gold);
            Text(root, name, new Vector3(0, 2.10f, -.105f), 2.94f, .44f);
            for (var i = 1; i < 5; i++)
                MeshPart(root, "Freight lane arrow", _arrow, new Vector3(0, .024f, 4f * i), _asphaltPaint);
        }

        private void PlaceTarget(ImpactMission mission, ImpactTargetSpec spec)
        {
            var name = ImpactTargetSpec.HitLabels[(int)spec.Kind];
            var root = new GameObject("Scoring target / " + name).transform;
            root.SetParent(_root.transform);
            root.SetPositionAndRotation(spec.Position, Quaternion.Euler(0, spec.Yaw, 0));
            var marker = MeshPart(_root.transform, name + " / Gold ground marking", _ring, spec.Position + Vector3.up * .003f, _gold);
            var body = root.gameObject.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.linearDamping = .42f;
            body.angularDamping = .65f;

            Vector3 center, size;
            float speed;
            switch (spec.Kind)
            {
                case ImpactTargetKind.Cone:
                    Box(root, "Cone base", new Vector3(0, .045f, 0), new Vector3(.44f, .085f, .44f), _rubber);
                    MeshPart(root, "Orange cone", _cone, Vector3.up * .066f, _orange);
                    MeshPart(root, "White cone collar", _stripe, Vector3.up * .285f, _white);
                    center = new Vector3(0, .33f, 0); size = new Vector3(.41f, .66f, .41f); body.mass = 3.2f; speed = 4f;
                    break;
                case ImpactTargetKind.Crate:
                    Box(root, "Crate box", Vector3.up * .39f, new Vector3(.79f, .78f, .84f), _wood);
                    foreach (var sign in new[] { -1f, 1f })
                    {
                        for (var board = 0; board < 4; board++)
                            Box(root, "Crate plank", new Vector3(0, .12f + board * .181f, sign * .425f), new Vector3(.83f, .168f, .036f), _woodTrim);
                        Box(root, "Reinforced corner", new Vector3(sign * .363f, .39f, 0), new Vector3(.10f, .78f, .89f), _woodTrim);
                        var brace = Box(root, "Diagonal timber brace", new Vector3(0, .39f, sign * .46f), new Vector3(.105f, .98f, .045f), _wood);
                        brace.localRotation = Quaternion.Euler(0, 0, 43f);
                    }
                    center = new Vector3(0, .39f, 0); size = new Vector3(.84f, .78f, .88f); body.mass = 21f; speed = 7f;
                    break;
                case ImpactTargetKind.Barrier:
                    foreach (var side in new[] { -1f, 1f })
                    {
                        Box(root, "Barricade foot", new Vector3(side * .58f, .06f, 0), new Vector3(.23f, .12f, .81f), _rubber);
                        Box(root, "Barricade stand", new Vector3(side * .58f, .43f, 0), new Vector3(.11f, .85f, .15f), _steel);
                    }
                    Box(root, "Orange rail", Vector3.up * .68f, new Vector3(1.59f, .29f, .13f), _orange);
                    for (var i = -2; i <= 2; i++)
                    {
                        var stripe = Box(root, "Barricade reflector", new Vector3(i * .282f, .68f, -.078f), new Vector3(.126f, .27f, .025f), _white);
                        stripe.localRotation = Quaternion.Euler(0, 0, -24f);
                    }
                    center = Vector3.up * .39f; size = new Vector3(1.52f, .78f, .44f); body.mass = 44f; speed = 8f;
                    break;
                case ImpactTargetKind.Streetlight:
                    Box(root, "Breakaway base", Vector3.up * .11f, new Vector3(.29f, .22f, .29f), _rubber);
                    Cylinder(root, "Galvanized streetlight column", Vector3.up * 2.10f, new Vector3(.15f, 2.08f, .15f), _steel);
                    Box(root, "Luminaire bracket", new Vector3(.47f, 4.16f, 0), new Vector3(1.02f, .11f, .13f), _steel);
                    Box(root, "Cast lamp housing", new Vector3(1f, 4.09f, 0), new Vector3(.48f, .17f, .29f), _rubber);
                    Box(root, "Streetlight glass", new Vector3(1f, 3.995f, 0), new Vector3(.38f, .04f, .22f), _lens);
                    center = new Vector3(0, 2.04f, 0); size = new Vector3(.21f, 4.08f, .21f); body.mass = 61f; speed = 12f;
                    body.isKinematic = true;
                    marker.localScale = new Vector3(.72f, 1f, .72f);
                    break;
                default:
                    MakeWreck(root);
                    center = new Vector3(0, .54f, 0); size = new Vector3(1.64f, 1.06f, 4.1f); body.mass = 645f; speed = 16f;
                    marker.localScale = new Vector3(1.25f, 1f, 2.48f);
                    break;
            }
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            root.gameObject.AddComponent<ImpactCourseTarget>().Configure(mission, spec.Kind, marker.gameObject, speed);
        }

        private void MakeWreck(Transform root)
        {
            if (_wreckModel != null)
            {
                var car = Object.Instantiate(_wreckModel, root, false);
                car.name = "Staged scrap car";
                foreach (var collider in car.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                var block = new MaterialPropertyBlock();
                foreach (var renderer in car.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.name.StartsWith("Damage_Windshield_Crack", StringComparison.Ordinal)) renderer.enabled = true;
                    var materials = renderer.sharedMaterials;
                    for (var slot = 0; slot < materials.Length; slot++)
                    {
                        if (materials[slot] == null || !materials[slot].name.Contains("Paint", StringComparison.OrdinalIgnoreCase)) continue;
                        block.SetColor("_Color", new Color(.26f, .22f, .17f));
                        block.SetFloat("_Glossiness", .17f);
                        renderer.SetPropertyBlock(block, slot);
                    }
                }
            }
            else
            {
                Box(root, "Scrap car body", Vector3.up * .56f, new Vector3(1.72f, .99f, 4.2f), _steel);
            }
            foreach (var end in new[] { -1f, 1f })
                Box(root, "Staged target tape", new Vector3(0, .66f, end * 2.11f), new Vector3(1.52f, .11f, .035f), _gold);
        }

        private void Text(Transform parent, string text, Vector3 offset, float width, float height)
        {
            var lettering = new GameObject(text, typeof(TextMesh));
            lettering.transform.SetParent(parent, false);
            lettering.transform.localPosition = offset;
            var mesh = lettering.GetComponent<TextMesh>();
            if (_font != null)
            {
                mesh.font = _font;
                lettering.GetComponent<MeshRenderer>().sharedMaterial = _signMaterial != null ? _signMaterial : _font.material;
            }
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = .05f;
            mesh.fontSize = 46;
            mesh.color = Color.white;
            var bounds = lettering.GetComponent<MeshRenderer>().bounds.size;
            lettering.transform.localScale *= Mathf.Min(width / Mathf.Max(.001f, Mathf.Max(bounds.x, bounds.z)), height / Mathf.Max(.001f, bounds.y));
        }

        private Material Material(string name, Color color, float metal, float smoothness, float emission = 0f)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", metal);
            material.SetFloat("_Glossiness", smoothness);
            if (emission > 0)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            _materials.Add(material);
            return material;
        }

        private static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Material material) =>
            Primitive(parent, PrimitiveType.Cube, name, position, size, material);

        private static Transform Cylinder(Transform parent, string name, Vector3 position, Vector3 size, Material material) =>
            Primitive(parent, PrimitiveType.Cylinder, name, position, size, material);

        private static Transform Primitive(Transform parent, PrimitiveType kind, string name, Vector3 position, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(kind);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part.transform;
        }

        private static Transform MeshPart(Transform parent, string name, Mesh mesh, Vector3 position, Material material)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return part.transform;
        }

        private static Mesh Frustum(string name, float bottom, float top, float height)
        {
            const int sides = 18;
            var vertices = new Vector3[sides * 2];
            var triangles = new int[sides * 6];
            for (var i = 0; i < sides; i++)
            {
                var angle = i * 2f * Mathf.PI / sides;
                var direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                vertices[i * 2] = direction * bottom;
                vertices[i * 2 + 1] = direction * top + Vector3.up * height;
                var next = (i + 1) % sides * 2;
                var start = i * 6;
                triangles[start] = i * 2; triangles[start + 1] = next; triangles[start + 2] = i * 2 + 1;
                triangles[start + 3] = next; triangles[start + 4] = next + 1; triangles[start + 5] = i * 2 + 1;
            }
            return MakeMesh(name, vertices, triangles);
        }

        private static Mesh Ring()
        {
            const int slices = 24;
            var vertices = new Vector3[slices * 2];
            var triangles = new int[slices * 6];
            for (var i = 0; i < slices; i++)
            {
                var angle = i * 2f * Mathf.PI / slices;
                var direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                var next = (i + 1) % slices * 2;
                vertices[i * 2] = direction * .96f;
                vertices[i * 2 + 1] = direction * 1.07f;
                var start = i * 6;
                triangles[start] = i * 2; triangles[start + 1] = i * 2 + 1; triangles[start + 2] = next + 1;
                triangles[start + 3] = i * 2; triangles[start + 4] = next + 1; triangles[start + 5] = next;
            }
            return MakeMesh("Gold target outline", vertices, triangles);
        }

        private static Mesh Arrow() => MakeMesh("Directional route chevron", new[]
        {
            new Vector3(-.88f, 0, -.54f), new Vector3(0, 0, .52f), new Vector3(.88f, 0, -.54f),
            new Vector3(.88f, 0, .04f), new Vector3(0, 0, 1.12f), new Vector3(-.88f, 0, .04f),
        }, new[] { 0, 5, 4, 0, 4, 1, 1, 4, 3, 1, 3, 2 });

        private static Mesh MakeMesh(string name, Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Dispose()
        {
            Object.Destroy(_root);
            foreach (var material in _materials) Object.Destroy(material);
            Object.Destroy(_cone);
            Object.Destroy(_stripe);
            Object.Destroy(_ring);
            Object.Destroy(_arrow);
        }
    }
}
