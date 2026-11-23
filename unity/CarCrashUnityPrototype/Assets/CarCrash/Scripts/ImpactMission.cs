using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    public sealed class ImpactMission : MonoBehaviour
    {
        private readonly int[] _hits = new int[ImpactTargetSpec.Points.Length];
        private readonly List<Vector3> _guidance = new();
        private ImpactCourseStage _stage;
        private Vector3 _previous;
        private int _gateIndex;
        private float _noticeUntil;
        private string _notice;

        public ImpactCourse Course { get; private set; }
        public Rigidbody VehicleBody { get; private set; }
        public bool Running { get; private set; }
        public bool Completed { get; private set; }
        public bool TimedOut { get; private set; }
        public int Score { get; private set; }
        public int Hits { get; private set; }
        public int LastPoints { get; private set; }
        public string LastTarget { get; private set; }
        public float LastHitTime { get; private set; } = -10f;
        public int PassedCheckpoints => Course == null ? 0 : Mathf.Min(_gateIndex, CheckpointCount);
        public int CheckpointCount => Course == null ? 0 : Course.Gates.Length - 1;
        public ImpactGate NextGate => Course != null && _gateIndex < Course.Gates.Length ? Course.Gates[_gateIndex] : null;
        public IReadOnlyList<Vector3> Guidance => _guidance;
        public Vector3 RecoveryPosition => _gateIndex == 0 ? Course.Start
            : Course.Gates[_gateIndex - 1].Position + (RecoveryRotation * Vector3.forward) * 6f;
        public Quaternion RecoveryRotation => _gateIndex == 0 ? Course.Heading : Course.Gates[_gateIndex - 1].Rotation;

        public int HitsOf(ImpactTargetKind kind) => _hits[(int)kind];

        public void Prepare(ImpactCourse course, ArcadeVehicleController vehicle, VehicleGarage garage)
        {
            Course = course;
            VehicleBody = vehicle.GetComponent<Rigidbody>();
            _stage = new ImpactCourseStage(this, garage != null && garage.vehicles.Length > 0 ? garage.vehicles[0].visualPrefab : null);
            foreach (var traffic in FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))
                traffic.gameObject.SetActive(false);
            vehicle.RecoverAt(course.Start, course.Heading);
            SyncPosition();
            ShowNextGate();
        }

        public void StartDriving()
        {
            SyncPosition();
            Running = true;
            Notice("Break marked targets  /  Follow the course", 4f);
        }

        public void SyncPosition() => _previous = VehicleBody.position;

        public void Step(float elapsed)
        {
            if (!Running) return;
            if (Course.TimeLimit > 0 && elapsed >= Course.TimeLimit)
            {
                TimedOut = true;
                Stop();
                return;
            }

            var position = VehicleBody.position;
            if (NextGate.Crossed(_previous, position))
            {
                _gateIndex++;
                if (NextGate == null)
                {
                    Completed = true;
                    Stop();
                }
                else
                {
                    Notice($"Checkpoint {PassedCheckpoints} of {CheckpointCount} cleared", 1.7f);
                    ShowNextGate();
                }
            }
            else if (_guidance.Count > 1 && FlatDistance(position, _guidance[0]) < 11f)
            {
                _guidance.RemoveAt(0);
            }
            _previous = position;
        }

        public bool TryCredit(ImpactTargetKind kind, bool chain)
        {
            if (!Running || Time.timeScale <= 0f) return false;
            var index = (int)kind;
            _hits[index]++;
            Hits++;
            LastPoints = ImpactTargetSpec.Points[index];
            Score += LastPoints;
            LastTarget = chain ? "Chain hit / " + ImpactTargetSpec.HitLabels[index] : ImpactTargetSpec.HitLabels[index];
            LastHitTime = Time.unscaledTime;
            return true;
        }

        public void Stop()
        {
            Running = false;
            _stage?.ShowGate(-1);
            _guidance.Clear();
        }

        public string Hint(Transform player)
        {
            if (Time.unscaledTime < _noticeUntil) return _notice;
            if (NextGate == null || _guidance.Count == 0) return "";
            var delta = _guidance[0] - player.position;
            delta.y = 0f;
            var angle = Vector3.SignedAngle(player.forward, delta, Vector3.up);
            var direction = Mathf.Abs(angle) > 135f ? "Turn around" : angle > 28f ? "Right" : angle < -28f ? "Left" : "Ahead";
            var gate = PassedCheckpoints == CheckpointCount ? "Finish" : $"Checkpoint {PassedCheckpoints + 1}/{CheckpointCount}";
            if (_guidance.Count > 1 && direction == "Ahead" && delta.sqrMagnitude <= 100f * 100f)
            {
                var turn = Vector3.SignedAngle(delta, _guidance[1] - _guidance[0], Vector3.up);
                if (Mathf.Abs(turn) > 37f) return $"{gate}  /  {(turn > 0f ? "Right" : "Left")} in {delta.magnitude:0} m";
            }
            return $"{gate}  /  {direction}  /  {delta.magnitude:0} m";
        }

        private void ShowNextGate()
        {
            _guidance.Clear();
            _guidance.AddRange(NextGate.Turns);
            _guidance.Add(NextGate.Position);
            _stage.ShowGate(_gateIndex);
        }

        private void Notice(string text, float seconds)
        {
            _notice = text;
            _noticeUntil = Time.unscaledTime + seconds;
        }

        private static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        private void OnDestroy() => _stage?.Dispose();
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class ImpactCourseTarget : MonoBehaviour
    {
        private ImpactMission _mission;
        private ImpactTargetKind _kind;
        private Rigidbody _body;
        private GameObject _marker;
        private float _minimumSpeed;
        private bool _credited;

        public void Configure(ImpactMission mission, ImpactTargetKind kind, GameObject marker, float minimumSpeed)
        {
            _mission = mission;
            _kind = kind;
            _marker = marker;
            _minimumSpeed = minimumSpeed;
            _body = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_credited || _mission == null || !_mission.Running || collision.rigidbody == null) return;
            var direct = collision.rigidbody == _mission.VehicleBody;
            var source = direct ? null : collision.rigidbody.GetComponent<ImpactCourseTarget>();
            if (!direct && (source == null || !source._credited || source._mission != _mission)) return;
            var speed = collision.relativeVelocity.magnitude * 3.6f;
            if (speed < _minimumSpeed || !_mission.TryCredit(_kind, !direct)) return;

            _credited = true;
            _marker.SetActive(false);
            var contact = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position + Vector3.up * .5f;
            if (_body.isKinematic)
            {
                _body.isKinematic = false;
                _body.useGravity = true;
                _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var direction = Vector3.ProjectOnPlane(transform.position - collision.rigidbody.position, Vector3.up).normalized;
                if (direction.sqrMagnitude < .1f) direction = collision.rigidbody.transform.forward;
                _body.linearVelocity = direction * Mathf.Clamp(speed * .045f, 1.5f, 6f) + Vector3.up * .25f;
                _body.angularVelocity = Vector3.Cross(Vector3.up, direction) * 2.6f;
            }

            if (_kind != ImpactTargetKind.Cone)
                _mission.VehicleBody.GetComponent<VehicleImpactEffects>()?.EmitImpact(contact, Vector3.up, collision.relativeVelocity, speed);
        }
    }
}
