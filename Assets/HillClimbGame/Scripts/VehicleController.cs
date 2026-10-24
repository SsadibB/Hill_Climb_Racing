using UnityEngine;
using UnityEngine.InputSystem;

namespace HillClimbGame
{
    public enum VehicleDriveState
    {
        Idle,       // Coasting / No input -> 0 fuel
        Gas,        // Driving forward -> normal fuel drain
        Braking,    // Actively braking forward momentum -> 0 fuel
        Backward    // Driving backward in reverse -> lower fuel drain
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Chassis & Physics References")]
        [SerializeField] private Rigidbody2D vehicleBody;
        [SerializeField] private Rigidbody2D frontWheelRb;
        [SerializeField] private Rigidbody2D rearWheelRb;
        [SerializeField] private WheelJoint2D frontWheelJoint;
        [SerializeField] private WheelJoint2D rearWheelJoint;

        [Header("Wheel Visual References")]
        [SerializeField] private Transform frontWheelTransform;
        [SerializeField] private Transform rearWheelTransform;
        [SerializeField] private float wheelRadius = 0.346f; // = wheelScale(0.54) × colliderRadius(0.64)

        [Header("Drive & Traction Settings")]
        [SerializeField] private float motorTorque            = 1200f;  // High torque for powerful launch
        [SerializeField] private float brakeTorque            = 900f;   // Firm, responsive braking torque
        [SerializeField] private float reverseTorque          = 400f;   // Gentle, controllable reverse torque
        [SerializeField] private float maxMotorSpeed          = 4500f;  // Degrees/sec forward (~750 RPM, ~26.5 m/s)
        [SerializeField] private float maxReverseMotorSpeed   = 500f;   // Degrees/sec reverse
        [SerializeField] private float motorAccelerationRate  = 2200f;  // Fast torque ramp for instant response
        [SerializeField] private float maxAllowedSlipSpeed    = 800f;   // Allow wheelspin for launch and downhill feel
        [SerializeField] private float maxSpeed               = 28f;    // Max linear speed (m/s)
        [SerializeField] private float airTiltTorque          = 38f;    // Mid-air rotation torque

        [Header("Wheelie Physics")]
        [SerializeField] private float wheelieFrontTorqueMult  = 0.25f;
        [SerializeField] private float wheelieLiftForce        = 12f;
        [SerializeField] private float wheelieRearCOMShift     = -0.40f;
        [SerializeField] private float wheelieSpeedThreshold   = 2.0f;
        [SerializeField] private float wheelieMaxAngle         = 38f;
        [SerializeField] private bool  enableWheelieAssist     = false;  // OFF by default — equal torque both wheels

        [Header("Vehicle Mass & Damping Settings")]
        [SerializeField] private float vehicleMass            = 1.5f;
        [SerializeField] private float wheelMass              = 0.30f;
        [SerializeField] private float gravityScale           = 2.2f;
        [SerializeField] private float linearDamping          = 0.15f;  // Low drag to preserve natural downhill momentum
        [SerializeField] private float angularDamping         = 1.2f;
        [SerializeField] private Vector2 centerOfMassOffset   = new Vector2(0f, -0.30f); // Centered, low — stable fast driving

        [Header("Wheel Size Settings")]
        [SerializeField] private float wheelScale             = 0.54f;  // Uniform wheel scale (was 0.46)
        [SerializeField] private float wheelColliderRadius    = 0.64f;  // CircleCollider2D radius (unchanged — scale handles size)
        [SerializeField] private bool  applyWheelSizeOnStart  = true;

        [Header("Crash Detection")]
        [SerializeField] private float upsideDownThreshold    = 155f;   // Degrees from upright
        [SerializeField] private float crashAngleDuration     = 2.2f;

        // Drive State & Telemetry
        public VehicleDriveState DriveState { get; private set; } = VehicleDriveState.Idle;
        public float  Throttle      { get; private set; }   // Normalized input -1..1
        public float  SpeedMs       { get; private set; }
        public float  SpeedKmh      { get; private set; }
        public bool   IsGrounded    { get; private set; }
        public bool   IsCrashed     { get; private set; }

        private bool  _active;
        private float _crashTimer;
        private float _currentMotorSpeed;

        // Mobile button state (set by UI buttons)
        private bool  _mobileAccel;
        private bool  _mobileBrake;

        // Raw input states
        private bool  _inputGas;
        private bool  _inputReverseOrBrake;
        private bool  _inputDedicatedBrake;

        void Awake()
        {
            AutoWire();
            SetupPhysics();
        }

