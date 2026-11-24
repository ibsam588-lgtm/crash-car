using System;
using UnityEngine;

namespace CarCrash
{
    [Serializable]
    public sealed class GarageVehicle
    {
        public string id, displayName, category, description;
        public GameObject visualPrefab;
        public Vector3 size, colliderCenter, colliderSize;
        public Vector3[] wheelCenters = new Vector3[4];
        public float wheelRadius = 0.385f, mass = 1350f, topSpeed = 192f, acceleration = 7.2f;
        public float suspension = 0.18f, spring = 42000f, damper = 5400f;
        public float steering = 29f, reverseSpeed = 30f;
        public bool allWheelDrive;
        [Range(0f, 1f)] public float impactResistance, trafficPush;

        internal GarageVehicle Variant(string variantId, string name, string type, string blurb)
        {
            var variant = (GarageVehicle)MemberwiseClone();
            variant.id = variantId;
            variant.displayName = name;
            variant.category = type;
            variant.description = blurb;
            return variant;
        }
    }

    [RequireComponent(typeof(ArcadeVehicleController))]
    public sealed class VehicleGarage : MonoBehaviour
    {
        public GarageVehicle[] vehicles = Array.Empty<GarageVehicle>();
        public Transform visualRoot;
        public int SelectedIndex { get; private set; }
        public GarageVehicle Current => vehicles.Length > 0 ? vehicles[SelectedIndex] : null;
        private readonly int[] _upgrades = new int[(int)GarageUpgrade.WindowTint + 1];
        private GarageUpgradeVisuals _upgradeVisuals;
        private HeadlightProfile Lamp => GarageCustomization.Lamps[GetUpgrade(GarageUpgrade.Headlights)];

        public Color HeadlightColor => Lamp.Color;
        public float HeadlightRange => Lamp.Range;
        public float HeadlightAngle => Lamp.Angle;
        public float HeadlightIntensityScale => Lamp.Intensity;

        private void Awake()
        {
            foreach (var vehicle in vehicles)
            {
                (vehicle.impactResistance, vehicle.trafficPush) = vehicle.id switch
                {
                    "roadbreaker" => (.12f, .06f),
                    "apex" => (.06f, .04f),
                    "suv" => (.38f, .32f),
                    "pickup" => (.47f, .45f),
                    "hauler" => (.66f, .71f),
                    "monster" => (.73f, 1f),
                    _ => (vehicle.impactResistance, vehicle.trafficPush)
                };
            }

            if (Array.Find(vehicles, vehicle => vehicle.id == "roadbreaker") is { } coupe &&
                Array.Find(vehicles, vehicle => vehicle.id == "apex") is { } roadster &&
                Array.Find(vehicles, vehicle => vehicle.id == "pickup") is { } truck)
            {
                var police = coupe.Variant("police", "ELYSIAN INTERCEPTOR", "PURSUIT CRUISER",
                    "Quick turn-in, a reinforced push bar and a low-profile lightbar.");
                police.mass = 1630f;
                police.topSpeed = 216f;
                police.acceleration = 8.4f;
                police.steering = 33f;
                police.spring = 49000f;
                police.damper = 6200f;
                police.colliderCenter.z += .095f;
                police.colliderSize.z += .19f;
                police.impactResistance = .34f;
                police.trafficPush = .26f;

                var velocity = roadster.Variant("velocity", "VELOCITY RS", "TRACK SPECIAL",
                    "A light, fast roadster with a rear wing and a close-ratio gearbox.");
                velocity.mass = 1220f;
                velocity.topSpeed = 252f;
                velocity.acceleration = 10.8f;
                velocity.steering = 32f;
                velocity.spring = 47000f;
                velocity.damper = 6000f;
                velocity.impactResistance = .05f;
                velocity.trafficPush = .03f;

                var bulwark = truck.Variant("bulwark", "BULWARK XR", "ARMORED 4×4",
                    "A reinforced truck with a brush guard, recovery gear and all-wheel drive.");
                bulwark.mass = 3220f;
                bulwark.topSpeed = 162f;
                bulwark.acceleration = 6.4f;
                bulwark.steering = 30f;
                bulwark.spring *= 1.30f;
                bulwark.damper *= 1.23f;
                bulwark.colliderCenter.z += .10f;
                bulwark.colliderSize.z += .20f;
                bulwark.impactResistance = .86f;
                bulwark.trafficPush = .82f;

                foreach (var variant in new[] { police, velocity, bulwark })
                {
                    if (Array.Exists(vehicles, vehicle => vehicle.id == variant.id)) continue;
                    Array.Resize(ref vehicles, vehicles.Length + 1);
                    vehicles[^1] = variant;
                }
            }
        }

        public bool TryGetHeadlightLenses(out Vector3 left, out Vector3 right)
        {
            left = right = Vector3.zero;
            if (_upgradeVisuals == null) return false;
            _upgradeVisuals.LampPositions(out left, out right);
            return true;
        }

        public int GetUpgrade(GarageUpgrade upgrade)
        {
            var index = (int)upgrade;
            return index >= 0 && index < _upgrades.Length ? _upgrades[index] : 0;
        }

        public int GetPaint(int fallback) => Current == null ? fallback : PlayerPrefs.GetInt(GarageCustomization.PaintKey(Current.id), fallback);

        public void RememberPaint(int paint)
        {
            if (Current == null) return;
            PlayerPrefs.SetInt(GarageCustomization.PaintKey(Current.id), paint);
            PlayerPrefs.Save();
        }

