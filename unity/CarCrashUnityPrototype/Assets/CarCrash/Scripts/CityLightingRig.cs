using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    [DefaultExecutionOrder(-1000)]
    public sealed class CityLightingRig : MonoBehaviour
    {
        private void OnEnable()
        {
            // Procedural scenes have no baked lighting asset. Supply a real ambient probe
            // explicitly so the shaded street and metallic bodywork also light in WebGL.
            var ambient = new SphericalHarmonicsL2();
            ambient.AddAmbientLight(new Color(0.29f, 0.33f, 0.40f));
            ambient.AddDirectionalLight(Vector3.up, new Color(0.51f, 0.60f, 0.73f), 0.32f);
            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.ambientProbe = ambient;
        }

    }
}
