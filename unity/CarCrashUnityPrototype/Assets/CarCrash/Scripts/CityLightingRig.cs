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
        private const int StreetLightCount = 3;
        private const float StreetLightReach = 54f;

        private static CityLightingRig _active;
        private readonly List<(Light light, Color color, float intensity)> _dayLights = new();
        private Material _daySky, _nightSky;
        private Color _dayFog;
        private float _dayDensity, _dayReflection;
        private SphericalHarmonicsL2 _dayAmbient, _nightAmbient;
        private ArcadeVehicleController _vehicle;
        private VehicleGarage _garage;
        private readonly Light[] _headlights = new Light[2];
        private readonly Light[] _streetLights = new Light[StreetLightCount];
        private readonly BreakableCityProp[] _nearbyLamps = new BreakableCityProp[StreetLightCount];
        private readonly float[] _lampDistances = new float[StreetLightCount];
        private readonly List<BreakableCityProp> _streetFixtures = new();
        private float _nextStreetLightUpdate;

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
            _nightAmbient.AddAmbientLight(new Color(.235f, .255f, .32f));
            _nightAmbient.AddDirectionalLight(Vector3.up, new Color(.55f, .63f, .82f), .38f);
            var shader = Resources.Load<Shader>("ElysianNightSky");
            if (_nightSky == null && shader != null && shader.isSupported)
                _nightSky = new Material(shader) { name = "Elysian night sky" };
            else if (_nightSky == null && _daySky != null && _daySky.HasProperty("_Exposure"))
            {
                _nightSky = new Material(_daySky) { name = "Elysian night sky fallback" };
                _nightSky.SetFloat("_Exposure", _daySky.GetFloat("_Exposure") * .24f);
            }
            Apply(NightEnabled);
        }

        private void Start()
        {
            _vehicle = FindFirstObjectByType<CarCrashHudInput>()?.vehicle;
            var signals = GetComponent<CityTrafficSignals>() ?? gameObject.AddComponent<CityTrafficSignals>();
            signals.Initialize(_vehicle != null ? _vehicle.transform : null);
            if (_vehicle == null) return;
            _garage = _vehicle.GetComponent<VehicleGarage>();
            _headlights[0] = CreateSpot("Night driving headlights", _vehicle.transform, LightRenderMode.ForcePixel);
            _headlights[1] = CreateSpot("Passenger-side night headlight", _vehicle.transform, LightRenderMode.ForcePixel);
            foreach (var prop in GetComponentsInChildren<BreakableCityProp>())
                if (prop.name.StartsWith("Breakaway streetlight", System.StringComparison.Ordinal)) _streetFixtures.Add(prop);
            for (var index = 0; index < _streetLights.Length; index++)
            {
                var light = CreateSpot($"Night road lighting {index + 1}", transform, LightRenderMode.Auto);
                light.color = new Color(1f, .83f, .67f);
                light.range = 18f;
                light.spotAngle = 108f;
                light.innerSpotAngle = 51f;
                light.intensity = 2.3f;
                _streetLights[index] = light;
            }
            SetNightLights(NightEnabled);
        }

        private static Light CreateSpot(string name, Transform parent, LightRenderMode mode)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.type = LightType.Spot;
            light.shadows = LightShadows.None;
            light.renderMode = mode;
            light.enabled = false;
            return light;
        }

        private void LateUpdate()
        {
            if (_headlights[0] == null || !_headlights[0].enabled) return;
            PositionHeadlights();
            if (Time.unscaledTime < _nextStreetLightUpdate) return;
            _nextStreetLightUpdate = Time.unscaledTime + .22f;
            UpdateStreetLights();
        }

        private void PositionHeadlights()
        {
            var car = _garage != null ? _garage.Current : null;
            var height = car != null ? car.size.y : 1.3f;
            var left = new Vector3(car != null ? -car.size.x * .34f : -.68f,
                Mathf.Clamp(height * .53f, .52f, 1.23f), car != null ? car.size.z * .49f : 2.28f);
            var right = new Vector3(-left.x, left.y, left.z);
            if (_garage != null && _garage.TryGetHeadlightLenses(out var fittedLeft, out var fittedRight))
            {
                left = fittedLeft;
                right = fittedRight;
            }
            AimHeadlight(_headlights[0], left);
            AimHeadlight(_headlights[1], right);
        }

        private void AimHeadlight(Light beam, Vector3 lens)
        {
            var angle = _garage != null ? _garage.HeadlightAngle : 56f;
            beam.transform.localPosition = lens + Vector3.forward * .09f;
            beam.transform.localRotation = Quaternion.Euler(Mathf.Clamp(lens.y * 4.6f, 2.8f, 6.1f), 0f, 0f);
            beam.color = _garage != null ? _garage.HeadlightColor : new Color(.87f, .94f, 1f);
            beam.range = _garage != null ? _garage.HeadlightRange : 48f;
            beam.spotAngle = angle;
            beam.innerSpotAngle = angle * .55f;
            beam.intensity = (_garage != null ? _garage.HeadlightIntensityScale : 1f) * 1.92f;
        }

        private void UpdateStreetLights()
        {
            var position = _vehicle.transform.position;
            var heading = _vehicle.transform.forward;
            for (var index = 0; index < _nearbyLamps.Length; index++)
            {
                _nearbyLamps[index] = null;
                _lampDistances[index] = float.PositiveInfinity;
            }
            foreach (var fixture in _streetFixtures)
            {
                if (fixture == null || fixture.Broken || !fixture.gameObject.activeInHierarchy) continue;
                var delta = fixture.transform.position - position;
                var distance = delta.sqrMagnitude;
                if (distance > StreetLightReach * StreetLightReach) continue;
                var priority = distance + Mathf.Max(0f, -Vector3.Dot(heading, delta)) * 9f;
                for (var index = 0; index < _nearbyLamps.Length; index++)
                {
                    if (priority >= _lampDistances[index]) continue;
                    for (var shift = _nearbyLamps.Length - 1; shift > index; shift--)
                    {
                        _nearbyLamps[shift] = _nearbyLamps[shift - 1];
                        _lampDistances[shift] = _lampDistances[shift - 1];
                    }
                    _nearbyLamps[index] = fixture;
                    _lampDistances[index] = priority;
                    break;
                }
            }
            for (var index = 0; index < _streetLights.Length; index++)
            {
                var light = _streetLights[index];
                var lamp = _nearbyLamps[index];
                if (light == null) continue;
                light.enabled = lamp != null;
                if (lamp == null) continue;
                var foot = lamp.transform.position;
                var avenue = CityRoadNetwork.Nearest(CityRoadNetwork.AvenueX, foot.x);
                var inward = Mathf.Sign(avenue - foot.x);
                light.transform.SetPositionAndRotation(foot + new Vector3(inward * 2.15f, 6.23f, 0f),
                    Quaternion.LookRotation(new Vector3(inward * .35f, -1f, 0f), Vector3.forward));
            }
        }

        private void Apply(bool night)
        {
            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.ambientProbe = night ? _nightAmbient : _dayAmbient;
            RenderSettings.skybox = night && _nightSky != null ? _nightSky : _daySky;
            RenderSettings.fogColor = night ? new Color(.128f, .174f, .252f) : _dayFog;
            RenderSettings.fogDensity = night ? .0037f : _dayDensity;
            RenderSettings.reflectionIntensity = night ? _dayReflection * .57f : _dayReflection;
            foreach (var state in _dayLights)
            {
                if (state.light == null) continue;
                state.light.color = night ? new Color(.73f, .81f, 1f) : state.color;
                state.light.intensity = night ? state.intensity * .44f : state.intensity;
            }
            SetNightLights(night);
        }

        private void SetNightLights(bool night)
        {
            foreach (var beam in _headlights) if (beam != null) beam.enabled = night;
            if (_headlights[0] == null) return;
            if (night)
            {
                PositionHeadlights();
                UpdateStreetLights();
            }
            else foreach (var light in _streetLights) if (light != null) light.enabled = false;
        }

        private void OnDisable()
        {
            if (_active == this)
            {
                Apply(false);
                _active = null;
            }
        }

        private void OnDestroy()
        {
            if (_nightSky != null) Destroy(_nightSky);
            foreach (var beam in _headlights) if (beam != null) Destroy(beam.gameObject);
        }
    }
}
