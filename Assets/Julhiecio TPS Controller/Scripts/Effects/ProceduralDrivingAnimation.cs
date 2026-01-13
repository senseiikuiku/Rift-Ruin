using UnityEngine;
using JUTPS.ExtendedInverseKinematics;
using JUTPS.VehicleSystem;
using JUTPS.ActionScripts;

namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Driver Procedural Animation")]
    [RequireComponent(typeof(DriveVehicles))]
    public class ProceduralDrivingAnimation : JUTPSActions.JUTPSAction
    {
        private DriveVehicles DriveAbility;

        [Header("Settings")]
        public bool Enabled = true;
        public bool FootPlacer; // Bật/Tắt tính năng đặt chân lên mặt đất (thường dùng cho xe máy)
        private Transform LeftFootTargetPosition, RightFootTargetPosition;

        public LayerMask GroundLayer; // Lớp va chạm của mặt đất

        [Header("Spine Lean")]
        [SerializeField] private bool SpineLean = true; // Bật/Tắt hiệu ứng nghiêng cột sống khi lái xe
        [Range(0, 1)]
        [SerializeField] private float LeanDirection = 0.2f; // Cường độ hướng nghiêng
        [SerializeField] private BodyLeanInert.Axis ForwardLeanAxis = BodyLeanInert.Axis.X; // Trục nghiêng về phía trước
        [SerializeField] private BodyLeanInert.Axis SidesLeanAxis = BodyLeanInert.Axis.Z; // Trục nghiêng sang hai bên
        public bool InvertForwardLean; // Đảo ngược hướng nghiêng trước/sau
        public bool InvertSideLean;    // Đảo ngược hướng nghiêng trái/phải

        private void Start()
        {
            // Lấy component điều khiển lái xe
            DriveAbility = GetComponent<DriveVehicles>();

            // Khởi tạo các đối tượng Transform ảo để làm mục tiêu IK cho bàn chân
            LeftFootTargetPosition = new GameObject("LeftFootTargetPosition").transform;
            RightFootTargetPosition = new GameObject("RightFootTargetPosition").transform;

            // Ẩn các đối tượng này trong cửa sổ Hierarchy để tránh làm rối dự án
            LeftFootTargetPosition.hideFlags = HideFlags.HideInHierarchy;
            RightFootTargetPosition.hideFlags = HideFlags.HideInHierarchy;

            LeftFootTargetPosition.position = transform.position;
            RightFootTargetPosition.position = transform.position;

            // Gán làm con của nhân vật để di chuyển theo nhân vật
            LeftFootTargetPosition.parent = transform;
            RightFootTargetPosition.parent = transform;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            // Kiểm tra điều kiện: Nếu tính năng bị tắt, không có khả năng lái, hoặc đang không lái xe thì thoát
            if (!Enabled || !DriveAbility || DriveAbility.DisableCharacterOnEnter || !DriveAbility.IsDriving)
                return;

            // Thực hiện tính toán chuyển động Procedural (theo thuật toán)
            DoProceduralDrivingAnimation(DriveAbility.CurrentVehicle, DriveAbility.CurrentVehicleCharacterIK);
        }

        protected virtual void DoProceduralDrivingAnimation(JUVehicle vehicle, JUVehicleCharacterIK vehicleCharacterIK)
        {
            // Nếu xe không tồn tại hoặc nhân vật đang ở trạng thái Ragdoll (văng ra) thì thoát
            if (!vehicle || !vehicleCharacterIK || TPSCharacter.IsRagdolled)
                return;

            // Thực hiện hiệu ứng đầu nhìn theo hướng lái
            HeadLookAtAnimation(vehicle, vehicleCharacterIK);

            // Giới hạn tốc độ hiển thị trong khoảng 0-15 để tính toán IK
            var vehicleSpeed = Mathf.Clamp(Mathf.Abs(vehicle.ForwardSpeed), 0, 15);

            // Xử lý IK cho Bàn tay (Giữ tay trên vô lăng/ghi đông)
            if (vehicleCharacterIK.InverseKinematicTargetPositions.RightHandPositionIK &&
                vehicleCharacterIK.InverseKinematicTargetPositions.LeftHandPositionIK)
            {
                anim.SetLeftHandOn(vehicleCharacterIK.InverseKinematicTargetPositions.LeftHandPositionIK, 1);
                anim.SetRightHandOn(vehicleCharacterIK.InverseKinematicTargetPositions.RightHandPositionIK, 1);
            }

            // Xử lý IK cho Bàn chân
            if (vehicleCharacterIK.InverseKinematicTargetPositions.LeftFootPositionIK &&
                vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK)
            {
                // Tính toán độ lệch (Hint) của đầu gối khi xe rẽ
                float leftHint = 6 * Mathf.Clamp(vehicle.FinalHorizontal, -1, 0) * vehicleSpeed / 20;
                float rightHint = 6 * Mathf.Clamp(vehicle.FinalHorizontal, 0, 1) * vehicleSpeed / 20;

                // Tạo vị trí mục tiêu cho đầu gối (IK Hints)
                float HintSpace = 3 * vehicleCharacterIK.AnimationWeights.HintMovementWeight;
                Vector3 LeftHintLocalPosition = Vector3.zero - Vector3.right * (HintSpace - leftHint) + Vector3.forward * 10;
                Vector3 RightHintLocalPosition = Vector3.zero + Vector3.right * (HintSpace + rightHint) + Vector3.forward * 10;

                // Nếu bật tính năng đặt chân xuống đất (ví dụ khi xe máy dừng lại)
                if (FootPlacer && vehicleCharacterIK.AnimationWeights.FootPlacement)
                {
                    Vector3 RightFootOriginalPosition = vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK.position;
                    Vector3 LeftFootOriginalPosition = vehicleCharacterIK.InverseKinematicTargetPositions.LeftFootPositionIK.position;

                    // Thực hiện Raycast xuống mặt đất để tìm vị trí đặt chân
                    RaycastHit LeftGroundHit;
                    Physics.Raycast(LeftFootOriginalPosition + vehicle.transform.forward * vehicleSpeed / 5 - vehicle.transform.right * 0.2f, -vehicle.transform.up, out LeftGroundHit, 0.8f, GroundLayer);

                    RaycastHit RightGroundHit;
                    Physics.Raycast(RightFootOriginalPosition + vehicle.transform.forward * vehicleSpeed / 5 + vehicle.transform.right * 0.2f, -vehicle.transform.up, out RightGroundHit, 0.8f, GroundLayer);

                    // Xác định vị trí trên mặt đất
                    Vector3 LeftFootOnGroundPosition = LeftGroundHit.collider ? LeftGroundHit.point + LeftGroundHit.normal * 0.15f : LeftFootOriginalPosition;
                    Vector3 RightFootOnGroundPosition = RightGroundHit.collider ? RightGroundHit.point + RightGroundHit.normal * 0.15f : RightFootOriginalPosition;

                    // Pha trộn (Lerp) vị trí chân giữa "trên xe" và "dưới đất" dựa trên tốc độ (tốc độ cao thì chân co lên xe)
                    Vector3 NewLeftFootPosition = Vector3.Lerp(LeftFootOnGroundPosition, LeftFootOriginalPosition, vehicleSpeed / 5);
                    Vector3 NewRightFootPosition = Vector3.Lerp(RightFootOnGroundPosition, RightFootOriginalPosition, vehicleSpeed / 5);

                    // Pha trộn góc xoay của bàn chân cho khớp với độ nghiêng của mặt đất
                    Quaternion NewLeftFootRotation = Quaternion.Lerp(Quaternion.FromToRotation(LeftFootTargetPosition.up, LeftGroundHit.normal) * LeftFootTargetPosition.rotation, vehicleCharacterIK.InverseKinematicTargetPositions.LeftFootPositionIK.rotation, vehicleSpeed / 5);
                    Quaternion NewRightFootRotation = Quaternion.Lerp(Quaternion.FromToRotation(RightFootTargetPosition.up, RightGroundHit.normal) * RightFootTargetPosition.rotation, vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK.rotation, vehicleSpeed / 5);

                    LeftFootTargetPosition.position = NewLeftFootPosition; LeftFootTargetPosition.rotation = NewLeftFootRotation;
                    RightFootTargetPosition.position = NewRightFootPosition; RightFootTargetPosition.rotation = NewRightFootRotation;

                    // Áp dụng kết quả IK vào Animator cho bàn chân và đầu gối
                    anim.SetLeftFootOn(LeftFootTargetPosition.position, LeftFootTargetPosition.rotation, 1, LeftHintLocalPosition, vehicleCharacterIK.AnimationWeights.HintMovementWeight);
                    anim.SetRightFootOn(vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK.position, RightFootTargetPosition.rotation, 1, RightHintLocalPosition, vehicleCharacterIK.AnimationWeights.HintMovementWeight);
                }
                else
                {
                    // Nếu không dùng FootPlacer, chỉ giữ chân ở vị trí mặc định trên xe
                    anim.SetLeftFootOn(vehicleCharacterIK.InverseKinematicTargetPositions.LeftFootPositionIK, 1, LeftHintLocalPosition, vehicleCharacterIK.AnimationWeights.HintMovementWeight);
                    anim.SetRightFootOn(vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK, 1, RightHintLocalPosition, vehicleCharacterIK.AnimationWeights.HintMovementWeight);
                }
            }

            // Xử lý Nghiêng cột sống (Spine Lean)
            if (!SpineLean) return;

            // Tính toán giá trị nghiêng dựa trên việc bẻ lái và tốc độ
            float SidewayLeanWeight = -vehicle.FinalHorizontal * (vehicleSpeed / 5);
            float ForwardLeanWeight = vehicle.FinalVertical * (vehicleSpeed / 4f);

            // Xác định trục nghiêng phía trước dựa trên cài đặt
            Vector3 ForwardAxis = new Vector3(0, 0, 0);
            switch (ForwardLeanAxis)
            {
                case BodyLeanInert.Axis.X:
                    ForwardAxis = InvertForwardLean ? Vector3.left : Vector3.right;
                    break;
                case BodyLeanInert.Axis.Y:
                    ForwardAxis = InvertForwardLean ? Vector3.down : Vector3.up;
                    break;
                case BodyLeanInert.Axis.Z:
                    ForwardAxis = InvertForwardLean ? Vector3.back : Vector3.forward;
                    break;
            }

            // Xác định trục nghiêng hai bên dựa trên cài đặt
            Vector3 SideAxis = new Vector3(0, 0, 0);
            switch (SidesLeanAxis)
            {
                case BodyLeanInert.Axis.X:
                    SideAxis = InvertSideLean ? Vector3.left : Vector3.right;
                    break;
                case BodyLeanInert.Axis.Y:
                    SideAxis = InvertSideLean ? Vector3.down : Vector3.up;
                    break;
                case BodyLeanInert.Axis.Z:
                    SideAxis = InvertSideLean ? Vector3.back : Vector3.forward;
                    break;
            }

            // Áp dụng độ nghiêng cột sống vào Animator
            anim.SpineInclination(ForwardAxis, ForwardLeanWeight, vehicleCharacterIK.AnimationWeights.FrontalLeanWeight);
            anim.SpineInclination(Vector3.Lerp(Vector3.up, SideAxis, LeanDirection), SidewayLeanWeight, vehicleCharacterIK.AnimationWeights.SideLeanWeight);
        }

        private void HeadLookAtAnimation(JUVehicle vehicle, JUVehicleCharacterIK vehicleCharacterIK)
        {
            if (!vehicle || !vehicleCharacterIK)
                return;

            // Tính toán hướng nhìn của đầu: Luôn nhìn về phía trước xe và hơi liếc sang hướng đang bẻ lái
            Vector3 LookVehicleDirection = transform.position + vehicle.transform.forward * 10 + vehicle.transform.up * 0.6f + vehicle.transform.right * vehicle.FinalHorizontal * 8;
            anim.NormalLookAt(LookVehicleDirection, vehicleCharacterIK.AnimationWeights.LookAtDirectionWeight, 0, 1);
        }
    }
}