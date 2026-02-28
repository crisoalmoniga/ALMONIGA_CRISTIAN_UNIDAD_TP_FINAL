using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button botonJugar;
    [SerializeField] private Button botonSalir;

    private void Start()
    {
        botonJugar.onClick.AddListener(IniciarJuego);
        botonSalir.onClick.AddListener(SalirJuego);
    }

    private void IniciarJuego()
    {
        SceneManager.LoadScene("10_Level_Conurban");
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