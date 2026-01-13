using UnityEngine;
using JUTPS.JUInputSystem;

namespace JUTPS.VehicleSystem
{
    /// <summary>
    /// Bộ điều khiển phương tiện mô tô của JU.
    /// </summary>
    public class DMotorcycleController : JUWheeledVehicle
    {
        /// <summary>
        /// Lưu trữ <see cref="WheelCollider"/> của phương tiện và hành vi của bánh xe.
        /// </summary>
        [System.Serializable]
        public struct Wheel
        {
            /// <summary>
            /// Góc bẻ lái tối đa của bánh xe, giá trị từ -180 đến 180.
            /// </summary>
            [Range(-180, 180)] public float MaxSteerAngle;

            /// <summary>
            /// Cường độ ga của bánh xe, giá trị từ 0 đến 1 (0 là không gia tốc, 1 là có gia tốc).
            /// </summary>
            [Range(0, 1)] public float ThrottleIntensity;

            /// <summary>
            /// Cường độ phanh của bánh xe, giá trị từ 0 đến 1 (0 là không phanh, 1 là có lực phanh).
            /// </summary>
            [Range(0, 1)] public float BrakeIntensity;

            /// <summary>
            /// Thành phần Wheel Collider của bánh xe.
            /// </summary>
            public WheelCollider WheelCollider;

            /// <summary>
            /// Transform của mesh bánh xe sẽ di chuyển theo <see cref="WheelCollider"/>.
            /// </summary>
            public Transform WheelMesh;
        }

        /// <summary>
        /// Lưu trữ các thuộc tính liên quan đến độ nghiêng của mô tô khi vào cua ở tốc độ cao.
        /// </summary>
        [System.Serializable]
        public struct InclinationSettings
        {
            /// <summary>
            /// Độ nhạy của độ nghiêng khi ở tốc độ cao.
            /// </summary>
            [Min(0)] public float Sensitive;

            /// <summary>
            /// Tốc độ thực hiện việc nghiêng.
            /// </summary>
            [Min(0.1f)] public float Speed;

            /// <summary>
            /// Góc nghiêng tối đa của mô tô khi vào cua.
            /// </summary>
            [Range(0, 60)] public float MaxAngle;

            /// <summary>
            /// Độ nghiêng tự nhiên của phương tiện khi dừng lại, rất hữu ích để mô phỏng việc nhân vật chống chân xuống đất.
            /// </summary>
            [Range(-45, 45)] public float StopedInclination;

            /// <summary>
            /// Lực cản khí động học (drag) của phương tiện khi ở trên mặt đất.
            /// </summary>
            [Min(0)] public float OnGroundDrag;

            /// <summary>
            /// Lực cản khí động học (drag) của phương tiện khi rời mặt đất (trên không).
            /// </summary>
            [Min(0)] public float OffGroundDrag;
        }

        private Transform _rotationPivotParent;
        private Transform _rotationPivotChild;

        /// <summary>
        /// Bánh trước của xe mô tô.
        /// </summary>
        [Header("Wheels")]
        public Wheel FrontWheel;

        /// <summary>
        /// Bánh sau của xe mô tô.
        /// </summary>
        public Wheel BackWheel;

        /// <summary>
        /// Căn chỉnh phương tiện theo hướng pháp tuyến (normal) của mặt đất khi đang tiếp đất.
        /// </summary>
        public VehicleOverturnCheck OverturnCheck;

        /// <summary>
        /// Lưu trữ các thiết lập liên quan đến độ nghiêng của mô tô khi vào cua ở tốc độ cao.
        /// </summary>
        public InclinationSettings Inclination;

        /// <summary>
        /// Nếu đúng (true), phương tiện sẽ căn chỉnh trục "up" theo mặt đất nếu tag của collider mặt đất trùng với <seealso cref="LoopTag"/>.
        /// </summary>
        [Header("Looping")]
        public bool EnableLooping;

