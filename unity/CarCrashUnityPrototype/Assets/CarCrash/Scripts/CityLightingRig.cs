using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    [DefaultExecutionOrder(-1000)]
    public sealed class CityLightingRig : MonoBehaviour
    {
        public const string NightPreferenceKey = "CarCrash.NightMode";
        public static bool NightEnabled => PlayerPrefs.GetInt(NightPreferenceKey, 0) != 0;

        private static CityLightingRig _active;
        private readonly List<(Light light, Color color, float intensity)> _dayLights = new();
        private Material _daySky, _nightSky;
        private Color _dayFog;
        private float _dayDensity, _dayReflection;
        private SphericalHarmonicsL2 _dayAmbient, _nightAmbient;
        private ArcadeVehicleController _vehicle;
        private VehicleGarage _garage;
        private Light _headlight;

        public static void SetNightEnabled(bool enabled)
        {
            if (NightEnabled == enabled) return;
            PlayerPrefs.SetInt(NightPreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (_active != null) _active.Apply(enabled);
        }

        private void OnEnable()
        {
            // Procedural scenes have no baked lighting asset. Supply a real ambient probe
            // explicitly so the shaded street and metallic bodywork also light in WebGL.
            _active = this;
            _daySky = RenderSettings.skybox;
            _dayFog = RenderSettings.fogColor;
            _dayDensity = RenderSettings.fogDensity;
            _dayReflection = RenderSettings.reflectionIntensity;
            _dayLights.Clear();
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) _dayLights.Add((light, light.color, light.intensity));

            _dayAmbient = new SphericalHarmonicsL2();
            _dayAmbient.AddAmbientLight(new Color(.29f, .33f, .40f));
            _dayAmbient.AddDirectionalLight(Vector3.up, new Color(.51f, .60f, .73f), .32f);
            _nightAmbient = new SphericalHarmonicsL2();
            _nightAmbient.AddAmbientLight(new Color(.12f, .15f, .23f));
            _nightAmbient.AddDirectionalLight(Vector3.up, new Color(.38f, .47f, .71f), .22f);
            var shader = Resources.Load<Shader>("ElysianNightSky");
            if (shader != null && shader.isSupported)
                _nightSky = new Material(shader) { name = "Elysian night sky" };
            else if (_daySky != null && _daySky.HasProperty("_Exposure"))
            {
                _nightSky = new Material(_daySky) { name = "Elysian night sky fallback" };
                _nightSky.SetFloat("_Exposure", _daySky.GetFloat("_Exposure") * .16f);
            }
            Apply(NightEnabled);
        }

        private void Start()
        {
            _vehicle = FindFirstObjectByType<CarCrashHudInput>()?.vehicle;
            if (_vehicle == null) return;
            _garage = _vehicle.GetComponent<VehicleGarage>();
            var lamp = new GameObject("Night driving headlights");
            lamp.transform.SetParent(_vehicle.transform, false);
            lamp.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);
            _headlight = lamp.AddComponent<Light>();
            _headlight.type = LightType.Spot;
            _headlight.range = 48f;
            _headlight.spotAngle = 56f;
            _headlight.color = new Color(.87f, .94f, 1f);
            _headlight.intensity = 4.6f;
            _headlight.shadows = LightShadows.None;
            _headlight.enabled = NightEnabled;
        }

        private void LateUpdate()
        {
            if (_headlight == null || !_headlight.enabled) return;
            var car = _garage != null ? _garage.Current : null;
            var width = car != null ? car.size.z : 4.65f;
            var height = car != null ? car.size.y : 1.3f;
            _headlight.transform.localPosition = new Vector3(0f, Mathf.Clamp(height * .57f, .60f, 1.22f), width * .51f);
        }

        private void Apply(bool night)
        {
            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.ambientProbe = night ? _nightAmbient : _dayAmbient;
            RenderSettings.skybox = night && _nightSky != null ? _nightSky : _daySky;
            RenderSettings.fogColor = night ? new Color(.090f, .131f, .206f) : _dayFog;
            RenderSettings.fogDensity = night ? .0046f : _dayDensity;
            RenderSettings.reflectionIntensity = night ? _dayReflection * .39f : _dayReflection;
            foreach (var state in _dayLights)
            {
                if (state.light == null) continue;
                state.light.color = night ? new Color(.58f, .70f, 1f) : state.color;
                state.light.intensity = night ? state.intensity * .26f : state.intensity;
            }
            if (_headlight != null) _headlight.enabled = night;
        }

        private void OnDisable()
        {
            if (_active == this)
            {
                Apply(false);
                _active = null;
            }
            if (_nightSky != null) Destroy(_nightSky);
            if (_headlight != null) Destroy(_headlight.gameObject);
        }
    }
}
