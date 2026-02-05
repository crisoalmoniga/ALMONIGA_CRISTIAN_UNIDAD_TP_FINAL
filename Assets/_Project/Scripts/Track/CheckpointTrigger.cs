using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private int index;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            $"[CheckpointTrigger] CHECKPOINT {index} | Entró: {other.name} | Layer: {LayerMask.LayerToName(other.gameObject.layer)} | Root: {other.transform.root.name}"
        );

        // Debug específico: ¿es el player?
        if (other.transform.root.name == "Reno_12")
        {
            Debug.Log($"[CheckpointTrigger] ✅ PLAYER pasó por checkpoint {index}");
        }
    }
}
