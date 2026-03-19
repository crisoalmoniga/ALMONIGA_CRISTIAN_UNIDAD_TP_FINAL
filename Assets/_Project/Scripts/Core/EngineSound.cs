using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class EngineSound : MonoBehaviour
{
    [Header("Referencia")]
    public Rigidbody rb;

    [Header("FMOD")]
    public EventReference engineEvent;

    private EventInstance engineInstance;

    [Header("RPM")]
    public float minRPM = 1000f;
    public float maxRPM = 7000f;
    public float maxSpeed = 50f;

    void Start()
    {
        if (rb == null)
        {
            Debug.LogError("❌ Rigidbody NO asignado en EngineSound");
            return;
        }

        if (engineEvent.IsNull)
        {
            Debug.LogError("❌ EventReference NO asignado");
            return;
        }

        engineInstance = RuntimeManager.CreateInstance(engineEvent);
        engineInstance.start();

        Debug.Log("✅ EngineSound iniciado correctamente");
    }

    void Update()
    {
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;

        float normalized = Mathf.Clamp01(speed / maxSpeed);
        float rpm = Mathf.Lerp(minRPM, maxRPM, normalized);

        engineInstance.setParameterByName("RPM", rpm);

        // 🔍 DEBUG
        Debug.Log(
            $"🚗 Speed: {speed:F2} | Normalized: {normalized:F2} | RPM: {rpm:F0}"
        );
    }

    void OnDestroy()
    {
        engineInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        engineInstance.release();

        Debug.Log("🛑 EngineSound destruido");
    }
}