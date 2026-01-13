using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

namespace JUTPS.FX
{
    /// <summary>
    /// Tạo âm thanh bước chân hoặc hiệu ứng (FX) khi nhân vật di chuyển.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("JU TPS/FX/Footstep")]
    public class JUFootstep : MonoBehaviour
    {
        // Khoảng thời gian (giây) giữa mỗi lần kiểm tra khoảng cách để tối ưu hóa hiệu suất
        private const float CHECK_FOOTSTEP_DISTANCE_INTERVAL = 2f;

        // Sử dụng biến static để lưu trữ camera dùng chung cho tất cả các nhân vật,
        // tránh việc gọi hàm tìm camera (Camera.main) quá nhiều lần gây tốn hiệu năng.
        private static Camera _mainCamera;

        private float _checkFootstepActiveTimer;

        private bool _leftFootGrounded;  // Trạng thái chân trái đang chạm đất
        private bool _rightFootGrounded; // Trạng thái chân phải đang chạm đất

        private float _checkLeftFootTimer;
        private float _checkRightFootTimer;

        /// <summary>
        /// Nguồn âm thanh (Audio Source) sẽ phát ra tiếng bước chân.
        /// </summary>
        [Header("FX Settings")]
        public AudioSource AudioSource;

        /// <summary>
        /// Danh sách tất cả các hiệu ứng âm thanh bước chân tương ứng với từng bề mặt.
        /// </summary>
        public SurfaceAudiosWithFX[] FootstepAudioClips;

        /// <summary>
        /// Đảo ngược trục X của decal bước chân (dấu chân)?
        /// </summary>
        public bool InvertX;

        /// <summary>
        /// Thời gian tối thiểu giữa các lần phát âm thanh để tránh việc tiếng bước chân bị chồng chéo quá nhanh.
        /// </summary>
        [Range(0, 1)]
        public float MinTimeToPlayAudio;

        /// <summary>
        /// Lớp (Layer) của mặt đất để kiểm tra va chạm.
        /// </summary>
        [Header("Ground Check")]
        public LayerMask GroundLayers;

        /// <summary>
        /// Khoảng cách tối đa để kiểm tra xem bàn chân có đang chạm đất hay không.
        /// </summary>
        [Range(0, 1)]
        public float CheckDistance;

        /// <summary>
        /// Độ lệch vị trí kiểm tra theo trục 'Y' so với vị trí bàn chân.
        /// </summary>
        [Header("Ground Check Position Offset")]
        [Range(-0.2f, 0.2f)]
        public float UpOffset;

        /// <summary>
        /// Độ lệch vị trí kiểm tra theo trục 'Z' so với vị trí bàn chân.
        /// </summary>
        [Range(-0.2f, 0.2f)]
        public float ForwardOffset;

        /// <summary>
        /// Transform của xương bàn chân trái.
        /// </summary>
        [Space]
        public Transform LeftFoot;

        /// <summary>
        /// Transform của xương bàn chân phải.
        /// </summary>
        public Transform RightFoot;

        /// <summary>
        /// Khoảng cách tối đa mà tiếng bước chân có thể phát ra dựa trên vị trí của Camera chính.
        /// </summary>
        public float MaxFootstepDistance;

        /// <summary>
        /// Sự kiện được gọi khi chân trái chạm đất.
        /// </summary>
        public UnityEvent<RaycastHit> OnLeftFootHit;

        /// <summary>
        /// Sự kiện được gọi khi chân phải chạm đất.
        /// </summary>
        public UnityEvent<RaycastHit> OnRightFootHit;

        /// <summary>
        /// Trả về true nếu nhân vật đang ở gần Camera hơn khoảng cách MaxFootstepDistance.
        /// Nếu false, hệ thống bước chân sẽ ngừng hoạt động để tối ưu hóa các nhân vật ở xa.
        /// </summary>
        public bool IsFootsepActing { get; private set; }

        /// <summary>
        /// Component Animator được hệ thống bước chân sử dụng.
        /// </summary>
        public Animator Animator { get; private set; }

        /// <summary>
        /// Vị trí điểm kiểm tra (checker) của chân trái.
        /// </summary>
        public Vector3 LeftFootCheckerPosition
        {
            get => LeftFoot ? GetFootCheckerPosition(LeftFoot) : Vector3.zero;
        }

        /// <summary>
        /// Vị trí điểm kiểm tra (checker) của chân phải.
        /// </summary>
        public Vector3 RightFootCheckerPosition
        {
            get => RightFoot ? GetFootCheckerPosition(RightFoot) : Vector3.zero;
        }

        /// <summary>
        /// Khởi tạo các giá trị mặc định cho component.
        /// </summary>
        public JUFootstep()
        {
            MinTimeToPlayAudio = 0.3f;
            CheckDistance = 0.2f;

            ForwardOffset = 0.07f;
            UpOffset = -0.07f;

            MaxFootstepDistance = 20;
        }

