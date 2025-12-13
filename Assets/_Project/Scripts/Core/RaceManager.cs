using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [Header("Checkpoints (en orden)")]
    [SerializeField] private List<Transform> checkpoints = new List<Transform>();

    [Header("Referencia al jugador")]
    [SerializeField] private Transform player;

    [Header("Configuración de carrera")]
    [SerializeField] private int totalLaps = 3;

    private int currentCheckpointIndex = 0;
    private int currentLap = 1;

    void Start()
    {
        if (checkpoints.Count == 0)
        {
            Debug.LogWarning("[RaceManager] No hay checkpoints asignados.");
        }

        Debug.Log($"[RaceManager] Carrera iniciada. Vueltas totales: {totalLaps}");
    }

    public void OnPlayerHitCheckpoint(Transform checkpoint)
    {
        if (checkpoints.Count == 0) return;

        if (checkpoint == checkpoints[currentCheckpointIndex])
        {
            currentCheckpointIndex++;
            Debug.Log($"[RaceManager] Checkpoint correcto. Índice ahora: {currentCheckpointIndex}");

            // Si pasó por todos los checkpoints, espera línea de meta
            if (currentCheckpointIndex >= checkpoints.Count)
            {
                currentCheckpointIndex = 0;
                Debug.Log("[RaceManager] Todos los checkpoints completados, esperando meta...");
            }
        }
        else
        {
            Debug.Log("[RaceManager] Checkpoint fuera de orden, no se avanza.");
        }
    }

    public void OnPlayerHitFinishLine()
    {
        // Solo cuenta vuelta si venía del último checkpoint
        if (currentCheckpointIndex == 0)
        {
            Debug.Log($"[RaceManager] Vuelta {currentLap} completada.");

            currentLap++;

            if (currentLap > totalLaps)
            {
                Debug.Log("[RaceManager] Carrera terminada. ¡Ganaste!");
                // Acá después disparamos pantalla de fin, etc.
            }
        }
    }
}
