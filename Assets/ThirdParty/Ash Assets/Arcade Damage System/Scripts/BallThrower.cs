using UnityEngine;

namespace Ash_ADS
{
    public class BallThrower : MonoBehaviour
    {
        // Assign your ball prefab in the Inspector.
        public GameObject ballPrefab;

        // Flight time in seconds for the ball to reach the target.
        public float flightTime = 1.0f;

        // Total number of balls to pool.
        public int poolSize = 10;
        private GameObject[] ballPool;
        private int poolIndex = 0;

        void Start()
        {
            // Pre-instantiate the pool of balls.
            ballPool = new GameObject[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                ballPool[i] = Instantiate(ballPrefab);
                // Initially disable the ball until it's thrown.
                ballPool[i].SetActive(false);
            }
        }

        void Update()
        {
            // On left mouse button click.
            if (Input.GetMouseButtonDown(0))
            {
                // Spawn position is the camera's position.
                Vector3 spawnPosition = Camera.main.transform.position;

                // Cast a ray from the camera through the mouse position.
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    // Get the target hit point.
                    Vector3 targetPoint = hit.point;

                    // Reuse a ball from the pool.
                    GameObject ball = ballPool[poolIndex];
                    poolIndex = (poolIndex + 1) % poolSize;

                    // Reset ball's position, rotation, and re-enable it.
                    ball.transform.position = spawnPosition;
                    ball.transform.rotation = Quaternion.identity;
                    ball.SetActive(true);

                    Rigidbody rb = ball.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        Debug.LogError("The ball prefab must have a Rigidbody component.");
                        return;
                    }

                    // Reset physics state.
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    // Compute displacement from the spawn position to the target.
                    Vector3 displacement = targetPoint - spawnPosition;
                    // Get the gravity from the physics engine.
                    Vector3 gravity = Physics.gravity;

                    // Calculate the initial velocity required to hit the target in 'flightTime' seconds.
                    // Formula: initialVelocity = (displacement - 0.5 * gravity * flightTime^2) / flightTime
                    Vector3 initialVelocity = (displacement - 0.5f * gravity * flightTime * flightTime) / flightTime;

                    // Set the ball's velocity.
                    rb.linearVelocity = initialVelocity;

                }
                else
                {
                    Debug.Log("No collider was hit by the ray.");
                }
            }
        }
    }


}
