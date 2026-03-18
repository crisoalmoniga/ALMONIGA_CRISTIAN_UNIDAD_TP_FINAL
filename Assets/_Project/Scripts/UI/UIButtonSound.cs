using UnityEngine;
using UnityEngine.EventSystems;
using FMODUnity;
using System.Collections;

public class UIButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("FMOD")]
    public EventReference hoverSound;
    public EventReference clickSound;

    [Header("Escalas")]
    public float normalScale = 2f;
    public float hoverScale = 2.2f;
    public float clickScale = 2.5f;

    [Header("Velocidad")]
    public float speed = 10f;

    private Vector3 targetScale;
    private Coroutine clickRoutine;

    void Start()
    {
        targetScale = Vector3.one * normalScale;
        transform.localScale = targetScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * speed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = Vector3.one * hoverScale;
        RuntimeManager.PlayOneShot(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one * normalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RuntimeManager.PlayOneShot(clickSound);

        if (clickRoutine != null)
            StopCoroutine(clickRoutine);

        clickRoutine = StartCoroutine(ClickAnimation());
    }

    IEnumerator ClickAnimation()
    {
        // Zoom fuerte
        targetScale = Vector3.one * clickScale;
        yield return new WaitForSeconds(0.1f);

        // Volver al hover (si sigue el mouse)
        targetScale = Vector3.one * hoverScale;
    }
}