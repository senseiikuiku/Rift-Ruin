using JU.CharacterSystem.AI;
using JUTPS;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Điều khiển việc phát âm thanh gầm gừ của Zombie dựa trên trạng thái AI.
/// </summary>
public class ZombieRoarController : MonoBehaviour
{
    // Cần một tham chiếu đến script AI chính của Zombie
    [Tooltip("Tham chiếu đến script JU_AI_Zombie trên cùng GameObject.")]
    public JU_AI_Zombie ZombieAI;

    // THÊM: Tham chiếu đến hệ thống máu
    [Tooltip("Tham chiếu đến script JUHealth của Zombie")]
    public JUHealth ZombieHealth;

    [Header("Audio Components")]
    [Tooltip("AudioSource sẽ phát các âm thanh gầm gừ")]
    public AudioSource RoarAudioSource;

    [Header("Arrays Audio Clip (Nguồn dữ liệu)")]
    [Tooltip("Danh sách âm thanh khi zombie chưa phát hiện")]
    public AudioClip[] AmbientRoarAudioClipList;
    [Tooltip("Danh sách âm thanh khi zombie đã phát hiện")]
    public AudioClip[] AggressiveRoarAudioClipList;

    // Các biến private chứa clip đã được chọn ngẫu nhiên 1 lần duy nhất trong Start()
    private AudioClip _ambientRoarSound;
    private AudioClip _aggressiveRoarSound;

    [Header("Timing")]
    [Tooltip("Khoảng thời gian (giây) tối thiểu giữa các lần gầm gừ môi trường.")]
    public float MinAmbientRoarInterval = 5f;

    [Tooltip("Khoảng thời gian (giây) tối đa giữa các lần gầm gừ môi trường.")]
    public float MaxAmbientRoarInterval = 15f;

    private float _nextRoarTime;
    private JU_AI_Zombie.ZombieState _previousState;

    private void Awake()
    {
        // Tự động lấy các Components nếu chưa được gán
        if (!ZombieAI)
        {
            ZombieAI = GetComponent<JU_AI_Zombie>();
        }
        if (!RoarAudioSource)
        {
            RoarAudioSource = GetComponent<AudioSource>();
        }

        // TỰ ĐỘNG LẤY JUHealth nếu chưa gán
        if (!ZombieHealth) ZombieHealth = GetComponent<JUHealth>();

        // Thiết lập thời gian gầm gừ ban đầu
        _nextRoarTime = Time.time + Random.Range(MinAmbientRoarInterval, MaxAmbientRoarInterval);
    }

    private void Start()
    {
        // Chọn Ambient Roar Clip (Chưa phát hiện)
        if (AmbientRoarAudioClipList != null && AmbientRoarAudioClipList.Length > 0)
        {
            // Gán vào biến private
            _ambientRoarSound = AmbientRoarAudioClipList[Random.Range(0, AmbientRoarAudioClipList.Length)];
        }

        // Chọn Aggressive Roar Clip (Đã phát hiện)
        if (AggressiveRoarAudioClipList != null && AggressiveRoarAudioClipList.Length > 0)
        {
            // Gán vào biến private
            _aggressiveRoarSound = AggressiveRoarAudioClipList[Random.Range(0, AggressiveRoarAudioClipList.Length)];
        }

        // Khởi tạo trạng thái ban đầu để tránh lỗi khi chuyển trạng thái lần đầu
        _previousState = ZombieAI.CurrentState;
    }

    private void Update()
    {
        if (!ZombieAI || !RoarAudioSource)
            return;

        // ĐIỀU KIỆN QUAN TRỌNG: Nếu Zombie đã chết thì dừng mọi âm thanh gầm gừ
        if (ZombieHealth.IsDead)
        {
            if (RoarAudioSource.isPlaying)
            {
                RoarAudioSource.Stop();
            }
            return; // Thoát hàm Update, không chạy logic gầm gừ bên dưới nữa
        }

        UpdateRoarSounds(ZombieAI.CurrentState);
    }

    private void UpdateRoarSounds(JU_AI_Zombie.ZombieState currentState)
    {
        // Xử lý chuyển đổi trạng thái lớn (từ yên tĩnh sang hung hãn)
        if (currentState == JU_AI_Zombie.ZombieState.Attacking)
        {
            // Trạng thái ĐÃ PHÁT HIỆN: Phát âm thanh gầm gừ hung hãn
            if (_previousState != JU_AI_Zombie.ZombieState.Attacking)
            {
                // Vừa chuyển sang trạng thái Tấn công (Aggressive)
                if (_aggressiveRoarSound)
                {
                    RoarAudioSource.Stop();
                    RoarAudioSource.clip = _aggressiveRoarSound;
                    RoarAudioSource.loop = true;
                    RoarAudioSource.Play();
                }
            }
        }
        else // Patrolling, MoveToLastTargetPosition, SearhLastTarget (Chưa phát hiện/Yên tĩnh)
        {
            // Trạng thái CHƯA PHÁT HIỆN: Phát âm thanh gầm gừ môi trường ngẫu nhiên

            // Nếu vừa thoát khỏi trạng thái Tấn công, chuyển AudioSource về chế độ không lặp
            if (_previousState == JU_AI_Zombie.ZombieState.Attacking)
            {
                RoarAudioSource.Stop();
                RoarAudioSource.loop = false;
                _nextRoarTime = Time.time + Random.Range(MinAmbientRoarInterval, MaxAmbientRoarInterval);
            }

            if (_ambientRoarSound)
            {
                if (Time.time >= _nextRoarTime && !RoarAudioSource.isPlaying)
                {
                    RoarAudioSource.PlayOneShot(_ambientRoarSound);

                    // Thiết lập thời gian cho lần gầm gừ tiếp theo
                    _nextRoarTime = Time.time + Random.Range(MinAmbientRoarInterval, MaxAmbientRoarInterval);
                }
            }
        }

        _previousState = currentState;
    }
}