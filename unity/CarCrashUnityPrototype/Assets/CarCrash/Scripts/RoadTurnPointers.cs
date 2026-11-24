using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    public sealed class RoadTurnPointers : MonoBehaviour
    {
        private const float LookAhead = 90f;
        private readonly List<Vector3> _coursePath = new();
        private CarCrashHudInput _session;
        private ImpactCourse _course;
        private int _checkpoint = -1;
        private GameObject _marker;
        private Transform _overhead;
        private Mesh _arrowMesh;
        private Mesh _overheadMesh;
        private Material _paint;
        private Material _edge;

        public void Initialize(CarCrashHudInput session)
        {
            _session = session;
            var shader = Resources.Load<Shader>("RoadTurnPaint");
            if (shader == null) return;
            _paint = new Material(shader) { name = "Amber turn arrow" };
            _marker = new GameObject("Route turn pointer", typeof(MeshFilter), typeof(MeshRenderer));
            _marker.transform.SetParent(transform, false);
            _arrowMesh = MakeArrow();
            _marker.GetComponent<MeshFilter>().sharedMesh = _arrowMesh;
            var renderer = _marker.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _paint;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _edge = new Material(shader) { name = "Turn sign edge", color = new Color(.22f, .10f, .018f) };
            var sign = new GameObject("Overhead 3D turn sign", typeof(MeshFilter), typeof(MeshRenderer));
            _overhead = sign.transform;
            _overhead.SetParent(_marker.transform, false);
            _overheadMesh = MakeOverheadArrow(_arrowMesh);
            sign.GetComponent<MeshFilter>().sharedMesh = _overheadMesh;
            var signRenderer = sign.GetComponent<MeshRenderer>();
            signRenderer.sharedMaterials = new[] { _paint, _edge };
            signRenderer.shadowCastingMode = ShadowCastingMode.Off;
            signRenderer.receiveShadows = false;
            _marker.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_marker == null) return;
            if (_session.Phase is not (CarCrashHudInput.RunPhase.Driving or CarCrashHudInput.RunPhase.Countdown))
            {
                _marker.SetActive(false);
                return;
            }
            var car = _session.vehicle.transform;
            var path = _session.IsMission ? ActiveCoursePath() : _session.Navigation.TurnPath;
            if (!TryUpcomingTurn(path, car.position, car.forward, out var turn, out var approach, out var right))
            {
                _marker.SetActive(false);
                return;
            }

            turn.y = .056f;
            _marker.transform.SetPositionAndRotation(turn, Quaternion.LookRotation(approach));
            _marker.transform.localScale = new Vector3(right ? 1f : -1f, 1f, 1f);
            _overhead.localPosition = new Vector3(-1.2f, 5f + .20f * Mathf.Sin(Time.unscaledTime * 3f), -3.6f);
            _overhead.localScale = Vector3.one * .60f;
            var flash = .88f + .12f * Mathf.Sin(Time.unscaledTime * 5f);
            _paint.SetColor("_Color", new Color(1f, .70f * flash, .11f, 1f));
            _marker.SetActive(true);
        }

        private IReadOnlyList<Vector3> ActiveCoursePath()
        {
            var mission = _session.Mission;
            var course = mission.Course;
            if (course == _course && _checkpoint == mission.PassedCheckpoints) return _coursePath;
            _coursePath.Clear();
            _course = course;
            _checkpoint = mission.PassedCheckpoints;
            if (course == null || mission.NextGate == null) return _coursePath;
            var gate = course.Gates[_checkpoint];
            _coursePath.Add(_checkpoint == 0 ? course.Start : course.Gates[_checkpoint - 1].Position);
            _coursePath.AddRange(gate.Turns);
            _coursePath.Add(gate.Position);
            if (_checkpoint + 1 < course.Gates.Length)
            {
                var following = course.Gates[_checkpoint + 1];
                _coursePath.AddRange(following.Turns);
                _coursePath.Add(following.Position);
            }
            return _coursePath;
        }

        public static bool TryUpcomingTurn(IReadOnlyList<Vector3> path, Vector3 position, Vector3 heading,
            out Vector3 corner, out Vector3 approach, out bool right)
        {
            corner = approach = Vector3.zero;
            right = false;
            heading.y = 0f;
            var nearest = LookAhead * LookAhead;
            for (var i = 1; i < path.Count - 1; i++)
            {
                var incoming = path[i] - path[i - 1];
                var outgoing = path[i + 1] - path[i];
                var offset = position - path[i];
                incoming.y = outgoing.y = offset.y = 0f;
                if (incoming.sqrMagnitude < 1f || outgoing.sqrMagnitude < 1f || offset.sqrMagnitude >= nearest) continue;
                incoming.Normalize();
                outgoing.Normalize();
                var side = Vector3.Cross(incoming, outgoing).y;
                if (Mathf.Abs(side) < .55f || Vector3.Dot(heading, incoming) < -.2f) continue;
                if (Vector3.Dot(offset, incoming) > 7f || Vector3.Dot(offset, outgoing) > 7f ||
                    Mathf.Abs(Vector3.Cross(incoming, offset).y) > 18f) continue;
                nearest = offset.sqrMagnitude;
                corner = path[i];
                approach = incoming;
                right = side > 0f;
            }
            return nearest < LookAhead * LookAhead;
        }

        private static Mesh MakeArrow()
        {
            var mesh = new Mesh { name = "Road turn arrow" };
            // The car follows the stem into the junction, then the arrow head into the turn.
            mesh.vertices = new[]
            {
                new Vector3(-.66f,0,-7f), new Vector3(.66f,0,-7f), new Vector3(-.66f,0,-1.5f), new Vector3(.66f,0,-2.1f),
                new Vector3(.06f,0,.65f), new Vector3(.94f,0,-.65f), new Vector3(3.45f,0,.65f), new Vector3(3.45f,0,-.65f),
                new Vector3(3.45f,0,1.58f), new Vector3(3.45f,0,-1.58f), new Vector3(5.6f,0,0f)
            };
            mesh.triangles = new[] { 0,2,1, 1,2,3, 2,4,3, 3,4,5, 4,6,5, 5,6,7, 8,10,9 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh MakeOverheadArrow(Mesh paint)
        {
            var flat = paint.vertices;
            var count = flat.Length;
            var vertices = new Vector3[count * 2];
            for (var i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(flat[i].x, flat[i].z + 2.1f, -.28f);
                vertices[i + count] = vertices[i] + Vector3.forward * .56f;
            }

            var paintTriangles = paint.triangles;
            var faces = new List<int>(paintTriangles.Length * 2);
            faces.AddRange(paintTriangles);
            for (var i = paintTriangles.Length - 1; i >= 0; i--) faces.Add(paintTriangles[i] + count);

            var outline = new[] { 0, 1, 3, 5, 7, 9, 10, 8, 6, 4, 2 };
            var edges = new List<int>(outline.Length * 6);
            for (var i = 0; i < outline.Length; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % outline.Length];
                edges.Add(a);
                edges.Add(b);
                edges.Add(a + count);
                edges.Add(a + count);
                edges.Add(b);
                edges.Add(b + count);
            }

            var mesh = new Mesh { name = "Extruded amber direction arrow", vertices = vertices, subMeshCount = 2 };
            mesh.SetTriangles(faces, 0);
            mesh.SetTriangles(edges, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_paint != null) Destroy(_paint);
            if (_edge != null) Destroy(_edge);
            if (_arrowMesh != null) Destroy(_arrowMesh);
            if (_overheadMesh != null) Destroy(_overheadMesh);
        }
    }
}
