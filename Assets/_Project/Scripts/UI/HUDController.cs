using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private TextMeshProUGUI lapText;

    void Start()
    {
        if (raceManager == null)
            raceManager = FindFirstObjectByType<RaceManager>();

        UpdateHUD();
    }

    void Update()
    {
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (raceManager == null || lapText == null) return;

        if (raceManager.RaceFinished)
            lapText.text = "Carrera terminada";
        else
            lapText.text = $"Vuelta {raceManager.CurrentLap} / {raceManager.TotalLaps}";
    }
}
