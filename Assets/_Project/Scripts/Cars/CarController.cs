using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Acceleration Settings")]
    [SerializeField] private float acceleration = 800f;
    [SerializeField] private float reverseAcceleration = 400f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float maxReverseSpeed = 10f;

    [Header("Steering Settings")]
    [SerializeField] private float turnSpeed = 80f;
    [SerializeField] private float steerResponsiveness = 0.8f;

    [Header("Arcade Handling")]
    [SerializeField] private float grip = 8f;
    [SerializeField] private float driftFactor = 0.95f;
    [SerializeField] private float stability = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private float downforce = 50f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayDistance = 1.2f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);

        Debug.Log("[CarController] Start() – Rigidbody encontrado en " + gameObject.name);
    }

    void FixedUpdate()
    {
        HandleMovement();
        ApplyStabilization();

        // Velocidad actual para debug
        Debug.Log("[CarController] Velocidad actual: " + rb.linearVelocity.magnitude.ToString("F2"));
    }

    private void HandleMovement()
    {
        float forward = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");

        Debug.Log($"[CarController] Input – Vertical: {forward:F2}, Horizontal: {turn:F2}");

        // Si no hay input, no seguimos para no spamear fuerzas
        if (Mathf.Abs(forward) < 0.01f && Mathf.Abs(turn) < 0.01f)
        {
            return;
        }

        // ACELERACIÓN
        if (forward > 0f)
        {
            if (rb.linearVelocity.magnitude < maxSpeed)
            {
                rb.AddForce(transform.forward * forward * acceleration * Time.fixedDeltaTime, ForceMode.Acceleration);
                Debug.Log("[CarController] Acelerando hacia adelante.");
            }
        }
        else if (forward < 0f)
        {
            if (rb.linearVelocity.magnitude < maxReverseSpeed)
            {
                rb.AddForce(transform.forward * forward * reverseAcceleration * Time.fixedDeltaTime, ForceMode.Acceleration);
                Debug.Log("[CarController] Acelerando en reversa.");
            }
        }

        // GIRO (solo si hay algo de velocidad)
        if (rb.linearVelocity.magnitude > 0.2f && Mathf.Abs(turn) > 0.01f)
        {
            float steerAmount = turn * turnSpeed * steerResponsiveness * Time.fixedDeltaTime;
            transform.Rotate(0f, steerAmount, 0f);
            Debug.Log("[CarController] Girando: " + steerAmount.ToString("F2"));
        }

        // DRIFT / GRIP
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= driftFactor; // reduce deslizamiento lateral
        localVel.z = Mathf.Clamp(localVel.z, -maxSpeed, maxSpeed);
        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    private void ApplyStabilization()
    {
        // GENERAMOS DOWNFORCE (agarre extra a alta velocidad)
        rb.AddForce(-transform.up * rb.linearVelocity.magnitude * stability, ForceMode.Acceleration);

        // RAYCAST PARA ADHERENCIA AL SUELO
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayer))
        {
            rb.AddForce(-transform.up * downforce * Time.fixedDeltaTime, ForceMode.Acceleration);
            Debug.Log("[CarController] Downforce aplicado. Hit en: " + hit.collider.name);
        }
        else
        {
            // Esto nos dice si nunca está detectando el suelo
            Debug.Log("[CarController] Raycast al suelo NO está golpeando nada.");
        }
    }
}