        /// <summary>
        /// Tag của collider đường vòng (loop), dùng để căn chỉnh phương tiện trên một bề mặt cụ thể nếu <seealso cref="EnableLooping"/> là true.
        /// </summary>
        public string LoopTag;

        /// <summary>
        /// Tốc độ xoay về hướng pháp tuyến của mặt đất nếu bề mặt đó có tag <seealso cref="LoopTag"/>.
        /// </summary>
        [Min(0.1f)] public float AlignWithLoopSpeed;

        /// <summary>
        /// Độ nghiêng hiện tại của mô tô.
        /// </summary>
        public float CurrentInclination { get; private set; }

        /// <summary>
        /// Trả về true nếu <seealso cref="JUVehicle.IsGrounded"/> là đúng và bề mặt va chạm có tag <seealso cref="LoopTag"/>. <para/>
        /// Khi đang đi trong vòng lặp (looping), phương tiện sẽ tự căn chỉnh theo hướng pháp tuyến của mặt đất.
        /// </summary>
        public bool IsLooping { get; private set; }

        /// <summary>
        /// Tạo một instance (thành phần) MotorcycleController trên GameObject.
        /// </summary>
        public DMotorcycleController() : base()
        {
            FrontWheel = new Wheel
            {
                MaxSteerAngle = 35,
                WheelCollider = null,
                WheelMesh = null,
                BrakeIntensity = 1,
                ThrottleIntensity = 0
            };

            BackWheel = new Wheel()
            {
                MaxSteerAngle = 0,
                WheelCollider = null,
                WheelMesh = null,
                ThrottleIntensity = 1,
                BrakeIntensity = 1
            };

            Inclination = new InclinationSettings()
            {
                Sensitive = 3,
                Speed = 1,
                MaxAngle = 45,
                StopedInclination = 20,
                OnGroundDrag = 5,
                OffGroundDrag = 1
            };

            LoopTag = "Loop";
            AlignWithLoopSpeed = 8;

            // (0, 0, 0) không được khuyến khích cho mô tô vì nó làm xe khó giữ thăng bằng hơn khi dừng lại.
            Engine.CenterOfMass = Vector3.up * 0.1f;
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();

            // Tạo các transform để quản lý độ nghiêng của mô tô.
            _rotationPivotParent = new GameObject("Motorcycle Lean Angle Pivot").transform;
            _rotationPivotChild = new GameObject("Motorcycle Lean Angle Z").transform;

            _rotationPivotChild.SetParent(_rotationPivotParent);
            _rotationPivotParent.position = transform.position;
            _rotationPivotParent.hideFlags = HideFlags.HideInHierarchy;

            _rotationPivotChild.SetParent(_rotationPivotChild);
            _rotationPivotParent.position = transform.position;
            _rotationPivotParent.hideFlags = HideFlags.HideInHierarchy;
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();

            if (!IsOn)
                return;

            // Kiểm tra chống lật
            OverturnCheck.OverturnCheck(transform);
            OverturnCheck.AntiOverturn(transform);
        }

        /// <inheritdoc/>
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!IsOn)
                return;

            // Xử lý độ nghiêng
            if (!IsLooping)
                MotorcycleLeanSystem();

            if (EnableLooping)
                LoopSystem();