        public void AutoWire()
        {
            if (vehicleBody == null)
            {
                vehicleBody = GetComponent<Rigidbody2D>();
                if (vehicleBody == null)
                {
                    var vb = GameObject.Find("VehicleBody");
                    if (vb != null) vehicleBody = vb.GetComponent<Rigidbody2D>();
                }
            }

            if (frontWheelRb == null)
            {
                var fw = GameObject.Find("FrontWheel");
                if (fw != null) frontWheelRb = fw.GetComponent<Rigidbody2D>();
            }

            if (rearWheelRb == null)
            {
                var rw = GameObject.Find("RearWheel");
                if (rw != null) rearWheelRb = rw.GetComponent<Rigidbody2D>();
            }

            if (frontWheelTransform == null && frontWheelRb != null)
                frontWheelTransform = frontWheelRb.transform;

            if (rearWheelTransform == null && rearWheelRb != null)
                rearWheelTransform = rearWheelRb.transform;

            if (vehicleBody != null)
            {
                var joints = vehicleBody.GetComponents<WheelJoint2D>();
                if (joints.Length > 0 && frontWheelJoint == null) frontWheelJoint = joints[0];
                if (joints.Length > 1 && rearWheelJoint == null)  rearWheelJoint  = joints[1];
            }
        }

