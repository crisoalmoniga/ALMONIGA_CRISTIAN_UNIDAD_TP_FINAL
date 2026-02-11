using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float aceleracion = 35f;
    [SerializeField] private float velocidadMaximaAdelante = 50f;
    [SerializeField] private float velocidadMaximaReversa = 20f;
    [SerializeField] private float fuerzaFreno = 60f;

    [Header("Direccion")]
    [SerializeField] private float velocidadGiro = 120f;
    [SerializeField] private float velocidadMinimaParaGirar = 0.5f;

    [Header("Suspension")]
    [SerializeField] private float distanciaSuspension = 1.2f;
    [SerializeField] private float fuerzaSuspension = 8000f;
    [SerializeField] private float amortiguacionSuspension = 800f;
    [SerializeField] private LayerMask capaSuelo;

    [Header("Presion al suelo")]
    [SerializeField] private float fuerzaDescendente = 40f;

    [Header("Estabilidad")]
    [SerializeField] private float fuerzaAntiVuelco = 6000f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.mass = 1200f;
        rb.linearDamping = 0f;
        rb.angularDamping = 2.5f;
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
    }

    void FixedUpdate()
    {
        ProcesarInput();
        AplicarSuspension();
        AplicarFuerzaDescendente();
        LimitarVelocidad();
        AplicarAntiVuelco();

    }

    void ProcesarInput()
    {
        float aceleracionInput = 0f;

        if (Input.GetKey(KeyCode.W))
            aceleracionInput = 1f;

        if (Input.GetKey(KeyCode.S))
            aceleracionInput = -1f;

        bool frenando = Input.GetKey(KeyCode.Space);

        float direccionInput = 0f;

        if (Input.GetKey(KeyCode.A))
            direccionInput = -1f;

        if (Input.GetKey(KeyCode.D))
            direccionInput = 1f;

        Mover(aceleracionInput, frenando);
        Girar(direccionInput);
    }

    void Mover(float aceleracionInput, bool frenando)
    {
        Vector3 velocidadAdelante =
            Vector3.Project(rb.linearVelocity, transform.forward);

        float magnitudVelocidad = velocidadAdelante.magnitude;
        float direccionMovimiento =
            Mathf.Sign(Vector3.Dot(rb.linearVelocity, transform.forward));

        // Adelante
        if (aceleracionInput > 0f &&
            magnitudVelocidad < velocidadMaximaAdelante)
        {
            rb.AddForce(transform.forward * aceleracion, ForceMode.Acceleration);
        }

        // Reversa
        if (aceleracionInput < 0f &&
            magnitudVelocidad < velocidadMaximaReversa)
        {
            rb.AddForce(-transform.forward * aceleracion * 0.7f,
                        ForceMode.Acceleration);
        }

        // Freno
        if (frenando)
        {
            rb.AddForce(-rb.linearVelocity.normalized * fuerzaFreno,
                        ForceMode.Acceleration);
        }
    }

    void Girar(float direccionInput)
    {
        float velocidad = rb.linearVelocity.magnitude;
        if (velocidad < velocidadMinimaParaGirar) return;

        float direccionMovimiento =
            Mathf.Sign(Vector3.Dot(rb.linearVelocity, transform.forward));

        float direccionFinal = direccionInput * direccionMovimiento;

        float giro = direccionFinal *
                     velocidadGiro *
                     Time.fixedDeltaTime;

        rb.MoveRotation(
            rb.rotation * Quaternion.Euler(0f, giro, 0f)
        );
    }

    void AplicarSuspension()
    {
        RaycastHit impacto;

        if (Physics.Raycast(transform.position,
                            Vector3.down,
                            out impacto,
                            distanciaSuspension,
                            capaSuelo))
        {
            float compresion = distanciaSuspension - impacto.distance;

            float fuerzaResorte =
                compresion * fuerzaSuspension;

            float fuerzaAmortiguador =
                Vector3.Dot(rb.linearVelocity, Vector3.down) *
                amortiguacionSuspension;

            rb.AddForce(Vector3.up *
                        (fuerzaResorte - fuerzaAmortiguador),
                        ForceMode.Force);
        }
    }

    void AplicarFuerzaDescendente()
    {
        float velocidad = rb.linearVelocity.magnitude;

        rb.AddForce(Vector3.down *
                    fuerzaDescendente *
                    velocidad,
                    ForceMode.Force);
    }

    void LimitarVelocidad()
    {
        Vector3 velocidadAdelante =
            Vector3.Project(rb.linearVelocity, transform.forward);

        float magnitudVelocidad = velocidadAdelante.magnitude;

        if (Vector3.Dot(rb.linearVelocity, transform.forward) > 0 &&
            magnitudVelocidad > velocidadMaximaAdelante)
        {
            rb.linearVelocity =
                transform.forward * velocidadMaximaAdelante;
        }

        if (Vector3.Dot(rb.linearVelocity, transform.forward) < 0 &&
            magnitudVelocidad > velocidadMaximaReversa)
        {
            rb.linearVelocity =
                -transform.forward * velocidadMaximaReversa;
        }
    }
    void AplicarAntiVuelco()
    {
        // Cuánto está inclinado lateralmente el auto
        float inclinacion = Vector3.Dot(transform.right, Vector3.up);

        // Aplicamos torque contrario a la inclinación
        rb.AddTorque(
            -transform.forward * inclinacion * fuerzaAntiVuelco,
            ForceMode.Force
        );
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position,
                        transform.position + Vector3.down * distanciaSuspension);
    }
}
