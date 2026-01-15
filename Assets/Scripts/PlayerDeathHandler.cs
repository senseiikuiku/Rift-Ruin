using JU;
using JUTPS;
using JUTPS.CameraSystems;
using JUTPS.GameSettings;
using System.Collections; // Cần thêm thư viện này để dùng Coroutine
using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField]
    private JUHealth juHealth;

    public static PlayerDeathHandler Instance;

    public bool checkPlayerLive = false;// Kiểm tra player vẫn còn sống

    [Header("Lose Settings")]
    public AudioClip loseSound; // Kéo file âm thanh thua vào đây
    public JUTag sfxTag;        // Gán tag SFX hoặc UI

    // Đảm bảo âm thanh thua chỉ phát 1 lần
    private static bool _hasPlayedLoseSound = false;

    private void Awake()
    {
        // Đảm bảo chỉ có một KillManager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        _hasPlayedLoseSound = false; // Reset khi bắt đầu lại ván mới

        // Lấy component JUHealth của Player
        juHealth = GetComponent<JUHealth>();

        if (juHealth != null)
        {
            // Đăng ký phương thức HandlePlayerDeath vào sự kiện OnDeath
            juHealth.OnDeath.AddListener(HandlePlayerDeath);
        }
        else
        {
            Debug.LogError("JUHealth component không tìm thấy Player! Không thể thiết lập xử lý thua.");
        }

    }

    private void HandlePlayerDeath()
    {
        // PHÁT ÂM THANH THUA NGAY LẬP TỨC KHI CHẾT
        PlayLoseSound();

        // Bắt đầu Coroutine để xử lý Slow Motion và hiển thị UI
        StartCoroutine(ShowLoseUIAfterSlowMotion(3f)); // Đợi 3 giây (duration của Slow Motion)
    }

    private void PlayLoseSound()
    {
        // Kiểm tra nếu đã phát rồi hoặc chưa gán clip thì bỏ qua
        if (_hasPlayedLoseSound || loseSound == null) return;

        _hasPlayedLoseSound = true;

        // Phát độc lập theo âm lượng hệ thống
        float volume = JUGameSettings.GetAudioVolume(sfxTag);

        GameObject soundObj = new GameObject("LoseSoundTemp");
        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = loseSound;
        source.volume = volume;
        source.spatialBlend = 0f; // Âm thanh 2D phát toàn màn hình

        // QUAN TRỌNG: Dòng này giúp âm thanh phát bất chấp Game bị Pause
        source.ignoreListenerPause = true;

        // Nếu bạn dùng phiên bản Unity mới, có thể dùng thuộc tính này:
        source.velocityUpdateMode = AudioVelocityUpdateMode.Fixed; // Đôi khi giúp ổn định hơn khi timescale = 0

        source.Play();

        Debug.Log("Đang phát âm thanh thua với âm lượng hệ thống: " + volume);

        // Vì Game bị Pause (Time.timeScale = 0), Destroy(obj, time) sẽ không hoạt động!
        // Chúng ta phải dùng một Coroutine riêng để xóa nó theo thời gian thực.
        StartCoroutine(DestroySoundRealtime(soundObj, loseSound.length));
    }

    // Hàm bổ trợ để xóa Object khi game bị Pause
    private IEnumerator DestroySoundRealtime(GameObject obj, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (obj != null) Destroy(obj);
    }

    private IEnumerator ShowLoseUIAfterSlowMotion(float delay)
    {
        checkPlayerLive = true;
        // KÍCH HOẠT HIỆU ỨNG SLOW MOTION
        // JUSlowmotion.DoSlowMotion(0.1f, 3f);
        JUTPS.FX.JUSlowmotion.DoSlowMotion(0.1f, delay);

        // Dùng WaitForSecondsRealtime để đợi 3 giây *thực tế*, không bị ảnh hưởng bởi Time.timeScale
        yield return new WaitForSecondsRealtime(delay);

        // HIỂN THỊ UI LOSE SAU KHI SLOW MOTION KẾT THÚC

        // Kiểm tra xem UIManager có tồn tại không
        if (UIManager.Instance != null)
        {
            Debug.Log("Player has died. Displaying 'You Lose!!!' UI after slow motion.");

            // Ẩn nút "Tiếp tục" (giả sử là [0])
            if (UIManager.Instance.UIWinOrLoseButton.Length > 0 && UIManager.Instance.UIWinOrLoseButton[0] != null)
            {
                UIManager.Instance.UIWinOrLoseButton[0].SetActive(false);
            }

            // Hiện nút "Chơi lại" (giả sử là [1])
            if (UIManager.Instance.UIWinOrLoseButton.Length > 1 && UIManager.Instance.UIWinOrLoseButton[1] != null)
            {
                UIManager.Instance.UIWinOrLoseButton[1].SetActive(true);
            }

            // Đặt text là "You Lose!!!"
            UIManager.Instance.UITextWinOrLose.text = "You Lose !!!";

            // Hiện UI Win/Lose
            UIManager.Instance.IsUIWinOrLose(true);
        }
    }
}