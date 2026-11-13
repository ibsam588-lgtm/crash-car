using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeVehicleController : MonoBehaviour
    {
        [Header("Wheel Colliders")]
        public WheelCollider frontLeftCollider;
        public WheelCollider frontRightCollider;
        public WheelCollider rearLeftCollider;
        public WheelCollider rearRightCollider;

        [Header("Wheel Visuals")]
        public Transform frontLeftVisual;
        public Transform frontRightVisual;
        public Transform rearLeftVisual;
        public Transform rearRightVisual;

        [Header("Handling")]
        public float maxMotorTorque = 1650f;
        public float maxSteerAngle = 29f;
        public float brakeTorque = 3200f;
        public float handbrakeTorque = 5200f;
        public float downforce = 85f;
        public float boostMultiplier = 1.45f;
        public float arcadeDriveAcceleration = 0f;
        public bool autoAccelerate = true;
        public float launchDelaySeconds = 5.6f;
        public float maxSpeedKmh = 188f;
        public float collisionVelocityCapKmh = 235f;
        public float lateralDamping = 3.6f;
        public float uprightAssist = 4.2f;

        public float SpeedKmh { get; private set; }
        public float CurrentSteer { get; private set; }
        public float CurrentThrottle { get; private set; }
        public float CurrentBrake { get; private set; }
        public bool BoostActive => _boostTimer > 0f;
        public float BoostCharge { get; private set; } = 1f;
        public bool LaunchReady => Time.time >= _launchAt;

        private Rigidbody _body;
        private float _mobileSteer;
        private float _mobileThrottle;
        private float _mobileBrake;
        private bool _useMobileInput;
        private float _boostTimer;
        private float _launchAt;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Quaternion _frontLeftRotationOffset = Quaternion.identity;
        private Quaternion _frontRightRotationOffset = Quaternion.identity;
        private Quaternion _rearLeftRotationOffset = Quaternion.identity;
        private Quaternion _rearRightRotationOffset = Quaternion.identity;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.mass = 1350f;
            _body.centerOfMass = new Vector3(0f, -0.28f, 0.12f);
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.linearDamping = 0.025f;
            _body.angularDamping = 0.55f;
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _launchAt = Time.time + launchDelaySeconds;
            _frontLeftRotationOffset = WheelRotationOffset(frontLeftCollider, frontLeftVisual);
            _frontRightRotationOffset = WheelRotationOffset(frontRightCollider, frontRightVisual);
            _rearLeftRotationOffset = WheelRotationOffset(rearLeftCollider, rearLeftVisual);
            _rearRightRotationOffset = WheelRotationOffset(rearRightCollider, rearRightVisual);
        }

        public void SetMobileInput(float steer, float throttle, float brake)
        {
            _useMobileInput = true;
            _mobileSteer = Mathf.Clamp(steer, -1f, 1f);
            _mobileThrottle = Mathf.Clamp(throttle, -1f, 1f);
            _mobileBrake = Mathf.Clamp01(brake);
        }

        public void ClearMobileInput()
        {
            _useMobileInput = false;
            _mobileSteer = 0f;
            _mobileThrottle = 0f;
            _mobileBrake = 0f;
        }

        public void Boost(float seconds = 1.4f)
        {
            TryBoost(seconds);
        }

        public bool TryBoost(float seconds = 1.4f)
        {
            if (!LaunchReady || BoostActive || BoostCharge < 0.35f || Time.timeScale <= 0f)
            {
                return false;
            }
            BoostCharge -= 0.35f;
            _boostTimer = Mathf.Clamp(seconds, 0.1f, 2f);
            return true;
        }

        public void ResetForRelaunch(float delaySeconds = 1.15f)
        {
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _boostTimer = 0f;
            _mobileSteer = 0f;
            _mobileThrottle = 0f;
            _mobileBrake = 0f;
            _launchAt = Time.time + delaySeconds;
        }

        public void RecoverAt(Vector3 position, Quaternion rotation)
        {
            _spawnPosition = position;
            _spawnRotation = rotation;
            ResetForRelaunch(0f);
            SpeedKmh = CurrentSteer = CurrentThrottle = CurrentBrake = 0f;
        }

        private void FixedUpdate()
        {
            if (!LaunchReady)
            {
                HoldAtLaunch();
                return;
            }

            var steer = _useMobileInput ? _mobileSteer : Input.GetAxis("Horizontal");
            var throttle = _useMobileInput ? _mobileThrottle : Input.GetAxis("Vertical");
            var brake = _useMobileInput ? _mobileBrake : (Input.GetKey(KeyCode.Space) ? 1f : 0f);
            var handbrake = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;

            SpeedKmh = _body.linearVelocity.magnitude * 3.6f;
            _boostTimer = Mathf.Max(0f, _boostTimer - Time.fixedDeltaTime);
            if (!BoostActive)
            {
                BoostCharge = Mathf.Min(1f, BoostCharge + Time.fixedDeltaTime * 0.10f);
            }
            if (autoAccelerate && !_useMobileInput && Mathf.Abs(throttle) < 0.05f && brake < 0.05f)
            {
                throttle = 1f;
            }

            CurrentSteer = steer;
            CurrentThrottle = throttle;
            CurrentBrake = brake;

            var speedLimit = _boostTimer > 0f ? maxSpeedKmh * 1.10f : maxSpeedKmh;
            var torqueFalloff = Mathf.Clamp01(1f - Mathf.InverseLerp(speedLimit * 0.82f, speedLimit, SpeedKmh));
            var torque = throttle * maxMotorTorque * torqueFalloff;
            if (_boostTimer > 0f && throttle > 0.1f)
            {
                torque *= boostMultiplier;
            }

            if (arcadeDriveAcceleration > 0f && throttle > 0.01f)
            {
                var boostForce = _boostTimer > 0f ? 1.18f : 1f;
                _body.AddForce(transform.forward * (throttle * arcadeDriveAcceleration * torqueFalloff * boostForce), ForceMode.Acceleration);
            }

            frontLeftCollider.steerAngle = steer * maxSteerAngle;
            frontRightCollider.steerAngle = steer * maxSteerAngle;

            rearLeftCollider.motorTorque = torque;
            rearRightCollider.motorTorque = torque;

            var wheelBrake = brake * brakeTorque + handbrake * handbrakeTorque;
            frontLeftCollider.brakeTorque = brake * brakeTorque;
            frontRightCollider.brakeTorque = brake * brakeTorque;
            rearLeftCollider.brakeTorque = wheelBrake;
            rearRightCollider.brakeTorque = wheelBrake;

            _body.AddForce(-transform.up * downforce * _body.linearVelocity.magnitude, ForceMode.Force);
            ApplyStability(speedLimit);
            ClampVelocity(collisionVelocityCapKmh);
        }

        private void LateUpdate()
        {
            SyncWheel(frontLeftCollider, frontLeftVisual, _frontLeftRotationOffset);
            SyncWheel(frontRightCollider, frontRightVisual, _frontRightRotationOffset);
            SyncWheel(rearLeftCollider, rearLeftVisual, _rearLeftRotationOffset);
            SyncWheel(rearRightCollider, rearRightVisual, _rearRightRotationOffset);
        }

        private static Quaternion WheelRotationOffset(WheelCollider collider, Transform visual)
        {
            if (collider == null || visual == null)
            {
                return Quaternion.identity;
            }
            return Quaternion.Inverse(collider.transform.rotation) * visual.rotation;
        }

        private static void SyncWheel(WheelCollider collider, Transform visual, Quaternion rotationOffset)
        {
            if (collider == null || visual == null)
            {
                return;
            }

            collider.GetWorldPose(out var position, out var rotation);
            visual.SetPositionAndRotation(position, rotation * rotationOffset);
        }

        private void HoldAtLaunch()
        {
            SpeedKmh = 0f;
            CurrentSteer = 0f;
            CurrentThrottle = 0f;
            CurrentBrake = 1f;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.MovePosition(_spawnPosition);
            _body.MoveRotation(_spawnRotation);
        }

        private void ApplyStability(float speedLimit)
        {
            var localVelocity = transform.InverseTransformDirection(_body.linearVelocity);
            localVelocity.x = Mathf.MoveTowards(localVelocity.x, 0f, lateralDamping * Time.fixedDeltaTime * Mathf.Max(1f, SpeedKmh / 35f));
            _body.linearVelocity = transform.TransformDirection(localVelocity);

            var upright = Vector3.Cross(transform.up, Vector3.up);
            _body.AddTorque(upright * uprightAssist, ForceMode.Acceleration);

            if (transform.position.y > 1.65f)
            {
                _body.AddForce(Vector3.down * 260f, ForceMode.Acceleration);
            }

            ClampVelocity(speedLimit);
        }

        private void ClampVelocity(float speedLimitKmh)
        {
            var limit = speedLimitKmh / 3.6f;
            if (_body.linearVelocity.magnitude > limit)
            {
                _body.linearVelocity = _body.linearVelocity.normalized * limit;
            }

            SpeedKmh = _body.linearVelocity.magnitude * 3.6f;
        }
    }
}
