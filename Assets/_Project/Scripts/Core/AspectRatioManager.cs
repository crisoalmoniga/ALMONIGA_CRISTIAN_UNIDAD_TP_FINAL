using UnityEngine;

public class AspectRatioManager : MonoBehaviour
{
    public static AspectRatioManager Instance;

    public float targetAspect = 16f / 9f;

    private Camera cam;

    void Awake()
    {
        // Singleton
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplyAspect();
    }

    void Update()
    {
        // Por si cambia resolución (resize ventana)
        ApplyAspect();
    }

    void ApplyAspect()
    {
        if (Camera.main == null) return;

        if (cam != Camera.main)
        {
            cam = Camera.main;
        }

        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            cam.rect = rect;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            cam.rect = rect;
        }
    }
}