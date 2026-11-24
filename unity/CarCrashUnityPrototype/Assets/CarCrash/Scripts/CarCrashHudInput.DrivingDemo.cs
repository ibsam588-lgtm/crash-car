using UnityEngine;

namespace CarCrash
{
    public sealed partial class CarCrashHudInput
    {
        public const string DrivingDemoSeenKey = "CarCrash.DrivingDemoSeen";
        public bool DrivingDemoActive { get; private set; }
        public bool DrivingDemoOffer { get; private set; }

        private static readonly ImpactCourse DemoCourse = new(
            "driving-demo", "Driving demo", "Accelerate, follow the turn arrows, and crash through the marked props.",
            new Vector3(2.3f, .22f, -91f), 0f, 0, 0, 0, 120,
            new[]
            {
                new ImpactGate("Practice right turn", 27f, -60.3f, 90f, 12f, new Vector3(2.3f, .22f, -60.3f)),
                new ImpactGate("Practice finish", 56.3f, -32f, 0f, 13f, new Vector3(56.3f, .22f, -60.3f))
            },
            new[]
            {
                new ImpactTargetSpec(ImpactTargetKind.Cone, 2.3f, -81f),
                new ImpactTargetSpec(ImpactTargetKind.Cone, 2.3f, -77f),
                new ImpactTargetSpec(ImpactTargetKind.Crate, 16f, -60.3f),
                new ImpactTargetSpec(ImpactTargetKind.Cone, 30f, -60.3f),
                new ImpactTargetSpec(ImpactTargetKind.Cone, 56.3f, -44f),
                new ImpactTargetSpec(ImpactTargetKind.Crate, 56.3f, -39f)
            });

        private void BuildDrivingDemo()
        {
            DrivingDemoOffer = Phase == RunPhase.Home && PlayerPrefs.GetInt(DrivingDemoSeenKey, 0) == 0;
            gameObject.AddComponent<RoadTurnPointers>().Initialize(this);
        }

        public void DismissDrivingDemo()
        {
            if (!DrivingDemoOffer) return;
            MarkDrivingDemoSeen();
            Hud.RefreshPhase();
        }

        public void StartDrivingDemo()
        {
            if (Phase != RunPhase.Home) return;
            MarkDrivingDemoSeen();
            Hud.ShowHelp(false);
            Hud.ShowSettings(false);
            DrivingDemoActive = true;
            Mode = DriveMode.ImpactRun;
            Phase = RunPhase.Briefing;
            BeginRun();
        }

        private void MarkDrivingDemoSeen()
        {
            DrivingDemoOffer = false;
            PlayerPrefs.SetInt(DrivingDemoSeenKey, 1);
            PlayerPrefs.Save();
        }

        private bool HandleDrivingDemoShortcuts()
        {
            if (DrivingDemoOffer && Phase == RunPhase.Home)
            {
                if (Input.GetKeyDown(KeyCode.Return)) StartDrivingDemo();
                else if (Input.GetKeyDown(KeyCode.Escape)) DismissDrivingDemo();
                return true;
            }
            if (!DrivingDemoActive || Phase != RunPhase.Results) return false;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)) ReturnHome();
            return true;
        }

        public string DrivingDemoHint
        {
            get
            {
                if (Phase == RunPhase.Countdown || ElapsedSeconds < 3f && vehicle.SpeedKmh < 4f)
                    return Application.isMobilePlatform ? "Hold Gas to move" : "Hold Gas or W to move";
                if (Mission.PassedCheckpoints == 0)
                    return "Slow down and follow the amber arrow right";
                return "Turn left, hit the marked props, and cross the finish";
            }
        }
    }
}
