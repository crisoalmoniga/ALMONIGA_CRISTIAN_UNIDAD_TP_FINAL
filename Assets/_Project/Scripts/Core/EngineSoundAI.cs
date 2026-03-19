using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class EngineSoundAI : MonoBehaviour
{
    public Rigidbody rb;
    public EventReference engineEvent;

    private EventInstance engineInstance;

    [Header("RPM")]
    public float minRPM = 1000f;
    public float maxRPM = 7000f;
    public float maxSpeed = 40f;

    [Header("Pitch")]
    [Range(0.1f, 2f)]
    public float pitchMultiplier = 0.5f; // 👈 -1 octava

    void Start()
    {
        engineInstance = RuntimeManager.CreateInstance(engineEvent);
        RuntimeManager.AttachInstanceToGameObject(engineInstance, transform, rb);

        // 🔻 Aplicamos pitch global
        engineInstance.setPitch(pitchMultiplier);

        engineInstance.start();
    }

    void Update()
    {
        float speed = rb.linearVelocity.magnitude;

        float normalized = Mathf.Clamp01(speed / maxSpeed);
        float rpm = Mathf.Lerp(minRPM, maxRPM, normalized);

        engineInstance.setParameterByName("RPM", rpm);
    }

    void OnDestroy()
    {
        engineInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        engineInstance.release();
    }
}