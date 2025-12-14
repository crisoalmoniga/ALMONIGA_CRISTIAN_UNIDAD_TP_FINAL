using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarAIController : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Lista de waypoints en orden. El AI va avanzando del 0 al último y vuelve a 0.")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("Índice inicial del waypoint (por si tenés 2 AIs que arrancan en distintos puntos).")]
    [SerializeField] private int currentIndex = 0;

    [Header("AI Params (per prefab)")]
    [Tooltip("Velocidad máxima del AI.")]
    [SerializeField] private float maxSpeed = 20f;

    [Tooltip("Aceleración del AI (ForceMode.Acceleration).")]
    [SerializeField] private float acceleration = 18f;

    [Tooltip("Velocidad de giro base.")]
    [SerializeField] private float turnSpeed = 90f;

    [Tooltip("Qué tan sensible es al girar hacia el target. Más alto = gira más agresivo.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float steeringSensitivity = 1.0f;

    [Tooltip("Distancia a la que considera que llegó al waypoint y pasa al siguiente.")]
    [SerializeField] private float waypointReachDistance = 3f;

    [Header("Behavior")]
    [Tooltip("Si el ángulo hacia el waypoint es grande, reduce aceleración para no pasarse.")]
    [SerializeField] private float slowDownAngle = 55f;

    [Tooltip("Mínimo de aceleración cuando está girando fuerte (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float minForwardDot = 0.2f;

    [Header("Physics / Stability")]
    [Tooltip("Baja el derrape lateral (0.8-0.98 típico).")]
    [Range(0.5f, 0.999f)]
    [SerializeField] private float driftFactor = 0.93f;

    [Tooltip("Agarre extra hacia abajo en velocidad (para que no se vuelva loco).")]
    [SerializeField] private float stabilityDownforce = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Clamp índice por si el prefab quedó con un número inválido
        if (waypoints != null && waypoints.Length > 0)
            currentIndex = Mathf.Clamp(currentIndex, 0, waypoints.Length - 1);
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform target = waypoints[currentIndex];
        if (target == null)
            return;

        // 1) Si estoy cerca del waypoint, avanzo al siguiente
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= waypointReachDistance)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
            target = waypoints[currentIndex];
            if (target == null) return;
        }

        // 2) Dirección al target (plano XZ)
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        Vector3 dir = toTarget.normalized;

        // 3) Giro: cuánto debo girar para apuntar al target
        float signedAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

        // Giro suave (más cercano = menos brusco; lo hacemos proporcional al ángulo)
        float steer = Mathf.Clamp(signedAngle / 45f, -1f, 1f) * steeringSensitivity;
        float yaw = steer * turnSpeed * Time.fixedDeltaTime;
        transform.Rotate(0f, yaw, 0f);

        // 4) Aceleración: reduce si el ángulo es grande
        float angleAbs = Mathf.Abs(signedAngle);

        float forwardDot = Vector3.Dot(transform.forward, dir); // -1..1
        float accelFactor = Mathf.InverseLerp(minForwardDot, 1f, Mathf.Clamp01(forwardDot));

        if (angleAbs > slowDownAngle)
            accelFactor *= 0.35f; // frena bastante si está doblando cerrado

        // 5) Empuje (NO multiplico por fixedDeltaTime porque ForceMode.Acceleration ya está por step)
        float speed = rb.linearVelocity.magnitude;
        if (speed < maxSpeed)
        {
            rb.AddForce(transform.forward * acceleration * accelFactor, ForceMode.Acceleration);
        }

        // 6) Drift / grip arcade (corta un poco la velocidad lateral)
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= driftFactor;
        rb.linearVelocity = transform.TransformDirection(localVel);

        // 7) Downforce proporcional a velocidad
        rb.AddForce(-transform.up * (rb.linearVelocity.magnitude * stabilityDownforce), ForceMode.Acceleration);

        // 8) Debug visuals
        if (debug)
        {
            Debug.DrawLine(transform.position + Vector3.up * 0.2f, target.position + Vector3.up * 0.2f, Color.yellow);
            Debug.DrawRay(transform.position + Vector3.up * 0.2f, transform.forward * 3f, Color.cyan);
        }
    }

    // Helpers por si querés leer estado desde otros sistemas
    public int CurrentIndex => currentIndex;
    public Transform[] Waypoints => waypoints;
}
