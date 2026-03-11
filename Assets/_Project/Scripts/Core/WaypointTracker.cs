using UnityEngine;

public class WaypointTracker : MonoBehaviour
{
    public int waypointActual = -1;
    public int vueltaActual = 0;

    [Header("Configuración pista")]
    [SerializeField] private int totalWaypoints = 9;
    [SerializeField] private int vueltasTotales = 3;

    private void Start()
    {
        // Registrar este corredor en el RaceManager
        RaceManager.Instance.RegistrarCorredor(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        Waypoint wp = other.GetComponent<Waypoint>();
        if (wp == null) return;

        int siguienteWaypoint = waypointActual + 1;

        // Inicio de carrera (primer waypoint)
        if (waypointActual == -1 && wp.indice == 0)
        {
            waypointActual = 0;

            if (CompareTag("Player"))
            {
                HUDManager.Instance.ActualizarVuelta(1, vueltasTotales);
            }

            Debug.Log("Inicio de carrera");
            return;
        }

        // Si estaba en el último waypoint y vuelve al 0 → nueva vuelta
        if (waypointActual == totalWaypoints - 1 && wp.indice == 0)
        {
            vueltaActual++;
            waypointActual = 0;

            if (CompareTag("Player"))
            {
                HUDManager.Instance.ActualizarVuelta(vueltaActual, vueltasTotales);
            }

            Debug.Log("VUELTA COMPLETADA: " + vueltaActual);
            return;
        }

        // Waypoint siguiente correcto
        if (wp.indice == siguienteWaypoint)
        {
            waypointActual = wp.indice;

            Debug.Log("Waypoint válido: " + waypointActual);
        }
    }
}