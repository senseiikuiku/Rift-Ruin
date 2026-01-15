//using System.Collections.Generic;
//using JUTPS;
//using UnityEngine;
//using UnityEngine.InputSystem.Utilities;

//namespace JU.CharacterSystem.AI
//{
//    /// <summary>
//    /// Cảm biến tầm nhìn (Field of View - FOV) cho các nhân vật AI.
//    /// </summary>
//    [System.Serializable]
//    public class FieldOfView
//    {
//        private JUCharacterAIBase _ai; // Tham chiếu đến lớp AI gốc

//        private float _scanTimer;      // Bộ đếm thời gian để kiểm tra quét mục tiêu
//        private Transform _pivot;     // Điểm xoay (thường là vị trí mắt/đầu của AI)
//        private Collider[] _detections; // Mảng lưu trữ các vật thể phát hiện được

//        /// <summary>
//        /// Nếu true, hệ thống tầm nhìn sẽ hoạt động.
//        /// </summary>
//        public bool Enabled;

//        /// <summary>
//        /// Khoảng cách nhìn tối đa của AI.
//        /// </summary>
//        public float Distance;

//        /// <summary>
//        /// Góc nhìn tối đa của AI (độ).
//        /// </summary>
//        [Range(1, 180)]
//        public float Angle;

//        /// <summary>
//        /// Tốc độ làm mới tầm nhìn (tính bằng giây).
//        /// </summary>
//        [Min(0.1f), Space]
//        public float RefreshRate;

//        /// <summary>
//        /// Số lượng đối tượng tối đa có thể phát hiện mỗi lần cập nhật (chưa qua bộ lọc vật cản).
//        /// Nếu chỉ tìm Player, hãy đặt là 1. Nếu tìm nhiều nhân vật, đặt từ 10 trở lên.
//        /// </summary>
//        [Min(1)]
//        public int MaxDetections;

//        /// <summary>
//        /// Các Layer (lớp) được coi là mục tiêu.
//        /// </summary>
//        public LayerMask TargetsLayer;

//        /// <summary>
//        /// Các Layer vật cản như tường, tòa nhà.
//        /// </summary>
//        public LayerMask ObstaclesLayer;

//        /// <summary>
//        /// Danh sách các Tag dùng để lọc mục tiêu.
//        /// </summary>
//        public string[] TargetTags;

//        /// <summary>
//        /// Collider gần nhất hiện đang nằm trong tầm nhìn.
//        /// </summary>
//        public Collider NearestColliderInView { get; private set; }

//        /// <summary>
//        /// Vị trí cuối cùng nhìn thấy mục tiêu.
//        /// </summary>
//        public Vector3 LastColliderViewedPosition { get; private set; }

//        /// <summary>
//        /// Tất cả các Collider tìm thấy trong tầm nhìn.
//        /// </summary>
//        public ReadOnlyArray<Collider> CollidersInView
//        {
//            get => new ReadOnlyArray<Collider>(_detections);
//        }

//        /// <summary>
//        /// Trả về true nếu đang nhìn thấy ít nhất một đối tượng.
//        /// </summary>
//        public bool HasCollidersInView
//        {
//            get => NearestColliderInView != null;
//        }

//        /// <summary>
//        /// Trả về vị trí tâm của tầm nhìn. Nếu có _pivot (mắt) thì lấy _pivot, nếu không lấy tâm của AI.
//        /// </summary>
//        public Vector3 Center
//        {
//            get => _pivot ? _pivot.position : _ai.Center;
//        }

//        /// <summary>
//        /// Hướng nhìn phía trước của AI.
//        /// </summary>
//        public Vector3 Forward
//        {
//            get
//            {
//                // Lấy hướng thẳng đứng của nhân vật để tính toán mặt phẳng nằm ngang
//                Vector3 charUp = _ai.transform.up;

