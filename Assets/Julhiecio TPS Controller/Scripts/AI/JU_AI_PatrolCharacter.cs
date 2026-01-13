using JU.AI;
using JU.CharacterSystem.AI.EscapeSystem;
using JU.CharacterSystem.AI.HearSystem;
using JUTPS;
using JUTPS.AI;
using UnityEngine;
using UnityEngine.Events;

namespace JU.CharacterSystem.AI
{
    /// <summary>
    /// Bộ điều khiển AI tuần tra (Patrol AI Controller).
    /// </summary>
    [AddComponentMenu("JU TPS/AI/Patrol AI")]
    public class JU_AI_PatrolCharacter : JUCharacterAIBase, IOnSetTarget, IOnHear
    {
        /// <summary>
        /// Các trạng thái của AI tuần tra.
        /// </summary>
        public enum PatrolStates
        {
            /// <summary>
            /// Đang tuần tra (Đứng yên hoặc di chuyển dọc theo lộ trình).
            /// </summary>
            Patrol,

            /// <summary>
            /// Di chuyển đến vị trí nghi ngờ có mục tiêu (ví dụ sau khi nghe tiếng động).
            /// </summary>
            MovingToPossibleTargetPosition,

            /// <summary>
            /// Đang tìm kiếm mục tiêu bị mất dấu.
            /// </summary>
            SearchingForLostTarget,

            /// <summary>
            /// Đang tấn công mục tiêu.
            /// </summary>
            Attacking
        }

        /// <summary>
        /// Các thiết lập chung cho AI.
        /// </summary>
        [System.Serializable]
        public struct GeneralSettings
        {
            // Thời gian tìm kiếm mục tiêu tối đa trước khi bỏ cuộc.
            public float MaxSearchTargetTime;

            /// <summary>
            /// Thời gian chờ trước khi xác nhận mất dấu mục tiêu (nếu mục tiêu không còn trong tầm nhìn).
            /// Sau đó AI sẽ đi tìm hoặc quay lại lộ trình tuần tra.
            /// </summary>
            public float LoseTargetDelay;

            /// <summary>
            /// AI sẽ trở nên cảnh giác (Alert) nếu nhìn thấy kẻ địch.
            /// Khi cảnh giác, AI sẽ hung hăng hơn: Thay vì chỉ đi kiểm tra tiếng động, 
            /// nó sẽ trực tiếp lùng sục và tấn công ngay khi thấy.
            /// </summary>
            public float AlertMaxTime;
        }

        private float _loseTargetTimer;      // Bộ đếm thời gian mất mục tiêu
        private float _searchTargetTimer;    // Bộ đếm thời gian đang tìm kiếm
        private Collider _target;            // Lưu trữ Collider của mục tiêu hiện tại
        private JUHealth _targetHealth;      // Máu của mục tiêu để kiểm tra xem đã chết chưa
        private Vector3 _possibleTargetPosition; // Vị trí nghi ngờ có mục tiêu

        private Vector3 _spawnPosition;      // Vị trí AI sinh ra (để quay về hoặc đi quanh đó)
        private float _inAlertTimeTargetLosed; // Thời gian đã trôi qua kể từ khi mất dấu trong trạng thái cảnh giác
        private JU_AIActionBase _currentAction; // Hành động (Action) hiện tại đang thực thi

        /// <summary>
        /// Transform của đầu nhân vật, dùng cho cảm biến tầm nhìn (Field of View).
        /// </summary>
        public Transform Head;

        /// <summary>
        /// Cấu hình thiết lập chung.
        /// </summary>
        [Header("Patrol AI")]
        public GeneralSettings General;

        /// <summary>
        /// Cảm biến tầm nhìn của AI.
        /// </summary>
        [Header("Sensors")]
        public FieldOfView FieldOfView;

        /// <summary>
        /// Cảm biến thính giác của AI.
        /// </summary>
        public HearSensor HearSensor;

        /// <summary>
        /// Đường đi (Waypoint) được sử dụng để tuần tra.
        /// </summary>
        [Header("Patrol Areas")]
        public WaypointPath PatrolPath;

