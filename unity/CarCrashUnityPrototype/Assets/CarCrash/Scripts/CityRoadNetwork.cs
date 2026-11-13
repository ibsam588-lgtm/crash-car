using UnityEngine;

namespace CarCrash
{
    // Shared world coordinates keep the city map aligned to the real street geometry.
    public sealed class CityRoadNetwork : MonoBehaviour
    {
        public static readonly float[] CrossStreetZ = { -58f, 35f, 108f, 187f, 266f };
        public static readonly float[] AvenueX = { -54f, 0f, 54f };
    }
}
