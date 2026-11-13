using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed class RacingHudView : MonoBehaviour
    {
        private static readonly Color Lime = new(0.92f, 0.73f, 0.33f);
        private static readonly Color White = new(0.95f, 0.97f, 0.96f);
        private static readonly Color Muted = new(0.63f, 0.71f, 0.74f);
        private static readonly Color PanelColor = new(0.025f, 0.043f, 0.055f, 0.87f);
        private CarCrashHudInput _session;
        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _safe;
        private Font _font;
        private Font _displayFont;
        private MovingTrafficObstacle[] _traffic;
        private Rect _lastSafeArea;
        private Vector2Int _lastSize;
        private bool _portrait;
        private float _width;
        private float _height;
        private float _displaySpeed;
        private float _displayScore;
        private GameObject _home, _help, _showroom, _race, _controls, _pause, _results, _countdown;
        private Text _timerCaption, _modeCaption, _runTitle, _runSubtitle;
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
        public bool HelpVisible => _help != null && _help.activeSelf;
        public void ShowHelp(bool visible) { if (_help != null) _help.SetActive(visible); }

        public void Initialize(CarCrashHudInput session)
        {
            _session = session;
            _font = Resources.Load<Font>("Fonts/BarlowCondensed-Bold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _displayFont = Resources.Load<Font>("Fonts/ArchivoBlack-Regular") ?? _font;
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
                new Color(0.008f, 0.018f, 0.025f, 0.52f));
            _home = Group(_safe, "Home screen");
            BuildHome(_home.transform);
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
            _help = Group(_safe, "Driving guide");
            BuildHelp(_help.transform);
            _help.SetActive(false);
            RefreshPaint();
            RefreshPhase();
        }

        private void BuildHome(Transform root)
        {
            var pad = _portrait ? 24f : 44f;
            Brand(root, pad, 27);
            Label(root, "ELYSIAN / OPEN STREETS", _width - pad - 230, 29, 230, 24, 12, White, TextAnchor.MiddleRight);
            ActionButton(root, "Home driving guide", "HOW TO PLAY", _width - pad - 128, 70, 128, 32,
                () => ShowHelp(true), PanelColor, White, 11);
            var titleY = _portrait ? 111f : 143f;
            Label(root, "WELCOME TO ELYSIAN", pad, titleY - 27, 380, 20, 11, Lime);
            Label(root, "THE CITY", pad, titleY, _portrait ? _width - 48 : 560, 74, _portrait ? 37 : 58, White);
            Label(root, "IS YOURS.", pad, titleY + (_portrait ? 49 : 72), _portrait ? _width - 48 : 560, 75, _portrait ? 37 : 58, White);
            if (!_portrait)
            {
                Label(root, "Take the wheel. Find your own way.", pad + 2, titleY + 165, 440, 31, 19, White);
                Label(root, "Cruise the district or chase the next high score.", pad + 2, titleY + 204, 460, 23, 14, Muted);
            }
            var y = _height - (_portrait ? 335 : 215);
            var width = _portrait ? _width - pad * 2 : (_width - pad * 2 - 28) / 3f;
            var height = _portrait ? 94f : 166f;
            HomeCard(root, "Free Drive", "FREE DRIVE", "No timer. Cruise, crash, keep driving.", "DRIVE NOW  >",
                pad, y, width, height, _session.StartFreeDrive, true);
            HomeCard(root, "Impact Run", "IMPACT RUN", "A 60 second score challenge.", "PLAY CHALLENGE  >",
                _portrait ? pad : pad + width + 14, _portrait ? y + 104 : y, width, height, _session.StartImpactRun, false);
            HomeCard(root, "Open garage", "YOUR GARAGE", "Choose your finish. Make it yours.", "ENTER GARAGE  >",
                _portrait ? pad : pad + (width + 14) * 2, _portrait ? y + 208 : y, width, height, _session.OpenGarage, false);
            Label(root, "ENTER  FREE DRIVE     G  GARAGE", pad, _height - 27, _width - pad * 2, 17, 10, Muted);
        }

        private void HomeCard(Transform parent, string name, string title, string copy, string action,
            float x, float y, float width, float height, UnityAction onClick, bool primary)
        {
            var panel = Shape(parent, name + " card", x, y, width, height, RacingHudGraphic.Shape.Panel,
                new Color(0.018f, 0.028f, 0.035f, 0.94f));
            Solid(panel.transform, "Card accent", 0, 0, 3, height, primary ? Lime : new Color(0.32f, 0.43f, 0.48f));
            Label(panel.transform, title, 18, _portrait ? 12 : 17, _portrait ? width - 165 : width - 36, 31, _portrait ? 21 : 25,
                primary ? Lime : White);
            Label(panel.transform, copy, 18, _portrait ? 45 : 53, _portrait ? width - 170 : width - 36, _portrait ? 36 : 35, 12, Muted);
            ActionButton(panel.transform, name, action, _portrait ? width - 151 : 18, _portrait ? 25 : 108,
                _portrait ? 135 : width - 36, _portrait ? 45 : 42, onClick,
                primary ? Lime : new Color(0.11f, 0.17f, 0.20f), primary ? new Color(0.045f, 0.06f, 0.07f) : White, 12);
        }

        private void BuildHelp(Transform root)
        {
            Dim(root);
            var width = Mathf.Min(_width - 40, 510f);
            var panel = Shape(root, "Driving guide panel", (_width - width) / 2, (_height - 510) / 2,
                width, 510, RacingHudGraphic.Shape.Panel, new Color(0.018f, 0.03f, 0.04f, 0.99f));
            Label(panel.transform, "YOUR FIRST DRIVE", 28, 24, width - 56, 24, 11, Lime);
            Label(panel.transform, "TAKE THE WHEEL.", 28, 61, width - 56, 42, 29, White);
            Label(panel.transform, "W / S     ACCELERATE / BRAKE\nA / D      STEER\nB           NITRO\nC           CHANGE CAMERA\nR           RECOVER YOUR CAR\nP / ESC   PAUSE", 28, 125, width - 56, 170, 17, White);
            Label(panel.transform, "On a phone, hold the steering and pedal buttons.\nFree Drive has no timer. Recovery keeps your session.",
                28, 314, width - 56, 68, 14, Muted);
            ActionButton(panel.transform, "Close driving guide", "GOT IT  >", 28, 420, width - 56, 54,
                () => ShowHelp(false), Lime, new Color(0.04f, 0.06f, 0.07f), 15);
        }

        private void BuildShowroom(Transform root)
        {
            var pad = _portrait ? 24f : 44f;
            Brand(root, pad, 27);
            ActionButton(root, "Garage to home", "<  HOME", _portrait ? _width - pad - 86 : _width * 0.5f - 43, 78, 86, 29,
                _session.ReturnHome, PanelColor, White, 11);
            Label(root, "ELYSIAN DISTRICT", _width - pad - 180, 27, 180, 22, 13, White, TextAnchor.MiddleRight);
            Label(root, "GOLDEN HOUR / STREET SERIES", _width - pad - 230, 51, 230, 17, 10, Muted, TextAnchor.MiddleRight);
            var titleY = _portrait ? 118f : 143f;
            Label(root, "DOWNTOWN  /  CHAPTER 01", pad, titleY - 28, 350, 18, 11, Lime);
            Label(root, "OWN THE", pad, titleY, _portrait ? _width - 48 : 510, 72, _portrait ? 36 : 55, White);
            Label(root, "STREETS.", pad, titleY + (_portrait ? 49 : 69), _portrait ? _width - 48 : 510, 75, _portrait ? 36 : 55, White);
            if (!_portrait)
            {
                Label(root, "One car. A city full of possibilities.", pad + 2, titleY + 160, 420, 30, 18, White);
                Label(root, "Thread the traffic. Build your impact chain.", pad + 2, titleY + 194, 420, 24, 14, Muted);
            }

            var footerY = _height - (_portrait ? 287f : 166f);
            Solid(root, "Garage footer", 0, footerY, _width, _height - footerY, new Color(0.018f, 0.021f, 0.025f, 0.86f));
            Rule(root, pad, footerY, _width - pad * 2);
            Label(root, "CURRENT VEHICLE", pad, footerY + 20, 250, 17, 10, Muted);
            Label(root, "ROADBREAKER GT", pad, footerY + 45, _portrait ? _width - 48 : 335, 34, 27, White);
            Label(root, $"PERFORMANCE COUPE   /   {_session.vehicle.maxSpeedKmh:0} KM/H", pad, footerY + 82, _portrait ? _width - 48 : 335, 20, 12, Muted);
            if (_session.BestScore > 0)
                Label(root, $"PERSONAL BEST  {_session.BestScore:N0}", pad, footerY - 33, 280, 22, 12, White);
            var paintX = _portrait ? pad : _width * 0.42f;
            var paintY = footerY + (_portrait ? 115 : 25);
            _paintLabel = Label(root, "", paintX, paintY, 290, 18, 10, White);
            for (var i = 0; i < _swatches.Length; i++)
            {
                var index = i;
                var button = ActionButton(root, "Paint " + CarCrashHudInput.PaintNames[i], "", paintX + i * 43, paintY + 31, 32, 27,
                    () => _session.SelectPaint(index), CarCrashHudInput.PaintColors[i], White);
                _swatches[i] = button.targetGraphic;
            }
            var orbitY = _portrait ? footerY - 89 : footerY - 84;
            ActionButton(root, "Orbit left", "<", _width - pad - 88, orbitY, 38, 38,
                () => _session.CameraRig.RotateShowcase(-32f), PanelColor, White);
            ActionButton(root, "Orbit right", ">", _width - pad - 42, orbitY, 38, 38,
                () => _session.CameraRig.RotateShowcase(32f), PanelColor, White);
            Label(root, "DRAG TO ROTATE", _portrait ? pad : _width - 325, orbitY + 9, 170, 20, 10, White);
            var startX = _portrait ? pad : _width - 324f;
            var startY = _portrait ? _height - 90f : footerY + 45f;
            ActionButton(root, "Start engine", "START FREE DRIVE   >", startX, startY, _portrait ? _width - pad * 2 : 280, 54,
                _session.StartFreeDrive, Lime, new Color(0.08f, 0.07f, 0.04f), 17);
            if (!_portrait)
                Label(root, "W / S   PEDALS     A / D   STEER     B   NITRO     C   CAMERA     P   PAUSE", pad, _height - 30, 800, 17, 11, Muted);
            else
                Label(root, "UNTIMED FREE DRIVE", pad, _height - 27, _width - 48, 17, 10, Muted, TextAnchor.MiddleCenter);
        }

        private void BuildRace(Transform root)
        {
            var pad = _portrait ? 22f : 30f;
            Label(root, "ELYSIAN", pad, 22, 200, 24, 17, White);
            _modeCaption = Label(root, "FREE DRIVE", pad, 49, 220, 18, 10, Muted);
            var right = _width - pad;
            Label(root, "IMPACT SCORE", right - 265, 20, 205, 18, 10, Muted, TextAnchor.MiddleRight);
            _score = Label(root, "0", right - 276, 36, 216, 49, _portrait ? 33 : 34, White, TextAnchor.MiddleRight);
            _hits = Label(root, "00 HITS", right - 270, 85, 130, 20, 12, White, TextAnchor.MiddleRight);
            _combo = Label(root, "x1.0", right - 125, 85, 65, 22, 15, Lime, TextAnchor.MiddleRight);
            ActionButton(root, "Pause", "II", right - 44, 22, 44, 44, _session.PauseRun, PanelColor, White, 19);
            _timerCaption = Label(root, "SESSION", right - 213, 121, 78, 17, 10, Muted, TextAnchor.MiddleRight);
            _timer = Label(root, "01:00", right - 128, 114, 68, 30, 22, White, TextAnchor.MiddleRight);
            _cameraLabel = ActionButton(root, "Change camera", "CAM / CHASE", pad, 80, 116, 29,
                _session.CycleCamera, PanelColor, White, 10).GetComponentInChildren<Text>();
            ActionButton(root, "Recover vehicle", "RECOVER  [R]", pad, 117, 116, 28,
                _session.RecoverVehicle, PanelColor, White, 10);
            if (!_portrait)
            {
                _runTitle = Label(root, "FREE DRIVE", _width * 0.5f - 170, 24, 340, 27, 15, White, TextAnchor.MiddleCenter);
                _runSubtitle = Label(root, "THE DISTRICT IS YOURS. KEEP DRIVING.", _width * 0.5f - 200, 53, 400, 18, 10, Muted, TextAnchor.MiddleCenter);
            }

            var mapWidth = _portrait ? 151f : 200f;
            var mapHeight = _portrait ? 130f : 143f;
            var mapY = _portrait ? 165f : _height - 260;
            _radar = Shape(root, "Live city minimap", pad, mapY, mapWidth, mapHeight, RacingHudGraphic.Shape.Radar,
                new Color(0.11f, 0.13f, 0.13f, 0.92f));
            _radar.player = _session.vehicle.transform;
            _radar.traffic = _traffic;
            var network = FindFirstObjectByType<CityRoadNetwork>();
            _radar.cityBuildings = network != null ? network.GetComponentsInChildren<BoxCollider>() : System.Array.Empty<BoxCollider>();
            Label(root, "N", pad + mapWidth - 20, mapY + 6, 15, 18, 11, White, TextAnchor.MiddleCenter);
            _integrity = Label(root, "VEHICLE 100%", pad, mapY + mapHeight + 8, mapWidth, 17, 10, White);
            _damageFill = Meter(root, "Body integrity", pad, mapY + mapHeight + 29, mapWidth, 5, new Color(0.42f, 0.72f, 0.40f));
            _boostLabel = Label(root, "NITRO READY", pad, mapY + mapHeight + 41, mapWidth, 17, 10, White);
            _boostFill = Meter(root, "Nitro charge", pad, mapY + mapHeight + 62, mapWidth, 4, new Color(0.36f, 0.63f, 0.79f));

            var speedWidth = _portrait ? 148f : 170f;
            var speedX = right - speedWidth;
            var speedY = _height - (_portrait ? 247f : 170f);
            _dial = Shape(root, "Digital driving instruments", speedX, speedY, speedWidth, 96, RacingHudGraphic.Shape.Panel,
                new Color(0.016f, 0.02f, 0.025f, 0.60f));
            _speed = Label(root, "000", speedX + 9, speedY + 4, speedWidth - 58, 55, _portrait ? 36 : 40, White, TextAnchor.MiddleRight);
            Label(root, "KM/H", right - 45, speedY + 34, 36, 18, 10, Muted);
            _gear = Label(root, "N", speedX + 15, speedY + 65, 25, 24, 18, White);
            Label(root, "AUTO", speedX + 43, speedY + 69, 36, 17, 9, Muted);
            _rpmFill = Meter(root, "Engine revs", speedX + 86, speedY + 75, speedWidth - 99, 3, Lime);
            if (!_portrait)
                Label(root, "Elysian Boulevard", right - 280, _height - 45, 280, 25, 19, White, TextAnchor.MiddleRight);

            _controls = Group(root, "Touch driving controls");
            var c = _controls.transform;
            var controlY = _height - (_portrait ? 91f : 103f);
            var controlX = _portrait ? pad : pad + mapWidth + 23;
            var controlSize = _portrait ? 65f : 47f;
            _left = HoldButton(c, "Steer left", "<", controlX, controlY, controlSize, controlSize, PanelColor, _portrait ? 28 : 22);
            _right = HoldButton(c, "Steer right", ">", controlX + controlSize + 10, controlY, controlSize, controlSize, PanelColor, _portrait ? 28 : 22);
            if (_portrait)
            {
                _brake = HoldButton(c, "Brake pedal", "BRAKE", right - 141, controlY + 6, 61, 59, PanelColor, 11);
                _gas = HoldButton(c, "Accelerator pedal", "GAS", right - 68, controlY - 12, 68, 77, PanelColor, 14);
            }
            else
            {
                _brake = HoldButton(c, "Brake pedal", "BRAKE", right - speedWidth - 146, controlY, 54, 47, PanelColor, 10);
                _gas = HoldButton(c, "Accelerator pedal", "GAS", right - speedWidth - 79, controlY - 7, 56, 54, PanelColor, 13);
            }
            var boostX = _portrait ? pad : _width * 0.48f - 55;
            var boostY = _height - (_portrait ? 174 : 103);
            var boost = ActionButton(c, "Nitro boost", "NITRO  [B]", boostX, boostY, _portrait ? 128 : 110, _portrait ? 54 : 47,
                _session.ActivateBoost, new Color(0.06f, 0.10f, 0.13f, 0.78f), new Color(0.58f, 0.80f, 0.91f), 13);
            _boostButton = boost.targetGraphic;
            if (!_portrait)
                Label(c, "W / S  PEDALS     A / D  STEER     B  NITRO     C  CAMERA     P  PAUSE", _width * 0.5f - 270, _height - 24, 540, 16, 10, Muted, TextAnchor.MiddleCenter);
            _eventLabel = Label(root, "", _width * 0.5f - 170, _height - (_portrait ? 433 : 188), 340, 21, 11, White, TextAnchor.MiddleCenter);
            var impactRoot = Group(root, "Impact feedback");
            _impact = impactRoot.AddComponent<CanvasGroup>();
            var impactY = _portrait ? 185f : 120f;
            _impactTitle = Label(impactRoot.transform, "SOLID IMPACT", _width * 0.5f - 200, impactY, 400, 35, _portrait ? 23 : 28, Lime, TextAnchor.MiddleCenter);
            _impactScore = Label(impactRoot.transform, "+0", _width * 0.5f - 200, impactY + 39, 400, 42, 32, White, TextAnchor.MiddleCenter);
        }
        private void BuildPause(Transform root)
        {
            Dim(root);
            var w = Mathf.Min(_width - 40, 420f);
            var panel = Shape(root, "Pause panel", (_width - w) * 0.5f, (_height - 430) * 0.5f, w, 430, RacingHudGraphic.Shape.Panel, new Color(0.025f, 0.043f, 0.055f, 0.98f));
            Label(panel.transform, "PIT STOP", 28, 26, w - 56, 20, 10, Lime);
            Label(panel.transform, "TAKE A BREATHER.", 28, 61, w - 56, 45, 29, White);
            Label(panel.transform, "Your run is paused. The street can wait.", 28, 112, w - 56, 42, 14, Muted);
            ActionButton(panel.transform, "Resume driving", "BACK TO THE STREET   >", 28, 182, w - 56, 55, _session.ResumeRun, Lime, new Color(0.06f, 0.09f, 0.02f), 15);
            ActionButton(panel.transform, "Recover and resume", "RECOVER & RESUME", 28, 252, (w - 68) * 0.5f, 47, _session.RecoverVehicle, new Color(0.12f, 0.17f, 0.19f), White, 11);
            ActionButton(panel.transform, "Return to garage", "GARAGE", 40 + (w - 68) * 0.5f, 252, (w - 68) * 0.5f, 47, _session.ReturnToShowroom, new Color(0.12f, 0.17f, 0.19f), White, 11);
            ActionButton(panel.transform, "Pause to home", "HOME", 28, 315, (w - 68) * 0.5f, 47, _session.ReturnHome, new Color(0.12f, 0.17f, 0.19f), White, 11);
            ActionButton(panel.transform, "End driving session", "END SESSION", 40 + (w - 68) * 0.5f, 315, (w - 68) * 0.5f, 47, _session.EndSession, new Color(0.12f, 0.17f, 0.19f), White, 11);
            Label(panel.transform, "ESC / P  TO RESUME", 28, 389, w - 56, 18, 9, Muted, TextAnchor.MiddleCenter);
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
            ActionButton(panel.transform, "Garage after run", "GARAGE", 28, 422, (w - 68) * 0.5f, 42, _session.ReturnToShowroom, new Color(0.12f, 0.17f, 0.19f), White, 11);
            ActionButton(panel.transform, "Results to home", "HOME", 40 + (w - 68) * 0.5f, 422, (w - 68) * 0.5f, 42, _session.ReturnHome, new Color(0.12f, 0.17f, 0.19f), White, 11);
        }

        public void RefreshPhase()
        {
            ReleaseControls();
            var phase = _session.Phase;
            var menu = phase == CarCrashHudInput.RunPhase.Home || phase == CarCrashHudInput.RunPhase.Showroom;
            _atmosphere.showroom = menu;
            _home.SetActive(phase == CarCrashHudInput.RunPhase.Home);
            _showroom.SetActive(phase == CarCrashHudInput.RunPhase.Showroom);
            _race.SetActive(!menu);
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
            var seconds = _session.IsTimed ? Mathf.Max(0, Mathf.CeilToInt(CarCrashHudInput.RunDuration - _session.ElapsedSeconds))
                : Mathf.FloorToInt(_session.ElapsedSeconds);
            SetText(_timer, $"{seconds / 60:00}:{seconds % 60:00}");
            SetText(_timerCaption, _session.IsTimed ? "TIME LEFT" : "SESSION");
            SetText(_modeCaption, _session.IsTimed ? "60 SECOND IMPACT RUN" : "FREE DRIVE / NO LIMIT");
            if (_runTitle != null)
            {
                SetText(_runTitle, _session.IsTimed ? "IMPACT RUN" : "FREE DRIVE");
                SetText(_runSubtitle, _session.IsTimed ? "BUILD THE CHAIN. LEAVE YOUR MARK." : "THE DISTRICT IS YOURS. KEEP DRIVING.");
            }
            _timer.color = _session.IsTimed && seconds <= 10 ? new Color(1f, 0.36f, 0.22f) : White;
            SetText(_speed, Mathf.RoundToInt(_displaySpeed).ToString("000"));
            SetText(_gear, car.SpeedKmh < 1f ? "N" : Mathf.Clamp(Mathf.FloorToInt(car.SpeedKmh / 38f) + 1, 1, 6).ToString());
            SetText(_cameraLabel, "CAM / " + _session.CameraRig.View.ToString().ToUpperInvariant());
            SetText(_boostLabel, car.BoostActive ? "NITRO ACTIVE" : car.BoostCharge >= 0.35f ? $"NITRO {car.BoostCharge * 100f:0}%" : "RECHARGING");
            SetText(_integrity, $"VEHICLE {100f - score.Damage:0}%");
            SetFill(_boostFill, car.BoostCharge);
            SetFill(_damageFill, 1f - score.Damage / 100f);
            _damageFill.GetComponent<Image>().color = score.Damage > 65f ? new Color(1f, 0.3f, 0.18f) : new Color(0.42f, 0.72f, 0.40f);
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
            SetText(_eventLabel, _session.RecoverySuggested ? "STUCK?  PRESS R TO RECOVER" : car.BoostActive ? "NITRO ACTIVE" : score.SlowMotionActive ? "IMPACT CAM" : "");

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
            text.font = size >= 42 ? _displayFont : _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
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
