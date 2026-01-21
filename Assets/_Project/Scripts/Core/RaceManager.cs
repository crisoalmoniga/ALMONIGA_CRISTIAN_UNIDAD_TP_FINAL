using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [Header("Target / Setup (arrastrá acá)")]
    [SerializeField] private Transform player;              // Tu auto
    [SerializeField] private Transform checkpointsRoot;     // Padre que contiene todos los checkpoints
    [SerializeField] private Transform finishLine;          // Objeto meta (opcional, solo referencia)

    [Header("Race Settings")]
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private bool requireAllCheckpointsBeforeFinish = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private List<CheckpointTrigger> checkpoints = new();
    private int totalCheckpoints = 0;

    private int currentLap = 1;
    private int nextCheckpointIndex = 0;
    private bool raceFinished = false;
    private float raceTime = 0f;

    // Expuestos por si querés HUD
    public int CurrentLap => currentLap;
    public int TotalLaps => totalLaps;
    public float RaceTime => raceTime;
    public bool RaceFinished => raceFinished;

    private void Awake()
    {
        RebuildCheckpointsList();
    }

    private void Start()
    {
        StartRace();
    }

    private void Update()
    {
        if (!raceFinished)
            raceTime += Time.deltaTime;
    }

    [ContextMenu("Rebuild Checkpoints List")]
    public void RebuildCheckpointsList()
    {
        checkpoints.Clear();

        if (checkpointsRoot == null)
        {
            if (debugLogs) Debug.LogWarning("[RaceManager] checkpointsRoot está vacío. Arrastrá el padre de checkpoints.");
            totalCheckpoints = 0;
            return;
        }

        checkpoints.AddRange(checkpointsRoot.GetComponentsInChildren<CheckpointTrigger>(true));

        // Ordenar por index
        checkpoints.Sort((a, b) => a.Index.CompareTo(b.Index));

        totalCheckpoints = checkpoints.Count;

        // Validación: índices 0..N-1 sin huecos
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i].Index != i)
            {
                Debug.LogError($"[RaceManager] Índices de checkpoints mal configurados. Esperaba {i} pero encontré {checkpoints[i].Index} en {checkpoints[i].name}.");
            }
        }

        if (debugLogs)
            Debug.Log($"[RaceManager] Checkpoints detectados: {totalCheckpoints} (root: {checkpointsRoot.name})");
    }

    public void StartRace()
    {
        if (player == null)
        {
            Debug.LogError("[RaceManager] Falta asignar Player.");
            return;
        }

        if (totalLaps <= 0)
        {
            Debug.LogError("[RaceManager] totalLaps debe ser >= 1.");
            return;
        }

        if (totalCheckpoints <= 0 && requireAllCheckpointsBeforeFinish)
        {
            Debug.LogWarning("[RaceManager] No hay checkpoints pero requireAllCheckpointsBeforeFinish está activo. No vas a poder cerrar vueltas.");
        }

        raceFinished = false;
        raceTime = 0f;

        currentLap = 1;
        nextCheckpointIndex = 0;

        if (debugLogs)
            Debug.Log($"[RaceManager] Carrera iniciada. Vueltas: {totalLaps} | Checkpoints: {totalCheckpoints}");
    }

    // Llamado por CheckpointTrigger
    public void NotifyCheckpointHit(int checkpointIndex)
    {
        if (raceFinished) return;

        if (checkpointIndex < 0 || checkpointIndex >= totalCheckpoints)
        {
            if (debugLogs)
                Debug.LogWarning($"[RaceManager] CheckpointIndex inválido: {checkpointIndex}. Rango: 0..{totalCheckpoints - 1}");
            return;
        }

        if (checkpointIndex != nextCheckpointIndex)
        {
            if (debugLogs)
                Debug.Log($"[RaceManager] Checkpoint fuera de orden. Esperado: {nextCheckpointIndex}, llegó: {checkpointIndex}");
            return;
        }

        nextCheckpointIndex++;

        if (debugLogs)
            Debug.Log($"[RaceManager] Checkpoint OK ({checkpointIndex}). Próximo: {nextCheckpointIndex}");
    }

    // Llamado por FinishLineTrigger
    public void NotifyFinishLineHit()
    {
        if (raceFinished) return;

        if (requireAllCheckpointsBeforeFinish && nextCheckpointIndex < totalCheckpoints)
        {
            if (debugLogs)
                Debug.Log($"[RaceManager] Meta ignorada: faltan checkpoints ({nextCheckpointIndex}/{totalCheckpoints}).");
            return;
        }

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
        nextCheckpointIndex = 0;
    }

    // Opcional: para que triggers consulten si el collider es el player
    public bool IsPlayer(Collider other)
    {
        return player != null && other.transform == player;
    }
}
