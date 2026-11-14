using UnityEngine;

namespace CarCrash
{
    public sealed class CityBuildingProfile : MonoBehaviour
    {
        public string architecture, shop;
        public int family, plot, side;
        public bool hasInterior;
        public Vector3 size, attendantLocalPosition;
    }
}
