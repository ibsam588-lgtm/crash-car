using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    internal static class GarageRimMesh
    {
        public static Mesh Face(int style, string name)
        {
            var surface = new Surface(3);
            // A recessed barrel covers the original hub while leaving the tyre untouched.
            surface.Ring(0f, .61f, 0f, 0f, 32, 1);
            surface.Ring(.54f, .64f, .046f, .018f, 32, 0);
            surface.Ring(.14f, .215f, .050f, .025f, 20, 0);
            surface.Ring(0f, .14f, .061f, .051f, 20, 0);

            switch (style)
            {
                case 1:
                    for (var i = 0; i < 5; i++)
                    {
                        var angle = i * 2f * Mathf.PI / 5f;
                        surface.Spoke(angle - .04f, angle - .151f, .12f, .057f);
                        surface.Spoke(angle + .04f, angle + .151f, .12f, .057f);
                    }
                    break;
                case 2:
                    for (var i = 0; i < 13; i++)
                    {
                        var angle = i * 2f * Mathf.PI / 13f;
                        surface.Spoke(angle, angle + .27f, .16f, .102f);
                    }
                    break;
                default:
                    for (var i = 0; i < 6; i++)
                    {
                        var angle = i * 2f * Mathf.PI / 6f;
                        surface.Spoke(angle, angle, .36f, .19f);
                    }
                    surface.Ring(.61f, .675f, .040f, .023f, 32, 0);
                    surface.Bolts(.625f, 16, .019f, .047f);
                    break;
            }
            surface.Bolts(.124f, 5, .028f, .070f);
            return surface.ToMesh(name);
        }

        public static Mesh Sidewall(int style)
        {
            var surface = new Surface(1);
            var width = style == 1 ? .068f : .021f;
            surface.Ring(.754f - width * .5f, .754f + width * .5f, 0f, 0f, 40, 0);
            return surface.ToMesh(style == 1 ? "White sidewall band" : "Coral sidewall pinstripe");
        }

        private sealed class Surface
        {
            private readonly List<Vector3> _vertices = new();
            private readonly List<int>[] _triangles;

            public Surface(int slots)
            {
                _triangles = new List<int>[slots];
                for (var i = 0; i < slots; i++) _triangles[i] = new List<int>();
            }

            public void Ring(float inner, float outer, float innerZ, float outerZ, int segments, int slot)
            {
                for (var i = 0; i < segments; i++)
                {
                    var angle = i * 2f * Mathf.PI / segments;
                    var next = (i + 1) * 2f * Mathf.PI / segments;
                    Quad(Polar(inner, angle, innerZ), Polar(outer, angle, outerZ),
                        Polar(outer, next, outerZ), Polar(inner, next, innerZ), slot);
                }
            }

            public void Spoke(float innerAngle, float outerAngle, float innerWidth, float outerWidth)
            {
                var p1 = Polar(.17f, innerAngle - innerWidth, .070f);
                var p2 = Polar(.585f, outerAngle - outerWidth, .065f);
                var p3 = Polar(.585f, outerAngle + outerWidth, .065f);
                var p4 = Polar(.17f, innerAngle + innerWidth, .070f);
                var recessed = Vector3.back * .026f;
                Quad(p1, p2, p3, p4, 0);
                Quad(p1 + recessed, p2 + recessed, p2, p1, 0);
                Quad(p3 + recessed, p4 + recessed, p4, p3, 0);
            }

            public void Bolts(float ringRadius, int count, float headRadius, float depth)
            {
                for (var bolt = 0; bolt < count; bolt++)
                {
                    var center = Polar(ringRadius, bolt * 2f * Mathf.PI / count, depth);
                    for (var side = 0; side < 6; side++)
                    {
                        var start = _vertices.Count;
                        _vertices.Add(center);
                        _vertices.Add(center + Polar(headRadius, side * Mathf.PI / 3f, -.003f));
                        _vertices.Add(center + Polar(headRadius, (side + 1) * Mathf.PI / 3f, -.003f));
                        _triangles[2].Add(start);
                        _triangles[2].Add(start + 1);
                        _triangles[2].Add(start + 2);
                    }
                }
            }

            private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int slot)
            {
                var start = _vertices.Count;
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _vertices.Add(d);
                var indices = _triangles[slot];
                indices.Add(start);
                indices.Add(start + 1);
                indices.Add(start + 2);
                indices.Add(start);
                indices.Add(start + 2);
                indices.Add(start + 3);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, subMeshCount = _triangles.Length };
                mesh.SetVertices(_vertices);
                for (var i = 0; i < _triangles.Length; i++) mesh.SetTriangles(_triangles[i], i);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }

            private static Vector3 Polar(float radius, float angle, float z) =>
                new(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), z);
        }
    }
}
