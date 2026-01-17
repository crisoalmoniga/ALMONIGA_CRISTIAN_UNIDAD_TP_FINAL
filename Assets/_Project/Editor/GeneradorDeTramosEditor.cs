#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GeneradorDeTramos))]
public class GeneradorDeTramosEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        var gen = (GeneradorDeTramos)target;

        if (GUILayout.Button("Generar tramos"))
            gen.Generar();

        if (GUILayout.Button("Limpiar tramos"))
            gen.Limpiar();
    }
}
#endif
