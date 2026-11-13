using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarCrash
{
    public sealed class CarCrashHudInput : MonoBehaviour
    {
        public enum RunPhase { Showroom, Countdown, Driving, Paused, Results }
        public ArcadeVehicleController vehicle;
        public CrashScoring scoring;
        public const float RunDuration = 60f;
        public static readonly Color[] PaintColors =
        {
            new(1f, 0.61f, 0.045f), new(0.12f, 0.57f, 0.86f),
            new(0.82f, 0.06f, 0.055f), new(0.72f, 0.77f, 0.80f), new(0.10f, 0.13f, 0.16f)
        };
        public static readonly string[] PaintNames = { "COMPETITION GOLD", "APEX BLUE", "SIGNAL RED", "GLACIER SILVER", "CARBON BLACK" };
        public RunPhase Phase { get; private set; } = RunPhase.Showroom;
        public float ElapsedSeconds { get; private set; }
        public float PeakSpeed { get; private set; }
        public float CountdownRemaining { get; private set; }
        public int BestScore { get; private set; }
        public int PaintIndex { get; private set; }
        public bool NewBest { get; private set; }
        public CarCrashCameraFollow CameraRig { get; private set; }
        public RacingHudView Hud { get; private set; }
        private static bool _retryImmediately;
        private MaterialPropertyBlock _paintBlock;
        private int _observedHits;
        private float _secondsSinceImpact;
        private bool _focused = true;

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
            BestScore = PlayerPrefs.GetInt("CarCrash.BestImpactScore", 0);
            PaintIndex = Mathf.Clamp(PlayerPrefs.GetInt("CarCrash.Paint", 0), 0, PaintColors.Length - 1);
            ApplyPaint(PaintIndex);
            scoring.SetSimulationPaused(true);
            CameraRig.SetShowcase(true);
            Hud = gameObject.AddComponent<RacingHudView>();
            Hud.Initialize(this);
            if (_retryImmediately)
            {
                _retryImmediately = false;
                BeginRun();
            }
        }

        private void Update()
        {
            if (Hud == null) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (Phase == RunPhase.Driving) PauseRun();
                else if (Phase == RunPhase.Paused) ResumeRun();
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
                if (scoring.Hits != _observedHits)
                {
                    _observedHits = scoring.Hits;
                    _secondsSinceImpact = 0f;
                }
                else _secondsSinceImpact += Time.unscaledDeltaTime;
                PeakSpeed = Mathf.Max(PeakSpeed, vehicle.SpeedKmh);
                if (Input.GetKeyDown(KeyCode.C)) CycleCamera();
                if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.LeftControl)) ActivateBoost();
                var steer = Mathf.Clamp(Input.GetAxisRaw("Horizontal") + (Hud.RightHeld ? 1f : 0f) - (Hud.LeftHeld ? 1f : 0f), -1f, 1f);
                var throttle = Hud.GasHeld ? 1f : Mathf.Clamp01(Input.GetAxisRaw("Vertical"));
                var brake = Hud.BrakeHeld || Input.GetKey(KeyCode.Space) ? 1f : Mathf.Clamp01(-Input.GetAxisRaw("Vertical"));
                vehicle.SetMobileInput(steer, brake > 0f ? 0f : throttle, brake);
                var settledWreck = scoring.Hits > 0 && vehicle.SpeedKmh < 8f && _secondsSinceImpact > 4.5f;
                if (ElapsedSeconds >= RunDuration || scoring.Damage >= 100f || settledWreck) FinishRun();
            }
            Hud.RefreshTelemetry();
        }

        public void BeginRun()
        {
            if (Phase != RunPhase.Showroom) return;
            Phase = RunPhase.Countdown;
            CountdownRemaining = 3f;
            vehicle.SetMobileInput(0f, 0f, 0f);
            CameraRig.SetShowcase(false);
            Hud.RefreshPhase();
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
            NewBest = scoring.Score > BestScore;
            if (NewBest)
            {
                BestScore = scoring.Score;
                PlayerPrefs.SetInt("CarCrash.BestImpactScore", BestScore);
                PlayerPrefs.Save();
            }
            Hud.RefreshPhase();
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
                        material.name.Contains("Competition_Yellow") || material.name.Contains("Yellow_Shadow");
                    var wheelMetal = material.name == "Rim1" || material.name == "Rim2" || material.name.Contains("Forged_Wheel") || material.name.Contains("Wheel_Edge");
                    if (!bodyPaint && !wheelMetal) continue;
                    renderer.GetPropertyBlock(_paintBlock, slot);
                    var color = wheelMetal ? new Color(0.10f, 0.12f, 0.14f) : PaintColors[index];
                    if (material.name.Contains("Shadow")) color *= 0.7f;
                    color.a = 1f;
                    _paintBlock.SetColor("_Color", color);
                    _paintBlock.SetFloat("_Metallic", wheelMetal ? 0.8f : 0.55f);
                    _paintBlock.SetFloat("_Glossiness", wheelMetal ? 0.55f : 0.72f);
                    renderer.SetPropertyBlock(_paintBlock, slot);
                    _paintBlock.Clear();
                }
            }
        }
        public void RestartRun()
        {
            _retryImmediately = true;
            ReloadScene();
        }
        public void ReturnToShowroom()
        {
            _retryImmediately = false;
            ReloadScene();
        }
        private void ReloadScene()
        {
            ReleaseInput();
            scoring.SetSimulationPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        private void ReleaseInput()
        {
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
