using UnityEngine;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed partial class RacingHudView
    {
        private const int VehiclePageSize = 6;
        private int _vehiclePage;
        private Text _vehiclePageLabel;
        private Text _vehicleSlot;
        private Button _customizeRide;

        private void BuildVehiclePager(Transform root, float pad, float choiceY)
        {
            if (_vehicleCards.Length <= VehiclePageSize) return;
            var y = _portrait ? choiceY - 110f : choiceY - 48f;
            var x = _width - pad - 144f;
            _vehiclePageLabel = Label(root, "", x + 45f, y, 54f, 36f, 13, Muted, TextAnchor.MiddleCenter);
            IconAction(root, "Previous vehicles", "", RacingHudGraphic.Symbol.ArrowLeft, x, y, 42f, 36f, () => StepVehiclePage(-1));
            IconAction(root, "More vehicles", "", RacingHudGraphic.Symbol.ArrowRight, x + 102f, y, 42f, 36f, () => StepVehiclePage(1));
            ApplyVehiclePage();
        }

        private int VehiclePageCount => Mathf.Max(1, (_vehicleCards.Length + VehiclePageSize - 1) / VehiclePageSize);

        private void StepVehiclePage(int direction)
        {
            _vehiclePage = (_vehiclePage + direction + VehiclePageCount) % VehiclePageCount;
            ApplyVehiclePage();
        }

        private void ShowSelectedVehiclePage()
        {
            _vehiclePage = (_session.Garage?.SelectedIndex ?? 0) / VehiclePageSize;
            ApplyVehiclePage();
        }

        private void ApplyVehiclePage()
        {
            for (var i = 0; i < _vehicleCards.Length; i++)
                _vehicleCards[i].gameObject.SetActive(i / VehiclePageSize == _vehiclePage);
            if (_vehiclePageLabel != null) SetText(_vehiclePageLabel, $"{_vehiclePage + 1} / {VehiclePageCount}");
        }

        private void RefreshVehicleDetails(GarageVehicle spec)
        {
            var locked = _session.SelectedRideLocked;
            var premium = spec != null && CarCrashHudInput.IsPremiumRide(spec.id);
            SetText(_vehicleSlot, locked ? "Premium / locked" : premium ? "Premium / unlocked" : "Selected ride");
            if (spec != null)
            {
                var strength = Mathf.RoundToInt(Mathf.Clamp01(spec.impactResistance) * 100f);
                var drive = spec.allWheelDrive ? "AWD" : "RWD";
                SetText(_vehicleSpecs, $"{spec.topSpeed:0} km/h  /  {drive}  /  {spec.mass:N0} kg\n" +
                    $"Accel {spec.acceleration:0.0}  /  Steering {spec.steering:0}°  /  Armor {strength:0}");
            }
            else SetText(_vehicleSpecs, $"{_session.vehicle.maxSpeedKmh:0} km/h");
            SetText(_garageDrive, locked ? "Watch clip" : _session.GarageForMission ? "Use this car" : "Drive this ride");
            _customizeRide.interactable = _session.Garage != null && !locked;
        }
    }
}
