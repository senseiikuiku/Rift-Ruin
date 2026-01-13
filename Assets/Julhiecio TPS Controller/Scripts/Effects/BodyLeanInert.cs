using JUTPSActions;
using UnityEditor;
using UnityEngine;

namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Body Lean")]
    public class BodyLeanInert : JUTPSAction
    {
        public JUFootPlacement JUFootPlacer; // Tham chiếu đến hệ thống đặt chân (để xử lý hạ thấp trọng tâm)
        public Transform RootBone; // Xương gốc của nhân vật (thường là xương Hips/Chậu)

        [Header("Settings")]
        public bool RootBoneSpineLean = true; // Bật/Tắt hiệu ứng nghiêng xương gốc
        public bool RootBoneSpineMovement = true; // Bật/Tắt hiệu ứng di chuyển xương gốc (lên/xuống)

        public float RootBoneLeanIntensity = 20; // Cường độ nghiêng của xương gốc
        public float RootBoneLeanSpeed = 8; // Tốc độ chuyển đổi trạng thái nghiêng
        public float RootBoneDownMovementIntensity = 0.25f; // Cường độ hạ thấp người khi dừng lại đột ngột
        public float BlockForwardLeanWeight = 4; // Trọng số để hãm độ nghiêng khi đang di chuyển

        float Speed; // Tốc độ di chuyển hiện tại của nhân vật (nội bộ)
        float Lean; // Giá trị góc nghiêng hiện tại (nội bộ)

        Vector3 NotAffectedEulerAngles; // Lưu trữ góc xoay gốc trước khi bị ảnh hưởng bởi quán tính
        Vector3 NotAffectedUpward; // Lưu trữ hướng hướng lên (Up) gốc

        public Axis AxisToLean; // Trục mà nhân vật sẽ nghiêng theo

        public enum Axis { X, Y, Z }

        protected override void Awake()
        {
            base.Awake();
            // Tự động tìm kiếm JUFootPlacement nếu chưa gán trong Inspector
            if (JUFootPlacer == null) JUFootPlacer = GetComponent<JUFootPlacement>();

            // Tự động tìm xương Hips (xương chậu) của nhân vật
            if (RootBone == null) RootBone = anim.GetBoneTransform(HumanBodyBones.Hips);

            // Đặt Animator ở chế độ Fixed Update để đồng bộ với vật lý
            anim.updateMode = AnimatorUpdateMode.Fixed;
        }

        void OnAnimatorIK()
        {
            // Lưu lại góc xoay cục bộ của xương gốc từ Animation trước khi chúng ta can thiệp chỉnh sửa
            NotAffectedEulerAngles = RootBone.localEulerAngles;
        }

        void LateUpdate()
        {
            // Thực hiện tính toán quán tính sau khi mọi Animation đã được xử lý xong
            DoInert();
        }

        private void OnEnable()
        {
            // Đặt lại các giá trị khi script được kích hoạt lại
            Speed = 0;
            Lean = 0;
        }

        /// <summary>
        /// Hàm xử lý chính cho hiệu ứng quán tính (Nghiêng và Nhún người)
        /// </summary>
        void DoInert()
        {
            Vector3 euler = NotAffectedEulerAngles;
            NotAffectedUpward = RootBone.up;

            // Kiểm tra các trạng thái không nên thực hiện nghiêng người (Đang đánh melee, đang ngã, đang nhắm bắn, đang lái xe, đã chết hoặc đang trên không)
            if (TPSCharacter.IsMeleeAttacking || TPSCharacter.IsRagdolled || TPSCharacter.IsAiming || TPSCharacter.FiringMode || TPSCharacter.IsDriving || TPSCharacter.IsDead || !TPSCharacter.IsGrounded)
            {
                Speed = 0;
                Lean = 0;
                return;
            }

            // Nội suy tốc độ dựa trên hệ số vận tốc của nhân vật
            Speed = Mathf.Lerp(Speed, TPSCharacter.VelocityMultiplier, 10 * Time.deltaTime);

            if (TPSCharacter.IsMoving)
            {
                // Khi đang di chuyển: Nghiêng người về phía trước dựa trên tốc độ
                Lean = Mathf.Lerp(Lean, (Speed * RootBoneLeanIntensity / BlockForwardLeanWeight), RootBoneLeanSpeed * Time.deltaTime);
            }
            else
            {
                // Khi dừng lại đột ngột: Tạo hiệu ứng quán tính bật ngược lại hoặc nhún người xuống
                Lean = Mathf.Lerp(Lean, -(Speed * RootBoneLeanIntensity / 2), RootBoneLeanSpeed * Time.deltaTime);

                // Nếu có hệ thống đặt chân, hạ thấp trọng tâm xương gốc (Y position) để tạo cảm giác nhân vật nhún chân khi dừng
                if (JUFootPlacer != null && RootBoneSpineMovement)
                {
                    JUFootPlacer.LastBodyPositionY -= RootBoneDownMovementIntensity * Mathf.Abs(Lean) / 10 * Time.deltaTime;
                }
            }

            // Áp dụng góc nghiêng vào trục đã chọn
            switch (AxisToLean)
            {
                case Axis.X:
                    euler.x += Lean;
                    break;
                case Axis.Y:
                    euler.y += Lean;
                    break;
                case Axis.Z:
                    euler.z += Lean;
                    break;
            }

            // Gán lại góc xoay mới cho xương gốc
            RootBone.localRotation = Quaternion.Euler(euler);
        }

#if UNITY_EDITOR
        // Vẽ các chỉ dẫn hỗ trợ trong cửa sổ Scene của Unity Editor
        private void OnDrawGizmos()
        {
            if (RootBone == null) return;

            // Tính toán góc nghiêng hiện tại để hiển thị thông số
            float angle = Vector3.SignedAngle(NotAffectedUpward, RootBone.up, RootBone.right);
            if (angle == 0) return;

            // Màu sắc thay đổi từ Xanh lá sang Đỏ dựa trên độ nghiêng
            Color color = Color.Lerp(Color.green, Color.red, angle / 10);
            Handles.color = color;

            // Vẽ cung tròn hiển thị góc nghiêng
            Handles.DrawWireArc(RootBone.position, -RootBone.right, RootBone.up, angle, 0.5f);

            Color colortransparent = color; colortransparent.a = 0.1f;
            Handles.color = colortransparent;
            Handles.DrawSolidArc(RootBone.position, -RootBone.right, RootBone.up, angle, 0.5f);

            // Vẽ các đường thẳng mô phỏng xương
            Handles.DrawLine(RootBone.position, RootBone.position + RootBone.up * 0.5f);
            Handles.color = Color.white;

            // Vẽ đường đứt đoạn biểu thị vị trí thẳng đứng ban đầu
            Handles.DrawDottedLine(RootBone.position, RootBone.position + NotAffectedUpward * 0.5f, 2);

            // Hiển thị số đo góc ngay trên xương gốc
            Handles.Label(RootBone.position + NotAffectedUpward * 0.6f, ((int)angle).ToString());
        }
#endif
    }
}