            // Cho phép tự xoay về hướng thẳng đứng khi trên không nếu không phải đang trong vòng lặp
            CanTurnToUpInAir = !IsLooping;
        }

        protected virtual void MotorcycleLeanSystem()
        {
            // Không thực hiện nghiêng nếu cấu trúc cha-con không chính xác
            if (_rotationPivotChild.parent != _rotationPivotParent)
            {
                Debug.LogError($"Biến {nameof(_rotationPivotChild)} không phải là con của {nameof(_rotationPivotParent)}");
                return;
            }

            if (!FrontWheel.WheelCollider || !FrontWheel.WheelMesh || !BackWheel.WheelCollider || !BackWheel.WheelMesh)
                return;

            if (!FrontWheel.WheelCollider.GetGroundHit(out WheelHit frontHit) || !BackWheel.WheelCollider.GetGroundHit(out WheelHit rearHit))
                return;

            Vector3 groundNormal = (frontHit.normal + rearHit.normal).normalized;
            SimulateVehicleInclination(groundNormal);
        }

        protected virtual void LoopSystem()
        {
            IsLooping = false;

            if (!IsGrounded)
                return;

            if (!FrontWheel.WheelCollider.GetGroundHit(out WheelHit frontHit) || !BackWheel.WheelCollider.GetGroundHit(out WheelHit rearHit))
                return;

            IsLooping = frontHit.collider.tag.Equals(LoopTag);

            if (IsLooping)
            {
                // Căn chỉnh mô tô theo mặt phẳng của đường vòng (loop).
                Vector3 loopNormal = (frontHit.normal + rearHit.normal).normalized;
                AlignVehicleToNormal(loopNormal, AlignWithLoopSpeed);
            }
        }

        private void SimulateVehicleInclination(Vector3 groundAligment)
        {
            if (!IsGrounded)
                return;

            // Tính toán độ nghiêng
            if (Mathf.Abs(ForwardSpeed) > 1)
                CurrentInclination = Horizontal * Mathf.Abs(ForwardSpeed) * Inclination.Sensitive;
            else
                CurrentInclination = Mathf.Lerp(CurrentInclination, Inclination.StopedInclination, Time.deltaTime);

            CurrentInclination = Mathf.Clamp(CurrentInclination, -Inclination.MaxAngle, Inclination.MaxAngle);

            float inclinationSpeed = Mathf.Clamp01(Inclination.Speed * Time.deltaTime);

            // Xoay phương tiện.
            Quaternion pivotTargetRotation = Quaternion.FromToRotation(_rotationPivotParent.up, groundAligment) * _rotationPivotParent.rotation;
            _rotationPivotChild.localEulerAngles = new Vector3(0, 0, -CurrentInclination);
            _rotationPivotParent.position = transform.position;
            _rotationPivotParent.rotation = Quaternion.Slerp(_rotationPivotParent.rotation, pivotTargetRotation, inclinationSpeed);
            _rotationPivotParent.localEulerAngles = new Vector3(_rotationPivotParent.localEulerAngles.x, transform.localEulerAngles.y, _rotationPivotParent.localEulerAngles.z);

            // Áp dụng độ nghiêng.
            transform.rotation = Quaternion.Lerp(transform.rotation, _rotationPivotChild.rotation, inclinationSpeed);

            // Khóa xoay của Rigidbody.
            if (IsGrounded)
            {
                RigidBody.angularDamping = Inclination.OnGroundDrag;
                RigidBody.constraints = RigidbodyConstraints.FreezeRotationZ;
            }
            else
            {
                RigidBody.angularDamping = Inclination.OffGroundDrag;
                RigidBody.constraints = RigidbodyConstraints.None;
            }
        }

        /// <inheritdoc/>
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            VehicleGizmo.DrawVehicleInclination(_rotationPivotParent, _rotationPivotChild);
            VehicleGizmo.DrawOverturnCheck(OverturnCheck, transform);
        }

        /// <inheritdoc/>
        public override void UpdateWheelsData()
        {
            base.UpdateWheelsData();

            WheelsData = new WheelData[2];
            WheelsData[0] = new WheelData(FrontWheel.WheelCollider, FrontWheel.WheelMesh, true, FrontWheel.ThrottleIntensity, FrontWheel.BrakeIntensity, FrontWheel.MaxSteerAngle);
            WheelsData[1] = new WheelData(BackWheel.WheelCollider, BackWheel.WheelMesh, true, BackWheel.ThrottleIntensity, BackWheel.BrakeIntensity, BackWheel.MaxSteerAngle);
        }
    }
}