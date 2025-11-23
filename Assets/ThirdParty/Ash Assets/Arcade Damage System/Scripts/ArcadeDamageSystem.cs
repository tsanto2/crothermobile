using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Events;
using System;

namespace Ash_ADS
{
    public class ArcadeDamageSystem : MonoBehaviour
    {
        [Header("Mesh and Collider Settings")]
        [Tooltip("List of mesh filters representing the Object parts to be deformed.")]
        public List<MeshFilter> ObjectMeshFilters = new List<MeshFilter>();

        [Tooltip("List of mesh colliders corresponding to the mesh filters. Used to update collision detection after deformation.")]
        public List<MeshCollider> ObjectMeshColliders = new List<MeshCollider>();

        [Tooltip("If true, colliders will be updated to match the deformed meshes.")]
        public bool updateColliders = false;

        [Header("Deformation Settings")]
        [Tooltip("Intensity of the deformation effect applied on collision impacts.")]
        public float deformationIntensity = 1f;

        [Tooltip("Controls the smoothness of the deformation. Higher values result in smoother deformations.")]
        public float deformationSmoothness = 0.5f;

        [Tooltip("Radius of influence around the impact point within which vertices will be deformed.")]
        [Range(0.05f, 5f)]
        public float deformationRadius = 0.5f;

        [Tooltip("Maximum number of deformations allowed per vertex to prevent over-distortion.")]
        public int maxDeformationsPerVertex = 3;

        [Tooltip("Minimum collision impulse required to trigger the mesh deformation.")]
        public float collisionImpulseThreshold = 7000f;

        [Header("Unity Events")]
        [Tooltip("Event invoked after a collision leads to mesh deformation.")]
        public UnityEvent onDeformationApplied;

        [Tooltip("Event invoked when all mesh deformations are reset to their original state.")]
        public UnityEvent onDeformationReset;

        // Lists to store per-part mesh data
        private List<Mesh> deformingMeshes = new List<Mesh>();
        private List<Vector3[]> originalVerticesList = new List<Vector3[]>();
        private List<Vector3[]> currentVerticesList = new List<Vector3[]>();
        private List<Dictionary<int, int>> deformationCountsList = new List<Dictionary<int, int>>();

        private Vector3 lastContactPoint;
        private bool showDeformationGizmos = false;

        private void Start()
        {
            InitializeMeshes();
        }

        /// <summary>
        /// Populates the per-part mesh data arrays from the objectMeshFilters list.
        /// This must be called after the mesh filters have been assigned or detected.
        /// </summary>
        private void InitializeMeshes()
        {
            // Clear previous data in case this function is called more than once.
            deformingMeshes.Clear();
            originalVerticesList.Clear();
            currentVerticesList.Clear();
            deformationCountsList.Clear();

            for (int i = 0; i < ObjectMeshFilters.Count; i++)
            {
                MeshFilter mf = ObjectMeshFilters[i];
                if (mf != null)
                {
                    Mesh mesh = mf.mesh;
                    deformingMeshes.Add(mesh);

                    Vector3[] originalVertices = mesh.vertices;
                    originalVerticesList.Add(originalVertices);

                    Vector3[] currentVertices = new Vector3[originalVertices.Length];
                    System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);
                    currentVerticesList.Add(currentVertices);

                    // Set the mesh to be dynamic for efficient modifications.
                    mesh.vertices = currentVertices;
                    mesh.MarkDynamic();

                    deformationCountsList.Add(new Dictionary<int, int>(originalVertices.Length));
                }
            }
        }

        /// <summary>
        /// Applies deformation across all mesh parts using a world-space distance from the impact point.
        /// </summary>
        public void ApplyGlobalDeformation(Vector3 contactPoint, Vector3 contactNormal)
        {
            lastContactPoint = contactPoint;
            showDeformationGizmos = true;

            // Optionally, you can scale the effective radius based on collision impulse.
            float effectiveRadius = deformationRadius;

            // Loop over each mesh part.
            for (int i = 0; i < ObjectMeshFilters.Count; i++)
            {
                MeshFilter mf = ObjectMeshFilters[i];
                if (mf == null)
                    continue;

                Mesh mesh = deformingMeshes[i];
                Vector3[] currentVertices = currentVerticesList[i];
                Dictionary<int, int> deformationCounts = deformationCountsList[i];

                // Create NativeArray for vertices.
                NativeArray<Vector3> vertices = new NativeArray<Vector3>(currentVertices, Allocator.TempJob);
                // Create NativeArray for deformation counts.
                NativeArray<int> counts = new NativeArray<int>(currentVertices.Length, Allocator.TempJob);
                for (int k = 0; k < currentVertices.Length; k++)
                {
                    counts[k] = deformationCounts.ContainsKey(k) ? deformationCounts[k] : 0;
                }

                // Prepare transformation data.
                Matrix4x4 localToWorld = mf.transform.localToWorldMatrix;
                Vector3 localNormal = mf.transform.InverseTransformDirection(contactNormal).normalized;

                // Setup and schedule the job.
                DeformJob job = new DeformJob
                {
                    vertices = vertices,
                    deformationCounts = counts,
                    effectiveRadius = effectiveRadius,
                    deformationIntensity = deformationIntensity,
                    deformationSmoothness = deformationSmoothness,
                    maxDeformationsPerVertex = maxDeformationsPerVertex,
                    contactPoint = contactPoint,
                    localNormal = localNormal,
                    localToWorld = localToWorld
                };

                JobHandle handle = job.Schedule(vertices.Length, 64);
                handle.Complete();

                // Copy modified vertices back.
                vertices.CopyTo(currentVertices);
                // Update the deformation counts dictionary.
                deformationCounts.Clear();
                for (int k = 0; k < counts.Length; k++)
                {
                    if (counts[k] != 0)
                        deformationCounts[k] = counts[k];
                }

                // Dispose NativeArrays.
                vertices.Dispose();
                counts.Dispose();

                // Update mesh properties after deformation.
                mesh.vertices = currentVertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                // Update the collider if needed.
                if (updateColliders && i < ObjectMeshColliders.Count && ObjectMeshColliders[i] != null)
                {
                    UpdateColliderMesh(ObjectMeshColliders[i], mesh);
                }
            }
        }

