using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AshVP
{

	public class AiCarContrtoller : MonoBehaviour
	{
		[Header("Suspension")]
		[Range(0, 5)]
		public float SuspensionDistance = 0.2f;
		public float suspensionForce = 30000f;
		public float suspensionDamper = 200f;
		public Transform groundCheck;
		public Transform fricAt;
		public Transform CenterOfMass;

		private Rigidbody rb;

		[Header("Car Stats")]
		public float accelerationForce = 200f;
		public float turnTorque = 100f;
		public float brakeForce = 150f;
		public float frictionForce = 70f;
		public float dragAmount = 4f;
		public float TurnAngle = 30f;

		public float maxRayLength = 0.8f, slerpTime = 0.2f;
		[HideInInspector]
		public bool grounded;

		public Transform TargetTransform;
		[Header("Visuals")]
		public Transform[] TireMeshes;
		public Transform[] TurnTires;

		[Header("Curves")]
		public AnimationCurve frictionCurve;
		public AnimationCurve accelerationCurve;
		public bool separateReverseCurve = false;
		public AnimationCurve ReverseCurve;
		public AnimationCurve turnCurve;
		public AnimationCurve driftCurve;
		public AnimationCurve engineCurve;

		private float speedValue, fricValue, turnValue, curveVelocity, brakeInput;
		[HideInInspector]
		public Vector3 carVelocity;
		[HideInInspector]
		public RaycastHit hit;

		[Header("Other Settings")]
		public AudioSource[] engineSounds;
		public bool airDrag;
		public float SkidEnable = 20f;
		public float skidWidth = 0.12f;
		private float frictionAngle;

		//Ai stuff
		[HideInInspector]
		public float TurnAI = 1f;
		[HideInInspector]
		public float SpeedAI = 1f;
		[HideInInspector]
		public float brakeAI = 0f;
		private Vector3 targetPosition;
		public float brakeAngle = 30f;

		public Sensors sensorScript;

		private float desiredTurning;

		private float VehicleGravity = -30;
		private Vector3 centerOfMass_ground;

		private bool StopVehicle;

		public void start_Vehicle()
		{
			StopVehicle = false;
			if (GetComponent<ReverseWhenStuck>())
			{
				GetComponent<ReverseWhenStuck>().enabled = true;
			}
		}

		public void stop_Vehicle()
		{
			StopVehicle = true;
            if (GetComponent<ReverseWhenStuck>())
            {
				GetComponent<ReverseWhenStuck>().enabled = false;
			}
		}


		private void Awake()
		{
			rb = GetComponent<Rigidbody>();
			grounded = false;
			engineSounds[1].mute = true;
			rb.centerOfMass = CenterOfMass.localPosition;
			Vector3 centerOfMass_ground_temp = Vector3.zero;
            for (int i = 0; i < TireMeshes.Length; i++)
            {
				centerOfMass_ground_temp += TireMeshes[i].parent.parent.localPosition;
			}
			centerOfMass_ground_temp.y = 0;
			centerOfMass_ground = centerOfMass_ground_temp / 4;


			if (GetComponent<GravityCustom>())
			{
				VehicleGravity = GetComponent<GravityCustom>().gravity;
			}
			else
			{
				VehicleGravity = Physics.gravity.y;
			}
		}
        private void Start()
        {
			start_Vehicle();
		}

        void FixedUpdate()
		{
			carVelocity = transform.InverseTransformDirection(rb.linearVelocity); //local velocity of car

			curveVelocity = Mathf.Abs(carVelocity.magnitude) / 100;

			//inputs
			float turnInput;
			if (sensorScript.obstacleInPath == true)
			{
				turnInput = (StopVehicle == true)? 0 : turnTorque * -sensorScript.turnmultiplyer * Time.fixedDeltaTime * 1000;
			}
			else
			{
				turnInput = (StopVehicle == true) ? 0 : turnTorque * TurnAI * Time.fixedDeltaTime * 1000;
			}

			float speedInput = (StopVehicle == true) ? 0 : accelerationForce * SpeedAI * Time.fixedDeltaTime * 1000;
			brakeInput = (StopVehicle == true) ? brakeForce * Time.fixedDeltaTime * 1000 : brakeForce * -brakeAI * Time.fixedDeltaTime * 1000;
			brakeInput *= Mathf.Clamp01(carVelocity.magnitude);

			//helping veriables
			speedValue = speedInput * accelerationCurve.Evaluate(Mathf.Abs(carVelocity.z) / 100);
			if (separateReverseCurve && carVelocity.z < 0 && speedInput < 0)
			{
				speedValue = speedInput * ReverseCurve.Evaluate(Mathf.Abs(carVelocity.z) / 100);
			}
			fricValue = frictionForce * frictionCurve.Evaluate(carVelocity.magnitude / 100);
			//turnValue = turnInput * turnCurve.Evaluate(carVelocity.magnitude / 100);


			// the new method of calculating turn value
			Vector3 aimedPoint = TargetTransform.position;
			aimedPoint.y = transform.position.y;
			Vector3 aimedDir = (aimedPoint - transform.position).normalized;
			Vector3 myDir = transform.forward;
			myDir.y = 0;
			myDir.Normalize();
			desiredTurning = Mathf.Abs(Vector3.Angle(myDir, aimedDir));
			turnValue = turnInput * turnCurve.Evaluate(desiredTurning / TurnAngle);


			//grounded check
			if (Physics.Raycast(groundCheck.position, -transform.up, out hit, maxRayLength))
			{
				accelarationLogic();
				turningLogic();
				frictionLogic();
				brakeLogic();
				//for drift behaviour
				rb.angularDamping = dragAmount * driftCurve.Evaluate(Mathf.Abs(carVelocity.x) / 70);

				//draws green ground checking ray ....ingnore
				Debug.DrawLine(groundCheck.position, hit.point, Color.green);
				grounded = true;
				rb.linearDamping = 0.1f;
                if (StopVehicle)
                {
					rb.linearDamping = 5f;
				}

				rb.centerOfMass = centerOfMass_ground;

			}
			else
			{
				grounded = false;
				rb.linearDamping = 0.1f;
				rb.centerOfMass = CenterOfMass.localPosition;
				if (!airDrag)
				{
					rb.angularDamping = 0.1f;
				}

			}
		}

		void Update()
		{
			tireVisuals();
			audioControl();
			SetTargetPosition(TargetTransform.position);

			float reachedTargetDistance = 1f;
			float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
			Vector3 dirToMovePosition = (targetPosition - transform.position).normalized;
			float dot = Vector3.Dot(transform.forward, dirToMovePosition);
			float angleToMove = Vector3.Angle(transform.forward, dirToMovePosition);
			if (angleToMove > brakeAngle || sensorScript.obstacleInPath)
			{
				if (carVelocity.z > 15)
				{
					brakeAI = -1;
				}
				else
				{
					brakeAI = 0;
				}

			}
			else { brakeAI = 0; }

			if (distanceToTarget > reachedTargetDistance)
			{
				//Target is still far , keep acelarating 

				if (dot > 0)
				{
					SpeedAI = 1f;

					float stoppingDistance = 5f;
					float stoppingSpeed = 5f;
					if (distanceToTarget < stoppingDistance && curveVelocity > stoppingSpeed)
					{
						//brakeAI = -1f;
					}
					else
					{
						//brakeAI = 0f;
					}
				}
				else
				{
					float reverseDistance = 5f;
					if (distanceToTarget > reverseDistance)
					{
						SpeedAI = 1f;
					}
					else
					{
						//brakeAI = -1f;
					}
				}

				float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);

				if (angleToDir > 0)
				{
					TurnAI = 1f * turnCurve.Evaluate(desiredTurning / TurnAngle);
				}
				else
				{
					TurnAI = -1f * turnCurve.Evaluate(desiredTurning / TurnAngle);
				}

			}
			else // reached target
			{
				if (carVelocity.z > 1f)
				{
					//brakeAI = -1f;
				}
				else
				{
					//brakeAI = 0f;
				}
				TurnAI = 0f;
			}



		}

		public void SetTargetPosition(Vector3 TargetPos)
		{
			targetPosition = TargetPos;
		}

		public void audioControl()
		{
			//audios
			if (grounded)
			{
				if (Mathf.Abs(carVelocity.x) > SkidEnable - 0.1f)
				{
					engineSounds[1].mute = false;
				}
				else { engineSounds[1].mute = true; }
			}
			else
			{
				engineSounds[1].mute = true;
			}

			engineSounds[1].pitch = 1f;

			engineSounds[0].pitch = 2 * engineCurve.Evaluate(curveVelocity);
			if (engineSounds.Length == 2)
			{
				return;
			}
			else { engineSounds[2].pitch = 2 * engineCurve.Evaluate(curveVelocity); }



		}

		public void tireVisuals()
		{
			//Tire mesh rotate
			foreach (Transform mesh in TireMeshes)
			{
				mesh.transform.RotateAround(mesh.transform.position, mesh.transform.right, carVelocity.z / 3);
			}

			//TireTurn
			foreach (Transform FM in TurnTires)
			{

				if (sensorScript.obstacleInPath == true)
				{
					FM.localRotation = Quaternion.Slerp(FM.localRotation, Quaternion.Euler(FM.localRotation.eulerAngles.x,
					Mathf.Clamp(desiredTurning, desiredTurning, TurnAngle) * -sensorScript.turnmultiplyer, FM.localRotation.eulerAngles.z), slerpTime);
				}
				else
				{
					FM.localRotation = Quaternion.Slerp(FM.localRotation, Quaternion.Euler(FM.localRotation.eulerAngles.x,
					Mathf.Clamp(desiredTurning, desiredTurning, TurnAngle) * TurnAI, FM.localRotation.eulerAngles.z), slerpTime);

				}
			}
		}

		public void accelarationLogic()
		{
			//speed control
			if (SpeedAI > 0.1f)
			{
				rb.AddForceAtPosition(transform.forward * speedValue, groundCheck.position);
			}
			if (SpeedAI < -0.1f)
			{
				rb.AddForceAtPosition(transform.forward * speedValue, groundCheck.position);
			}
		}

		public void turningLogic()
		{
			//turning
			if (carVelocity.z > 0.1f)
			{
				rb.AddTorque(transform.up * turnValue);
			}

			if (carVelocity.z < -0.1f)
			{
				rb.AddTorque(transform.up * -turnValue);
			}
		}


		public void frictionLogic()
		{
			Vector3 sideVelocity = carVelocity.x * transform.right;

			Vector3 contactDesiredAccel = -sideVelocity / Time.fixedDeltaTime;

			float clampedFrictionForce = rb.mass * contactDesiredAccel.magnitude;

			Vector3 gravityForce = VehicleGravity * rb.mass * Vector3.up;

			Vector3 gravityFriction = -Vector3.Project(gravityForce, transform.right);

			Vector3 maxfrictionForce = Vector3.ClampMagnitude(fricValue * 50 * (-sideVelocity.normalized), clampedFrictionForce);
			rb.AddForceAtPosition(maxfrictionForce + gravityFriction, fricAt.position);
		}

		public void brakeLogic()
		{
			//brake
			if (carVelocity.z > 0.1f)
			{
				rb.AddForceAtPosition(transform.forward * -brakeInput, groundCheck.position);
			}
			if (carVelocity.z < -0.1f)
			{
				rb.AddForceAtPosition(transform.forward * brakeInput, groundCheck.position);
			}
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{

			if (!Application.isPlaying)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine(groundCheck.position, groundCheck.position - maxRayLength * groundCheck.up);
				Gizmos.DrawWireCube(groundCheck.position - maxRayLength * (groundCheck.up.normalized), new Vector3(5, 0.02f, 10));
				Gizmos.color = Color.magenta;
				if (GetComponent<BoxCollider>())
				{
					var Box_collider = GetComponent<BoxCollider>();
					Gizmos.DrawWireCube(Box_collider.bounds.center, GetComponent<BoxCollider>().size);
				}



				Gizmos.color = Color.red;
				foreach (Transform mesh in TireMeshes)
				{
					var ydrive = mesh.parent.parent.GetComponent<ConfigurableJoint>().yDrive;
					ydrive.positionDamper = suspensionDamper;
					ydrive.positionSpring = suspensionForce;


					mesh.parent.parent.GetComponent<ConfigurableJoint>().yDrive = ydrive;

					var jointLimit = mesh.parent.parent.GetComponent<ConfigurableJoint>().linearLimit;
					jointLimit.limit = SuspensionDistance;
					mesh.parent.parent.GetComponent<ConfigurableJoint>().linearLimit = jointLimit;

					Handles.color = Color.red;
					Handles.ArrowHandleCap(0, mesh.position, mesh.rotation * Quaternion.LookRotation(Vector3.up), jointLimit.limit, EventType.Repaint);
					Handles.ArrowHandleCap(0, mesh.position, mesh.rotation * Quaternion.LookRotation(Vector3.down), jointLimit.limit, EventType.Repaint);

				}
				float wheelRadius = TurnTires[0].parent.GetComponent<SphereCollider>().radius;
				float wheelYPosition = TurnTires[0].parent.parent.localPosition.y + TurnTires[0].parent.localPosition.y;
				maxRayLength = (groundCheck.localPosition.y - wheelYPosition + (0.05f + wheelRadius));

			}

		}
#endif
	}
}

