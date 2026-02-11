using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TrackGenerator : MonoBehaviour
{
    [Header("Puntos de la pista (en orden)")]
    [Tooltip("Transform padre que contiene los puntos P0, P1, P2...")]
    public Transform pointsParent;

    [Header("Parámetros de la pista")]
    public float trackWidth = 12f;
    public bool closedLoop = false;

    // Internos
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    void Awake()
    {
        AutoAssignComponents();
    }

    void OnEnable()
    {
        AutoAssignComponents();
    }

    void AutoAssignComponents()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshCollider == null)
            meshCollider = GetComponent<MeshCollider>();
    }

    public void GenerarPista()
    {
        AutoAssignComponents();

        if (pointsParent == null)
        {
            Debug.LogError("TrackGenerator: Points Parent no asignado.");
            return;
        }

        List<Transform> points = new List<Transform>();

        foreach (Transform child in pointsParent)
            points.Add(child);

        if (points.Count < 2)
        {
            Debug.LogError("TrackGenerator: Se necesitan al menos 2 puntos.");
            return;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Procedural Track";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int segmentCount = closedLoop ? points.Count : points.Count - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            Transform p0 = points[i];
            Transform p1 = points[(i + 1) % points.Count];

            Vector3 dir = (p1.position - p0.position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 v0 = transform.InverseTransformPoint(p0.position + right * trackWidth * 0.5f);
            Vector3 v1 = transform.InverseTransformPoint(p0.position - right * trackWidth * 0.5f);
            Vector3 v2 = transform.InverseTransformPoint(p1.position + right * trackWidth * 0.5f);
            Vector3 v3 = transform.InverseTransformPoint(p1.position - right * trackWidth * 0.5f);

            int baseIndex = vertices.Count;

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            triangles.Add(baseIndex + 0);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);

            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 1);

            uvs.Add(new Vector2(0, i));
            uvs.Add(new Vector2(1, i));
            uvs.Add(new Vector2(0, i + 1));
            uvs.Add(new Vector2(1, i + 1));
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        Debug.Log("Pista generada correctamente.");
    }
}
