using System.Collections;
using UnityEngine;
using Unity;

namespace Ashsvp
{
    public class GearSystem : MonoBehaviour
    {
        public float VehicleSpeed;
        public int currentGear;
        private SimcadeVehicleController vehicleController;
        public int[] gearSpeeds = new int[] { 40, 80, 120, 160, 220 };

        public AudioSystem AudioSystem;

        public bool isShiftingGear = false;

        private int currentGearTemp;
        void Start()
        {
            vehicleController = GetComponent<SimcadeVehicleController>();
            currentGear = 1;

        }

        void Update()
        {
            float velocityMag = Vector3.ProjectOnPlane( vehicleController.localVehicleVelocity, transform.up).magnitude;
            if (vehicleController.vehicleIsGrounded)
            {
                velocityMag = vehicleController.localVehicleVelocity.magnitude;
            }

            VehicleSpeed = velocityMag; //car speed in Km/hr

            gearShift();


        }


        void gearShift()
        {
            for (int i = 0; i < gearSpeeds.Length; i++)
            {
                if (VehicleSpeed > gearSpeeds[i])
                {
                    currentGear = i + 1;
                }
                else break;
            }
            if (CurrentGearProperty != currentGear)
            {
                CurrentGearProperty = currentGear;
            }

        }

        public int CurrentGearProperty
        {
            get
            {
                return currentGearTemp;
            }

            set
            {
                currentGearTemp = value;

                if (vehicleController.accelerationInput > 0 && vehicleController.localVehicleVelocity.z > 0 && !AudioSystem.GearSound.isPlaying && vehicleController.vehicleIsGrounded)
                {
                    vehicleController.VehicleEvents.OnGearChange.Invoke();
                    AudioSystem.GearSound.Play();
                    StartCoroutine(shiftingGear());
                }

                AudioSystem.engineSound.volume = 0.5f;
            }
        }

        IEnumerator shiftingGear()
        {
            vehicleController.CanAccelerate = false;
            isShiftingGear = true;
            yield return new WaitForSeconds(0.3f);
            vehicleController.CanAccelerate = true;
            isShiftingGear = false;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Adjust GearSpeeds")]
        public void AutoAdjustGearSpeeds()
        {
            // Get the vehicle controller (assumes one is attached)
            vehicleController = GetComponent<SimcadeVehicleController>();

            // Register the undo operation for the gearSpeeds array
            UnityEditor.Undo.RecordObject(this, "Auto Adjust GearSpeeds");

            int numberOfGears = gearSpeeds.Length;
            if (numberOfGears <= 0)
            {
                Debug.LogWarning("No gears defined!");
                return;
            }

            // Define the top gear's speed as 95% of the maximum speed
            float topGearSpeed = vehicleController.MaxSpeed * 0.95f;
            // Set the first gear's speed to a base speed (e.g., 15% of max speed)
            float baseSpeed = vehicleController.MaxSpeed * 0.15f;

            // If there's only one gear, assign it to the top gear speed
            if (numberOfGears == 1)
            {
                gearSpeeds[0] = Mathf.RoundToInt(topGearSpeed);
            }
            else
            {
                // Calculate the multiplier needed so that:
                // baseSpeed * multiplier^(numberOfGears-1) = topGearSpeed
                float multiplier = Mathf.Pow(topGearSpeed / baseSpeed, 1f / (numberOfGears - 1));

                // Set each gear speed using a geometric progression
                for (int i = 0; i < numberOfGears; i++)
                {
                    float gearSpeed = baseSpeed * Mathf.Pow(multiplier, i);
                    gearSpeeds[i] = Mathf.RoundToInt(gearSpeed);
                }
            }

            // Mark the object as dirty so that Unity saves the changes
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif


    }
}
