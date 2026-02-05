using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [SerializeField] private Transform player;

    private void Awake()
    {
        if (player != null)
            Debug.Log($"[RaceManager] Player asignado: {player.name}");
        else
            Debug.LogError("[RaceManager] ❌ Player NO asignado");
    }
}
