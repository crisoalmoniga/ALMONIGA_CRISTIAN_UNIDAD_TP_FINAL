using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [Header("Race Settings")]
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private int totalCheckpoints = 1; // poné la cantidad real de checkpoints (sin contar la meta)
    [SerializeField] private bool requireAllCheckpointsBeforeFinish = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private int currentLap = 1;
    private int nextCheckpointIndex = 0; // el próximo checkpoint que se espera
    private bool raceFinished = false;

    private float raceTime = 0f;

    // Expuestos para HUD
    public int CurrentLap => currentLap;
    public int TotalLaps => totalLaps;
    public bool RaceFinished => raceFinished;
    public float RaceTime => raceTime;

    private void Start()
    {
        StartRace();
    }

    private void Update()
    {
        if (!raceFinished)
            raceTime += Time.deltaTime;
    }

    public void StartRace()
    {
        raceFinished = false;
        raceTime = 0f;

        currentLap = 1;
        nextCheckpointIndex = 0;

        if (debugLogs)
            Debug.Log($"[RaceManager] Carrera iniciada. Vueltas totales: {totalLaps}");
    }

    // --- Estos 2 métodos son los que te están faltando según tu error ---

    // Llamalo desde el trigger de checkpoint, pasando el índice del checkpoint (0..N-1)
    public void OnPlayerHitCheckpoint(int checkpointIndex)
    {
        if (raceFinished) return;

        // Solo acepta el checkpoint esperado (orden)
        if (checkpointIndex != nextCheckpointIndex)
        {
            if (debugLogs)
                Debug.Log($"[RaceManager] Checkpoint fuera de orden. Esperado: {nextCheckpointIndex}, llegó: {checkpointIndex}");
            return;
        }

        nextCheckpointIndex++;

        if (debugLogs)
            Debug.Log($"[RaceManager] Checkpoint correcto. Próximo índice: {nextCheckpointIndex}");

        // Si completó todos los checkpoints, queda habilitada la meta (si la querés estricta)
        if (nextCheckpointIndex >= totalCheckpoints)
        {
            if (debugLogs)
                Debug.Log("[RaceManager] Todos los checkpoints completados, esperando meta...");
        }
    }

    // Llamalo desde el trigger de meta
    public void OnPlayerHitFinishLine()
    {
        if (raceFinished) return;

        if (requireAllCheckpointsBeforeFinish && nextCheckpointIndex < totalCheckpoints)
        {
            if (debugLogs)
                Debug.Log($"[RaceManager] Meta ignorada: faltan checkpoints ({nextCheckpointIndex}/{totalCheckpoints}).");
            return;
        }

        // Vuelta completada
        if (debugLogs)
            Debug.Log($"[RaceManager] Vuelta {currentLap} completada.");

        if (currentLap >= totalLaps)
        {
            raceFinished = true;
            if (debugLogs)
                Debug.Log($"[RaceManager] Carrera finalizada. Tiempo: {raceTime:0.00}s");
            return;
        }

        currentLap++;
        nextCheckpointIndex = 0; // reinicia el circuito de checkpoints para la próxima vuelta
    }
}
