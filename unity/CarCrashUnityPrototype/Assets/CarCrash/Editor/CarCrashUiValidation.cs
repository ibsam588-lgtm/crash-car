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
    // Runs the actual scene and input handlers without adding a test-framework dependency.
    [InitializeOnLoad]
    public static class CarCrashUiValidation
    {
        private const string ActiveKey = "CarCrash.UiValidation.Active";
        private const string ReportPath = "Builds/Validation/ui-smoke.json";
        private static readonly List<string> Checks = new();
        private static int _stage;
        private static double _deadline;
        private static double _nextAt;
        private static CarCrashHudInput _session;
        private static Vector3 _pausedPosition;
        private static float _pausedTime;
        private static int _idleHitCount;
        private static string _runtimeError;
        private static int _oldPaint, _oldBest;
        private static bool _hadPaint, _hadBest;
        private static bool _capturedPrefs;

        static CarCrashUiValidation()
        {
            if (SessionState.GetBool(ActiveKey, false)) Attach();
        }

        [MenuItem("Car Crash/Validate Dynamic UI")]
        public static void ValidateInPlayMode()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before starting UI validation.");
            EditorSceneManager.OpenScene("Assets/CarCrash/Scenes/ImpactRunPrototype.unity");
            SessionState.SetBool(ActiveKey, true);
            Attach();
            EditorApplication.isPlaying = true;
        }

        private static void Attach()
        {
            _stage = 0;
            Checks.Clear();
            _runtimeError = null;
            _capturedPrefs = false;
            _deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) _runtimeError = message;
        }

        private static void Tick()
        {
            try
            {
                if (!string.IsNullOrEmpty(_runtimeError)) throw new Exception("Runtime error: " + _runtimeError);
                if (EditorApplication.timeSinceStartup > _deadline) throw new TimeoutException("UI validation timed out at stage " + _stage);
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                if (EditorApplication.timeSinceStartup < _nextAt) return;

                switch (_stage)
                {
                    case 0:
                        _session = Object.FindFirstObjectByType<CarCrashHudInput>();
                        if (_session == null || _session.Hud == null) return;
                        _hadPaint = PlayerPrefs.HasKey("CarCrash.Paint");
                        _hadBest = PlayerPrefs.HasKey("CarCrash.BestImpactScore");
                        _oldPaint = PlayerPrefs.GetInt("CarCrash.Paint");
                        _oldBest = PlayerPrefs.GetInt("CarCrash.BestImpactScore");
                        _capturedPrefs = true;
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Home && Time.timeScale == 0f && _session.CameraRig.Showcasing,
                            "The game opens at a real home screen with the city safely paused");
                        Check(GameObject.Find("Free Drive") != null && GameObject.Find("Impact Run") != null && GameObject.Find("Open garage") != null,
                            "Home offers untimed driving, an explicit timed challenge, and the garage");
                        _session.Hud.ShowHelp(true);
                        Check(_session.Hud.HelpVisible, "The driving guide opens from Home");
                        _session.Hud.ShowHelp(false);
                        _session.OpenGarage();
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Showroom && !_session.Hud.HelpVisible,
                            "Home navigation opens the live vehicle garage");
                        var vertices = 0;
                        foreach (var filter in _session.vehicle.GetComponentsInChildren<MeshFilter>())
                            if (filter.sharedMesh != null) vertices += filter.sharedMesh.vertexCount;
                        Check(vertices > 50000 && _session.vehicle.frontLeftVisual != null && _session.vehicle.rearRightVisual != null,
                            "Detailed vehicle geometry retains independent wheel bindings");
                        var renderers = _session.vehicle.GetComponentsInChildren<MeshRenderer>();
                        var vehicleBounds = renderers[0].bounds;
                        foreach (var renderer in renderers) vehicleBounds.Encapsulate(renderer.bounds);
                        Check(vehicleBounds.size.x > 1.8f && vehicleBounds.size.x < 2.8f &&
                            vehicleBounds.size.y > 0.9f && vehicleBounds.size.y < 1.9f &&
                            vehicleBounds.size.z > 4f && vehicleBounds.size.z < 5.5f,
                            "Imported coupe is life size, upright, and aligned with the driving chassis");
                        foreach (var wheel in new[] { _session.vehicle.frontLeftVisual, _session.vehicle.frontRightVisual,
                            _session.vehicle.rearLeftVisual, _session.vehicle.rearRightVisual })
                        {
                            var parts = wheel.GetComponentsInChildren<MeshRenderer>();
                            var bounds = parts[0].bounds;
                            foreach (var part in parts) bounds.Encapsulate(part.bounds);
                            if (bounds.size.x >= bounds.size.y * 0.7f)
                                throw new Exception("Imported wheel axle is not aligned across the chassis: " + wheel.name);
                        }
                        Check(true, "All four tire axles retain their source orientation and neutral steering");
                        foreach (var traffic in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))
                        {
                            var meshes = traffic.GetComponentsInChildren<MeshFilter>();
                            var fleetVertices = 0;
                            foreach (var mesh in meshes) fleetVertices += mesh.sharedMesh != null ? mesh.sharedMesh.vertexCount : 0;
                            var fleetRenderers = traffic.GetComponentsInChildren<MeshRenderer>();
                            var bounds = fleetRenderers[0].bounds;
                            foreach (var renderer in fleetRenderers) bounds.Encapsulate(renderer.bounds);
                            Check(fleetVertices > 20000 && bounds.size.x > 1.7f && bounds.size.x < 3f &&
                                bounds.size.y > 0.9f && bounds.size.y < 1.9f && bounds.size.z > 4f && bounds.size.z < 5.8f,
                                "Detailed traffic has complete life-size bodywork, wheels, and upright alignment: " + traffic.name);
                            var centre = traffic.transform.InverseTransformPoint(bounds.center);
                            Check(new Vector2(centre.x, centre.z).magnitude < 0.8f,
                                "Traffic bodywork is centred on its actual collision chassis: " + traffic.name);
                            foreach (var wheel in traffic.GetComponent<TrafficWheelAnimator>().wheels)
                            {
                                var wheelRenderers = wheel.GetComponentsInChildren<MeshRenderer>();
                                var tire = wheelRenderers[0].bounds;
                                foreach (var renderer in wheelRenderers) tire.Encapsulate(renderer.bounds);
                                Check(tire.size.x < 0.7f && tire.size.y > 0.5f && tire.size.y < 1.15f && tire.size.z < 1.15f,
                                    "Traffic wheel rotates only its own aligned tire and rim: " + traffic.name + "/" + wheel.name);
                            }
                        }
                        var reflection = Object.FindFirstObjectByType<ReflectionProbe>();
                        Check(reflection != null && reflection.mode == UnityEngine.Rendering.ReflectionProbeMode.Custom &&
                            reflection.customBakedTexture != null,
                            "Street reflections use a persistent baked cubemap compatible with browser rendering");
                        Check(RenderSettings.skybox != null && RenderSettings.skybox.GetTexture("_MainTex") != null &&
                            QualitySettings.activeColorSpace == ColorSpace.Linear,
                            "HDR sunset lighting and linear color rendering are configured");
                        var city = Object.FindFirstObjectByType<CityRoadNetwork>();
                        Check(city != null && city.GetComponentsInChildren<BoxCollider>().Length > 10 &&
                            city.GetComponentsInChildren<MeshRenderer>().Length < 200,
                            "The real city street map has building collision and batched scenery");
                        Check(Resources.Load<Font>("Fonts/BarlowCondensed-Bold") != null && Resources.Load<Font>("Fonts/ArchivoBlack-Regular") != null,
                            "The game UI includes its licensed display fonts");
                        _session.SelectPaint(1);
                        var paintVerified = false;
                        var block = new MaterialPropertyBlock();
                        foreach (var renderer in _session.vehicle.GetComponentsInChildren<Renderer>())
                        {
                            for (var slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                            {
                                var material = renderer.sharedMaterials[slot];
                                if (material == null || !(material.name.Contains("Competition_") || material.name.Contains("Paint_1"))) continue;
                                renderer.GetPropertyBlock(block, slot);
                                paintVerified |= block.GetColor("_Color") == CarCrashHudInput.PaintColors[1];
                            }
                        }
                        Check(paintVerified, "Paint selection changes the actual 3D body material");
                        Check(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length == 1,
                            "Exactly one responsive HUD canvas is present");
                        _session.SendMessage("OnApplicationFocus", true);
                        _session.BeginRun();
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Countdown && Time.timeScale == 0f,
                            "Countdown holds the vehicle and traffic before launch");
                        _stage = 1;
                        break;
                    case 1:
                        if (_session.Phase != CarCrashHudInput.RunPhase.Driving) return;
                        Check(Time.timeScale == 1f && !_session.CameraRig.Showcasing, "Countdown transitions into live driving");
                        Pointer("Accelerator pedal", true, 101);
                        Pointer("Steer left", true, 102);
                        _nextAt = EditorApplication.timeSinceStartup + 1.3;
                        _stage = 2;
                        break;
                    case 2:
                        Check(_session.vehicle.SpeedKmh > 5f && _session.vehicle.CurrentThrottle > 0.9f && _session.vehicle.CurrentSteer < -0.9f,
                            "Independent held touch controls accelerate and steer real vehicle physics");
                        Pointer("Brake pedal", true, 103);
                        _nextAt = EditorApplication.timeSinceStartup + 0.2;
                        _stage = 3;
                        break;
                    case 3:
                        Check(_session.vehicle.CurrentBrake > 0.9f && _session.vehicle.CurrentThrottle == 0f,
                            "Braking overrides the accelerator");
                        _session.Hud.ReleaseControls();
                        Pointer("Accelerator pedal", true, 201);
                        Pointer("Accelerator pedal", true, 202);
                        Pointer("Accelerator pedal", false, 201);
                        Check(_session.Hud.GasHeld, "Releasing one pointer preserves a second held pointer");
                        Pointer("Accelerator pedal", false, 202);
                        Check(!_session.Hud.GasHeld, "Releasing the pedal clears throttle input");
                        var charge = _session.vehicle.BoostCharge;
                        _session.ActivateBoost();
                        Check(_session.vehicle.BoostActive && _session.vehicle.BoostCharge < charge,
                            "Nitro consumes charge and boosts the vehicle");
                        var view = _session.CameraRig.View;
                        _session.CycleCamera();
                        Check(_session.CameraRig.View != view, "Camera control changes the live driving view");
                        var target = Object.FindFirstObjectByType<TrafficImpactTarget>();
                        _session.scoring.RegisterTrafficImpact(target, _session.vehicle.transform.position,
                            Vector3.forward * 25f, 90f, 0);
                        var looseParts = 0;
                        foreach (var body in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
                        {
                            if (!body.name.StartsWith("Damage_", StringComparison.Ordinal) || body.transform.IsChildOf(_session.vehicle.transform)) continue;
                            looseParts++;
                            var shape = body.GetComponent<Collider>();
                            if (shape == null || shape.bounds.size.sqrMagnitude > 64f)
                                throw new Exception("Detached panel has an oversized collision shape: " + body.name);
                        }
                        Check(looseParts > 0, "Detached body panels keep collision shapes proportional to their real geometry");
                        Check(_session.scoring.Score > 0 && _session.scoring.SlowMotionActive && Time.timeScale < 1f,
                            "Impact updates score and activates cinematic slow motion");
                        _session.PauseRun();
                        _pausedPosition = _session.vehicle.transform.position;
                        _pausedTime = _session.ElapsedSeconds;
                        _nextAt = EditorApplication.timeSinceStartup + 1.4;
                        _stage = 4;
                        break;
                    case 4:
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Paused && Time.timeScale == 0f && _session.scoring.SlowMotionActive,
                            "An impact coroutine cannot unpause the pause menu");
                        Check(Vector3.Distance(_pausedPosition, _session.vehicle.transform.position) < 0.001f && _session.ElapsedSeconds == _pausedTime,
                            "Pause freezes physics and the run timer");
                        Check(!_session.Hud.GasHeld && !_session.Hud.LeftHeld && !_session.Hud.BrakeHeld,
                            "Pause releases every touch control");
                        _session.ResumeRun();
                        Check(Time.timeScale > 0f && Time.timeScale < 1f, "Resume preserves the unfinished slow-motion effect");
                        _stage = 5;
                        break;
                    case 5:
                        if (_session.scoring.SlowMotionActive) return;
                        Check(Time.timeScale == 1f, "Slow motion returns to normal simulation speed");
                        Check(!_session.IsTimed, "Garage launches untimed Free Drive by default");
                        _session.Hud.ReleaseControls();
                        _session.vehicle.enabled = false;
                        var parkedBody = _session.vehicle.GetComponent<Rigidbody>();
                        parkedBody.interpolation = RigidbodyInterpolation.None;
                        parkedBody.linearVelocity = Vector3.zero;
                        parkedBody.angularVelocity = Vector3.zero;
                        _pausedPosition = new Vector3(54f, 0.2f, 210f);
                        _session.vehicle.transform.position = _pausedPosition;
                        parkedBody.position = _pausedPosition;
                        Physics.SyncTransforms();
                        _session.GetComponent<ImpactRunLoopDirector>().SendMessage("LateUpdate");
                        Check(Vector3.Distance(_pausedPosition, _session.vehicle.transform.position) < 0.01f,
                            "Crossing the old lane and distance boundaries does not teleport the player");
                        _session.vehicle.ResetForRelaunch(0f);
                        Physics.SyncTransforms();
                        _pausedPosition = parkedBody.position;
                        parkedBody.constraints = RigidbodyConstraints.FreezeAll;
                        _idleHitCount = _session.scoring.Hits;
                        _stage = 50;
                        break;
                    case 50:
                        if (_session.ElapsedSeconds < 65f) return;
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Driving && !_session.IsTimed,
                            "Free Drive remains playable after a real 65 second session");
                        Check(Vector3.Distance(_pausedPosition, _session.vehicle.GetComponent<Rigidbody>().position) < 0.01f,
                            "The parked player stays in position without an automatic respawn (actual " + _session.vehicle.GetComponent<Rigidbody>().position + ")");
                        var trafficStable = _session.scoring.Hits == _idleHitCount;
                        foreach (var traffic in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))
                            trafficStable &= !traffic.IsReleased;
                        Check(trafficStable, "Traffic brakes and recycles past a stationary player without phantom high-speed impacts");
                        _session.scoring.RegisterTrafficImpact(Object.FindFirstObjectByType<TrafficImpactTarget>(),
                            _session.vehicle.transform.position, Vector3.forward * 25, 90f, 0);
                        _nextAt = EditorApplication.timeSinceStartup + 6;
                        _stage = 51;
                        break;
                    case 51:
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Driving,
                            "Stopping for six seconds after a crash does not end the driving session");
                        _session.vehicle.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                        _session.vehicle.transform.position = new Vector3(54f, 0.2f, 210f);
                        _session.vehicle.GetComponent<Rigidbody>().position = new Vector3(54f, 0.2f, 210f);
                        Physics.SyncTransforms();
                        _session.vehicle.enabled = true;
                        var retainedScore = _session.scoring.Score;
                        var retainedTime = _session.ElapsedSeconds;
                        _session.RecoverVehicle();
                        _session.vehicle.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Driving && _session.scoring.Score == retainedScore &&
                            _session.ElapsedSeconds >= retainedTime && _session.vehicle.transform.position.z > 175f,
                            "Manual recovery returns to a nearby street without restarting time, score, or the session");
                        _session.FinishRun();
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Results && Time.timeScale == 0f &&
                            _session.BestScore >= _session.scoring.Score, "Results freeze the run and persist the personal best");
                        _session.RestartRun();
                        _session = null;
                        _stage = 6;
                        _nextAt = EditorApplication.timeSinceStartup + 0.5;
                        break;
                    case 6:
                        _session = Object.FindFirstObjectByType<CarCrashHudInput>();
                        if (_session == null || _session.Hud == null) return;
                        _session.SendMessage("OnApplicationFocus", true);
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Countdown, "Retry resets the scene and starts a fresh countdown");
                        Check(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length == 1,
                            "Scene reload removes the previous HUD and input objects");
                        _stage = 7;
                        break;
                    case 7:
                        if (_session.Phase != CarCrashHudInput.RunPhase.Driving) return;
                        _session.ReturnToShowroom();
                        _session = null;
                        _stage = 8;
                        _nextAt = EditorApplication.timeSinceStartup + 0.5;
                        break;
                    case 8:
                        _session = Object.FindFirstObjectByType<CarCrashHudInput>();
                        if (_session == null || _session.Hud == null) return;
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Showroom && _session.PaintIndex == 1 && Time.timeScale == 0f,
                            "Garage return restores the chosen finish and an idle simulation");
                        _session.ReturnHome();
                        _session = null;
                        _stage = 9;
                        _nextAt = EditorApplication.timeSinceStartup + 0.5;
                        break;
                    case 9:
                        _session = Object.FindFirstObjectByType<CarCrashHudInput>();
                        if (_session == null || _session.Hud == null) return;
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Home,
                            "The player can return to the Home screen from the garage");
                        _session.SendMessage("OnApplicationFocus", true);
                        _session.StartImpactRun();
                        _stage = 10;
                        break;
                    case 10:
                        if (_session.Phase != CarCrashHudInput.RunPhase.Driving) return;
                        Check(_session.IsTimed, "The time limit only applies when the challenge is explicitly selected");
                        typeof(CarCrashHudInput).GetProperty("ElapsedSeconds").SetValue(_session, CarCrashHudInput.RunDuration + 0.1f);
                        _stage = 11;
                        break;
                    case 11:
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Results,
                            "The optional challenge ends at its advertised time limit");
                        Complete(null);
                        break;
                }
            }
            catch (Exception error) { Complete(error.ToString()); }
        }

        private static void Pointer(string name, bool down, int id)
        {
            var control = GameObject.Find(name);
            if (control == null) throw new Exception("Missing touch control: " + name);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = id };
            if (down) ExecuteEvents.Execute(control, pointer, ExecuteEvents.pointerDownHandler);
            else ExecuteEvents.Execute(control, pointer, ExecuteEvents.pointerUpHandler);
        }

        private static void Check(bool condition, string description)
        {
            if (!condition) throw new Exception(description);
            Checks.Add(description);
            Debug.Log("[Racing UI check] " + description);
        }

        [Serializable]
        private sealed class Report { public bool passed; public string[] checks; public string error; }

        private static void Complete(string error)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(ActiveKey, false);
            if (_capturedPrefs)
            {
                if (_hadPaint) PlayerPrefs.SetInt("CarCrash.Paint", _oldPaint); else PlayerPrefs.DeleteKey("CarCrash.Paint");
                if (_hadBest) PlayerPrefs.SetInt("CarCrash.BestImpactScore", _oldBest); else PlayerPrefs.DeleteKey("CarCrash.BestImpactScore");
                PlayerPrefs.Save();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { passed = error == null, checks = Checks.ToArray(), error = error ?? "" }, true));
            if (error == null) Debug.Log("RACING_UI_VALIDATION_PASS: " + Checks.Count + " checks");
            else Debug.LogError("RACING_UI_VALIDATION_FAIL: " + error);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
    }
}
