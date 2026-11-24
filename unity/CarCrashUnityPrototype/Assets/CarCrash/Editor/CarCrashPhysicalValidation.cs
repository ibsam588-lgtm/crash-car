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
        private static readonly string[] Gaits = { "Idle", "Jog", "Run" };
        private static int _contactIndex;
        private static int _routeIndex;
        private static PhysicalQaContactProbe _probe;
        private static Collider _palmTrunk;
        private static BoxCollider _shopPane;
        private static BreakableCityProp _glass;
        private static CityStreetActor _occupant;
        private static Vector3 _glassPosition;
        private static float _evacuationDeadline;
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
                        var trafficCars = Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None);
                        foreach (var traffic in trafficCars)
                        {
                            if (traffic.GetComponent<TrafficCrashVisual>() != null) CheckDriver(traffic.transform);
                            traffic.gameObject.SetActive(false); // Isolate steering from traffic contacts.
                        }
                        _people = Object.FindObjectsByType<CityPedestrian>(FindObjectsSortMode.None);
                        Result.pedestrians = _people.Length;
                        Check(_people.Length == 120, "The expanded city has 120 human pedestrians on sidewalk routes");
                        _session.Garage.Select(0);
                        CheckDriver(_session.vehicle.transform);
                        var verifiedGaits = new HashSet<(AnimationClip WalkSource, string Gait)>();
                        foreach (var person in _people)
                        {
                            var animation = person.GetComponentInChildren<Animation>();
                            var renderers = person.GetComponentsInChildren<SkinnedMeshRenderer>();
                            Check(animation != null && person.walkClip != null && person.walkClip.length > 1.5f && renderers.Length > 0 &&
                                person.GetComponent<CapsuleCollider>() != null && person.GetComponent<Rigidbody>() != null,
                                "Pedestrian retains a skinned model, walk clip, and physical body: " + person.name);
                            if (animation == null) continue;
                            foreach (var gait in Gaits)
                            {
                                var clip = animation[gait]?.clip;
                                var valid = clip != null && HasGaitBinding(animation, clip);
                                // The legacy Animation component creates distinct aliased clips on every actor.
                                if (!valid || verifiedGaits.Add((person.walkClip, gait)))
                                    Check(valid, "Legacy motion binds to the existing human rig: " + person.name + " / " + gait);
                            }
                        }
                        Check(verifiedGaits.Count == 3 * Gaits.Length,
                            "Each of the three human rigs has its own idle, jog and run motion. Verified pairs: " + verifiedGaits.Count);
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
                            Check(person.DistanceWalked > 0.5f && Mathf.Abs(person.transform.position.x-person.avenueCenterX) > 9.1f && Mathf.Abs(person.transform.position.x-person.avenueCenterX) < 12.1f,
                                "Pedestrian walks on the sidewalk rather than the road: " + person.name +
                                " travel=" + person.DistanceWalked.ToString("F2") + " position=" + person.transform.position + " obstacle=" + person.LastObstacle);
                            if (Vector3.Distance(person.transform.position, _session.vehicle.transform.position) < 80f)
                            {
                                var animation = person.GetComponentInChildren<Animation>();
                                Check(animation.IsPlaying("Walk") || animation.IsPlaying("Jog") || animation.IsPlaying("Run") ||
                                    animation.IsPlaying("Idle"), "The nearby human plays a motion-capture cycle: " + person.name);
                            }
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
                        _palmTrunk = _contactTargets.Find(collider => collider.GetComponentInParent<BreakableCityProp>()?.minimumBreakImpulse > 0f);
                        if (_palmTrunk == null) throw new Exception("No impulse-qualified palm trunk for the weak collision check.");
                        StartContactProbe(_palmTrunk,12f,3.5f);
                        _nextAt = EditorApplication.timeSinceStartup + 1.1;
                        _stage = 15;
                        break;
                    case 15:
                        var palm = _palmTrunk.GetComponentInParent<BreakableCityProp>();
                        Check(_probe.Contacted && !palm.Broken && palm.GetComponent<Rigidbody>().isKinematic,
                            "A real 12 km/h bump contacts the palm and leaves its trunk rooted");
                        _palmTrunk.enabled = false;
                        _stage = 8;
                        break;
                    case 8:
                        if (_contactIndex >= _contactTargets.Count) { _stage = 16; break; }
                        StartContactProbe(_contactTargets[_contactIndex]);
                        _nextAt = EditorApplication.timeSinceStartup + 0.9;
                        _stage = 9;
                        break;
                    case 9:
                        var contactTarget = _contactTargets[_contactIndex];
                        var breakaway=contactTarget.GetComponentInParent<BreakableCityProp>();
                        if(breakaway!=null)
                        {
                            Check(_probe.Contacted&&breakaway.Broken&&!breakaway.GetComponent<Rigidbody>().isKinematic,
                                "A real 80 km/h car collision releases the breakaway assembly: "+contactTarget.name);
                            Check(Vector3.Angle(breakaway.transform.up,Vector3.up)>20,
                                "The struck street object physically topples instead of remaining an immovable wall: "+contactTarget.name);
                        }
                        else Check(_probe.Contacted && _session.vehicle.transform.position.x < contactTarget.bounds.center.x,
                            "The real car hits and cannot tunnel through at 80 km/h: " + contactTarget.name);
                        Result.contactChecks++;
                        contactTarget.enabled = false;
                        _contactIndex++;
                        _stage = 8;
                        break;
                    case 16:
                        PrepareStorefrontProbe();
                        _evacuationDeadline=Time.time+5f;
                        _nextAt=EditorApplication.timeSinceStartup+1.0;
                        _stage=17;
                        break;
                    case 17:
                        var travelIntoStore=(_session.vehicle.GetComponent<Rigidbody>().position.x-_glassPosition.x)*_occupant.shop.side;
                        Check(_probe.Contacted && _glass.Broken && !_shopPane.enabled && Array.TrueForAll(_glass.visibleParts, part => !part.enabled),
                            "A real 35 km/h strike shatters the storefront and removes the pane collision and rendering");
                        Check(travelIntoStore>.25f,"The car crosses the former pane plane into the store; travel="+travelIntoStore.ToString("F2"));
                        Check(_occupant.shop.Evacuating && (_occupant.IsFleeing||_occupant.HasEvacuated),
                            "The storefront impact starts the occupant's evacuation");
                        var occupantAnimation = _occupant.GetComponentInChildren<Animation>();
                        Check(occupantAnimation.IsPlaying("Run") || occupantAnimation.IsPlaying("Jog"),
                            "The evacuating attendant uses a running gait");
                        _stage=18;
                        break;
                    case 18:
                        if (!_occupant.HasEvacuated && Time.time<_evacuationDeadline) return;
                        var door=_occupant.shop.transform.TransformPoint(_occupant.shop.egressLocalPosition);
                        Check(_occupant.HasEvacuated && (_occupant.transform.position.x-door.x)*_occupant.shop.side<-.25f,
                            "The occupant runs through the separate exit and reaches the sidewalk");
                        _stage=10;
                        break;
                    case 10:
                        foreach (var collider in _cityColliders) if(collider!=_shopPane)collider.enabled = true;
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
                        _session.vehicle.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                        _session.vehicle.enabled = true;
                        foreach (var wheel in _session.vehicle.GetComponentsInChildren<WheelCollider>()) wheel.enabled = true;
                        _routeIndex = 0;
                        _stage = 12;
                        break;
                    case 12:
                        if (_routeIndex == 4) { Complete(); break; }
                        var west = _routeIndex < 2;
                        var crossStreet = _routeIndex % 2 == 0;
                        var start = crossStreet ? new Vector3(west ? -20f : 20f,.15f,35f+(west ? 2.3f : -2.3f))
                            : new Vector3((west ? -54f : 54f)+2.3f,.15f,-10f);
                        _forward = crossStreet ? west ? Vector3.left : Vector3.right : Vector3.forward;
                        _session.vehicle.RecoverAt(start,Quaternion.LookRotation(_forward));
                        _start = start;
                        _nextAt = EditorApplication.timeSinceStartup + .6;
                        _stage = 13;
                        break;
                    case 13:
                        Hold("Accelerator pedal",true,20);
                        _nextAt = EditorApplication.timeSinceStartup + 1.5;
                        _stage = 14;
                        break;
                    case 14:
                        var distance = Vector3.Dot(_session.vehicle.transform.position-_start,_forward);
                        Check(distance > 8f && _session.vehicle.ForwardSpeedKmh > 20f,
                            "The car really drives through the expanded "+(_routeIndex < 2 ? "west" : "east")+
                            (_routeIndex % 2 == 0 ? " cross street" : " avenue")+"; travel="+distance.ToString("F2"));
                        _session.Hud.ReleaseControls();
                        _routeIndex++;
                        _stage = 12;
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

        private static void CheckDriver(Transform chassis)
        {
            var driver = chassis.Find("Cabin driver");
            var mesh = driver == null ? null : driver.GetComponent<MeshFilter>();
            Check(mesh != null && mesh.sharedMesh != null && mesh.sharedMesh.vertexCount > 100 &&
                driver.GetComponent<MeshRenderer>().enabled,
                "A seated driver is drawn in the cabin: " + chassis.name);
        }

        private static bool HasGaitBinding(Animation animation, AnimationClip clip)
        {
            if (!clip.legacy || clip.length <= 1f) return false;
            var matchingLegs = new HashSet<string>();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if ((!binding.propertyName.Contains("Rotation", StringComparison.Ordinal) &&
                     !binding.propertyName.Contains("Euler", StringComparison.OrdinalIgnoreCase)) ||
                    !binding.path.Contains("/thigh_", StringComparison.Ordinal)) continue;
                if (animation.transform.Find(binding.path) != null && matchingLegs.Add(binding.path) && matchingLegs.Count == 2)
                    return true;
            }
            return false;
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
            foreach (var avenue in CityRoadNetwork.AvenueX)
            {
                var buildings = 0;
                foreach (var collider in colliders)
                    if ((collider.name == "Masonry building"||collider.name == "Building ground floor core") && Mathf.Abs(collider.bounds.center.x-avenue) < 25f) buildings++;
                Check(buildings >= 12,"Detailed solid shopfront blocks line both sides of avenue "+avenue);
            }
            foreach (var z in new[] {-58f,35f,108f,187f,266f})
            {
                var clear = true;
                for (var x=-87f;x<=87f;x+=6f)
                    clear &= !Physics.CheckBox(new Vector3(x,.8f,z),new Vector3(1.8f,.3f,.95f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                Check(clear,"Cross street connects the west and east neighborhoods without scenery in its driving corridor at "+z);
            }
        }

        private static void PrepareStorefrontProbe()
        {
            foreach(var actor in Object.FindObjectsByType<CityStreetActor>(FindObjectsSortMode.None))
            {
                if(actor.shop==null||actor.shop.Evacuating)continue;
                foreach(var pane in actor.shop.GetComponentsInChildren<BreakableCityProp>())
                {
                    // A full-width outer bay gives the sedan room to pass and leaves
                    // the shop's separate doorway available for its occupant.
                    var box=pane.GetComponentInChildren<BoxCollider>();
                    if(!pane.shatterGlass||box==null||box.transform.lossyScale.z<3.5f||pane.transform.position.z>=actor.transform.position.z-2f)continue;
                    _occupant=actor;_glass=pane;_shopPane=box;
                    break;
                }
                if(_shopPane!=null)break;
            }
            if(_shopPane==null)throw new Exception("No storefront with an occupant, a glass bay and a separate exit was found.");
            foreach(var collider in _occupant.shop.GetComponentsInChildren<Collider>())
                if(collider.attachedRigidbody==null)collider.enabled=true;
            _glassPosition=_glass.transform.position;
            StartContactProbe(_shopPane,35f,3.7f,_occupant.shop.side);
        }

        private static void StartContactProbe(Collider target,float speedKmh=80f,float distance=8f,float side=1f)
        {
            target.enabled = true;
            var direction = Vector3.right*side;
            var position = new Vector3(target.bounds.center.x - distance*side, 0.16f, target.bounds.center.z);
            var rotation = Quaternion.LookRotation(direction);
            var body = _session.vehicle.GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.None;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            _session.vehicle.RecoverAt(position, rotation);
            body.position = position; body.rotation = rotation;
            Physics.SyncTransforms();
            _probe.Target = target;
            _probe.Contacted = false;
            body.linearVelocity = direction * (speedKmh / 3.6f);
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
