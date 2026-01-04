using UnityEngine;

public class WheelVisualSync : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("Wheel Visual Mesh Transforms")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Optional offsets (local to mesh)")]
    [Tooltip("Si un mesh tiene el pivote corrido, ajustá acá (en metros) para centrarlo.")]
    public Vector3 frontLeftPosOffset;
    public Vector3 frontRightPosOffset;
    public Vector3 rearLeftPosOffset;
    public Vector3 rearRightPosOffset;

    [Tooltip("Si un mesh está rotado raro, ajustá acá (en grados).")]
    public Vector3 frontLeftRotOffset;
    public Vector3 frontRightRotOffset;
    public Vector3 rearLeftRotOffset;
    public Vector3 rearRightRotOffset;

    private void LateUpdate()
    {
        Sync(frontLeft, frontLeftMesh, frontLeftPosOffset, frontLeftRotOffset);
        Sync(frontRight, frontRightMesh, frontRightPosOffset, frontRightRotOffset);
        Sync(rearLeft, rearLeftMesh, rearLeftPosOffset, rearLeftRotOffset);
        Sync(rearRight, rearRightMesh, rearRightPosOffset, rearRightRotOffset);
    }

    private static void Sync(WheelCollider wc, Transform mesh, Vector3 posOffset, Vector3 rotOffset)
    {
        if (wc == null || mesh == null) return;

        wc.GetWorldPose(out Vector3 pos, out Quaternion rot);

        // Aplicamos offsets en el espacio local del mesh (pero posicionados en world)
        mesh.position = pos + (rot * posOffset);
        mesh.rotation = rot * Quaternion.Euler(rotOffset);
    }
}
