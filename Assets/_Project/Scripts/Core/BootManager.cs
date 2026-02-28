using UnityEngine;
using UnityEngine.SceneManagement;

public class BootManager : MonoBehaviour
{
    private void Start()
    {
        CargarMenu();
    }

    private void CargarMenu()
    {
        SceneManager.LoadScene("01_Menu");
    }
}