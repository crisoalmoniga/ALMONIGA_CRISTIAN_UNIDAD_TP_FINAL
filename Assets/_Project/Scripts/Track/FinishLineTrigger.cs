using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishLineTrigger : MonoBehaviour
{
    [SerializeField] private bool debugLogs = false;

    private RaceManager raceManager;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        raceManager = FindFirstObjectByType<RaceManager>();
        if (raceManager == null)
            Debug.LogError($"[FinishLineTrigger] No encontré RaceManager en escena. ({name})");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (raceManager == null) return;
        if (!raceManager.IsPlayer(other)) return;

        if (debugLogs)
            Debug.Log($"[FinishLineTrigger] Hit ({name})");

        raceManager.NotifyFinishLineHit();
    }
}
