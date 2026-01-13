using JU.AI;
using JUTPS.AI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace JU.CharacterSystem.AI
{
    /// <summary>
    /// Bộ điều khiển AI cho Zombie. Kế thừa từ JUCharacterAIBase.
    /// </summary>
    [AddComponentMenu("JU TPS/AI/Zombie AI")]
    public class JU_AI_Zombie : JUCharacterAIBase, IOnSetTarget, IOnHear
    {
        /// <summary>
        /// Các trạng thái của Zombie.
        /// </summary>
        public enum ZombieState
        {
            /// <summary>
            /// Đang đi tuần tra trên một lộ trình hoặc trong một khu vực nhất định.
            /// </summary>
            Patrolling,

            /// <summary>
            /// Đang tấn công một đối tượng (khi mục tiêu nằm trong tầm mắt).
            /// </summary>
            Attacking,

            /// <summary>
            /// Mất dấu mục tiêu, di chuyển tới vị trí cuối cùng mà mục tiêu từng ở đó.
            /// </summary>
            MoveToLastTargetPosition,

            /// <summary>
            /// Đang tìm kiếm mục tiêu tại vị trí cuối cùng bằng cách di chuyển ngẫu nhiên quanh đó.
            /// </summary>
            SearhLastTarget,
        }

        /// <summary>
        /// Cài đặt chung cho Zombie.
        /// </summary>
        [System.Serializable]
        public struct GeneralSettings
        {
            /// <summary>
            /// Thời gian tối đa Zombie sẽ tìm kiếm mục tiêu trước khi bỏ cuộc và quay lại tuần tra.
            /// </summary>
            public float SearhLastTargetTime;
        }

        private Collider _currentTarget; // Mục tiêu hiện tại (thường là Player)
        private Vector3 _spawnPosition;   // Vị trí lúc Zombie mới sinh ra
        private Vector3 _lastTargetPosition; // Vị trí cuối cùng nhìn thấy mục tiêu
        private float _searchLastTargetTimer; // Bộ đếm thời gian tìm kiếm

        private JU_AIActionBase _currentAction; // Hành động AI hiện tại đang thực hiện

        [Header("References")]
        /// <summary>
        /// Transform của đầu Zombie, dùng cho hệ thống tầm nhìn (Field of View).
        /// </summary>
        public Transform Head;

        /// <summary>
        /// Lộ trình tuần tra (nếu đi theo các điểm waypoint).
        /// </summary>
        public WaypointPath PatrolPath;

        /// <summary>
        /// Khu vực tuần tra (nếu di chuyển tự do trong một vùng hình hộp).
        /// </summary>
        public JUBoxArea PatrolArea;

        [Space]
        public GeneralSettings General;

        [Header("Sensors")]
        /// <summary>
        /// Cảm biến tầm nhìn để phát hiện mục tiêu.
        /// </summary>
        public FieldOfView FieldOfView;

        /// <summary>
        /// Cảm biến sát thương, giúp Zombie quay lại nhìn kẻ đã tấn công nó.
        /// </summary>
        public DamageDetector DamageDetector;

        /// <summary>
        /// Cảm biến thính giác, giúp Zombie di chuyển tới nguồn âm thanh (tiếng súng, nổ).
        /// </summary>
        public HearSystem.HearSensor Hear;

        [Header("Actions")]
        /// <summary>
        /// Nếu không có lộ trình hay khu vực cụ thể, Zombie sẽ di chuyển ngẫu nhiên.
        /// </summary>
        public bool PatrolRandomlyIfNotHavePath;

        /// <summary>
        /// Hành động: Di chuyển ngẫu nhiên quanh một điểm.
        /// </summary>
        public MoveRandomAroundPoint MoveRandom;

        /// <summary>
        /// Hành động: Đi tuần theo Waypoint.
        /// </summary>
        public FollowWaypoint FollowPatrolPath;

        /// <summary>
        /// Hành động: Di chuyển ngẫu nhiên trong khu vực giới hạn.
        /// </summary>
        public MoveRandomInsideArea PatrolInsideArea;

        /// <summary>
        /// Hành động: Di chuyển tới một điểm cố định (vị trí cuối của mục tiêu hoặc nơi có tiếng động).
        /// </summary>
        public FollowPoint MoveToLastTargetPosition;

        /// <summary>
        /// Hành động: Tìm kiếm mục tiêu bằng cách đi loanh quanh vị trí mất dấu.
        /// </summary>
        public MoveRandomAroundPoint SearchLastTarget;

        /// <summary>
        /// Hành động: Tấn công mục tiêu (Cận chiến hoặc bắn súng).
        /// </summary>
        public Attack Attack;

        // Các sự kiện (Events) để các script khác có thể đăng ký lắng nghe
        public event UnityAction<GameObject> OnSetTarget;
        public event UnityAction<Vector3, GameObject> OnHear;

        /// <summary>
        /// Thuộc tính quản lý mục tiêu hiện tại.
        /// </summary>
        public Collider CurrentTarget
        {
            get => _currentTarget;
            private set
            {
                if (_currentTarget == value) return;
                _currentTarget = value;
                // Kích hoạt sự kiện khi đổi mục tiêu
                OnSetTarget?.Invoke(value ? value.gameObject : null);
            }
        }

        public ZombieState CurrentState { get; private set; }

        // Constructor: Khởi tạo các giá trị mặc định cho Zombie
        public JU_AI_Zombie()
        {
            General = new GeneralSettings { SearhLastTargetTime = 15 };
            PatrolRandomlyIfNotHavePath = true;

            // Khởi tạo các lớp Hành động (Actions)
            MoveRandom = new MoveRandomAroundPoint();
            FollowPatrolPath = new FollowWaypoint();
            PatrolInsideArea = new MoveRandomInsideArea();
            MoveToLastTargetPosition = new FollowPoint();
            SearchLastTarget = new MoveRandomAroundPoint();
            Attack = new Attack();
            FieldOfView = new FieldOfView();

            // Thiết lập thông số mặc định (Tầm nhìn, khoảng cách tấn công...)
            FieldOfView.Distance = 10;
            FollowPatrolPath.StopDistance = 2f;
            Attack.MeleeAttack.AttackDistance = 0.9f;
        }

        protected override void Start()
        {
            base.Start();
            _spawnPosition = transform.position;

            // Đảm bảo vị trí sinh ra nằm trên NavMesh để AI di chuyển được
            if (JU_Ai.ClosestToNavMesh(_spawnPosition, out var spawnPointOnNavmesh))
            {
                _spawnPosition = spawnPointOnNavmesh;
            }

            // Thiết lập (Setup) tất cả các Action và Sensor
            MoveRandom.Setup(this);
            FollowPatrolPath.Setup(this);
            PatrolInsideArea.Setup(this);
            MoveToLastTargetPosition.Setup(this);
            SearchLastTarget.Setup(this);
            Attack.Setup(this);
            FieldOfView.Setup(this);
            DamageDetector.Setup(this);
            Hear.Setup(this);

            // Đăng ký sự kiện khi nghe thấy tiếng động
            Hear.OnHear.AddListener(OnZombieHear);
        }

        protected override void Update()
        {
            base.Update();
            // Cập nhật Máy trạng thái (Finite State Machine)
            UpdateCurrentState();
        }

        /// <summary>
        /// Máy trạng thái cốt lõi của Zombie.
        /// </summary>
        private void UpdateCurrentState()
        {
            AIControlData control = new AIControlData();

            // 1. Cập nhật cảm biến tầm nhìn
            FieldOfView.Update(Head);
            CurrentTarget = FieldOfView.NearestColliderInView;

            // 2. Chuyển trạng thái sang tấn công nếu thấy mục tiêu
            if (CurrentTarget)
            {
                _searchLastTargetTimer = 0;
                _lastTargetPosition = CurrentTarget.bounds.center;
                CurrentState = ZombieState.Attacking;
            }

            // 3. Thực thi Action dựa trên trạng thái hiện tại
            switch (CurrentState)
            {
                case ZombieState.Patrolling:
                    UpdatePatrolState(ref control);
                    break;
                case ZombieState.Attacking:
                    UpdateAttackState(ref control);
                    break;
                case ZombieState.MoveToLastTargetPosition:
                    UpdateMoveToLastTargetPositionState(ref control);
                    break;
                case ZombieState.SearhLastTarget:
                    UpdateSearchLastTargetState(ref control);
                    break;
            }

            // Nếu không bận tấn công, cập nhật cảm biến sát thương để phản ứng khi bị bắn tỉa
            if (CurrentState != ZombieState.Attacking)
                DamageDetector.Update(ref control);

            // Gửi dữ liệu điều khiển về lớp cơ sở để di chuyển nhân vật
            Control = control;
        }

        // --- CHI TIẾT CÁC TRẠNG THÁI ---

        private void UpdatePatrolState(ref AIControlData control)
        {
            // Ưu tiên: 1. Lộ trình (Path) -> 2. Khu vực (Area) -> 3. Ngẫu nhiên quanh điểm Spawn
            if (!PatrolPath && !PatrolArea && PatrolRandomlyIfNotHavePath)
            {
                _currentAction = MoveRandom;
                MoveRandom.Update(_spawnPosition, ref control);
                return;
            }

            if (PatrolPath)
            {
                _currentAction = FollowPatrolPath;
                FollowPatrolPath.Update(PatrolPath, ref control);
            }
            else
            {
                _currentAction = PatrolInsideArea;
                PatrolInsideArea.Update(PatrolArea, ref control);
            }
        }

        private void UpdateAttackState(ref AIControlData control)
        {
            // Nếu mất dấu mục tiêu trong khi đang tấn công
            if (CurrentState == ZombieState.Attacking && !CurrentTarget)
            {
                CurrentState = ZombieState.MoveToLastTargetPosition;
                return;
            }

            _currentAction = Attack;
            Attack.Update(CurrentTarget.gameObject, ref control);
        }

        private void UpdateMoveToLastTargetPositionState(ref AIControlData control)
        {
            _currentAction = MoveToLastTargetPosition;
            MoveToLastTargetPosition.Update(_lastTargetPosition, ref control);

            // Khi đã đến nơi mục tiêu biến mất -> Chuyển sang tìm kiếm xung quanh
            if (MoveToLastTargetPosition.IsStopedClosestToDestination)
                CurrentState = ZombieState.SearhLastTarget;
        }

        private void UpdateSearchLastTargetState(ref AIControlData control)
        {
            _currentAction = SearchLastTarget;
            _searchLastTargetTimer += Time.deltaTime;

            // Nếu tìm quá lâu không thấy -> Quay lại đi tuần
            if (_searchLastTargetTimer > General.SearhLastTargetTime)
            {
                CurrentState = ZombieState.Patrolling;
                return;
            }

            SearchLastTarget.Update(_lastTargetPosition, ref control);
        }

        /// <summary>
        /// Phản ứng khi nghe thấy tiếng động.
        /// </summary>
        private void OnZombieHear(Vector3 position, GameObject source)
        {
            // Nếu đang mải tấn công ai đó thì bỏ qua tiếng động
            if (CurrentState == ZombieState.Attacking)
                return;

            // Chạy tới nơi phát ra tiếng động
            _lastTargetPosition = position;
            CurrentState = ZombieState.MoveToLastTargetPosition;

            OnHear?.Invoke(position, source);
        }
    }
}