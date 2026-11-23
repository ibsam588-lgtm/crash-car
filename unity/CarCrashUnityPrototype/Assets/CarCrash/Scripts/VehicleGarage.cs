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
    }

    [RequireComponent(typeof(ArcadeVehicleController))]
    public sealed class VehicleGarage : MonoBehaviour
    {
        public GarageVehicle[] vehicles = Array.Empty<GarageVehicle>();
        public Transform visualRoot;
        public int SelectedIndex { get; private set; }
        public GarageVehicle Current => vehicles.Length > 0 ? vehicles[SelectedIndex] : null;

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
            controller.ResetForRelaunch(0f);
        }
    }
}
