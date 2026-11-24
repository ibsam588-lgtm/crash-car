using UnityEngine;

namespace CarCrash
{
    public enum VehicleImpactSurface
    {
        Default,
        Metal,
        Glass,
        Wood,
        Stone,
        Plastic
    }

    [DisallowMultipleComponent]
    public sealed class VehicleImpactMaterial : MonoBehaviour
    {
        public VehicleImpactSurface surface;
    }
}
