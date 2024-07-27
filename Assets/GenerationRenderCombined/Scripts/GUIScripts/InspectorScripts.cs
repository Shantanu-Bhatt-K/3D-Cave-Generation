using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(GUIValues))]
public class InspectorScripts : Editor
{
    public override void OnInspectorGUI()
    {
        GUIValues gen = (GUIValues)target;
        DrawDefaultInspector();
        if (GUILayout.Button("Generate"))
        {
            gen.GenerateMap();
        }
        if (GUILayout.Button("Clear"))
        {
            gen.ClearWindow();
        }
    }

}