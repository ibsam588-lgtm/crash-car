using UnityEngine;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed partial class RacingHudView
    {
        private static readonly string[] UpgradeTabs = { "Wheels", "Lights", "Body" };
        private static readonly GarageUpgrade[][] UpgradeGroups =
        {
            new[] { GarageUpgrade.Wheels, GarageUpgrade.RimFinish, GarageUpgrade.Sidewalls },
            new[] { GarageUpgrade.Headlights, GarageUpgrade.AuxiliaryLights },
            new[] { GarageUpgrade.PaintFinish, GarageUpgrade.BodyKit, GarageUpgrade.WindowTint }
        };

        private GameObject _garageStockView, _garageCustomizationView;
        private Text _customizationVehicle;
        private readonly Graphic[] _upgradeTabs = new Graphic[UpgradeTabs.Length];
        private readonly GameObject[] _upgradeRows = new GameObject[3];
        private readonly Text[] _upgradeLabels = new Text[3], _upgradeChoices = new Text[3];
        private int _upgradeTab;
        public bool CustomizationVisible { get; private set; }

        private void BuildCustomization(Transform showroom)
        {
            _garageCustomizationView = Group(showroom, "Garage customization");
            var width = _portrait ? Mathf.Min(_width - 24f, 486f) : Mathf.Min(392f, _width * .49f - 12f);
            const float pad = 16f, gap = 6f;
            var arrow = Mathf.Max(42f, Units(38f));
            var rowHeight = arrow + 21f;
            var tabsHeight = Mathf.Max(42f, Units(38f));
            var actionHeight = Mathf.Max(44f, Units(40f));
            var header = _compactWide ? 60f : 73f;
            var height = header + tabsHeight + rowHeight * 3f + actionHeight + 29f;
            var x = _portrait ? (_width - width) * .5f : _compactWide ? 16f : 24f;
            var y = _portrait ? Mathf.Max(112f, _height - height - 15f) : Mathf.Max(114f, (_height - height) * .76f);
            y = Mathf.Max(0f, Mathf.Min(y, _height - height - 8f));
            var panel = Shape(_garageCustomizationView.transform, "Garage upgrade panel", x,y,width,height,
                RacingHudGraphic.Shape.Panel, GarageDeck);
            panel.cornerRadius = 21f;
            Text title = Label(panel.transform, "Customize", pad, 10, width - pad * 2f, 33f, _compactWide ? 22 : 27, White);
            title.font = _displayFont;
            _customizationVehicle = Label(panel.transform, "", pad, _compactWide ? 39f : 45f, width - pad * 2f, 23f, 13, Muted);

            var tabWidth = (width - pad * 2f - gap * 2f) / UpgradeTabs.Length;
            for (var i = 0; i < UpgradeTabs.Length; i++)
            {
                var tab = i;
                _upgradeTabs[i] = ActionButton(panel.transform, "Customize " + UpgradeTabs[i], UpgradeTabs[i],
                    pad + i * (tabWidth + gap), header, tabWidth, tabsHeight,
                    () => { _upgradeTab = tab; RefreshCustomization(); }, GarageTile, White, 14).targetGraphic;
            }

            var rowWidth = width - 2f * pad;
            for (var i = 0; i < _upgradeRows.Length; i++)
            {
                var row = i;
                _upgradeRows[i] = Rect(panel.transform,"Upgrade option " + (i + 1),pad,header + tabsHeight + 9f + i * rowHeight,rowWidth,rowHeight).gameObject;
                var root = _upgradeRows[i].transform;
                _upgradeLabels[i] = Label(root,"",0,0,rowWidth,20f,13,Muted,TextAnchor.MiddleCenter);
                IconAction(root,"Previous upgrade choice " + (i+1),"",RacingHudGraphic.Symbol.ArrowLeft,
                    0,19f,arrow,arrow,()=>StepUpgrade(row,-1));
                _upgradeChoices[i] = Label(root,"",arrow,19f,rowWidth - arrow * 2f,arrow,16,White,TextAnchor.MiddleCenter);
                _upgradeChoices[i].font = _mediumFont;
                IconAction(root,"Next upgrade choice " + (i+1),"",RacingHudGraphic.Symbol.ArrowRight,
                    rowWidth - arrow,19f,arrow,arrow,()=>StepUpgrade(row,1));
            }

            ActionButton(panel.transform, "Close ride customization", "Done", pad, height - actionHeight - 11f,
                rowWidth, actionHeight, () => ShowCustomization(false), Primary, Ink, 15);
            var orbitY = _portrait ? Mathf.Max(100f, y - arrow - 12f) : Mathf.Min(_height - arrow - 12f, y + height - actionHeight);
            var orbitX = _width - Mathf.Max(24f, Units(19f)) - 2f * arrow - gap;
            IconAction(_garageCustomizationView.transform,"Inspect car left","",RacingHudGraphic.Symbol.ArrowLeft,
                orbitX,orbitY,arrow,arrow,()=>_session.CameraRig.RotateShowcase(-35f));
            IconAction(_garageCustomizationView.transform,"Inspect car right","",RacingHudGraphic.Symbol.ArrowRight,
                orbitX + arrow + gap,orbitY,arrow,arrow,()=>_session.CameraRig.RotateShowcase(35f));
            ShowCustomization(CustomizationVisible);
        }

        public void ShowCustomization(bool open)
        {
            CustomizationVisible = open && _session.Garage?.Current != null;
            if (_garageStockView != null) _garageStockView.SetActive(!CustomizationVisible);
            if (_garageCustomizationView != null) _garageCustomizationView.SetActive(CustomizationVisible);
            if (CustomizationVisible) RefreshCustomization();
        }

        private void StepUpgrade(int row, int delta)
        {
            var group = UpgradeGroups[_upgradeTab];
            if (row >= group.Length) return;
            _session.StepGarageUpgrade(group[row],delta);
            RefreshCustomization();
        }

        public void RefreshCustomization()
        {
            var garage = _session.Garage;
            if (!CustomizationVisible || _garageCustomizationView == null || garage?.Current == null) return;
            var vehicle = garage.Current;
            SetText(_customizationVehicle, GarageName(vehicle.displayName));
            for (var i = 0; i < _upgradeTabs.Length; i++)
                TintGarageTab(_upgradeTabs[i].GetComponent<Button>(), i == _upgradeTab, CoastalHudTheme.Seafoam);
            var group = UpgradeGroups[_upgradeTab];
            for (var i = 0; i < _upgradeRows.Length; i++)
            {
                _upgradeRows[i].SetActive(i < group.Length);
                if (i >= group.Length) continue;
                var upgrade = group[i];
                SetText(_upgradeLabels[i], GarageCustomization.Label(upgrade));
                SetText(_upgradeChoices[i], GarageCustomization.Options(vehicle.id, upgrade)[garage.GetUpgrade(upgrade)]);
            }
        }
    }
}
