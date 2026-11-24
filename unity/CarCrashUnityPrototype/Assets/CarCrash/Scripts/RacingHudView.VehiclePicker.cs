using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed partial class RacingHudView
    {
        private const float GarageDeckHeight = 430f;
        private const float GarageCardWidth = 128f;
        private const float GarageCardStep = GarageCardWidth + 8f;
        private static readonly Color GarageDeck = new Color32(8, 25, 33, 251);
        private static readonly Color GarageTile = new Color32(18, 44, 51, 255);
        private static readonly Color GarageSlate = new Color32(14, 32, 42, 255);
        private static readonly Color PremiumAccent = CoastalHudTheme.Amber;
        private static readonly Color[] GaugeColors = { CoastalHudTheme.Seafoam, CoastalHudTheme.Coral,
            new Color32(149, 197, 255, 255), PremiumAccent };

        private sealed class VehicleTile
        {
            public int Index;
            public RacingHudGraphic Surface;
            public Text Name, Status;
        }

        private sealed class VehicleShelf
        {
            public ScrollRect Scroll;
            public readonly List<VehicleTile> Tiles = new();
        }

        private readonly VehicleShelf[] _garageShelves = new VehicleShelf[2];
        private readonly int[] _lastShelfRide = { -1, -1 };
        private readonly Text[] _garageStatAmounts = new Text[4];
        private readonly GarageStatMeter[] _garageMeters = new GarageStatMeter[4];
        private Text _garageClass, _garageStatus, _garageShelfTitle;
        private Button _customizeRide;
        private Button _fleetTab, _premiumTab, _shelfPrevious, _shelfNext;
        private int _shownShelf = -1;
        private float _nextRentalRefresh;

        private void BuildGarageCatalog(Transform root)
        {
            var pad = _portrait ? 12f : _compactWide ? 16f : 24f;
            var deckWidth = _portrait ? _width - 2f * pad : Mathf.Min(392f, _width * .49f - 12f);
            var deckTop = _portrait ? _height - GarageDeckHeight - 10f : (_height - GarageDeckHeight) * .5f;
            var headerX = _portrait ? pad + 5f : deckWidth + pad * 2f;
            var logoWidth = _portrait ? 106f : _compactWide ? 104f : 123f;
            PlaceLogo(root, "Coastal Circuit garage logo", headerX, 14f, logoWidth);
            var backWidth = _portrait ? 104f : 94f;
            _garageBack = IconAction(root, "Garage to home", "Home", RacingHudGraphic.Symbol.ArrowLeft,
                _width - pad - backWidth, 18f, backWidth, 42f, _session.ReturnFromGarage).GetComponentInChildren<Text>();
            var titleX = headerX + logoWidth + 12f;
            var availableTitleWidth = _width - pad - backWidth - titleX - 8f;
            if (availableTitleWidth < 110f)
            {
                titleX = headerX;
                availableTitleWidth = _width - pad - titleX;
            }
            Label(root, "Garage", titleX, titleX == headerX ? 64f : 22f, availableTitleWidth, 36f, _compactWide ? 25 : 29, White);
            var orbitY = _portrait ? deckTop - 48f : _height - 66f;
            var orbitX = _width - pad - 90f;
            IconAction(root, "Orbit left", "", RacingHudGraphic.Symbol.ArrowLeft, orbitX, orbitY, 42f, 42f,
                () => _session.CameraRig.RotateShowcase(-32f));
            IconAction(root, "Orbit right", "", RacingHudGraphic.Symbol.ArrowRight, orbitX + 46f, orbitY, 42f, 42f,
                () => _session.CameraRig.RotateShowcase(32f));
            var hintX = _portrait ? pad + 8f : headerX;
            if (orbitX - hintX > 105f)
                Label(root, "Drag to rotate", hintX, orbitY + 10f, orbitX - hintX - 8f, 22f, 13, Muted);

            var deck = Shape(root, "Garage collection panel", pad, deckTop, deckWidth, GarageDeckHeight,
                RacingHudGraphic.Shape.Panel, GarageDeck);
            deck.cornerRadius = 21f;
            deck.borderColor = new Color32(136, 216, 209, 74);
            deck.raycastTarget = true;

            const float inset = 13f, gap = 8f;
            var contentWidth = deckWidth - 2f * inset;
            var tabWidth = (contentWidth - gap) * .5f;
            _fleetTab = ActionButton(deck.transform, "Garage fleet tab", "Fleet", inset, 11f, tabWidth, 40f,
                () => SelectGarageShelf(0), GarageTile, White);
            _premiumTab = ActionButton(deck.transform, "Garage premium tab", "Premium", inset + tabWidth + gap, 11f, tabWidth, 40f,
                () => SelectGarageShelf(1), GarageTile, White);

            _vehicleName = Label(deck.transform, "", inset, 57f, contentWidth, 35f, _compactWide ? 25 : 30, White);
            _garageClass = Label(deck.transform, "", inset, 94f, contentWidth - 106f, 21f, 13, Muted);
            _garageClass.resizeTextForBestFit = true;
            _garageClass.resizeTextMinSize = PixelFont(10, 10f);
            _garageClass.resizeTextMaxSize = _garageClass.fontSize;
            _garageClass.verticalOverflow = VerticalWrapMode.Truncate;
            _garageStatus = Label(deck.transform, "", deckWidth - inset - 102f, 94f, 102f, 21f, 13, PremiumAccent,
                TextAnchor.MiddleRight);
            BuildGarageGauges(deck.transform, inset, 122f, contentWidth);
            BuildGarageShelves(deck.transform, inset, 233f, contentWidth);
            BuildGaragePaint(deck.transform, inset, 326f, contentWidth);

            var actionWidth = (contentWidth - gap) * .5f;
            _customizeRide = ActionButton(deck.transform, "Customize ride", "Customize", inset, 374f, actionWidth, 43f,
                () => ShowCustomization(true), GarageTile, White, 15);
            _garageDrive = ActionButton(deck.transform, "Start engine", "Drive", inset + actionWidth + gap, 374f, actionWidth, 43f,
                _session.ConfirmGarage, Primary, Ink, 15).GetComponentInChildren<Text>();
        }

        private void BuildGarageGauges(Transform root, float x, float y, float width)
        {
            const float gap = 7f, height = 49f;
            var cardWidth = (width - gap) * .5f;
            var labels = new[] { "Speed", "Acceleration", "Steering", "Armor" };
            for (var i = 0; i < labels.Length; i++)
            {
                var card = Shape(root, labels[i] + " gauge", x + i % 2 * (cardWidth + gap), y + i / 2 * (height + gap),
                    cardWidth, height, RacingHudGraphic.Shape.Panel, GarageSlate);
                card.cornerRadius = 9f;
                card.borderColor = new Color32(143, 190, 192, 45);
                Label(card.transform, labels[i], 9f, 4f, cardWidth - 53f, 23f, 12, Muted);
                _garageStatAmounts[i] = Label(card.transform, "", 56f, 4f, cardWidth - 65f, 23f, 14,
                    GaugeColors[i], TextAnchor.MiddleRight);
                _garageStatAmounts[i].font = _mediumFont;
                _garageMeters[i] = Rect(card.transform, labels[i] + " meter", 9f, 34f, cardWidth - 18f, 6f)
                    .gameObject.AddComponent<GarageStatMeter>();
                _garageMeters[i].color = GaugeColors[i];
                _garageMeters[i].raycastTarget = false;
            }
        }

        private void BuildGarageShelves(Transform root, float x, float y, float width)
        {
            _garageShelfTitle = Label(root, "", x, y + 3f, width - 95f, 24f, 14, White);
            _garageShelfTitle.font = _mediumFont;
            _shelfPrevious = IconAction(root, "Previous vehicles", "", RacingHudGraphic.Symbol.ArrowLeft,
                x + width - 84f, y - 7f, 40f, 32f, () => ScrollGarageShelf(-1));
            _shelfNext = IconAction(root, "More vehicles", "", RacingHudGraphic.Symbol.ArrowRight,
                x + width - 40f, y - 7f, 40f, 32f, () => ScrollGarageShelf(1));
            var rides = _session.Garage?.vehicles;
            for (var shelfIndex = 0; shelfIndex < _garageShelves.Length; shelfIndex++)
            {
                var lane = Rect(root, shelfIndex == 0 ? "Fleet collection" : "Premium collection", x, y + 34f, width, 55f);
                var viewport = Rect(lane, "Vehicle shelf viewport", 0, 0, width, 55f);
                viewport.gameObject.AddComponent<RectMask2D>();
                var inputSurface = viewport.gameObject.AddComponent<Image>();
                inputSurface.color = Color.clear;
                var content = Rect(viewport, "Browse vehicles", 0, 0, width, 54f);
                var scroll = lane.gameObject.AddComponent<ScrollRect>();
                scroll.horizontal = true;
                scroll.vertical = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.viewport = viewport;
                scroll.content = content;
                var shelf = _garageShelves[shelfIndex] = new VehicleShelf { Scroll = scroll };
                if (rides == null) continue;
                for (var index = 0; index < rides.Length; index++)
                {
                    var ride = rides[index];
                    if (ride.premium != (shelfIndex == 1)) continue;
                    var slot = index;
                    var tile = ActionButton(content, "Select vehicle " + ride.id, "", GarageCardStep * shelf.Tiles.Count, 0,
                        GarageCardWidth, 54f, () => _session.SelectVehicle(slot), GarageTile, White);
                    var title = Label(tile.transform, GarageTileName(ride), 9f, 5f, GarageCardWidth - 18f, 23f, 14, White);
                    title.font = _mediumFont;
                    var status = Label(tile.transform, "", 9f, 31f, GarageCardWidth - 18f, 17f, 12, Muted);
                    shelf.Tiles.Add(new VehicleTile { Index = index, Surface = (RacingHudGraphic)tile.targetGraphic, Name = title, Status = status });
                }
                content.sizeDelta = new Vector2(Mathf.Max(width, shelf.Tiles.Count * GarageCardStep - 8f), 54f);
            }
            _shownShelf = -1;
        }

        private void BuildGaragePaint(Transform root, float x, float y, float width)
        {
            _paintLabel = Label(root, "", x, y, width - 132f, 22f, 12, Muted);
            var chipWidth = Mathf.Min(25f, (width - 36f) / _swatches.Length);
            var spacing = chipWidth + 7f;
            for (var i = 0; i < _swatches.Length; i++)
            {
                var paint = i;
                var button = ActionButton(root, "Paint " + CarCrashHudInput.PaintNames[i], "",
                    x + i * spacing, y + 20f, chipWidth, chipWidth, () => _session.SelectPaint(paint), CarCrashHudInput.PaintColors[i], White);
                var graphic = (RacingHudGraphic)button.targetGraphic;
                graphic.rectTransform.sizeDelta = new Vector2(chipWidth, chipWidth);
                graphic.shape = RacingHudGraphic.Shape.Circle;
                _swatches[i] = graphic;
                if (i != _swatches.Length - 1) continue;
                var mark = Shape(button.transform, "Track Gold locked", (chipWidth - 13f) * .5f, (chipWidth - 13f) * .5f,
                    13f, 13f, RacingHudGraphic.Shape.Icon, White);
                mark.symbol = RacingHudGraphic.Symbol.Lock;
                _goldLock = mark.gameObject;
            }
            var goldX = x + _swatches.Length * spacing - 1f;
            _goldHint = Label(root, "Gold medal unlock", goldX, y + 20f, width - (goldX - x), chipWidth, 12, Muted).gameObject;
        }

        private void SelectGarageShelf(int index)
        {
            if (_shownShelf == index) return;
            var shelf = _garageShelves[index];
            if (shelf.Tiles.Count == 0) return;
            var last = _lastShelfRide[index];
            _session.SelectVehicle(last >= 0 ? last : shelf.Tiles[0].Index);
        }

        private void ScrollGarageShelf(int direction)
        {
            if (_shownShelf < 0) return;
            var scroll = _garageShelves[_shownShelf].Scroll;
            var offset = Mathf.Max(0f, -scroll.content.anchoredPosition.x);
            PositionGarageShelf(scroll, offset + direction * GarageCardStep);
        }

        private static void PositionGarageShelf(ScrollRect scroll, float offset)
        {
            var available = scroll.viewport.rect.width;
            scroll.StopMovement();
            scroll.content.anchoredPosition = new Vector2(-Mathf.Clamp(offset, 0f, scroll.content.rect.width - available), 0f);
        }

        private void RefreshGarageCatalog()
        {
            var garage = _session.Garage;
            var ride = garage?.Current;
            var shelfIndex = ride?.premium == true ? 1 : 0;
            var shelf = _garageShelves[shelfIndex];
            var selected = garage?.SelectedIndex ?? -1;
            if (ride != null) _lastShelfRide[shelfIndex] = selected;

            SetText(_vehicleName, ride == null ? "Roadbreaker GT" : GarageName(ride.displayName));
            SetText(_garageClass, ride == null ? "" : FormatGarageClass(ride));
            SetGarageGauge(0, ride == null ? "—" : $"{ride.topSpeed:0} km/h", (ride?.topSpeed ?? 0f) / 320f);
            SetGarageGauge(1, ride == null ? "—" : $"{ride.acceleration:0.0}", (ride?.acceleration ?? 0f) / 14f);
            SetGarageGauge(2, ride == null ? "—" : $"{ride.steering:0}°", (ride?.steering ?? 0f) / 46f);
            SetGarageGauge(3, ride == null ? "—" : $"{ride.impactResistance * 100f:0}", ride?.impactResistance ?? 0f);
            RefreshGarageRentalStatus();

            if (_shownShelf != shelfIndex)
            {
                _shownShelf = shelfIndex;
                _garageShelves[0].Scroll.gameObject.SetActive(shelfIndex == 0);
                _garageShelves[1].Scroll.gameObject.SetActive(shelfIndex == 1);
                TintGarageTab(_fleetTab, shelfIndex == 0, CoastalHudTheme.Seafoam);
                TintGarageTab(_premiumTab, shelfIndex == 1, PremiumAccent);
                SetText(_garageShelfTitle, (shelfIndex == 1 ? "Premium collection" : "Your fleet") + $"  ·  {shelf.Tiles.Count}");
                _shelfPrevious.interactable = _shelfNext.interactable = shelf.Scroll.content.rect.width > shelf.Scroll.viewport.rect.width;
            }

            for (var i = 0; i < shelf.Tiles.Count; i++)
            {
                var tile = shelf.Tiles[i];
                var spec = garage.vehicles[tile.Index];
                var active = tile.Index == selected;
                var accent = spec.premium ? PremiumAccent : CoastalHudTheme.Seafoam;
                var locked = CarCrashHudInput.RideLocked(spec);
                tile.Surface.color = active ? spec.premium ? new Color32(70, 52, 34, 255) : SelectedColor : GarageTile;
                tile.Surface.borderColor = active ? accent : BorderColor;
                tile.Surface.SetVerticesDirty();
                tile.Name.color = active ? accent : White;
                SetText(tile.Status, spec.premium ? locked ? "Locked" : "Ready" : spec.allWheelDrive ? "AWD" : "RWD");
                tile.Status.color = locked && active ? PremiumAccent : Muted;
                if (!active) continue;
                var scroll = shelf.Scroll;
                var left = Mathf.Max(0f, -scroll.content.anchoredPosition.x);
                var cardLeft = i * GarageCardStep;
                var cardRight = cardLeft + GarageCardWidth;
                var view = scroll.viewport.rect.width;
                if (cardLeft < left) PositionGarageShelf(scroll, cardLeft);
                else if (cardRight > left + view) PositionGarageShelf(scroll, cardRight - view);
            }
        }

        private void RefreshGarageRentalStatus()
        {
            var ride = _session.Garage?.Current;
            var premium = ride?.premium == true;
            var time = CarCrashHudInput.RentalRemaining(ride);
            var locked = premium && time.Ticks == 0L;
            var status = !premium ? "" : locked ? "24h via clip" : time.TotalHours >= 1
                ? $"{time.Hours + time.Days * 24}h {time.Minutes:00}m left" : $"{Mathf.Max(1, Mathf.CeilToInt((float)time.TotalMinutes))}m left";
            SetText(_garageStatus, status);
            SetText(_garageDrive, locked ? "Watch clip" : _session.GarageForMission ? "Use car" : "Drive");
            _garageDrive.GetComponentInParent<RacingHudGraphic>().color = premium ? PremiumAccent : Primary;
            _customizeRide.interactable = ride != null && !locked;
        }

        private void TickGarageRental()
        {
            if (_session.Phase != CarCrashHudInput.RunPhase.Showroom || _session.Garage?.Current?.premium != true ||
                Time.unscaledTime < _nextRentalRefresh) return;
            _nextRentalRefresh = Time.unscaledTime + 1f;
            RefreshGarageRentalStatus();
        }

        private void SetGarageGauge(int index, string amount, float progress)
        {
            SetText(_garageStatAmounts[index], amount);
            _garageMeters[index].SetValue(progress);
        }

        private static void TintGarageTab(Button button, bool active, Color tint)
        {
            var graphic = (RacingHudGraphic)button.targetGraphic;
            graphic.color = active ? tint : GarageSlate;
            graphic.borderColor = active ? tint : BorderColor;
            graphic.SetVerticesDirty();
            button.GetComponentInChildren<Text>().color = active ? Ink : White;
        }

        private static string GarageName(string name) => TitleCase(name).Replace(" Rs", " RS").Replace(" Xr", " XR");

        private string FormatGarageClass(GarageVehicle ride)
        {
            var chassis = $"{(ride.allWheelDrive ? "AWD" : "RWD")}  ·  {ride.mass:N0} kg";
            return _compactWide ? chassis : GarageName(ride.category) + "  ·  " + chassis;
        }

        private static string GarageTileName(GarageVehicle ride) => !string.IsNullOrEmpty(ride.shortName) ? ride.shortName : ride.id switch
        {
            "roadbreaker" => "Roadbreaker",
            "apex" => "Apex GT",
            "summit" => "Summit",
            "ridgeback" => "Ridgeback",
            "atlas" => "Atlas",
            "ironclaw" => "Ironclaw",
            "police" => "Interceptor",
            _ => GarageName(ride.displayName)
        };
    }
}
