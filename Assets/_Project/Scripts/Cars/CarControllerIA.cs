using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarControllerIA : MonoBehaviour
{
    [Header("Ruta")]
    [SerializeField] private Transform[] puntosRuta;
    [SerializeField] private float distanciaCambioPunto = 6f;

    [Header("IA - LookAhead")]
    [SerializeField] private float lookAheadMin = 8f;
    [SerializeField] private float lookAheadMax = 22f;

    [Header("IA - Direccion")]
    [SerializeField] private float anguloMaxParaSteer = 45f; // grados -> steer = angulo/esto

    [Header("IA - Curvas / velocidad")]
    [SerializeField] private float anguloCurvaFuerte = 45f;
    [SerializeField] private float velocidadObjetivoEnCurvaFuerte = 12f;

    [Header("IA - Anti-traba")]
    [SerializeField] private float tiempoParaConsiderarAtasco = 1.2f;
    [SerializeField] private float velocidadMinAtasco = 0.8f;
    [SerializeField] private float duracionManiobra = 0.8f;

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

    private float aceleracionInput;
    private float direccionInput;
    private bool frenando;

    private int indiceRuta = 0;
    private float tiempoLento = 0f;
    private float tiempoManiobra = 0f;
    private int sentidoManiobra = 1;

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
        ProcesarIA();

        Mover();
        Girar();

        AplicarSuspension();
        AplicarFuerzaDescendente();
        LimitarVelocidad();
        AplicarAntiVuelco();
    }

    void ProcesarIA()
    {
        if (puntosRuta == null || puntosRuta.Length == 0)
        {
            aceleracionInput = 0f;
            direccionInput = 0f;
            frenando = true;
            return;
        }

        // Maniobra de desatasco (reversa + giro alternado)
        if (tiempoManiobra > 0f)
        {
            tiempoManiobra -= Time.fixedDeltaTime;
            aceleracionInput = -1f;
            direccionInput = sentidoManiobra;
            frenando = false;
            return;
        }

        // Asegurar indice válido
        if (indiceRuta < 0) indiceRuta = 0;
        if (indiceRuta >= puntosRuta.Length) indiceRuta = 0;

        // Si llegamos al waypoint actual, avanzamos
        float distActual = Vector3.Distance(transform.position, puntosRuta[indiceRuta].position);
        if (distActual <= distanciaCambioPunto)
            indiceRuta = (indiceRuta + 1) % puntosRuta.Length;

        // Elegimos objetivo look-ahead (evita orbitar un punto cercano)
        Transform objetivo = ElegirObjetivoLookAhead();

        Vector3 local = transform.InverseTransformPoint(objetivo.position);

        // Si el objetivo quedó detrás (pasa en curvas cerradas o waypoints mal espaciados),
        // forzamos avanzar índice para no quedarnos girando.
        if (local.z < 0f)
        {
            indiceRuta = (indiceRuta + 1) % puntosRuta.Length;
            objetivo = ElegirObjetivoLookAhead();
            local = transform.InverseTransformPoint(objetivo.position);
        }

        // Steering por ángulo (más estable que x/dist)
        float angulo = Mathf.Atan2(local.x, Mathf.Max(0.001f, local.z)) * Mathf.Rad2Deg;
        direccionInput = Mathf.Clamp(angulo / Mathf.Max(1f, anguloMaxParaSteer), -1f, 1f);

        // Control de velocidad por curva
        float absAng = Mathf.Abs(angulo);
        float factorCurva = Mathf.InverseLerp(0f, anguloCurvaFuerte, absAng);

        float vel = rb.linearVelocity.magnitude;
        float velObjetivo = Mathf.Lerp(velocidadMaximaAdelante, velocidadObjetivoEnCurvaFuerte, factorCurva);

        // Acelera o frena según objetivo
        frenando = vel > velObjetivo + 0.5f;

        if (frenando)
        {
            aceleracionInput = 0f;
        }
        else
        {
            // En curvas afloja un poco
            aceleracionInput = Mathf.Lerp(1f, 0.35f, factorCurva);
        }

        // Anti “giro en el lugar”: si estoy muy lento y el ángulo es grande, no clavamos steer al palo
        if (vel < 2f && absAng > 50f)
        {
            direccionInput *= 0.5f;
            aceleracionInput = Mathf.Max(aceleracionInput, 0.35f); // empuja para salir
        }

        DetectarAtasco(distActual);
    }

    Transform ElegirObjetivoLookAhead()
    {
        float v = rb.linearVelocity.magnitude;
        float lookAhead = Mathf.Lerp(lookAheadMin, lookAheadMax, Mathf.Clamp01(v / 25f));

        // buscamos el primer punto cuya distancia sea >= lookAhead
        int idx = indiceRuta;
        Vector3 pos = transform.position;

        for (int k = 0; k < puntosRuta.Length; k++)
        {
            Transform p = puntosRuta[idx];
            if (p != null)
            {
                float d = Vector3.Distance(pos, p.position);
                if (d >= lookAhead)
                    return p;
            }
            idx = (idx + 1) % puntosRuta.Length;
        }

        return puntosRuta[indiceRuta];
    }

    void DetectarAtasco(float distanciaAlWaypointActual)
    {
        if (distanciaAlWaypointActual > 4f && rb.linearVelocity.magnitude < velocidadMinAtasco)
        {
            tiempoLento += Time.fixedDeltaTime;

            if (tiempoLento >= tiempoParaConsiderarAtasco)
            {
                tiempoManiobra = duracionManiobra;
                sentidoManiobra *= -1;
                tiempoLento = 0f;
            }
        }
        else
        {
            tiempoLento = 0f;
        }
    }

    void Mover()
    {
        Vector3 velocidadAdelante = Vector3.Project(rb.linearVelocity, transform.forward);
        float magnitud = velocidadAdelante.magnitude;

        // Adelante
        if (aceleracionInput > 0f && magnitud < velocidadMaximaAdelante)
            rb.AddForce(transform.forward * aceleracion, ForceMode.Acceleration);

        // Reversa (solo usada por maniobra)
        if (aceleracionInput < 0f && magnitud < velocidadMaximaReversa)
            rb.AddForce(-transform.forward * aceleracion * 0.7f, ForceMode.Acceleration);

        // Freno
        if (frenando && rb.linearVelocity.sqrMagnitude > 0.01f)
            rb.AddForce(-rb.linearVelocity.normalized * fuerzaFreno, ForceMode.Acceleration);
    }

    void Girar()
    {
        if (rb.linearVelocity.magnitude < velocidadMinimaParaGirar)
            return;

        float direccionMovimiento =
            Mathf.Sign(Vector3.Dot(rb.linearVelocity, transform.forward));

        float giro =
            direccionInput * direccionMovimiento *
            velocidadGiro * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, giro, 0f));
    }

    void AplicarSuspension()
    {
        if (Physics.Raycast(transform.position, Vector3.down,
            out RaycastHit impacto, distanciaSuspension, capaSuelo))
        {
            float compresion = distanciaSuspension - impacto.distance;

            float fuerzaResorte = compresion * fuerzaSuspension;
            float fuerzaAmortiguador =
                Vector3.Dot(rb.linearVelocity, Vector3.down) * amortiguacionSuspension;

            rb.AddForce(Vector3.up * (fuerzaResorte - fuerzaAmortiguador), ForceMode.Force);
        }
    }

    void AplicarFuerzaDescendente()
    {
        rb.AddForce(Vector3.down * fuerzaDescendente * rb.linearVelocity.magnitude, ForceMode.Force);
    }

    void LimitarVelocidad()
    {
        Vector3 velAdelante = Vector3.Project(rb.linearVelocity, transform.forward);
        float magnitud = velAdelante.magnitude;

        // adelante
        if (Vector3.Dot(rb.linearVelocity, transform.forward) > 0f && magnitud > velocidadMaximaAdelante)
            rb.linearVelocity = transform.forward * velocidadMaximaAdelante;

        // reversa
        if (Vector3.Dot(rb.linearVelocity, transform.forward) < 0f && magnitud > velocidadMaximaReversa)
            rb.linearVelocity = -transform.forward * velocidadMaximaReversa;
    }

    void AplicarAntiVuelco()
    {
        float inclinacion = Vector3.Dot(transform.right, Vector3.up);

        rb.AddTorque(
            -transform.forward * inclinacion * fuerzaAntiVuelco,
            ForceMode.Force
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * distanciaSuspension);

        if (puntosRuta != null && puntosRuta.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < puntosRuta.Length; i++)
            {
                if (!puntosRuta[i]) continue;
                Gizmos.DrawSphere(puntosRuta[i].position, 0.5f);

                Transform next = puntosRuta[(i + 1) % puntosRuta.Length];
                if (next) Gizmos.DrawLine(puntosRuta[i].position, next.position);
            }
        }
    }
}