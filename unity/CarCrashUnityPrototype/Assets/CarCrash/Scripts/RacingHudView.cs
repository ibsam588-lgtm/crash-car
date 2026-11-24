using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed partial class RacingHudView : MonoBehaviour
    {
        private static readonly Color Accent = CoastalHudTheme.Seafoam;
        private static readonly Color White = CoastalHudTheme.Cream;
        private static readonly Color Muted = CoastalHudTheme.Muted;
        private static readonly Color PanelColor = CoastalHudTheme.Panel;
        private static readonly Color Ink = CoastalHudTheme.Ink;
        private static readonly Color Primary = CoastalHudTheme.Coral;
        private static readonly Color TileColor = CoastalHudTheme.Tile;
        private static readonly Color SelectedColor = CoastalHudTheme.Selected;
        private static readonly Color BorderColor = CoastalHudTheme.Border;
        // Trim transparent source padding in the UI while retaining the exact artwork file.
        private static readonly Rect LogoRegion = new(114f / 1774f, 75f / 887f, 1542f / 1774f, 724f / 887f);
        private const float LogoAspect = 1542f / 724f;
        private CarCrashHudInput _session;
        private Texture2D _coastalLogo;
        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _safe;
        private Font _font;
        private Font _displayFont;
        private Font _mediumFont;
        private MovingTrafficObstacle[] _traffic;
        private Rect _lastSafeArea;
        private Vector2Int _lastSize;
        private bool _portrait;
        private bool _compactWide;
        private float _width;
        private float _height;
        private float _displaySpeed;
        private float _displayScore;
        private GameObject _home, _help, _briefing, _showroom, _race, _controls, _pause, _results, _missionResults, _countdown, _map, _missionMap;
        private Text _timerCaption, _modeCaption;
        private Text _score, _hits, _combo, _timer, _speed, _gear, _boostLabel, _integrity, _cameraLabel, _eventLabel;
        private Text _paintLabel, _countdownText, _impactTitle, _impactScore;
        private Text _vehicleName, _vehicleSpecs;
        private Text _courseName, _courseIndex, _courseBrief, _courseMedals, _courseRide, _courseStatus;
        private Text _missionTitle, _missionScore, _missionStatus, _missionBest, _missionSummary, _garageBack, _garageDrive;
        private string _missionResultStatus;
        private Button _startMission, _nextCourse;
        private GameObject _goldLock, _goldHint;
        private readonly Text[] _targetAmounts = new Text[ImpactTargetSpec.Points.Length];
        private RacingHudGraphic _coursePreview, _missionRoute;
        private readonly Graphic[] _vehicleCards = new Graphic[6];
        private readonly Graphic[] _destinationCards = new Graphic[6];
        private CanvasGroup _impact;
        private RacingHudGraphic _dial, _radar, _atmosphere, _worldMap;
        private Text _mapStatus;
        private GameObject _eventBacking;
        private RectTransform _boostFill, _damageFill, _rpmFill;
        private Graphic _boostButton;
        private HudHoldButton _left, _right, _gas, _brake, _reverse;
        private readonly Graphic[] _swatches = new Graphic[CarCrashHudInput.PaintColors.Length];
        public bool LeftHeld => _left != null && _left.IsHeld;
        public bool RightHeld => _right != null && _right.IsHeld;
        public bool GasHeld => _gas != null && _gas.IsHeld;
        public bool BrakeHeld => _brake != null && _brake.IsHeld;
        public bool ReverseHeld => _reverse != null && _reverse.IsHeld;
        public bool HelpVisible => _help != null && _help.activeSelf;
        public void ShowHelp(bool visible) { if (_help != null) _help.SetActive(visible); }

        public void Initialize(CarCrashHudInput session)
        {
            _session = session;
            _font = Resources.Load<Font>("Fonts/Inter-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _mediumFont = Resources.Load<Font>("Fonts/Inter-Medium") ?? _font;
            _displayFont = Resources.Load<Font>("Fonts/Inter-SemiBold") ?? _font;
            _coastalLogo = Resources.Load<Texture2D>("Brand/CoastalCircuit");
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
            if (SettingsVisible) RefreshSettings();
        }

        private void RebuildLayout()
        {
            var helpWasVisible = HelpVisible;
            var settingsWasVisible = SettingsVisible;
            ReleaseControls();
            if (_safe != null)
            {
                _safe.gameObject.SetActive(false);
                Destroy(_safe.gameObject);
            }
            _lastSize = new Vector2Int(Screen.width, Screen.height);
            _lastSafeArea = Screen.safeArea;
            _portrait = Screen.width < Screen.height * 1.4f;
            _compactWide = !_portrait && Screen.height < 540;
            _scaler.referenceResolution = _portrait ? new Vector2(480f, 900f) : new Vector2(1280f, _compactWide ? 480f : 720f);
            _safe = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            _safe.SetParent(_canvas.transform, false);
            _safe.anchorMin = new Vector2(Screen.safeArea.xMin / Screen.width, Screen.safeArea.yMin / Screen.height);
            _safe.anchorMax = new Vector2(Screen.safeArea.xMax / Screen.width, Screen.safeArea.yMax / Screen.height);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
            // The CanvasScaler may not have applied the new reference size yet after rotation.
            var scale = Screen.height / _scaler.referenceResolution.y;
            _width = _lastSafeArea.width / scale;
            _height = _lastSafeArea.height / scale;

            _atmosphere = Shape(_safe, "Edge shading", 0, 0, _width, _height, RacingHudGraphic.Shape.Atmosphere,
                new Color(.015f, .038f, .035f, .42f));
            _home = Group(_safe, "Home screen");
            BuildHome(_home.transform);
            _briefing = Group(_safe, "Mission briefing");
            BuildBriefing(_briefing.transform);
            _showroom = Group(_safe, "Live garage");
            BuildShowroom(_showroom.transform);
            _race = Group(_safe, "Driving telemetry");
            BuildRace(_race.transform);
            _countdown = Group(_safe, "Starting lights");
            _countdownText = Label(_countdown.transform, "3", _width * 0.5f - 90, _height * 0.37f - 48, 180, 140, 110, Accent, TextAnchor.MiddleCenter);
            Label(_countdown.transform, "GET READY", _width * 0.5f - 120, _height * 0.37f + 96, 240, 25, 13, White, TextAnchor.MiddleCenter);
            _pause = Group(_safe, "Pause menu");
            BuildPause(_pause.transform);
            _results = Group(_safe, "Run results");
            BuildResults(_results.transform);
            _missionResults = Group(_safe, "Impact Run results");
            BuildMissionResults(_missionResults.transform);
            _map=Group(_safe,"Explore city map");BuildCityMap(_map.transform);
            _missionMap = Group(_safe, "Mission route map");
            BuildMissionMap(_missionMap.transform);
            _help = Group(_safe, "Driving guide");
            BuildHelp(_help.transform);
            _help.SetActive(helpWasVisible);
            _settings = Group(_safe, "Driving settings");
            BuildSettings(_settings.transform);
            ShowSettings(settingsWasVisible);
            BuildDrivingDemoHud();
            RefreshPaint();
            RefreshVehicle();
            RefreshPhase();
        }

        private void BuildHome(Transform root)
        {
            var pad = _portrait ? 20f : 44f;
            var width = _width - pad * 2;
            var guideWidth = Mathf.Min(144f, (width - 10f) * .5f);
            var right = _width - pad;
            ActionButton(root, "Home settings", "Settings", right - guideWidth, 23, guideWidth, 44,
                () => ShowSettings(true), TileColor, White, 15);
            ActionButton(root, "Home driving guide", "Controls", right - guideWidth * 2 - 10, 23, guideWidth, 44,
                () => ShowHelp(true), TileColor, White, 15);

            var logoWidth = _portrait ? Mathf.Min(width, 434f) : Mathf.Min(width * .49f, 536f);
            var logoTop = _portrait ? Mathf.Max(100f, _height * .12f) : Mathf.Max(88f, _height * .15f);
            PlaceLogo(root, "Coastal Circuit home logo", _portrait ? (_width - logoWidth) * .5f : pad, logoTop, logoWidth);

            var dockHeight = _portrait ? 179f : _compactWide ? 111f : 139f;
            var dockY = _height - dockHeight - (_portrait ? 27f : 39f);
            var dock = Shape(root, "Home action dock", pad, dockY, width, dockHeight, RacingHudGraphic.Shape.Panel, PanelColor);
            dock.cornerRadius = 19f;
            if (_portrait)
            {
                IconAction(dock.transform, "Impact Run", "Impact Run", RacingHudGraphic.Symbol.Bolt,
                    14, 14, width - 28, 63, _session.StartImpactRun, true);
                var tileWidth = (width - 38f) * .5f;
                HomeCard(dock.transform, "Free Drive", "Free Drive", "Explore the city", 14, 88, tileWidth, 76,
                    _session.StartFreeDrive, RacingHudGraphic.Symbol.Map);
                HomeCard(dock.transform, "Open garage", "Garage", "Customize your car", 24 + tileWidth, 88, tileWidth, 76,
                    _session.OpenGarage, RacingHudGraphic.Symbol.Garage);
            }
            else
            {
                const float inset = 15, gap = 12;
                var tileWidth = (width - inset * 2 - gap * 2) / 3f;
                var tileHeight = dockHeight - inset * 2;
                IconAction(dock.transform, "Impact Run", "Impact Run", RacingHudGraphic.Symbol.Bolt,
                    inset, inset, tileWidth, tileHeight, _session.StartImpactRun, true);
                HomeCard(dock.transform, "Free Drive", "Free Drive", "Explore the city", inset + tileWidth + gap, inset,
                    tileWidth, tileHeight, _session.StartFreeDrive, RacingHudGraphic.Symbol.Map);
                HomeCard(dock.transform, "Open garage", "Garage", "Customize your car", inset + (tileWidth + gap) * 2, inset,
                    tileWidth, tileHeight, _session.OpenGarage, RacingHudGraphic.Symbol.Garage);
            }
            if (!Application.isMobilePlatform)
                Label(root, "Enter for Impact Run   /   G for garage", pad, _height - 27, width, 18, 12, Muted);
        }

        private void BuildBriefing(Transform root)
        {
            Dim(root);
            var w = Mathf.Min(_width - 30, 860);
            var h = Mathf.Min(_height - 30, _portrait ? 690 : 500);
            const float pad = 22f;
            var panel = Shape(root, "Course card", (_width - w) * .5f, (_height - h) * .5f, w, h, RacingHudGraphic.Shape.Panel, PanelColor);
            Label(panel.transform, "IMPACT RUN", pad, 17, w - 156, 28, 16, Accent).font = _mediumFont;
            _courseIndex = Label(panel.transform, "", w - 188, 19, 62, 42, 15, Muted, TextAnchor.MiddleCenter);
            IconAction(panel.transform, "Previous course", "", RacingHudGraphic.Symbol.ArrowLeft, w - 121, 19, 44, 42, () => _session.ChangeCourse(-1));
            IconAction(panel.transform, "Next course preview", "", RacingHudGraphic.Symbol.ArrowRight, w - 69, 19, 44, 42, () => _session.ChangeCourse(1));
            _courseName = Label(panel.transform, "", pad, 56, w - pad * 2, 54, _portrait ? 34 : 41, White);

            var infoWidth = _portrait ? w - pad * 2 : w * .51f - pad;
            var mapX = _portrait ? pad : infoWidth + 2 * pad;
            var mapY = _portrait ? 227 : 120;
            var mapWidth = _portrait ? infoWidth : w - mapX - pad;
            var mapHeight = _portrait ? Mathf.Max(95, h - 470) : h - 244;
            _coursePreview = Shape(panel.transform, "Course preview map", mapX, mapY, mapWidth, mapHeight,
                RacingHudGraphic.Shape.CoursePreview, Ink);
            _courseBrief = Label(panel.transform, "", pad, 116, infoWidth, 66, 17, Muted, TextAnchor.UpperLeft);
            Label(panel.transform, "Smash marked obstacles. Cross the finish to bank your score.", pad, _portrait ? 183 : 179,
                infoWidth, 44, 16, White, TextAnchor.UpperLeft);

            var statsY = _portrait ? mapY + mapHeight + 13 : 231;
            _courseMedals = Label(panel.transform, "", pad, statsY, infoWidth, 32, 14, Accent);
            Label(panel.transform, "Cones 10  /  Crates 25  /  Barriers 50\nStreetlights 100  /  Wrecks 200", pad, statsY + 36,
                infoWidth, 44, 13, Muted, TextAnchor.UpperLeft);
            _courseRide = Label(panel.transform, "", pad, statsY + 84, infoWidth, 24, 15, White);
            _courseStatus = Label(panel.transform, "", pad, statsY + 111, _portrait ? infoWidth : w - pad * 2,
                26, 14, Muted);

            var controlsY = h - 60;
            var buttonWidth = (w - pad * 2 - 16) / 3;
            ActionButton(panel.transform, "Briefing home", "Home", pad, controlsY, buttonWidth, 44, _session.ReturnHome, PanelColor, White);
            ActionButton(panel.transform, "Mission vehicle", "Change car", pad + buttonWidth + 8, controlsY, buttonWidth, 44, _session.OpenGarage, PanelColor, White);
            _startMission = IconAction(panel.transform, "Start mission", "Start run", RacingHudGraphic.Symbol.Play,
                pad + (buttonWidth + 8) * 2, controlsY, buttonWidth, 44, _session.BeginRun, true);
        }

        public void RefreshBriefing()
        {
            var course = _session.SelectedCourse;
            SetText(_courseName, course.Name);
            SetText(_courseIndex, $"{System.Array.IndexOf(ImpactCourse.Catalog, course) + 1} / {ImpactCourse.Catalog.Length}");
            SetText(_courseBrief, course.Brief);
            SetText(_courseMedals, $"Bronze {course.Bronze}  /  Silver {course.Silver}  /  Gold {course.Gold}");
            var car = _session.Garage != null ? _session.Garage.Current : null;
            SetText(_courseRide, "Your car  /  " + (car == null ? "Roadbreaker GT" : TitleCase(car.displayName)));
            var checks = $"{course.Gates.Length - 1} checkpoints";
            var pace = course.TimeLimit > 0 ? $"{course.TimeLimit}s limit" : "Finish-line scoring";
            var best = ImpactCareer.Best(course);
            var previous = System.Array.IndexOf(ImpactCourse.Catalog, course) - 1;
            var status = _session.CourseUnlocked ? $"{checks}  /  {pace}" + (best > 0 ? $"  /  Best {best:N0}" : "")
                : $"Finish {ImpactCourse.Catalog[previous].Name} to unlock";
            SetText(_courseStatus, status);
            _startMission.interactable = _session.CourseUnlocked;
            _coursePreview.previewCourse = course;
            _coursePreview.SetVerticesDirty();
        }

        private void HomeCard(Transform parent,string name,string title,string copy,float x,float y,float width,float height,UnityAction action,RacingHudGraphic.Symbol symbol)
        {
            var button=ActionButton(parent,name,"",x,y,width,height,action,TileColor,White);
            var iconSize=_portrait ? 0 : 24f;
            if(iconSize>0)Glyph(button.transform,symbol,17,18,iconSize,Accent);
            Label(button.transform,title,17+iconSize+(iconSize>0 ? 10 : 0),_portrait ? 13 : 17,width-34-iconSize,25,PixelFont(17,16),White).font=_mediumFont;
            Label(button.transform,copy,17,_portrait ? 43 : 56,width-34,22,12,Muted);
        }

        private void BuildHelp(Transform root)
        {
            Dim(root);
            var w=Mathf.Min(_width-40,510f);var h=Mathf.Min(_height-32,570);
            var panel=Shape(root,"Driving guide panel",(_width-w)*.5f,(_height-h)*.5f,w,h,RacingHudGraphic.Shape.Panel,PanelColor);
            Chip(panel.transform,"Quick start",26,24,122,TileColor,Accent);
            Label(panel.transform,"Take the wheel.",26,76,w-52,48,32,White);
            var controls = Application.isMobilePlatform
                ? "Hold Gas and use the steering arrows.\nSwitch to tilt steering in Settings.\nUse Brake to stop or Rev to back up.\nTap Nitro to boost or Map to explore.\nTap Unstuck to get back on the road."
                : _compactWide
                    ? "W  Drive     A / D  Steer     Q  Reverse\nSpace  Brake     B  Nitro     C  Camera\nM  City map     R  Unstuck     P  Pause"
                    : "W / arrows     Accelerate\nA / D                Steer\nQ / Rev             Reverse\nSpace / Brake   Stop\nB                        Nitro\nC                        Camera\nM                       City map\nR                        Unstuck";
            Label(panel.transform,controls,28,_compactWide ? 138 : 146,w-56,_compactWide ? 100 : 250,PixelFont(16,14),White);
            Label(panel.transform,"Reverse brakes first when you're moving forward.",28,h-139,w-56,55,13,Muted);
            ActionButton(panel.transform,"Close driving guide","Let's drive",26,h-72,w-52,48,()=>ShowHelp(false),Primary,Ink,16);
        }

        private void BuildShowroom(Transform root)
        {
            var pad=_portrait ? 24f : 48f;
            Brand(root,pad,28);
            var showroom = root;
            _garageStockView = Group(showroom, "Garage vehicle picker");
            root = _garageStockView.transform;
            _garageBack = IconAction(root,"Garage to home","Home",RacingHudGraphic.Symbol.ArrowLeft,_width-pad-130,28,130,44,
                _session.ReturnFromGarage).GetComponentInChildren<Text>();
            Label(root,"Garage",pad,_compactWide ? 102 : 122,_width-pad*2,50,_portrait||_compactWide ? 30 : 45,White);
            var footerY=_height-(_portrait ? Mathf.Max(456,Units(350)) : 164);
            var footer=Shape(root,"Garage selection sheet",_portrait ? 12 : 24,footerY,_width-(_portrait ? 24 : 48),_height-footerY+18,RacingHudGraphic.Shape.Panel,new Color(.025f,.045f,.08f,.97f));
            Label(root,"Selected ride",pad,footerY+18,260,20,12,Muted);
            _vehicleName=Label(root,"",pad,footerY+43,_portrait ? _width-pad*2 : 370,36,_portrait ? 25 : 29,White);
            _vehicleSpecs=Label(root,"",pad,footerY+80,_portrait ? _width-pad*2 : 375,24,13,Muted);
            BuildVehicleChoices(root,pad,_portrait ? footerY+117 : _compactWide ? 169 : 276);
            var paintX=_portrait ? pad : _width*.43f;
            var paintY=footerY+(_portrait ? 286 : 29);
            _paintLabel=Label(root,"",paintX,paintY,290,22,12,Muted);
            for(var i=0;i<_swatches.Length;i++)
            {
                var index=i;
                var button=ActionButton(root,"Paint "+CarCrashHudInput.PaintNames[i],"",paintX+i*Units(48),paintY+30,Units(40),Units(40),()=>_session.SelectPaint(index),CarCrashHudInput.PaintColors[i],White);
                var graphic=(RacingHudGraphic)button.targetGraphic;graphic.shape=RacingHudGraphic.Shape.Circle;
                graphic.cornerRadius=30;_swatches[i]=graphic;
                if (i == _swatches.Length - 1)
                {
                    var lockPicture = Shape(button.transform, "Track Gold locked", Units(9), Units(9), Units(22), Units(22), RacingHudGraphic.Shape.Icon, White);
                    lockPicture.symbol = RacingHudGraphic.Symbol.Lock;
                    _goldLock = lockPicture.gameObject;
                }
            }
            _goldHint = Label(root,"Earn a Gold medal to unlock Track Gold",paintX,paintY+35+Units(40),
                _portrait ? _width-pad*2 : 265,20,12,Muted).gameObject;
            var orbitY=footerY-64;
            IconAction(root,"Orbit left","",RacingHudGraphic.Symbol.ArrowLeft,_width-pad-Units(96),orbitY,Units(44),Units(44),()=>_session.CameraRig.RotateShowcase(-32));
            IconAction(root,"Orbit right","",RacingHudGraphic.Symbol.ArrowRight,_width-pad-Units(44),orbitY,Units(44),Units(44),()=>_session.CameraRig.RotateShowcase(32));
            if(!_compactWide)Label(root,"Drag to look around",pad,orbitY+12,_portrait ? 230 : 300,22,12,Muted);
            var driveHeight=_portrait ? Mathf.Max(56,Units(44)) : 56;
            var actionsWidth = _portrait ? _width - pad * 2f : 300f;
            var actionsX = _portrait ? pad : _width - 348f;
            var actionsY = _portrait ? _height - Units(12) - driveHeight : footerY + 44f;
            var customizeWidth = (actionsWidth - 8f) * .47f;
            var customize = ActionButton(root, "Customize ride", "Customize", actionsX, actionsY, customizeWidth, driveHeight,
                () => ShowCustomization(true), TileColor, White, 15);
            customize.interactable = _session.Garage != null;
            _garageDrive = IconAction(root, "Start engine", "Drive this ride", RacingHudGraphic.Symbol.ArrowRight,
                actionsX + customizeWidth + 8f, actionsY, actionsWidth - customizeWidth - 8f, driveHeight, _session.ConfirmGarage, true).GetComponentInChildren<Text>();
            BuildCustomization(showroom);
        }

        private void BuildVehicleChoices(Transform root,float pad,float y)
        {
            if(_session.Garage==null)return;
            var columns=_portrait||_compactWide ? 3 : 2;
            var width=_portrait ? (_width-pad*2-16)/3 : _compactWide ? 164 : 180;
            var height=_portrait ? 72 : _compactWide ? 54 : 72;
            var shortNames=new[]{"Roadbreaker","Apex GT","Summit","Ridgeback","Atlas","Ironclaw"};
            if(_portrait&&_width<470){shortNames[0]="GT";shortNames[3]="Ridge";}
            var types=new[]{"Coupe","Sport","SUV","Pickup","Truck","Monster"};
            for(var i=0;i<Mathf.Min(6,_session.Garage.vehicles.Length);i++)
            {
                var index=i;var spec=_session.Garage.vehicles[i];
                var button=ActionButton(root,"Select vehicle "+spec.id,"",pad+i%columns*(width+8),y+i/columns*(height+8),width,height,()=>_session.SelectVehicle(index),PanelColor,White);
                _vehicleCards[i]=button.targetGraphic;
                Label(button.transform,shortNames[i],10,13,width-20,24,PixelFont(15,16),White).font=_mediumFont;
                if(!_compactWide)Label(button.transform,types[i],10,44,width-20,20,12,Muted);
            }
        }

        private void BuildRace(Transform root)
        {
            // Panels, spacing, button targets and type use the same scaled canvas units.
            var pad = _portrait ? 18f : 28f;
            var right = _width - pad;
            const float top = 20f, headerHeight = 82f, gap = 8f, pauseSize = 46f;
            var available = _width - 2 * pad - pauseSize - 2 * gap;
            var statusWidth = Mathf.Min(144f, available * .48f);
            var scoreWidth = Mathf.Min(148f, available - statusWidth);
            var status = Shape(root, "Drive status card", pad, top, statusWidth, headerHeight, RacingHudGraphic.Shape.Panel, PanelColor);
            Label(status.transform, "ELYSIAN", 12, 8, statusWidth - 24, 20, 15, White).font = _mediumFont;
            _modeCaption = Label(status.transform, "Free roam", 12, _portrait ? 29 : 42, statusWidth - 24, 20, 13, Muted);
            if (_portrait)
            {
                _timerCaption = null;
                _timer = Label(status.transform, "00:00", 12, 51, statusWidth - 24, 24, 18, White);
            }
            else
            {
                var session = Shape(root, "Session time card", pad + statusWidth + gap, top, 104, headerHeight, RacingHudGraphic.Shape.Panel, PanelColor);
                _timerCaption = Label(session.transform, "Session", 12, 11, 80, 22, 12, Muted);
                _timer = Label(session.transform, "00:00", 12, 35, 80, 30, 20, White);
            }

            var scoreX = right - pauseSize - gap - scoreWidth;
            var scoreCard = Shape(root, "Impact score card", scoreX, top, scoreWidth, headerHeight, RacingHudGraphic.Shape.Panel, PanelColor);
            Label(scoreCard.transform, "Score", 12, 6, scoreWidth - 24, 18, 12, Muted);
            _score = Label(scoreCard.transform, "0", 12, 23, scoreWidth - 24, 34, 25, White);
            _score.resizeTextForBestFit = true;
            _score.resizeTextMinSize = 14;
            _score.resizeTextMaxSize = _score.fontSize;
            _hits = Label(scoreCard.transform, "0 hits", 12, 58, scoreWidth * .60f - 12, 18, 12, Muted);
            _combo = Label(scoreCard.transform, "x1.0", scoreWidth * .60f, 58, scoreWidth * .40f - 12, 18, 12, Accent, TextAnchor.MiddleRight);
            IconAction(root, "Pause", "", RacingHudGraphic.Symbol.Pause, right - pauseSize, top, pauseSize, pauseSize, _session.PauseRun);

            var actionsY = top + headerHeight + 10;
            var actionWidth = _portrait ? (_width - pad * 2 - gap * 2) / 3 : 112f;
            const float actionHeight = 44f;
            var camera = IconAction(root, "Change camera", "Chase", RacingHudGraphic.Symbol.Camera, pad, actionsY, actionWidth, actionHeight, _session.CycleCamera);
            _cameraLabel = camera.GetComponentInChildren<Text>();
            IconAction(root, "Open city map", "Map", RacingHudGraphic.Symbol.Map, pad + actionWidth + gap, actionsY, actionWidth, actionHeight, _session.ToggleMap);
            IconAction(root, "Recover vehicle", "Unstuck", RacingHudGraphic.Symbol.Recover, pad + (actionWidth + gap) * 2, actionsY, actionWidth, actionHeight, _session.RecoverVehicle);

            var mobile = Application.isMobilePlatform;
            const float controlHeight = 62f, cardHeight = 128f;
            var controlY = _height - controlHeight - (mobile ? 14f : 26f);
            var cardY = controlY - cardHeight - 12;
            var cardWidth = Mathf.Min(_portrait ? 134f : 164f, (_width - 2 * pad - 16) * .5f);
            var mapCard = Shape(root, "Navigation widget", pad, cardY, cardWidth, cardHeight, RacingHudGraphic.Shape.Panel, PanelColor);
            _radar = Shape(mapCard.transform, "Live city minimap", 8, 8, cardWidth - 16, cardHeight - 48, RacingHudGraphic.Shape.Radar, Ink);
            _radar.player = _session.vehicle.transform;
            _radar.navigation = _session.Navigation;
            _radar.mission = _session.Mission;
            _radar.traffic = _traffic;
            var network = FindFirstObjectByType<CityRoadNetwork>();
            _radar.cityBuildings = network != null
                ? System.Array.FindAll(network.GetComponentsInChildren<BoxCollider>(), box => box.bounds.size.y > 4)
                : System.Array.Empty<BoxCollider>();
            _integrity = Label(mapCard.transform, "Body 100%", 12, 94, cardWidth - 24, 17, 12, Muted);
            _damageFill = Meter(mapCard.transform, "Body integrity", 12, 115, cardWidth - 24, 3, Accent);

            var speedX = right - cardWidth;
            _dial = Shape(root, "Digital driving instruments", speedX, cardY, cardWidth, cardHeight, RacingHudGraphic.Shape.Panel, PanelColor);
            _speed = Label(_dial.transform, "0", 12, 5, cardWidth - 48, 46, _portrait ? 34 : 38, White);
            _speed.resizeTextForBestFit = true;
            _speed.resizeTextMinSize = 22;
            _speed.resizeTextMaxSize = _speed.fontSize;
            Label(_dial.transform, "km/h", 13, 54, 62, 18, 12, Muted);
            var gear = Shape(_dial.transform, "Gear badge", cardWidth - 38, 14, 26, 26, RacingHudGraphic.Shape.Panel, SelectedColor);
            _gear = Label(gear.transform, "N", 0, 0, 26, 26, 16, Accent, TextAnchor.MiddleCenter);
            _rpmFill = Meter(_dial.transform, "Engine revs", 12, 78, cardWidth - 24, 3, Accent);
            _boostLabel = Label(_dial.transform, "Nitro ready", 12, 90, cardWidth - 24, 17, 12, Muted);
            _boostFill = Meter(_dial.transform, "Nitro charge", 12, 115, cardWidth - 24, 3, CoastalHudTheme.Amber);

            _controls = Group(root, "Touch driving controls");
            var controls = _controls.transform;
            var controlWidth = (_width - 2 * pad - gap * 4) / 5;
            if (!_portrait) controlWidth = Mathf.Min(controlWidth, mobile ? 76f : 66f);
            var rowWidth = 5 * controlWidth + 4 * gap;
            var steerX = _portrait || mobile ? pad : (_width - rowWidth) * .5f;
            var pedalX = mobile && !_portrait ? right - 3 * controlWidth - 2 * gap : steerX + (controlWidth + gap) * 2;
            _left = HoldButton(controls, "Steer left", "", steerX, controlY, controlWidth, controlHeight, PanelColor);
            _right = HoldButton(controls, "Steer right", "", steerX + controlWidth + gap, controlY, controlWidth, controlHeight, PanelColor);
            Glyph(_left.transform, RacingHudGraphic.Symbol.ArrowLeft, (controlWidth - 28) * .5f, (controlHeight - 28) * .5f, 28, White);
            Glyph(_right.transform, RacingHudGraphic.Symbol.ArrowRight, (controlWidth - 28) * .5f, (controlHeight - 28) * .5f, 28, White);
            _reverse = HoldButton(controls, "Reverse pedal", "Rev", pedalX, controlY, controlWidth, controlHeight, PanelColor, 16);
            _brake = HoldButton(controls, "Brake pedal", "Brake", pedalX + controlWidth + gap, controlY, controlWidth, controlHeight, PanelColor, 16);
            _gas = HoldButton(controls, "Accelerator pedal", "Gas", pedalX + (controlWidth + gap) * 2, controlY, controlWidth, controlHeight, SelectedColor, 16);
            _boostButton = IconAction(controls, "Nitro boost", "Nitro", RacingHudGraphic.Symbol.Bolt, speedX, cardY - 48, cardWidth, 40, _session.ActivateBoost).targetGraphic;
            if (!_portrait && !mobile)
                Label(controls, "W  Drive     A / D  Steer     Q  Reverse     M  Map     P  Pause", _width * .5f - 310, _height - 23, 620, 20, 12, Muted, TextAnchor.MiddleCenter);

            var eventWidth = Mathf.Min(_width - 2 * pad, 580);
            var eventY = actionsY + actionHeight + 10;
            _eventBacking = Shape(root, "Navigation hint background", (_width - eventWidth) * .5f, eventY, eventWidth, 36, RacingHudGraphic.Shape.Panel, PanelColor).gameObject;
            _eventLabel = Label(_eventBacking.transform, "", 12, 0, eventWidth - 24, 36, 15, White, TextAnchor.MiddleCenter);
            var impactRoot = Group(root, "Impact feedback");
            _impact = impactRoot.AddComponent<CanvasGroup>();
            _impactTitle = Label(impactRoot.transform, "Impact", _width * .5f - 180, _portrait ? 300 : 190, 360, 35, 24, Accent, TextAnchor.MiddleCenter);
            _impactScore = Label(impactRoot.transform, "+0", _width * .5f - 180, _portrait ? 340 : 230, 360, 45, 34, White, TextAnchor.MiddleCenter);
        }

        private void BuildPause(Transform root)
        {
            Dim(root);var w=Mathf.Min(_width-40,420);var h=440f;
            var panel=Shape(root,"Pause panel",(_width-w)*.5f,(_height-h)*.5f,w,h,RacingHudGraphic.Shape.Panel,PanelColor);
            Chip(panel.transform,"On pause",26,25,110,TileColor,Accent);
            var settingsWidth = Mathf.Max(112, Units(112));
            ActionButton(panel.transform,"Pause settings","Settings",w-26-settingsWidth,24,settingsWidth,42,()=>ShowSettings(true),PanelColor,White,14);
            Label(panel.transform,"Take a moment.",26,81,w-52,46,31,White);
            Label(panel.transform,"Your city will be right here.",26,135,w-52,30,15,Muted);
            IconAction(panel.transform,"Resume driving","Keep driving",RacingHudGraphic.Symbol.Play,26,192,w-52,56,_session.ResumeRun,true);
            ActionButton(panel.transform,"Recover and resume","Unstuck",26,266,(w-64)*.5f,46,_session.RecoverVehicle,PanelColor,White,15);
            ActionButton(panel.transform,"Return to garage","Garage",38+(w-64)*.5f,266,(w-64)*.5f,46,_session.ReturnToShowroom,PanelColor,White,15);
            ActionButton(panel.transform,"Pause to home","Home",26,324,(w-64)*.5f,46,_session.ReturnHome,PanelColor,White,15);
            ActionButton(panel.transform,"End driving session","End drive",38+(w-64)*.5f,324,(w-64)*.5f,46,_session.EndSession,PanelColor,Muted,15);
            if (!Application.isMobilePlatform)
                Label(panel.transform,"P / Esc to resume",26,397,w-52,20,12,Muted,TextAnchor.MiddleCenter);
        }

        private void BuildResults(Transform root)
        {
            Dim(root);var w=Mathf.Min(_width-40,460);var h=_compactWide ? 444f : 510f;var sy=h/510f;
            var panel=Shape(root,"Results panel",(_width-w)*.5f,(_height-h)*.5f,w,h,RacingHudGraphic.Shape.Panel,PanelColor);
            Label(panel.transform,"Drive complete",26,27*sy,w-52,23,13,Accent).name="Result status";
            Label(panel.transform,"That was a ride.",26,70*sy,w-52,45,32,White);
            Label(panel.transform,"Impact score",26,134*sy,w-52,22,13,Muted);
            Label(panel.transform,"0",24,164*sy,w-48,67,55,White).name="Final score";
            var columns=(w-52)/3;
            var titles=new[]{"Vehicles hit","Best chain","Top km/h"};var names=new[]{"Final hits","Final chain","Final speed"};
            for(var i=0;i<3;i++)
            {
                Label(panel.transform,titles[i],26+columns*i,259*sy,columns,22,12,Muted);
                Label(panel.transform,"0",26+columns*i,290*sy,columns,40,27,White).name=names[i];
            }
            IconAction(panel.transform,"Retry run","One more drive",RacingHudGraphic.Symbol.ArrowRight,26,368*sy,w-52,56,_session.RestartRun,true);
            ActionButton(panel.transform,"Garage after run","Garage",26,442*sy,(w-64)*.5f,44,_session.ReturnToShowroom,PanelColor,White,15);
            ActionButton(panel.transform,"Results to home","Home",38+(w-64)*.5f,442*sy,(w-64)*.5f,44,_session.ReturnHome,PanelColor,White,15);
        }

        private void BuildMissionResults(Transform root)
        {
            Dim(root);
            var w = Mathf.Min(_width - 28, 590);
            var h = Mathf.Min(_height - 24, _portrait ? 575 : 481);
            const float pad = 22f;
            var panel = Shape(root, "Mission results card", (_width - w) * .5f, (_height - h) * .5f, w, h,
                RacingHudGraphic.Shape.Panel, PanelColor);
            var playGames = _session.PlayGames.Available;
            _missionTitle = Label(panel.transform, "", pad, 17, w - (playGames ? 150 : 44), 31, 22, White);
            if (playGames)
                ActionButton(panel.transform, "Play Games achievements", "Awards", w - 115, 14, 93, 40,
                    _session.ShowAchievements, PanelColor, White);
            _missionStatus = Label(panel.transform, "", pad, 53, w - 44, 26, 15, Accent);
            _missionScore = Label(panel.transform, "", pad, 77, w * .61f - pad, 61, 47, White);
            _missionBest = Label(panel.transform, "", w * .62f, 87, w * .38f - pad, 46, 15, Muted, TextAnchor.MiddleRight);
            Rule(panel.transform, pad, 150, w - pad * 2);
            _missionSummary = Label(panel.transform, "", pad, 156, w - pad * 2, 26, 14, Muted);
            var rowHeight = _portrait ? 37 : 26;
            for (var i = 0; i < _targetAmounts.Length; i++)
            {
                var y = 193 + i * rowHeight;
                Label(panel.transform, ImpactTargetSpec.Labels[i], pad, y, w * .45f, 23, 15, White);
                _targetAmounts[i] = Label(panel.transform, "", w * .50f, y, w * .50f - pad, 23, 15, Muted, TextAnchor.MiddleRight);
            }

            var actionY = h - 110;
            var half = (w - pad * 2 - 10) * .5f;
            IconAction(panel.transform, "Retry mission", "Retry", RacingHudGraphic.Symbol.Recover, pad, actionY, half, 44, _session.RestartRun, true);
            _nextCourse = IconAction(panel.transform, "Next mission", "Next course", RacingHudGraphic.Symbol.ArrowRight,
                pad + half + 10, actionY, half, 44, _session.NextCourse);
            var buttonWidth = (w - pad * 2 - (playGames ? 16 : 8)) / (playGames ? 3 : 2);
            ActionButton(panel.transform, "Mission results garage", "Garage", pad, h - 58, buttonWidth, 42, _session.ReturnToShowroom, PanelColor, White);
            ActionButton(panel.transform, "Mission results home", "Home", pad + buttonWidth + 8, h - 58, buttonWidth, 42, _session.ReturnHome, PanelColor, White);
            if (playGames)
                ActionButton(panel.transform, "Mission leaderboard", "Ranks", pad + (buttonWidth + 8) * 2, h - 58, buttonWidth, 42,
                    _session.ShowLeaderboard, PanelColor, White);
        }

        private void RefreshMissionResults()
        {
            var mission = _session.Mission;
            var medal = mission.Completed ? mission.Course.Medal(mission.Score) : "";
            SetText(_missionTitle, mission.Course.Name);
            _missionResultStatus = mission.Completed ? medal + (_session.NewBest ? "  /  New personal best" : "  /  Course complete")
                : mission.TimedOut ? "Time expired  /  Finish to bank your score" : "Run ended  /  Finish to bank your score";
            SetText(_missionStatus, _missionResultStatus);
            _missionStatus.color = medal == "Gold" ? new Color(1f, .77f, .32f) : Accent;
            SetText(_missionScore, $"{mission.Score:N0}");
            SetText(_missionBest, $"Personal best\n{_session.BestScore:N0} pts");
            var seconds = Mathf.FloorToInt(_session.ElapsedSeconds);
            SetText(_missionSummary, $"{mission.Hits} targets  /  {mission.PassedCheckpoints}/{mission.CheckpointCount} checkpoints  /  {seconds / 60:00}:{seconds % 60:00}");
            for (var i = 0; i < _targetAmounts.Length; i++)
            {
                var hits = mission.HitsOf((ImpactTargetKind)i);
                SetText(_targetAmounts[i], $"{hits}  ×  {ImpactTargetSpec.Points[i]}  =  {hits * ImpactTargetSpec.Points[i]:N0} pts");
            }
            SetText(_nextCourse.GetComponentInChildren<Text>(), mission.Completed && _session.NextCourseUnlocked ? "Next course" : "Courses");
        }

        private void BuildMissionMap(Transform root)
        {
            Dim(root);
            var w = Mathf.Min(_width - 32, 660);
            var h = Mathf.Min(_height - 24, 720);
            var panel = Shape(root, "Course route panel", (_width - w) * .5f, (_height - h) * .5f, w, h, RacingHudGraphic.Shape.Panel, PanelColor);
            Label(panel.transform, "Course route", 21, 13, w - 42, 44, 29, White);
            _missionRoute = Shape(panel.transform, "Course live route", 21, 65, w - 42, h - 133, RacingHudGraphic.Shape.CoursePreview,
                Ink);
            _missionRoute.player = _session.vehicle.transform;
            _missionRoute.mission = _session.Mission;
            IconAction(panel.transform, "Close mission map", "Keep driving", RacingHudGraphic.Symbol.ArrowRight, 21, h - 56, w - 42, 42,
                _session.CloseMap, true);
        }

        private void BuildCityMap(Transform root)
        {
            Dim(root);
            var w=Mathf.Min(_width-32,1100);var h=Mathf.Min(_height-32,860);
            var panel=Shape(root,"City map panel",(_width-w)*.5f,(_height-h)*.5f,w,h,RacingHudGraphic.Shape.Panel,PanelColor);
            var compact=_compactWide;
            Label(panel.transform,"Explore Elysian",24,compact ? 18 : 23,w-48,43,compact ? 26 : 32,White);
            _mapStatus=Label(panel.transform,"",24,compact ? 64 : 76,w-48,25,12,Muted);
            var mapX=24f;var mapY=compact ? 103 : 124;
            var mapW=_portrait ? w-48 : w*.47f;
            var mapH=_portrait ? h-478 : h-mapY-96;
            _worldMap=Shape(panel.transform,"Full city road map",mapX,mapY,mapW,mapH,RacingHudGraphic.Shape.CityMap,Ink);
            _worldMap.player=_session.vehicle.transform;_worldMap.navigation=_session.Navigation;
            _worldMap.cityBuildings=_radar.cityBuildings;
            var scale=Mathf.Min((mapW-8)/(CityRoadNetwork.MaxX-CityRoadNetwork.MinX),(mapH-8)/(CityRoadNetwork.MaxZ-CityRoadNetwork.MinZ));
            for(var i=0;i<CityRoadNetwork.Places.Length;i++)
            {
                var point=CityRoadNetwork.Places[i].position;
                var x=mapX+mapW*.5f+point.x*scale;
                var y=mapY+mapH*.5f-(point.z-(CityRoadNetwork.MinZ+CityRoadNetwork.MaxZ)*.5f)*scale;
                Label(panel.transform,(i+1).ToString(),x-8,y-10,16,20,12,Ink,TextAnchor.MiddleCenter).font=_mediumFont;
            }
            var choicesX=_portrait ? 24 : mapX+mapW+24;
            var choicesY=_portrait ? mapY+mapH+49 : mapY+43;
            Label(panel.transform,"Find your next stop",choicesX,choicesY-35,w-choicesX-24,24,15,White).font=_mediumFont;
            var choiceWidth=(_portrait ? w-48 : w-choicesX-24)-10;
            choiceWidth*=.5f;
            var choiceHeight=_portrait ? 68 : compact ? 52 : 91;
            var names=new[]{"Old Town","Sunset Marina","Vista Beach","Paloma Park","Freight Yard","Apex Motor Park"};
            if(_portrait&&_width<450){names[1]="Marina";names[5]="Apex circuit";}
            var details=new[]{"Downtown","Waterfront","Coastal escape","Green spaces","Industrial lanes","Driving circuit"};
            for(var i=0;i<CityRoadNetwork.Places.Length;i++)
            {
                var index=i;
                var button=ActionButton(panel.transform,"Explore destination "+i,"",choicesX+i%2*(choiceWidth+10),choicesY+i/2*(choiceHeight+10),choiceWidth,choiceHeight,
                    ()=>_session.ExploreDestination(index),TileColor,White,14);
                _destinationCards[i]=button.targetGraphic;
                Label(button.transform,(i+1).ToString("00"),12,compact ? 14 : 9,26,22,12,Accent).font=_mediumFont;
                Label(button.transform,names[i],compact ? 39 : 12,compact ? 13 : 33,choiceWidth-(compact ? 48 : 24),24,PixelFont(14,16),White).font=_mediumFont;
                if(!_portrait&&!compact)Label(button.transform,details[i],12,63,choiceWidth-24,20,12,Muted);
            }
            if(!_portrait&&!compact)Label(panel.transform,"Choose a place to set a road route.\nGo at your own pace.",choicesX,choicesY+318,w-choicesX-24,52,13,Muted);
            var bottom=h-72;
            ActionButton(panel.transform,"Clear navigation route","Clear route",24,bottom,(w-58)*.5f,48,
                ()=>{_session.Navigation.ClearRoute();_session.CloseMap();},TileColor,White,15);
            IconAction(panel.transform,"Close city map","Keep driving",RacingHudGraphic.Symbol.ArrowRight,34+(w-58)*.5f,bottom,(w-58)*.5f,48,_session.CloseMap,true);
        }

        public void RefreshPhase()
        {
            ReleaseControls();
            var phase = _session.Phase;
            if (SettingsVisible && phase is not (CarCrashHudInput.RunPhase.Home or CarCrashHudInput.RunPhase.Paused)) ShowSettings(false);
            var menu = phase is CarCrashHudInput.RunPhase.Home or CarCrashHudInput.RunPhase.Briefing or CarCrashHudInput.RunPhase.Showroom;
            _atmosphere.showroom = menu;
            _home.SetActive(phase == CarCrashHudInput.RunPhase.Home);
            _briefing.SetActive(phase == CarCrashHudInput.RunPhase.Briefing);
            if (_briefing.activeSelf) RefreshBriefing();
            _showroom.SetActive(phase == CarCrashHudInput.RunPhase.Showroom);
            if (_showroom.activeSelf)
            {
                SetText(_garageBack, _session.GarageForMission ? "Mission" : "Home");
                SetText(_garageDrive, _session.GarageForMission ? "Use this car" : "Drive this ride");
            }
            _race.SetActive(phase is CarCrashHudInput.RunPhase.Driving or CarCrashHudInput.RunPhase.Countdown);
            _controls.SetActive(phase == CarCrashHudInput.RunPhase.Driving);
            _countdown.SetActive(phase == CarCrashHudInput.RunPhase.Countdown);
            _pause.SetActive(phase == CarCrashHudInput.RunPhase.Paused&&!_session.MapVisible);
            _map.SetActive(_session.MapVisible && !_session.IsMission);
            _missionMap.SetActive(_session.MapVisible && _session.IsMission);
            _results.SetActive(phase == CarCrashHudInput.RunPhase.Results && !_session.IsMission);
            _missionResults.SetActive(phase == CarCrashHudInput.RunPhase.Results && _session.IsMission && !_session.DrivingDemoActive);
            if (_missionResults.activeSelf) RefreshMissionResults();
            if (_results.activeSelf)
            {
                ResultText("Result status", _session.NewBest ? "New personal best" : "Drive complete");
                ResultText("Final score", _session.scoring.Score.ToString("N0"));
                ResultText("Final hits", _session.scoring.Hits.ToString("00"));
                ResultText("Final chain", "x" + (_session.scoring.PeakChainDepth + 1));
                ResultText("Final speed", _session.PeakSpeed.ToString("0"));
            }
            RefreshDrivingDemoHud();
        }

        private void ResultText(string name, string value)
        {
            foreach (var text in _results.GetComponentsInChildren<Text>(true))
                if (text.name == name) text.text = value;
        }

        public void RefreshPaint()
        {
            _paintLabel.text = "Finish / " + TitleCase(CarCrashHudInput.PaintNames[_session.PaintIndex]);
            for (var i = 0; i < _swatches.Length; i++)
            {
                var graphic=(RacingHudGraphic)_swatches[i];
                graphic.borderColor=i==_session.PaintIndex ? White : new Color(1,1,1,.16f);
                graphic.strokeWidth=i==_session.PaintIndex ? 3 : 1;
                graphic.GetComponent<Button>().interactable = CarCrashHudInput.PaintUnlocked(i);
                graphic.SetVerticesDirty();
            }
            var goldLocked = !CarCrashHudInput.PaintUnlocked(_swatches.Length - 1);
            _goldLock.SetActive(goldLocked);
            _goldHint.SetActive(goldLocked);
        }

        public void RefreshVehicle()
        {
            var spec=_session.Garage != null ? _session.Garage.Current : null;
            SetText(_vehicleName,spec != null ? TitleCase(spec.displayName) : "Roadbreaker GT");
            SetText(_vehicleSpecs,(spec != null ? TitleCase(spec.category) : "Performance coupe")+" / "+_session.vehicle.maxSpeedKmh.ToString("0")+" km/h");
            for (var i=0;i<_vehicleCards.Length;i++)
                if (_vehicleCards[i]!=null)
                {
                    var selected=_session.Garage != null && _session.Garage.SelectedIndex==i;
                    _vehicleCards[i].color=selected ? SelectedColor : TileColor;
                    var graphic=(RacingHudGraphic)_vehicleCards[i];
                    graphic.borderColor=selected ? Accent : BorderColor;
                    graphic.SetVerticesDirty();
                }
        }

        public void RefreshTelemetry()
        {
            var car = _session.vehicle;
            var score = _session.scoring;
            var course = _session.IsMission ? _session.Mission : null;
            var total = course != null ? course.Score : score.Score;
            var hits = course != null ? course.Hits : score.Hits;
            if (_missionResults.activeSelf)
            {
                var gamesMessage = _session.PlayGames.Message;
                SetText(_missionStatus, string.IsNullOrEmpty(gamesMessage) ? _missionResultStatus : gamesMessage);
            }
            var dt = Time.unscaledDeltaTime;
            _displaySpeed = Mathf.Lerp(_displaySpeed, car.SpeedKmh, 1f - Mathf.Exp(-10f * dt));
            _displayScore = Mathf.MoveTowards(_displayScore, total, Mathf.Max(500f, Mathf.Abs(total - _displayScore) * 12f) * dt);
            SetText(_score, Mathf.RoundToInt(_displayScore).ToString("N0"));
            SetText(_hits, $"{hits} hits");
            SetText(_combo, course != null ? "PTS" : $"x{score.LastMultiplier:0.0}");
            var seconds = _session.IsTimed ? Mathf.Max(0, Mathf.CeilToInt(_session.SelectedCourse.TimeLimit - _session.ElapsedSeconds))
                : Mathf.FloorToInt(_session.ElapsedSeconds);
            SetText(_timer, $"{seconds / 60:00}:{seconds % 60:00}");
            if (_timerCaption != null) SetText(_timerCaption, _session.IsTimed ? "Time left" : "Session");
            SetText(_modeCaption, _session.DrivingDemoActive ? "Driving demo" : course != null ? "Impact Run" : "Free roam");
            _timer.color = _session.IsTimed && seconds <= 10 ? new Color(1f, 0.36f, 0.22f) : White;
            SetText(_speed, Mathf.RoundToInt(_displaySpeed).ToString("0"));
            SetText(_gear, car.Reversing ? "R" : car.SpeedKmh < 1f ? "N" : Mathf.Clamp(Mathf.FloorToInt(car.SpeedKmh / 38f) + 1, 1, 6).ToString());
            SetText(_cameraLabel, _portrait&&_width<450 ? "View" : _session.CameraRig.View.ToString());
            SetText(_boostLabel, car.BoostActive ? "Nitro active" : car.BoostCharge >= 0.35f ? $"Nitro {car.BoostCharge * 100f:0}%" : "Recharging");
            SetText(_integrity, $"Body {100f - score.Damage:0}%");
            SetFill(_boostFill, car.BoostCharge);
            SetFill(_damageFill, 1f - score.Damage / 100f);
            _damageFill.GetComponent<Image>().color = score.Damage > 65f ? new Color(1f, 0.43f, 0.35f) : Accent;
            var revs = car.SpeedKmh < 1 ? 0.08f : 0.2f + Mathf.Repeat(car.SpeedKmh / 38f, 1f) * 0.7f;
            SetFill(_rpmFill, revs);
            if (_race.activeSelf) _radar.SetVerticesDirty();
            _atmosphere.value = car.SpeedKmh / 240f;
            _atmosphere.boosting = car.BoostActive;
            _atmosphere.SetVerticesDirty();
            _boostButton.color = car.BoostActive ? new Color(.52f,.24f,.13f,.98f) : TileColor;
            var recoveryHint = Application.isMobilePlatform ? "Need a hand? Tap Unstuck" : "Need a hand? Unstuck / R";
            var hint = _session.RecoverySuggested ? recoveryHint : _session.DrivingDemoActive ? _session.DrivingDemoHint
                : course != null ? course.Hint(car.transform)
                : car.BoostActive ? "Nitro active" : score.SlowMotionActive ? "Impact cam" : TitleCase(_session.Navigation.Hint);
            SetText(_eventLabel,hint);_eventBacking.SetActive(!string.IsNullOrEmpty(hint));
            if (_missionMap.activeSelf) _missionRoute.SetVerticesDirty();
            if(_map.activeSelf)
            {
                _worldMap.SetVerticesDirty();SetText(_mapStatus,"7 avenues / 13 streets / "+_session.Navigation.VisitedCount+" of 6 places explored");
                for(var i=0;i<_destinationCards.Length;i++)
                    if(_destinationCards[i]!=null)
                    {
                        var graphic=(RacingHudGraphic)_destinationCards[i];var selected=_session.Navigation.Destination==i;
                        graphic.color=selected ? SelectedColor : TileColor;
                        graphic.borderColor=selected ? Accent : BorderColor;
                    }
            }

            var sinceImpact = course != null ? Time.unscaledTime - course.LastHitTime : Time.realtimeSinceStartup - score.LastImpactRealtime;
            var feedbackDuration = course != null ? 1.35f : 2.2f;
            _impact.alpha = _session.Phase == CarCrashHudInput.RunPhase.Driving && sinceImpact < feedbackDuration
                ? Mathf.Clamp01((feedbackDuration - sinceImpact) * 2.8f) : 0f;
            if (_impact.alpha > 0)
            {
                SetText(_impactTitle, course != null ? course.LastTarget : score.LastChainDepth > 0 ? $"Chain reaction  x{score.LastChainDepth + 1}" : "Solid impact");
                SetText(_impactScore, $"+{(course != null ? course.LastPoints : score.LastImpactScore):N0}");
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
            _left?.Release(); _right?.Release(); _gas?.Release(); _brake?.Release(); _reverse?.Release();
        }

        private void Brand(Transform root, float x, float y) =>
            PlaceLogo(root, "Coastal Circuit garage logo", x, y, 152f);

        private void PlaceLogo(Transform root, string name, float x, float y, float width)
        {
            var height = width / LogoAspect;
            if (_coastalLogo == null)
            {
                Label(root, "Crash Car · Elysian", x, y, width, height, width > 200 ? 34 : 16, White);
                return;
            }
            var logo = Rect(root, name, x, y, width, height).gameObject.AddComponent<RawImage>();
            logo.texture = _coastalLogo;
            logo.uvRect = LogoRegion;
            logo.color = Color.white;
            logo.raycastTarget = false;
        }

        private void Chip(Transform parent,string label,float x,float y,float width,Color background,Color foreground)
        {
            var panel=Shape(parent,label+" badge",x,y,width,30,RacingHudGraphic.Shape.Panel,background);
            panel.cornerRadius=15;panel.strokeWidth=0;
            Label(panel.transform,label,8,0,width-16,30,12,foreground,TextAnchor.MiddleCenter).font=_mediumFont;
        }

        private void Glyph(Transform parent,RacingHudGraphic.Symbol symbol,float x,float y,float size,Color tint)
        {
            var icon=Shape(parent,symbol+" icon",x,y,size,size,RacingHudGraphic.Shape.Icon,tint);
            icon.symbol=symbol;
        }

        private Button IconAction(Transform parent,string name,string label,RacingHudGraphic.Symbol symbol,float x,float y,float width,float height,UnityAction action,bool primary=false)
        {
            height=Mathf.Max(height,Units(40));
            var foreground=primary ? Ink : White;
            var button=ActionButton(parent,name,label,x,y,width,height,action,primary ? Primary : TileColor,foreground,height>=80 ? 22 : 16);
            var text=button.GetComponentInChildren<Text>();
            var iconOnly=string.IsNullOrEmpty(label);var showIcon=iconOnly||width>=text.preferredWidth+50;
            if(showIcon)
            {
                var size=Mathf.Min(iconOnly ? 20 : 18, Mathf.Min(width, height) * .64f);
                const float inset=12;
                Glyph(button.transform,symbol,iconOnly ? (width-size)*.5f : inset,(height-size)*.5f,size,foreground);
                if(!iconOnly)
                {
                    var rect=text.rectTransform;
                    rect.anchoredPosition=new Vector2(inset+size+8,0);
                    rect.sizeDelta=new Vector2(width-rect.anchoredPosition.x-inset,height);
                    text.alignment=TextAnchor.MiddleLeft;
                }
            }
            return button;
        }

        private static string TitleCase(string value)=>System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant()).Replace(" Gt"," GT").Replace(" Awd"," AWD").Replace(" Suv"," SUV");

        private float Units(float pixels) => pixels*_scaler.referenceResolution.y/Mathf.Max(1,Screen.height);
        private int PixelFont(int size,float minimumPixels) => Mathf.Max(size,Mathf.CeilToInt(Units(minimumPixels)));

        private Text Label(Transform parent, string value, float x, float y, float width, float height, int size,
            Color tint, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            size = PixelFont(size,12f);
            var rect = Rect(parent, value, x, y, width, Mathf.Max(height,size*1.2f));
            var text = rect.gameObject.AddComponent<Text>();
            text.font = size >= 24 ? _displayFont : _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.color = tint;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private RacingHudGraphic Shape(Transform parent, string name, float x, float y, float width, float height,
            RacingHudGraphic.Shape shape, Color tint)
        {
            var graphic = Rect(parent, name, x, y, width, height).gameObject.AddComponent<RacingHudGraphic>();
            graphic.shape = shape;
            graphic.color = tint;
            graphic.accent = Accent;
            graphic.borderColor = BorderColor;
            graphic.raycastTarget = false;
            return graphic;
        }

        private Button ActionButton(Transform parent, string name, string label, float x, float y, float width, float height,
            UnityAction action, Color background, Color foreground, int fontSize = 16)
        {
            fontSize = PixelFont(fontSize,16f);
            height = Mathf.Max(height,Units(40));
            var graphic = Shape(parent, name, x, y, width, height, RacingHudGraphic.Shape.Panel, background);
            graphic.raycastTarget = true;
            var button = graphic.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            graphic.cornerRadius=14;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f);
            colors.pressedColor = new Color(.78f,.84f,.79f);
            colors.fadeDuration = .12f;
            button.colors = colors;
            button.gameObject.AddComponent<HudButtonMotion>();
            button.onClick.AddListener(action);
            var text=Label(graphic.transform, label, 7, 0, width - 14, height, fontSize, foreground, TextAnchor.MiddleCenter);
            text.horizontalOverflow=HorizontalWrapMode.Overflow;
            text.font=_mediumFont;
            return button;
        }

        private HudHoldButton HoldButton(Transform parent, string name, string label, float x, float y, float width, float height,
            Color background, int size = 28)
        {
            size = PixelFont(size,14f);
            var graphic = Shape(parent, name, x, y, width, height, RacingHudGraphic.Shape.Panel, background);
            graphic.raycastTarget = true;
            var hold = graphic.gameObject.AddComponent<HudHoldButton>();
            Label(graphic.transform, label, 2, 0, width - 4, height, size, White, TextAnchor.MiddleCenter).font=_mediumFont;
            return hold;
        }

        private RectTransform Meter(Transform parent, string name, float x, float y, float width, float height, Color tint)
        {
            var track = Solid(parent, name, x, y, width, height, new Color(0.75f, 0.92f, 0.83f, 0.22f));
            var fill = Solid(track, "Fill", 0, 0, width, height, tint);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            return fill;
        }

        private static void SetFill(RectTransform rect, float amount) => rect.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
        private static void SetText(Text text, string value) { if (text.text != value) text.text = value; }
        private void Rule(Transform parent, float x, float y, float width) => Solid(parent, "Divider", x, y, width, 1, BorderColor);
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
            Solid(parent, "Modal backdrop", 0, 0, _width, _height, new Color(.008f, .031f, .032f, .70f)).GetComponent<Image>().raycastTarget = true;
        }
        private static GameObject Group(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            if(name is "Home screen" or "Live garage" or "Pause menu" or "Run results" or "Explore city map" or "Driving guide")rect.gameObject.AddComponent<HudPanelMotion>();
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
