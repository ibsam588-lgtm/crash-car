using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    internal static class PremiumVehicleFleet
    {
        private static readonly string[] WheelNames = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };

        internal static GarageVehicle[] Append(GarageVehicle[] standards)
        {
            var fleet = new List<GarageVehicle>(standards);
            Add(fleet, "velocity", "Velocity RS", "MID-ENGINE TRACK COUPE",
                "Mid-engine balance, a carbon aerofoil and the fastest straight-line setup.",
                new(2.12f, 1.20f, 4.64f), new(-.34f, 1.027f, .16f), 3, .354f, 1280f, 264f, 11.4f, 32f,
                false, .15f, .08f, .04f, new(.74f, .88f, 1f));
            Add(fleet, "bulwark", "Bulwark XR", "ARMORED TROPHY PICKUP",
                "A full-width brush guard, stepped bed cage and low-range traction carry it through heavier hits.",
                new(2.32f, 2.02f, 5.56f), new(-.43f, 1.75f, .49f), 0, .457f, 3260f, 166f, 6.5f, 30f,
                true, .25f, .88f, .83f, new(1f, .92f, .76f));
            Add(fleet, "solstice", "Solstice GT", "FRONT-ENGINE TOURING FASTBACK",
                "The long hood, compact cabin and tall gearing favor high-speed sweepers.",
                new(2.02f, 1.31f, 4.86f), new(-.34f, 1.119f, .12f), 1, .359f, 1540f, 244f, 9.2f, 29f,
                false, .18f, .29f, .14f, new(1f, .98f, .89f));
            Add(fleet, "switchback", "Switchback R", "AWD RALLY HATCH",
                "A short wheelbase and supple suspension make it quick through tight corners and over rough streets.",
                new(1.97f, 1.56f, 4.18f), new(-.35f, 1.315f, .18f), 4, .369f, 1380f, 223f, 10.2f, 37f,
                true, .22f, .36f, .22f, new(.86f, .95f, 1f));
            Add(fleet, "undertow", "Undertow E", "ELECTRIC SPORT SEDAN",
                "Instant all-wheel torque, a low battery floor and full-width running lights.",
                new(2.06f, 1.43f, 5.02f), new(-.37f, 1.254f, .30f), 2, .371f, 1820f, 232f, 12.3f, 30f,
                true, .17f, .47f, .34f, new(.69f, .97f, 1f));
            Add(fleet, "monarch", "Monarch V", "PERFORMANCE SHOOTING BRAKE",
                "The long roof and wide track combine wagon proportions with strong passing power.",
                new(2.09f, 1.51f, 5.05f), new(-.38f, 1.315f, .25f), 0, .373f, 1720f, 239f, 8.2f, 34f,
                true, .19f, .40f, .31f, new(.94f, .98f, 1f));
            return fleet.ToArray();
        }

        private static void Add(List<GarageVehicle> fleet, string id, string name, string category, string description,
            Vector3 dimensions, Vector3 cabinHead, int paint, float radius, float mass, float speed, float acceleration,
            float steering, bool allWheelDrive, float suspension, float resistance, float trafficPush, Color headlight)
        {
            if (fleet.Exists(spec => spec.id == id)) return;
            var prefab = Resources.Load<GameObject>("PremiumGarage/" + id);
            if (prefab == null)
            {
                Debug.LogError("Missing premium garage model: " + id);
                return;
            }

            var pivots = prefab.GetComponentsInChildren<Transform>(true);
            var centers = new Vector3[4];
            for (var index = 0; index < WheelNames.Length; index++)
            {
                var pivot = Array.Find(pivots, child => child.name == WheelNames[index]);
                if (pivot == null)
                {
                    Debug.LogError("Missing wheel pivot " + WheelNames[index] + " on premium garage model: " + id);
                    return;
                }
                centers[index] = pivot.position;
            }

            const float hullBottom = .35f;
            var hullTop = dimensions.y * .91f;
            fleet.Add(new GarageVehicle
            {
                id = id, displayName = name.ToUpperInvariant(), shortName = name, category = category,
                description = description, visualPrefab = prefab, size = dimensions, premium = true,
                colliderCenter = new Vector3(0f, (hullTop + hullBottom) * .5f, 0f),
                colliderSize = new Vector3(dimensions.x * .89f, hullTop - hullBottom, dimensions.z * .93f),
                wheelCenters = centers, wheelRadius = radius, mass = mass, topSpeed = speed,
                acceleration = acceleration, steering = steering, reverseSpeed = id == "bulwark" ? 26f : 32f,
                allWheelDrive = allWheelDrive, suspension = suspension,
                spring = mass * (id == "switchback" ? 28f : 33f), damper = mass * 3.8f,
                impactResistance = resistance, trafficPush = trafficPush,
                defaultPaint = paint, driverHead = cabinHead, stockHeadlight = headlight
            });
        }
    }
}
