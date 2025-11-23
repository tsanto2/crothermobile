using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshVP
{
    public class ReverseWhenStuck : MonoBehaviour
    {
        private AiCarContrtoller AiCarContrtoller;
        private Rigidbody rb;
        public float minVelocityToReverse;
        public Transform groundCheck;
        private float speedValue;
        public float reverseTime;
        private float velocity;
        private float timeToStartReverse;
        private float reverseStartTime = 3;

        bool TakingReverse = false;

        void Start()
        {
            AiCarContrtoller = GetComponent<AiCarContrtoller>();
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            velocity = rb.linearVelocity.magnitude;


            if (velocity < minVelocityToReverse)
            {
                timeToStartReverse += Time.fixedDeltaTime;
            }
            else
            {
                timeToStartReverse = 0;
            }

            if (timeToStartReverse > reverseStartTime)
            {
                StartCoroutine(startReverse());
                timeToStartReverse = 0;
            }

            if (TakingReverse)
            {
                TakeReverse();
            }
        }

        public void TakeReverse()
        {
            AiCarContrtoller.carVelocity = transform.InverseTransformDirection(rb.linearVelocity);
            AiCarContrtoller.tireVisuals();
            speedValue = AiCarContrtoller.accelerationForce * Time.fixedDeltaTime * 1000 
                * AiCarContrtoller.ReverseCurve.Evaluate(Mathf.Abs(AiCarContrtoller.carVelocity.z) / 100);
            rb.AddForceAtPosition(-groundCheck.forward * speedValue, groundCheck.position);
        }

        IEnumerator startReverse()
        {
            float timePassed = 0;
            while (timePassed < reverseTime)
            {
                TakingReverse = true;
                AiCarContrtoller.enabled = false;

                timePassed += Time.deltaTime;

                yield return null;
            }
            TakingReverse = false;
            AiCarContrtoller.enabled = true;
            timeToStartReverse = 0f;
        }

    }
}
