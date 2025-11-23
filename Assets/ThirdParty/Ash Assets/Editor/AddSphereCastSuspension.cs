using UnityEngine;
using UnityEditor;
using System;

namespace AshVP
{
    public class AddSphereCastSuspension : EditorWindow
    {
        Transform ConfiguredVehicle;

        private GameObject suspensionPoints;

        [MenuItem("Tools/Ash Vehicle Physics/Add SphereCast Suspension")]

        static void OpenWindow()
        {
            AddSphereCastSuspension AddSphereCastSuspensionWindow = (AddSphereCastSuspension)GetWindow(typeof(AddSphereCastSuspension));
            AddSphereCastSuspensionWindow.minSize = new Vector2(300, 70);
            AddSphereCastSuspensionWindow.Show();
        }


        private void OnGUI()
        {
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.green;
            GUILayout.Label("Add SphereCast Suspension", style);
            ConfiguredVehicle = EditorGUILayout.ObjectField("Configured Vehicle", ConfiguredVehicle, typeof(Transform), true) as Transform;

            if (GUILayout.Button("Add SphereCast Suspension"))
            {
                AddAshSuspension();
            }

        }

        private void AddAshSuspension()
        {
            suspensionPoints = (GameObject)Resources.Load("SuspensionPoints");

            Transform temp_SuspensionPoints = Instantiate(suspensionPoints, ConfiguredVehicle).transform;

            temp_SuspensionPoints.GetChild(0).GetComponent<AshSuspension>().wheel = ConfiguredVehicle.transform.Find("wheels").Find("FL rb");
            temp_SuspensionPoints.GetChild(1).GetComponent<AshSuspension>().wheel = ConfiguredVehicle.transform.Find("wheels").Find("FR rb");
            temp_SuspensionPoints.GetChild(2).GetComponent<AshSuspension>().wheel = ConfiguredVehicle.transform.Find("wheels").Find("RL rb");
            temp_SuspensionPoints.GetChild(3).GetComponent<AshSuspension>().wheel = ConfiguredVehicle.transform.Find("wheels").Find("RR rb");

            temp_SuspensionPoints.GetChild(0).GetComponent<AshSuspension>().carRigidBody = ConfiguredVehicle.GetComponent<Rigidbody>();
            temp_SuspensionPoints.GetChild(1).GetComponent<AshSuspension>().carRigidBody = ConfiguredVehicle.GetComponent<Rigidbody>();
            temp_SuspensionPoints.GetChild(2).GetComponent<AshSuspension>().carRigidBody = ConfiguredVehicle.GetComponent<Rigidbody>();
            temp_SuspensionPoints.GetChild(3).GetComponent<AshSuspension>().carRigidBody = ConfiguredVehicle.GetComponent<Rigidbody>();
        }
    }
}
