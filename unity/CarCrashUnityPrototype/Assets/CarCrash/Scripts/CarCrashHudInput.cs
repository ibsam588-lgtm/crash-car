using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarCrash
{
    public sealed class CarCrashHudInput : MonoBehaviour
    {
        public enum RunPhase { Home, Showroom, Countdown, Driving, Paused, Results }
        public enum DriveMode { FreeDrive, ImpactRun }
        public ArcadeVehicleController vehicle;
        public CrashScoring scoring;
        public const float RunDuration = 60f;
        public static readonly Color[] PaintColors =
        {
            new(0.22f, 0.26f, 0.30f), new(0.70f, 0.75f, 0.80f),
            new(0.06f, 0.24f, 0.42f), new(0.60f, 0.035f, 0.025f), new(0.86f, 0.87f, 0.84f)
        };
        public static readonly string[] PaintNames = { "MIDNIGHT GRAPHITE", "ARCTIC SILVER", "OCEAN BLUE", "CARMINE RED", "PEARL WHITE" };
        public RunPhase Phase { get; private set; } = RunPhase.Home;
        public DriveMode Mode { get; private set; } = DriveMode.FreeDrive;
        public bool IsTimed => Mode == DriveMode.ImpactRun;
        public float ElapsedSeconds { get; private set; }
        public float PeakSpeed { get; private set; }
        public float CountdownRemaining { get; private set; }
        public int BestScore { get; private set; }
        public int PaintIndex { get; private set; }
        public bool NewBest { get; private set; }
        public CarCrashCameraFollow CameraRig { get; private set; }
        public RacingHudView Hud { get; private set; }
        public VehicleGarage Garage { get; private set; }
        private static bool _retryImmediately;
        private static RunPhase _nextMenu = RunPhase.Home;
        private static DriveMode _selectedMode = DriveMode.FreeDrive;
        private MaterialPropertyBlock _paintBlock;
        private bool _focused = true;
        private ImpactRunLoopDirector _recovery;
        private float _reverseHoldSeconds;

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
            Phase = _nextMenu;
            _nextMenu = RunPhase.Home;
            BestScore = PlayerPrefs.GetInt("CarCrash.BestImpactScore", 0);
            PaintIndex = Mathf.Clamp(PlayerPrefs.GetInt("CarCrash.Paint", 0), 0, PaintColors.Length - 1);
            Garage = vehicle.GetComponent<VehicleGarage>();
            if (Garage != null) Garage.Select(PlayerPrefs.GetInt("CarCrash.Vehicle", 0));
            ApplyPaint(PaintIndex);
            scoring.SetSimulationPaused(true);
            CameraRig.SetShowcase(true);
            CameraRig.HomeScreen = Phase == RunPhase.Home;
            Hud = gameObject.AddComponent<RacingHudView>();
            Hud.Initialize(this);
            if (_retryImmediately)
            {
                _retryImmediately = false;
                Phase = RunPhase.Showroom;
                BeginRun();
            }
        }

        private void Update()
        {
            if (Hud == null) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (Hud.HelpVisible) Hud.ShowHelp(false);
                else if (Phase == RunPhase.Showroom) ReturnHome();
                else if (Phase == RunPhase.Driving) PauseRun();
                else if (Phase == RunPhase.Paused) ResumeRun();
            }
            if (Phase == RunPhase.Home && !Hud.HelpVisible)
            {
                if (Input.GetKeyDown(KeyCode.Return)) StartFreeDrive();
                if (Input.GetKeyDown(KeyCode.G)) OpenGarage();
            }
            if (Phase == RunPhase.Showroom && Input.GetKeyDown(KeyCode.Return)) BeginRun();
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
                vehicle.SetMobileInput((right ? 1f : 0f) - (left ? 1f : 0f), throttle, brake ? 1f : 0f);
                // Only the challenge owns a time limit. Stopping after a collision
                // never ends a drive, and Free Drive stays open until the player exits.
                if (IsTimed && (ElapsedSeconds >= RunDuration || scoring.Damage >= 100f)) FinishRun();
            }
            Hud.RefreshTelemetry();
        }

        public void BeginRun()
        {
            if (Phase != RunPhase.Showroom && Phase != RunPhase.Home) return;
            _selectedMode = Mode;
            Phase = RunPhase.Countdown;
            CountdownRemaining = 3f;
            vehicle.SetMobileInput(0f, 0f, 0f);
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
            BeginRun();
        }

        public void OpenGarage()
        {
            if (Phase != RunPhase.Home) return;
            Mode = DriveMode.FreeDrive;
            Phase = RunPhase.Showroom;
            CameraRig.HomeScreen = false;
            Hud.ShowHelp(false);
            Hud.RefreshPhase();
        }

        public void RecoverVehicle()
        {
            if (Phase != RunPhase.Driving && Phase != RunPhase.Paused) return;
            ReleaseInput();
            _recovery?.RecoverVehicle();
            if (Phase == RunPhase.Paused) ResumeRun();
        }

        public bool RecoverySuggested => _recovery != null && _recovery.RecoverySuggested;

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
            SaveBestScore();
            Hud.RefreshPhase();
        }

        private void SaveBestScore()
        {
            NewBest = scoring.Score > BestScore;
            if (NewBest)
            {
                BestScore = scoring.Score;
                PlayerPrefs.SetInt("CarCrash.BestImpactScore", BestScore);
                PlayerPrefs.Save();
            }
        }

        public void EndSession()
        {
            if (Phase == RunPhase.Paused) Phase = RunPhase.Driving;
            FinishRun();
        }

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
            if (Phase != RunPhase.Showroom || index < 0 || index >= PaintColors.Length) return;
            ApplyPaint(index);
            PlayerPrefs.SetInt("CarCrash.Paint", index);
            PlayerPrefs.Save();
            Hud.RefreshPaint();
        }

        public void SelectVehicle(int index)
        {
            if (Phase != RunPhase.Showroom || Garage == null || index < 0 || index >= Garage.vehicles.Length) return;
            ReleaseInput();
            Garage.Select(index);
            ApplyPaint(PaintIndex);
            CameraRig.SetShowcase(true);
            PlayerPrefs.SetInt("CarCrash.Vehicle", index);
            PlayerPrefs.Save();
            Hud.RefreshVehicle();
        }
        private void ApplyPaint(int index)
        {
            PaintIndex = index;
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
                    _paintBlock.SetFloat("_Metallic", 0.45f);
                    _paintBlock.SetFloat("_Glossiness", 0.78f);
                    renderer.SetPropertyBlock(_paintBlock, slot);
                    _paintBlock.Clear();
                }
            }
        }
        public void RestartRun()
        {
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
            _nextMenu = RunPhase.Home;
            ReloadScene();
        }
        private void ReloadScene()
        {
            SaveBestScore();
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
