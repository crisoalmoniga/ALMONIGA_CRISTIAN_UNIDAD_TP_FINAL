using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, -6f);

    [Header("Smoothness")]
    [Tooltip("Más bajo = más delay (más suave).")]
    [SerializeField] private float positionSmoothTime = 0.08f;

    [Tooltip("Qué tan rápido acompaña la rotación. Más bajo = más delay.")]
    [SerializeField] private float rotationLerpSpeed = 8f;

    [Header("Look")]
    [Tooltip("Punto al que mira (altura sobre el auto).")]
    [SerializeField] private float lookHeight = 1.2f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (!target) return;

        // POSICIÓN suavizada (tipo resorte suave)
        Vector3 desiredPos = target.TransformPoint(offset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, positionSmoothTime);

        // ROTACIÓN suavizada (delay leve al doblar)
        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime));
    }
}
