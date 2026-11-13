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
            _deadline = EditorApplication.timeSinceStartup + 90;
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
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Showroom && Time.timeScale == 0f && _session.CameraRig.Showcasing,
                            "Live garage starts with the simulation safely paused");
                        _session.SelectPaint(1);
                        var paintVerified = false;
                        var block = new MaterialPropertyBlock();
                        foreach (var renderer in _session.vehicle.GetComponentsInChildren<Renderer>())
                        {
                            for (var slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                            {
                                var material = renderer.sharedMaterials[slot];
                                if (material == null || !material.name.Contains("Competition_")) continue;
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
