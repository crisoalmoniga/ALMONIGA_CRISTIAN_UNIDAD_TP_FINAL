using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RaceManager raceManager;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI lapText;
    [SerializeField] private TextMeshProUGUI timeText;

    private void Start()
    {
        if (raceManager == null)
            raceManager = FindFirstObjectByType<RaceManager>();

        UpdateHUD();
    }

    private void Update()
    {
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (raceManager == null) return;

        if (lapText != null)
        {
            lapText.text = $"Vuelta: {raceManager.CurrentLap}/{raceManager.TotalLaps}";
        }

        if (timeText != null)
        {
            float t = raceManager.RaceTime;
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            int ms = Mathf.FloorToInt((t * 100f) % 100f);

            timeText.text = $"{minutes:00}:{seconds:00}.{ms:00}";
        }
    }
}