        /// <summary>
        /// Khu vực tuần tra nếu không sử dụng đường đi cố định.
        /// </summary>
        public JUBoxArea PatrolArea;

        /// <summary>
        /// Nếu không có lộ trình hay khu vực cụ thể, AI sẽ đi lang thang ngẫu nhiên.
        /// </summary>
        [Header("States")]
        public bool PatrolRandomlyIfNotHavePath;

        // --- CÁC HÀNH ĐỘNG CỤ THỂ CỦA AI (AI ACTIONS) ---

        public MoveRandomAroundPoint MoveRandom;           // Di chuyển ngẫu nhiên quanh điểm sinh ra
        public FollowWaypoint FollowPatrolPath;            // Đi theo lộ trình Waypoint
        public MoveRandomInsideArea MoveRandomPatrolArea; // Di chuyển ngẫu nhiên trong vùng Box
        public FollowPoint MoveToPossibleTargetPosition;   // Đi tới điểm nghi ngờ
        public MoveRandomAroundPoint SearchLosedTarget;    // Tìm quanh khu vực mất dấu
        public DamageDetector DamageDetector;             // Phát hiện nguồn sát thương để quay lại nhìn
        public Attack Attack;                             // Trạng thái tấn công
        public Escape EscapeAreas;                         // Chạy trốn khỏi vùng nguy hiểm (lựu đạn/nổ)

        /// <summary>
        /// Sự kiện kích hoạt khi xác định được mục tiêu.
        /// </summary>
        public event UnityAction<GameObject> OnSetTarget;

        /// <summary>
        /// Sự kiện kích hoạt khi nghe thấy tiếng động (và AI đang không bận tấn công).
        /// </summary>
        public event UnityAction<Vector3, GameObject> OnHear;

        /// <summary>
        /// Trạng thái hiện tại của AI.
        /// </summary>
        public PatrolStates CurrentState { get; private set; }

        /// <summary>
        /// AI có đang trong trạng thái cảnh giác hay không.
        /// </summary>
        public bool IsAlert { get; private set; }

        /// <summary>
        /// Thuộc tính quản lý mục tiêu hiện tại.
        /// </summary>
        public Collider CurrentTarget
        {
            get => _target;
            set
            {
                if (_target != value)
                {
                    _target = value;
                    _targetHealth = null;
                    // Gọi sự kiện thông báo đã đổi mục tiêu
                    OnSetTarget?.Invoke(value ? value.gameObject : null);
                }
            }
        }

        /// <summary>
        /// Lấy thông tin máu của mục tiêu hiện tại.
        /// </summary>
        public JUHealth CurrentTargetHealth
        {
            get
            {
                if (!_targetHealth && _target)
                {
                    // Tìm component Health ở mục tiêu hoặc cha của mục tiêu
                    _targetHealth = CurrentTarget.GetComponent<JUHealth>();
                    if (!_targetHealth && _target.transform.parent)
                        _targetHealth = _target.transform.parent.GetComponent<JUHealth>();
                }
                return _targetHealth;
            }
        }

        /// <summary>
        /// Hàm khởi tạo: Thiết lập các giá trị mặc định cho các Action.
        /// </summary>
        public JU_AI_PatrolCharacter() : base()
        {
            General = new GeneralSettings()
            {
                LoseTargetDelay = 10,
                AlertMaxTime = 20,
                MaxSearchTargetTime = 30
            };

            PatrolRandomlyIfNotHavePath = true;

            // Khởi tạo các cảm biến và hành động
            FieldOfView = new FieldOfView();
            HearSensor = new HearSensor();
            DamageDetector = new DamageDetector();
            MoveRandom = new MoveRandomAroundPoint();
            FollowPatrolPath = new FollowWaypoint();
            MoveRandomPatrolArea = new MoveRandomInsideArea();
            SearchLosedTarget = new MoveRandomAroundPoint();
            MoveToPossibleTargetPosition = new FollowPoint();
            Attack = new Attack();
            EscapeAreas = new Escape();

            // Cấu hình hành vi di chuyển
            MoveRandom.StartRunDistance = 20;
            MoveRandom.ChangeDestinationInterval = 10;

            // Buộc AI sử dụng tư thế chiến đấu (Fire Pose) khi đi tìm mục tiêu
            SearchLosedTarget.UseFirePose = true;
            MoveToPossibleTargetPosition.UseFirePose = true;
            DamageDetector.ForceFirePose = true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            EscapeAreas.OnValidate();
        }

