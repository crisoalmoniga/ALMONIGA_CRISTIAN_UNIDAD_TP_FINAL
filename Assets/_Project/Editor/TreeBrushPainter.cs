#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

public class TreeBrushPainter : EditorWindow
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Brush")]
    [SerializeField] private float brushRadius = 6f;
    [SerializeField] private float minSpacing = 1.8f;   // distancia mínima entre árboles pintados
    [SerializeField] private int instancesPerStroke = 6; // cuántos intenta poner por “pasada”

    [Header("Placement")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayHeight = 200f;
    [SerializeField] private float maxSlopeAngle = 35f; // evita poner en pendientes fuertes
    [SerializeField] private bool alignToGroundNormal = false;

    [Header("Randomization")]
    [SerializeField] private bool randomYaw = true;
    [SerializeField] private Vector2 randomScale = new Vector2(0.9f, 1.3f);
    [SerializeField] private Vector2 yOffset = new Vector2(0f, 0f);

    [Header("Parenting")]
    [SerializeField] private Transform parentContainer;

    [Header("Mode")]
    [SerializeField] private bool paint = true;
    [SerializeField] private bool erase = false;
    [SerializeField] private float eraseRadius = 4f;

    private bool isPainting;
    private Vector3 lastPaintPos;
    private readonly List<Vector3> placedPoints = new List<Vector3>(); // cache simple para spacing

    [MenuItem("Tools/Tree Brush Painter")]
    public static void Open()
    {
        var w = GetWindow<TreeBrushPainter>("Tree Brush");
        w.minSize = new Vector2(320, 420);
        w.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += Repaint;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= Repaint;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Tree Brush Painter", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        DrawPrefabList();

        EditorGUILayout.Space(10);
        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, 0.5f, 50f);
        minSpacing = EditorGUILayout.Slider("Min Spacing", minSpacing, 0.2f, 20f);
        instancesPerStroke = EditorGUILayout.IntSlider("Instances Per Stroke", instancesPerStroke, 1, 50);

        EditorGUILayout.Space(10);
        groundMask = LayerMaskField("Ground Mask", groundMask);
        rayHeight = EditorGUILayout.FloatField("Ray Height", rayHeight);
        maxSlopeAngle = EditorGUILayout.Slider("Max Slope Angle", maxSlopeAngle, 0f, 89f);
        alignToGroundNormal = EditorGUILayout.Toggle("Align To Ground Normal", alignToGroundNormal);

        EditorGUILayout.Space(10);
        randomYaw = EditorGUILayout.Toggle("Random Yaw", randomYaw);
        randomScale = EditorGUILayout.Vector2Field("Random Scale (min,max)", randomScale);
        yOffset = EditorGUILayout.Vector2Field("Y Offset (min,max)", yOffset);

        EditorGUILayout.Space(10);
        parentContainer = (Transform)EditorGUILayout.ObjectField("Parent Container", parentContainer, typeof(Transform), true);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);
        paint = EditorGUILayout.Toggle("Paint", paint);
        erase = EditorGUILayout.Toggle("Erase", erase);
        eraseRadius = EditorGUILayout.Slider("Erase Radius", eraseRadius, 0.5f, 50f);

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Uso:\n" +
            "- Abrí esta ventana.\n" +
            "- En la Scene: mantené presionado LMB para pintar.\n" +
            "- Mantené SHIFT + LMB para borrar (si Erase está activado).\n" +
            "- Tip: apagá Erase si no lo usás.\n",
            MessageType.Info
        );

        if (GUILayout.Button("Clear Spacing Cache"))
        {
            placedPoints.Clear();
        }
    }

    private void DrawPrefabList()
    {
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

        int newSize = Mathf.Max(0, EditorGUILayout.IntField("Count", prefabs != null ? prefabs.Length : 0));
        if (prefabs == null || newSize != prefabs.Length)
        {
            System.Array.Resize(ref prefabs, newSize);
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < prefabs.Length; i++)
        {
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField($"Prefab {i}", prefabs[i], typeof(GameObject), false);
        }
        EditorGUI.indentLevel--;
    }

    private void OnSceneGUI(SceneView view)
    {
        if (!paint && !erase) return;

        Event e = Event.current;

        // Evita seleccionar cosas mientras pintás
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 10000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            DrawBrushGizmo(view, ray.origin + ray.direction * 30f, false);
            return;
        }

        Vector3 brushCenter = hit.point;

        bool shift = e.shift;
        bool wantErase = erase && shift;
        bool wantPaint = paint && !wantErase;

        DrawBrushGizmo(view, brushCenter, true, wantErase ? eraseRadius : brushRadius, wantErase);

        // Mouse down
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            isPainting = true;
            lastPaintPos = brushCenter;
            e.Use();
        }

        // Mouse drag
        if (e.type == EventType.MouseDrag && e.button == 0 && isPainting)
        {
            float dist = Vector3.Distance(lastPaintPos, brushCenter);
            if (dist >= Mathf.Max(0.05f, minSpacing * 0.5f))
            {
                if (wantErase) EraseAt(brushCenter);
                if (wantPaint) PaintAt(brushCenter);
                lastPaintPos = brushCenter;
            }
            e.Use();
        }

        // Mouse up
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isPainting = false;
            e.Use();
        }
    }

    private void PaintAt(Vector3 center)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        for (int i = 0; i < instancesPerStroke; i++)
        {
            Vector2 r = Random.insideUnitCircle * brushRadius;
            Vector3 candidate = center + new Vector3(r.x, 0f, r.y);

            // Raycast vertical desde arriba para colocar en superficie
            Vector3 origin = candidate + Vector3.up * rayHeight;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                continue;

            // Pendiente
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) continue;

            // Spacing simple (evita amontonamiento)
            if (!IsFarEnough(hit.point, minSpacing)) continue;

            GameObject prefab = PickRandomPrefab();
            if (!prefab) continue;

            // Instanciación con Undo
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (!go) continue;

            Undo.RegisterCreatedObjectUndo(go, "Paint Trees");

            // Parent
            if (parentContainer != null)
                go.transform.SetParent(parentContainer);

            // Position + offset
            float yOff = Random.Range(yOffset.x, yOffset.y);
            Vector3 pos = hit.point + Vector3.up * yOff;
            go.transform.position = pos;

            // Rotation
            Quaternion rot;
            if (alignToGroundNormal)
            {
                rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                if (randomYaw) rot *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                rot = randomYaw ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;
            }
            go.transform.rotation = rot;

            // Scale
            float sMin = Mathf.Min(randomScale.x, randomScale.y);
            float sMax = Mathf.Max(randomScale.x, randomScale.y);
            float s = Random.Range(sMin, sMax);
            go.transform.localScale = go.transform.localScale * s;

            placedPoints.Add(hit.point);
        }
    }

    private void EraseAt(Vector3 center)
    {
        float r = eraseRadius;

        // Busca colliders en radio (solo editor)
        Collider[] cols = Physics.OverlapSphere(center, r, ~0, QueryTriggerInteraction.Collide);
        foreach (var c in cols)
        {
            if (!c) continue;

            GameObject go = c.gameObject;

            // Borramos solo si es instancia de alguno de nuestros prefabs
            if (IsFromPrefabList(go))
            {
                Undo.DestroyObjectImmediate(go);
            }
        }
    }

    private bool IsFromPrefabList(GameObject go)
    {
        if (prefabs == null) return false;

        // Compara con el "source prefab" si existe
        GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(go);
        if (!src) return false;

        foreach (var p in prefabs)
        {
            if (!p) continue;
            if (src == p) return true;
        }
        return false;
    }

    private GameObject PickRandomPrefab()
    {
        // Elige un prefab no null
        int tries = 10;
        while (tries-- > 0)
        {
            var p = prefabs[Random.Range(0, prefabs.Length)];
            if (p) return p;
        }
        return null;
    }

    private bool IsFarEnough(Vector3 point, float spacing)
    {
        float s2 = spacing * spacing;
        for (int i = placedPoints.Count - 1; i >= 0; i--)
        {
            if ((placedPoints[i] - point).sqrMagnitude < s2)
                return false;
        }
        return true;
    }

    private void DrawBrushGizmo(SceneView view, Vector3 center, bool valid, float radius = -1f, bool isErase = false)
    {
        float r = radius > 0f ? radius : brushRadius;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.color = valid ? (isErase ? new Color(1f, 0.3f, 0.3f, 0.9f) : new Color(0.3f, 1f, 0.3f, 0.9f))
                              : new Color(1f, 0.8f, 0.2f, 0.7f);

        Handles.DrawWireDisc(center, Vector3.up, r);
        Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.08f);
        Handles.DrawSolidDisc(center, Vector3.up, r);
    }

    // Helper para LayerMask en EditorWindow
    private LayerMask LayerMaskField(string label, LayerMask selected)
    {
        var layers = InternalEditorUtility.layers;
        int mask = selected.value;

        // Convierte de LayerMask (bitmask por layer index) a mask por lista de layers
        int convertedMask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layers[i]);
            if ((mask & (1 << layer)) != 0)
                convertedMask |= (1 << i);
        }

        convertedMask = EditorGUILayout.MaskField(label, convertedMask, layers);

        // Convierte de vuelta
        int finalMask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            if ((convertedMask & (1 << i)) != 0)
            {
                int layer = LayerMask.NameToLayer(layers[i]);
                finalMask |= (1 << layer);
            }
        }

        selected.value = finalMask;
        return selected;
    }
}
#endif
