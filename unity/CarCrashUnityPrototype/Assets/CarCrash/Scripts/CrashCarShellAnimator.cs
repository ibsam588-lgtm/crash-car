using UnityEngine;

namespace CarCrash
{
    public sealed class CrashCarShellAnimator : MonoBehaviour
    {
        public ArcadeVehicleController vehicle;
        public Transform visibleShell;
        public Transform[] spinningWheels = System.Array.Empty<Transform>();
        public Renderer[] tailLightRenderers = System.Array.Empty<Renderer>();
        public Light[] tailLightGlows = System.Array.Empty<Light>();

        public float wheelSpinDegreesPerKmh = 13.5f;
        public float shellLeanDegrees = 2.6f;
        public float motionBob = 0.018f;

        private Vector3 _shellBasePosition;
        private Quaternion _shellBaseRotation;
        private Quaternion[] _wheelBaseRotations = System.Array.Empty<Quaternion>();
        private float _wheelSpin;

        public void BindShell(Transform shell)
        {
            visibleShell = shell;
            _shellBasePosition = shell.localPosition;
            _shellBaseRotation = shell.localRotation;
            var lamps = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in shell.GetComponentsInChildren<Renderer>())
                if (renderer.name.Contains("Taillight") || renderer.name.Contains("TailLamp")) lamps.Add(renderer);
            tailLightRenderers = lamps.ToArray();
            spinningWheels = System.Array.Empty<Transform>();
        }

        private void Awake()
        {
            if (visibleShell != null)
            {
                _shellBasePosition = visibleShell.localPosition;
                _shellBaseRotation = visibleShell.localRotation;
            }

            _wheelBaseRotations = new Quaternion[spinningWheels.Length];
            for (var i = 0; i < spinningWheels.Length; i++)
            {
                _wheelBaseRotations[i] = spinningWheels[i] != null ? spinningWheels[i].localRotation : Quaternion.identity;
            }
        }

        private void Update()
        {
            if (vehicle == null)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            var speed = Mathf.Max(vehicle.SpeedKmh, Mathf.Abs(vehicle.CurrentThrottle) * 35f);
            _wheelSpin -= speed * wheelSpinDegreesPerKmh * dt;

            for (var i = 0; i < spinningWheels.Length; i++)
            {
                if (spinningWheels[i] == null)
                {
                    continue;
                }

                spinningWheels[i].localRotation = _wheelBaseRotations[i] * Quaternion.Euler(0f, 0f, _wheelSpin);
            }

            if (visibleShell != null)
            {
                var speed01 = Mathf.Clamp01(vehicle.SpeedKmh / 160f);
                var bob = Mathf.Sin(Time.unscaledTime * Mathf.Lerp(8f, 21f, speed01)) * motionBob * speed01;
                visibleShell.localPosition = _shellBasePosition + new Vector3(vehicle.CurrentSteer * 0.035f, bob, 0f);
                visibleShell.localRotation = _shellBaseRotation * Quaternion.Euler(0f, 0f, -vehicle.CurrentSteer * shellLeanDegrees);
            }

            var brakeOrBoost = Mathf.Clamp01(vehicle.CurrentBrake + (vehicle.BoostActive ? 0.45f : 0f));
            var pulse = 0.78f + Mathf.Sin(Time.unscaledTime * 28f) * 0.10f;
            var lightAmount = Mathf.Lerp(0.45f, pulse, brakeOrBoost);
            var color = Color.Lerp(new Color(1f, 0.04f, 0.02f, 0.48f), new Color(1f, 0.08f, 0.025f, 0.92f), lightAmount);

            foreach (var renderer in tailLightRenderers)
            {
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }

            foreach (var glow in tailLightGlows)
            {
                if (glow != null)
                {
                    glow.intensity = Mathf.Lerp(0.08f, 0.65f, lightAmount);
                }
            }
        }
    }
}
