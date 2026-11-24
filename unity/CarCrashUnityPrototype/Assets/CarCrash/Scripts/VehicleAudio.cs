using System;
using UnityEngine;

namespace CarCrash
{
    [DisallowMultipleComponent]
    public sealed class VehicleAudio : MonoBehaviour
    {
        private const int SampleRate = 24000;
        private const float Tau = Mathf.PI * 2f;
        private readonly AudioClip[] _impacts = new AudioClip[(int)VehicleImpactSurface.Plastic + 1];
        private ArcadeVehicleController _vehicle;
        private CrashScoring _scoring;
        private ElysianSoundtrack _settings;
        private AudioSource _engine;
        private AudioSource _friction;
        private AudioSource _oneShots;
        private AudioClip _engineLoop;
        private AudioClip _tyres;
        private AudioClip _scrape;
        private AudioClip _boost;
        private float _scrapeUntil;
        private float _scrapeStrength;
        private float _nextImpact;
        private bool _boosting;
        private bool _playing;

        private void Awake()
        {
            _vehicle = GetComponent<ArcadeVehicleController>();
            _scoring = GetComponent<CrashScoring>();
            _settings = ElysianSoundtrack.GetOrCreate();
            _engine = CreateSource(true, .08f, 120);
            _friction = CreateSource(true, .19f, 115);
            _oneShots = CreateSource(false, .18f, 85);
            _engineLoop = CreateClip("Player engine", 1f, 9, EngineSample, false);
            _engine.clip = _engineLoop;
            _tyres = CreateClip("Tyre slip", 1.1f, 72, (t, mid, high) =>
                .52f * mid + .30f * high + .11f * Mathf.Sin(Tau * (874f * t + .24f * Mathf.Sin(t * 14f))));
            _scrape = CreateClip("Body scraping", .83f, 132, (t, mid, high) =>
                .42f * mid + .36f * high + .13f * Mathf.Sin(Tau * 634f * t) * Mathf.Sin(Tau * 31f * t) +
                .085f * Mathf.Sin(Tau * 1297f * t));
            _boost = CreateClip("Nitro boost", .79f, 141, (t, mid, high) =>
                (.47f * mid + .39f * high + .25f * Mathf.Sin(Tau * (51f * t + 145f * t * t))) *
                Mathf.Min(1f, t * 36f) * Mathf.Exp(-t * 4.2f));
            for (var i = 1; i < _impacts.Length; i++)
                _impacts[i] = CreateImpact((VehicleImpactSurface)i);
        }

        private AudioSource CreateSource(bool loop, float spatialBlend, int priority)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.minDistance = 4f;
            source.maxDistance = 40f;
            source.priority = priority;
            source.volume = loop ? 0f : 1f;
            return source;
        }

        public void PlayImpact(VehicleImpactSurface surface, float speedKmh)
        {
            if (!isActiveAndEnabled || !_settings.GameplayEffectsAllowed || Time.unscaledTime < _nextImpact) return;
            _playing = true;
            _nextImpact = Time.unscaledTime + .075f;
            var strength = Mathf.InverseLerp(5f, 115f, speedKmh);
            var clip = surface == VehicleImpactSurface.Default ? _impacts[(int)VehicleImpactSurface.Metal] : _impacts[(int)surface];
            _oneShots.pitch = Mathf.Lerp(1.18f, .84f, strength);
            _oneShots.PlayOneShot(clip, Mathf.Lerp(.12f, .79f, strength));
        }

        public void Scrape(float speedKmh)
        {
            _scrapeUntil = Time.unscaledTime + .10f;
            _scrapeStrength = Mathf.InverseLerp(14f, 66f, speedKmh);
        }

        private void LateUpdate()
        {
            var boosting = _vehicle != null && _vehicle.BoostActive;
            var startsBoost = boosting && !_boosting;
            _boosting = boosting;
            if (!_settings.GameplayEffectsAllowed)
            {
                if (_playing) StopAudio();
                return;
            }

            _playing = true;
            if (_vehicle == null) return;

            if (startsBoost)
            {
                _oneShots.pitch = 1f;
                _oneShots.PlayOneShot(_boost, .48f);
            }

            var speedKmh = _vehicle.SpeedKmh;
            var load = Mathf.Abs(_vehicle.CurrentThrottle);
            var roadSpeed = Mathf.Clamp01(speedKmh / Mathf.Max(40f, _vehicle.maxSpeedKmh));
            var revs = _vehicle.Reversing ? Mathf.Clamp01(speedKmh / 40f) : Mathf.Repeat(speedKmh / 39f, 1f);
            var pitch = .87f + .84f * revs + .23f * load + .12f * roadSpeed;
            if (_scoring != null && _scoring.Damage > 55f)
                pitch *= 1f + Mathf.Sin(Time.unscaledTime * 43f) * .035f;

            var response = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 13f);
            _engine.pitch = Mathf.Lerp(_engine.pitch, pitch, response);
            _engine.volume = Mathf.Lerp(_engine.volume, .075f + .19f * load + .085f * roadSpeed, response);
            if (!_engine.isPlaying) _engine.Play();

