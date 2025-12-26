using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneModeLoader : MonoBehaviour
{
    [Header("Settings")]
    public string LevelName = "Sample Scene";
    public int LevelBuildID = -1;
    public bool LoadOnAwake = false;

    [Header("UI Loading Settings")]
    public GameObject LoadingPanel;
    public Slider LoadingBar;
    public Text ModeNameText; // Tên chế độ chơi hiển thị trên UI

    [Tooltip("Tốc độ chạy của thanh. Ví dụ 0.5 nghĩa là mất 2 giây để chạy từ 0 đến 1.")]
    public float FillSpeed = 0.5f;   // Tốc độ chạy từ từ
    public float DelayAfterFull = 1f;

    void Awake()
    {
        if (LoadingPanel != null) LoadingPanel.SetActive(false);
        if (LoadOnAwake) LoadLevel();
    }

    public void LoadLevel()
    {
        // Trước khi load, cập nhật tên Mode dựa trên ID hiện tại
        UpdateModeText(LevelBuildID);
        StartCoroutine(LoadLevelAsyncRoutine());
    }

    // Hàm phụ trách đổi tên Text hiển thị
    private void UpdateModeText(int id)
    {
        if (ModeNameText == null) return;

        switch (id)
        {
            case 1:
                ModeNameText.text = "MODE TPS";
                break;
            case 2:
                ModeNameText.text = "MODE FPS";
                break;
            case 3:
                ModeNameText.text = "MODE GRAVITY SWITCH";
                break;
            default:
                ModeNameText.text = "MODE..."; // Tên mặc định nếu ID không khớp
                break;
        }
    }

    private IEnumerator LoadLevelAsyncRoutine()
    {
        if (LoadingPanel != null) LoadingPanel.SetActive(true);

        AsyncOperation operation;
        if (LevelBuildID > -1)
            operation = SceneManager.LoadSceneAsync(LevelBuildID);
        else
            operation = SceneManager.LoadSceneAsync(LevelName);

        // Chặn không cho tự chuyển cảnh
        operation.allowSceneActivation = false;

        float visualProgress = 0f; // Giá trị hiển thị trên Slider

        // Vòng lặp chạy cho đến khi Slider đạt 1 (100%)
        while (visualProgress < 1f)
        {
            // Mục tiêu thực tế của Unity (nạp đến 0.9 là xong 100% dữ liệu)
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Dùng MoveTowards để ép visualProgress tăng dần đều theo thời gian
            // Nó sẽ không nhảy vọt mà "lết" từ từ đến targetProgress
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, FillSpeed * Time.deltaTime);

            if (LoadingBar != null)
            {
                LoadingBar.value = visualProgress;
            }

            // Chỉ khi thanh chạy đầy (100%) VÀ Unity đã nạp xong dữ liệu ngầm (0.9)
            if (visualProgress >= 1f && operation.progress >= 0.9f)
            {
                // Chờ thêm 1 giây như bạn muốn
                yield return new WaitForSeconds(DelayAfterFull);

                // Kích hoạt vào Scene mới
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // Các hàm bổ trợ
    public void LoadLevel(string levelName) { LevelName = levelName; LoadLevel(); }
    public void LoadLevel(int levelID) { LevelBuildID = levelID; LoadLevel(); }

    public void LoadLevelInSeconds(float Seconds)
    {
        Invoke(nameof(LoadLevel), Seconds);
    }
}