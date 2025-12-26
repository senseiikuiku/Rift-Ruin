using UnityEngine;
using JUTPS.GravitySwitchSystem; // Cần thiết để gọi JUGravity

public class GravityBox : MonoBehaviour
{
    [Header("Settings")]
    public float GravityForce = -35f;
    public string[] TagsToIgnore;

    [Header("Alignment")]
    public bool AlignRigidbodies = true;
    public bool AlignCharacters = true;
    public float AlignmentForce = -35f;
    public float DistanceToStopAligment = 0.1f;

    // Sử dụng FixedUpdate vì đây là các tính toán tác động lực (Physics)
    void FixedUpdate()
    {
        Collider[] colliders;

        // Gọi hàm xử lý lõi từ thư viện JU TPS
        JUGravity.SimulateGravityBox(
            transform.position,
            transform.lossyScale,
            transform.rotation,
            -transform.up,
            GravityForce,
            AlignRigidbodies,
            AlignmentForce,
            DistanceToStopAligment,
            out colliders,
            TagsToIgnore
        );

        // Căn chỉnh hướng xoay của nhân vật JU TPS nếu họ ở trong vùng này
        if (AlignCharacters && colliders != null)
        {
            JUGravity.AlignJUTPSCharacterUpOrientation(colliders, transform.up);
        }
    }

#if UNITY_EDITOR
    // Vẽ vùng ảnh hưởng trong Editor để dễ dàng căn chỉnh
    private void OnDrawGizmos()
    {
        // Thiết lập ma trận vẽ theo Transform của Object
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;

        // Vẽ khối đặc trong suốt màu xanh lá
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);

        // Vẽ khung dây trắng
        Gizmos.color = new Color(1, 1, 1, 0.2f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        // Vẽ mũi tên chỉ hướng trọng lực (ngược với trục Up của Box)
        UnityEditor.Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(-transform.up), 1f, EventType.Repaint);
    }
#endif
}