//                if (_pivot)
//                {
//                    // Chiếu hướng nhìn của Pivot lên mặt phẳng di chuyển để tránh sai số khi AI ngước lên/xuống
//                    return Vector3.ProjectOnPlane(_pivot.forward, charUp).normalized;
//                }

//#if UNITY_EDITOR
//                Debug.Assert(_ai, $"{nameof(JUCharacterAIBase)} chưa được thêm vào gameObject.");
//                Debug.Assert(_ai.Character, $"{nameof(JUCharacterController)} chưa được thêm vào {_ai.name}.");

//                if (!Application.isPlaying)
//                    return _ai.transform.forward;
//#endif

//                // Tính toán hướng nhìn dựa trên điểm LookAt của Character Controller
//                Vector3 lookAtDirection = _ai.Character.LookAtPosition - _ai.transform.position;
//                if (lookAtDirection.magnitude > 0.1f)
//                    return Vector3.ProjectOnPlane(lookAtDirection, charUp).normalized;

//                return _ai.transform.forward;
//            }
//        }

//        /// <summary>
//        /// Hàm khởi tạo mặc định cho Field of View.
//        /// </summary>
//        public FieldOfView()
//        {
//            Enabled = true;
//            RefreshRate = 0.5f;
//            MaxDetections = 10;
//            Distance = 20;
//            Angle = 90;
//            ObstaclesLayer = 0;
//        }

//        /// <summary>
//        /// Reset các thuộc tính về mặc định (thường dùng trong Editor).
//        /// </summary>
//        public void Reset()
//        {
//#if UNITY_EDITOR
//            // Tự động tìm và gán các Layer phổ biến cho vật cản và mục tiêu
//            LayerMask[] defaultObstacleLayers = {
//                LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Wall"),
//                LayerMask.NameToLayer("Walls"), LayerMask.NameToLayer("Obstacle"),
//                LayerMask.NameToLayer("Obstacles"), LayerMask.NameToLayer("Terrain")
//            };

//            LayerMask[] defaultTargetLayers = {
//                LayerMask.NameToLayer("Character"), LayerMask.NameToLayer("Characters"),
//                LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Players"),
//                LayerMask.NameToLayer("Vehicle"), LayerMask.NameToLayer("Vehicles")
//            };

//            string[] defaultTargetTags = {
//                "Player", "Players", "Character", "Characters", "Vehicle", "Vehicles",
//                "Distractable", "Distractables"
//            };

//            // Thiết lập Layer vật cản bằng phép Bitwise OR
//            ObstaclesLayer = 0;
//            for (int i = 0; i < defaultObstacleLayers.Length; i++)
//            {
//                if (defaultObstacleLayers[i] != -1)
//                    ObstaclesLayer |= 1 << defaultObstacleLayers[i];
//            }

//            // Thiết lập Layer mục tiêu
//            TargetsLayer = 0;
//            for (int i = 0; i < defaultTargetLayers.Length; i++)
//            {
//                if (defaultTargetLayers[i] != -1)
//                    TargetsLayer |= 1 << defaultTargetLayers[i];
//            }

//            // Lọc các Tag thực sự tồn tại trong Project để gán vào danh sách
//            List<string> existentDefaultTags = new List<string>();
//            foreach (var tag in UnityEditorInternal.InternalEditorUtility.tags)
//            {
//                for (int i = 0; i < defaultTargetTags.Length; i++)
//                {
//                    if (tag.Equals(defaultTargetTags[i]))
//                        existentDefaultTags.Add(tag);
//                }
//            }
//            TargetTags = existentDefaultTags.ToArray();
//#endif
//        }

//        /// <summary>
//        /// Thiết lập hệ thống FOV với tham chiếu AI.
//        /// </summary>
//        public void Setup(JUCharacterAIBase ai)
//        {
//            _ai = ai;
//            // Khởi tạo mảng detections để tránh cấp phát bộ nhớ liên tục (tối ưu Garbage Collector)
//            _detections = new Collider[MaxDetections + 1];
//        }