        protected override void Reset()
        {
            base.Reset();
            // Tự động tìm xương Đầu (Head) nếu là nhân vật Humanoid
            if (gameObject.TryGetComponent<Animator>(out var anim))
            {
                if (anim.isHuman)
                {
                    Head = anim.GetBoneTransform(HumanBodyBones.Head);
                }
            }
            Attack.Reset();
            FieldOfView.Reset();
        }

        protected override void Start()
        {
            // Lưu vị trí xuất phát và khớp nó vào NavMesh
            _spawnPosition = transform.position;
            if (JU_Ai.ClosestToNavMesh(_spawnPosition, out var spawnPointOnNavmesh))
            {
                _spawnPosition = spawnPointOnNavmesh;
            }

            base.Start();

            // Kích hoạt (Setup) tất cả cảm biến và hành động
            FieldOfView.Setup(this);
            HearSensor.Setup(this);
            DamageDetector.Setup(this);
            MoveRandom.Setup(this);
            FollowPatrolPath.Setup(this);
            MoveRandomPatrolArea.Setup(this);
            MoveToPossibleTargetPosition.Setup(this);
            SearchLosedTarget.Setup(this);
            Attack.Setup(this);
            EscapeAreas.Setup(this);

            // Đăng ký sự kiện nghe tiếng động
            HearSensor.OnHear.AddListener(OnHearSomething);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // Hủy đăng ký để tránh lỗi bộ nhớ (Memory Leak)
            HearSensor.OnHear.RemoveListener(OnHearSomething);
            // Giải phóng các Action
            DamageDetector.Unsetup();
            // ... (các Unsetup khác tương tự)
        }

        protected override void Update()
        {
            base.Update();

            // 1. Cập nhật cảm biến tầm nhìn
            FieldOfView.Update(Head);

            // 2. Nếu thấy mục tiêu mới, gán vào CurrentTarget
            if (FieldOfView.NearestColliderInView)
                CurrentTarget = FieldOfView.NearestColliderInView;

            // 3. Xử lý mất dấu mục tiêu
            if (CurrentTarget && !FieldOfView.IsOnView(CurrentTarget))
            {
                _loseTargetTimer += Time.deltaTime;
                if (_loseTargetTimer > General.LoseTargetDelay)
                {
                    // Sau khi hết thời gian chờ, lưu vị trí cuối cùng và đi tới đó kiểm tra
                    _possibleTargetPosition = CurrentTarget.transform.position;
                    CurrentState = PatrolStates.MovingToPossibleTargetPosition;
                    CurrentTarget = null;
                }
            }
            else
                _loseTargetTimer = 0;

            // 4. Nếu có mục tiêu trực diện -> Chuyển sang tấn công
            if (CurrentTarget)
                CurrentState = PatrolStates.Attacking;

            // 5. Nếu mục tiêu chết hoặc biến mất -> Quay lại tuần tra
            if ((CurrentState == PatrolStates.Attacking && !CurrentTarget) || (CurrentTargetHealth && CurrentTargetHealth.IsDead))
            {
                CurrentTarget = null;
                CurrentState = PatrolStates.Patrol;
            }

            // 6. Cập nhật trạng thái cảnh giác và quyết định hành động tiếp theo
            UpdateAlertMode();
            AIControlData control = UpdateCurrentState();

            // Gán dữ liệu điều khiển (di chuyển, nhìn, bắn) cho AI Base
            Control = control;
        }

        /// <summary>
        /// Quản lý thời gian tồn tại của trạng thái Cảnh giác (Alert).
        /// </summary>
        private void UpdateAlertMode()
        {
            if (CurrentTarget)
            {
                IsAlert = true;
                _inAlertTimeTargetLosed = 0;
                return;
            }

            if (IsAlert)
            {
                _inAlertTimeTargetLosed += Time.deltaTime;
                if (_inAlertTimeTargetLosed > General.AlertMaxTime)
                    IsAlert = false;
            }
        }

