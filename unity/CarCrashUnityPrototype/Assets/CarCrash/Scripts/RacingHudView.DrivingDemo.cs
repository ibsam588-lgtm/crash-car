using UnityEngine;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed partial class RacingHudView
    {
        private GameObject _demoOffer, _demoResult;
        private Text _demoOutcome, _demoScore;

        private void BuildDrivingDemoHud()
        {
            var width = Mathf.Min(490f, _width - 32f);
            var height = Mathf.Min(255f, _height - 32f);
            _demoOffer = Group(_safe, "First drive");
            Dim(_demoOffer.transform);
            var panel = DemoPanel(_demoOffer.transform, width, height);
            Label(panel, "Try the driving demo", 20, 16, width - 40, 54, _portrait ? 28 : 34, White);
            Label(panel, "Practice steering, follow the road arrows, and smash a few marked props in a short run.",
                20, 79, width - 40, 70, 17, Muted);
            var buttonWidth = (width - 52f) * .5f;
            ActionButton(panel, "Skip driving demo", "Skip", 20, height - 66, buttonWidth, 46,
                _session.DismissDrivingDemo, PanelColor, White, 15);
            IconAction(panel, "Start driving demo", "Try demo", RacingHudGraphic.Symbol.Play,
                32f + buttonWidth, height - 66, buttonWidth, 46, _session.StartDrivingDemo, true);

            _demoResult = Group(_safe, "Driving demo results");
            Dim(_demoResult.transform);
            panel = DemoPanel(_demoResult.transform, width, height);
            _demoOutcome = Label(panel, "Demo complete", 20, 16, width - 40, 54, _portrait ? 28 : 34, White);
            _demoScore = Label(panel, "", 20, 79, width - 40, 70, 17, Muted);
            IconAction(panel, "Finish driving demo", "Go to home", RacingHudGraphic.Symbol.ArrowRight,
                20, height - 66, width - 40, 46, _session.ReturnHome, true);
        }

        private Transform DemoPanel(Transform parent, float width, float height) =>
            Shape(parent, "Driving demo card", (_width - width) * .5f, (_height - height) * .5f,
                width, height, RacingHudGraphic.Shape.Panel, PanelColor).transform;

        private void RefreshDrivingDemoHud()
        {
            _demoOffer.SetActive(_session.DrivingDemoOffer && _session.Phase == CarCrashHudInput.RunPhase.Home);
            _demoResult.SetActive(_session.DrivingDemoActive && _session.Phase == CarCrashHudInput.RunPhase.Results);
            if (!_demoResult.activeSelf) return;
            SetText(_demoOutcome, _session.Mission.Completed ? "Demo complete" : "Demo ended");
            SetText(_demoScore, $"{_session.Mission.Hits} practice hits · {_session.Mission.Score:N0} points\nReplay anytime from Settings.");
        }
    }
}
