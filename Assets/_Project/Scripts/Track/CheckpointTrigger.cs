using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private int index = 0;
    [SerializeField] private bool debugLogs = false;

    public int Index => index;

    private RaceManager raceManager;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        raceManager = FindFirstObjectByType<RaceManager>();
        if (raceManager == null)
            Debug.LogError($"[CheckpointTrigger] No encontré RaceManager en escena. ({name})");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (raceManager == null) return;

        // Si querés tag en vez de referencia exacta, lo cambiamos.
        if (!raceManager.IsPlayer(other)) return;

        if (debugLogs)
            Debug.Log($"[CheckpointTrigger] Hit index {index} ({name})");

        raceManager.NotifyCheckpointHit(index);
    }
}