//        /// <summary>
//        /// Cập nhật FOV. Thường gọi trong Update của AI.
//        /// </summary>
//        /// <param name="pivot">Điểm gốc của FOV, ví dụ: Transform đầu của nhân vật.</param>
//        public void Update(Transform pivot)
//        {
//            if (!Enabled)
//            {
//                NearestColliderInView = null;
//                return;
//            }

//            _pivot = pivot;

//            // Chỉ quét mục tiêu dựa trên RefreshRate để tối ưu hiệu năng
//            _scanTimer += Time.deltaTime;
//            if (_scanTimer > RefreshRate)
//            {
//                _scanTimer = 0;
//                Scan();
//            }
//        }

//        /// <summary>
//        /// Thực hiện quét và lọc các mục tiêu trong tầm nhìn.
//        /// </summary>
//        private void Scan()
//        {
//            NearestColliderInView = null;

//            Vector3 center = Center;
//            Vector3 forward = Forward;

//            // Tìm tất cả Collider trong bán kính Distance thuộc TargetsLayer
//            // Sử dụng NonAlloc để không tạo ra rác (Garbage) bộ nhớ
//            int foundCount = Physics.OverlapSphereNonAlloc(center, Distance, _detections, TargetsLayer);

//            if (foundCount < 1) return;

//            for (int i = 0; i < foundCount; i++)
//            {
//                Collider collider = _detections[i];

//                // Bỏ qua nếu Collider chính là bản thân AI này
//                if (collider.gameObject == _ai.gameObject)
//                {
//                    _detections[i] = null;
//                    continue;
//                }

//                Vector3 colliderCenter = collider.bounds.center;

//                // Kiểm tra Góc nhìn: Nếu góc giữa hướng forward và mục tiêu lớn hơn Angle thì bỏ qua
//                if (Vector3.Angle(forward, colliderCenter - center) > Angle)
//                {
//                    _detections[i] = null;
//                    continue;
//                }

//                // Kiểm tra Tag: Mục tiêu phải có Tag nằm trong danh sách TargetTags
//                if (TargetTags.Length > 0)
//                {
//                    bool hasTag = false;
//                    for (int x = 0; x < TargetTags.Length; x++)
//                    {
//                        if (collider.CompareTag(TargetTags[x]))
//                        {
//                            hasTag = true;
//                            break;
//                        }
//                    }

//                    if (!hasTag)
//                    {
//                        _detections[i] = null;
//                        continue;
//                    }
//                }

//                // Kiểm tra Vật cản: Dùng Linecast bắn một tia từ mắt AI tới tâm mục tiêu
//                if (Physics.Linecast(center, colliderCenter, out RaycastHit hit, ObstaclesLayer, QueryTriggerInteraction.Ignore))
//                {
//                    // Nếu tia va chạm vào thứ gì đó trước khi tới được mục tiêu, nghĩa là bị che khuất
//                    if (hit.collider != collider)
//                    {
//                        _detections[i] = null;
//                        continue;
//                    }
//                }
//            }

//            // Sau khi lọc, tìm đối tượng còn sống ở gần AI nhất
//            float minDistance = float.MaxValue;
//            for (int i = 0; i < foundCount; i++)
//            {
//                if (!_detections[i]) continue;

//                Vector3 colliderCenter = _detections[i].bounds.center;
//                float distance = Vector3.Distance(center, colliderCenter);
//                if (distance < minDistance)
//                {
//                    // Bỏ qua nếu mục tiêu đã chết (kiểm tra component JUHealth)
//                    if (_detections[i].TryGetComponent(out JUHealth health))
//                    {
//                        if (health.IsDead) continue;
//                    }

//                    minDistance = distance;
//                    NearestColliderInView = _detections[i];
//                    LastColliderViewedPosition = colliderCenter;
//                }
//            }
//        }

