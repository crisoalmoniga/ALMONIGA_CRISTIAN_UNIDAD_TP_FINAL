using UnityEngine;

public class WaypointTracker : MonoBehaviour
{
    public int waypointActual = -1;
    public int vueltaActual = 0;

    [SerializeField] private int totalWaypoints = 9;

    private void Start()
    {
        // 🔥 Se registra automáticamente en el RaceManager
        RaceManager.Instance.RegistrarCorredor(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        Waypoint wp = other.GetComponent<Waypoint>();
        if (wp == null) return;

        int siguienteWaypoint = waypointActual + 1;

        // Inicio de carrera
        if (waypointActual == -1 && wp.indice == 0)
        {
            waypointActual = 0;
            Debug.Log("Inicio de carrera");
            return;
        }

        // Si estamos en el último y volvemos al 0 → vuelta
        if (waypointActual == totalWaypoints - 1 && wp.indice == 0)
        {
            vueltaActual++;
            waypointActual = 0;
            Debug.Log("VUELTA COMPLETADA: " + vueltaActual);
            return;
        }

        // Waypoint siguiente normal
        if (wp.indice == siguienteWaypoint)
        {
            waypointActual = wp.indice;
            Debug.Log("Waypoint válido: " + waypointActual);
        }
    }
}