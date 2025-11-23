using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshVP
{
    public class GetReference : MonoBehaviour
    {
        private Rigidbody rb;
        public float CurrentSpeedMps;
        public float CurrentSpeedKmph;
        public int currentGear = 0;
        [Serializable]
        private class GearShifts
        {
            public int GearShiftNumber;
            public float ShiftAt;
        }

        [SerializeField] GearShifts[] gearShifts;
        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnDrawGizmosSelected()
        {
            if (gearShifts == null)
            {
                return;
            }
            else
            {
                for (int i = 0; i < gearShifts.Length; i++)
                {
                    gearShifts[i].GearShiftNumber = i + 1;
                }
            }

        }
        private void Update()
        {
            CurrentSpeedKmph = CurrentSpeedMps * 3.6f;
        }

        void FixedUpdate()
        {
            CurrentSpeedMps = transform.InverseTransformDirection(rb.linearVelocity).magnitude;


            for (int i = 0; i < gearShifts.Length; i++)
            {
                if (CurrentSpeedMps < 0)
                {
                    currentGear = 0;
                }
                else if (i == 0 && 0 < CurrentSpeedMps && CurrentSpeedMps < gearShifts[i].ShiftAt * 100)
                {
                    currentGear = gearShifts[i].GearShiftNumber;
                }
                else if (CurrentSpeedMps < gearShifts[i].ShiftAt * 100)
                {
                    return;
                }
                else if (i == gearShifts.Length - 1 && gearShifts[i - 1].ShiftAt * 100 < CurrentSpeedMps && CurrentSpeedMps < gearShifts[i].ShiftAt * 100)
                {
                    currentGear = gearShifts[i].GearShiftNumber;
                }
                else
                {
                    currentGear = gearShifts[i].GearShiftNumber + 1;
                }

            }

        }
    }
}
