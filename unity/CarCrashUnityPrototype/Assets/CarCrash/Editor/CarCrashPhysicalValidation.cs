using System;
using System.Collections.Generic;
using System.IO;
using CarCrash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace CarCrashEditor
{
    // Check actual travel, heading and world contact, rather than only input flags.
    [InitializeOnLoad]
    public static class CarCrashPhysicalValidation
    {
        private const string ActiveKey = "CarCrash.PhysicalQa.Active";
        private static CarCrashHudInput _session;
        private static int _stage;
        private static double _nextAt, _deadline;
        private static Vector3 _start, _forward;
        private static BoxCollider _wall;
        private static Collider[] _cityColliders;
        private static readonly List<Collider> _contactTargets = new();
        private static int _contactIndex;
        private static PhysicalQaContactProbe _probe;
        private static CityPedestrian[] _people;
        private static readonly string[] SolidNames = { "Palm trunk", "Streetlight column", "Sidewalk bench seat", "Litter bin", "Fire hydrant", "Traffic signal pole" };
        private static readonly List<string> Passed = new();
        private static readonly List<string> Failed = new();
        private static readonly Report Result = new();

        [Serializable] private sealed class Report
        {
            public bool passed;
            public float leftDegrees, rightDegrees, leftTravel, rightTravel;
            public float reverseTravel;
            public int pedestrians, contactChecks;
            public int groundedWheels;
            public string[] checks, failures;
        }

        static CarCrashPhysicalValidation() { if (SessionState.GetBool(ActiveKey, false)) Attach(); }

        [MenuItem("Car Crash/Validate Driving and World Physics")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/CarCrash/Scenes/ImpactRunPrototype.unity");
            SessionState.SetBool(ActiveKey, true);
            Attach();
            EditorApplication.isPlaying = true;
        }

        private static void Attach()
        {
            _stage = 0; _nextAt = 0; _deadline = EditorApplication.timeSinceStartup + 120;
            Passed.Clear(); Failed.Clear();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > _deadline) throw new TimeoutException("Physical QA timed out at stage " + _stage);
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < _nextAt) return;
                switch (_stage)
                {
                    case 0:
                        _session = Object.FindFirstObjectByType<CarCrashHudInput>();
                        if (_session == null || _session.Hud == null) return;
                        foreach (var traffic in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))
                            traffic.gameObject.SetActive(false); // Isolate steering from traffic contacts.
                        _people = Object.FindObjectsByType<CityPedestrian>(FindObjectsSortMode.None);
                        Result.pedestrians = _people.Length;
                        Check(_people.Length == 24, "The district has 24 human pedestrians on sidewalk routes");
                        foreach (var person in _people)
                        {
                            var animation = person.GetComponentInChildren<Animation>();
                            var renderers = person.GetComponentsInChildren<SkinnedMeshRenderer>();
                            Check(animation != null && person.walkClip != null && person.walkClip.length > 1.5f && renderers.Length > 0 &&
                                person.GetComponent<CapsuleCollider>() != null && person.GetComponent<Rigidbody>() != null,
                                "Pedestrian retains a skinned model, walk clip, and physical body: " + person.name);
                        }
                        _session.SendMessage("OnApplicationFocus", true);
                        _session.StartFreeDrive();
                        _stage = 1;
                        break;
                    case 1:
                        if (_session.Phase != CarCrashHudInput.RunPhase.Driving) return;
                        _nextAt = EditorApplication.timeSinceStartup + 0.5;
                        _stage = 2;
                        break;
                    case 2:
                        Result.groundedWheels = Grounded();
                        _start = _session.vehicle.transform.position;
                        _forward = _session.vehicle.transform.forward;
                        Hold("Accelerator pedal", true, 1);
                        Hold("Steer left", true, 2);
                        _nextAt = EditorApplication.timeSinceStartup + 1.2;
                        _stage = 3;
                        break;
                    case 3:
                        Result.leftDegrees = Vector3.SignedAngle(_forward, _session.vehicle.transform.forward, Vector3.up);
                        Result.leftTravel = _session.vehicle.transform.position.x - _start.x;
                        Check(Result.leftDegrees < -8f && Result.leftTravel < -0.35f,
                            "Held left steering changes the real heading and path");
                        _session.Hud.ReleaseControls();
                        _session.vehicle.RecoverAt(new Vector3(2.3f, 0.12f, -10f), Quaternion.identity);
                        _nextAt = EditorApplication.timeSinceStartup + 0.5;
                        _stage = 4;
                        break;
                    case 4:
                        _start = _session.vehicle.transform.position;
                        _forward = _session.vehicle.transform.forward;
                        Hold("Accelerator pedal", true, 3);
                        Hold("Steer right", true, 4);
                        _nextAt = EditorApplication.timeSinceStartup + 1.2;
                        _stage = 5;
                        break;
                    case 5:
                        Result.rightDegrees = Vector3.SignedAngle(_forward, _session.vehicle.transform.forward, Vector3.up);
                        Result.rightTravel = _session.vehicle.transform.position.x - _start.x;
                        Check(Result.rightDegrees > 8f && Result.rightTravel > 0.35f,
                            "Held right steering changes the real heading and path");
                        _session.Hud.ReleaseControls();
                        CheckWorldShapes();
                        foreach (var person in _people)
                        {
                            Check(person.DistanceWalked > 0.5f && Mathf.Abs(person.transform.position.x) > 9.1f && Mathf.Abs(person.transform.position.x) < 12.1f,
                                "Pedestrian walks on the sidewalk rather than the road: " + person.name +
                                " travel=" + person.DistanceWalked.ToString("F2") + " position=" + person.transform.position + " obstacle=" + person.LastObstacle);
                            if (Vector3.Distance(person.transform.position, _session.vehicle.transform.position) < 80f)
                                Check(person.GetComponentInChildren<Animation>().IsPlaying("Walk"), "The nearby human plays a real walk cycle: " + person.name);
                        }
                        _session.vehicle.RecoverAt(new Vector3(2.3f, 0.12f, -10f), Quaternion.identity);
                        _nextAt = EditorApplication.timeSinceStartup + 0.5;
                        _stage = 6;
                        break;
                    case 6:
                        _start = _session.vehicle.transform.position;
                        Hold("Brake pedal", true, 5);
                        Hold("Steer left", true, 6);
                        _nextAt = EditorApplication.timeSinceStartup + 2.0;
                        _stage = 7;
                        break;
                    case 7:
                        Result.reverseTravel = _session.vehicle.transform.position.z - _start.z;
                        Check(_session.vehicle.Reversing && _session.vehicle.ForwardSpeedKmh < -2f && Result.reverseTravel < -0.6f,
                            "Holding the brake at rest reverses the car out of a blocked position; speed=" + _session.vehicle.ForwardSpeedKmh +
                            " throttle=" + _session.vehicle.CurrentThrottle + " brake=" + _session.vehicle.CurrentBrake);
                        Check(Vector3.SignedAngle(Vector3.forward, _session.vehicle.transform.forward, Vector3.up) > 8f,
                            "Steering also changes the heading while reversing; heading=" + Vector3.SignedAngle(Vector3.forward, _session.vehicle.transform.forward, Vector3.up));
                        _session.Hud.ReleaseControls();
                        foreach (var person in _people) person.gameObject.SetActive(false);
                        _session.vehicle.enabled = false;
                        foreach (var wheel in _session.vehicle.GetComponentsInChildren<WheelCollider>()) wheel.enabled = false;
                        _cityColliders = Object.FindFirstObjectByType<CityRoadNetwork>().GetComponentsInChildren<Collider>();
                        _contactTargets.Clear();
                        foreach (var name in SolidNames)
                            foreach (var collider in _cityColliders)
                                if (collider.name.StartsWith(name, StringComparison.Ordinal)) { _contactTargets.Add(collider); break; }
                        foreach (var collider in _cityColliders) collider.enabled = false;
                        _probe = _session.vehicle.gameObject.AddComponent<PhysicalQaContactProbe>();
                        _contactIndex = 0;
                        _stage = 8;
                        break;
                    case 8:
                        if (_contactIndex >= _contactTargets.Count) { _stage = 10; break; }
                        StartContactProbe(_contactTargets[_contactIndex]);
                        _nextAt = EditorApplication.timeSinceStartup + 0.9;
                        _stage = 9;
                        break;
                    case 9:
                        var contactTarget = _contactTargets[_contactIndex];
                        Check(_probe.Contacted && _session.vehicle.transform.position.x < contactTarget.bounds.center.x,
                            "The real car hits and cannot tunnel through at 80 km/h: " + contactTarget.name);
                        Result.contactChecks++;
                        contactTarget.enabled = false;
                        _contactIndex++;
                        _stage = 8;
                        break;
                    case 10:
                        foreach (var collider in _cityColliders) collider.enabled = true;
                        var city = Object.FindFirstObjectByType<CityRoadNetwork>();
                        foreach (var box in city.GetComponentsInChildren<BoxCollider>())
                            if (box.bounds.size.y > 5 && box.bounds.center.x > 0 && Mathf.Abs(box.bounds.center.z) < 40) { _wall = box; break; }
                        if (_wall == null) throw new Exception("No building wall available for camera clearance QA.");
                        var position = new Vector3(_wall.bounds.min.x - 2.5f, 0.2f, _wall.bounds.center.z);
                        var body = _session.vehicle.GetComponent<Rigidbody>();
                        body.interpolation = RigidbodyInterpolation.None;
                        _session.vehicle.RecoverAt(position, Quaternion.LookRotation(Vector3.left));
                        Physics.SyncTransforms();
                        _session.vehicle.enabled = false;
                        body.constraints = RigidbodyConstraints.FreezeAll;
                        _nextAt = EditorApplication.timeSinceStartup + 1.2;
                        _stage = 11;
                        break;
                    case 11:
                        var cameraPosition = _session.CameraRig.transform.position;
                        Check(Vector3.Distance(_wall.ClosestPoint(cameraPosition), cameraPosition) > 0.18f,
                            "The chase camera stays outside nearby building walls");
                        Complete();
                        break;
                }
            }
            catch (Exception error) { Failed.Add(error.ToString()); Complete(); }
        }

        private static int Grounded()
        {
            var count = 0;
            foreach (var wheel in _session.vehicle.GetComponentsInChildren<WheelCollider>()) if (wheel.isGrounded) count++;
            return count;
        }

        private static void CheckWorldShapes()
        {
            var colliders = Object.FindFirstObjectByType<CityRoadNetwork>().GetComponentsInChildren<Collider>();
            foreach (var name in SolidNames)
            {
                var found = false;
                foreach (var collider in colliders) if (collider.name.StartsWith(name, StringComparison.Ordinal)) found = true;
                Check(found, "Street object is solid: " + name);
            }
        }

        private static void StartContactProbe(Collider target)
        {
            target.enabled = true;
            var position = new Vector3(target.bounds.center.x - 8f, 0.16f, target.bounds.center.z);
            var rotation = Quaternion.LookRotation(Vector3.right);
            var body = _session.vehicle.GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.None;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            _session.vehicle.RecoverAt(position, rotation);
            body.position = position; body.rotation = rotation;
            Physics.SyncTransforms();
            _probe.Target = target;
            _probe.Contacted = false;
            body.linearVelocity = Vector3.right * (80f / 3.6f);
        }

        private static void Hold(string name, bool down, int id)
        {
            var pointer = new PointerEventData(EventSystem.current) { pointerId = id };
            if (down) ExecuteEvents.Execute(GameObject.Find(name), pointer, ExecuteEvents.pointerDownHandler);
            else ExecuteEvents.Execute(GameObject.Find(name), pointer, ExecuteEvents.pointerUpHandler);
        }

        private static void Check(bool passed, string description)
        {
            (passed ? Passed : Failed).Add(description);
            Debug.Log("[Physical QA] " + (passed ? "PASS " : "FAIL ") + description);
        }

        private static void Complete()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(ActiveKey, false);
            Result.passed = Failed.Count == 0;
            Result.checks = Passed.ToArray(); Result.failures = Failed.ToArray();
            Directory.CreateDirectory("Builds/Validation");
            File.WriteAllText("Builds/Validation/driving-qa.json", JsonUtility.ToJson(Result, true));
            Debug.Log("PHYSICAL_QA_" + (Result.passed ? "PASS" : "FAIL") + " " + JsonUtility.ToJson(Result));
            if (Application.isBatchMode) EditorApplication.Exit(Result.passed ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
    }

    public sealed class PhysicalQaContactProbe : MonoBehaviour
    {
        public Collider Target;
        public bool Contacted;
        private void OnCollisionEnter(Collision collision) { if (collision.collider == Target) Contacted = true; }
    }
}
