using System;

namespace CarCrash
{
    public sealed partial class CarCrashHudInput
    {
        private static int _selectedTrial;
        private static int _dailyTrialIndex;
        private static string _dailyTrialDay;

        public bool DailyTrial => IsTrial && _dailyTrialDay != null;
        public int TrialIndex => DailyTrial ? _dailyTrialIndex : _selectedTrial;
        public float TrialBest => DailyTrial ? TimeTrialCareer.DailyBest(_dailyTrialDay) : TimeTrialCareer.Best(TrialIndex);

        public void StartTimeTrial()
        {
            if (Phase != RunPhase.Home) return;
            Mode = DriveMode.TimeTrial;
            _dailyTrialDay = null;
            ShowBriefing();
        }

        public void ToggleDailyTrial()
        {
            if (Phase != RunPhase.Briefing || !IsTrial) return;
            var today = DateTime.UtcNow;
            _dailyTrialDay = DailyTrial ? null : TimeTrialCareer.DayKey(today);
            _dailyTrialIndex = TimeTrialCareer.DailyLevel(today);
            Hud.RefreshBriefing();
        }

        private void ChangeTrial(int direction)
        {
            if (DailyTrial) _dailyTrialDay = null;
            else
            {
                var count = TimeTrialCareer.Levels.Length;
                _selectedTrial = (_selectedTrial + direction + count) % count;
            }
            Hud.RefreshBriefing();
        }
    }
}
