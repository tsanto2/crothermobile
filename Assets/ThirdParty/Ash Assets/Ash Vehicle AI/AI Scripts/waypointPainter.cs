
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AshVP
{
#if UNITY_EDITOR

    [ExecuteInEditMode]
    public class waypointPainter : MonoBehaviour
    {
        public Color nodeColor = Color.green;
        [HideInInspector]
        public Transform[] nodes;
        private Color pathColor = Color.red;
        
        public bool layoutMode;
        //public bool looped = true;

        void OnDrawGizmos()
        {
            Gizmos.color = nodeColor;

            //Draw green cube on main transform to show path parent
            Gizmos.DrawWireCube(transform.position, new Vector3(2, 2, 2));

            //Draw spheres on each node
            if (nodes.Length > 0)
            {
                for (int i = 1; i < nodes.Length; i++)
                {
                    Gizmos.DrawSphere(new Vector3(nodes[i].position.x, nodes[i].position.y + 1.0f, nodes[i].position.z), 1f);
                }
            }
        }


        void Update()
        {
            Transform[] transforms = GetComponentsInChildren<Transform>();

            nodes = new Transform[transforms.Length];

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = transforms[i];
            }

            for (int n = 0; n < nodes.Length; n++)
            {
                Debug.DrawLine(nodes[n].position - Vector3.down, nodes[(n + 1) % nodes.Length].position - Vector3.down, pathColor);
            }

            int c = 0;
            foreach (Transform child in transforms)
            {
                if (child != transform)
                {
                    child.name = (c++).ToString("000");
                }
            }
        }


        public void AlignToGround()
        {
            for (int i = 1; i < nodes.Length; i++)
            {
                Ray ray = new Ray(nodes[i].position, Vector3.down);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 500))
                {
                    nodes[i].position = hit.point + Vector3.up;
                }
            }
        }

        public void DeleteLastNode()
        {
            Transform[] nodes = GetComponentsInChildren<Transform>();

            if (nodes.Length > 2)
                DestroyImmediate(nodes[nodes.Length - 1].gameObject);
        }

        public void UnpackPrefab()
        {
            PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

    }
#endif
}
