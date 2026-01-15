using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.GravitySwitchSystem
{
    [AddComponentMenu("JU TPS/Third Person System/Gravity Switcher/Gravity Box")]
    public class GravityBox : MonoBehaviour
    {
        [Header("Settings")]
        // Độ mạnh của lực trọng lực áp dụng lên các vật thể bên trong (mặc định là -35)
        public float GravityForce = -35;
        // Danh sách các thẻ (Tags) sẽ bị bỏ qua, không bị ảnh hưởng bởi trọng lực này
        public string[] TagsToIgnore;

        [Header("Alignment")]
        // Nếu bật, các vật thể có Rigidbody sẽ được xoay để khớp với hướng của Box
        public bool AlignRigidbodies;
        // Nếu bật, các nhân vật JUTPS sẽ tự động xoay hướng chân/đầu theo trục của Box
        public bool AlignCharacters;
        // Lực dùng để xoay/căn chỉnh vật thể
        public float AlignmentForce = -35;
        // Khoảng cách tối thiểu để dừng việc căn chỉnh (tránh bị rung lắc khi đã khớp hướng)
        public float DistanceToStopAligment;

        void Update()
        {
            // Biến tạm để lưu trữ các Collider được tìm thấy bên trong vùng trọng lực
            Collider[] colliders;
            // Gọi hàm mô phỏng trọng lực hình hộp từ hệ thống lõi JUGravity
            JUGravity.SimulateGravityBox(
                transform.position,         // Vị trí của Box
                transform.lossyScale,       // Kích thước của Box (tính cả tỉ lệ cha-con)
                transform.rotation,         // Độ xoay của Box
                -transform.up,              // Hướng trọng lực (mặc định là hướng xuống của trục Up)
                GravityForce,               // Lực trọng lực
                AlignRigidbodies,           // Có căn chỉnh Rigidbody không
                AlignmentForce,             // Lực căn chỉnh
                DistanceToStopAligment,     // Khoảng cách dừng căn chỉnh
                out colliders,              // Trả về danh sách các Collider đang nằm trong vùng này
                TagsToIgnore);              // Bỏ qua các vật thể có tag trong danh sách


            // Nếu tùy chọn căn chỉnh nhân vật được bật
            if (AlignCharacters)
                // Cập nhật hướng "Up" cho nhân vật JUTPS để họ có thể đi trên tường/trần nhà của Box
                JUGravity.AlignJUTPSCharacterUpOrientation(colliders, transform.up);
        }
#if UNITY_EDITOR
        // Hàm này chỉ chạy trong trình biên tập Unity (Scene View) để hỗ trợ nhìn trực quan
        private void OnDrawGizmos()
        {
            // Thiết lập ma trận xoay dựa trên vị trí, góc xoay và kích thước của Transform
            // Điều này giúp Gizmos (hình vẽ) xoay đúng theo hướng của Object
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.matrix = rotationMatrix;

            // Vẽ một hình hộp mờ màu xanh lá đại diện cho vùng ảnh hưởng
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawCube(Vector3.zero, transform.localScale);

            // Vẽ khung viền cho hình hộp
            Gizmos.color = new Color(1, 1, 1, 0.2f);
            Gizmos.DrawWireCube(Vector3.zero, transform.localScale);

            // Vẽ một mũi tên chỉ hướng trọng lực đang tác động bên trong Box
            UnityEditor.Handles.ArrowHandleCap(0, transform.position + transform.up * 0.5f, Quaternion.LookRotation(-transform.up), 1, EventType.Repaint);
        }
#endif
    }

}