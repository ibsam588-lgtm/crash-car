using UnityEngine;

namespace CarCrash
{
    public sealed class CityBuildingProfile : MonoBehaviour
    {
        public string architecture, shop;
        public int family, plot, side;
        public bool hasInterior;
        public Vector3 size, attendantLocalPosition;
        public Vector3 egressLocalPosition;
        public bool Evacuating { get; private set; }

        public void AlertOccupants() => Evacuating=true;
    }
}