        public bool SetUpgrade(GarageUpgrade upgrade, int option)
        {
            if (Current == null || _upgradeVisuals == null) return false;
            var count = GarageCustomization.Options(Current.id, upgrade).Count;
            if (option < 0 || option >= count || option == GetUpgrade(upgrade)) return false;
            _upgrades[(int)upgrade] = option;
            PlayerPrefs.SetInt(GarageCustomization.UpgradeKey(Current.id, upgrade), option);
            PlayerPrefs.Save();
            switch (upgrade)
            {
                case GarageUpgrade.Wheels:
                case GarageUpgrade.RimFinish:
                case GarageUpgrade.Sidewalls:
                    ApplyWheelOptions();
                    break;
                case GarageUpgrade.Headlights:
                    _upgradeVisuals.ApplyHeadlights(option);
                    ApplyAuxiliaryLights();
                    break;
                case GarageUpgrade.AuxiliaryLights:
                    ApplyAuxiliaryLights();
                    break;
                case GarageUpgrade.BodyKit:
                    _upgradeVisuals.ApplyBodyKit(option);
                    break;
                case GarageUpgrade.WindowTint:
                    _upgradeVisuals.ApplyWindows(option);
                    break;
            }
            return true;
        }

        public void Select(int index)
        {
            if (vehicles.Length == 0) return;
            SelectedIndex = Mathf.Clamp(index, 0, vehicles.Length - 1);
            var spec = Current;
            var controller = GetComponent<ArcadeVehicleController>();
            var scoring = GetComponent<CrashScoring>();
            if (visualRoot != null) { visualRoot.gameObject.SetActive(false); Destroy(visualRoot.gameObject); }
            visualRoot = Instantiate(spec.visualPrefab, transform, false).transform;
            visualRoot.name = "Selected vehicle / " + spec.id;
            var shape = GetComponent<BoxCollider>();
            shape.center = spec.colliderCenter;
            shape.size = spec.colliderSize;
            var wheels = new[] { controller.frontLeftCollider, controller.frontRightCollider, controller.rearLeftCollider, controller.rearRightCollider };
            var names = new[] { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };
            var visuals = new Transform[4];
            for (var i = 0; i < wheels.Length; i++)
            {
                wheels[i].transform.localPosition = spec.wheelCenters[i];
                wheels[i].radius = spec.wheelRadius;
                wheels[i].suspensionDistance = spec.suspension;
                wheels[i].mass = spec.mass > 3000f ? 90f : 42f;
                var spring = wheels[i].suspensionSpring;
                spring.spring = spec.spring; spring.damper = spec.damper; spring.targetPosition = 0.46f;
                wheels[i].suspensionSpring = spring;
                foreach (var child in visualRoot.GetComponentsInChildren<Transform>(true)) if (child.name == names[i]) { visuals[i] = child; break; }
            }
            controller.frontLeftVisual = visuals[0]; controller.frontRightVisual = visuals[1];
            controller.rearLeftVisual = visuals[2]; controller.rearRightVisual = visuals[3];
            controller.ConfigureVehicle(spec);
            var details = visualRoot.gameObject.AddComponent<VehicleDetailKit>();
            details.Fit(transform, spec);
            var shell = GetComponent<CrashCarShellAnimator>();
            if (shell != null)
            {
                shell.BindShell(visualRoot, details);
                for (var i = 0; i < shell.tailLightGlows.Length; i++)
                    if (shell.tailLightGlows[i] != null) shell.tailLightGlows[i].transform.localPosition =
                        new Vector3((i == 0 ? -1 : 1) * spec.size.x * .28f, Mathf.Max(.6f,spec.size.y*.43f), -spec.size.z*.49f);
            }
            var damage = visualRoot.GetComponent<CrashDamageVisual>() ?? visualRoot.gameObject.AddComponent<CrashDamageVisual>();
            damage.scoring = scoring;
            var cracks = new System.Collections.Generic.List<Renderer>();
            foreach (var child in visualRoot.GetComponentsInChildren<Transform>(true))
                if (child.name.StartsWith("Damage_Windshield_Crack", StringComparison.Ordinal))
                    foreach (var renderer in child.GetComponentsInChildren<Renderer>(true)) { renderer.enabled = false; cracks.Add(renderer); }
            damage.windshieldCracks = cracks.ToArray();
            _upgradeVisuals = visualRoot.gameObject.AddComponent<GarageUpgradeVisuals>();
            _upgradeVisuals.Bind(transform, spec, visuals);
            LoadUpgrades(spec.id);
            VehicleCabinOccupant.InstallPlayer(transform, spec.id);
            controller.ResetForRelaunch(0f);
        }

        private void LoadUpgrades(string id)
        {
            for (var i = 0; i < _upgrades.Length; i++)
            {
                var part = (GarageUpgrade)i;
                var count = GarageCustomization.Options(id, part).Count;
                _upgrades[i] = Mathf.Clamp(PlayerPrefs.GetInt(GarageCustomization.UpgradeKey(id, part)), 0, count - 1);
            }
            ApplyWheelOptions();
            _upgradeVisuals.ApplyHeadlights(GetUpgrade(GarageUpgrade.Headlights));
            ApplyAuxiliaryLights();
            _upgradeVisuals.ApplyBodyKit(GetUpgrade(GarageUpgrade.BodyKit));
            _upgradeVisuals.ApplyWindows(GetUpgrade(GarageUpgrade.WindowTint));
        }

        private void ApplyWheelOptions() => _upgradeVisuals.ApplyWheels(
            GetUpgrade(GarageUpgrade.Wheels), GetUpgrade(GarageUpgrade.RimFinish), GetUpgrade(GarageUpgrade.Sidewalls));

        private void ApplyAuxiliaryLights() => _upgradeVisuals.ApplyAuxiliary(
            GetUpgrade(GarageUpgrade.AuxiliaryLights), GetUpgrade(GarageUpgrade.Headlights));
    }
}
