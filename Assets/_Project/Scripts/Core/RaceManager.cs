using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Cuenta Regresiva UI")]
    [SerializeField] private Image imagenCuentaRegresiva;
    [SerializeField] private Sprite[] spritesCuenta;

    [Header("Animación Countdown")]
    [SerializeField] private float escalaInicial = 2f;
    [SerializeField] private float escalaFinal = 0.30f;
    [SerializeField] private float duracionAnimacion = 0.25f;

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

        for (int i = 0; i < spritesCuenta.Length; i++)
        {
            imagenCuentaRegresiva.sprite = spritesCuenta[i];
            imagenCuentaRegresiva.gameObject.SetActive(true);

            yield return StartCoroutine(AnimarNumero());

            yield return new WaitForSeconds(0.7f);
        }

        imagenCuentaRegresiva.gameObject.SetActive(false);

        estadoActual = EstadoCarrera.EnCarrera;
    }

    private IEnumerator AnimarNumero()
    {
        RectTransform rect = imagenCuentaRegresiva.rectTransform;

        float tiempo = 0f;

        rect.localScale = Vector3.one * escalaInicial;

        while (tiempo < duracionAnimacion)
        {
            tiempo += Time.deltaTime;

            float t = tiempo / duracionAnimacion;

            float escala = Mathf.Lerp(escalaInicial, escalaFinal, t);

            rect.localScale = Vector3.one * escala;

            yield return null;
        }

        rect.localScale = Vector3.one * escalaFinal;
    }

    private void CalcularPosiciones()
    {
        corredores = corredores
            .OrderByDescending(c => c.vueltaActual)
            .ThenByDescending(c => c.waypointActual)
            .ToList();

        for (int i = 0; i < corredores.Count; i++)
        {
            if (corredores[i].CompareTag("Player"))
            {
                HUDManager.Instance.ActualizarPosicion(i + 1, corredores.Count);
            }
        }
    }

    private void VerificarVictoria()
    {
        foreach (var corredor in corredores)
        {
            if (corredor.vueltaActual >= vueltasParaGanar)
            {
                estadoActual = EstadoCarrera.Finalizada;

                RaceData.nombreGanador = corredor.name;

                SceneManager.LoadScene("20_Results");

                break;
            }
        }
    }
}