using UnityEngine;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed partial class RacingHudView
    {
        private Text _courseMode, _courseInstruction, _coursePointsHelp;
        private Button _dailyTrialButton;
        private GameObject _timeTrialResults;
        private Text _trialResultName, _trialResultMedal, _trialResultTime, _trialResultBest, _trialResultCheckpoints, _trialNext;

        private void RefreshTrialBriefing(ImpactCourse course)
        {
            var daily = _session.DailyTrial;
            var best = _session.TrialBest;
            SetText(_courseName, course.Name);
            SetText(_courseIndex, daily ? "Daily" : $"{_session.TrialIndex + 1} / {TimeTrialCareer.Levels.Length}");
            SetText(_courseBrief, daily ? "One time trial each day. Today's best is saved separately." : course.Brief);
            SetText(_courseMedals, $"Gold {course.Gold}s  /  Silver {course.Silver}s  /  Bronze {course.Bronze}s");
            SetText(_dailyTrialButton.GetComponentInChildren<Text>(), daily ? "Levels" : "Daily trial");
            var car = _session.Garage?.Current;
            SetText(_courseRide, "Your car  /  " + (car == null ? "Roadbreaker GT" : TitleCase(car.displayName)));
            var prior = _session.TrialIndex > 0 ? TimeTrialCareer.Levels[_session.TrialIndex - 1].Name : "";
            SetText(_courseStatus, _session.CourseUnlocked
                ? $"{course.Gates.Length - 1} checkpoints  /  " + (best > 0f ? "Best " + TimeTrialCareer.Format(best) : "No time set")
                : $"Finish {prior} to unlock");
            _startMission.interactable = _session.CourseUnlocked;
            _coursePreview.previewCourse = course;
            _coursePreview.SetVerticesDirty();
        }

        private void BuildTrialResults(Transform root)
        {
            Dim(root);
            var w = Mathf.Min(_width - 28f, 540f);
            var h = Mathf.Min(_height - 28f, 392f);
            const float p = 22f;
            var panel = Shape(root, "Race result", (_width - w) / 2f, (_height - h) / 2f, w, h,
                RacingHudGraphic.Shape.Panel, PanelColor).transform;
            _trialResultName = Label(panel, "", p, 15, w - 2f * p, 33, 22, White);
            _trialResultMedal = Label(panel, "", p, 53, w - 2f * p, 27, 15, Accent);
            _trialResultTime = Label(panel, "", p, 82, w - 2f * p, 64, 43, White);
            _trialResultBest = Label(panel, "", p, 152, w - 2f * p, 27, 16, White);
            _trialResultCheckpoints = Label(panel, "", p, 185, w - 2f * p, 27, 14, Muted);
            var half = (w - 2f * p - 8f) / 2f;
            ActionButton(panel, "Retry race", "Retry", p, h - 109, half, 45, _session.RestartRun, Primary, Ink, 15);
            _trialNext = ActionButton(panel, "Race levels", "Race levels", p + half + 8f, h - 109, half, 45,
                _session.NextCourse, TileColor, White, 15).GetComponentInChildren<Text>();
            ActionButton(panel, "Race results home", "Home", p, h - 56, w - 2f * p, 42, _session.ReturnHome, TileColor, White, 15);
        }

        private void RefreshTrialResults()
        {
            var mission = _session.Mission;
            var course = mission.Course;
            SetText(_trialResultName, course.Name + (_session.DailyTrial ? "  /  Daily" : ""));
            var medal = mission.Completed ? TimeTrialCareer.Medal(course, _session.ElapsedSeconds) : "";
            SetText(_trialResultMedal, mission.Completed ? medal + (_session.NewBest ? "  /  New personal best" : "  /  Race complete") :
                mission.TimedOut ? "Race timed out" : "Race ended");
            SetText(_trialResultTime, TimeTrialCareer.Format(_session.ElapsedSeconds));
            SetText(_trialResultBest, _session.TrialBest > 0f ? "Best " + TimeTrialCareer.Format(_session.TrialBest) : "Finish to set a time");
            SetText(_trialResultCheckpoints, $"{mission.PassedCheckpoints}/{mission.CheckpointCount} checkpoints cleared");
            SetText(_trialNext, mission.Completed && _session.NextCourseUnlocked ? "Next level" : "Race levels");
        }
    }
}