//        /// <summary>
//        /// Trả về true nếu một Transform nằm trong tầm nhìn.
//        /// </summary>
//        /// <param name="otherTransform"></param>
//        /// <returns></returns>
//        public bool IsOnView(Transform otherTransform)
//        {
//            if (!otherTransform)
//                return false;

//            if (TargetTags.Length > 0)
//            {
//                bool hasTag = false;
//                for (int x = 0; x < TargetTags.Length; x++)
//                {
//                    if (otherTransform.CompareTag(TargetTags[x]))
//                    {
//                        hasTag = true;
//                        break;
//                    }
//                }

//                if (!hasTag)
//                    return false;
//            }

//            Vector3 center = Center;
//            Vector3 otherTransformPosition = otherTransform.position;

//            if (Vector3.Angle(Forward, otherTransformPosition - center) > Angle)
//                return false;

//            if (Physics.Linecast(center, otherTransformPosition, out RaycastHit hit, ObstaclesLayer, QueryTriggerInteraction.Ignore))
//            {
//                if (hit.collider != otherTransform)
//                    return false;
//            }

//            return true;
//        }

//        /// <summary>
//        /// Trả về true nếu một Collider nằm trong tầm nhìn.
//        /// </summary>
//        /// <param name="otherCollider"></param>
//        /// <returns></returns>
//        public bool IsOnView(Collider otherCollider)
//        {
//            if (!otherCollider)
//                return false;

//            if (TargetTags.Length > 0)
//            {
//                bool hasTag = false;
//                for (int x = 0; x < TargetTags.Length; x++)
//                {
//                    if (otherCollider.CompareTag(TargetTags[x]))
//                    {
//                        hasTag = true;
//                        break;
//                    }
//                }

//                if (!hasTag)
//                    return false;
//            }

//            Vector3 center = Center;
//            Vector3 otherColliderPosition = otherCollider.bounds.center;

//            if (Vector3.Angle(Forward, otherColliderPosition - center) > Angle)
//                return false;

//            if (Physics.Linecast(center, otherColliderPosition, out RaycastHit hit, ObstaclesLayer, QueryTriggerInteraction.Ignore))
//            {
//                if (hit.collider != otherCollider)
//                    return false;
//            }

//            return true;
//        }

//        /// <summary>
//        /// Tra về true nếu một Bounds nằm trong tầm nhìn.
//        /// </summary>
//        /// <param name="bounds"></param>
//        /// <returns></returns>
//        public bool IsOnView(Bounds bounds)
//        {
//            return IsOnView(bounds.center);
//        }

//        /// <summary>
//        /// Trả về true nếu một điểm nằm trong tầm nhìn.
//        /// </summary>
//        /// <param name="point"></param>
//        /// <returns></returns>
//        public bool IsOnView(Vector3 point)
//        {
//            return IsOnView(Center, _ai.transform.forward, point);
//        }

//        /// <summary>
//        /// Trả về true nếu một điểm nằm trong tầm nhìn với tham số tùy chỉnh.
//        /// </summary>
//        /// <param name="center">The field of view center.</param>
//        /// <param name="forward">The field of view direction</param>
//        /// <param name="point">The point to check.</param>
//        /// <returns></returns>
//        public bool IsOnView(Vector3 center, Vector3 forward, Vector3 point)
//        {
//            if (Vector3.Angle(forward, point - center) > Angle)
//                return false;


//            if (Physics.Linecast(center, point, ObstaclesLayer, QueryTriggerInteraction.Ignore))
//                return false;

//            return true;
//        }

//        /// <summary>
//        /// Vẽ Gizmos hiển thị tầm nhìn trong Editor.
//        /// </summary>
//        public void DrawGizmos()
//        {
//#if UNITY_EDITOR
//            if (!_ai)
//                return;