        void SetupPhysics()
        {
            if (vehicleBody != null)
            {
                vehicleBody.mass          = vehicleMass;
                vehicleBody.gravityScale  = gravityScale;
                vehicleBody.linearDamping = linearDamping;
                vehicleBody.angularDamping= angularDamping;
                vehicleBody.centerOfMass  = centerOfMassOffset;
                vehicleBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            if (frontWheelRb != null)
            {
                frontWheelRb.mass          = wheelMass;
                frontWheelRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            if (rearWheelRb != null)
            {
                rearWheelRb.mass           = wheelMass;
                rearWheelRb.interpolation  = RigidbodyInterpolation2D.Interpolate;
            }

            // Apply wheel sizes if requested
            if (applyWheelSizeOnStart)
                ApplyWheelSize();
        }

        void ApplyWheelSize()
        {
            if (frontWheelRb != null)
            {
                frontWheelRb.transform.localScale = new Vector3(wheelScale, wheelScale, 1f);
                var col = frontWheelRb.GetComponent<CircleCollider2D>();
                if (col != null) col.radius = wheelColliderRadius;
            }
            if (rearWheelRb != null)
            {
                rearWheelRb.transform.localScale = new Vector3(wheelScale, wheelScale, 1f);
                var col = rearWheelRb.GetComponent<CircleCollider2D>();
                if (col != null) col.radius = wheelColliderRadius;
            }
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (!active)
            {
                _currentMotorSpeed = 0f;
                DriveState = VehicleDriveState.Idle;
                CoastWheels();
            }
        }

        void Update()
        {
            if (!_active) return;
            ReadInput();
            UpdateCrashState();
        }

        void FixedUpdate()
        {
            if (!_active) return;
            DriveWheels();
            UpdateSpeed();
        }

        void ReadInput()
        {
            bool gasPressed   = false;
            bool reverseBrake = false;
            bool hardBrake    = false;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                gasPressed   = keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed || keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
                reverseBrake = keyboard.leftArrowKey.isPressed  || keyboard.aKey.isPressed;
                hardBrake    = keyboard.downArrowKey.isPressed  || keyboard.sKey.isPressed || keyboard.spaceKey.isPressed;
            }

            // Mobile button override
            if (_mobileAccel) gasPressed   = true;
            if (_mobileBrake) reverseBrake = true;

            _inputGas            = gasPressed;
            _inputReverseOrBrake = reverseBrake;
            _inputDedicatedBrake = hardBrake;

            if (_inputGas)
                Throttle = 1f;
            else if (_inputReverseOrBrake || _inputDedicatedBrake)
                Throttle = -1f;
            else
                Throttle = 0f;
        }

        void DriveWheels()
        {
            if (vehicleBody == null) return;

            // Project linear velocity along chassis forward vector for accurate downhill/uphill speed
            Vector2 forwardDir     = vehicleBody.transform.right;
            float   forwardSpeed   = Vector2.Dot(vehicleBody.linearVelocity, forwardDir);
            bool    atMaxForwardSpeed = forwardSpeed > maxSpeed;
            IsGrounded             = CheckIsGrounded();

            // Wheel rolling speed (in Unity 2D, clockwise rotation = negative angularVelocity)
            float fwRoll = frontWheelRb != null ? -frontWheelRb.angularVelocity : 0f;
            float rwRoll = rearWheelRb  != null ? -rearWheelRb.angularVelocity  : 0f;
            float currentRollingSpeed = Mathf.Max(fwRoll, rwRoll, 0f);

            // Chassis pitch angle (positive = nose-up / wheelie)
            float chassisAngle = vehicleBody.rotation;
            // Normalize to -180..180
            while (chassisAngle >  180f) chassisAngle -= 360f;
            while (chassisAngle < -180f) chassisAngle += 360f;

            // 1. Dedicated Handbrake / Brake Key (Down / S / Space)
            if (_inputDedicatedBrake)
            {
                DriveState = VehicleDriveState.Braking;
                _currentMotorSpeed = Mathf.MoveTowards(_currentMotorSpeed, 0f, motorAccelerationRate * 2f * Time.fixedDeltaTime);
                vehicleBody.centerOfMass = centerOfMassOffset;
                ApplyBrakeToWheels(brakeTorque);
                return;
            }

            // 2. Gas Input (Right / D / W / Up / On-screen Gas Pedal)
            if (_inputGas && !atMaxForwardSpeed)
            {
                DriveState = VehicleDriveState.Gas;

                // DOWNHILL MOMENTUM PRESERVATION:
                // If wheels are already rolling forward faster than our motor speed (e.g. coasting downhill under gravity),
                // immediately synchronize _currentMotorSpeed to the current wheel rolling speed.
                // This guarantees the motor joint never applies counter-torque to brake existing forward momentum!
                if (currentRollingSpeed > _currentMotorSpeed)
                {
                    _currentMotorSpeed = currentRollingSpeed;
                }

                // Smoothly accelerate toward maxMotorSpeed
                _currentMotorSpeed = Mathf.MoveTowards(_currentMotorSpeed, maxMotorSpeed, motorAccelerationRate * Time.fixedDeltaTime);

                // Drive speed must always be at least as fast as current rolling speed plus positive drive margin
                // so the motor strictly imparts forward accelerating torque and never acts as a brake
                float targetDriveSpeed = Mathf.Max(_currentMotorSpeed, currentRollingSpeed + 60f);

                // Traction control: allow healthy wheelspin while preventing runaway spinning when grounded
                if (IsGrounded && wheelRadius > 0.01f)
                {
                    float naturalGroundWheelSpeed = (Mathf.Max(0f, forwardSpeed) / wheelRadius) * Mathf.Rad2Deg;
                    float maxAllowedSpeed = Mathf.Max(naturalGroundWheelSpeed + maxAllowedSlipSpeed, currentRollingSpeed + 60f);
                    targetDriveSpeed = Mathf.Min(targetDriveSpeed, maxAllowedSpeed);
                }

                // Negative speed in Unity 2D WheelJoint = clockwise rotation (drives forward to the right)
                float appliedSpeed = -targetDriveSpeed;

                // --- WHEELIE SYSTEM ---
                bool wheelieActive = enableWheelieAssist
                                  && IsGrounded
                                  && forwardSpeed > wheelieSpeedThreshold
                                  && chassisAngle < wheelieMaxAngle;

                if (wheelieActive)
                {
                    ApplyMotorToJoint(rearWheelJoint,  motorTorque,                          appliedSpeed);
                    ApplyMotorToJoint(frontWheelJoint, motorTorque * wheelieFrontTorqueMult,  appliedSpeed);

                    vehicleBody.centerOfMass = new Vector2(centerOfMassOffset.x + wheelieRearCOMShift,
                                                           centerOfMassOffset.y);

                    Vector2 frontAnchorWorld = vehicleBody.position
                        + (Vector2)(vehicleBody.transform.TransformDirection(
                              new Vector3(frontWheelJoint != null ? frontWheelJoint.anchor.x : 0.65f,
                                          frontWheelJoint != null ? frontWheelJoint.anchor.y : -0.44f, 0f)));
                    vehicleBody.AddForceAtPosition(Vector2.up * wheelieLiftForce, frontAnchorWorld);
                }
                else
                {
                    vehicleBody.centerOfMass = centerOfMassOffset;
                    ApplyMotorToWheels(motorTorque, appliedSpeed);
                }

                // Mid-air pitch control (tilt nose up in air)
                if (!IsGrounded)
                {
                    vehicleBody.AddTorque(airTiltTorque);
                }
                return;
            }

            // 3. Reverse / Brake Input (Left / A / On-screen Brake Pedal)
            if (_inputReverseOrBrake)
            {
                vehicleBody.centerOfMass = centerOfMassOffset;

                if (forwardSpeed > 0.35f)
                {
                    // BRAKING
                    DriveState = VehicleDriveState.Braking;
                    _currentMotorSpeed = Mathf.MoveTowards(_currentMotorSpeed, 0f, motorAccelerationRate * 2.5f * Time.fixedDeltaTime);
                    ApplyBrakeToWheels(brakeTorque);
                }
                else
                {
                    // BACKWARD
                    DriveState = VehicleDriveState.Backward;
                    _currentMotorSpeed = Mathf.MoveTowards(_currentMotorSpeed, maxReverseMotorSpeed, motorAccelerationRate * Time.fixedDeltaTime);
                    ApplyMotorToWheels(reverseTorque, _currentMotorSpeed);
                }

                if (!IsGrounded)
                    vehicleBody.AddTorque(-airTiltTorque);

                return;
            }

            // 4. No Input / Coasting — restore normal COM and let wheels roll freely
            DriveState = VehicleDriveState.Idle;
            vehicleBody.centerOfMass = centerOfMassOffset;
            _currentMotorSpeed = Mathf.MoveTowards(_currentMotorSpeed, 0f, motorAccelerationRate * 1.5f * Time.fixedDeltaTime);
            CoastWheels();
        }

        void ApplyMotorToWheels(float torque, float targetSpeed)
        {
            ApplyMotorToJoint(frontWheelJoint, torque, targetSpeed);
            ApplyMotorToJoint(rearWheelJoint,  torque, targetSpeed);
        }

        void ApplyBrakeToWheels(float torque)
        {
            // Set motor speed to 0 with brake torque to stop the wheels cleanly
            ApplyMotorToJoint(frontWheelJoint, torque, 0f);
            ApplyMotorToJoint(rearWheelJoint,  torque, 0f);
        }

        void CoastWheels()
        {
            if (frontWheelJoint != null) frontWheelJoint.useMotor = false;
            if (rearWheelJoint  != null) rearWheelJoint.useMotor  = false;
        }

        void ApplyMotorToJoint(WheelJoint2D joint, float torque, float targetSpeed)
        {
            if (joint == null) return;
            var motor = joint.motor;
            motor.motorSpeed     = targetSpeed;
            motor.maxMotorTorque = torque;
            joint.motor          = motor;
            joint.useMotor       = true;
        }

        bool CheckIsGrounded()
        {
            if (frontWheelRb != null && frontWheelRb.IsTouchingLayers()) return true;
            if (rearWheelRb  != null && rearWheelRb.IsTouchingLayers())  return true;
            return false;
        }

        void UpdateSpeed()
        {
            if (vehicleBody == null) return;
            SpeedMs  = vehicleBody.linearVelocity.magnitude;
            SpeedKmh = SpeedMs * 3.6f;
        }

        void UpdateCrashState()
        {
            float angle = Mathf.Abs(vehicleBody.rotation % 360f);
            if (angle > 180f) angle = 360f - angle;

            bool upsideDown = angle > upsideDownThreshold;
            if (upsideDown)
            {
                _crashTimer += Time.deltaTime;
                if (_crashTimer >= crashAngleDuration) IsCrashed = true;
            }
            else
            {
                _crashTimer = 0f;
                IsCrashed   = false;
            }
        }

        public void ResetVehicle(Vector3 position)
        {
            _active            = false;
            IsCrashed          = false;
            _crashTimer        = 0f;
            Throttle           = 0f;
            _currentMotorSpeed = 0f;
            DriveState         = VehicleDriveState.Idle;
            _mobileAccel       = false;
            _mobileBrake       = false;

            if (vehicleBody != null)
            {
                vehicleBody.linearVelocity  = Vector2.zero;
                vehicleBody.angularVelocity = 0f;
                vehicleBody.rotation        = 0f;
                vehicleBody.position        = position;
            }

            transform.position = position;
            transform.rotation = Quaternion.identity;

            if (frontWheelRb != null)
            {
                frontWheelRb.linearVelocity  = Vector2.zero;
                frontWheelRb.angularVelocity = 0f;
                frontWheelRb.rotation        = 0f;
                Vector2 frontOffset = frontWheelJoint != null ? frontWheelJoint.anchor : new Vector2(0.65f, -0.44f);
                frontWheelRb.position = (Vector2)position + frontOffset;
            }
            if (rearWheelRb != null)
            {
                rearWheelRb.linearVelocity  = Vector2.zero;
                rearWheelRb.angularVelocity = 0f;
                rearWheelRb.rotation        = 0f;
                Vector2 rearOffset = rearWheelJoint != null ? rearWheelJoint.anchor : new Vector2(-0.65f, -0.44f);
                rearWheelRb.position = (Vector2)position + rearOffset;
            }

            if (frontWheelTransform != null) frontWheelTransform.rotation = Quaternion.identity;
            if (rearWheelTransform  != null) rearWheelTransform.rotation  = Quaternion.identity;

            CoastWheels();
        }

        // Mobile input hooks (called by on-screen UI buttons)
        public void OnAccelPressed()  => _mobileAccel = true;
        public void OnAccelReleased() => _mobileAccel = false;
        public void OnBrakePressed()  => _mobileBrake = true;
        public void OnBrakeReleased() => _mobileBrake = false;
    }
}
