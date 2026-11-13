using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed class RacingHudView : MonoBehaviour
    {
        private static readonly Color Lime = new(0.80f, 1f, 0.26f);
        private static readonly Color White = new(0.95f, 0.97f, 0.96f);
        private static readonly Color Muted = new(0.63f, 0.71f, 0.74f);
        private static readonly Color PanelColor = new(0.025f, 0.043f, 0.055f, 0.87f);
        private CarCrashHudInput _session;
        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _safe;
        private Font _font;
        private MovingTrafficObstacle[] _traffic;
        private Rect _lastSafeArea;
        private Vector2Int _lastSize;
        private bool _portrait;
        private float _width;
        private float _height;
        private float _displaySpeed;
        private float _displayScore;
        private GameObject _showroom, _race, _controls, _pause, _results, _countdown;
        private Text _score, _hits, _combo, _timer, _speed, _gear, _boostLabel, _integrity, _cameraLabel, _eventLabel;
        private Text _paintLabel, _countdownText, _impactTitle, _impactScore;
        private CanvasGroup _impact;
        private RacingHudGraphic _dial, _radar, _atmosphere;
        private RectTransform _boostFill, _damageFill, _rpmFill;
        private Graphic _boostButton;
        private HudHoldButton _left, _right, _gas, _brake;
        private readonly Graphic[] _swatches = new Graphic[CarCrashHudInput.PaintColors.Length];
        public bool LeftHeld => _left != null && _left.IsHeld;
        public bool RightHeld => _right != null && _right.IsHeld;
        public bool GasHeld => _gas != null && _gas.IsHeld;
        public bool BrakeHeld => _brake != null && _brake.IsHeld;

        public void Initialize(CarCrashHudInput session)
        {
            _session = session;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _traffic = FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None);
            var ui = new GameObject("Racing UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = ui.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            _scaler = ui.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.matchWidthOrHeight = 1f;
            if (EventSystem.current == null)
            {
                var events = new GameObject("Racing Input", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(ui.transform, false);
            }
            RebuildLayout();
        }

        private void Update()
        {
            if (_canvas != null && (_lastSize.x != Screen.width || _lastSize.y != Screen.height || _lastSafeArea != Screen.safeArea))
                RebuildLayout();
        }

        private void RebuildLayout()
        {
            ReleaseControls();
            if (_safe != null)
            {
                _safe.gameObject.SetActive(false);
                Destroy(_safe.gameObject);
            }
            _lastSize = new Vector2Int(Screen.width, Screen.height);
            _lastSafeArea = Screen.safeArea;
            _portrait = Screen.width < Screen.height * 1.4f;
            _scaler.referenceResolution = _portrait ? new Vector2(480f, 900f) : new Vector2(1280f, 720f);
            _safe = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            _safe.SetParent(_canvas.transform, false);
            _safe.anchorMin = new Vector2(Screen.safeArea.xMin / Screen.width, Screen.safeArea.yMin / Screen.height);
            _safe.anchorMax = new Vector2(Screen.safeArea.xMax / Screen.width, Screen.safeArea.yMax / Screen.height);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            _width = _safe.rect.width;
            _height = _safe.rect.height;

            _atmosphere = Shape(_safe, "Edge shading", 0, 0, _width, _height, RacingHudGraphic.Shape.Atmosphere,
                new Color(0.008f, 0.018f, 0.025f, 0.80f));
            _showroom = Group(_safe, "Live garage");
            BuildShowroom(_showroom.transform);
            _race = Group(_safe, "Driving telemetry");
            BuildRace(_race.transform);
            _countdown = Group(_safe, "Starting lights");
            _countdownText = Label(_countdown.transform, "3", _width * 0.5f - 90, _height * 0.37f - 48, 180, 140, 110, Lime, TextAnchor.MiddleCenter);
            Label(_countdown.transform, "GET READY", _width * 0.5f - 120, _height * 0.37f + 96, 240, 25, 13, White, TextAnchor.MiddleCenter);
            _pause = Group(_safe, "Pause menu");
            BuildPause(_pause.transform);
            _results = Group(_safe, "Run results");
            BuildResults(_results.transform);
            RefreshPaint();
            RefreshPhase();
        }

        private void BuildShowroom(Transform root)
        {
            var pad = _portrait ? 24f : 44f;
            Brand(root, pad, 25);
            Label(root, "DOWNTOWN STRIP", _width - pad - 175, 28, 175, 20, 12, White, TextAnchor.MiddleRight);
            Label(root, "STREET SERIES  /  01", _width - pad - 190, 49, 190, 18, 10, Muted, TextAnchor.MiddleRight);
            var titleY = _portrait ? 112f : 132f;
            Label(root, "NO RULES. JUST IMPACT.", pad, titleY - 24, 340, 20, 11, Lime);
            Label(root, "BUILT FOR", pad, titleY, _portrait ? _width - 48 : 500, 74, _portrait ? 45 : 64, White);
            Label(root, "THE IMPACT.", pad, titleY + (_portrait ? 49 : 68), _portrait ? _width - 48 : 540, 74, _portrait ? 45 : 64, Lime);
            if (!_portrait)
                Label(root, "Find the gap. Build the chain.\nLeave your mark on the city.", pad + 2, titleY + 160, 370, 62, 17, White);

            var cardY = _height - (_portrait ? 292 : 224);
            var cardWidth = _portrait ? _width - pad * 2 : 366f;
            var card = Shape(root, "Vehicle specification", pad, cardY, cardWidth, _portrait ? 142 : 156, RacingHudGraphic.Shape.Panel, PanelColor);
            Label(card.transform, "YOUR MACHINE", 18, 13, 190, 18, 10, Lime);
            Label(card.transform, "ROADBREAKER GT", 18, 37, cardWidth - 36, 28, _portrait ? 23 : 25, White);
            Label(card.transform, "SPORT COUPE  /  REAR-WHEEL DRIVE", 18, 69, cardWidth - 36, 18, 10, Muted);
            Rule(card.transform, 18, 98, cardWidth - 36);
            Label(card.transform, $"{_session.vehicle.maxSpeedKmh:0} KM/H", 18, 110, 120, 24, 16, White);
            Label(card.transform, "60 SECOND RUN", cardWidth - 165, 111, 146, 23, 12, Lime, TextAnchor.MiddleRight);

            var paintX = _portrait ? pad : _width - 324f;
            var paintY = _height - (_portrait ? 394f : 236f);
            _paintLabel = Label(root, "", paintX, paintY, 280, 19, 10, White);
            for (var i = 0; i < _swatches.Length; i++)
            {
                var index = i;
                var button = ActionButton(root, "Paint " + CarCrashHudInput.PaintNames[i], "", paintX + i * 44, paintY + 29, 34, 30,
                    () => _session.SelectPaint(index), CarCrashHudInput.PaintColors[i], White);
                _swatches[i] = button.targetGraphic;
            }
            ActionButton(root, "Orbit left", "<", _portrait ? _width - pad - 88 : _width - 145, _portrait ? _height * 0.48f : 365, 38, 38,
                () => _session.CameraRig.RotateShowcase(-32f), PanelColor, White);
            ActionButton(root, "Orbit right", ">", _portrait ? _width - pad - 40 : _width - 99, _portrait ? _height * 0.48f : 365, 38, 38,
                () => _session.CameraRig.RotateShowcase(32f), PanelColor, White);
            Label(root, "DRAG TO EXPLORE", _portrait ? pad : _width - 324, _portrait ? _height * 0.49f : 412, 210, 20, 10, Muted);

            var startX = _portrait ? pad : _width - 324f;
            var startY = _height - (_portrait ? 126f : 117f);
            ActionButton(root, "Start engine", "START ENGINE    >", startX, startY, _portrait ? _width - pad * 2 : 280, 61,
                _session.BeginRun, Lime, new Color(0.06f, 0.09f, 0.02f), 20);
            Label(root, "IMPACT RUN  /  TRAFFIC CHAINS", startX, startY + 71, _portrait ? _width - 48 : 280, 18, 10, White, TextAnchor.MiddleCenter);
            if (!_portrait)
                Label(root, "W / S   DRIVE & BRAKE     A / D   STEER     B   NITRO     C   CAMERA", pad, _height - 33, 780, 18, 10, Muted);
            else
                Label(root, "HOLD THE PEDALS TO DRIVE", pad, _height - 31, _width - 48, 16, 9, Muted, TextAnchor.MiddleCenter);
            if (_session.BestScore > 0)
                Label(root, $"PERSONAL BEST   {_session.BestScore:N0}", pad, cardY - 30, 280, 20, 11, White);
        }

        private void BuildRace(Transform root)
        {
            var pad = _portrait ? 22f : 32f;
            Label(root, "IMPACT RUN", pad, 22, 220, 20, 12, Lime);
            Label(root, "TOTAL SCORE", pad, 51, 180, 18, 10, Muted);
            _score = Label(root, "0", pad - 1, 70, 255, 53, _portrait ? 42 : 46, White);
            _hits = Label(root, "00 HITS", pad, 126, 110, 22, 12, White);
            _combo = Label(root, "x1.0", pad + 112, 122, 100, 28, 20, Lime);
            var right = _width - pad;
            Label(root, "TIME LEFT", right - 190, 22, 105, 18, 9, Muted, TextAnchor.MiddleRight);
            _timer = Label(root, "01:00", right - 190, 45, 105, 35, 27, White, TextAnchor.MiddleRight);
            ActionButton(root, "Pause", "II", right - 57, 23, 57, 51, _session.PauseRun, PanelColor, White, 22);
            _cameraLabel = ActionButton(root, "Change camera", "CAM / CHASE", right - 157, 90, 157, 35, _session.CycleCamera, PanelColor, White, 10)
                .GetComponentInChildren<Text>();
            if (!_portrait)
            {
                Label(root, "DOWNTOWN STRIP", _width * 0.5f - 160, 25, 320, 25, 13, White, TextAnchor.MiddleCenter);
                Label(root, "S T R E E T   /   S E R I E S", _width * 0.5f - 160, 52, 320, 18, 9, Muted, TextAnchor.MiddleCenter);
            }

            var radarY = _height - (_portrait ? 382f : 314f);
            Label(root, "TRAFFIC SCAN / 70 M", pad, radarY - 25, 175, 18, 9, Muted);
            _radar = Shape(root, "Live traffic radar", pad, radarY, _portrait ? 130 : 156, _portrait ? 140 : 158,
                RacingHudGraphic.Shape.Radar, new Color(0.03f, 0.06f, 0.075f, 0.75f));
            _radar.player = _session.vehicle.transform;
            _radar.traffic = _traffic;

            var dialSize = _portrait ? 178f : 222f;
            var dialX = _width - pad - dialSize;
            var dialY = _height - (_portrait ? 352f : 268f);
            _dial = Shape(root, "Analog speedometer", dialX, dialY, dialSize, dialSize, RacingHudGraphic.Shape.Gauge, White);
            for (var i = 0; i <= 8; i++)
            {
                var a = (225f - i * 33.75f) * Mathf.Deg2Rad;
                var radius = dialSize * 0.30f;
                Label(root, (i * 30).ToString(), dialX + dialSize * 0.5f + Mathf.Cos(a) * radius - 15,
                    dialY + dialSize * 0.5f - Mathf.Sin(a) * radius - 9, 30, 18, _portrait ? 8 : 10, Muted, TextAnchor.MiddleCenter);
            }
            _speed = Label(root, "000", dialX + dialSize * 0.2f, dialY + dialSize * 0.52f, dialSize * 0.6f, 51,
                _portrait ? 34 : 42, White, TextAnchor.MiddleCenter);
            Label(root, "KM/H", dialX, dialY + dialSize * 0.76f, dialSize, 18, 9, Muted, TextAnchor.MiddleCenter);
            _gear = Label(root, "N", dialX + dialSize * 0.5f - 16, dialY + dialSize * 0.86f, 32, 25, 18, Lime, TextAnchor.MiddleCenter);
            Label(root, "AUTO", dialX + dialSize * 0.5f - 55, dialY + dialSize * 0.88f, 32, 18, 8, Muted, TextAnchor.MiddleRight);
            _rpmFill = Meter(root, "Engine revs", dialX + 22, dialY - 10, dialSize - 44, 3, Lime);

            var integrityY = _portrait ? radarY + 151 : radarY + 173;
            _integrity = Label(root, "INTEGRITY 100%", pad, integrityY, 180, 18, 9, White);
            _damageFill = Meter(root, "Body integrity", pad, integrityY + 25, _portrait ? 130 : 156, 4, Lime);

            _controls = Group(root, "Touch driving controls");
            var c = _controls.transform;
            var controlY = _height - (_portrait ? 91f : 115f);
            _left = HoldButton(c, "Steer left", "<", pad, controlY, 65, 65, PanelColor);
            _right = HoldButton(c, "Steer right", ">", pad + 76, controlY, 65, 65, PanelColor);
            if (_portrait)
            {
                _brake = HoldButton(c, "Brake pedal", "BRAKE", _width - pad - 141, controlY + 6, 61, 59, PanelColor, 11);
                _gas = HoldButton(c, "Accelerator pedal", "GAS", _width - pad - 68, controlY - 12, 68, 77, new Color(0.12f, 0.17f, 0.07f, 0.94f), 14);
            }
            else
            {
                _brake = HoldButton(c, "Brake pedal", "BRAKE", dialX - 169, controlY + 1, 64, 64, PanelColor, 11);
                _gas = HoldButton(c, "Accelerator pedal", "GAS", dialX - 92, controlY - 20, 76, 85, new Color(0.12f, 0.17f, 0.07f, 0.94f), 14);
            }
            var boostX = _portrait ? pad : _width * 0.43f - 70;
            var boostY = _height - (_portrait ? 170f : 116f);
            var boost = ActionButton(c, "Nitro boost", "NITRO  >", boostX, boostY, _portrait ? 128 : 140, 57,
                _session.ActivateBoost, new Color(0.08f, 0.16f, 0.19f, 0.94f), new Color(0.40f, 0.87f, 1f), 15);
            _boostButton = boost.targetGraphic;
            _boostLabel = Label(c, "NITRO READY", boostX - 15, boostY - 27, _portrait ? 158 : 170, 18, 10, White, TextAnchor.MiddleCenter);
            _boostFill = Meter(c, "Nitro charge", boostX, boostY + 66, _portrait ? 128 : 140, 4, new Color(0.35f, 0.85f, 1f));
            Label(c, _portrait ? "HOLD TO STEER" : "A / D  STEER", pad, controlY + 75, 160, 18, 9, Muted);
            if (!_portrait)
                Label(c, "W / S   PEDALS       B   NITRO       C   CAMERA       ESC   PAUSE", _width * 0.5f - 280, _height - 24, 560, 16, 9, Muted, TextAnchor.MiddleCenter);

            _eventLabel = Label(root, "FIND YOUR LINE", _width * 0.5f - 150, _height - (_portrait ? 436 : 57), 300, 21, 10, White, TextAnchor.MiddleCenter);
            var impactRoot = Group(root, "Impact feedback");
            _impact = impactRoot.AddComponent<CanvasGroup>();
            var impactY = _portrait ? 185f : 130f;
            _impactTitle = Label(impactRoot.transform, "SOLID IMPACT", _width * 0.5f - 200, impactY, 400, 35, _portrait ? 24 : 30, Lime, TextAnchor.MiddleCenter);
            _impactScore = Label(impactRoot.transform, "+0", _width * 0.5f - 200, impactY + 39, 400, 42, 32, White, TextAnchor.MiddleCenter);
        }

        private void BuildPause(Transform root)
        {
            Dim(root);
            var w = Mathf.Min(_width - 40, 420f);
            var panel = Shape(root, "Pause panel", (_width - w) * 0.5f, (_height - 360) * 0.5f, w, 360, RacingHudGraphic.Shape.Panel, new Color(0.025f, 0.043f, 0.055f, 0.98f));
            Label(panel.transform, "PIT STOP", 28, 26, w - 56, 20, 10, Lime);
            Label(panel.transform, "TAKE A BREATHER.", 28, 61, w - 56, 45, 29, White);
            Label(panel.transform, "Your run is paused. The street can wait.", 28, 112, w - 56, 42, 14, Muted);
            ActionButton(panel.transform, "Resume driving", "BACK TO THE STREET   >", 28, 182, w - 56, 55, _session.ResumeRun, Lime, new Color(0.06f, 0.09f, 0.02f), 15);
            ActionButton(panel.transform, "Restart driving", "RESTART RUN", 28, 252, (w - 68) * 0.5f, 47, _session.RestartRun, new Color(0.12f, 0.17f, 0.19f), White, 11);
            ActionButton(panel.transform, "Return to garage", "GARAGE", 40 + (w - 68) * 0.5f, 252, (w - 68) * 0.5f, 47, _session.ReturnToShowroom, new Color(0.12f, 0.17f, 0.19f), White, 11);
            Label(panel.transform, "ESC / P  TO RESUME", 28, 320, w - 56, 18, 9, Muted, TextAnchor.MiddleCenter);
        }

        private void BuildResults(Transform root)
        {
            Dim(root);
            var w = Mathf.Min(_width - 40, 460f);
            var panel = Shape(root, "Results panel", (_width - w) * 0.5f, (_height - 490) * 0.5f, w, 490, RacingHudGraphic.Shape.Panel, new Color(0.025f, 0.043f, 0.055f, 0.98f));
            Label(panel.transform, "RUN COMPLETE", 28, 26, w - 56, 21, 10, Lime).name = "Result status";
            Label(panel.transform, "LEFT YOUR MARK.", 28, 61, w - 56, 40, 29, White);
            Label(panel.transform, "TOTAL IMPACT SCORE", 28, 127, w - 56, 18, 10, Muted);
            Label(panel.transform, "0", 25, 151, w - 50, 65, 52, White).name = "Final score";
            Rule(panel.transform, 28, 235, w - 56);
            Label(panel.transform, "VEHICLES HIT", 28, 254, (w - 56) / 3, 20, 9, Muted);
            Label(panel.transform, "PEAK CHAIN", 28 + (w - 56) / 3, 254, (w - 56) / 3, 20, 9, Muted);
            Label(panel.transform, "TOP KM/H", 28 + (w - 56) * 2 / 3, 254, (w - 56) / 3, 20, 9, Muted);
            Label(panel.transform, "0", 28, 282, (w - 56) / 3, 34, 27, White).name = "Final hits";
            Label(panel.transform, "x1", 28 + (w - 56) / 3, 282, (w - 56) / 3, 34, 27, Lime).name = "Final chain";
            Label(panel.transform, "0", 28 + (w - 56) * 2 / 3, 282, (w - 56) / 3, 34, 27, White).name = "Final speed";
            ActionButton(panel.transform, "Retry run", "ONE MORE RUN   >", 28, 349, w - 56, 57, _session.RestartRun, Lime, new Color(0.06f, 0.09f, 0.02f), 17);
            ActionButton(panel.transform, "Garage after run", "BACK TO GARAGE", 28, 422, w - 56, 42, _session.ReturnToShowroom, new Color(0.12f, 0.17f, 0.19f), White, 11);
        }

        public void RefreshPhase()
        {
            ReleaseControls();
            var phase = _session.Phase;
            _atmosphere.showroom = phase == CarCrashHudInput.RunPhase.Showroom;
            _showroom.SetActive(phase == CarCrashHudInput.RunPhase.Showroom);
            _race.SetActive(phase != CarCrashHudInput.RunPhase.Showroom);
            _controls.SetActive(phase == CarCrashHudInput.RunPhase.Driving);
            _countdown.SetActive(phase == CarCrashHudInput.RunPhase.Countdown);
            _pause.SetActive(phase == CarCrashHudInput.RunPhase.Paused);
            _results.SetActive(phase == CarCrashHudInput.RunPhase.Results);
            if (phase == CarCrashHudInput.RunPhase.Results)
            {
                ResultText("Result status", _session.NewBest ? "NEW PERSONAL BEST" : "RUN COMPLETE");
                ResultText("Final score", _session.scoring.Score.ToString("N0"));
                ResultText("Final hits", _session.scoring.Hits.ToString("00"));
                ResultText("Final chain", "x" + (_session.scoring.PeakChainDepth + 1));
                ResultText("Final speed", _session.PeakSpeed.ToString("0"));
            }
        }

        private void ResultText(string name, string value)
        {
            foreach (var text in _results.GetComponentsInChildren<Text>(true))
                if (text.name == name) text.text = value;
        }

        public void RefreshPaint()
        {
            _paintLabel.text = "FINISH / " + CarCrashHudInput.PaintNames[_session.PaintIndex];
            for (var i = 0; i < _swatches.Length; i++)
            {
                var outline = _swatches[i].GetComponent<Outline>();
                if (outline == null) outline = _swatches[i].gameObject.AddComponent<Outline>();
                outline.effectColor = i == _session.PaintIndex ? White : new Color(0f, 0f, 0f, 0.4f);
                outline.effectDistance = i == _session.PaintIndex ? new Vector2(2, -2) : Vector2.one;
            }
        }

        public void RefreshTelemetry()
        {
            var car = _session.vehicle;
            var score = _session.scoring;
            var dt = Time.unscaledDeltaTime;
            _displaySpeed = Mathf.Lerp(_displaySpeed, car.SpeedKmh, 1f - Mathf.Exp(-10f * dt));
            _displayScore = Mathf.MoveTowards(_displayScore, score.Score, Mathf.Max(500f, Mathf.Abs(score.Score - _displayScore) * 12f) * dt);
            SetText(_score, Mathf.RoundToInt(_displayScore).ToString("N0"));
            SetText(_hits, $"{score.Hits:00} HITS");
            SetText(_combo, $"x{score.LastMultiplier:0.0}");
            var remaining = Mathf.Max(0, Mathf.CeilToInt(CarCrashHudInput.RunDuration - _session.ElapsedSeconds));
            SetText(_timer, $"{remaining / 60:00}:{remaining % 60:00}");
            _timer.color = remaining <= 10 ? new Color(1f, 0.36f, 0.22f) : White;
            SetText(_speed, Mathf.RoundToInt(_displaySpeed).ToString("000"));
            SetText(_gear, car.SpeedKmh < 1f ? "N" : Mathf.Clamp(Mathf.FloorToInt(car.SpeedKmh / 38f) + 1, 1, 6).ToString());
            SetText(_cameraLabel, "CAM / " + _session.CameraRig.View.ToString().ToUpperInvariant());
            SetText(_boostLabel, car.BoostActive ? "NITRO ACTIVE" : car.BoostCharge >= 0.35f ? $"NITRO {car.BoostCharge * 100f:0}%" : "RECHARGING");
            SetText(_integrity, $"INTEGRITY {100f - score.Damage:0}%");
            SetFill(_boostFill, car.BoostCharge);
            SetFill(_damageFill, 1f - score.Damage / 100f);
            _damageFill.GetComponent<Image>().color = score.Damage > 65f ? new Color(1f, 0.3f, 0.18f) : Lime;
            var revs = car.SpeedKmh < 1 ? 0.08f : 0.2f + Mathf.Repeat(car.SpeedKmh / 38f, 1f) * 0.7f;
            SetFill(_rpmFill, revs);
            _dial.value = _displaySpeed / 240f;
            _dial.boosting = car.BoostActive;
            _dial.SetVerticesDirty();
            if (_race.activeSelf) _radar.SetVerticesDirty();
            _atmosphere.value = car.SpeedKmh / 240f;
            _atmosphere.boosting = car.BoostActive;
            _atmosphere.SetVerticesDirty();
            _boostButton.color = car.BoostActive ? new Color(0.1f, 0.45f, 0.58f, 0.95f) : new Color(0.08f, 0.16f, 0.19f, 0.94f);
            SetText(_eventLabel, car.BoostActive ? "N I T R O   A C T I V E" : score.SlowMotionActive ? "I M P A C T   C A M" : "FIND YOUR LINE");

            var sinceImpact = Time.realtimeSinceStartup - score.LastImpactRealtime;
            _impact.alpha = _session.Phase == CarCrashHudInput.RunPhase.Driving && sinceImpact < 2.2f
                ? Mathf.Clamp01((2.2f - sinceImpact) * 2f) : 0f;
            if (_impact.alpha > 0)
            {
                SetText(_impactTitle, score.LastChainDepth > 0 ? $"CHAIN REACTION  x{score.LastChainDepth + 1}" : "SOLID IMPACT");
                SetText(_impactScore, $"+{score.LastImpactScore:N0}");
                _impact.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, 0.16f - sinceImpact) * 0.8f);
            }
            if (_countdown.activeSelf)
            {
                SetText(_countdownText, Mathf.Max(1, Mathf.CeilToInt(_session.CountdownRemaining)).ToString());
                var fraction = Mathf.Repeat(_session.CountdownRemaining, 1f);
                _countdownText.transform.localScale = Vector3.one * (1f + fraction * 0.12f);
            }
        }

        public void ReleaseControls()
        {
            _left?.Release(); _right?.Release(); _gas?.Release(); _brake?.Release();
        }

        private void Brand(Transform root, float x, float y)
        {
            Solid(root, "Brand stroke", x, y + 3, 5, 31, Lime);
            Solid(root, "Brand stroke 2", x + 9, y + 3, 5, 23, Lime);
            Label(root, "CRASH CAR", x + 27, y - 2, 230, 28, 21, White);
            Label(root, "S T R E E T   /   I M P A C T", x + 28, y + 27, 260, 17, 8, Muted);
        }

        private Text Label(Transform parent, string value, float x, float y, float width, float height, int size,
            Color tint, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var rect = Rect(parent, value, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = tint;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = value;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            shadow.effectDistance = new Vector2(0, -1);
            return text;
        }

        private RacingHudGraphic Shape(Transform parent, string name, float x, float y, float width, float height,
            RacingHudGraphic.Shape shape, Color tint)
        {
            var graphic = Rect(parent, name, x, y, width, height).gameObject.AddComponent<RacingHudGraphic>();
            graphic.shape = shape;
            graphic.color = tint;
            graphic.raycastTarget = false;
            return graphic;
        }

        private Button ActionButton(Transform parent, string name, string label, float x, float y, float width, float height,
            UnityAction action, Color background, Color foreground, int fontSize = 16)
        {
            var graphic = Shape(parent, name, x, y, width, height, RacingHudGraphic.Shape.Panel, background);
            graphic.raycastTarget = true;
            var button = graphic.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f);
            colors.pressedColor = new Color(0.70f, 0.80f, 0.65f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.onClick.AddListener(action);
            Label(graphic.transform, label, 7, 0, width - 14, height, fontSize, foreground, TextAnchor.MiddleCenter);
            return button;
        }

        private HudHoldButton HoldButton(Transform parent, string name, string label, float x, float y, float width, float height,
            Color background, int size = 28)
        {
            var graphic = Shape(parent, name, x, y, width, height, RacingHudGraphic.Shape.Panel, background);
            graphic.raycastTarget = true;
            var hold = graphic.gameObject.AddComponent<HudHoldButton>();
            Label(graphic.transform, label, 2, 0, width - 4, height - 4, size, White, TextAnchor.MiddleCenter);
            if (name.Contains("pedal"))
            {
                for (var i = 0; i < 3; i++) Solid(graphic.transform, "Pedal grip", 10, height - 15 + i * 3, width - 20, 1, new Color(1f, 1f, 1f, 0.18f));
            }
            return hold;
        }

        private RectTransform Meter(Transform parent, string name, float x, float y, float width, float height, Color tint)
        {
            var track = Solid(parent, name, x, y, width, height, new Color(0.7f, 0.85f, 0.9f, 0.18f));
            var fill = Solid(track, "Fill", 0, 0, width, height, tint);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            return fill;
        }

        private static void SetFill(RectTransform rect, float amount) => rect.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
        private static void SetText(Text text, string value) { if (text.text != value) text.text = value; }
        private void Rule(Transform parent, float x, float y, float width) => Solid(parent, "Divider", x, y, width, 1, new Color(0.8f, 0.9f, 1f, 0.15f));
        private RectTransform Solid(Transform parent, string name, float x, float y, float width, float height, Color tint)
        {
            var rect = Rect(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            return rect;
        }
        private void Dim(Transform parent)
        {
            Solid(parent, "Modal backdrop", 0, 0, _width, _height, new Color(0f, 0.012f, 0.019f, 0.72f)).GetComponent<Image>().raycastTarget = true;
        }
        private static GameObject Group(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect.gameObject;
        }
        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }
    }
}
