using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    public List<WaypointTracker> corredores = new List<WaypointTracker>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("Corredores registrados: " + corredores.Count);
    }

    public void RegistrarCorredor(WaypointTracker corredor)
    {
        if (!corredores.Contains(corredor))
        {
            corredores.Add(corredor);
        }
    }
}