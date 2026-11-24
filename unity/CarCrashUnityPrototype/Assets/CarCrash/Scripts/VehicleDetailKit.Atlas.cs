using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    internal sealed partial class VehicleDetailKit
    {
        private readonly List<Mesh> _atlasMeshes = new();

        private bool FitModernAtlas()
        {
            var stock = Array.Find(GetComponentsInChildren<MeshRenderer>(),
                renderer => renderer.name == "Body");
            if (stock == null) return false;

            // The imported wheel pivots and detachable under-bumper stay in place.
            // Only the old exterior/interior Body mesh is replaced.
            var stockGlass = Array.Find(stock.sharedMaterials,
                material => material != null && material.name.Contains("Glass"));
            if (stockGlass == null) return false;

            var paint = new Material(Shader.Find("CarCrash/AutomotivePaint"))
                { name = "M_Garage_Paint_Atlas", color = new Color(.20f, .31f, .34f) };
            _materials.Add(paint);
            var cargo = Paint("Atlas satin cargo skin", new Color(.115f, .188f, .21f), .43f, .40f);
            var dark = Paint("Atlas graphite polymer", new Color(.043f, .058f, .064f), .14f, .37f);
            var bright = Paint("Atlas milled satin rail", new Color(.56f, .64f, .65f), .63f, .46f);
            var inset = Paint("Atlas recessed hatch", new Color(.077f, .112f, .126f), .31f, .39f);
            var fabric = Paint("Atlas charcoal upholstery", new Color(.071f, .092f, .096f), .05f, .20f);
            var headLeft = Paint("Atlas Headlamp left", new Color(.77f, .93f, 1f), .13f, .70f,
                new Color(.53f, .79f, .98f) * .62f);
            var headRight = Paint("Atlas Headlamp right", headLeft.color, .13f, .70f, headLeft.GetColor("_EmissionColor"));
            var tail = Paint("Atlas Taillamp", new Color(.43f, .028f, .028f), .18f, .69f,
                new Color(.72f, .038f, .018f));
            var amber = Paint("Atlas side marker lens", new Color(.72f, .32f, .049f), .10f, .61f,
                new Color(.57f, .24f, .018f) * .72f);
            var reverse = Paint("Atlas clear reverse lens", new Color(.46f, .57f, .62f), .15f, .67f, Color.black);
            var glass = new Material(stockGlass) { name = "Atlas panoramic Glass", color = new Color(.49f, .67f, .72f, .25f) };
            glass.SetTexture("_MainTex", Texture2D.whiteTexture);
            glass.SetFloat("_Glossiness", .74f);
            _materials.Add(glass);

            var shell = new AtlasMesh();
            var windows = new AtlasMesh();
            var reversing = new AtlasMesh();
            BuildAtlasCab(shell, windows, paint, dark, bright, inset, fabric, glass, headLeft, headRight, amber);
            BuildAtlasCargo(shell, reversing, cargo, paint, dark, bright, inset, tail, amber, reverse);
            BuildAtlasMesh("Atlas modern body", shell);
            BuildAtlasMesh("Atlas panoramic glazing", windows).shadowCastingMode = ShadowCastingMode.Off;
            _reverse.Add(BuildAtlasMesh("Atlas reverse lamps", reversing));
            Letter("ATLAS", new Vector3(0f, 1.57f, 3.662f), .111f, new Color(.83f, .89f, .89f), .51f, 180f);
            Letter("EL 04 39", new Vector3(0f, .831f, -3.678f), .097f, new Color(.05f, .076f, .087f), .38f);
            foreach (var side in new[] { -1f, 1f })
                Letter("ATLAS", new Vector3(side * 1.218f, 2.59f, -.34f), .207f,
                    new Color(.68f, .81f, .81f), .92f, -90f * side);

            // Strip materials as well so the garage's include-inactive search binds
            // beam and tint upgrades exclusively to the new physical lamp faces.
            stock.sharedMaterials = Array.Empty<Material>();
            stock.gameObject.SetActive(false);
            return true;
        }

        private static void BuildAtlasCab(AtlasMesh shell, AtlasMesh windows, Material paint, Material dark, Material bright,
            Material inset, Material fabric, Material glass, Material leftLamp, Material rightLamp, Material amber)
        {
            shell.Loft(paint,
                AtlasMesh.Section(1.07f, .98f, 1.84f, .13f, 1.29f),
                AtlasMesh.Section(1.14f, .98f, 1.91f, .14f, 1.56f),
                AtlasMesh.Section(1.14f, .98f, 1.94f, .13f, 3.09f),
                AtlasMesh.Section(1.08f, .91f, 1.92f, .12f, 3.45f),
                AtlasMesh.Section(.96f, 1.03f, 1.86f, .10f, 3.61f));
            shell.Loft(paint,
                AtlasMesh.Section(1.16f, 2.54f, 3.23f, .18f, 1.29f),
                AtlasMesh.Section(1.10f, 2.62f, 3.10f, .14f, 2.04f),
                AtlasMesh.Section(1.035f, 2.64f, 2.94f, .094f, 2.65f),
                AtlasMesh.Section(.96f, 2.63f, 2.80f, .078f, 3.12f));
            shell.Box(0f, .73f, -.17f, 1.53f, .28f, 6.11f, .06f, dark);
            shell.Box(0f, .76f, 3.41f, 1.99f, .39f, .50f, .10f, dark);
            shell.Box(0f, .77f, 3.662f, 1.35f, .17f, .033f, .019f, inset);
            for (var slot = -4; slot <= 4; slot++)
                shell.Box(slot * .132f, .78f, 3.678f, .027f, .117f, .022f, .007f, bright);
            shell.Box(0f, 1.549f, 3.565f, 2.04f, .38f, .158f, .082f, dark);
            shell.Box(0f, 1.361f, 3.59f, 1.85f, .033f, .072f, .012f, bright);

            // Slightly curved front glass between the low nose and tapered air deflector.
            var windshieldBottom = new Vector3(0f, 1.91f, 3.594f);
            var windshieldTop = new Vector3(0f, 2.696f, 3.153f);
            var screenLeftBottom = new Vector3(-1.053f, 1.916f, 3.499f);
            var screenRightBottom = new Vector3(1.053f, 1.916f, 3.499f);
            var screenLeftTop = new Vector3(-.961f, 2.656f, 3.086f);
            var screenRightTop = new Vector3(.961f, 2.656f, 3.086f);
            windows.Panel(screenLeftBottom, windshieldBottom, windshieldTop, screenLeftTop, Vector3.forward, glass);
            windows.Panel(windshieldBottom, screenRightBottom, screenRightTop, windshieldTop, Vector3.forward, glass);
            shell.Box(0f, 1.933f, 3.469f, 2.12f, .085f, .215f, .023f, dark);
            shell.Box(0f, 1.949f, 3.297f, 2.04f, .097f, .318f, .027f, inset);
            shell.Box(0f, 2.234f, 1.372f, 2.12f, .80f, .106f, .031f, dark);

            foreach (var side in new[] { -1f, 1f })
            {
                var outward = Vector3.right * side;
                var lamp = side < 0f ? leftLamp : rightLamp;
                Vector3 Wiper(float x, float height) => new(side * x, height,
                    3.602f - (height - 1.91f) * .56f - x * .090f);
                shell.Panel(Wiper(.116f, 1.957f), Wiper(.757f, 2.053f), Wiper(.765f, 2.071f),
                    Wiper(.116f, 1.975f), Vector3.forward, dark);
                var frontPillarLower = new Vector3(side * 1.065f, 1.905f, 3.5f);
                var frontPillarUpper = new Vector3(side * .960f, 2.681f, 3.091f);
                var windowBackBottom = new Vector3(side * 1.097f, 1.922f, 1.55f);
                var windowBackTop = new Vector3(side * 1.05f, 2.651f, 1.53f);
                windows.Panel(windowBackBottom, frontPillarLower, frontPillarUpper, windowBackTop, outward, glass);
                shell.Panel(frontPillarLower + new Vector3(side * .032f, .045f, -.101f),
                    frontPillarLower + new Vector3(side * -.012f, -.017f, .019f),
                    frontPillarUpper + new Vector3(side * -.019f, .047f, .029f),
                    frontPillarUpper + new Vector3(side * .044f, -.041f, -.084f), outward, dark);
                shell.Box(side * 1.105f, 1.955f, 2.486f, .074f, .071f, 1.923f, .019f, dark);
                shell.Box(side * 1.075f, 2.36f, 1.546f, .14f, .85f, .14f, .033f, paint);
                shell.Box(side * 1.158f, 1.687f, 1.773f, .036f, .075f, .254f, .015f, inset);
                shell.Box(side * 1.183f, 1.691f, 1.774f, .026f, .030f, .137f, .007f, bright);
                shell.Box(side * 1.157f, .762f, 1.564f, .126f, .175f, .531f, .045f, dark);
                shell.Box(side * 1.2f, .844f, 1.566f, .097f, .030f, .409f, .011f, bright);
                shell.Box(side * 1.145f, 2.234f, 3.096f, .307f, .075f, .105f, .025f, dark);
                shell.Box(side * 1.271f, 2.25f, 3.086f, .183f, .238f, .190f, .069f, dark);
                shell.Box(side * 1.276f, 2.25f, 2.989f, .121f, .141f, .018f, .009f, bright);
                shell.Box(side * 1.265f, 2.22f, 3.183f, .111f, .027f, .017f, .007f, amber);

                shell.WheelArch(side, 1.137f, .472f, 2.51f, .48f, .587f, dark);
                shell.Box(side * .534f, 1.606f, 2.15f, .357f, .17f, .534f, .055f, fabric);
                shell.Box(side * .534f, 1.86f, 1.908f, .383f, .52f, .139f, .057f, fabric);
                shell.Box(side * .534f, 2.126f, 1.928f, .259f, .157f, .151f, .061f, fabric);

                shell.Box(side * .586f, 1.671f, 3.647f, .47f, .037f, .023f, .008f, lamp);
                shell.Box(side * .848f, 1.641f, 3.637f, .112f, .094f, .045f, .014f, lamp);
                shell.Box(side * .901f, 1.568f, 3.624f, .094f, .049f, .043f, .011f, lamp);
                shell.Box(side * .901f, 1.499f, 3.616f, .094f, .044f, .039f, .011f, lamp);
            }
            shell.Box(-.491f, 1.744f, 2.933f, .11f, .176f, .307f, .026f, dark);
            shell.SteeringWheel(new Vector3(-.491f, 1.859f, 2.952f), dark);
        }

        private static void BuildAtlasCargo(AtlasMesh shell, AtlasMesh reversing, Material cargo, Material paint,
            Material dark, Material bright, Material inset, Material tail, Material amber, Material reverse)
        {
            shell.Loft(cargo,
                AtlasMesh.Section(1.064f, 1.062f, 3.19f, .205f, -3.604f),
                AtlasMesh.Section(1.188f, .984f, 3.295f, .18f, -3.412f),
                AtlasMesh.Section(1.188f, .984f, 3.295f, .18f, 1.206f),
                AtlasMesh.Section(1.105f, 1.07f, 3.215f, .18f, 1.424f));
            shell.Loft(paint,
                AtlasMesh.Section(.922f, 3.22f, 3.285f, .024f, -3.451f),
                AtlasMesh.Section(1.006f, 3.231f, 3.313f, .028f, -3.272f),
                AtlasMesh.Section(1.006f, 3.231f, 3.313f, .028f, 1.092f),
                AtlasMesh.Section(.921f, 3.177f, 3.277f, .033f, 1.324f));
            shell.Box(0f, 2.19f, -3.61f, 1.785f, 1.73f, .054f, .023f, dark);
            shell.Box(0f, 2.2f, -3.638f, 1.591f, 1.57f, .031f, .014f, inset);
            for (var line = 0; line < 13; line++)
                shell.Box(0f, 1.479f + line * .119f, -3.655f, 1.533f, .019f, .014f, .005f, bright);
            shell.Box(0f, 1.41f, -3.668f, .413f, .043f, .031f, .012f, dark);
            shell.Box(0f, .798f, -3.50f, 2.07f, .34f, .314f, .086f, dark);
            shell.Box(0f, .831f, -3.657f, .50f, .149f, .027f, .013f, bright);

            foreach (var side in new[] { -1f, 1f })
            {
                var outward = Vector3.right * side;
                var x = side * 1.196f;
                shell.Panel(new Vector3(x, 1.31f, -3.285f), new Vector3(x, 1.42f, 1.069f),
                    new Vector3(x, 2.24f, 1.069f), new Vector3(x, 1.96f, -3.285f), outward, inset);
                shell.Panel(new Vector3(x * 1.006f, 1.967f, -3.283f), new Vector3(x * 1.006f, 2.244f, 1.069f),
                    new Vector3(x * 1.006f, 2.299f, 1.069f), new Vector3(x * 1.006f, 2.022f, -3.283f), outward, bright);
                shell.Box(side * 1.189f, 2.987f, -1.090f, .032f, .068f, 4.336f, .012f, dark);
                shell.Box(side * 1.193f, 1.237f, -1.08f, .048f, .069f, 4.312f, .015f, bright);
                shell.Box(side * 1.133f, .738f, -.165f, .171f, .422f, 1.739f, .070f, dark);
                shell.Box(side * 1.221f, .75f, -.268f, .032f, .217f, 1.06f, .015f, inset);
                for (var vent = 0; vent < 6; vent++)
                    shell.Box(side * 1.24f, .75f, -.692f + vent * .172f, .019f, .131f, .031f, .006f, bright);
                shell.WheelArch(side, 1.245f, .472f, -1.9f, .48f, .60f, dark);
                foreach (var z in new[] { -3.11f, -1.04f, .88f })
                    shell.Box(side * 1.213f, 1.31f, z, .033f, .045f, .131f, .011f, amber);

                shell.Box(side * .991f, 2.16f, -3.59f, .155f, 1.504f, .132f, .032f, dark);
                shell.Box(side * .99f, 2.292f, -3.665f, .066f, 1.062f, .035f, .014f, tail);
                shell.Box(side * .989f, 1.708f, -3.663f, .098f, .061f, .035f, .013f, tail);
                reversing.Box(side * .99f, 1.604f, -3.665f, .087f, .056f, .036f, .013f, reverse);
                shell.Box(side * .773f, .80f, -3.662f, .327f, .045f, .036f, .013f, amber);
            }
        }

        private MeshRenderer BuildAtlasMesh(string name, AtlasMesh assembly)
        {
            var detail = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            detail.transform.SetPositionAndRotation(_chassis.position, _chassis.rotation);
            detail.transform.SetParent(transform, true);
            var mesh = assembly.Create(name, out var materials);
            _atlasMeshes.Add(mesh);
            detail.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = detail.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }

        private sealed class AtlasMesh
        {
            private readonly List<Vector3> _vertices = new();
            private readonly List<Vector2> _uv = new();
            private readonly Dictionary<Material, List<int>> _surfaces = new();

            internal static Vector3[] Section(float width, float bottom, float top, float corner, float z) => new[]
            {
                new Vector3(-width + corner, bottom, z), new Vector3(width - corner, bottom, z),
                new Vector3(width, bottom + corner, z), new Vector3(width, top - corner, z),
                new Vector3(width - corner, top, z), new Vector3(-width + corner, top, z),
                new Vector3(-width, top - corner, z), new Vector3(-width, bottom + corner, z)
            };

            internal void Box(float x, float y, float z, float width, float height, float depth, float bevel, Material material)
            {
                bevel = Mathf.Min(bevel, Mathf.Min(width, Mathf.Min(height, depth)) * .36f);
                var left = z - depth * .5f;
                var right = z + depth * .5f;
                var bottom = y - height * .5f;
                var top = y + height * .5f;
                var half = width * .5f;
                var sections = new[]
                {
                    Section(half - bevel, bottom + bevel, top - bevel, bevel * .49f, left),
                    Section(half, bottom, top, bevel, left + bevel),
                    Section(half, bottom, top, bevel, right - bevel),
                    Section(half - bevel, bottom + bevel, top - bevel, bevel * .49f, right)
                };
                foreach (var row in sections)
                    for (var i = 0; i < row.Length; i++)
                        row[i].x += x;
                Loft(material, sections);
            }

            internal void Loft(Material material, params Vector3[][] sections)
            {
                for (var row = 0; row < sections.Length - 1; row++)
                {
                    var back = sections[row];
                    var front = sections[row + 1];
                    var center = (back[0] + back[4] + front[0] + front[4]) * .25f;
                    for (var i = 0; i < back.Length; i++)
                    {
                        var next = (i + 1) % back.Length;
                        var outside = (back[i] + back[next] + front[i] + front[next]) * .25f - center;
                        Panel(back[i], back[next], front[next], front[i], outside, material);
                    }
                }
                Cap(sections[0], Vector3.back, material);
                Cap(sections[^1], Vector3.forward, material);
            }

            private void Cap(Vector3[] section, Vector3 outside, Material material)
            {
                var middle = (section[0] + section[4]) * .5f;
                for (var i = 0; i < section.Length; i++)
                    Triangle(middle, section[i], section[(i + 1) % section.Length], outside, material);
            }

            internal void WheelArch(float side, float x, float height, float axleZ, float inner, float outer, Material material)
            {
                Vector3 Arc(float radius, float angle, float offset) =>
                    new(side * (x + offset), height + Mathf.Sin(angle) * radius, axleZ + Mathf.Cos(angle) * radius);
                for (var i = 0; i < 12; i++)
                {
                    var start = Mathf.PI * i / 12f;
                    var end = Mathf.PI * (i + 1) / 12f;
                    Panel(Arc(inner, start, .023f), Arc(inner, end, .023f), Arc(outer, end, .023f),
                        Arc(outer, start, .023f), Vector3.right * side, material);
                    Panel(Arc(outer, start, -.079f), Arc(outer, start, .023f), Arc(outer, end, .023f),
                        Arc(outer, end, -.079f), Vector3.up, material);
                }
            }

            internal void SteeringWheel(Vector3 center, Material material)
            {
                Vector3 Grip(float rotation, float roundness)
                {
                    var radius = .164f + .014f * Mathf.Cos(roundness);
                    return center + new Vector3(Mathf.Cos(rotation) * radius, Mathf.Sin(rotation) * radius * .82f,
                        Mathf.Sin(rotation) * radius * .57f + .014f * Mathf.Sin(roundness));
                }
                for (var i = 0; i < 16; i++)
                {
                    var start = Mathf.PI * 2f * i / 16;
                    var end = Mathf.PI * 2f * (i + 1) / 16;
                    for (var j = 0; j < 6; j++)
                    {
                        var near = Mathf.PI * 2f * j / 6;
                        var far = Mathf.PI * 2f * (j + 1) / 6;
                        var grip = Grip(start, near);
                        Panel(grip, Grip(end, near), Grip(end, far), Grip(start, far),
                            grip - center - new Vector3(Mathf.Cos(start), Mathf.Sin(start) * .82f, Mathf.Sin(start) * .57f) * .164f,
                            material);
                    }
                }
                Box(center.x, center.y, center.z, .257f, .036f, .048f, .012f, material);
                Box(center.x, center.y - .048f, center.z - .02f, .059f, .094f, .055f, .018f, material);
            }

            internal void Panel(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outside, Material material)
            {
                Triangle(a, b, c, outside, material);
                Triangle(a, c, d, outside, material);
            }

            private void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outside, Material material)
            {
                if (!_surfaces.TryGetValue(material, out var indices)) _surfaces.Add(material, indices = new List<int>());
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outside) < 0f) (b, c) = (c, b);
                indices.Add(_vertices.Count);
                indices.Add(_vertices.Count + 1);
                indices.Add(_vertices.Count + 2);
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _uv.Add(Vector2.zero);
                _uv.Add(Vector2.right);
                _uv.Add(Vector2.up);
            }

            internal Mesh Create(string name, out Material[] materials)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(_vertices);
                mesh.SetUVs(0, _uv);
                mesh.subMeshCount = _surfaces.Count;
                materials = new Material[_surfaces.Count];
                var slot = 0;
                foreach (var (material, triangles) in _surfaces)
                {
                    materials[slot] = material;
                    mesh.SetTriangles(triangles, slot++);
                }
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
