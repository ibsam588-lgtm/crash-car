using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarCrash
{
    public sealed partial class CarCrashHudInput : MonoBehaviour
    {
        public enum RunPhase { Home, Briefing, Showroom, Countdown, Driving, Paused, Results }
        public enum DriveMode { FreeDrive, ImpactRun, TimeTrial }
        public ArcadeVehicleController vehicle;
        public CrashScoring scoring;
        public static readonly Color[] PaintColors =
        {
            new(0.22f, 0.26f, 0.30f), new(0.70f, 0.75f, 0.80f),
            new(0.06f, 0.24f, 0.42f), new(0.60f, 0.035f, 0.025f), new(0.86f, 0.87f, 0.84f), new(0.64f, 0.37f, 0.09f)
        };
        public static readonly string[] PaintNames = { "MIDNIGHT GRAPHITE", "ARCTIC SILVER", "OCEAN BLUE", "CARMINE RED", "PEARL WHITE", "TRACK GOLD" };
        public RunPhase Phase { get; private set; } = RunPhase.Home;
        public DriveMode Mode { get; private set; } = DriveMode.FreeDrive;
        public bool IsTrial => Mode == DriveMode.TimeTrial;
        public bool IsMission => Mode != DriveMode.FreeDrive;
        public bool IsTimed => IsMission && SelectedCourse.TimeLimit > 0;
        public bool GarageForMission => _garageForMission;
        public ImpactMission Mission { get; private set; }
        public ElysianPlayGames PlayGames { get; private set; }
        public ElysianSoundtrack Soundtrack { get; private set; }
        public ImpactCourse SelectedCourse => DrivingDemoActive ? DemoCourse : IsTrial ? TimeTrialCareer.Levels[TrialIndex] : ImpactCourse.Catalog[_selectedCourse];
        public bool CourseUnlocked => DrivingDemoActive || (IsTrial ? DailyTrial || TimeTrialCareer.Unlocked(TrialIndex) : ImpactCareer.Unlocked(_selectedCourse));
        public bool NextCourseUnlocked => IsTrial ? !DailyTrial && TimeTrialCareer.Unlocked(TrialIndex + 1) : ImpactCareer.Unlocked(_selectedCourse + 1);
        public float ElapsedSeconds { get; private set; }
        public float PeakSpeed { get; private set; }
        public float CountdownRemaining { get; private set; }
        public int BestScore => IsMission ? ImpactCareer.Best(SelectedCourse) : _freeDriveBest;
        public int PaintIndex { get; private set; }
        public bool NewBest { get; private set; }
        public CarCrashCameraFollow CameraRig { get; private set; }
        public RacingHudView Hud { get; private set; }
        public VehicleGarage Garage { get; private set; }
        public CityNavigator Navigation { get; private set; }
        public bool MapVisible { get; private set; }
        private bool _resumeAfterMap;
        private static bool _retryImmediately;
        private static RunPhase _nextMenu = RunPhase.Home;
        private static DriveMode _selectedMode = DriveMode.FreeDrive;
        private static int _selectedCourse;
        private int _freeDriveBest;
        private bool _garageForMission;
        private GarageShowroom _garageShowroom;
        private MaterialPropertyBlock _paintBlock;
        private bool _focused = true;
        private ImpactRunLoopDirector _recovery;
        private float _reverseHoldSeconds;
        private MotionSteering _motionSteering;
        public bool MotionSteeringAvailable => _motionSteering?.Available == true;
        public bool MotionSteeringEnabled => _motionSteering?.Enabled == true;
        public bool MotionSteeringCalibrating => _motionSteering?.Calibrating == true;

        private void Awake()
        {
            _paintBlock = new MaterialPropertyBlock();
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        private void Start()
        {
            if (vehicle == null || scoring == null)
            {
                Debug.LogError("The racing UI requires a vehicle and scoring component.", this);
                enabled = false;
                return;
            }
            CameraRig = GetComponent<CarCrashCameraFollow>();
            _recovery = GetComponent<ImpactRunLoopDirector>();
            Mode = _selectedMode;
            scoring.AwardCityPropPoints = !IsMission;
            Phase = _nextMenu;
            _nextMenu = RunPhase.Home;
            _garageForMission = Phase == RunPhase.Showroom && IsMission;
            _freeDriveBest = PlayerPrefs.GetInt("CarCrash.BestImpactScore", 0);
            Garage = vehicle.GetComponent<VehicleGarage>();
            if (Garage != null) Garage.Select(SavedUsableRide());
            Navigation=gameObject.AddComponent<CityNavigator>();Navigation.player=vehicle.transform;
            Mission = gameObject.AddComponent<ImpactMission>();
            PlayGames = gameObject.AddComponent<ElysianPlayGames>();
            Soundtrack = ElysianSoundtrack.GetOrCreate();
            _motionSteering = new MotionSteering();
            RestoreVehiclePaint();
            scoring.SetSimulationPaused(true);
            CameraRig.SetShowcase(true);
            CameraRig.HomeScreen = Phase == RunPhase.Home;
            if (Phase == RunPhase.Showroom) EnterGarageShowroom();
            Hud = gameObject.AddComponent<RacingHudView>();
            BuildDrivingDemo();
            Hud.Initialize(this);
            if (_retryImmediately)
            {
                _retryImmediately = false;
                Phase = IsMission ? RunPhase.Briefing : RunPhase.Showroom;
                BeginRun();
            }
        }

        private void Update()
        {
            if (Hud == null) return;
            if (Hud.PremiumVideoVisible)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) Hud.ClosePremiumVideo();
                return;
            }
            if (HandleDrivingDemoShortcuts()) return;
            _motionSteering.Step(Time.unscaledDeltaTime);
            if (Hud.SettingsVisible)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) Hud.ShowSettings(false);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if(MapVisible)CloseMap();
                else if (Hud.HelpVisible) Hud.ShowHelp(false);
                else if (Hud.CustomizationVisible) Hud.ShowCustomization(false);
                else if (Phase == RunPhase.Showroom) ReturnFromGarage();
                else if (Phase == RunPhase.Briefing) ReturnHome();
                else if (Phase == RunPhase.Driving) PauseRun();
                else if (Phase == RunPhase.Paused) ResumeRun();
            }
            if(Input.GetKeyDown(KeyCode.M))ToggleMap();
            if (Phase == RunPhase.Home && !Hud.HelpVisible)
            {
                if (Input.GetKeyDown(KeyCode.Return)) StartImpactRun();
                if (Input.GetKeyDown(KeyCode.G)) OpenGarage();
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (Phase == RunPhase.Showroom && !Hud.CustomizationVisible) ConfirmGarage();
                else if (Phase == RunPhase.Briefing && !Hud.HelpVisible && !_openedBriefingThisFrame) BeginRun();
            }
            _openedBriefingThisFrame = false;
            if (Phase == RunPhase.Results && Input.GetKeyDown(KeyCode.R)) RestartRun();
            if (Phase == RunPhase.Countdown)
            {
                if (!_focused) return;
                CountdownRemaining -= Time.unscaledDeltaTime;
                if (CountdownRemaining <= 0f)
                {
                    vehicle.ResetForRelaunch(0f);
                    Phase = RunPhase.Driving;
                    scoring.SetSimulationPaused(false);
                    if (IsMission) Mission.StartDriving(IsTrial);
                    Hud.RefreshPhase();
                }
            }
            else if (Phase == RunPhase.Driving)
            {
                ElapsedSeconds += Time.unscaledDeltaTime;
                PeakSpeed = Mathf.Max(PeakSpeed, vehicle.SpeedKmh);
                if (Input.GetKeyDown(KeyCode.C)) CycleCamera();
                if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.LeftControl)) ActivateBoost();
                if (Input.GetKeyDown(KeyCode.R)) RecoverVehicle();
                var left = Hud.LeftHeld || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                var right = Hud.RightHeld || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                var gas = Hud.GasHeld || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                var brakeReverse = Hud.BrakeHeld || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                var explicitReverse = Hud.ReverseHeld || Input.GetKey(KeyCode.Q);
                var hardBrake = Input.GetKey(KeyCode.Space);
                _reverseHoldSeconds = brakeReverse && !gas && !hardBrake && vehicle.ForwardSpeedKmh < 2f
                    ? _reverseHoldSeconds + Time.deltaTime : 0f;
                var reverse = !gas && ((_reverseHoldSeconds >= 0.35f) || (explicitReverse && vehicle.ForwardSpeedKmh < 1.8f));
                var brake = hardBrake || ((brakeReverse || explicitReverse) && !reverse) || (gas && vehicle.ForwardSpeedKmh < -2f);
                var throttle = brake ? 0f : reverse ? -1f : gas ? 1f : 0f;
                var steering = left || right ? (right ? 1f : 0f) - (left ? 1f : 0f) : _motionSteering.Value;
                vehicle.SetMobileInput(steering, throttle, brake ? 1f : 0f);
                if (IsMission)
                {
                    Mission.Step(ElapsedSeconds);
                    if (Mission.Completed || Mission.TimedOut) FinishRun();
                }
            }
            Hud.RefreshTelemetry();
        }

        public void SetMotionSteeringEnabled(bool enabled) => _motionSteering?.SetEnabled(enabled);
        public void CenterMotionSteering() => _motionSteering?.Center();

        public void BeginRun()
        {
            if (IsMission ? Phase != RunPhase.Briefing || !CourseUnlocked : Phase is not (RunPhase.Showroom or RunPhase.Home)) return;
            LeaveGarageShowroom();
            RestoreUsableRide();
            _selectedMode = Mode;
            scoring.AwardCityPropPoints = !IsMission;
            scoring.ImpactSlowMotionEnabled = !IsTrial;
            Phase = RunPhase.Countdown;
            CountdownRemaining = 3f;
            vehicle.SetMobileInput(0f, 0f, 0f);
            if (IsMission) Mission.Prepare(SelectedCourse, vehicle, Garage);
            CameraRig.SetShowcase(false);
            Hud.RefreshPhase();
        }

        public void StartFreeDrive()
        {
            if (Phase != RunPhase.Home && Phase != RunPhase.Showroom) return;
            Mode = DriveMode.FreeDrive;
            BeginRun();
        }

        public void StartImpactRun()
        {
            if (Phase != RunPhase.Home) return;
            Mode = DriveMode.ImpactRun;
            ShowBriefing();
        }

        private bool _openedBriefingThisFrame;

        private void ShowBriefing()
        {
            LeaveGarageShowroom();
            Phase = RunPhase.Briefing;
            _openedBriefingThisFrame = true;
            _garageForMission = false;
            Hud.ShowHelp(false);
            CameraRig.HomeScreen = false;
            Hud.RefreshPhase();
        }

        public void ChangeCourse(int offset)
        {
            if (Phase != RunPhase.Briefing) return;
            if (IsTrial)
            {
                ChangeTrial(offset);
                return;
            }
            var count = ImpactCourse.Catalog.Length;
            _selectedCourse = (_selectedCourse + offset + count) % count;
            Hud.RefreshBriefing();
        }

        public void NextCourse()
        {
            if (Phase != RunPhase.Results || !IsMission) return;
            if (Mission.Completed && NextCourseUnlocked)
            {
                if (IsTrial) _selectedTrial++;
                else _selectedCourse++;
            }
            if (IsTrial) _dailyTrialDay = null;
            _selectedMode = Mode;
            _nextMenu = RunPhase.Briefing;
            ReloadScene();
        }

        public void OpenGarage()
        {
            if (Phase is not (RunPhase.Home or RunPhase.Briefing)) return;
            _garageForMission = Phase == RunPhase.Briefing;
            if (!_garageForMission) Mode = DriveMode.FreeDrive;
            Phase = RunPhase.Showroom;
            CameraRig.HomeScreen = false;
            EnterGarageShowroom();
            Hud.ShowHelp(false);
            Hud.RefreshPhase();
        }

        private void EnterGarageShowroom()
        {
            if (_garageShowroom == null) _garageShowroom = GarageShowroom.Create();
            _garageShowroom.Enter(vehicle.GetComponent<Rigidbody>(), Garage?.Current);
            CameraRig.SetGarageShowroom(true);
        }

        private void LeaveGarageShowroom()
        {
            if (_garageShowroom == null || !_garageShowroom.isActiveAndEnabled) return;
            _garageShowroom.Leave();
            CameraRig.SetGarageShowroom(false);
        }

        public void ConfirmGarage()
        {
            if (Phase != RunPhase.Showroom) return;
            if (SelectedRideLocked)
            {
                Hud.ShowPremiumVideo(Garage.Current.id);
                return;
            }
            if (_garageForMission) ShowBriefing();
            else StartFreeDrive();
        }

        public void ReturnFromGarage()
        {
            if (Phase != RunPhase.Showroom) return;
            LeaveGarageShowroom();
            RestoreUsableRide();
            if (_garageForMission) ShowBriefing();
            else ReturnHome();
        }

        public void RecoverVehicle()
        {
            if (Phase != RunPhase.Driving && Phase != RunPhase.Paused) return;
            ReleaseInput();
            if (IsMission)
            {
                if (_recovery != null && _recovery.RecoverFromCheckpoint(Mission.RecoveryPosition, Mission.RecoveryRotation))
                {
                    Mission.SyncPosition();
                    if (IsTrial) ElapsedSeconds += 4f;
                }
            }
            else
            {
                _recovery?.RecoverVehicle();
                if (_recovery != null && !_recovery.RecoverySuggested && Navigation.Destination >= 0)
                    Navigation.NavigateTo(Navigation.Destination);
            }
            if (Phase == RunPhase.Paused) ResumeRun();
        }

        public bool RecoverySuggested => _recovery != null && _recovery.RecoverySuggested;

        public void ToggleMap()
        {
            if(MapVisible){CloseMap();return;}
            if(Phase!=RunPhase.Driving&&Phase!=RunPhase.Paused)return;
            _resumeAfterMap=Phase==RunPhase.Driving;
            if(_resumeAfterMap)PauseRun();
            MapVisible=true;Hud.RefreshPhase();
        }
        public void CloseMap()
        {
            if(!MapVisible)return;MapVisible=false;
            if(_resumeAfterMap)ResumeRun();else Hud.RefreshPhase();
        }
        public void ExploreDestination(int index)
        {
            if (!MapVisible || IsMission) return;
            Navigation.NavigateTo(index);
            CloseMap();
        }

        public void PauseRun()
        {
            if (Phase != RunPhase.Driving) return;
            Phase = RunPhase.Paused;
            ReleaseInput();
            scoring.SetSimulationPaused(true);
            Hud.RefreshPhase();
        }

        public void ResumeRun()
        {
            if (Phase != RunPhase.Paused) return;
            ReleaseInput();
            Phase = RunPhase.Driving;
            scoring.SetSimulationPaused(false);
            Hud.RefreshPhase();
        }

        public void FinishRun()
        {
            if (Phase != RunPhase.Driving) return;
            Phase = RunPhase.Results;
            ReleaseInput();
            scoring.SetSimulationPaused(true);
            if (IsMission)
            {
                Mission.Stop();
                if (Mission.Completed && !DrivingDemoActive)
                {
                    if (IsTrial) NewBest = TimeTrialCareer.Record(TrialIndex, _dailyTrialDay, ElapsedSeconds);
                    else
                    {
                        NewBest = ImpactCareer.Record(SelectedCourse, Mission.Score, Mission.Hits);
                        PlayGames.RecordCompletion();
                    }
                }
            }
            else SaveBestScore();
            Hud.RefreshPhase();
        }

        private void SaveBestScore()
        {
            NewBest = scoring.Score > BestScore;
            if (NewBest)
            {
                _freeDriveBest = scoring.Score;
                PlayerPrefs.SetInt("CarCrash.BestImpactScore", _freeDriveBest);
                PlayerPrefs.Save();
            }
        }

        public void EndSession()
        {
            if (Phase == RunPhase.Paused) Phase = RunPhase.Driving;
            FinishRun();
        }

        public void ShowLeaderboard() => PlayGames.Show(SelectedCourse, false);
        public void ShowAchievements() => PlayGames.Show(SelectedCourse, true);

        public void ActivateBoost()
        {
            if (Phase == RunPhase.Driving) vehicle.TryBoost(1.55f);
        }
        public void CycleCamera()
        {
            if (Phase == RunPhase.Driving) CameraRig.CycleView();
        }
        public void SelectPaint(int index)
        {
            if (Phase != RunPhase.Showroom || !PaintUnlocked(index)) return;
            ApplyPaint(index);
            if (Garage != null) Garage.RememberPaint(index);
            else
            {
                PlayerPrefs.SetInt("CarCrash.Paint", index);
                PlayerPrefs.Save();
            }
            Hud.RefreshPaint();
        }

        public static bool PaintUnlocked(int index) => index >= 0 && index < PaintColors.Length &&
            (index < PaintColors.Length - 1 || ImpactCareer.GoldPaintUnlocked);

        public void SelectVehicle(int index)
        {
            if (Phase != RunPhase.Showroom || Garage == null || index < 0 || index >= Garage.vehicles.Length) return;
            ReleaseInput();
            Garage.Select(index);
            RestoreVehiclePaint();
            _garageShowroom?.PlaceVehicle(Garage.Current);
            CameraRig.SetShowcase(true);
            if (!SelectedRideLocked) RememberRide();
            Hud.RefreshPaint();
            Hud.RefreshVehicle();
            Hud.RefreshCustomization();
        }

        public void StepGarageUpgrade(GarageUpgrade upgrade, int direction)
        {
            if (Phase != RunPhase.Showroom || Garage?.Current == null) return;
            var options = GarageCustomization.Options(Garage.Current.id, upgrade);
            if (options.Count < 2) return;
            var next = (Garage.GetUpgrade(upgrade) + (direction < 0 ? -1 : 1) + options.Count) % options.Count;
            if (Garage.SetUpgrade(upgrade, next) && upgrade == GarageUpgrade.PaintFinish) ApplyPaint(PaintIndex);
        }

        private void RestoreVehiclePaint()
        {
            var legacy = PlayerPrefs.GetInt("CarCrash.Paint", 0);
            var stored = Garage != null ? Garage.GetPaint(legacy) : legacy;
            var index = Mathf.Clamp(stored, 0, PaintColors.Length - 1);
            ApplyPaint(PaintUnlocked(index) ? index : 0);
        }

        private void ApplyPaint(int index)
        {
            PaintIndex = index;
            var finish = Garage?.GetUpgrade(GarageUpgrade.PaintFinish) ?? 0;
            // Per-material blocks preserve glass, lights, wheels and shared traffic materials.
            foreach (var renderer in vehicle.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (var slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    if (material == null) continue;
                    var bodyPaint = material.name.Contains("Competition_Orange") || material.name.Contains("Orange_Shadow") ||
                        material.name.Contains("Competition_Yellow") || material.name.Contains("Yellow_Shadow") || material.name.Contains("Paint_1") ||
                        material.name.StartsWith("M_Garage_Paint") || material.name.StartsWith("M_Fleet_Paint");
                    if (!bodyPaint) continue;
                    renderer.GetPropertyBlock(_paintBlock, slot);
                    var color = PaintColors[index];
                    if (material.name.Contains("Shadow")) color *= 0.7f;
                    color.a = 1f;
                    _paintBlock.SetColor("_Color", color);
                    _paintBlock.SetFloat("_Metallic", GarageCustomization.BodyMetallic[finish]);
                    _paintBlock.SetFloat("_Glossiness", GarageCustomization.BodyGloss[finish]);
                    if (material.HasProperty("_LiveryOrigin"))
                    {
                        var origin = renderer.transform.InverseTransformPoint(vehicle.transform.position);
                        var side = renderer.transform.InverseTransformDirection(vehicle.transform.right);
                        _paintBlock.SetVector("_LiveryOrigin", new Vector4(origin.x, origin.y, origin.z, index == PaintColors.Length - 1 ? 1f : 0f));
                        _paintBlock.SetVector("_LiverySide", side);
                    }
                    renderer.SetPropertyBlock(_paintBlock, slot);
                    _paintBlock.Clear();
                }
            }
        }
        public void RestartRun()
        {
            if (Phase != RunPhase.Results) return;
            _retryImmediately = true;
            _selectedMode = Mode;
            ReloadScene();
        }
        public void ReturnToShowroom()
        {
            _retryImmediately = false;
            _selectedMode = Mode;
            _nextMenu = RunPhase.Showroom;
            ReloadScene();
        }
        public void ReturnHome()
        {
            _retryImmediately = false;
            _selectedMode = DriveMode.FreeDrive;
            if (Phase is RunPhase.Briefing or RunPhase.Showroom)
            {
                LeaveGarageShowroom();
                Mode = DriveMode.FreeDrive;
                Phase = RunPhase.Home;
                CameraRig.HomeScreen = true;
                Hud.RefreshPhase();
                return;
            }
            _nextMenu = RunPhase.Home;
            ReloadScene();
        }
        private void ReloadScene()
        {
            LeaveGarageShowroom();
            if (!IsMission) SaveBestScore();
            ReleaseInput();
            scoring.SetSimulationPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        private void ReleaseInput()
        {
            _reverseHoldSeconds = 0f;
            Hud?.ReleaseControls();
            vehicle?.SetMobileInput(0f, 0f, 0f);
        }
        private void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            if (!focused) PauseRun();
        }
        private void OnApplicationPause(bool paused)
        {
            _focused = !paused;
            if (paused) PauseRun();
        }
        private void OnDestroy()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
