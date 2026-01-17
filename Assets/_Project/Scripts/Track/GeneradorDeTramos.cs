using System.Collections.Generic;
using UnityEngine;

public class GeneradorDeTramos : MonoBehaviour
{
    [Header("Prefab a repetir (debe tener Start y End en algún hijo)")]
    [SerializeField] private GameObject prefabTramo;

    [Min(1)]
    [SerializeField] private int cantidad = 10;

    [Tooltip("0 = pegado. Negativo encime un pelín.")]
    [SerializeField] private float padding = 0f;

    [SerializeField, HideInInspector]
    private List<GameObject> instancias = new();

    public void Generar()
    {
        if (!prefabTramo)
        {
            Debug.LogWarning("GeneradorDeTramos: asigná un prefab.");
            return;
        }

        Limpiar();

        // Cursor: punto donde debe caer el Start del próximo tramo
        Vector3 cursorPos = transform.position;
        Quaternion cursorRot = transform.rotation;

        for (int i = 0; i < cantidad; i++)
        {
            GameObject tramo = Instantiate(prefabTramo, cursorPos, cursorRot, transform);
            instancias.Add(tramo);

            Transform start = FindDeep(tramo.transform, "Start");
            Transform end = FindDeep(tramo.transform, "End");

            if (start == null || end == null)
            {
                Debug.LogError(
                    $"El prefab '{prefabTramo.name}' debe tener objetos hijos llamados 'Start' y 'End' " +
                    $"(pueden estar anidados)."
                );
                break;
            }

            // 1) Alinear Start con cursorPos
            Vector3 delta = cursorPos - start.position;
            tramo.transform.position += delta;

            // 2) Recalcular End (cambió al mover)
            // (start/end son transforms del tramo instanciado, ya quedaron actualizados)

            // 3) Avanzar cursor al End + padding en la dirección Start->End
            Vector3 dir = (end.position - start.position).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = tramo.transform.forward;

            cursorPos = end.position + dir * padding;
            cursorRot = tramo.transform.rotation; // por si más adelante rotás tramos
        }
    }

    public void Limpiar()
    {
        for (int i = instancias.Count - 1; i >= 0; i--)
            if (instancias[i]) DestroyImmediate(instancias[i]);

        instancias.Clear();
    }

    // Busca por nombre en toda la jerarquía (incluye hijos de hijos)
    private Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