        /// <summary>
        /// Máy trạng thái (FSM) - Quyết định Action nào sẽ chạy dựa trên CurrentState.
        /// </summary>
        private AIControlData UpdateCurrentState()
        {
            AIControlData control = new AIControlData();

            switch (CurrentState)
            {
                case PatrolStates.Patrol:
                    UpdateFollowPathState(ref control);
                    break;
                case PatrolStates.MovingToPossibleTargetPosition:
                    UpdateMoveToPossibleTargetPositionState(ref control);
                    break;
                case PatrolStates.SearchingForLostTarget:
                    UpdateSearchForLosedTarget(ref control);
                    break;
                case PatrolStates.Attacking:
                    UpdateAttackState(ref control);
                    break;
            }

            // Kiểm tra né tránh nguy hiểm (Lựu đạn...) - Ưu tiên cao hơn các trạng thái khác
            EscapeAreas.Update(ref control);
            if (EscapeAreas.IsTryingEscape)
                _currentAction = EscapeAreas;

            // Tự động thoát trạng thái tìm kiếm nếu quá lâu không thấy gì
            if (CurrentState == PatrolStates.SearchingForLostTarget)
            {
                _searchTargetTimer += Time.deltaTime;
                if (_searchTargetTimer > General.MaxSearchTargetTime)
                {
                    _searchTargetTimer = 0;
                    CurrentState = PatrolStates.Patrol;
                }
            }
            else
                _searchTargetTimer = 0;

            // Nếu không phải đang tấn công, vẫn kiểm tra xem có bị trúng đạn từ phía sau không
            if (CurrentState != PatrolStates.Attacking)
                DamageDetector.Update(ref control);

            return control;
        }

        // --- CÁC HÀM CẬP NHẬT CHI TIẾT TỪNG TRẠNG THÁI ---

        private void UpdateFollowPathState(ref AIControlData control)
        {
            // Ưu tiên đi theo Waypoint -> Vùng Box -> Đi ngẫu nhiên
            if (PatrolPath)
            {
                _currentAction = FollowPatrolPath;
                FollowPatrolPath.Update(PatrolPath, ref control);
            }
            else if (PatrolArea)
            {
                _currentAction = MoveRandomPatrolArea;
                MoveRandomPatrolArea.Update(PatrolArea, ref control);
            }
            else if (PatrolRandomlyIfNotHavePath)
            {
                _currentAction = MoveRandom;
                MoveRandom.Update(_spawnPosition, ref control);
            }
        }

        private void UpdateMoveToPossibleTargetPositionState(ref AIControlData control)
        {
            _currentAction = MoveToPossibleTargetPosition;
            MoveToPossibleTargetPosition.Update(_possibleTargetPosition, ref control);

            // Khi đã đến điểm nghi ngờ: Nếu đang cảnh giác thì đi lùng sục, không thì quay lại tuần tra
            if (MoveToPossibleTargetPosition.IsStopedClosestToDestination)
                CurrentState = IsAlert ? PatrolStates.SearchingForLostTarget : PatrolStates.Patrol;
        }

        private void UpdateAttackState(ref AIControlData control)
        {
            if (CurrentTarget)
            {
                _currentAction = Attack;
                Attack.Update(CurrentTarget.gameObject, ref control);
            }
        }

        private void UpdateSearchForLosedTarget(ref AIControlData control)
        {
            _currentAction = SearchLosedTarget;
            SearchLosedTarget.Update(_possibleTargetPosition, ref control);
        }

        /// <summary>
        /// Xử lý khi nghe thấy âm thanh từ HearSensor.
        /// </summary>
        private void OnHearSomething(Vector3 position, GameObject source)
        {
            // Đang bận đánh nhau thì không quan tâm tiếng động khác
            if (CurrentState == PatrolStates.Attacking)
                return;

            _possibleTargetPosition = position;

            // Nếu không phải đang tìm mục tiêu, chuyển sang đi kiểm tra tiếng động
            if (CurrentState != PatrolStates.SearchingForLostTarget)
                CurrentState = PatrolStates.MovingToPossibleTargetPosition;

            OnHear?.Invoke(position, source);
        }
    }
}