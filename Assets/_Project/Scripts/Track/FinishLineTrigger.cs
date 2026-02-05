using UnityEngine;

public class FinishLineTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            $"[FinishLineTrigger] ENTRÓ ALGO | Nombre: {other.name} | Layer: {LayerMask.LayerToName(other.gameObject.layer)} | Root: {other.transform.root.name}"
        );

        if (other.transform.root.name == "Reno_12")
        {
            Debug.Log("[FinishLineTrigger] 🏁 PLAYER cruzó la META");
        }
    }
}
