using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GenerateNoise))]
public class PerlinGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GenerateNoise gen=(GenerateNoise)target;
        DrawDefaultInspector();
        if (GUILayout.Button("Generate"))
        {
            gen.GenerateMap();
        }if(GUILayout.Button("Clear"))
        {
            gen.ClearWindow();
        }
    }

}
