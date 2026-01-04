using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Puntos desde donde se tiran los rayos al suelo (ideal: 4 esquinas del auto).")]
    [SerializeField] private Transform[] hoverPoints;

    [Tooltip("Qué capas cuentan como suelo.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Hover (Suspensión)")]
    [Tooltip("Altura objetivo sobre el suelo (metros).")]
    [SerializeField] private float hoverHeight = 1.2f;

    [Tooltip("Fuerza de la suspensión (más alto = más duro).")]
    [SerializeField] private float hoverForce = 8000f;

    [Tooltip("Amortiguación vertical (más alto = menos rebote).")]
    [SerializeField] private float hoverDamp = 600f;

    [Tooltip("Distancia máxima del rayo (debe ser > hoverHeight).")]
    [SerializeField] private float rayLength = 3.0f;

    [Header("Movimiento")]
    [Tooltip("Aceleración hacia adelante/atrás.")]
    [SerializeField] private float acceleration = 30f;

    [Tooltip("Velocidad máxima (m/s).")]
    [SerializeField] private float maxSpeed = 25f;

    [Header("Giro")]
    [Tooltip("Torque base de giro (se escala con la velocidad).")]
    [SerializeField] private float turnTorque = 180f;

    [Tooltip("Velocidad (m/s) a la que alcanza el giro máximo.")]
    [SerializeField] private float steerFullAtSpeed = 12f;

    [Tooltip("Amortiguación del giro en Y (anti trompo). Más alto = menos trompo.")]
    [SerializeField] private float yawDamping = 6f;

    [Tooltip("Límite de velocidad angular (rad/s). Baja si sigue trompeando.")]
    [SerializeField] private float maxYawRate = 3.5f;

    [Header("Agarre lateral")]
    [Tooltip("Agarre lateral (1 = sin drift).")]
    [Range(0f, 1f)]
    [SerializeField] private float lateralGrip = 0.92f;

    [Header("Freno de mano")]
    [Tooltip("Agarre lateral con Space.")]
    [Range(0f, 1f)]
    [SerializeField] private float handbrakeGrip = 0.45f;

    [Tooltip("Fuerza de frenado con Space.")]
    [SerializeField] private float handbrakeBrake = 20f;

    [Header("Estabilidad")]
    [SerializeField] private bool autoLevel = true;
    [SerializeField] private float autoLevelStrength = 8f;
    [SerializeField] private float autoLevelDamp = 1.5f;

    private Rigidbody rb;

    private float throttle;
    private float steer;
    private bool handbrake;

    private float grounded01; // 0..1 qué tanto está “apoyado”

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Ayuda mucho a evitar trompos “infinitos”
        rb.angularDamping = 1.5f;   // si ya lo tocaste, dejalo parecido
        rb.linearDamping = 0.05f;
    }

    void Update()
    {
        throttle = Input.GetAxis("Vertical");
        steer = Input.GetAxis("Horizontal");
        handbrake = Input.GetKey(KeyCode.Space);
    }

    void FixedUpdate()
    {
        ApplyHover();        // setea grounded01
        ApplyDrive();
        ApplySteer();        // ahora es anti-trompo
        ApplyLateralGrip();

        if (autoLevel)
            ApplyAutoLevel();
    }

    void ApplyHover()
    {
        if (hoverPoints == null || hoverPoints.Length == 0)
        {
            grounded01 = 0f;
            return;
        }

        int hits = 0;

        foreach (var p in hoverPoints)
        {
            if (!p) continue;

            if (Physics.Raycast(p.position, -transform.up, out RaycastHit hit, rayLength, groundMask, QueryTriggerInteraction.Ignore))
            {
                hits++;

                float error = hoverHeight - hit.distance;
                float vel = Vector3.Dot(rb.GetPointVelocity(p.position), transform.up);
                float force = (error * hoverForce) - (vel * hoverDamp);

                rb.AddForceAtPosition(transform.up * force, p.position, ForceMode.Force);
            }
            else
            {
                // Si no toca suelo, empujoncito hacia abajo para no quedar “flotando” raro
                rb.AddForceAtPosition(-transform.up * (hoverForce * 0.15f), p.position, ForceMode.Force);
            }
        }

        grounded01 = Mathf.Clamp01((float)hits / hoverPoints.Length);
    }

    void ApplyDrive()
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (Mathf.Abs(forwardSpeed) < maxSpeed)
            rb.AddForce(transform.forward * (throttle * acceleration) * grounded01, ForceMode.Acceleration);

        if (handbrake)
            rb.AddForce(-rb.linearVelocity * handbrakeBrake, ForceMode.Acceleration);
    }

    void ApplySteer()
    {
        // 1) Giro escalado por velocidad (evita trompo a baja velocidad)
        float speed = rb.linearVelocity.magnitude;
        float steerStrength01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, steerFullAtSpeed));

        // 2) Solo girar realmente si está apoyado (si está en el aire, casi no)
        float groundedFactor = grounded01;

        float desiredTorque = steer * turnTorque * steerStrength01 * groundedFactor;

        rb.AddTorque(transform.up * desiredTorque, ForceMode.Acceleration);

        // 3) Anti-trompo: amortiguación del yaw (frena la rotación en Y)
        float yawRate = rb.angularVelocity.y;
        rb.AddTorque(transform.up * (-yawRate * yawDamping), ForceMode.Acceleration);

        // 4) Clamp de yawRate (hard stop)
        rb.angularVelocity = new Vector3(rb.angularVelocity.x,
                                         Mathf.Clamp(rb.angularVelocity.y, -maxYawRate, maxYawRate),
                                         rb.angularVelocity.z);
    }

    void ApplyLateralGrip()
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float grip = handbrake ? handbrakeGrip : lateralGrip;

        localVel.x *= grip;
        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    void ApplyAutoLevel()
    {
        Vector3 axis = Vector3.Cross(transform.up, Vector3.up);
        float angle = axis.magnitude;
        if (angle < 0.001f) return;

        axis.Normalize();
        float angVel = Vector3.Dot(rb.angularVelocity, axis);

        Vector3 torque = axis * (angle * autoLevelStrength) - axis * (angVel * autoLevelDamp);
        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (hoverPoints == null) return;

        foreach (var p in hoverPoints)
        {
            if (!p) continue;
            Gizmos.DrawLine(p.position, p.position - transform.up * rayLength);
        }
    }
}
