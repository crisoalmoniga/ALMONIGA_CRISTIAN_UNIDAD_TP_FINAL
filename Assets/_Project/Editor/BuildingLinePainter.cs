#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class BuildingLinePainter : EditorWindow
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Ground")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayHeight = 500f;
    [SerializeField] private bool alignToGroundNormal = false;

    [Header("Line Placement")]
    [SerializeField] private float spacing = 6f;                 // distancia entre edificios
    [SerializeField] private float startOffset = 0f;             // corre el primer edificio
    [SerializeField] private float lateralOffset = 0f;           // corre toda la línea a un costado
    [SerializeField] private float lateralJitter = 1.0f;         // random a izq/der por edificio
    [SerializeField] private float forwardJitter = 0.5f;         // random adelante/atrás por edificio
    [SerializeField] private Vector2 randomScale = new Vector2(0.95f, 1.25f);
    [SerializeField] private Vector2 yOffset = new Vector2(0f, 0f);

    [Header("Rotation")]
    [SerializeField] private bool faceLineDirection = true;      // edificios mirando "a lo largo" de la línea
    [SerializeField] private float yawOffset = 90f;              // 90 suele servir si el prefab mira X en vez de Z
    [SerializeField] private float randomYawJitter = 6f;         // pequeña variación

    [Header("Parenting")]
    [SerializeField] private Transform parentContainer;

    [Header("Controls")]
    [SerializeField] private bool livePreview = true;
    [SerializeField] private Color previewColor = new Color(0.2f, 0.8f, 1f, 0.9f);

    private bool isDragging;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Vector3 lastHitPoint;
    private bool hasValidHit;

    [MenuItem("Tools/Building Line Painter")]
    public static void Open()
    {
        var w = GetWindow<BuildingLinePainter>("Building Line");
        w.minSize = new Vector2(360, 420);
        w.Show();
    }

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Building Line Painter", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        DrawPrefabList();

        EditorGUILayout.Space(10);
        groundMask = LayerMaskField("Ground Mask", groundMask);
        rayHeight = EditorGUILayout.FloatField("Ray Height", rayHeight);
        alignToGroundNormal = EditorGUILayout.Toggle("Align To Ground Normal", alignToGroundNormal);

        EditorGUILayout.Space(10);
        spacing = EditorGUILayout.Slider("Spacing", spacing, 0.5f, 50f);
        startOffset = EditorGUILayout.FloatField("Start Offset", startOffset);
        lateralOffset = EditorGUILayout.FloatField("Lateral Offset", lateralOffset);
        lateralJitter = EditorGUILayout.FloatField("Lateral Jitter", lateralJitter);
        forwardJitter = EditorGUILayout.FloatField("Forward Jitter", forwardJitter);
        randomScale = EditorGUILayout.Vector2Field("Random Scale (min,max)", randomScale);
        yOffset = EditorGUILayout.Vector2Field("Y Offset (min,max)", yOffset);

        EditorGUILayout.Space(10);
        faceLineDirection = EditorGUILayout.Toggle("Face Line Direction", faceLineDirection);
        yawOffset = EditorGUILayout.FloatField("Yaw Offset", yawOffset);
        randomYawJitter = EditorGUILayout.FloatField("Random Yaw Jitter", randomYawJitter);

        EditorGUILayout.Space(10);
        parentContainer = (Transform)EditorGUILayout.ObjectField("Parent Container", parentContainer, typeof(Transform), true);

        EditorGUILayout.Space(10);
        livePreview = EditorGUILayout.Toggle("Live Preview", livePreview);

        EditorGUILayout.HelpBox(
            "Uso (Scene View):\n" +
            "- Click izquierdo (LMB) y arrastrar para definir la línea.\n" +
            "- Soltás el mouse y se instancian edificios a lo largo de la línea.\n" +
            "- Se elige prefab aleatorio y se aplica jitter/escala.\n",
            MessageType.Info
        );
    }

    private void OnSceneGUI(SceneView view)
    {
        // Evita seleccionar mientras dibujás
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;

        // Raycast desde mouse a suelo
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        hasValidHit = Physics.Raycast(ray, out RaycastHit hit, 100000f, groundMask, QueryTriggerInteraction.Ignore);

        if (hasValidHit)
            lastHitPoint = hit.point;

        // Preview línea
        if (livePreview && isDragging && hasValidHit)
        {
            endPoint = lastHitPoint;
            DrawLinePreview(startPoint, endPoint);
            DrawPlacementPreview(startPoint, endPoint);
            view.Repaint();
        }
        else if (livePreview && !isDragging && hasValidHit)
        {
            // Un puntito donde está el mouse
            Handles.color = previewColor;
            Handles.DrawWireDisc(lastHitPoint, Vector3.up, 0.6f);
        }

        // Mouse down: set start
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (!hasValidHit) return;
            isDragging = true;
            startPoint = lastHitPoint;
            endPoint = lastHitPoint;
            e.Use();
        }

        // Mouse drag: update end
        if (e.type == EventType.MouseDrag && e.button == 0 && isDragging)
        {
            if (hasValidHit) endPoint = lastHitPoint;
            e.Use();
        }

        // Mouse up: paint
        if (e.type == EventType.MouseUp && e.button == 0 && isDragging)
        {
            isDragging = false;

            if (Vector3.Distance(startPoint, endPoint) > 0.5f)
                PaintLine(startPoint, endPoint);

            e.Use();
        }
    }

    private void PaintLine(Vector3 a, Vector3 b)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        Vector3 dir = (b - a);
        dir.y = 0f;
        float length = dir.magnitude;
        if (length < 0.01f) return;

        dir /= length;

        // Lateral (perpendicular) para offset/jitter
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

        // Cantidad de instancias por longitud
        float safeSpacing = Mathf.Max(0.25f, spacing);
        int count = Mathf.FloorToInt((length - startOffset) / safeSpacing) + 1;
        if (count <= 0) return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        for (int i = 0; i < count; i++)
        {
            float t = startOffset + i * safeSpacing;
            if (t < 0f) continue;
            if (t > length) break;

            Vector3 p = a + dir * t;

            // Jitters
            float side = lateralOffset + Random.Range(-lateralJitter, lateralJitter);
            float fwd = Random.Range(-forwardJitter, forwardJitter);
            p += right * side + dir * fwd;

            // Raycast vertical para apoyar en el suelo
            Vector3 origin = p + Vector3.up * rayHeight;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                continue;

            GameObject prefab = PickRandomPrefab();
            if (!prefab) continue;

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (!go) continue;

            Undo.RegisterCreatedObjectUndo(go, "Paint Buildings Line");

            if (parentContainer != null)
                go.transform.SetParent(parentContainer);

            // Pos + Y offset
            float yOff = Random.Range(yOffset.x, yOffset.y);
            go.transform.position = hit.point + Vector3.up * yOff;

            // Rotación
            Quaternion rot;
            if (alignToGroundNormal)
            {
                rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                if (faceLineDirection)
                    rot *= Quaternion.LookRotation(dir, Vector3.up);
                rot *= Quaternion.Euler(0f, yawOffset + Random.Range(-randomYawJitter, randomYawJitter), 0f);
            }
            else
            {
                rot = faceLineDirection ? Quaternion.LookRotation(dir, Vector3.up) : Quaternion.identity;
                rot *= Quaternion.Euler(0f, yawOffset + Random.Range(-randomYawJitter, randomYawJitter), 0f);
            }

            go.transform.rotation = rot;

            // Escala random uniforme
            float sMin = Mathf.Min(randomScale.x, randomScale.y);
            float sMax = Mathf.Max(randomScale.x, randomScale.y);
            float s = Random.Range(sMin, sMax);
            go.transform.localScale = go.transform.localScale * s;
        }

        Undo.CollapseUndoOperations(group);
    }

    private GameObject PickRandomPrefab()
    {
        int tries = 10;
        while (tries-- > 0)
        {
            var p = prefabs[Random.Range(0, prefabs.Length)];
            if (p) return p;
        }
        return null;
    }

    private void DrawLinePreview(Vector3 a, Vector3 b)
    {
        Handles.color = previewColor;
        Handles.DrawAAPolyLine(5f, a + Vector3.up * 0.05f, b + Vector3.up * 0.05f);
        Handles.DrawWireDisc(a, Vector3.up, 0.5f);
        Handles.DrawWireDisc(b, Vector3.up, 0.5f);
    }

    private void DrawPlacementPreview(Vector3 a, Vector3 b)
    {
        Vector3 dir = (b - a);
        dir.y = 0f;
        float length = dir.magnitude;
        if (length < 0.01f) return;
        dir /= length;

        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

        float safeSpacing = Mathf.Max(0.25f, spacing);
        int count = Mathf.FloorToInt((length - startOffset) / safeSpacing) + 1;
        if (count <= 0) return;

        Handles.color = new Color(previewColor.r, previewColor.g, previewColor.b, 0.45f);

        for (int i = 0; i < count; i++)
        {
            float t = startOffset + i * safeSpacing;
            if (t < 0f) continue;
            if (t > length) break;

            Vector3 p = a + dir * t;
            p += right * lateralOffset;
            Handles.DrawWireCube(p + Vector3.up * 0.1f, new Vector3(0.8f, 0.2f, 0.8f));
        }
    }

    private void DrawPrefabList()
    {
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

        int newSize = Mathf.Max(0, EditorGUILayout.IntField("Count", prefabs != null ? prefabs.Length : 0));
        if (prefabs == null || newSize != prefabs.Length)
            System.Array.Resize(ref prefabs, newSize);

        EditorGUI.indentLevel++;
        for (int i = 0; i < prefabs.Length; i++)
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField($"Prefab {i}", prefabs[i], typeof(GameObject), false);
        EditorGUI.indentLevel--;
    }

    private LayerMask LayerMaskField(string label, LayerMask selected)
    {
        var layers = InternalEditorUtility.layers;
        int mask = selected.value;

        int convertedMask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layers[i]);
            if ((mask & (1 << layer)) != 0)
                convertedMask |= (1 << i);
        }

        convertedMask = EditorGUILayout.MaskField(label, convertedMask, layers);

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
