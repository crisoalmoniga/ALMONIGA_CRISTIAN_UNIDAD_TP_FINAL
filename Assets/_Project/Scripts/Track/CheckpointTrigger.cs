using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public enum TriggerType { Checkpoint, FinishLine }

    [Header("Type")]
    [SerializeField] private TriggerType triggerType = TriggerType.Checkpoint;

    [Header("Checkpoint Settings")]
    [SerializeField] private int checkpointIndex = 0; // 0..N-1 (solo si es Checkpoint)

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private RaceManager raceManager;
    private bool consumed = false; // evita dobles triggers si el auto queda rozando

    private void Start()
    {
        raceManager = FindFirstObjectByType<RaceManager>();

        if (raceManager == null)
            Debug.LogError("[CheckpointTrigger] No se encontró RaceManager en la escena.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed) return;
        if (raceManager == null) return;

        // Ajustá esto si tu Player tiene otro tag
        if (!other.CompareTag("Player"))
            return;

        consumed = true;

        if (triggerType == TriggerType.Checkpoint)
        {
            if (debugLogs)
                Debug.Log($"[CheckpointTrigger] Player entró al checkpoint {checkpointIndex}");

            raceManager.OnPlayerHitCheckpoint(checkpointIndex);
        }
        else // FinishLine
        {
            if (debugLogs)
                Debug.Log("[CheckpointTrigger] Player entró a la meta");

            raceManager.OnPlayerHitFinishLine();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // cuando sale, lo habilitamos para la próxima vuelta
        consumed = false;
    }
}
