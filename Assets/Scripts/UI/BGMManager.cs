using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    private AudioSource _audioSource;

    [System.Serializable]
    public struct SceneMusic
    {
        public string sceneName;
        public AudioClip musicClip;
    }

    [Header("Cấu hình nhạc theo Scene")]
    public SceneMusic[] sceneMusicList;

    [Header("Cấu hình âm thanh")]
    public float fadeSpeed = 1.5f; // Tốc độ nhỏ dần/to dần khi đổi nhạc
    public float maxVolume = 0.5f;

    void Awake()
    {
        // Singleton để giữ duy nhất 1 Manager qua các Scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Kiểm tra và phát nhạc ngay lập tức cho Scene đầu tiên khi mở Game
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnEnable()
    {
        // Đăng ký sự kiện: Mỗi khi một Scene mới được load xong, hàm OnSceneLoaded sẽ chạy
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Hủy đăng ký sự kiện khi đối tượng bị vô hiệu hóa
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Hàm tự động chạy mỗi khi chuyển cảnh thành công
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Tìm nhạc phù hợp với Scene hiện tại
        foreach (var item in sceneMusicList)
        {
            // Nếu tìm thấy Scene phù hợp
            if (item.sceneName == scene.name)
            {
                StopAllCoroutines(); // Dừng tất cả Coroutine đang chạy (nếu có)
                StartCoroutine(FadeToNewMusic(item.musicClip)); // Bắt đầu Coroutine chuyển nhạc
                return;
            }
        }
    }

    // Coroutine giúp chuyển nhạc mượt mà (Fade In - Fade Out)
    private IEnumerator FadeToNewMusic(AudioClip newClip)
    {
        // Nếu nhạc hiện tại đã là nhạc mới thì không làm gì
        if (_audioSource.clip == newClip && _audioSource.isPlaying) yield break;

        // Giảm âm lượng nhạc cũ
        while (_audioSource.volume > 0)
        {
            _audioSource.volume -= Time.deltaTime * fadeSpeed; // Giảm dần âm lượng
            yield return null; // Chờ đến frame tiếp theo
        }

        _audioSource.Stop(); // Dừng nhạc cũ
        _audioSource.clip = newClip; // Thay nhạc mới
        _audioSource.Play(); // Phát nhạc mới

        // Tăng âm lượng nhạc mới
        while (_audioSource.volume < maxVolume)
        {
            _audioSource.volume += Time.deltaTime * fadeSpeed; // Tăng dần âm lượng
            yield return null; // Chờ đến frame tiếp theo   
        }
    }
}