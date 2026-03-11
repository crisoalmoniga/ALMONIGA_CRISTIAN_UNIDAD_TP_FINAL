using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI posicionText;
    [SerializeField] private TextMeshProUGUI vueltaText;
    [SerializeField] private TextMeshProUGUI velocidadText;
    [SerializeField] private TextMeshProUGUI tiempoText;

    private float tiempoCarrera;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (RaceManager.Instance.estadoActual != RaceManager.EstadoCarrera.EnCarrera)
            return;

        tiempoCarrera += Time.deltaTime;

        int minutos = Mathf.FloorToInt(tiempoCarrera / 60);
        int segundos = Mathf.FloorToInt(tiempoCarrera % 60);

        tiempoText.text = $"TIME {minutos:00}:{segundos:00}";
    }

    public void ActualizarPosicion(int posicion, int total)
    {
        posicionText.text = $"POS {posicion}/{total}";
    }

    public void ActualizarVuelta(int vuelta, int total)
    {
        vueltaText.text = $"LAP {vuelta}/{total}";
    }

    public void ActualizarVelocidad(float velocidad)
    {
        velocidadText.text = $"{Mathf.RoundToInt(velocidad)} km/h";
    }
}