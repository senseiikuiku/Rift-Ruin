using UnityEngine;
using JUTPS;

namespace JU.CharacterSystem.AI
{
    /// <summary>
    /// Lớp cơ sở để tạo các trí tuệ nhân tạo (AI) cho <see cref="JUCharacterController"/>.
    /// </summary>
    public class JUCharacterAIBase : MonoBehaviour
    {
        /// <summary>
        /// Các chế độ định vị (Navigation).
        /// </summary>
        public enum NavigationModes
        {
            /// <summary>
            /// Không sử dụng hệ thống Navmesh (Di chuyển đơn giản theo đường thẳng).
            /// </summary>
            Simple,

            /// <summary>
            /// Sử dụng hệ thống Navmesh của Unity (Tính toán đường đi quanh vật cản).
            /// </summary>
            UseNavmesh
        }

        /// <summary>
        /// Các cài đặt điều hướng AI.
        /// </summary>
        [System.Serializable]
        public class AINavigationSettings
        {
            /// <summary>
            /// Chế độ định vị hiện tại.
            /// </summary>
            public NavigationModes Mode;

            /// <summary>
            /// <para>Chỉ sử dụng nếu <see cref="Mode"/> là <see cref="NavigationModes.UseNavmesh"/>.</para>
            /// Tốc độ làm mới (giây) việc tính toán đường đi để tránh làm nặng CPU.
            /// </summary>
            [Space]
            public float NavigationRefreshRate;
        }

        /// <summary>
        /// Cấu trúc dữ liệu điều khiển AI (Chứa các lệnh hành động).
        /// </summary>
        [System.Serializable]
        public struct AIControlData
        {
            /// <summary>
            /// AI có đang chạy (Sprint) hay không.
            /// </summary>
            public bool IsRunning;

            /// <summary>
            /// AI có đang ở tư thế sẵn sàng chiến đấu (Aiming/Attack Pose) hay không.
            /// </summary>
            public bool IsAttackPose;

            /// <summary>
            /// Lệnh thực hiện tấn công (Bóp cò súng hoặc vung kiếm).
            /// </summary>
            public bool IsAttacking;

            /// <summary>
            /// Hướng mà AI muốn di chuyển tới.
            /// </summary>
            public Vector3 MoveToDirection;

            /// <summary>
            /// Hướng mà AI muốn nhìn vào (Mục tiêu).
            /// </summary>
            public Vector3 LookToDirection;
        }

        private Vector3 _currentLookDirection; // Hướng nhìn hiện tại sau khi đã nội suy mượt mà

        /// <summary>
        /// Tốc độ xoay hướng nhìn (Aim) của AI.
        /// </summary>
        [Min(1f)]
        public float AimSpeed;

        /// <summary>
        /// Cho phép hoặc ngăn chặn AI di chuyển.
        /// </summary>
        public bool MoveEnabled;

        /// <summary>
        /// Cài đặt điều hướng của AI.
        /// </summary>
        public AINavigationSettings NavigationSettings;

        /// <summary>
        /// Collider của cơ thể nhân vật.
        /// </summary>
        public Collider BodyCollider { get; private set; }

        /// <summary>
        /// Dữ liệu điều khiển nhân vật. 
        /// Các lớp AI con sẽ ghi đè giá trị vào đây để điều khiển hành vi của nhân vật.
        /// </summary>
        protected AIControlData Control { get; set; }

        /// <summary>
        /// Tham chiếu đến Controller điều khiển nhân vật thực tế của JUTPS.
        /// </summary>
        public JUCharacterController Character { get; private set; }

        /// <summary>
        /// Trọng tâm của AI (Vị trí trung tâm của Collider).
        /// </summary>
        public Vector3 Center
        {
            get => BodyCollider ? BodyCollider.bounds.center : transform.position;
        }

        /// <summary>
        /// Constructor mặc định thiết lập các giá trị khởi tạo cơ bản.
        /// </summary>
        protected JUCharacterAIBase()
        {
            AimSpeed = 200;
            MoveEnabled = true;

            NavigationSettings = new AINavigationSettings
            {
                Mode = NavigationModes.UseNavmesh,
                NavigationRefreshRate = 0.3f
            };
        }

        /// <summary>
        /// Được gọi khi giá trị thay đổi trong Editor (Dùng để cập nhật tham chiếu).
        /// </summary>
        protected virtual void OnValidate()
        {
            FindComponents();
        }

        protected virtual void Reset()
        {
        }

