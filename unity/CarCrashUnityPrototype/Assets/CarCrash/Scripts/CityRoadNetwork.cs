using UnityEngine;

namespace CarCrash
{
    // Shared world coordinates keep the city map aligned to the real street geometry.
    public sealed class CityRoadNetwork : MonoBehaviour
    {
        public static readonly float[] CrossStreetZ = { -110f, -58f, 35f, 108f, 187f, 266f, 315f };
        public static readonly float[] AvenueX = { -54f, 0f, 54f };

        public static void NearestRoadPose(Vector3 point, Vector3 forward, out Vector3 position, out Quaternion rotation)
        {
            var avenue = AvenueX[0];
            foreach (var x in AvenueX) if (Mathf.Abs(x - point.x) < Mathf.Abs(avenue - point.x)) avenue = x;
            var crossing = CrossStreetZ[0];
            foreach (var z in CrossStreetZ) if (Mathf.Abs(z - point.z) < Mathf.Abs(crossing - point.z)) crossing = z;
            if (Mathf.Abs(avenue - point.x) <= Mathf.Abs(crossing - point.z))
            {
                var direction = forward.z < 0f ? -1f : 1f;
                position = new Vector3(avenue + direction * 2.3f, 0.15f, Mathf.Clamp(point.z, -110f, 310f));
                rotation = Quaternion.LookRotation(Vector3.forward * direction);
            }
            else
            {
                var direction = forward.x < 0f ? -1f : 1f;
                position = new Vector3(Mathf.Clamp(point.x, -82f, 82f), 0.15f, crossing - direction * 2.3f);
                rotation = Quaternion.LookRotation(Vector3.right * direction);
            }
        }
    }
}
