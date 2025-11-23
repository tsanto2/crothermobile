using Ashsvp;

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimcadeVehicleController))]
[CanEditMultipleObjects]
public class SimcadeVehicleControllerEditor : Editor
{
    private const string DiscordUrl = "https://discord.gg/yU82FbNHcu";
    private const string TutorilUrl = "https://youtu.be/XCCPcvEU7Qc";
    private const string DocumentationUrl = "/Ash Assets/Sim-Cade Vehicle Physics/Documentation/Documentation.pdf";
    private const string RateUrl = "https://assetstore.unity.com/packages/tools/physics/sim-cade-vehicle-physics-243624#reviews";

    private Texture2D headerBackground;

    private void OnEnable()
    {
        // Create a white texture for the header background
        headerBackground = new Texture2D(1, 1);
        headerBackground.SetPixel(0, 0, new Color(0,0,0,0.5f));
        headerBackground.Apply();
    }

    private void OnDisable()
    {
        // Destroy the texture to free up memory
        DestroyImmediate(headerBackground);
    }

    bool expand = false;
    public override void OnInspectorGUI()
    {
        // Define the colors
        Color primaryColor = new Color(0, 1f, 0); // Green

        var sectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(5, 5, 5, 5),
            border = new RectOffset(2, 2, 2, 2),
            normal = { background = MakeTexture(1, 1, new Color(1f, 0f, 0f, 1f)) }, // red Background
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };

        var sectionStyle_2 = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(5, 5, 5, 5)
        };

        var headerStyle_main = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 27,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = primaryColor, background = headerBackground }
            
        };

        var buttonStyle_main = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(35, 10, 5, 5)
        };

        expand = EditorGUILayout.BeginFoldoutHeaderGroup(expand, "Sim-Cade Help");
        if (expand)
        {
            EditorGUILayout.BeginVertical(sectionStyle_2);

            // Create a header for the script with white background
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 27;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = primaryColor;
            headerStyle.normal.background = headerBackground;
            headerStyle.padding = new RectOffset(1, 1, 1, 1);
            GUILayout.Space(10f);
            GUILayout.Label("Sim-Cade Vehicle Physics", headerStyle_main);
            GUILayout.Space(10f);

            // Create the buttons
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.fontSize = 12;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.padding = new RectOffset(5, 5, 5, 5);


            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Join Discord", null, "Join the Discord community"), buttonStyle, GUILayout.Height(20f), GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL(DiscordUrl);
            }
            if (GUILayout.Button(new GUIContent("Tutorial", null, "Watch videos on YouTube"), buttonStyle, GUILayout.Height(20f), GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL(TutorilUrl);
            }
            if (GUILayout.Button(new GUIContent("Documentation", null, "Read the documentation"), buttonStyle, GUILayout.Height(20f), GUILayout.ExpandWidth(true)))
            {
                string doc_path = Application.dataPath + DocumentationUrl;
                Application.OpenURL("file://" + doc_path);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Rate the Asset", null, "Rate this asset on the Unity Asset Store"), buttonStyle, GUILayout.Height(20f), GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL(RateUrl);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10f);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Display all public variables of the SimcadeVehicleController script
        DrawDefaultInspector();
    }


    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }


}
