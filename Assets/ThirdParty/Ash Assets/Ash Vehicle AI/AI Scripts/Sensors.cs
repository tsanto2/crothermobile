using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AshVP
{
    public class Sensors : MonoBehaviour
    {
        [Serializable]
        private class Sensor
        {
            [HideInInspector]
            public float weight;
            public Transform sensorPoint;
            [HideInInspector]
            public float direction;
            [HideInInspector]
            public RaycastHit hit;
        }

        [SerializeField] Sensor[] sensorArray;

        [HideInInspector]
        public float turnmultiplyer;
        public float sensorLength;
        [HideInInspector]
        public float SensorTurnAmount;
        [HideInInspector]
        public bool obstacleInPath;
        [HideInInspector]
        public float ObstacleAngle;
        public string IgnoreSensorTag;



        void FixedUpdate()
        {
            foreach (Sensor sensor_ in sensorArray)
            {
                if (sensor_.sensorPoint.localPosition.x == 0)
                {
                    sensor_.direction = 0;
                }
                else
                {
                    sensor_.direction = sensor_.sensorPoint.localPosition.x / Mathf.Abs(sensor_.sensorPoint.localPosition.x);
                }

                if (Physics.Raycast(sensor_.sensorPoint.position, sensor_.sensorPoint.forward, out sensor_.hit, sensorLength))
                {
                    if (sensor_.hit.collider.CompareTag(IgnoreSensorTag))
                    {
                        sensor_.weight = 0;
                    }
                    else
                    {
                        sensor_.weight = 1;

                        Debug.DrawLine(sensor_.sensorPoint.position, sensor_.hit.point, Color.red);
                    }

                }
                else
                {
                    sensor_.weight = 0;
                }
            }

            obstacleInPath = IsobstacleInPath();

            SensorTurnAmount = SensorValue(sensorArray);
            //turnmultiplyer = Mathf.Sign(SensorTurnAmount);

            if (SensorTurnAmount == 0 && obstacleInPath)
            {
                ObstacleAngle = Vector3.Dot(sensorArray[1].hit.normal, transform.right);

                if (ObstacleAngle > 0)
                {
                    turnmultiplyer = -1;
                }
                if (ObstacleAngle < 0)
                {
                    turnmultiplyer = 1;
                }

            }
            else
            {
                turnmultiplyer = Mathf.Sign(SensorTurnAmount);
            }




        }

        bool IsobstacleInPath()
        {

            for (int i = 0; i < sensorArray.Length; i++)
            {
                if (sensorArray[i].weight == 1)
                {
                    return true;
                }
            }
            return false;

        }

        float SensorValue(Sensor[] sensors)
        {
            float Sensorvalue = 0;
            for (int i = 0; i < sensors.Length; i++)
            {
                Sensorvalue += sensors[i].weight * sensors[i].direction;
            }
            return Sensorvalue;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            foreach (Sensor sensor_ in sensorArray)
            {
                if (sensor_.weight == 0)
                {
                    Gizmos.DrawRay(sensor_.sensorPoint.position, sensor_.sensorPoint.forward * sensorLength);
                }
            }
        }
    }
}
