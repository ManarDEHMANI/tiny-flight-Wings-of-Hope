﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MFlight.Demo
{
  
    [RequireComponent(typeof(Rigidbody))]
    public class Plane : MonoBehaviour
    {   

        // --------------------------------------------------
        // Inspector Fields
        // --------------------------------------------------

        [Header("Components")]
        [SerializeField] private MouseFlightController controller = null;


        [Header("Physics")]
        [Tooltip("Force to push plane forwards with")] public float thrust;
        [Tooltip("Pitch, Yaw, Roll")] public Vector3 turnTorque;
        [Tooltip("Multiplier for all forces")] public float forceMult;
        [Tooltip("Maximum thrust the engine can provide")]public float maxThrust;

        [Header("Autopilot")]
        [Tooltip("Sensitivity for autopilot flight.")] public float sensitivity;
        [Tooltip("Angle at which airplane banks fully into target.")] public float aggressiveTurnAngle;
        

        [Header("Input")]
        [SerializeField] [Range(-1f, 1f)] private float pitch ;
        [SerializeField] [Range(-1f, 1f)] private float yaw;
        [SerializeField] [Range(-1f, 1f)] private float roll;


        [Header("Advanced Aerodynamics")]
        [Tooltip("Lift power of the plane.")] public float liftPower;
        [Tooltip("Additional lift power from flaps")] public float flapsLiftPower;   
        [Tooltip("Flaps Angle of Attack bias (in degrees)")] public float flapsAOABias ;       
        [Tooltip("Rudder power (yaw axis)")] public float rudderPower;       


        [Header("Flight Controls")]
        [Tooltip("Normalized control input (pitch, yaw, roll)")] public Vector3 controlInput;
        [Tooltip("Target turn speed in deg/s or similar factor")] public Vector3 turnSpeed;
        [Tooltip("Angular acceleration values")] public Vector3 turnAcceleration;
        [Tooltip("Effective input after G-limit and corrections")] public Vector3 EffectiveInput;


        [Header("G-Limit Settings")]
        [Tooltip("General G-limit")] public float gLimit;
        [Tooltip("G-limit specifically for pitch axis")] public float gLimitPitch;

        [Header("Additional Drag Settings")]
        [Tooltip("Extra drag from airbrakes")] public float airbrakeDrag;
        [Tooltip("Extra drag from flaps")] public float flapsDrag;
        [Tooltip("Angular drag coefficients")] public Vector3 angularDrag;
        
         // --------------------------------------------------
        // Private Fields
        // --------------------------------------------------

        private Rigidbody rigid;
        private bool rollOverride = false;
        private bool pitchOverride = false;
        private bool Dead = false;
        private float flapsRetractSpeed = 2.0f;

        // State variables
        private Vector3 Velocity;
        private Vector3 lastVelocity;
        private Vector3 LocalVelocity;
        private Vector3 LocalAngularVelocity;
        private Vector3 LocalGForce;
        private float AngleOfAttack;
        private float AngleOfAttackYaw;

        // Deployable states
        public bool AirbrakeDeployed = false;
        public bool FlapsDeployed = false;

        // --------------------------------------------------
        // Public Properties
        // --------------------------------------------------
        public float Pitch { set { pitch = Mathf.Clamp(value, -1f, 1f); } get { return pitch; } }
        public float Yaw { set { yaw = Mathf.Clamp(value, -1f, 1f); } get { return yaw; } }
        public float Roll { set { roll = Mathf.Clamp(value, -1f, 1f); } get { return roll; } }

        
         // --------------------------------------------------
        // Unity Lifecycle
        // --------------------------------------------------
        
        /// <summary>
        /// Unity Awake method. Initializes the plane.
        /// </summary>
        /// <remarks>
        /// Called once when the script instance is being loaded.
        /// </remarks>
        private void Awake()
        {
            rigid = GetComponent<Rigidbody>();

            if (controller == null)
                Debug.LogError(name + ": Plane - Missing reference to MouseFlightController!");

        }

        /// <summary>
        /// Unity Update method. Called once per frame to handle the player's input.
        /// </summary>
        /// <remarks>
        /// This method processes user input every frame, allowing for real-time 
        /// control adjustments and interaction with the plane's systems.
        /// </remarks>

        private void Update()
        {
            HandlePlayerInput();
        }

        /// <summary>
        /// Unity FixedUpdate method. Handles the physics calculations and updates for the plane 
        /// at a fixed time interval. It calculates the current state, angle of attack, and g-force 
        /// before applying updates to flaps, thrust, lift, steering, drag, and angular drag. 
        /// Finally, it recalculates the state to reflect these updates.
        /// </summary>
        /// <remarks>
        /// This method is called at a consistent rate, making it suitable for physics-related 
        /// calculations and updates to ensure smooth and accurate simulations.
        /// </remarks>

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Calculate state before applying forces
            CalculateState(dt);
            CalculateAngleOfAttack();
            CalculateGForce(dt);
            
            // Updates and physics
            UpdateFlaps();
            UpdateThrust();
            UpdateLift();
            UpdateSteering(dt);
            UpdateDrag();
            UpdateAngularDrag();

            // Recalculate state after updates
            CalculateState(dt);
        }


        // --------------------------------------------------
        // Input Handling
        // --------------------------------------------------
        
        /// <summary>
        /// Handles the player's input from the keyboard, overriding the autopilot
        /// if the player is actively controlling the plane.
        /// </summary>
        /// <remarks>
        /// This method is responsible for processing the player's input from the
        /// keyboard and deciding whether to use the autopilot or keyboard input.
        /// </remarks>
        private void HandlePlayerInput()
        {
            // When the player commands their own stick input, it should override what the
            // autopilot is trying to do.
            rollOverride = false;
            pitchOverride = false;

            float keyboardRoll = Input.GetAxis("Horizontal");
            if (Mathf.Abs(keyboardRoll) > .25f)
            {
                rollOverride = true;
            }

            float keyboardPitch = Input.GetAxis("Vertical");
            if (Mathf.Abs(keyboardPitch) > .25f)
            {
                pitchOverride = true;
                rollOverride = true;
            }

            // Calculate the autopilot stick inputs.
            float autoYaw = 0f;
            float autoPitch = 0f;
            float autoRoll = 0f;
            if (controller != null)
                RunAutopilot(controller.MouseAimPos, out autoYaw, out autoPitch, out autoRoll);

            // Use either keyboard or autopilot input.
            yaw = autoYaw;
            pitch = (pitchOverride) ? keyboardPitch : autoPitch;
            roll = (rollOverride) ? keyboardRoll : autoRoll;
            controlInput = new Vector3(pitch, yaw, roll);
        }

        /// <summary>
        /// Calculates the autopilot inputs (yaw, pitch, roll) to target the given flyTarget.
        /// </summary>
        /// <param name="flyTarget">The position in world space to target.</param>
        /// <param name="yaw">The yaw input to target the given flyTarget, ranging from -1 to 1.</param>
        /// <param name="pitch">The pitch input to target the given flyTarget, ranging from -1 to 1.</param>
        /// <param name="roll">The roll input to target the given flyTarget, ranging from -1 to 1.</param>
        /// <remarks>
        /// This method uses the following rules to generate the autopilot inputs:
        /// <list type="number">
        /// <item>Yaw and Pitch are calculated such that the target is directly in front of the aircraft.</item>
        /// <item>Roll is calculated to either roll into the target or fly wings level, depending on the angle of the target.</item>
        /// </list>
        /// </remarks>
        private void RunAutopilot(Vector3 flyTarget, out float yaw, out float pitch, out float roll)
        {
            // This is my usual trick of converting the fly to position to local space.
            // You can derive a lot of information from where the target is relative to self.
            var localFlyTarget = transform.InverseTransformPoint(flyTarget).normalized * sensitivity;
            var angleOffTarget = Vector3.Angle(transform.forward, flyTarget - transform.position);

            // IMPORTANT!
            // These inputs are created proportionally. This means it can be prone to
            // overshooting. The physics in this example are tweaked so that it's not a big
            // issue, but in something with different or more realistic physics this might
            // not be the case. Use of a PID controller for each axis is highly recommended.

            // ====================
            // PITCH AND YAW
            // ====================

            // Yaw/Pitch into the target so as to put it directly in front of the aircraft.
            // A target is directly in front the aircraft if the relative X and Y are both
            // zero. Note this does not handle for the case where the target is directly behind.
            yaw = Mathf.Clamp(localFlyTarget.x, -1f, 1f);
            pitch = -Mathf.Clamp(localFlyTarget.y, -1f, 1f);

            // ====================
            // ROLL
            // ====================

            // Roll is a little special because there are two different roll commands depending
            // on the situation. When the target is off axis, then the plane should roll into it.
            // When the target is directly in front, the plane should fly wings level.

            // An "aggressive roll" is input such that the aircraft rolls into the target so
            // that pitching up (handled above) will put the nose onto the target. This is
            // done by rolling such that the X component of the target's position is zeroed.
            var agressiveRoll = Mathf.Clamp(localFlyTarget.x, -1f, 1f);

            // A "wings level roll" is a roll commands the aircraft to fly wings level.
            // This can be done by zeroing out the Y component of the aircraft's right.
            var wingsLevelRoll = transform.right.y;

            // Blend between auto level and banking into the target.
            var wingsLevelInfluence = Mathf.InverseLerp(0f, aggressiveTurnAngle, angleOffTarget);
            roll = Mathf.Lerp(wingsLevelRoll, agressiveRoll, wingsLevelInfluence);
        }


        // --------------------------------------------------
        // State Calculations
        // --------------------------------------------------

        /// <summary>
        /// Calculates and updates the state of the aircraft.
        /// The state calculations are:
        /// <list type="bullet">
        /// <item><description>Velocity</description>: The global velocity of the aircraft.</item>
        /// <item><description>LocalVelocity</description>: The local velocity of the aircraft.</item>
        /// <item><description>LocalAngularVelocity</description>: The local angular velocity of the aircraft.</item>
        /// </list>
        /// </summary>
        /// <param name="dt">The time since the last frame, in seconds.</param>
        private void CalculateState(float dt)
        {
            var invRotation = Quaternion.Inverse(transform.rotation);
            Velocity = rigid.velocity;
            LocalVelocity = invRotation * Velocity;
            LocalAngularVelocity = invRotation * rigid.angularVelocity;
        }

        /// <summary>
        /// Calculates the angle of attack and yaw angle of attack for the aircraft.
        /// </summary>
        /// <remarks>
        /// If the squared magnitude of the local velocity is below a threshold, both angles are set to zero to avoid inaccuracies.
        /// Otherwise, the angle of attack is computed using the arctangent of the negative Y and Z components of the local velocity,
        /// while the yaw angle of attack is computed using the arctangent of the X and Z components of the local velocity.
        /// </remarks>
        private void CalculateAngleOfAttack()
        {
            if(LocalVelocity.sqrMagnitude < 0.1f)
            {
                AngleOfAttack = 0;
                AngleOfAttackYaw = 0;
                return ;
            }

            AngleOfAttack = Mathf.Atan2(-LocalVelocity.y,LocalVelocity.z);
            AngleOfAttackYaw = Mathf.Atan2(LocalVelocity.x,LocalVelocity.z);
        }
        
        /// <summary>
        /// Calculates the local G-Force experienced by the aircraft.
        /// </summary>
        /// <remarks>
        /// This method computes the G-Force by determining the change in velocity (acceleration)
        /// over the given time delta, transforms it into the local space using the inverse of the
        /// current rotation, and updates the last recorded velocity.
        /// </remarks>
        /// <param name="dt">The time since the last frame, in seconds.</param>

        private void CalculateGForce(float dt)
        {
            var invRotation = Quaternion.Inverse(rigid.rotation);
            var acceleration = (Velocity - lastVelocity) / dt;
            LocalGForce = invRotation * acceleration;
            lastVelocity = Velocity;
        }
        

        // --------------------------------------------------
        // Control and Steering
        // --------------------------------------------------

        /// <summary>
        /// Calculates a steering value that will limit the aircraft's angular velocity
        /// to the given target velocity, given the current angular velocity and acceleration.
        /// </summary>
        /// <param name="dt">The time since the last frame, in seconds.</param>
        /// <param name="angularVelocity">The current angular velocity of the aircraft.</param>
        /// <param name="targetVelocity">The target angular velocity of the aircraft.</param>
        /// <param name="acceleration">The maximum angular acceleration of the aircraft.</param>
        /// <returns>The steering value that should be applied to the aircraft to limit its angular velocity to the target velocity.</returns>
        private float CalculateSteering(float dt, float angularVelocity, float targetVelocity, float acceleration) {
            var error = targetVelocity - angularVelocity;
            var accel = acceleration * dt;
            return Mathf.Clamp(error, -accel, accel);
        }

        /// <summary>
        /// Calculates a factor that will limit the angular velocity of the aircraft
        /// to a given maximum G-Force, given the current control input and maximum
        /// angular velocity.
        /// </summary>
        /// <param name="controlInput">The control input of the aircraft.</param>
        /// <param name="maxAngularVelocity">The maximum angular velocity of the aircraft.</param>
        /// <returns>The limiting factor that should be applied to the control input to limit the angular velocity to the given maximum G-Force.</returns>
        private float CalculateGLimiter(Vector3 controlInput, Vector3 maxAngularVelocity) {
            if (controlInput.magnitude < 0.01f) {
                return 1;
            }

            //if the player gives input with magnitude less than 1, scale up their input so that magnitude == 1
            var maxInput = controlInput.normalized;

            var limit = CalculateGForceLimit(maxInput);
            var maxGForce = CalculateGForce(Vector3.Scale(maxInput, maxAngularVelocity), LocalVelocity);

            if (maxGForce.magnitude > limit.magnitude) {
                //example:
                //maxGForce = 16G, limit = 8G
                //so this is 8 / 16 or 0.5
                return limit.magnitude / maxGForce.magnitude;
            }
            return 1;
        }
        /// <summary>
        /// Calculates the limit of G-Force that can be applied to the aircraft based on the input vector.
        /// This method scales the input vector by the specified G-Force limits for each axis and multiplies
        /// it by the gravitational constant to obtain the maximum allowable G-Force.
        /// </summary>
        /// <param name="input">The input vector representing control inputs or forces on the aircraft.</param>
        /// <returns>A Vector3 representing the scaled G-Force limit for each axis.</returns>

        private Vector3 CalculateGForceLimit(Vector3 input) {
            return Utilities.Scale6(
                input,
                gLimit, gLimitPitch,   
                gLimit, gLimit,       
                gLimit, gLimit   
            ) * 9.81f;
        }

        /// <summary>
        /// Estimates the G-Force exerted on the aircraft from its angular velocity and velocity.
        /// The G-Force is calculated using the cross product of the angular velocity and velocity vectors.
        /// </summary>
        /// <param name="angularVelocity">The angular velocity of the aircraft.</param>
        /// <param name="velocity">The velocity of the aircraft.</param>
        /// <returns>The G-Force exerted on the aircraft.</returns>
        private Vector3 CalculateGForce(Vector3 angularVelocity, Vector3 velocity) {
            // G-Force estimation using cross product
            return Vector3.Cross(angularVelocity, velocity);
        }
        
        /// <summary>
        /// Updates the steering of the aircraft by applying a torque to the Rigidbody
        /// based on the input vector and the current angular velocity.
        /// The torque is calculated using the CalculateSteering method.
        /// </summary>
        /// <param name="dt">The time since the last frame, in seconds.</param>
        private void UpdateSteering(float dt)
        {
            var speed = Mathf.Max(0, LocalVelocity.z);

            float baseSteeringPower = 2.0f;        
            float steeringPower = baseSteeringPower * (1.0f + speed / 100f); 

            var gForceScaling = CalculateGLimiter(controlInput,turnSpeed);
            var targetAV = Vector3.Scale(controlInput, turnSpeed * steeringPower * gForceScaling);
            var av = LocalAngularVelocity * Mathf.Rad2Deg;
            var correction = new Vector3(
                CalculateSteering(dt, av.x, targetAV.x, turnAcceleration.x),
                CalculateSteering(dt, av.y, targetAV.y, turnAcceleration.y),
                CalculateSteering(dt, av.z, targetAV.z, turnAcceleration.z)
            );

            rigid.AddRelativeTorque(correction * Mathf.Deg2Rad, ForceMode.VelocityChange);
        }

        // --------------------------------------------------
        // Aerodynamics
        // --------------------------------------------------

        /// <summary>
        /// Calculates the lift force of the aircraft given the angle of attack, lift power, and right axis.
        /// The lift force is calculated using the cross product of the lift velocity and right axis, and the lift coefficient is calculated using the angle of attack.
        /// </summary>
        /// <param name="angleOfAttack">The angle of attack of the aircraft, in radians.</param>
        /// <param name="rightAxis">The right axis of the aircraft, used to calculate the lift direction.</param>
        /// <param name="liftPower">The lift power of the aircraft, used to scale the lift force magnitude.</param>
        /// <returns>The lift force of the aircraft, in Newtons.</returns>
        private Vector3 CalculateLift(float angleOfAttack, Vector3 rightAxis, float liftPower)
        {
            var liftVelocity = Vector3.ProjectOnPlane(LocalVelocity, rightAxis);
            var velocitySquared = liftVelocity.sqrMagnitude;
            

            float liftCoefficient = Mathf.Clamp(1.0f + (0.1f * angleOfAttack), -1.5f, 1.5f);
            float liftForceMagnitude = velocitySquared * liftCoefficient * liftPower;

            Vector3 liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);

            return liftDirection * liftForceMagnitude;
        }
        
        /// <summary>
        /// Updates the lift force of the aircraft by calculating the lift force using the AngleOfAttack
        /// and lift power, and applying it to the Rigidbody.
        /// If the aircraft is moving slower than 1 unit per second, the lift force is not updated.
        /// The flaps lift power and angle of attack bias are used when the flaps are deployed.
        /// </summary>
        private void UpdateLift()
        {
            if (LocalVelocity.sqrMagnitude < 1f) return;

            float flapsLiftPower = FlapsDeployed ? this.flapsLiftPower : 0;
            float flapsAOABias = FlapsDeployed ? this.flapsAOABias * Mathf.Deg2Rad : 0;

            var liftForce = CalculateLift(AngleOfAttack + flapsAOABias, Vector3.right, liftPower + flapsLiftPower);
            var yawForce = CalculateLift(AngleOfAttackYaw, Vector3.up, rudderPower);

            rigid.AddRelativeForce(liftForce);
            rigid.AddRelativeForce(yawForce);
        }

        /// <summary>
        /// Updates the thrust force of the aircraft by applying the thrust times the maximum thrust to the Rigidbody.
        /// </summary>
        private void UpdateThrust()
        {
            rigid.AddRelativeForce(thrust*maxThrust*Vector3.forward);

        }

       
        
        // --------------------------------------------------
        // Drag and Other Forces
        // --------------------------------------------------
        
        /// <summary>
        /// Updates the drag force of the aircraft by calculating the drag force using the LocalVelocity
        /// and the drag factor, airbrake drag, and flaps drag, and applying it to the Rigidbody.
        /// </summary>
        private void UpdateDrag()
        {
            var lv = LocalVelocity;             
            var lv2 = lv.sqrMagnitude;

            float dragFactor = 2.0f;           
            float airbrakeDrag = AirbrakeDeployed ? this.airbrakeDrag : 0;
            float flapsDrag = FlapsDeployed ? this.flapsDrag : 0;

            float dragX = dragFactor * Mathf.Abs(lv.x);
            float dragY = dragFactor * Mathf.Abs(lv.y);
            float dragZ = dragFactor * (Mathf.Abs(lv.z) + airbrakeDrag + flapsDrag);

            Vector3 dragForce = new Vector3(-dragX * lv.x, -dragY * lv.y, -dragZ * lv.z);
            rigid.AddRelativeForce(dragForce);
        }

        /// <summary>
        /// Updates the angular drag of the aircraft by calculating the drag torque
        /// based on the local angular velocity and applying it to the Rigidbody.
        /// </summary>
        /// <remarks>
        /// The drag is calculated using the square of the magnitude of the local angular velocity,
        /// and is applied in the opposite direction of the angular velocity. The calculated drag 
        /// torque is then scaled by the angular drag factor.
        /// </remarks>
        private void UpdateAngularDrag() {
            var av = LocalAngularVelocity;
            var drag = av.sqrMagnitude * -av.normalized; 
            rigid.AddRelativeTorque(Vector3.Scale(drag, angularDrag), ForceMode.Acceleration);
            }

        /// <summary>
        /// Updates the deployment of the flaps based on the aircraft's airspeed.
        /// </summary>
        /// <remarks>
        /// The flaps are automatically retracted when the aircraft's airspeed exceeds the
        /// flaps retract speed.
        /// </remarks>
        private void UpdateFlaps() {
            if (LocalVelocity.z > flapsRetractSpeed) {
                FlapsDeployed = false;
            }
        }
    
    }
}
