using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public enum TriggerType { Checkpoint, FinishLine }

    [SerializeField] private TriggerType triggerType = TriggerType.Checkpoint;

    private RaceManager raceManager;

    void Start()
    {
        raceManager = FindFirstObjectByType<RaceManager>();

        if (raceManager == null)
        {
            Debug.LogError("[CheckpointTrigger] No se encontró RaceManager en la escena.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (triggerType == TriggerType.Checkpoint)
        {
            raceManager?.OnPlayerHitCheckpoint(transform);
        }
        else if (triggerType == TriggerType.FinishLine)
        {
            raceManager?.OnPlayerHitFinishLine();
        }
    }
}
