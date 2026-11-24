using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    public static class VehicleCabinOccupant
    {
        private const string DriverName = "Cabin driver";
        private const float CabinShade = .79f;
        private static GameObject _sam;

        public static void InstallPlayer(Transform chassis, string vehicleId)
        {
            var driver = chassis.Find(DriverName) ?? Create(chassis, false);
            if (driver != null)
                driver.SetLocalPositionAndRotation(HeadPosition(vehicleId), Quaternion.identity);
        }

        public static void InstallTraffic(Transform chassis, Vector3 velocity, float chassisMass)
        {
            if (chassis.Find(DriverName) != null) return;
            var driver = Create(chassis, true);
            if (driver == null) return;

            // The traffic builder uses each road car's stock mass for all of its liveries.
            var vehicleId = chassisMass >= 2200f ? "pickup" : chassisMass >= 1750f ? "suv" : chassisMass >= 1440f ? "apex" : "roadbreaker";
            var heading = Vector3.ProjectOnPlane(velocity, Vector3.up);
            var localHeading = heading.sqrMagnitude > .01f ? chassis.InverseTransformDirection(heading.normalized) : Vector3.forward;
            var rotation = Quaternion.LookRotation(localHeading);
            driver.SetLocalPositionAndRotation(rotation * HeadPosition(vehicleId), rotation);
        }

        // Each model uses a head-centered origin. Keep the face within the glass and the
        // shoulders behind the dashboard. The monster shares the pickup cab 1.2 m higher.
        private static Vector3 HeadPosition(string vehicleId) => vehicleId switch
        {
            "apex" or "velocity" => new(-.33f, .99f, .03f),
            "suv" => new(-.413f, 1.47f, .29f),
            "pickup" or "bulwark" => new(-.436f, 1.59f, .59f),
            "hauler" => new(-.494f, 2.06f, 2.54f),
            "monster" => new(-.436f, 2.79f, .59f),
            _ => new(-.35f, 1.095f, .09f)
        };

        private static Transform Create(Transform chassis, bool traffic)
        {
            if (_sam == null) _sam = Resources.Load<GameObject>("CabinDrivers/Sam");
            if (_sam == null) return null;

            var seat = new GameObject(DriverName).transform;
            seat.SetParent(chassis, false);
            // Keep the model import's orientation under the seat anchor.
            var driver = Object.Instantiate(_sam, seat, false);
            var baseColor = Shader.PropertyToID("_Color");
            var block = new MaterialPropertyBlock();
            var renderers = driver.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null || !material.HasProperty(baseColor)) continue;
                    var color = material.GetColor(baseColor);
                    block.SetColor(baseColor, new Color(color.r * CabinShade, color.g * CabinShade, color.b * CabinShade, color.a));
                    renderer.SetPropertyBlock(block, index);
                }
            }
            if (traffic)
            {
                // A distant occupant is sub-pixel through the glass. Save the extra material passes.
                var detail = seat.gameObject.AddComponent<LODGroup>();
                detail.SetLODs(new[] { new LOD(.027f, renderers) });
                detail.RecalculateBounds();
            }
            return seat;
        }
    }
}
