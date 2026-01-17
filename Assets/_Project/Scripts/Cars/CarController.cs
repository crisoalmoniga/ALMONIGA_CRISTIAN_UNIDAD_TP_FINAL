using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Puntos desde donde se tiran los rayos al suelo (ideal: 4 esquinas del auto).")]
    [InspectorName("Puntos de Hover")]
    [SerializeField] private Transform[] hoverPoints;

    [Tooltip("Qué capas cuentan como suelo.")]
    [InspectorName("Capas de Suelo")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Hover (Suspensión)")]
    [Tooltip("Altura objetivo sobre el suelo (metros).")]
    [InspectorName("Altura Objetivo")]
    [SerializeField] private float hoverHeight = 1.2f;

    [Tooltip("Fuerza de la suspensión (más alto = más duro).")]
    [InspectorName("Fuerza de Suspensión")]
    [SerializeField] private float hoverForce = 8000f;

    [Tooltip("Amortiguación vertical (más alto = menos rebote).")]
    [InspectorName("Amortiguación Vertical")]
    [SerializeField] private float hoverDamp = 600f;

    [Tooltip("Distancia máxima del rayo (debe ser mayor a la altura).")]
    [InspectorName("Longitud del Rayo")]
    [SerializeField] private float rayLength = 3.0f;

    [Header("Movimiento")]
    [Tooltip("Aceleración hacia adelante y atrás.")]
    [InspectorName("Aceleración")]
    [SerializeField] private float acceleration = 30f;

    [Tooltip("Velocidad máxima del vehículo.")]
    [InspectorName("Velocidad Máxima")]
    [SerializeField] private float maxSpeed = 25f;

    [Header("Giro")]
    [Tooltip("Torque base de giro (se escala con la velocidad).")]
    [InspectorName("Torque de Giro")]
    [SerializeField] private float turnTorque = 180f;

    [Tooltip("Velocidad a la que se alcanza el giro máximo.")]
    [InspectorName("Velocidad para Giro Máx.")]
    [SerializeField] private float steerFullAtSpeed = 12f;

    [Tooltip("Amortiguación del giro en Y (anti trompo).")]
    [InspectorName("Amortiguación de Trompo")]
    [SerializeField] private float yawDamping = 6f;

    [Tooltip("Límite máximo de rotación en Y.")]
    [InspectorName("Límite de Giro")]
    [SerializeField] private float maxYawRate = 3.5f;

    [Header("Agarre lateral")]
    [Tooltip("Nivel de agarre lateral (1 = sin derrape).")]
    [InspectorName("Agarre Lateral")]
    [Range(0f, 1f)]
    [SerializeField] private float lateralGrip = 0.92f;

    [Header("Freno de mano")]
    [Tooltip("Agarre lateral al usar freno de mano.")]
    [InspectorName("Agarre con Freno")]
    [Range(0f, 1f)]
    [SerializeField] private float handbrakeGrip = 0.45f;

    [Tooltip("Fuerza de frenado del freno de mano.")]
    [InspectorName("Fuerza de Frenado")]
    [SerializeField] private float handbrakeBrake = 20f;

    [Header("Estabilidad")]
    [InspectorName("Auto Nivelar")]
    [SerializeField] private bool autoLevel = true;

    [InspectorName("Fuerza de Nivelado")]
    [SerializeField] private float autoLevelStrength = 8f;

    [InspectorName("Amortiguación de Nivelado")]
    [SerializeField] private float autoLevelDamp = 1.5f;

    private Rigidbody rb;

    private float throttle;
    private float steer;
    private bool handbrake;

    private float grounded01;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        rb.angularDamping = 1.5f;
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
        ApplyHover();
        ApplyDrive();
        ApplySteer();
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

            if (Physics.Raycast(p.position, -transform.up, out RaycastHit hit, rayLength, groundMask))
            {
                hits++;

                float error = hoverHeight - hit.distance;
                float vel = Vector3.Dot(rb.GetPointVelocity(p.position), transform.up);
                float force = (error * hoverForce) - (vel * hoverDamp);

                rb.AddForceAtPosition(transform.up * force, p.position);
            }
            else
            {
                rb.AddForceAtPosition(-transform.up * (hoverForce * 0.15f), p.position);
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
        float speed = rb.linearVelocity.magnitude;
        float steerStrength01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, steerFullAtSpeed));
        float desiredTorque = steer * turnTorque * steerStrength01 * grounded01;

        rb.AddTorque(transform.up * desiredTorque, ForceMode.Acceleration);

        float yawRate = rb.angularVelocity.y;
        rb.AddTorque(transform.up * (-yawRate * yawDamping), ForceMode.Acceleration);

        rb.angularVelocity = new Vector3(
            rb.angularVelocity.x,
            Mathf.Clamp(rb.angularVelocity.y, -maxYawRate, maxYawRate),
            rb.angularVelocity.z
        );
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
}
