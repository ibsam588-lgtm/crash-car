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
        private const string SavedDemoSeenKey = "CarCrash.UiValidation.SavedDemoSeen";
        private const string SavedListenerVolumeKey = "CarCrash.UiValidation.SavedListenerVolume";
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
        private static int _oldPaint, _oldRoadbreakerPaint, _oldBest, _oldVehicle, _oldExplored;
        private static bool _hadPaint, _hadRoadbreakerPaint, _hadBest, _hadVehicle, _hadExplored;
        private static bool _capturedPrefs;
        private static int? _oldMusic, _oldEffects, _oldNight;
        private static ElysianSoundtrack _soundtrack;
        private static Rigidbody _curbLamp;
        private static int _scoreBeforeCurbLamp;
        private static float _curbLampApproachAt;
        private static readonly Dictionary<string, int?> CareerPrefs = new();
        private static int _gateToTest;
        private static CityPedestrian _impactPedestrian;
        private static int _scoreBeforePedestrian;
        private static int _careerHitsBeforeDemo;

        static CarCrashUiValidation()
        {
            if (SessionState.GetBool(ActiveKey, false)) Attach();
        }

        [MenuItem("Car Crash/Validate Dynamic UI")]
        public static void ValidateInPlayMode()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before starting UI validation.");
            EditorSceneManager.OpenScene("Assets/CarCrash/Scenes/ImpactRunPrototype.unity");
            SessionState.SetInt(SavedDemoSeenKey, PlayerPrefs.GetInt(CarCrashHudInput.DrivingDemoSeenKey, -1));
            PlayerPrefs.SetInt(CarCrashHudInput.DrivingDemoSeenKey, 1);
            SessionState.SetFloat(SavedListenerVolumeKey, AudioListener.volume);
            AudioListener.volume = 0f;
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
            _deadline = EditorApplication.timeSinceStartup + 240;
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
                        AudioListener.volume = 0f;
                        _hadPaint = PlayerPrefs.HasKey("CarCrash.Paint");
                        _hadRoadbreakerPaint = PlayerPrefs.HasKey(GarageCustomization.PaintKey("roadbreaker"));
                        _oldRoadbreakerPaint = PlayerPrefs.GetInt(GarageCustomization.PaintKey("roadbreaker"));
                        _hadBest = PlayerPrefs.HasKey("CarCrash.BestImpactScore");
                        _oldPaint = PlayerPrefs.GetInt("CarCrash.Paint");
                        _oldBest = PlayerPrefs.GetInt("CarCrash.BestImpactScore");
                        _hadVehicle = PlayerPrefs.HasKey("CarCrash.Vehicle");
                        _oldVehicle = PlayerPrefs.GetInt("CarCrash.Vehicle");
                        _hadExplored=PlayerPrefs.HasKey("CarCrash.ExploredPlaces");_oldExplored=PlayerPrefs.GetInt("CarCrash.ExploredPlaces");
                        _oldMusic = PlayerPrefs.HasKey(ElysianSoundtrack.PreferenceKey) ? PlayerPrefs.GetInt(ElysianSoundtrack.PreferenceKey) : null;
                        _oldEffects = PlayerPrefs.HasKey(ElysianSoundtrack.EffectsPreferenceKey) ? PlayerPrefs.GetInt(ElysianSoundtrack.EffectsPreferenceKey) : null;
                        _oldNight = PlayerPrefs.HasKey(CityLightingRig.NightPreferenceKey) ? PlayerPrefs.GetInt(CityLightingRig.NightPreferenceKey) : null;
                        _capturedPrefs = true;
                        CityLightingRig.SetNightEnabled(false);
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Home && Time.timeScale == 0f && _session.CameraRig.Showcasing,
                            "The game opens at a real home screen with the city safely paused");
                        Check(GameObject.Find("Free Drive") != null && GameObject.Find("Impact Run") != null && GameObject.Find("Open garage") != null,
                            "Home offers Free Drive, Impact Run, and the garage");
                        var homeLogo = GameObject.Find("Coastal Circuit home logo")?.GetComponent<UnityEngine.UI.RawImage>();
                        Check(homeLogo != null && homeLogo.texture != null && homeLogo.texture.name == "CoastalCircuit" &&
                            homeLogo.uvRect.width > .8f && !homeLogo.raycastTarget,
                            "Home loads the supplied wordmark with its original proportions and keeps menu input unobstructed");
                        _soundtrack = _session.Soundtrack;
                        var source = _soundtrack.GetComponent<AudioSource>();
                        Check(source.clip != null && source.clip.name == "ArcadeRush" && source.clip.length > 27f &&
                            source.clip.channels == 2 && source.loop && source.spatialBlend == 0f,
                            "Arcade Rush is bundled as the default looping stereo soundtrack");
                        _soundtrack.SetMusicEnabled(true);
                        _soundtrack.SetEffectsEnabled(true);
                        GameObject.Find("Home settings").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                        Check(_session.Hud.SettingsVisible && GameObject.Find("Replay driving demo").GetComponent<UnityEngine.UI.Button>().interactable,
                            "Home opens driving settings and offers a demo replay");
                        GameObject.Find("Music setting").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                        Check(!_soundtrack.MusicEnabled && !source.isPlaying && _soundtrack.EffectsEnabled &&
                            PlayerPrefs.GetInt(ElysianSoundtrack.PreferenceKey) == 0,
                            "Music can be muted and saved while sound effects stay on");
                        GameObject.Find("Sound effects setting").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                        Check(!_soundtrack.EffectsEnabled && !_soundtrack.MusicEnabled &&
                            PlayerPrefs.GetInt(ElysianSoundtrack.EffectsPreferenceKey) == 0,
                            "Sound effects have their own saved mute setting");
                        _soundtrack.SetEffectsEnabled(true);
                        var wasNight = CityLightingRig.NightEnabled;
                        var darkMode = GameObject.Find("Dark mode setting").GetComponent<UnityEngine.UI.Button>();
                        darkMode.onClick.Invoke();
                        Check(CityLightingRig.NightEnabled != wasNight && PlayerPrefs.GetInt(CityLightingRig.NightPreferenceKey) == (wasNight ? 0 : 1),
                            "Dark mode switches city lighting and saves the setting");
                        darkMode.onClick.Invoke();
                        _session.Hud.SendMessage("RebuildLayout");
                        Check(_session.Hud.SettingsVisible &&
                            GameObject.Find("Motion steering setting").GetComponent<UnityEngine.UI.Button>().interactable == _session.MotionSteeringAvailable,
                            "Driving settings survive a layout rebuild and reflect sensor support");
                        GameObject.Find("Close settings").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                        CheckLightPlacement();
                        var clearShowcase = true;
                        foreach (var traffic in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))
                            if (Vector3.Distance(traffic.transform.position, _session.vehicle.transform.position) < 12f)
                                foreach (var renderer in traffic.GetComponentsInChildren<MeshRenderer>()) clearShowcase &= !renderer.enabled;
                        Check(clearShowcase, "Nearby traffic cannot obstruct the Home or garage vehicle preview");
                        _session.Hud.ShowHelp(true);
                        Check(_session.Hud.HelpVisible, "The driving guide opens from Home");
                        _session.Hud.SendMessage("RebuildLayout");
                        Check(_session.Hud.HelpVisible, "The driving guide survives a screen layout rebuild");
                        _session.Hud.ShowHelp(false);
                        _session.OpenGarage();
                        _session.SelectVehicle(0);
                        var readableButtons = true;
                        var uiCanvas = Object.FindFirstObjectByType<Canvas>();
                        foreach (var button in uiCanvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                        {
                            var label = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
                            if (label != null && !string.IsNullOrEmpty(label.text))
                            {
                                var fits=label.fontSize*uiCanvas.scaleFactor>=15.8f && label.preferredWidth<=label.rectTransform.rect.width+1;
                                if(!fits)Debug.LogWarning($"[UI text fit] {button.name}: '{label.text}' needs {label.preferredWidth:0.0}, available {label.rectTransform.rect.width:0.0}, font {label.fontSize*uiCanvas.scaleFactor:0.0}px. Screen {Screen.width}x{Screen.height}.");
                                readableButtons &= fits;
                            }
                        }
                        Check(readableButtons, "Every labelled menu button retains at least 16 physical pixels of text and fits its available width");
                        Check(Resources.Load<Font>("Fonts/Inter-Regular") != null && Resources.Load<Font>("Fonts/Inter-Medium") != null,
                            "The interface bundles Inter regular and medium for readable interface labels");
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
                        Check(vehicleBounds.size.x > 1.6f && vehicleBounds.size.x < 2.8f &&
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
                            Check(fleetVertices > 20000 && bounds.size.x > 1.6f && bounds.size.x < 3f &&
                                bounds.size.y > 0.9f && bounds.size.y < 2.5f && bounds.size.z > 3.6f && bounds.size.z < 6.5f,
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
                        var batches=0;var localBatches=0;
                        foreach(var mesh in city.GetComponentsInChildren<MeshFilter>())
                            if(mesh.name.StartsWith("CityBatch_",StringComparison.Ordinal))
                            {
                                batches++;
                                if(mesh.sharedMesh.bounds.size.x<220&&mesh.sharedMesh.bounds.size.z<220)localBatches++;
                            }
                        Check(city != null && city.GetComponentsInChildren<BoxCollider>().Length>100&&batches>100&&batches<800&&localBatches>batches*.8f,
                            "The larger city keeps solid buildings and spatial scenery batches for distance culling");
                        Check(Resources.Load<Font>("Fonts/Inter-SemiBold") != null,
                            "The game UI includes its licensed Inter display weight");
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
                        var trafficRestored = true;
                        foreach (var traffic in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))
                            if (Vector3.Distance(traffic.transform.position, _session.vehicle.transform.position) < 12f)
                                foreach (var renderer in traffic.GetComponentsInChildren<MeshRenderer>())
                                    if (!renderer.name.StartsWith("Damage_Windshield_Crack", StringComparison.Ordinal)) trafficRestored &= renderer.enabled;
                        Check(trafficRestored, "Starting a drive restores the nearby traffic bodywork and wheels");
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
                        var panelsBeforeBump = AttachedDamageParts();
                        var hitsBeforeBump = _session.scoring.Hits;
                        _session.scoring.RegisterTrafficImpact(target, _session.vehicle.transform.position,
                            Vector3.forward * 12.5f, 45f, 0);
                        Check(_session.scoring.Hits == hitsBeforeBump + 1 && _session.scoring.Damage >= 3f &&
                            AttachedDamageParts() == panelsBeforeBump,
                            "Moderate bumps register meaningful damage without detaching whole body panels");
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
                        GameObject.Find("Pause settings").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                        Check(_session.Hud.SettingsVisible && !GameObject.Find("Replay driving demo").GetComponent<UnityEngine.UI.Button>().interactable,
                            "Paused drivers can change settings without replacing their run");
                        GameObject.Find("Music setting").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                        Check(_soundtrack.MusicEnabled && PlayerPrefs.GetInt(ElysianSoundtrack.PreferenceKey) == 1,
                            "The pause menu can turn music back on");
                        GameObject.Find("Close settings").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
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
                        var pedestriansStaySafe = true;
                        foreach (var person in Object.FindObjectsByType<CityPedestrian>(FindObjectsSortMode.None))
                        {
                            pedestriansStaySafe &= person.DistanceWalked > 20f && Mathf.Abs(person.transform.position.x-person.avenueCenterX) > 9.1f &&
                                Mathf.Abs(person.transform.position.x-person.avenueCenterX) < 12.1f && person.transform.position.z > person.pointA.z - 0.6f &&
                                person.transform.position.z < person.pointB.z + 0.6f;
                            if(person.DistanceWalked<=20f||Mathf.Abs(person.transform.position.x-person.avenueCenterX)<9.1f||Mathf.Abs(person.transform.position.x-person.avenueCenterX)>12.1f)
                                Debug.Log("[Pedestrian QA] "+person.name+" travel="+person.DistanceWalked+" position="+person.transform.position+" obstacle="+person.LastObstacle);
                        }
                        Check(pedestriansStaySafe, "Pedestrians complete a sustained walk without leaving their sidewalk blocks");
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
                        Check(_session.Soundtrack == _soundtrack && _soundtrack.MusicEnabled &&
                            Object.FindObjectsByType<ElysianSoundtrack>(FindObjectsSortMode.None).Length == 1,
                            "A scene reload preserves the soundtrack and its setting without starting a second source");
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
                        CaptureCareerPrefs();
                        _session.StartImpactRun();
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Briefing && GameObject.Find("Course card") != null,
                            "Impact Run opens a course briefing with a route preview");
                        _session.ChangeCourse(1);
                        Check(!_session.CourseUnlocked && !GameObject.Find("Start mission").GetComponent<UnityEngine.UI.Button>().interactable,
                            "Old Town is locked before the first finish");
                        _session.ChangeCourse(-1);
                        _session.OpenGarage();
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Showroom && _session.GarageForMission,
                            "The mission can change car in the live garage");
                        _session.SelectVehicle(0);
                        _session.ConfirmGarage();
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Briefing && _session.CourseUnlocked,
                            "Selecting a car returns to the same mission");
                        CheckCourseClearance();
                        CaptureFrame("freight-yard-briefing");
                        _session.BeginRun();
                        _stage = 10;
                        break;
                    case 10:
                        if (_session.Phase != CarCrashHudInput.RunPhase.Driving) return;
                        Check(_session.IsMission && !_session.IsTimed, "Freight Yard ends at the finish line");
                        Check(Object.FindObjectsByType<ImpactCourseTarget>(FindObjectsSortMode.None).Length == _session.SelectedCourse.Targets.Length,
                            "All marked props spawn for the selected course");
                        CaptureFrame("freight-yard-start");
                        _nextAt = EditorApplication.timeSinceStartup + .35;
                        _stage = 19;
                        break;
                    case 19:
                        PositionPedestrianImpact();
                        _stage = 18;
                        break;
                    case 18:
                        Check(_impactPedestrian.IsDown && _impactPedestrian.GetComponentsInChildren<CharacterJoint>().Length >= 6,
                            "A hard hit puts a pedestrian into a physical ragdoll pose");
                        Check(_session.Mission.Score == _scoreBeforePedestrian, "Pedestrian reactions do not affect course scoring");
                        _session.vehicle.RecoverAt(_session.Mission.Course.Start, _session.Mission.Course.Heading);
                        _session.Mission.SyncPosition();
                        Pointer("Accelerator pedal", true, 58);
                        _nextAt = EditorApplication.timeSinceStartup + 4.7;
                        _stage = 11;
                        break;
                    case 11:
                        Pointer("Accelerator pedal", false, 58);
                        Check(_session.Mission.HitsOf(ImpactTargetKind.Cone) > 0 && _session.Mission.Score >= 10,
                            "A real collision with the opening cones awards mission points");
                        CaptureFrame("freight-yard-scored-hit");
                        PositionCurbLampImpact();
                        _stage = 21;
                        break;
                    case 21:
                        if (Time.time < _curbLampApproachAt) return;
                        var approachBody = _session.Mission.VehicleBody;
                        _scoreBeforeCurbLamp = _session.Mission.Score;
                        approachBody.linearVelocity = approachBody.rotation * Vector3.forward * 20f;
                        _nextAt = EditorApplication.timeSinceStartup + .65;
                        _stage = 20;
                        break;
                    case 20:
                        if (_curbLamp.isKinematic || _session.Mission.Score < _scoreBeforeCurbLamp + 100)
                            throw new Exception($"Curbside lamp check: parked={_curbLamp.isKinematic}, points={_session.Mission.Score - _scoreBeforeCurbLamp}, " +
                                $"player={_session.Mission.VehicleBody.position}, speed={_session.vehicle.SpeedKmh:0.0} km/h, lamp={_curbLamp.position}");
                        Check(!_curbLamp.isKinematic && _session.Mission.HitsOf(ImpactTargetKind.Streetlight) == 1 &&
                            _session.Mission.Score >= _scoreBeforeCurbLamp + ImpactTargetSpec.Points[(int)ImpactTargetKind.Streetlight],
                            "A curbside streetlight falls and awards its 100 points once");
                        DriveAcross(_session.Mission.Course.Gates[^1]);
                        _stage = 12;
                        break;
                    case 12:
                        Check(!_session.Mission.Completed && _session.Mission.PassedCheckpoints == 0,
                            "The finish line cannot skip checkpoints");
                        _gateToTest = 0;
                        DriveAcross(_session.Mission.NextGate);
                        _stage = 13;
                        break;
                    case 13:
                        _gateToTest++;
                        if (_gateToTest < _session.Mission.Course.Gates.Length)
                        {
                            Check(_session.Mission.PassedCheckpoints == _gateToTest,
                                $"Checkpoint {_gateToTest} registers in order (body {_session.Mission.VehicleBody.position})");
                            if (_gateToTest == 1)
                            {
                                var score = _session.Mission.Score;
                                _session.RecoverVehicle();
                                Check(_session.Mission.PassedCheckpoints == 1 && _session.Mission.Score == score &&
                                    Vector3.Distance(_session.vehicle.transform.position, _session.Mission.RecoveryPosition) < 5f,
                                    "Recovery returns to the last checkpoint and preserves points");
                                _nextAt = EditorApplication.timeSinceStartup + .35;
                                _stage = 17;
                                break;
                            }
                            DriveAcross(_session.Mission.NextGate);
                            break;
                        }
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Results && _session.Mission.Completed && Time.timeScale == 0f,
                            "Crossing the finish after every checkpoint opens scored results and freezes physics");
                        var expected = 0;
                        for (var i = 0; i < ImpactTargetSpec.Points.Length; i++) expected += _session.Mission.HitsOf((ImpactTargetKind)i) * ImpactTargetSpec.Points[i];
                        Check(_session.Mission.Score == expected && _session.BestScore == expected && ImpactCareer.Finished(_session.SelectedCourse),
                            "The best score and result equal the sum of the individual obstacle values");
                        Check(GameObject.Find("Mission results card") != null && _session.NextCourseUnlocked,
                            "Finishing unlocks the next course and shows the score breakdown");
                        CaptureFrame("freight-yard-results");
                        _session.NextCourse();
                        _nextAt = EditorApplication.timeSinceStartup + .5;
                        _stage = 14;
                        break;
                    case 17:
                        DriveAcross(_session.Mission.NextGate);
                        _stage = 13;
                        break;
                    case 14:
                        _session = Object.FindFirstObjectByType<CarCrashHudInput>();
                        if (_session == null || _session.Hud == null) return;
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Briefing && _session.SelectedCourse.Id == "old-town",
                            "Next Course opens Old Town's briefing");
                        PlayerPrefs.SetInt(ImpactCareer.FinishKey(ImpactCourse.Catalog[2]), 1);
                        _session.ChangeCourse(2);
                        _session.SendMessage("OnApplicationFocus", true);
                        _session.BeginRun();
                        _stage = 15;
                        break;
                    case 15:
                        if (_session.Phase != CarCrashHudInput.RunPhase.Driving) return;
                        Check(_session.IsTimed && _session.SelectedCourse.Id == "apex-motor-park", "Apex Motor Park has a 75-second time limit");
                        typeof(CarCrashHudInput).GetProperty("ElapsedSeconds").SetValue(_session, _session.SelectedCourse.TimeLimit + .1f);
                        _stage = 16;
                        break;
                    case 16:
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Results && _session.Mission.TimedOut && !ImpactCareer.Finished(_session.SelectedCourse),
                            "A timed-out course cannot bank an unfinished result");
                        _careerHitsBeforeDemo = PlayerPrefs.GetInt(ImpactCareer.TargetsKey, 0);
                        PlayerPrefs.DeleteKey(CarCrashHudInput.DrivingDemoSeenKey);
                        _session.ReturnHome();
                        _nextAt = EditorApplication.timeSinceStartup + .5;
                        _stage = 31;
                        break;
                    case 31:
                        _session = Object.FindFirstObjectByType<CarCrashHudInput>();
                        if (_session == null || _session.Hud == null) return;
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Home && _session.DrivingDemoOffer &&
                            GameObject.Find("First drive") != null && GameObject.Find("Skip driving demo") != null,
                            "First launch offers a skippable driving demo");
                        CaptureFrame("first-drive-offer");
                        GameObject.Find("Start driving demo").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                        _session.SendMessage("OnApplicationFocus", true);
                        Check(_session.DrivingDemoActive && _session.Phase == CarCrashHudInput.RunPhase.Countdown &&
                            PlayerPrefs.GetInt(CarCrashHudInput.DrivingDemoSeenKey) == 1,
                            "Starting the demo launches a short practice route and remembers the prompt");
                        _stage = 32;
                        break;
                    case 32:
                        if (_session.Phase != CarCrashHudInput.RunPhase.Driving) return;
                        var rightPointer = GameObject.Find("Route turn pointer");
                        Check(rightPointer != null && rightPointer.transform.localScale.x > 0f,
                            "The practice route paints the upcoming right turn onto the road");
                        CaptureFrame("demo-right-turn");
                        CrossDemoGate();
                        _stage = 33;
                        _nextAt = EditorApplication.timeSinceStartup + .22;
                        break;
                    case 33:
                        var leftPointer = GameObject.Find("Route turn pointer");
                        Check(_session.Mission.PassedCheckpoints == 1 && leftPointer != null && leftPointer.transform.localScale.x < 0f,
                            "The pointer switches to the left turn after clearing the right-turn checkpoint " +
                            $"(checkpoints: {_session.Mission.PassedCheckpoints}, player: {_session.vehicle.transform.position}, arrow: {leftPointer?.transform.localScale.x})");
                        CityLightingRig.SetNightEnabled(true);
                        Check(RenderSettings.skybox.shader.name == "CarCrash/ElysianNightSky" &&
                            GameObject.Find("Night driving headlights").GetComponent<Light>().enabled,
                            "Night mode uses its own sky and lights the road ahead of the car");
                        _nextAt = EditorApplication.timeSinceStartup + .30;
                        _stage = 34;
                        break;
                    case 34:
                        CaptureFrame("demo-night-left-turn");
                        Check(_session.Mission.TryCredit(ImpactTargetKind.Cone, false), "The demo can show practice points");
                        CrossDemoGate();
                        _stage = 35;
                        _nextAt = EditorApplication.timeSinceStartup + .22;
                        break;
                    case 35:
                        Check(_session.Phase == CarCrashHudInput.RunPhase.Results && _session.Mission.Completed &&
                            GameObject.Find("Driving demo results") != null && GameObject.Find("Mission results card") == null &&
                            PlayerPrefs.GetInt(ImpactCareer.TargetsKey, 0) == _careerHitsBeforeDemo && !ImpactCareer.Finished(_session.SelectedCourse),
                            "Finishing the practice run shows a recap without recording career progress");
                        CaptureFrame("demo-results");
                        Complete(null);
                        break;
                }
            }
            catch (Exception error) { Complete(error.ToString()); }
        }

        private static void CheckCourseClearance()
        {
            foreach (var course in ImpactCourse.Catalog)
            {
                var from = course.Start;
                foreach (var gate in course.Gates)
                {
                    foreach (var turn in gate.Turns)
                    {
                        CheckLane(from, turn, course.Name);
                        from = turn;
                    }
                    CheckLane(from, gate.Position, course.Name);
                    from = gate.Position;
                }
                Check(true, course.Name + " route and checkpoints have clearance for the fleet");
            }
        }

        private static void CheckLightPlacement()
        {
            var lightCount = 0;
            foreach (var prop in Object.FindObjectsByType<BreakableCityProp>(FindObjectsSortMode.None))
            {
                if (!prop.name.StartsWith("Breakaway streetlight", StringComparison.Ordinal) &&
                    !prop.name.StartsWith("Breakaway traffic signal", StringComparison.Ordinal)) continue;
                var pole = prop.GetComponentInChildren<Collider>();
                if (pole == null || !OutsideRoads(pole.bounds.center, pole.bounds.extents.x, pole.bounds.extents.z))
                    throw new Exception($"{prop.name} blocks the road at {prop.transform.position}");
                lightCount++;
            }
            Check(lightCount > 100, $"All {lightCount} city light and signal poles clear both intersecting roads");

            foreach (var course in ImpactCourse.Catalog)
            foreach (var target in course.Targets)
            {
                if (target.Kind != ImpactTargetKind.Streetlight) continue;
                var position = target.Position;
                if (!OutsideRoads(position, .15f, .15f) ||
                    !Physics.Raycast(position + Vector3.up * .25f, Vector3.down, out var ground, .33f) ||
                    ground.point.y > position.y)
                    throw new Exception($"{course.Name} has a streetlight without a clear, visible roadside base at {position}");
            }
            Check(true, "Scoring streetlights stand off the marked roads with visible ground markers");
        }

        private static bool OutsideRoads(Vector3 point, float extentX, float extentZ)
        {
            var avenue = CityRoadNetwork.Nearest(CityRoadNetwork.AvenueX, point.x);
            var crossing = CityRoadNetwork.Nearest(CityRoadNetwork.CrossStreetZ, point.z);
            return Mathf.Abs(point.x - avenue) > CityRoadNetwork.AvenueHalfWidth + extentX &&
                Mathf.Abs(point.z - crossing) > CityRoadNetwork.CrossStreetHalfWidth + extentZ;
        }

        private static void CheckLane(Vector3 from, Vector3 to, string course)
        {
            var distance = Vector3.Distance(from, to);
            if (distance < .05f) return;
            var samples = Mathf.CeilToInt(distance / 2.5f);
            var heading = Quaternion.LookRotation(to - from);
            for (var i = 0; i <= samples; i++)
            {
                var point = Vector3.Lerp(from, to, i / (float)samples);
                foreach (var obstacle in Physics.OverlapBox(point + Vector3.up * .92f, new Vector3(1.43f, .78f, .28f), heading, ~0,
                    QueryTriggerInteraction.Ignore))
                {
                    if (obstacle.attachedRigidbody != null || obstacle.bounds.max.y <= .32f) continue;
                    throw new Exception($"{course} route meets {obstacle.name} at {point}");
                }
            }
        }

        private static void PositionPedestrianImpact()
        {
            _scoreBeforePedestrian = _session.Mission.Score;
            _impactPedestrian = Object.FindFirstObjectByType<CityPedestrian>();
            var target = new Vector3(2.3f, .21f, 134f);
            var walkerBody = _impactPedestrian.GetComponent<Rigidbody>();
            walkerBody.position = target;
            walkerBody.linearVelocity = Vector3.zero;
            var body = _session.Mission.VehicleBody;
            body.position = target - Vector3.forward * 8f;
            body.rotation = Quaternion.identity;
            body.angularVelocity = Vector3.zero;
            body.linearVelocity = Vector3.forward * 19f;
            Physics.SyncTransforms();
            _session.Mission.SyncPosition();
            _nextAt = EditorApplication.timeSinceStartup + .7;
        }

        private static void PositionCurbLampImpact()
        {
            var spec = Array.Find(_session.Mission.Course.Targets, target => target.Kind == ImpactTargetKind.Streetlight);
            _curbLamp = null;
            foreach (var target in Object.FindObjectsByType<ImpactCourseTarget>(FindObjectsSortMode.None))
                if (Vector3.Distance(target.transform.position, spec.Position) < .1f) _curbLamp = target.GetComponent<Rigidbody>();
            Check(_curbLamp != null && _curbLamp.isKinematic, "The marked curbside streetlight spawns with a solid breakaway base");
            var approach = new Vector3(.65f, 0f, 1f).normalized;
            var startingPoint = spec.Position - approach * 7f;
            startingPoint.y = .21f;
            _session.vehicle.RecoverAt(startingPoint, Quaternion.LookRotation(approach));
            _session.Mission.SyncPosition();
            // Let the recovery settle the suspension before applying the approach speed.
            _curbLampApproachAt = Time.time + .33f;
        }

        private static void CaptureFrame(string name)
        {
            var output = Environment.GetEnvironmentVariable("CRASH_QA_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(output) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Directory.CreateDirectory(output);
            var camera = _session.GetComponent<Camera>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var width = Screen.width;
            var height = Screen.height;
            var previous = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var cameraCanvas = canvas.worldCamera;
            var mode = canvas.renderMode;
            var surface = RenderTexture.GetTemporary(width, height, 24);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = surface;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1.2f;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = surface;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                File.WriteAllBytes(Path.Combine(output, name + ".png"), pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = mode;
                canvas.worldCamera = cameraCanvas;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(surface);
                Object.Destroy(pixels);
            }
        }

        private static void DriveAcross(ImpactGate gate)
        {
            var body = _session.Mission.VehicleBody;
            var forward = gate.Rotation * Vector3.forward;
            body.position = gate.Position - forward * 4f;
            body.rotation = gate.Rotation;
            body.angularVelocity = Vector3.zero;
            body.linearVelocity = forward * 15f;
            Physics.SyncTransforms();
            _session.Mission.SyncPosition();
            _nextAt = EditorApplication.timeSinceStartup + .62;
        }

        private static void CrossDemoGate()
        {
            var gate = _session.Mission.NextGate;
            var forward = gate.Rotation * Vector3.forward;
            // Keep the arrow check independent of collisions with the practice targets.
            _session.vehicle.RecoverAt(gate.Position - forward * 2f, gate.Rotation);
            _session.Mission.SyncPosition();
            _session.vehicle.RecoverAt(gate.Position + forward * 2f, gate.Rotation);
            _session.Mission.Step(_session.ElapsedSeconds);
        }

        private static void CaptureCareerPrefs()
        {
            CareerPrefs.Clear();
            foreach (var course in ImpactCourse.Catalog)
            {
                Capture(ImpactCareer.ScoreKey(course));
                Capture(ImpactCareer.FinishKey(course));
            }
            Capture(ImpactCareer.TargetsKey);

            void Capture(string key)
            {
                CareerPrefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
                PlayerPrefs.DeleteKey(key);
            }
        }

        private static void Pointer(string name, bool down, int id)
        {
            var control = GameObject.Find(name);
            if (control == null) throw new Exception("Missing touch control: " + name);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = id };
            if (down) ExecuteEvents.Execute(control, pointer, ExecuteEvents.pointerDownHandler);
            else ExecuteEvents.Execute(control, pointer, ExecuteEvents.pointerUpHandler);
        }

        private static int AttachedDamageParts()
        {
            var count=0;
            foreach(var part in _session.vehicle.GetComponentsInChildren<Transform>(true))
                if(part.name.StartsWith("Damage_",StringComparison.Ordinal))count++;
            return count;
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
            AudioListener.volume = SessionState.GetFloat(SavedListenerVolumeKey, 1f);
            SessionState.EraseFloat(SavedListenerVolumeKey);
            var oldDemoSeen = SessionState.GetInt(SavedDemoSeenKey, -1);
            if (oldDemoSeen >= 0) PlayerPrefs.SetInt(CarCrashHudInput.DrivingDemoSeenKey, oldDemoSeen);
            else PlayerPrefs.DeleteKey(CarCrashHudInput.DrivingDemoSeenKey);
            SessionState.EraseInt(SavedDemoSeenKey);
            foreach (var (key, value) in CareerPrefs)
            {
                if (value.HasValue) PlayerPrefs.SetInt(key, value.Value);
                else PlayerPrefs.DeleteKey(key);
            }
            CareerPrefs.Clear();
            if (_capturedPrefs)
            {
                if (_hadPaint) PlayerPrefs.SetInt("CarCrash.Paint", _oldPaint); else PlayerPrefs.DeleteKey("CarCrash.Paint");
                if (_hadRoadbreakerPaint) PlayerPrefs.SetInt(GarageCustomization.PaintKey("roadbreaker"), _oldRoadbreakerPaint);
                else PlayerPrefs.DeleteKey(GarageCustomization.PaintKey("roadbreaker"));
                if (_hadBest) PlayerPrefs.SetInt("CarCrash.BestImpactScore", _oldBest); else PlayerPrefs.DeleteKey("CarCrash.BestImpactScore");
                if (_hadVehicle) PlayerPrefs.SetInt("CarCrash.Vehicle", _oldVehicle); else PlayerPrefs.DeleteKey("CarCrash.Vehicle");
                if(_hadExplored)PlayerPrefs.SetInt("CarCrash.ExploredPlaces",_oldExplored);else PlayerPrefs.DeleteKey("CarCrash.ExploredPlaces");
                if (_oldMusic.HasValue) PlayerPrefs.SetInt(ElysianSoundtrack.PreferenceKey, _oldMusic.Value);
                else PlayerPrefs.DeleteKey(ElysianSoundtrack.PreferenceKey);
                if (_oldEffects.HasValue) PlayerPrefs.SetInt(ElysianSoundtrack.EffectsPreferenceKey, _oldEffects.Value);
                else PlayerPrefs.DeleteKey(ElysianSoundtrack.EffectsPreferenceKey);
                if (_oldNight.HasValue) PlayerPrefs.SetInt(CityLightingRig.NightPreferenceKey, _oldNight.Value);
                else PlayerPrefs.DeleteKey(CityLightingRig.NightPreferenceKey);
            }
            PlayerPrefs.Save();
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { passed = error == null, checks = Checks.ToArray(), error = error ?? "" }, true));
            if (error == null) Debug.Log("RACING_UI_VALIDATION_PASS: " + Checks.Count + " checks");
            else Debug.LogError("RACING_UI_VALIDATION_FAIL: " + error);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
    }
}