        private void Start()
        {
            // Tự động lấy AudioSource nếu chưa gán
            if (!AudioSource)
                AudioSource = GetComponent<AudioSource>();

            Animator = GetComponent<Animator>();
            if (Animator)
            {
                // Tự động tìm xương chân trái và chân phải từ hình người (Humanoid)
                if (!LeftFoot) LeftFoot = Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                if (!RightFoot) RightFoot = Animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }

            // Mặc định sử dụng layer Default nếu chưa thiết lập GroundLayers
            if (GroundLayers.value == 0)
                GroundLayers = LayerMask.GetMask("Default");
        }

        private void Update()
        {
            if (!LeftFoot || !RightFoot)
                return;

            // Kiểm tra xem nhân vật có đủ gần camera để xử lý bước chân không (Tối ưu performance)
            UpdateFootstepActiveByDistance();

            if (!IsFootsepActing)
                return;

            // XỬ LÝ CHÂN TRÁI
            if (_checkLeftFootTimer < MinTimeToPlayAudio)
                _checkLeftFootTimer += Time.deltaTime;
            else
            {
                bool hasLeftGroundHit = GetFootHitInfo(LeftFoot, out RaycastHit leftFootHit);

                // Nếu chân vừa chạm đất trong khung hình này
                if (hasLeftGroundHit && !_leftFootGrounded)
                {
                    DoFootstep(LeftFoot, leftFootHit);
                    _checkLeftFootTimer = 0;
                    _leftFootGrounded = true;

                    OnLeftFootHit.Invoke(leftFootHit);
                }

                if (!hasLeftGroundHit)
                    _leftFootGrounded = false;
            }

            // XỬ LÝ CHÂN PHẢI
            if (_checkRightFootTimer < MinTimeToPlayAudio)
                _checkRightFootTimer += Time.deltaTime;
            else
            {
                bool hasRightGroundHit = GetFootHitInfo(RightFoot, out RaycastHit rightFootHit);

                if (hasRightGroundHit && !_rightFootGrounded)
                {
                    DoFootstep(RightFoot, rightFootHit);
                    _checkRightFootTimer = 0;
                    _rightFootGrounded = true;

                    OnRightFootHit.Invoke(rightFootHit);
                }

                if (!hasRightGroundHit)
                    _rightFootGrounded = false;
            }
        }

        // Bắn Raycast từ bàn chân xuống đất để lấy thông tin bề mặt
        private bool GetFootHitInfo(Transform foot, out RaycastHit hit)
        {
            Vector3 footPosition = GetFootCheckerPosition(foot);
            return Physics.Raycast(footPosition, -transform.up, out hit, CheckDistance, GroundLayers);
        }

        // Tính toán vị trí điểm kiểm tra dựa trên các chỉ số Offset (bù trừ)
        private Vector3 GetFootCheckerPosition(Transform foot)
        {
            return foot.position + transform.forward * ForwardOffset + transform.up * UpOffset;
        }

        // Thực hiện phát âm thanh và tạo Decal dấu chân
        private void DoFootstep(Transform foot, RaycastHit groundHit)
        {
            // Phát âm thanh ngẫu nhiên và tạo decal dựa trên Tag của bề mặt va chạm
            GameObject footstepDecal = SurfaceAudiosWithFX.Play(AudioSource, FootstepAudioClips, groundHit.point, Quaternion.identity, null, groundHit.collider.tag);

            if (!footstepDecal)
                return;

            Transform decalTransform = footstepDecal.transform;

            // Căn chỉnh decal nằm phẳng trên mặt đất dựa trên Normal (véc-tơ pháp tuyến) của điểm va chạm
            decalTransform.rotation = Quaternion.LookRotation(groundHit.normal) * Quaternion.Euler(90, 0, 0);

            // Xoay decal theo hướng di chuyển của nhân vật
            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            forward /= forward.magnitude;
            decalTransform.rotation *= Quaternion.LookRotation(forward);

            // Xử lý đảo ngược dấu chân trái/phải để không bị trùng lặp hình ảnh
            if (foot == RightFoot)
            {
                Vector3 decalScale = decalTransform.localScale;
                decalScale.x *= -1;

                if (InvertX)
                    decalScale.x *= -1;

                decalTransform.localScale = decalScale;
            }
            else
            {
                Vector3 decalScale = decalTransform.localScale;
                if (InvertX)
                    decalScale.x *= -1;

                decalTransform.localScale = decalScale;
            }

            // Vẽ một đường tia màu đỏ trong Scene view để debug hướng của decal
            Debug.DrawRay(footstepDecal.transform.position, footstepDecal.transform.up * 2, Color.red, 1);
        }

