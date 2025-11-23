using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Ashsvp
{
    public class SimcadeVehicleController : MonoBehaviour
    {
#region Variables
        [Header("Suspension")]
        public float springForce = 30000f;
        public float springDamper = 200f;
        private float MaxSpringDistance;
        private float[] suspensionForce = new float[4];
        private float[] compressions = new float[4];
        public LayerMask drivableLayers = ~0;

        [Header("Car Stats")]
        public float MaxSpeed = 200f;
        public float Acceleration;
        public AnimationCurve AccelerationCurve;
        [Tooltip("Curve for acceleration adjustment based on incline angle")]
        public AnimationCurve InclineAccelerationCurve = AnimationCurve.Constant(0, 1, 1);
        public AnimationCurve turnRadiusCurve = AnimationCurve.EaseInOut(0, 10, 1, 40);
        public float tireTurnSpeed = 10;
        public float brakeAcceleration = 50f;
        public float RollingResistance = 2f;
        [Range(0, 90)] public float maxDriftAngle = 60f;
        private float driftAngle;
        public float DriftAngle => driftAngle;
        public float FrictionCoefficient = 1f;
        public float slopeSlideAngle = 30f;
        public AnimationCurve driftFrictionCurve = AnimationCurve.EaseInOut(0, 0.2f, 1, 0.7f);
        public AnimationCurve sideFrictionCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.5f);
        public AnimationCurve forwardFrictionCurve = AnimationCurve.EaseInOut(0, 1, 1, 2);
        [FormerlySerializedAs("CenterOfMass_air")] public Transform airCenterOfMass;
        private Vector3 groundCenterOfMass;//Do we really need two centers of mass?
        public bool AutoCounterSteer = false;
        public float DownForce = 5;
        public float airAngularDamping = 0.2f;
        public AnimationCurve driftAngularDampingCurve = AnimationCurve.EaseInOut(0, 1, 1, 2);

        [Header("Visuals")]
        public Transform VehicleBody;
        [Range(0, 10)] public float forwardBodyTilt = 3f;
        [Range(0, 10)] public float sidewaysBodyTilt = 3f;
        public GameObject WheelSkid;
        public GameObject SkidMarkController;
        public float wheelRadius;
        public float maxWheelTravel = 0.2f;
        public float skidmarkWidth;
        public Transform[] HardPoints = new Transform[4];
        public Transform[] Wheels;      
        public Vector3 carVelocity;

        [Header("Events")]
        public Vehicle_Events VehicleEvents;
        [Serializable] public class Vehicle_Events
        {
            public UnityEvent OnTakeOff;
            public UnityEvent OnGrounded;
            public UnityEvent OnGearChange;
        }

        private RaycastHit[] wheelHits = new RaycastHit[4];
        private bool[] wheelIsGrounded = new bool[4];
        public float steerInput, accelerationInput, handbrakeInput, rearTrack, wheelBase, ackermennLeftAngle, ackermennRightAngle;
        private Rigidbody rb;
        public Rigidbody Rigidbody => rb;
        public Vector3 localVehicleVelocity;
        private Vector3 lastVelocity;
        public int NumberOfGroundedWheels => wheelIsGrounded.Count(w => w);
        public bool vehicleIsGrounded;
        private float[] offset_Prev = new float[4];
        public bool CanDrive, CanAccelerate;
        private GearSystem GearSystem;
        private float[] forwardSlip = new float[4];
        private float[] slipCoeff = new float[4];
        private float[] skidTotal = new float[4];
        private WheelSkid[] wheelSkids = new WheelSkid[4];
        public float vehicleScale = 1f;

#endregion//Variables

#region Unity Lifecycle
        void Awake()
        {
            Skidmarks skidmarks = Instantiate(SkidMarkController).GetComponent<Skidmarks>();
            skidmarks.SkidmarkWidth = skidmarkWidth;
            CanDrive = true;
            CanAccelerate = true;
            rb = GetComponent<Rigidbody>();
            lastVelocity = Vector3.zero;


            for (int i = 0; i < Wheels.Length; i++)
            {
                Transform wheel = Wheels[i];
                HardPoints[i].localPosition = new Vector3(wheel.localPosition.x, 0, wheel.localPosition.z);

                wheelSkids[i] = Instantiate(WheelSkid, wheel.position - wheel.up * wheelRadius, wheel.rotation, wheel).GetComponent<WheelSkid>();
                InitializeWheelSkid(i, skidmarks, wheelRadius);
            }
            MaxSpringDistance = Mathf.Abs(Wheels[0].localPosition.y - HardPoints[0].localPosition.y) + (0.1f * vehicleScale) + wheelRadius;
            wheelBase = Vector3.Distance(Wheels[0].position, Wheels[2].position);
            rearTrack = Vector3.Distance(Wheels[0].position, Wheels[1].position);
            GearSystem = GetComponent<GearSystem>();
        }

        private void Start()
        {
            groundCenterOfMass = (HardPoints[0].localPosition + HardPoints[1].localPosition + HardPoints[2].localPosition + HardPoints[3].localPosition) / 4;
            rb.centerOfMass = groundCenterOfMass;
        }

        void FixedUpdate()
        {
            localVehicleVelocity = transform.InverseTransformDirection(rb.linearVelocity);

            //NOTE: projecting zero velo onto a plane can yield unexpected angles 
            if (localVehicleVelocity.sqrMagnitude < 1) driftAngle = 0;
            else driftAngle = Vector3.Angle(transform.forward, rb.linearVelocity);

            AckermannSteering(steerInput);

            suspensionForce[0] = 0;
            suspensionForce[1] = 0;
            suspensionForce[2] = 0;
            suspensionForce[3] = 0;

            for (int i = 0; i < Wheels.Length; i++)
            {
                AddSuspensionForce(HardPoints[i].position, Wheels[i], MaxSpringDistance, out wheelHits[i], out wheelIsGrounded[i], out suspensionForce[i], i);
                UpdateTireVisuals(wheelIsGrounded[i], Wheels[i], HardPoints[i], wheelHits[i].distance, i);
                UpdateWheelSkid(i, skidTotal[i], wheelHits[i].point, wheelHits[i].normal);
            }

            float suspensionForce_hackSum = (suspensionForce[0] + suspensionForce[1] + suspensionForce[2] + suspensionForce[3]) / 4;

            suspensionForce[0] = suspensionForce_hackSum;
            suspensionForce[1] = suspensionForce_hackSum;
            suspensionForce[2] = suspensionForce_hackSum;
            suspensionForce[3] = suspensionForce_hackSum;

            bool vehicleWasGrounded = vehicleIsGrounded;
            vehicleIsGrounded = wheelIsGrounded.Any();

            if (vehicleIsGrounded)
            {
                Accelerate();
                AddRollingResistance();
                Brake();
                AnimateBody();

                //AutoBalence
                if (rb.centerOfMass != groundCenterOfMass) rb.centerOfMass = groundCenterOfMass;
                rb.angularDamping = driftAngularDampingCurve.Evaluate(driftAngle / maxDriftAngle);
                rb.AddForce(-transform.up * DownForce * rb.mass);
            }
            else
            {
                if (rb.centerOfMass != airCenterOfMass.localPosition) rb.centerOfMass = airCenterOfMass.localPosition;
                rb.angularDamping = airAngularDamping;
            }

            //friction
            for (int i = 0; i < Wheels.Length; i++)
            {
                AddLateralFriction(HardPoints[i].position, Wheels[i], wheelHits[i], vehicleIsGrounded, suspensionForce[i], i);
            }

            //takeoff/landed events
            if (vehicleIsGrounded != vehicleWasGrounded)
            {
                if (vehicleIsGrounded) VehicleEvents.OnGrounded.Invoke();
                else VehicleEvents.OnTakeOff.Invoke();
            }
        }
#endregion// Unity Lifecycle

#region Inputs

        public void ProvideInputs(float _accelerationInput, float _steerInput, float _handbrakeInput)
        {
            if (CanDrive && CanAccelerate)
            {
                accelerationInput = _accelerationInput;
                steerInput = _steerInput;
                handbrakeInput = _handbrakeInput;
            }
            else if (CanDrive && !CanAccelerate)
            {
                accelerationInput = 0;
                steerInput = _steerInput;
                handbrakeInput = _handbrakeInput;
            }
            else
            {
                accelerationInput = 0;
                steerInput = 0;
                handbrakeInput = 1;
            }
        }

#endregion //Inputs

#region Acceleration/Brake/RollingResistance
        void Accelerate()
        {
            float angle = Vector3.Angle(transform.up, Vector3.up);
            float accelerationModifier = InclineAccelerationCurve.Evaluate(angle / 180f); // Assuming the curve is set up for a 0-1 range
            float adjustedAccelerationInput = accelerationInput * accelerationModifier;
            float deltaSpeed = Acceleration * adjustedAccelerationInput * Time.fixedDeltaTime;
            deltaSpeed = Mathf.Clamp(deltaSpeed, -MaxSpeed, MaxSpeed) * AccelerationCurve.Evaluate(Mathf.Abs(localVehicleVelocity.z / MaxSpeed));

            if (adjustedAccelerationInput > 0 && localVehicleVelocity.z < 0 || adjustedAccelerationInput < 0 && localVehicleVelocity.z > 0)
            {
                deltaSpeed = (1 + Mathf.Abs(localVehicleVelocity.z / MaxSpeed)) * Acceleration * adjustedAccelerationInput * Time.fixedDeltaTime;
            }

            for (int i = 0; i < 4; i++)
            {
                if (!wheelIsGrounded[i]) continue;
                
                Vector3 forward = Wheels[i].forward;
                float forwardDot = Vector3.Dot(forward, transform.forward);
                Vector3 force = forward * deltaSpeed * 0.25f * forwardDot;
                rb.AddForceAtPosition(force, HardPoints[i].position, ForceMode.VelocityChange);
            }
        }

        void AddRollingResistance()
        {
            if (GearSystem.isShiftingGear) return;

            float localSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

            float deltaSpeed = RollingResistance * Time.fixedDeltaTime * Mathf.Clamp01(Mathf.Abs(localSpeed));
            deltaSpeed = Mathf.Clamp(deltaSpeed, -MaxSpeed, MaxSpeed);
            if (accelerationInput == 0)
            {
                if (localSpeed > 0) rb.linearVelocity -= transform.forward * deltaSpeed;
                else rb.linearVelocity += transform.forward * deltaSpeed;
            }
        }

        void Brake()
        {
            float localSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            float deltaSpeed = brakeAcceleration * handbrakeInput * Time.fixedDeltaTime * Mathf.Clamp01(Mathf.Abs(localSpeed));
            deltaSpeed = Mathf.Clamp(deltaSpeed, -MaxSpeed, MaxSpeed);
            if (localSpeed > 0) rb.linearVelocity -= transform.forward * deltaSpeed;
            else rb.linearVelocity += transform.forward * deltaSpeed;
        }
#endregion//Acceleration/brake/RollingResistance

#region Suspension
        void AddSuspensionForce(Vector3 hardPoint, Transform wheel, float MaxSpringDistance, out RaycastHit wheelHit, out bool WheelIsGrounded, out float SuspensionForce, int WheelNum)
        {
            Ray ray = new Ray(hardPoint + wheel.up*wheelRadius, -wheel.up);
            WheelIsGrounded = Physics.SphereCast(ray, wheelRadius, out wheelHit, MaxSpringDistance, drivableLayers, QueryTriggerInteraction.Ignore);

            // suspension spring force
            if (WheelIsGrounded)
            {
                Vector3 springDir = wheelHit.normal;
                float offset = (MaxSpringDistance + (0.1f * vehicleScale) - wheelHit.distance) / (MaxSpringDistance - wheelRadius - (0.1f * vehicleScale));
                offset = Mathf.Clamp01(offset);
                compressions[WheelNum] = offset;
                float vel = -((offset - offset_Prev[WheelNum]) / Time.fixedDeltaTime);
                Vector3 wheelWorldVel = rb.GetPointVelocity(wheelHit.point);
                float WheelVel = Vector3.Dot(transform.up, wheelWorldVel);
                offset_Prev[WheelNum] = offset;
                if (offset < 0.3f) vel = 0;
                else if (vel < 0 && offset > 0.6f && WheelVel < 10) vel *= 10;

                float TotalSpringForce = offset * offset * springForce;
                float totalDampingForce = Mathf.Clamp(-(vel * springDamper), -0.25f * rb.mass * Mathf.Abs(WheelVel) / Time.fixedDeltaTime, 0.25f * rb.mass * Mathf.Abs(WheelVel) / Time.fixedDeltaTime);
                if ((MaxSpringDistance + 0.1f*vehicleScale - wheelHit.distance) < (0.1f * vehicleScale)) totalDampingForce = 0;

                float force = TotalSpringForce + totalDampingForce;
                SuspensionForce = force;

                Vector3 projectedSuspensionForce = Vector3.Project(springDir, transform.up) * force;
                rb.AddForceAtPosition(projectedSuspensionForce, hardPoint);
            }
            else
            {
                SuspensionForce = 0;
                compressions[WheelNum] = 0;
            }
        }
#endregion//Suspension

#region Friction
        public void AddLateralFriction(Vector3 hardPoint, Transform wheel, RaycastHit wheelHit, bool wheelIsGrounded, float suspensionForce, int wheelNum)
        {
            if (!wheelIsGrounded) return;

            Vector3 wheelVelocity = rb.GetPointVelocity(hardPoint);
            Vector3 sideVelocity = wheel.InverseTransformDirection(wheelVelocity).x * wheel.right;
            float sideSpeed = sideVelocity.magnitude;
            float forwardSpeed = Vector3.Dot(wheelVelocity, wheel.forward);
            slipCoeff[wheelNum] = sideSpeed / (sideSpeed + Mathf.Max(0.1f, forwardSpeed));
            float sideFriction = sideFrictionCurve.Evaluate(slipCoeff[wheelNum]);
            Vector3 friction = suspensionForce * FrictionCoefficient * -sideVelocity.normalized * sideFriction;
            friction *= forwardFrictionCurve.Evaluate(forwardSpeed / MaxSpeed);

            bool shouldDrift = wheelNum >= 2 && driftAngle < maxDriftAngle && handbrakeInput > 0.1f;
            if (shouldDrift) friction *= driftFrictionCurve.Evaluate(forwardSpeed / MaxSpeed);

            //clamp friction to reduce wobble
            Vector3 contactDesiredAccel = -Vector3.ProjectOnPlane(sideVelocity, wheelHit.normal) / Time.fixedDeltaTime;
            float clampedFrictionForce = Mathf.Min(rb.mass / 4 * contactDesiredAccel.magnitude, -Physics.gravity.y * rb.mass);
            friction = Vector3.ClampMagnitude(friction, clampedFrictionForce);

            // gravity friction
            float slopeAngle = Vector3.Angle(transform.up, Vector3.up);
            Vector3 gravityForce = Physics.gravity.y * (rb.mass / 4) * Vector3.up;
            Vector3 gravitySideFriction = -Vector3.Project(gravityForce, transform.right);
            Vector3 gravityForwardFriction = -Vector3.Project(gravityForce, transform.forward);

            if (slopeAngle > slopeSlideAngle)
            {
                gravitySideFriction = Vector3.zero;
                gravityForwardFriction = Vector3.zero;
            }

            rb.AddForceAtPosition(friction + gravitySideFriction, hardPoint);
            if (handbrakeInput > 0 || localVehicleVelocity.magnitude < 0.1f) rb.AddForce(gravityForwardFriction);
        }
#endregion//Friction

#region Steering
        void AckermannSteering(float steerInput)
        {
            float turnRadius = turnRadiusCurve.Evaluate(localVehicleVelocity.z / MaxSpeed);
            if (steerInput > 0) //is turning right
            {
                ackermennLeftAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + (rearTrack / 2))) * steerInput;
                ackermennRightAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - (rearTrack / 2))) * steerInput;
            }
            else if (steerInput < 0) //is turning left
            {
                ackermennLeftAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - (rearTrack / 2))) * steerInput;
                ackermennRightAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + (rearTrack / 2))) * steerInput;
            }
            else
            {
                ackermennLeftAngle = 0;
                ackermennRightAngle = 0;
            }

            // auto counter steering
            if (localVehicleVelocity.z > 0 && AutoCounterSteer && Mathf.Abs(localVehicleVelocity.x) > 1f)
            {
                ackermennLeftAngle += Vector3.SignedAngle(transform.forward, rb.linearVelocity + transform.forward, transform.up);
                ackermennLeftAngle = Mathf.Clamp(ackermennLeftAngle, -70, 70);
                ackermennRightAngle += Vector3.SignedAngle(transform.forward, rb.linearVelocity + transform.forward, transform.up);
                ackermennRightAngle = Mathf.Clamp(ackermennRightAngle, -70, 70);
            }

            Vector3 leftWheelEulerAngles = Wheels[0].localEulerAngles;
            if (leftWheelEulerAngles.y > 180) leftWheelEulerAngles.y -= 360;
            leftWheelEulerAngles.y = Mathf.Lerp(leftWheelEulerAngles.y, ackermennLeftAngle, tireTurnSpeed*Time.fixedDeltaTime);
            Wheels[0].localEulerAngles = leftWheelEulerAngles;

            Vector3 rightWheelEulerAngles = Wheels[0].localEulerAngles;
            if (rightWheelEulerAngles.y > 180) rightWheelEulerAngles.y -= 360;
            rightWheelEulerAngles.y = Mathf.Lerp(rightWheelEulerAngles.y, ackermennLeftAngle, tireTurnSpeed*Time.fixedDeltaTime);
            Wheels[1].localEulerAngles = rightWheelEulerAngles;
        }
