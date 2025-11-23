using UnityEngine;
using System.Collections;
using UnityEditor;

namespace AshVP
{

    [CustomEditor(typeof(waypointPainter))]
    public class waypointPainter_Editor : Editor
    {

        waypointPainter m_target;
        RaycastHit hit;

        public void OnEnable()
        {
            m_target = (waypointPainter)target;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Waypoint Painting", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Hold LeftShift and Left Mouse Click to Place waypoint", MessageType.Info);

            //Ground all of them!
            if (GUILayout.Button("Align Nodes To Ground", GUILayout.Height(30)))
            {
                m_target.AlignToGround();
            }

            //Delete the last placed node
            if (GUILayout.Button("Delete Last Node", GUILayout.Height(30)))
            {
                m_target.DeleteLastNode();
            }

            //Finish
            if (GUILayout.Button("Finish", GUILayout.Height(30)))
            {
                CreateWaypointCircuit();
            }

            GUILayout.EndVertical();
        }

        void OnSceneGUI()
        {

            //Handle UI
            Handles.BeginGUI();

            Rect outRect = new Rect(Screen.width - 250, Screen.height - 100, 200, 50);

            GUILayout.BeginArea(new Rect(outRect));
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Node Layout Mode : " + m_target.layoutMode, EditorStyles.boldLabel);
            string s = (m_target.layoutMode) ? "Disable" : "Enable";
            if (GUILayout.Button(s, GUILayout.Height(30)))
            {
                m_target.layoutMode = !m_target.layoutMode;
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            Handles.EndGUI();

            //Handles.Label(m_target.transform.position, "Path");

            Event e1 = Event.current;
            if (e1.shift)
            {
                m_target.layoutMode = true;
            }
            else
            {
                m_target.layoutMode = false;
            }

            //Layout Mode
            if (m_target.layoutMode)
            {
                if (Event.current.type == EventType.MouseDown)
                {

                    Event e = Event.current;


                    if (e.button == 0)
                    {
                        //Make sure we cant click anythingelse
                        int controlID = GUIUtility.GetControlID(FocusType.Passive);
                        GUIUtility.hotControl = controlID;
                        e.Use();

                        //Create a new node at clicked pos
                        Ray sceneRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                        if (Physics.Raycast(sceneRay, out hit, 1000))
                        {
                            GameObject newNode = new GameObject("Node");
                            newNode.transform.position = hit.point;
                            newNode.transform.parent = m_target.transform;
                        }
                    }
                    else
                    {
                        //Reset hot control
                        GUIUtility.hotControl = 0;
                    }
                }
            }
        }

        public void CreateWaypointCircuit()
        {
            m_target.UnpackPrefab();
            WaypointCircuit circuit = m_target.gameObject.AddComponent<WaypointCircuit>();
            circuit.AddWaypointsFromChildren();
            //circuit.loopedPath = m_target.looped;
            DestroyImmediate(m_target.gameObject.GetComponent<waypointPainter>());
        }
    }
}
