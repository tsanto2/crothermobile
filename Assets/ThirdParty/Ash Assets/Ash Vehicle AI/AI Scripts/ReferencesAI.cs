using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshVP
{
	public class ReferencesAI : MonoBehaviour
	{
		public Transform GroundRayPt;


		public Transform wheels;

		public Transform wheelFL;
		public Transform wheelFR;
		public Transform wheelRL;
		public Transform wheelRR;


		private void OnEnable()
		{
			transform.GetComponent<VehicleEditorAI>().enabled = false;
		}
		private void OnDisable()
		{
			transform.GetComponent<VehicleEditorAI>().enabled = true;
		}
	}
}
