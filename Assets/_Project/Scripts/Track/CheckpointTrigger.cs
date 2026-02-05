using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;

    private bool alreadyTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // 🔒 Evita spam
        if (alreadyTriggered) return;

        // 🎯 Filtrado fuerte
        if (!other.CompareTag("RacerTrigger")) return;

        // Confirmamos root
        Transform root = other.transform.root;

        Debug.Log(
            $"[CheckpointTrigger] ✅ CHECKPOINT {checkpointIndex} | " +
            $"Entró: {other.name} | Root: {root.name}"
        );

        alreadyTriggered = true;
    }

    // (opcional) para debug
    public void ResetCheckpoint()
    {
        alreadyTriggered = false;
    }
}
