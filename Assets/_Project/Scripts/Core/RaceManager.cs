using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    public enum EstadoCarrera
    {
        EsperandoInicio,
        CuentaRegresiva,
        EnCarrera,
        Finalizada
    }

    public EstadoCarrera estadoActual = EstadoCarrera.EsperandoInicio;

    public List<WaypointTracker> corredores = new List<WaypointTracker>();

    [Header("Configuración Carrera")]
    [SerializeField] private int vueltasParaGanar = 3;
    [SerializeField] private float tiempoCuentaRegresiva = 3f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        StartCoroutine(CuentaRegresiva());
    }

    private void Update()
    {
        if (estadoActual != EstadoCarrera.EnCarrera)
            return;

        CalcularPosiciones();
        VerificarVictoria();
    }

    public void RegistrarCorredor(WaypointTracker corredor)
    {
        if (!corredores.Contains(corredor))
        {
            corredores.Add(corredor);
        }
    }

    private IEnumerator CuentaRegresiva()
    {
        estadoActual = EstadoCarrera.CuentaRegresiva;

        float tiempoRestante = tiempoCuentaRegresiva;

        while (tiempoRestante > 0)
        {
            Debug.Log(Mathf.Ceil(tiempoRestante));
            yield return new WaitForSeconds(1f);
            tiempoRestante--;
        }

        Debug.Log("GO!");

        estadoActual = EstadoCarrera.EnCarrera;
    }

    private void CalcularPosiciones()
    {
        corredores = corredores
            .OrderByDescending(c => c.vueltaActual)
            .ThenByDescending(c => c.waypointActual)
            .ToList();
    }

    private void VerificarVictoria()
    {
        foreach (var corredor in corredores)
        {
            if (corredor.vueltaActual >= vueltasParaGanar)
            {
                estadoActual = EstadoCarrera.Finalizada;

                // Guardamos el ganador
                RaceData.nombreGanador = corredor.name;

                // Cargamos escena Results directamente
                SceneManager.LoadScene("20_Results");

                break;
            }
        }
    }
}