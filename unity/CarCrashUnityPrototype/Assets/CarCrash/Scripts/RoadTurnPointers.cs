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
        private Mesh _arrowMesh;
        private Material _paint;

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

        private void OnDestroy()
        {
            if (_paint != null) Destroy(_paint);
            if (_arrowMesh != null) Destroy(_arrowMesh);
        }
    }
}