        // Tối ưu hóa: Chỉ xử lý bước chân nếu nhân vật ở gần camera
        private void UpdateFootstepActiveByDistance()
        {
            if (!AudioSource)
            {
                return;
            }

            _checkFootstepActiveTimer += Time.deltaTime;
            if (_checkFootstepActiveTimer < CHECK_FOOTSTEP_DISTANCE_INTERVAL)
            {
                return;
            }

            // Kiểm tra camera chính
            if (_mainCamera && !_mainCamera.isActiveAndEnabled)
            {
                _mainCamera = null;
            }

            if (!_mainCamera)
            {
                _mainCamera = Camera.main;
            }

            if (!_mainCamera)
            {
                return;
            }

            _checkFootstepActiveTimer = 0;
            // Tính toán khoảng cách từ nhân vật tới camera
            IsFootsepActing = Vector3.Distance(transform.position, _mainCamera.transform.position) < MaxFootstepDistance;

            // Tắt AudioSource nếu ở quá xa để tiết kiệm tài nguyên
            AudioSource.enabled = IsFootsepActing;
        }

        // Vẽ các khối cầu Gizmos trong Editor để người dùng dễ dàng căn chỉnh vị trí kiểm tra bước chân
        private void OnDrawGizmos()
        {
            if (LeftFoot == null || RightFoot == null)
            {
                Animator = GetComponent<Animator>();
                if (Animator == null) return;
                LeftFoot = Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                RightFoot = Animator.GetBoneTransform(HumanBodyBones.RightFoot);
                return;
            }
            Color collisionColor = Color.green; // Màu xanh khi chân chạm đất
            collisionColor.a = 0.4f;

            Color noCollisionColor = Color.red; // Màu đỏ khi chân đang nhấc lên
            noCollisionColor.a = 0.2f;

            Gizmos.color = _leftFootGrounded ? collisionColor : noCollisionColor;
            Gizmos.DrawSphere(LeftFootCheckerPosition, CheckDistance / 2);
            Gizmos.DrawWireSphere(LeftFootCheckerPosition, CheckDistance / 2);

            Gizmos.color = _rightFootGrounded ? collisionColor : noCollisionColor;
            Gizmos.DrawSphere(RightFootCheckerPosition, CheckDistance / 2);
            Gizmos.DrawWireSphere(RightFootCheckerPosition, CheckDistance / 2);
        }

#if UNITY_EDITOR
        // Tính năng trong menu chuột phải để tự động gán các âm thanh bước chân mặc định của JUTPS
        [ContextMenu("Load Default Footstep Audios", false, 100)]
        public void LoadDefaultFootstepInInspector()
        {
            LoadDefaultFootstepAudios(this);
        }

        private static void LoadDefaultFootstepAudios(JUFootstep footsteper, string path = "Assets/Julhiecio TPS Controller/Audio/Footstep/")
        {
            if (!System.IO.Directory.Exists(path))
            {
                Debug.LogError("Không thể tải âm thanh mặc định vì đường dẫn không tồn tại.");
                return;
            }

            // Tạo các ô chứa âm thanh trống
            footsteper.FootstepAudioClips = new SurfaceAudiosWithFX[4];
            for (int i = 0; i < 4; i++)
            {
                footsteper.FootstepAudioClips[i] = new SurfaceAudiosWithFX();
                for (int x = 0; x < 4; x++)
                    footsteper.FootstepAudioClips[i].AudioClips.Add(null);
            }

            // Tải âm thanh cho từng loại bề mặt: Bê tông, Đá, Cỏ, Gạch.
            footsteper.FootstepAudioClips[0].SurfaceTag = "Untagged";
            for (int i = 0; i < 4; i++)
            {
                string audioClipPath = $"{path}Concrete/Footstep on Concrete 0{i + 1}.ogg";
                footsteper.FootstepAudioClips[0].AudioClips[i] = LoadAsset<AudioClip>(audioClipPath);
            }

            footsteper.FootstepAudioClips[1].SurfaceTag = "Stone";
            for (int i = 0; i < 4; i++)
            {
                string audioClipPath = $"{path}Stones/Footsteps-on-stone0{i + 1}.ogg";
                footsteper.FootstepAudioClips[1].AudioClips[i] = LoadAsset<AudioClip>(audioClipPath);
            }

            footsteper.FootstepAudioClips[2].SurfaceTag = "Grass";
            for (int i = 0; i < 4; i++)
            {
                string audioClipPath = $"{path}Grass/Footsteps-on-grass0{i + 1}.ogg";
                footsteper.FootstepAudioClips[2].AudioClips[i] = LoadAsset<AudioClip>(audioClipPath);
            }

            footsteper.FootstepAudioClips[3].SurfaceTag = "Tiles";
            for (int i = 0; i < 4; i++)
            {
                string audioClipPath = $"{path}Tiles/Footstep-on-tiles0{i + 1}.ogg";
                footsteper.FootstepAudioClips[3].AudioClips[i] = LoadAsset<AudioClip>(audioClipPath);
            }
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"Không thể tải asset {typeof(T).Name}: {path}");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
#endif
    }
}