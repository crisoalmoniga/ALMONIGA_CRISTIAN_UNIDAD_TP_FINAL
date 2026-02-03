using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarControllerAI : MonoBehaviour
{
    [Header("IA - Ruta")]
    [Tooltip("Arrastrá acá el root que contiene los waypoints (por ejemplo: CheckpointsRoot).")]
    [SerializeField] private Transform waypointsRoot;

    [Tooltip("Si querés, podés cargar waypoints manualmente (si esto tiene elementos, ignora waypointsRoot).")]
    [SerializeField] private Transform[] waypointsManual;

    [Tooltip("Distancia para considerar waypoint 'pasado'.")]
    [SerializeField] private float waypointRadius = 6f;

    [Tooltip("Lookahead (cuántos metros adelante apunta para suavizar).")]
    [SerializeField] private float lookAhead = 12f;

    [Header("IA - Dificultad / Nivel")]
    [Tooltip("0 = fácil / torpe | 1 = difícil / rápido y ágil")]
    [Range(0f, 1f)]
    [SerializeField] private float aiLevel = 0.5f;

    [Tooltip("Qué tan agresivo acelera (extra) según aiLevel.")]
    [SerializeField] private float aggressiveness = 1.0f;

    [Header("IA - Frenos")]
    [Tooltip("Ángulo (grados) a partir del cual puede usar freno de mano en curvas cerradas.")]
    [SerializeField] private float handbrakeAngle = 55f;

    [Tooltip("Velocidad mínima para permitir freno de mano.")]
    [SerializeField] private float handbrakeMinSpeed = 10f;

    [Header("IA - Anti-bug choque")]
    [Tooltip("Reduce giro brutal después de un choque (0..1). Más bajo = más estabiliza.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float collisionSpinDamp = 0.6f;

    [Tooltip("Reduce velocidad lateral después de un choque (0..1). Más bajo = más estabiliza.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float collisionSideDamp = 0.55f;

    [Tooltip("Tiempo de estabilización tras choque (segundos).")]
    [SerializeField] private float collisionCooldown = 0.25f;

    [Header("Rigidbody - Estabilidad")]
    [Tooltip("Baja el centro de masa para evitar trompos/vuelcos.")]
    [SerializeField] private float centerOfMassY = -0.5f;

    [Header("Referencias")]
    [Tooltip("Puntos desde donde se tiran los rayos al suelo (ideal: 4 esquinas del auto).")]
    [InspectorName("Puntos de Hover")]
    [SerializeField] private Transform[] hoverPoints;

    [Tooltip("Qué capas cuentan como suelo.")]
    [InspectorName("Capas de Suelo")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Hover (Suspensión)")]
    [InspectorName("Altura Objetivo")]
    [SerializeField] private float hoverHeight = 1.2f;

    [InspectorName("Fuerza de Suspensión")]
    [SerializeField] private float hoverForce = 8000f;

    [InspectorName("Amortiguación Vertical")]
    [SerializeField] private float hoverDamp = 600f;

    [InspectorName("Longitud del Rayo")]
    [SerializeField] private float rayLength = 3.0f;

    [Header("Movimiento (Base)")]
    [InspectorName("Aceleración")]
    [SerializeField] private float acceleration = 30f;

    [InspectorName("Velocidad Máxima")]
    [SerializeField] private float maxSpeed = 25f;

    [Header("Giro (Base)")]
    [InspectorName("Torque de Giro")]
    [SerializeField] private float turnTorque = 180f;

    [InspectorName("Velocidad para Giro Máx.")]
    [SerializeField] private float steerFullAtSpeed = 12f;

    [InspectorName("Amortiguación de Trompo")]
    [SerializeField] private float yawDamping = 6f;

    [InspectorName("Límite de Giro")]
    [SerializeField] private float maxYawRate = 3.5f;

    [Header("Agarre lateral (Base)")]
    [InspectorName("Agarre Lateral")]
    [Range(0f, 1f)]
    [SerializeField] private float lateralGrip = 0.92f;

    [Header("Freno de mano (Base)")]
    [InspectorName("Agarre con Freno")]
    [Range(0f, 1f)]
    [SerializeField] private float handbrakeGrip = 0.45f;

    [InspectorName("Fuerza de Frenado")]
    [SerializeField] private float handbrakeBrake = 20f;

    [Header("Estabilidad")]
    [InspectorName("Auto Nivelar")]
    [SerializeField] private bool autoLevel = true;

    [InspectorName("Fuerza de Nivelado")]
    [SerializeField] private float autoLevelStrength = 8f;

    [InspectorName("Amortiguación de Nivelado")]
    [SerializeField] private float autoLevelDamp = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    // --- runtime ---
    private Rigidbody rb;
    private readonly List<Transform> waypoints = new();
    private int currentWp = 0;

    private float throttle;
    private float steer;
    private bool handbrake;

    private float grounded01;

    // “stats” escalados por dificultad
    private float accelMul;
    private float speedMul;
    private float turnMul;
    private float gripMul;

    // anti-choque
    private float collisionTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Si te trompea mucho, subí estos desde el Rigidbody también:
        // rb.angularDamping = 3f; rb.linearDamping = 0.1f;
        rb.angularDamping = 1.5f;
        rb.linearDamping = 0.05f;

        // Centro de masa bajo = menos trompo/vuelco
        rb.centerOfMass = new Vector3(0f, centerOfMassY, 0f);

        BuildWaypoints();
        ComputeDifficultyMultipliers();
    }

    private void BuildWaypoints()
    {
        waypoints.Clear();

        if (waypointsManual != null && waypointsManual.Length > 0)
        {
            for (int i = 0; i < waypointsManual.Length; i++)
                if (waypointsManual[i]) waypoints.Add(waypointsManual[i]);
        }
        else if (waypointsRoot != null)
        {
            // Toma hijos por orden de jerarquía
            for (int i = 0; i < waypointsRoot.childCount; i++)
            {
                var t = waypointsRoot.GetChild(i);
                if (t) waypoints.Add(t);
            }
        }

        if (waypoints.Count == 0 && debugLogs)
            Debug.LogWarning($"[CarControllerAI] No hay waypoints asignados en {name}.");
    }

    private void ComputeDifficultyMultipliers()
    {
        // 0..1 -> multipliers
        speedMul = Mathf.Lerp(0.88f, 1.18f, aiLevel);
        accelMul = Mathf.Lerp(0.85f, 1.25f, aiLevel) * Mathf.Max(0.5f, aggressiveness);
        turnMul = Mathf.Lerp(0.90f, 1.30f, aiLevel);
        gripMul = Mathf.Lerp(0.95f, 1.05f, aiLevel);

        if (debugLogs)
            Debug.Log($"[CarControllerAI] {name} aiLevel={aiLevel:0.00} speedMul={speedMul:0.00} accelMul={accelMul:0.00} turnMul={turnMul:0.00} gripMul={gripMul:0.00}");
    }

    private void FixedUpdate()
    {
        if (collisionTimer > 0f)
            collisionTimer -= Time.fixedDeltaTime;

        ThinkAI();
        ApplyPostCollisionStabilizer();

        ApplyHover();
        ApplyDrive();
        ApplySteer();
        ApplyLateralGrip();

        if (autoLevel)
            ApplyAutoLevel();
    }

    private void OnCollisionEnter(Collision collision)
    {
        collisionTimer = collisionCooldown;
    }

    private void ApplyPostCollisionStabilizer()
    {
        if (collisionTimer <= 0f) return;

        // Bajar giro brutal
        rb.angularVelocity *= collisionSpinDamp;

        // Bajar velocidad lateral (la que genera trompo)
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= collisionSideDamp;
        rb.linearVelocity = transform.TransformDirection(localVel);

        // Durante el cooldown: no acelerar a fondo
        throttle = Mathf.Min(throttle, 0.2f);
        handbrake = false;
    }

    // ---------------- AI Brain ----------------
    private void ThinkAI()
    {
        if (waypoints.Count == 0)
        {
            throttle = 0f;
            steer = 0f;
            handbrake = false;
            return;
        }

        Transform target = GetLookAheadTarget();
        Vector3 toTarget = (target.position - transform.position);
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist < 0.001f)
        {
            throttle = 0f;
            steer = 0f;
            handbrake = false;
            return;
        }

        Vector3 dir = toTarget / dist;

        // Ángulo de giro
        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        float absAngle = Mathf.Abs(angle);

        // Steer normalizado -1..1 (más suave)
        steer = Mathf.Clamp(angle / 45f, -1f, 1f);

        // Control de velocidad según curva
        float curve01 = Mathf.InverseLerp(0f, 70f, absAngle);          // 0 recta, 1 curva fuerte
        float desiredSpeed01 = Mathf.Lerp(1.0f, 0.45f, curve01);       // en curva baja

        float desiredSpeed = (maxSpeed * speedMul) * desiredSpeed01;

        float currentSpeedForward = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedError = desiredSpeed - currentSpeedForward;

        // Throttle: si falta velocidad acelera, si sobra suelta (y a veces frena)
        throttle = Mathf.Clamp(speedError / Mathf.Max(5f, desiredSpeed), -0.3f, 1f);

        // Handbrake solo en curvas fuertes y si viene rápido
        float speed = rb.linearVelocity.magnitude;
        handbrake = (absAngle > handbrakeAngle) && (speed > handbrakeMinSpeed);

        AdvanceWaypointIfNeeded();
    }

    private Transform GetLookAheadTarget()
    {
        Transform baseWp = waypoints[currentWp];

        if (lookAhead <= 0f) return baseWp;

        Vector3 pos = transform.position;

        for (int i = 0; i < waypoints.Count; i++)
        {
            int test = (currentWp + i) % waypoints.Count;
            float d = Vector3.Distance(pos, waypoints[test].position);
            if (d >= lookAhead) return waypoints[test];
        }

        return baseWp;
    }

    private void AdvanceWaypointIfNeeded()
    {
        Transform wp = waypoints[currentWp];
        Vector3 p = wp.position;
        p.y = transform.position.y;

        if (Vector3.Distance(transform.position, p) <= waypointRadius)
        {
            currentWp = (currentWp + 1) % waypoints.Count;
            if (debugLogs)
                Debug.Log($"[CarControllerAI] {name} -> Next WP {currentWp}");
        }
    }

    // ---------------- Physics (igual que player, con multiplicadores) ----------------

    private void ApplyHover()
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

    private void ApplyDrive()
    {
        float maxSpd = maxSpeed * speedMul;
        float accel = acceleration * accelMul;

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (Mathf.Abs(forwardSpeed) < maxSpd)
            rb.AddForce(transform.forward * (throttle * accel) * grounded01, ForceMode.Acceleration);

        if (handbrake)
            rb.AddForce(-rb.linearVelocity * handbrakeBrake, ForceMode.Acceleration);
    }

    private void ApplySteer()
    {
        float speed = rb.linearVelocity.magnitude;
        float steerStrength01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, steerFullAtSpeed));
        float desiredTorque = steer * (turnTorque * turnMul) * steerStrength01 * grounded01;

        rb.AddTorque(transform.up * desiredTorque, ForceMode.Acceleration);

        float yawRate = rb.angularVelocity.y;
        rb.AddTorque(transform.up * (-yawRate * yawDamping), ForceMode.Acceleration);

        // Clamp completo (X/Z/Y) = anti trompo infinito
        float yawClamp = Mathf.Lerp(maxYawRate * 0.9f, maxYawRate * 1.15f, aiLevel);
        Vector3 av = rb.angularVelocity;
        av.x = Mathf.Clamp(av.x, -1.2f, 1.2f);
        av.z = Mathf.Clamp(av.z, -1.2f, 1.2f);
        av.y = Mathf.Clamp(av.y, -yawClamp, yawClamp);
        rb.angularVelocity = av;
    }

    private void ApplyLateralGrip()
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

        float baseGrip = handbrake ? handbrakeGrip : lateralGrip;
        baseGrip = Mathf.Clamp01(baseGrip * gripMul);

        float accel01 = Mathf.Clamp01(Mathf.Abs(throttle));
        float grip = Mathf.Lerp(baseGrip, Mathf.Min(1f, baseGrip + 0.05f), 1f - accel01);

        localVel.x *= grip;
        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    private void ApplyAutoLevel()
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