//            Vector3 position = Center;
//            Vector3 forward = Forward;
//            //Vector3 up = Quaternion.LookRotation(forward) * Vector3.up;

//            // SỬA THÀNH:
//            Vector3 up = _ai.transform.up; // Vòng tròn xanh sẽ luôn nằm song song với mặt đất Zombie đang đứng

//            UnityEditor.Handles.color = Color.green;
//            UnityEditor.Handles.DrawWireDisc(position, up, Distance);

//            UnityEditor.Handles.color = Color.red;
//            UnityEditor.Handles.DrawWireArc(position, up, forward, Angle, Distance - 0.1f);
//            UnityEditor.Handles.DrawWireArc(position, up, forward, -Angle, Distance - 0.1f);

//            UnityEditor.Handles.color = new Color(1, 0, 0, 0.1f);
//            UnityEditor.Handles.DrawSolidArc(position, up, forward, Angle, Distance - 0.2f);
//            UnityEditor.Handles.DrawSolidArc(position, up, forward, -Angle, Distance - 0.2f);
//#endif
//        }
//    }
//}

using System.Collections.Generic;
using JUTPS;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace JU.CharacterSystem.AI
{
    /// <summary>
    /// Cảm biến tầm nhìn (Field of View) dành cho các nhân vật AI.
    /// </summary>
    [System.Serializable]
    public class FieldOfView
    {
        private JUCharacterAIBase _ai;

        private float _scanTimer;
        private Transform _pivot;
        private Collider[] _detections;

        /// <summary>
        /// Nếu true, tầm nhìn có thể tìm thấy các vật thể có Collider.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Khoảng cách nhìn tối đa.
        /// </summary>
        public float Distance;

        /// <summary>
        /// Góc nhìn tối đa.
        /// </summary>
        [Range(1, 180)]
        public float Angle;

        /// <summary>
        /// Tốc độ làm mới tầm nhìn (quét lại sau mỗi x giây).
        /// </summary>
        [Min(0.1f), Space]
        public float RefreshRate;

        /// <summary>
        /// Số lượng vật thể thô tối đa có thể phát hiện mỗi lần cập nhật (chưa qua bộ lọc vật cản hay thẻ tag).
        /// Nếu AI chỉ cần tìm 1 đối tượng (như người chơi) hãy đặt là 1. Nếu cần tìm nhiều nhân vật, hãy đặt 10 trở lên.
        /// </summary>
        [Min(1)]
        public int MaxDetections;

        /// <summary>
        /// Layer của các mục tiêu cần tìm.
        /// </summary>
        public LayerMask TargetsLayer;

        /// <summary>
        /// Layer của các vật cản, như tường hoặc tòa nhà.
        /// </summary>
        public LayerMask ObstaclesLayer;

        /// <summary>
        /// Dùng để lọc tìm kiếm bằng các thẻ (Tags) của GameObject.
        /// </summary>
        public string[] TargetTags;

        /// <summary>
        /// Collider gần nhất được tìm thấy trong tầm nhìn.
        /// </summary>
        public Collider NearestColliderInView { get; private set; }

        /// <summary>
        /// Vị trí cuối cùng của vật thể được nhìn thấy.
        /// </summary>
        public Vector3 LastColliderViewedPosition { get; private set; }

        /// <summary>
        /// Tất cả các Collider được tìm thấy trong tầm nhìn.
        /// </summary>
        public ReadOnlyArray<Collider> CollidersInView
        {
            get => new ReadOnlyArray<Collider>(_detections);
        }

        /// <summary>
        /// Trả về true nếu đang nhìn thấy ít nhất một vật thể.
        /// </summary>
        public bool HasCollidersInView
        {
            get => NearestColliderInView != null;
        }

        /// <summary>
        /// Trả về vị trí trung tâm của tầm nhìn, lấy từ <see cref="_pivot"/> nếu có gán. 
        /// Nếu không, trả về tâm của AI (bounds center).
        /// </summary>
        public Vector3 Center
        {
            get => _pivot ? _pivot.position : _ai.Center;
        }

        /// <summary>
        /// Hướng nhìn phía trước của tầm nhìn.
        /// </summary>
        public Vector3 Forward
        {
            get
            {
                // Nếu có điểm xoay (pivot - ví dụ như xương đầu), lấy hướng phía trước của nó trên mặt phẳng ngang
                if (_pivot)
                    return Vector3.ProjectOnPlane(_pivot.forward, Vector3.up);

#if UNITY_EDITOR
                Debug.Assert(_ai, $"{nameof(JUCharacterAIBase)} chưa được thêm vào gameObject.");
                Debug.Assert(_ai.Character, $"{nameof(JUCharacterController)} chưa được thêm vào gameObject {_ai.name}.");

                if (!Application.isPlaying)
                    return _ai.transform.forward;
#endif
                // Tính toán hướng nhìn dựa trên vị trí LookAt của nhân vật
                Vector3 lookAtDirection = _ai.Character.LookAtPosition - _ai.transform.position;
                if (lookAtDirection.magnitude > 0.1f)
                    return Vector3.ProjectOnPlane(lookAtDirection, Vector3.up).normalized;

                return _ai.transform.forward;
            }
        }

        /// <summary>
        /// Khởi tạo một tầm nhìn mới với các giá trị mặc định.
        /// </summary>
        public FieldOfView()
        {
            Enabled = true;
            RefreshRate = 0.5f;
            MaxDetections = 10;

            Distance = 20;
            Angle = 90;

            MaxDetections = 10;
            ObstaclesLayer = 0;
        }

        /// <summary>
        /// Được gọi bởi editor để thiết lập lại các thuộc tính mặc định (thường dùng khi nhấn Reset trong Inspector).
        /// </summary>
        public void Reset()
        {
#if UNITY_EDITOR
            // Định nghĩa các Layer mặc định được coi là vật cản
            LayerMask[] defaultObstacleLayers = {
                LayerMask.NameToLayer("Default"),
                LayerMask.NameToLayer("Wall"),
                LayerMask.NameToLayer("Walls"),
                LayerMask.NameToLayer("Obstacle"),
                LayerMask.NameToLayer("Obstacles"),
                LayerMask.NameToLayer("Terrain")
            };

            // Định nghĩa các Layer mặc định được coi là mục tiêu
            LayerMask[] defaultTargetLayers = {
                LayerMask.NameToLayer("Character"),
                LayerMask.NameToLayer("Characters"),
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Players"),
                LayerMask.NameToLayer("Vehicle"),
                LayerMask.NameToLayer("Vehicles")
            };

            // Định nghĩa các Tag mặc định cần tìm kiếm
            string[] defaultTargetTags = {
                "Player",
                "Players",
                "Character",
                "Characters",
                "Vehicle",
                "Vehicles",
                "Distractable",
                "Distractables"
            };

            // Thiết lập layer vật cản (Bitwise OR)
            ObstaclesLayer = 0;
            for (int i = 0; i < defaultObstacleLayers.Length; i++)
            {
                if (defaultObstacleLayers[i] != -1)
                    ObstaclesLayer |= 1 << defaultObstacleLayers[i];
            }

            // Thiết lập layer mục tiêu
            TargetsLayer = 0;
            for (int i = 0; i < defaultTargetLayers.Length; i++)
            {
                if (defaultTargetLayers[i] != -1)
                    TargetsLayer |= 1 << defaultTargetLayers[i];
            }

            // Lọc các Tag có tồn tại trong dự án để gán vào danh sách tìm kiếm
            List<string> existentDefaultTags = new List<string>();
            foreach (var tag in UnityEditorInternal.InternalEditorUtility.tags)
            {
                for (int i = 0; i < defaultTargetTags.Length; i++)
                {
                    if (tag.Equals(defaultTargetTags[i]))
                        existentDefaultTags.Add(tag);
                }
            }

            TargetTags = existentDefaultTags.ToArray();
#endif
        }

        /// <summary>
        /// Thiết lập tầm nhìn.
        /// </summary>
        /// <param name="ai">Nhân vật AI sẽ sở hữu tầm nhìn này.</param>
        public void Setup(JUCharacterAIBase ai)
        {
            _ai = ai;
            // Khởi tạo mảng lưu trữ các vật thể phát hiện được
            _detections = new Collider[MaxDetections + 1];
        }

        /// <summary>
        /// Cập nhật tầm nhìn theo thời gian thực.
        /// </summary>
        /// <param name="pivot">Gốc của tầm nhìn, thường là "đầu" của nhân vật.</param>
        public void Update(Transform pivot)
        {
            if (!Enabled)
            {
                NearestColliderInView = null;
                return;
            }

            _pivot = pivot;

            // Bộ đếm thời gian để quét theo RefreshRate (giảm tải hiệu năng)
            _scanTimer += Time.deltaTime;
            if (_scanTimer > RefreshRate)
            {
                _scanTimer = 0;
                Scan();
            }
        }

        /// <summary>
        /// Logic quét các vật thể trong môi trường.
        /// </summary>
        private void Scan()
        {
            NearestColliderInView = null;

            Vector3 center = Center;
            Vector3 forward = Forward;
            // Tìm tất cả Collider trong hình cầu xung quanh AI mà thuộc Target Layer
            int foundCount = Physics.OverlapSphereNonAlloc(center, Distance, _detections, TargetsLayer);

            if (foundCount < 1)
                return;

            for (int i = 0; i < foundCount; i++)
            {
                Collider collider = _detections[i];

                // Loại bỏ chính bản thân AI nếu vô tình quét trúng
                if (collider.gameObject == _ai.gameObject)
                {
                    _detections[i] = null;
                    continue;
                }

                Vector3 colliderCenter = collider.bounds.center;

                // Loại bỏ những Collider nằm ngoài góc nhìn (Angle)
                if (Vector3.Angle(forward, colliderCenter - center) > Angle)
                {
                    _detections[i] = null;
                    continue;
                }

                // Loại bỏ những Collider không có thẻ (Tag) đúng yêu cầu
                if (TargetTags.Length > 0)
                {
                    bool hasTag = false;
                    for (int x = 0; x < TargetTags.Length; x++)
                    {
                        if (collider.CompareTag(TargetTags[x]))
                        {
                            hasTag = true;
                            break;
                        }
                    }

                    if (!hasTag)
                    {
                        _detections[i] = null;
                        continue;
                    }
                }

                // Loại bỏ những Collider bị che khuất bởi vật cản (Linecast)
                if (Physics.Linecast(center, colliderCenter, out RaycastHit hit, ObstaclesLayer, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != collider)
                    {
                        _detections[i] = null;
                        continue;
                    }
                }
            }

            // Tìm vật thể gần nhất trong số những vật còn lại sau bộ lọc
            NearestColliderInView = null;

            float minDistance = float.MaxValue;
            for (int i = 0; i < foundCount; i++)
            {
                if (!_detections[i])
                    continue;

                Vector3 colliderCenter = _detections[i].bounds.center;
                float distance = Vector3.Distance(center, colliderCenter);
                if (distance < minDistance)
                {
                    // Bỏ qua nếu đối tượng đã chết (có script JUHealth và IsDead = true)
                    if (_detections[i].TryGetComponent(out JUHealth health))
                    {
                        if (health.IsDead)
                            continue;
                    }

                    minDistance = distance;
                    NearestColliderInView = _detections[i];
                    LastColliderViewedPosition = colliderCenter;
                }
            }
        }

        /// <summary>
        /// Trả về true nếu một Transform nằm trong tầm nhìn.
        /// </summary>
        public bool IsOnView(Transform otherTransform)
        {
            if (!otherTransform)
                return false;

            // Kiểm tra thẻ tag
            if (TargetTags.Length > 0)
            {
                bool hasTag = false;
                for (int x = 0; x < TargetTags.Length; x++)
                {
                    if (otherTransform.CompareTag(TargetTags[x]))
                    {
                        hasTag = true;
                        break;
                    }
                }

                if (!hasTag)
                    return false;
            }

            Vector3 center = Center;
            Vector3 otherTransformPosition = otherTransform.position;

            // Kiểm tra góc nhìn
            if (Vector3.Angle(_ai.transform.forward, otherTransformPosition - center) > Angle)
                return false;

            // Kiểm tra vật cản
            if (Physics.Linecast(center, otherTransformPosition, out RaycastHit hit, ObstaclesLayer, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != otherTransform)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Trả về true nếu một Collider nằm trong tầm nhìn.
        /// </summary>
        public bool IsOnView(Collider otherCollider)
        {
            if (!otherCollider)
                return false;

            if (TargetTags.Length > 0)
            {
                bool hasTag = false;
                for (int x = 0; x < TargetTags.Length; x++)
                {
                    if (otherCollider.CompareTag(TargetTags[x]))
                    {
                        hasTag = true;
                        break;
                    }
                }

                if (!hasTag)
                    return false;
            }

            Vector3 center = Center;
            Vector3 otherColliderPosition = otherCollider.bounds.center;

            if (Vector3.Angle(_ai.transform.forward, otherColliderPosition - center) > Angle)
                return false;

            if (Physics.Linecast(center, otherColliderPosition, out RaycastHit hit, ObstaclesLayer, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != otherCollider)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Trả về true nếu một vùng giới hạn (Bounds) nằm trong tầm nhìn dựa trên tâm của nó.
        /// </summary>
        public bool IsOnView(Bounds bounds)
        {
            return IsOnView(bounds.center);
        }

        /// <summary>
        /// Trả về true nếu một điểm Vector3 nằm trong tầm nhìn.
        /// </summary>
        public bool IsOnView(Vector3 point)
        {
            return IsOnView(Center, _ai.transform.forward, point);
        }

        /// <summary>
        /// Trả về true nếu một điểm nằm trong tầm nhìn dựa trên tâm và hướng cụ thể.
        /// </summary>
        public bool IsOnView(Vector3 center, Vector3 forward, Vector3 point)
        {
            if (Vector3.Angle(forward, point - center) > Angle)
                return false;

            if (Physics.Linecast(center, point, ObstaclesLayer, QueryTriggerInteraction.Ignore))
                return false;

            return true;
        }

        /// <summary>
        /// Vẽ các đường hỗ trợ (Gizmos) trong Editor để hiển thị vùng tầm nhìn.
        /// </summary>
        public void DrawGizmos()
        {
#if UNITY_EDITOR
            if (!_ai)
                return;

            Vector3 position = Center;
            Vector3 forward = Forward;
            Vector3 up = Quaternion.LookRotation(forward) * Vector3.up;

            // Vẽ vòng tròn thể hiện khoảng cách nhìn tối đa
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(position, up, Distance);

            // Vẽ cung tròn thể hiện giới hạn góc nhìn
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireArc(position, up, forward, Angle, Distance - 0.1f);
            UnityEditor.Handles.DrawWireArc(position, up, forward, -Angle, Distance - 0.1f);

            // Vẽ vùng diện tích tầm nhìn với màu đỏ nhạt
            UnityEditor.Handles.color = new Color(1, 0, 0, 0.1f);
            UnityEditor.Handles.DrawSolidArc(position, up, forward, Angle, Distance - 0.2f);
            UnityEditor.Handles.DrawSolidArc(position, up, forward, -Angle, Distance - 0.2f);
#endif
        }
    }
}