using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoGanador;
    [SerializeField] private Button botonRevancha;
    [SerializeField] private Button botonMenu;
    [SerializeField] private Button botonSalir;

    private void Start()
    {
        if (!string.IsNullOrEmpty(RaceData.nombreGanador))
            textoGanador.text = "GANADOR:\n" + RaceData.nombreGanador;
        else
            textoGanador.text = "GANADOR DESCONOCIDO";

        botonRevancha.onClick.AddListener(ReiniciarCarrera);
        botonMenu.onClick.AddListener(VolverMenu);
        botonSalir.onClick.AddListener(SalirJuego);
    }

    private void ReiniciarCarrera()
    {
        SceneManager.LoadScene("10_Level_Conurban");
    }

    private void VolverMenu()
    {
        SceneManager.LoadScene("01_Menu");
    }

    private void SalirJuego()
    {
        Debug.Log("Saliendo del juego...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}