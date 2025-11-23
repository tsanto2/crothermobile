using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshVP
{
	public class SkidMarksAi : MonoBehaviour
	{
		private TrailRenderer skidMark;
		private ParticleSystem smoke;
		public AiCarContrtoller carController;
		private void Awake()
		{
			smoke = GetComponent<ParticleSystem>();
			skidMark = GetComponent<TrailRenderer>();
			skidMark.emitting = false;
			transform.localPosition = new Vector3(0, -transform.parent.parent.GetComponent<SphereCollider>().radius + 0.03f, 0);
			skidMark.startWidth = carController.skidWidth;
		}


		private void OnEnable()
		{
			skidMark.enabled = true;
		}
		private void OnDisable()
		{
			skidMark.enabled = false;
		}

		// Update is called once per frame
		void Update()
		{
			Vector3 velocity = carController.carVelocity;


			if (carController.grounded)
			{

				if (Mathf.Abs(velocity.x) > carController.SkidEnable)
				{
					skidMark.emitting = true;
				}
				else
				{
					skidMark.emitting = false;
				}
			}
			else
			{
				skidMark.emitting = false;
			}

			// smoke
			if (skidMark.emitting == true)
			{
				smoke.Play();
			}
			else { smoke.Stop(); }

		}
	}
}
