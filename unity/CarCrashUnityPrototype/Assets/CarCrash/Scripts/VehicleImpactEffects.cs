using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleImpactEffects : MonoBehaviour
    {
        public Material particleMaterial, ringMaterial;
        public int TotalBursts { get; private set; }
        public float LastImpactRealtime { get; private set; } = -10f;
        public float LastImpactKmh { get; private set; }
        public int LiveParticles => (_sparks != null ? _sparks.particleCount : 0) + (_dust != null ? _dust.particleCount : 0) + (_glass != null ? _glass.particleCount : 0);
        private ParticleSystem _sparks, _dust, _glass, _smoke;
        private ArcadeVehicleController _vehicle;
        private CrashScoring _scoring;
        private VehicleAudio _audio;
        private float _nextSmoke;
#if UNITY_ANDROID && !UNITY_EDITOR
        private float _nextHaptic;
#endif
        private readonly Transform[] _rings = new Transform[3];
        private readonly float[] _ringBorn = { -10f, -10f, -10f };
        private readonly float[] _ringSize = new float[3];
        private readonly Vector3[] _ringPosition = new Vector3[3];
        private MaterialPropertyBlock _block;
        private Vector3 _lastPoint;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _vehicle = GetComponent<ArcadeVehicleController>();
            _scoring = GetComponent<CrashScoring>();
            _sparks = CreateEmitter("Impact sparks", 480, 0.58f, false);
            _dust = CreateEmitter("Impact dust splash", 240, 1.45f, true);
            _glass = CreateEmitter("Impact glass spray", 180, 0.95f, false);
            _smoke = CreateEmitter("Tyre and engine smoke", 110, 1.15f, true);
            _audio = GetComponent<VehicleAudio>();
            if (_audio == null) _audio = gameObject.AddComponent<VehicleAudio>();
            var sparksRenderer = _sparks.GetComponent<ParticleSystemRenderer>();
            sparksRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            sparksRenderer.lengthScale = 2.2f;
            sparksRenderer.velocityScale = 0.035f;
            for (var i = 0; i < _rings.Length; i++)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
                ring.name = "Impact ground splash " + i;
                ring.layer = 2;
                ring.GetComponent<Collider>().enabled = false;
                Destroy(ring.GetComponent<Collider>());
                ring.transform.SetParent(transform, true);
                ring.transform.rotation = Quaternion.Euler(90, 0, 0);
                ring.GetComponent<MeshRenderer>().sharedMaterial = ringMaterial;
                ring.SetActive(false);
                _rings[i] = ring.transform;
            }
        }

        private ParticleSystem CreateEmitter(string name, int limit, float life, bool expanding)
        {
            var obj = new GameObject(name);
            obj.SetActive(false);
            obj.transform.SetParent(transform, false);
            var system = obj.AddComponent<ParticleSystem>();
            var main = system.main;
            main.playOnAwake = false; main.loop = true; main.duration = 3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = limit; main.startLifetime = life;
            main.gravityModifier = expanding ? -0.06f : 0.7f;
            var emission = system.emission; emission.enabled = false;
            var shape = system.shape; shape.enabled = false;
            var size = system.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, expanding ? 0.45f : 1f, 1, expanding ? 2.8f : 0.25f));
            var color = system.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0.85f, 0), new GradientAlphaKey(0.65f, 0.55f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
            obj.SetActive(true);
            system.Play();
            return system;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contactCount == 0 || collision.collider.GetComponentInParent<CityPedestrian>() != null) return;
            var speed = collision.relativeVelocity.magnitude * 3.6f;
            if (speed < 4f) return;
            var contact = collision.GetContact(0);
            var closingSpeed = Mathf.Abs(Vector3.Dot(contact.normal, collision.relativeVelocity)) * 3.6f;
            var other = collision.rigidbody;
            var surface = ContactSurface(collision, closingSpeed);
            if (speed < 12f)
            {
                if (closingSpeed >= 4f) _audio.PlayImpact(surface, speed);
                return;
            }
            if (other == null || (other.mass >= 36f && other.GetComponent<TrafficImpactTarget>() == null))
                _scoring?.RegisterSceneryDamage(contact.point, closingSpeed);
            EmitImpact(contact.point, contact.normal, collision.relativeVelocity, speed, surface);
        }

        private static VehicleImpactSurface ContactSurface(Collision collision, float closingSpeed)
        {
            var material = collision.collider.GetComponentInParent<VehicleImpactMaterial>();
            if (material != null)
            {
                var prop = material.GetComponent<BreakableCityProp>();
                if (prop != null && (prop.Broken || !prop.MeetsBreakThreshold(closingSpeed, collision.impulse.magnitude)))
                    return VehicleImpactSurface.Stone;
                return material.surface;
            }

            var target = collision.collider.GetComponentInParent<ImpactCourseTarget>();
            if (target != null) return target.Surface;
            return collision.rigidbody == null ? VehicleImpactSurface.Stone : VehicleImpactSurface.Metal;
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.contactCount == 0 || collision.relativeVelocity.sqrMagnitude < 16f ||
                collision.collider.GetComponentInParent<CityPedestrian>() != null) return;
            var normal = collision.GetContact(0).normal;
            if (Mathf.Abs(normal.y) > .64f) return;
            var speedKmh = Vector3.ProjectOnPlane(collision.relativeVelocity, normal).magnitude * 3.6f;
            if (speedKmh >= 14f) _audio.Scrape(speedKmh);
        }

        public void EmitImpact(Vector3 point, Vector3 normal, Vector3 velocity, float speedKmh, VehicleImpactSurface surface = VehicleImpactSurface.Default)
        {
            if (speedKmh < 12f || (Time.unscaledTime - LastImpactRealtime < 0.17f && Vector3.Distance(point, _lastPoint) < 4f)) return;
            TotalBursts++;
            LastImpactRealtime = Time.unscaledTime;
            LastImpactKmh = speedKmh;
            _lastPoint = point;
            var strength = Mathf.InverseLerp(12f, 135f, speedKmh);
            _audio.PlayImpact(surface, speedKmh);
            if (speedKmh >= 52f) ImpactHaptic(strength);
            var origin = point + normal * 0.12f + Vector3.up * 0.12f;
            var direction = normal.normalized + Vector3.up * 0.6f;
            Emit(_sparks, origin, direction, Mathf.RoundToInt(Mathf.Lerp(24, 170, strength)), 0.035f, 0.075f,
                Mathf.Lerp(6, 19, strength), new Color(1f, 0.68f, 0.18f, 1f), 0.55f);
            Emit(_dust, origin, direction, Mathf.RoundToInt(Mathf.Lerp(12, 62, strength)), 0.35f, 0.95f,
                Mathf.Lerp(1.3f, 4.8f, strength), new Color(0.57f, 0.53f, 0.44f, 0.40f), 1.5f);
            if (surface == VehicleImpactSurface.Glass || speedKmh > 40f)
                Emit(_glass, origin, direction, Mathf.RoundToInt(Mathf.Lerp(8, 65, strength)), 0.025f, 0.10f,
                    Mathf.Lerp(4, 13, strength), new Color(0.66f, 0.87f, 0.97f, 0.9f), 0.9f);
            var slot = TotalBursts % _rings.Length;
            _ringBorn[slot] = Time.time;
            _ringSize[slot] = Mathf.Lerp(2.8f, 7.5f, strength);
            _rings[slot].position = new Vector3(point.x, 0.035f, point.z);
            _ringPosition[slot] = _rings[slot].position;
            _rings[slot].gameObject.SetActive(true);
        }

        private static void Emit(ParticleSystem system, Vector3 position, Vector3 normal, int count, float minSize, float maxSize, float speed, Color color, float life)
        {
            for (var i = 0; i < count; i++)
            {
                var direction = (normal + Random.insideUnitSphere * 1.4f).normalized;
                var emission = new ParticleSystem.EmitParams
                {
                    position = position + Random.insideUnitSphere * 0.18f,
                    velocity = direction * Random.Range(speed * 0.3f, speed),
                    startLifetime = life * Random.Range(0.65f, 1.25f),
                    startSize = Random.Range(minSize, maxSize),
                    startColor = color, rotation = Random.Range(0, 360)
                };
                system.Emit(emission, 1);
            }
        }

        private void LateUpdate()
        {
            if (Time.timeScale > 0f && _vehicle != null && Time.time >= _nextSmoke)
            {
                _nextSmoke = Time.time + .085f;
                if (_vehicle.SpeedKmh > 36f && (Mathf.Abs(_vehicle.CurrentSteer) > .56f || _vehicle.BoostActive))
                {
                    TireSmoke(_vehicle.rearLeftCollider);
                    TireSmoke(_vehicle.rearRightCollider);
                }
                if (_scoring != null && _scoring.Damage > 38f)
                {
                    var hood = transform.TransformPoint(new Vector3(0, GetComponent<BoxCollider>().bounds.size.y * .62f, 1.72f));
                    Emit(_smoke, hood, Vector3.up, 2, .21f, .42f, 1.3f, new Color(.30f,.32f,.32f,.29f), 1.4f);
                }
            }
            for (var i = 0; i < _rings.Length; i++)
            {
                if (_rings[i] == null || !_rings[i].gameObject.activeSelf) continue;
                var age = (Time.time - _ringBorn[i]) / 0.55f;
                if (age >= 1f) { _rings[i].gameObject.SetActive(false); continue; }
                _rings[i].position = _ringPosition[i];
                _rings[i].rotation = Quaternion.Euler(90,0,0);
                _rings[i].localScale = Vector3.one * Mathf.Lerp(0.8f, _ringSize[i], age);
                _block.SetColor("_Color", new Color(0.74f, 0.68f, 0.51f, (1f - age) * 0.22f));
                _rings[i].GetComponent<Renderer>().SetPropertyBlock(_block);
            }
        }

        private void TireSmoke(WheelCollider wheel)
        {
            if (!wheel.GetGroundHit(out var hit) || (Mathf.Abs(hit.sidewaysSlip) < .34f && Mathf.Abs(hit.forwardSlip) < .45f)) return;
            Emit(_smoke, hit.point + Vector3.up * .09f, Vector3.up - transform.forward * .35f, 3, .16f, .28f, 1.7f,
                new Color(.72f,.73f,.71f,.28f), 1.15f);
        }

        private void ImpactHaptic(float strength)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Time.unscaledTime < _nextHaptic) return;
            _nextHaptic = Time.unscaledTime + .55f;
            try
            {
                using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;
                using var type = new AndroidJavaClass("android.os.VibrationEffect");
                using var effect = type.CallStatic<AndroidJavaObject>("createOneShot", (long)Mathf.Lerp(19, 35, strength), (int)Mathf.Lerp(54, 98, strength));
                vibrator.Call("vibrate", effect);
            }
            catch (AndroidJavaException) { }
#endif
        }
    }
}