        private void UpdateColliderMesh(MeshCollider collider, Mesh mesh)
        {
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.magnitude > collisionImpulseThreshold)
            {
                Profiler.BeginSample("ApplyGlobalDeformation");
                ApplyGlobalDeformation(collision.contacts[0].point, collision.contacts[0].normal);
                Profiler.EndSample();

                // Invoke UnityEvents to signal that a collision and subsequent deformation occurred.
                onDeformationApplied?.Invoke();
            }
        }

        private void OnDrawGizmos()
        {
            if (showDeformationGizmos)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(lastContactPoint, 0.1f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(lastContactPoint, deformationRadius);
            }
        }

        [ContextMenu("Reset Deformation")]
        public void ResetDeformation()
        {
            for (int i = 0; i < ObjectMeshFilters.Count; i++)
            {
                MeshFilter mf = ObjectMeshFilters[i];
                if (mf == null || i >= originalVerticesList.Count)
                    continue;

                Mesh mesh = deformingMeshes[i];
                Vector3[] originalVertices = originalVerticesList[i];
                Vector3[] currentVertices = currentVerticesList[i];

                System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);
                mesh.vertices = currentVertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                deformationCountsList[i].Clear();

                if (updateColliders && i < ObjectMeshColliders.Count && ObjectMeshColliders[i] != null)
                {
                    UpdateColliderMesh(ObjectMeshColliders[i], mesh);
                }
            }
            showDeformationGizmos = false;
            Debug.Log("Mesh deformations reset to original shapes.");

            // Invoke event to signal that the deformations have been reset.
            onDeformationReset?.Invoke();
        }

        /// <summary>
        /// Detects and assigns all MeshFilters and MeshColliders in child objects.
        /// This method is callable from the context menu in the Unity Editor.
        /// </summary>
        [ContextMenu("Detect Child Meshes and Colliders")]
        private void DetectChildMeshesAndColliders()
        {
            // Ensure the lists exist.
            if (ObjectMeshFilters == null)
                ObjectMeshFilters = new List<MeshFilter>();
            if (ObjectMeshColliders == null)
                ObjectMeshColliders = new List<MeshCollider>();

            ObjectMeshFilters.Clear();
            ObjectMeshColliders.Clear();

            // Retrieve all MeshFilters and MeshColliders from children (including the current object if applicable).
            MeshCollider[] foundMeshColliders = GetComponentsInChildren<MeshCollider>();

            for (int i = 0; i < foundMeshColliders.Length; i++)
            {
                if (foundMeshColliders[i] != null)
                {
                    ObjectMeshColliders.Add(foundMeshColliders[i]);
                    ObjectMeshFilters.Add(foundMeshColliders[i].GetComponent<MeshFilter>());
                }
            }

#if UNITY_EDITOR
            // set dirty
            UnityEditor.EditorUtility.SetDirty(this);
#endif

            Debug.Log($"Detected {ObjectMeshFilters.Count} MeshFilters and {ObjectMeshColliders.Count} MeshColliders in children.");

            // Reinitialize mesh data now that the filters may have changed.
            //InitializeMeshes();
        }

        [BurstCompile]
        private struct DeformJob : IJobParallelFor
        {
            public NativeArray<Vector3> vertices;
            public NativeArray<int> deformationCounts;
            [ReadOnly] public float effectiveRadius;
            [ReadOnly] public float deformationIntensity;
            [ReadOnly] public float deformationSmoothness;
            [ReadOnly] public int maxDeformationsPerVertex;
            [ReadOnly] public Vector3 contactPoint;
            [ReadOnly] public Vector3 localNormal;
            [ReadOnly] public Matrix4x4 localToWorld;

            public void Execute(int index)
            {
                Vector3 vertex = vertices[index];
                Vector3 worldVertex = localToWorld.MultiplyPoint3x4(vertex);
                float distance = math.distance(worldVertex, contactPoint);

                if (distance <= effectiveRadius)
                {
                    float deformationFactor = math.clamp(1f - distance / effectiveRadius, 0f, 1f);
                    if (deformationCounts[index] < maxDeformationsPerVertex)
                    {
                        vertex += localNormal * deformationFactor * deformationIntensity * deformationSmoothness;
                        vertices[index] = vertex;
                        deformationCounts[index] = deformationCounts[index] + 1;
                    }
                }
            }
        }
    }
}
