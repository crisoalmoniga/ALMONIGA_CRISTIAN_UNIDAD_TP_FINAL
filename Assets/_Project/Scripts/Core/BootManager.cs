using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootManager : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(CargarMenu());
    }

    IEnumerator CargarMenu()
    {
        yield return new WaitForSeconds(4f); // duración del boot
        SceneManager.LoadScene("01_Menu");
    }
}