#endregion//Steering

#region Visuals
        void UpdateTireVisuals(bool WheelIsGrounded, Transform wheel, Transform hardPoint, float hitDistance, int tireNum)
        {
            Vector3 wheelPos = wheel.localPosition;
            if (WheelIsGrounded)
            {
                if (offset_Prev[tireNum] > 0.3f)
                {
                    wheelPos = hardPoint.localPosition + (Vector3.up * wheelRadius) - Vector3.up * hitDistance;
                }
                else
                {
                    wheelPos = Vector3.Lerp(
                        new Vector3(hardPoint.localPosition.x, wheelPos.y, hardPoint.localPosition.z), 
                        hardPoint.localPosition + Vector3.up*wheelRadius - Vector3.up*hitDistance, 
                        0.1f
                    );
                }

                float maxY = hardPoint.localPosition.y + wheelRadius + maxWheelTravel - MaxSpringDistance;
                if (wheelPos.y > maxY) wheelPos.y = maxY;
            }
            else
            {
                wheelPos = Vector3.Lerp(
                    new Vector3(hardPoint.localPosition.x, wheelPos.y, hardPoint.localPosition.z), 
                    hardPoint.localPosition + (Vector3.up * wheelRadius) - Vector3.up * MaxSpringDistance, 
                    0.05f
                );
            }
            wheel.localPosition = wheelPos;

            Vector3 wheelVelocity = rb.GetPointVelocity(hardPoint.position);
            float minRotation = Vector3.Dot(wheelVelocity, wheel.forward) / wheelRadius * Time.fixedDeltaTime * Mathf.Rad2Deg;
            float maxRotation = Mathf.Sign(Vector3.Dot(wheelVelocity, wheel.forward)) * MaxSpeed / wheelRadius * Time.fixedDeltaTime * Mathf.Rad2Deg;
            float wheelRotation;
            if (Mathf.Abs(accelerationInput) > 0.1f)
            {
                wheel.GetChild(0).RotateAround(wheel.position, wheel.right, maxRotation / 2);
                wheelRotation = maxRotation;
            }
            else
            {
                wheel.GetChild(0).RotateAround(wheel.position, wheel.right, minRotation);
                wheelRotation = minRotation;
            }
            wheel.GetChild(0).localPosition = Vector3.zero;
            var rot = wheel.GetChild(0).localRotation;
            rot.y = 0;
            rot.z = 0;
            wheel.GetChild(0).localRotation = rot;

            //wheel slip calculation
            forwardSlip[tireNum] = Mathf.Abs(Mathf.Clamp((wheelRotation - minRotation) / maxRotation, -1, 1));
            if (!WheelIsGrounded) skidTotal[tireNum] = 0;
            else skidTotal[tireNum] = Mathf.MoveTowards(skidTotal[tireNum], (forwardSlip[tireNum] + slipCoeff[tireNum]) / 2, 0.05f);
        }

        void InitializeWheelSkid(int wheelNum, Skidmarks skidmarks, float radius)
        {
            wheelSkids[wheelNum].skidmarks = skidmarks;
            wheelSkids[wheelNum].radius = wheelRadius;
        }

        void UpdateWheelSkid(int wheelNum, float skidTotal, Vector3 skidPoint, Vector3 normal)
        {
            wheelSkids[wheelNum].skidTotal = skidTotal;
            wheelSkids[wheelNum].skidPoint = skidPoint;
            wheelSkids[wheelNum].normal = normal;
        }

        void AnimateBody()
        {
            Vector3 accel = Vector3.ProjectOnPlane((rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime, transform.up);
            accel = transform.InverseTransformDirection(accel);
            lastVelocity = rb.linearVelocity;
            Quaternion targetRotation = Quaternion.Euler(
                Mathf.Clamp(-accel.z / 10, -forwardBodyTilt, forwardBodyTilt), 
                0, 
                Mathf.Clamp(accel.x / 5, -sidewaysBodyTilt, sidewaysBodyTilt)
            );
            VehicleBody.localRotation = Quaternion.Lerp(VehicleBody.localRotation, targetRotation, 0.1f);
        }
#endregion//Visuals

#if UNITY_EDITOR
#region Editor
        [ContextMenu("Adjust Accele Curve By Gears")]
        public void AdjustAccelerationCurveByGears()
        {
            GearSystem = GetComponent<GearSystem>();
            float totalGears = GearSystem.gearSpeeds.Length;
            AnimationCurve accelCurve = new AnimationCurve();

            for (int i = 0; i < totalGears; i++)
            {
                float t = GearSystem.gearSpeeds[i] / MaxSpeed;
                float v = 1 - (i / totalGears);
                accelCurve.AddKey(t, v);
            }
            AccelerationCurve = accelCurve;

            // Mark the object as dirty so that Unity saves the changes
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;

            for (int i = 0; i < Wheels.Length; i++)
            {
                Gizmos.DrawLine(HardPoints[i].position + (transform.up * wheelRadius), Wheels[i].position);
                Gizmos.DrawWireSphere(Wheels[i].position, wheelRadius);
                Gizmos.DrawSphere(HardPoints[i].position + (transform.up * wheelRadius), 0.05f);

                UnityEditor.Handles.color = Color.red;
                UnityEditor.Handles.ArrowHandleCap(0, Wheels[i].position + transform.up * wheelRadius, Wheels[i].rotation * Quaternion.LookRotation(Vector3.up), maxWheelTravel, EventType.Repaint);
            }
        }
#endregion//Editor
#endif
    }
}