        protected virtual void Awake()
        {
            FindComponents();

            // Kiểm tra xem nhân vật có gắn JUCharacterController không
            Debug.Assert(Character, $"The gameObject {name} hasn't a {typeof(JUCharacterController)} component.");

            _currentLookDirection = Character.transform.forward;

            // QUAN TRỌNG: Tắt quyền điều khiển bằng bàn phím/tay cầm để AI chiếm quyền điều khiển
            Character.UseDefaultControllerInput = false;
        }

        protected virtual void Start()
        {
        }

        protected virtual void OnDestroy()
        {
        }

        protected virtual void Update()
        {
            // Nếu AI chết, ngừng thực hiện logic
            if (Character.IsDead)
            {
                enabled = false;
                return;
            }

            // Cập nhật hướng xoay người/hướng nhìn mượt mà
            UpdateCharacterLookAt();
            // Chuyển các lệnh từ AI (Control) sang cho JUCharacterController thực thi
            UpdateCharacterControls();
        }

        protected virtual void OnDrawGizmos()
        {
        }

        protected virtual void OnDrawGizmosSelected()
        {
        }

        // Tự động tìm các thành phần cần thiết trên cùng GameObject
        private void FindComponents()
        {
            if (!BodyCollider) BodyCollider = GetComponent<Collider>();
            if (!Character) Character = GetComponent<JUCharacterController>();
        }

        /// <summary>
        /// Xử lý logic xoay hướng nhìn của AI một cách mượt mà theo thời gian.
        /// </summary>
        private void UpdateCharacterLookAt()
        {
            var lookDirection = Control.LookToDirection;

            // Nếu không có hướng nhìn xác định, mặc định nhìn về phía trước
            if (lookDirection.magnitude < 0.5f)
                lookDirection = Character.transform.forward;

            // Tính toán tốc độ xoay dựa trên góc lệch (Góc càng lớn xoay càng mượt)
            float angleToDirection = Vector3.Angle(_currentLookDirection.normalized, lookDirection);
            float lookToDirectionSpeed = Mathf.Clamp01(Time.deltaTime * (AimSpeed / Mathf.Max(angleToDirection, 0.01f)));

            // Nội suy (Lerp) hướng nhìn
            _currentLookDirection = Vector3.Lerp(_currentLookDirection, lookDirection, lookToDirectionSpeed);

            // Gán vị trí nhìn (LookAtPosition) cho nhân vật (cách 10 mét theo hướng nhìn)
            Vector3 lookAtPosition = transform.position + (_currentLookDirection * 10);
            Character.LookAtPosition = lookAtPosition;
        }

        /// <summary>
        /// Chuyển đổi dữ liệu từ cấu trúc AIControlData sang các hàm điều khiển của nhân vật.
        /// </summary>
        private void UpdateCharacterControls()
        {
            bool attackPose = Control.IsAttackPose;
            bool attacking = Control.IsAttacking;
            bool running = Control.IsRunning;
            Vector3 moveDirection = Control.MoveToDirection;

            // Chuẩn hóa vector di chuyển
            bool isMoving = moveDirection.magnitude > 0.1f;
            if (isMoving)
            {
                // Chỉ lấy hướng di chuyển trên mặt phẳng (không bay lên trời)
                moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
                moveDirection /= moveDirection.magnitude;
            }

            // Nếu không tấn công và không di chuyển, nhưng AI có lệnh nhìn vào đâu đó, thì bắt nhân vật nhìn về phía đó
            if (!Control.IsAttackPose && !isMoving && Control.LookToDirection.magnitude > 0)
                Character.DoLookAt(transform.position + (Control.LookToDirection * 10));

            // Cài đặt các thông số cho Character Controller
            Character.FiringModeIK = attackPose && Character.RightHandWeapon; // Bật IK tay khi cầm súng
            Character.FiringMode = attackPose && Character.RightHandWeapon;   // Bật chế độ bắn

            // Thực hiện hành động sử dụng item (Bắn, chém, ném...)
            Character.DefaultUseOfAllItems(attacking, attacking, attacking, true, attacking, attacking, attacking && !Character.RightHandWeapon);

            // Ngăn di chuyển nếu biến MoveEnabled bị tắt
            if (!MoveEnabled)
                moveDirection = Vector3.zero;

            // Thực thi lệnh di chuyển thực tế trên Controller
            Character._Move(moveDirection.x, moveDirection.z, running);
        }
    }
}