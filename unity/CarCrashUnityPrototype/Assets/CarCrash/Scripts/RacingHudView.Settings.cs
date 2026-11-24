using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed partial class RacingHudView
    {
        private GameObject _settings;
        private Button _motionSetting, _centerSetting, _demoSetting;
        private Text _motionState, _musicState, _effectsState, _nightState, _demoState, _settingsHint;

        public bool SettingsVisible => _settings != null && _settings.activeSelf;

        public void ShowSettings(bool visible)
        {
            if (_settings == null) return;
            if (visible && (_session.MapVisible || _session.Phase is not (CarCrashHudInput.RunPhase.Home or CarCrashHudInput.RunPhase.Paused)))
                return;
            if (visible)
            {
                ShowHelp(false);
                ReleaseControls();
                RefreshSettings();
            }
            _settings.SetActive(visible);
        }

        private void BuildSettings(Transform root)
        {
            Dim(root);
            var width = Mathf.Min(_width - 30f, _portrait ? 450f : 690f);
            var columns = !_portrait && width > 555f ? 2 : 1;
            const float pad = 20f, gap = 9f;
            const int optionCount = 6;
            var rows = optionCount / columns;
            var header = Mathf.Max(62f, Units(52f));
            var footer = Mathf.Max(110f, Units(90f));
            var rowHeight = Mathf.Max(58f, Units(44f));
            var optionsHeight = rows * rowHeight + (rows - 1) * gap;
            var height = Mathf.Min(_height - 26f, header + optionsHeight + footer);
            var columnWidth = (width - 2f * pad - (columns - 1) * gap) / columns;
            var panel = Shape(root,"Settings panel",(_width - width)*.5f,(_height - height)*.5f,width,height,
                RacingHudGraphic.Shape.Panel,PanelColor);
            Label(panel.transform,"Settings",pad,12f,width - 2f*pad,39f,29,White);
            var optionsRoot = panel.transform;
            var optionsY = header;
            var viewportHeight = height - header - footer;
            if (viewportHeight < optionsHeight - .01f)
            {
                var viewport = Solid(panel.transform,"Settings options viewport",0f,header,width,Mathf.Max(16f,viewportHeight),Color.clear);
                viewport.GetComponent<Image>().raycastTarget = true;
                viewport.gameObject.AddComponent<RectMask2D>();
                var scroll = viewport.gameObject.AddComponent<ScrollRect>();
                scroll.viewport = viewport;
                scroll.content = Rect(viewport,"Settings options",0f,0f,width,optionsHeight);
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 32f;
                optionsRoot = scroll.content;
                optionsY = 0f;
            }

            Button Option(int index, string name, string title, UnityAction action, out Text state)
            {
                var x = pad + index % columns * (columnWidth + gap);
                var y = optionsY + index / columns * (rowHeight + gap);
                return SettingsOption(optionsRoot, name, title, x, y, columnWidth, rowHeight, action, out state);
            }

            _motionSetting = Option(0,"Motion steering setting","Motion steering",() =>
            {
                _session.SetMotionSteeringEnabled(!_session.MotionSteeringEnabled);
                RefreshSettings();
            },out _motionState);
            _centerSetting = Option(1,"Center tilt steering","Center tilt",() =>
            {
                _session.CenterMotionSteering();
                RefreshSettings();
            },out _);
            Option(2,"Music setting","Music",() =>
            {
                _session.Soundtrack.SetMusicEnabled(!_session.Soundtrack.MusicEnabled);
                RefreshSettings();
            },out _musicState);
            Option(3,"Sound effects setting","Sound effects",() =>
            {
                _session.Soundtrack.SetEffectsEnabled(!_session.Soundtrack.EffectsEnabled);
                RefreshSettings();
            },out _effectsState);
            Option(4,"Dark mode setting","Dark mode",() =>
            {
                CityLightingRig.SetNightEnabled(!CityLightingRig.NightEnabled);
                RefreshSettings();
            },out _nightState);
            _demoSetting = Option(5,"Replay driving demo","Driving demo",() =>
            {
                ShowSettings(false);
                _session.StartDrivingDemo();
            },out _demoState);

            var contentBottom = height - footer;
            _settingsHint = Label(panel.transform,"",pad,contentBottom+6f,width-2f*pad,footer-56f,14,Muted);
            var doneHeight = Mathf.Max(42f, Units(40f));
            var footerButtonY = height - doneHeight - 12f;
            var footerButtonWidth = (width - 2f * pad - gap) * .5f;
            ActionButton(panel.transform, "Privacy policy setting", "Privacy policy", pad, footerButtonY, footerButtonWidth, doneHeight,
                () => Application.OpenURL("https://corsairlabs.com/crash-car-elysian-privacy-policy"), TileColor, White);
            ActionButton(panel.transform, "Close settings", "Done", pad + footerButtonWidth + gap, footerButtonY, footerButtonWidth, doneHeight,
                () => ShowSettings(false), Primary, Ink);
        }

        private Button SettingsOption(Transform parent, string name, string title, float x, float y, float width, float height,
            UnityAction action, out Text state)
        {
            var button = ActionButton(parent,name,title,x,y,width,height,action,TileColor,White,16);
            var titleText = button.GetComponentInChildren<Text>();
            var stateWidth = Mathf.Max(58f, Units(47f));
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.rectTransform.anchoredPosition = new Vector2(13f,0f);
            titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-stateWidth-24f);
            state = Label(button.transform,"",width-stateWidth-9f,0f,stateWidth,height,14,Accent,TextAnchor.MiddleRight);
            state.font = _mediumFont;
            return button;
        }

        private void RefreshSettings()
        {
            var available = _session.MotionSteeringAvailable;
            var motionEnabled = _session.MotionSteeringEnabled;
            var centering = _session.MotionSteeringCalibrating;
            var home = _session.Phase == CarCrashHudInput.RunPhase.Home;
            _motionSetting.interactable = available;
            _centerSetting.interactable = motionEnabled && !centering;
            _demoSetting.interactable = home;
            SetText(_motionState, available ? motionEnabled ? "On" : "Off" : "N/A");
            SetText(_musicState, _session.Soundtrack.MusicEnabled ? "On" : "Off");
            SetText(_effectsState, _session.Soundtrack.EffectsEnabled ? "On" : "Off");
            SetText(_nightState, CityLightingRig.NightEnabled ? "On" : "Off");
            SetText(_demoState, home ? "Replay" : "Home");
            SetText(_settingsHint, centering ? "Keep the phone steady while steering centers."
                : motionEnabled ? "Hold the phone straight and tap Center tilt. The arrow buttons still steer."
                : available ? "Turn on motion steering to steer with your phone."
                : "Use the arrow buttons or a keyboard to steer on this device.");
        }
    }
}