            var speedFactor = Mathf.InverseLerp(18f, 91f, speedKmh);
            var skid = Mathf.Max(TyreSlip(_vehicle.rearLeftCollider), TyreSlip(_vehicle.rearRightCollider));
            var tyreStrength = speedFactor * Mathf.Clamp01(Mathf.Max(skid, _vehicle.CurrentBrake * .65f));
            var scrapeStrength = Time.unscaledTime < _scrapeUntil ? _scrapeStrength : 0f;
            var useScrape = scrapeStrength > tyreStrength;
            var frictionClip = useScrape ? _scrape : _tyres;
            var frictionVolume = useScrape ? scrapeStrength * .33f : tyreStrength * .28f;
            if (_friction.clip != frictionClip)
            {
                _friction.Stop();
                _friction.clip = frictionClip;
                _friction.volume = 0f;
            }
            _friction.pitch = Mathf.Lerp(.84f, 1.22f, speedFactor);
            _friction.volume = Mathf.Lerp(_friction.volume, frictionVolume, response);
            if (_friction.volume > .012f && !_friction.isPlaying) _friction.Play();
            if (_friction.volume <= .012f && _friction.isPlaying) _friction.Stop();
        }

        private static float TyreSlip(WheelCollider wheel)
        {
            if (wheel == null || !wheel.GetGroundHit(out var contact)) return 0f;
            var slip = Mathf.Max(Mathf.Abs(contact.sidewaysSlip), Mathf.Abs(contact.forwardSlip) * .72f);
            return Mathf.InverseLerp(.33f, 1.6f, slip);
        }

        private static float EngineSample(float time, float mid, float high)
        {
            var pulse = Tau * 42f * time;
            var firing = Mathf.Sin(pulse * 2f + .25f * Mathf.Sin(pulse));
            var growl = .38f * Mathf.Sin(pulse) + .29f * firing + .14f * Mathf.Sin(pulse * 3f) +
                .10f * Mathf.Sin(pulse * 5f) + .068f * Mathf.Sin(pulse * 8f + .12f);
            return .77f * (float)Math.Tanh(growl * (1.4f + .27f * Mathf.Sin(Tau * 7f * time)));
        }

        private static AudioClip CreateImpact(VehicleImpactSurface surface)
        {
            switch (surface)
            {
                case VehicleImpactSurface.Glass:
                    return CreateClip("Glass shattering", .45f, 912, (t, mid, high) =>
                        (.49f * high + .41f * mid) * (Decay(t, 27f) + .47f * Decay(t - .049f, 45f) +
                            .31f * Decay(t - .115f, 58f) + .13f * Decay(t - .232f, 50f)) +
                        (.15f * Mathf.Sin(Tau * 1499f * t) + .10f * Mathf.Sin(Tau * 2612f * t) +
                            .067f * Mathf.Sin(Tau * 4177f * t)) * Decay(t, 16f));
                case VehicleImpactSurface.Wood:
                    return CreateClip("Wood breaking", .47f, 502, (t, mid, high) =>
                        (.46f * mid + .22f * high) * (Decay(t, 31f) + .60f * Decay(t - .035f, 45f) +
                            .28f * Decay(t - .083f, 39f)) + .40f * Thud(t, 123f, 28f) + .26f * Thud(t, 57f, 19f));
                case VehicleImpactSurface.Stone:
                    return CreateClip("Stone and concrete impact", .47f, 302, (t, mid, high) =>
                        .58f * Thud(t, 63f, 16f) + .22f * Thud(t, 167f, 24f) +
                        (mid * .56f + high * .15f) * (Decay(t, 28f) + .31f * Decay(t - .088f, 20f)));
                case VehicleImpactSurface.Plastic:
                    return CreateClip("Plastic strike", .22f, 213, (t, mid, high) =>
                        .44f * Thud(t, 273f, 46f) + (mid * .32f + high * .21f) * Decay(t, 31f));
                default:
                    return CreateClip("Metal and body impact", .48f, 238, (t, mid, high) =>
                        .53f * Thud(t, 64f, 19f) + (.49f * mid + .24f * high) * Decay(t, 25f) +
                        (.13f * Mathf.Sin(Tau * 396f * t) + .16f * Mathf.Sin(Tau * 721f * t) +
                            .09f * Mathf.Sin(Tau * 1187f * t)) * Decay(t, 14f));
            }
        }

        private static float Thud(float time, float frequency, float decay) =>
            Mathf.Sin(Tau * frequency * time + 3.4f * (1f - Mathf.Exp(-time * 27f))) * Decay(time, decay);

        private static float Decay(float time, float decay) =>
            time < 0f ? 0f : Mathf.Min(1f, time * 1400f) * Mathf.Exp(-time * decay);

        private static AudioClip CreateClip(string name, float seconds, int seed, Func<float, float, float, float> sample, bool fadeEdges = true)
        {
            var samples = new float[Mathf.RoundToInt(seconds * SampleRate)];
            var noise = new System.Random(seed);
            var lowPass = 0f;
            var midPass = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)SampleRate;
                var signal = (float)noise.NextDouble() * 2f - 1f;
                lowPass += (signal - lowPass) * .13f;
                midPass += (signal - midPass) * .62f;
                var fade = fadeEdges ? Mathf.Clamp01(time * 990f) * Mathf.Clamp01((seconds - time) * 180f) : 1f;
                samples[i] = Mathf.Clamp(sample(time, midPass - lowPass, signal - midPass) * fade, -.99f, .99f);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void StopAudio()
        {
            _playing = false;
            _engine.Stop();
            _friction.Stop();
            _oneShots.Stop();
            _engine.volume = _friction.volume = 0f;
        }

        private void OnDisable() => StopAudio();

        private void OnDestroy()
        {
            Destroy(_engineLoop);
            Destroy(_tyres);
            Destroy(_scrape);
            Destroy(_boost);
            foreach (var impact in _impacts) Destroy(impact);
        }
    }
}
