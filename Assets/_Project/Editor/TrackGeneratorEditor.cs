using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrackGenerator))]
public class TrackGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TrackGenerator generator = (TrackGenerator)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generar pista"))
        {
            generator.GenerarPista();
        }
    }
}
