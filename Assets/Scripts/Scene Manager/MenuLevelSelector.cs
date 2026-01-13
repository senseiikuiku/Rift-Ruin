using UnityEngine;
using JUTPS.AI;
using TMPro;
public class MenuLevelSelector : MonoBehaviour
{
    // Đây là biến tạm để lưu Loader của cái nút Mode vừa được bấm
    private SceneModeLoader _activeModeLoader;

    [Header("UI Panel")]
    public GameObject DifficultyPanel; // Panel chứa 3 nút Easy, Medium, Hard

    public TMP_Text ModeNameText; // Tên chế độ chơi hiển thị trên UI

    // BƯỚC 1: Hàm này để cái nút Mode tự "đăng ký" bản thân nó vào đây
    public void OpenDifficultySelection(SceneModeLoader clickedLoader)
    {
        _activeModeLoader = clickedLoader;

        // Hiện cái bảng 3 nút lên
        if (DifficultyPanel != null) DifficultyPanel.SetActive(true);

        Debug.Log("Đang chọn Level cho Mode: " + _activeModeLoader.LevelName);
        UpdateModeText(_activeModeLoader.LevelBuildID);
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
            case 4:
                ModeNameText.text = "MODE ONLINE";
                break;
            default:
                ModeNameText.text = "MODE..."; // Tên mặc định nếu ID không khớp
                break;
        }
    }

    // BƯỚC 2: Các nút Easy, Medium, Hard sẽ gọi các hàm này
    public void ClickEasy() { SelectLevel(DifficultyLevel.Easy); }
    public void ClickMedium() { SelectLevel(DifficultyLevel.Medium); }
    public void ClickHard() { SelectLevel(DifficultyLevel.Hard); }
    private void SelectLevel(DifficultyLevel level)
    {
        if (DifficultyManager.Instance == null)
        {
            Debug.LogError("LỖI: Thiếu DifficultyManager trong Scene. Hãy tạo 1 Object và gắn script DifficultyManager vào!");
            return;
        }

        if (_activeModeLoader == null)
        {
            Debug.LogError("LỖI: Chưa xác định được Mode Loader. Bạn đã kéo nút Mode vào ô tham số trong OnClick chưa?");
            return;
        }

        // Nếu không lỗi thì chạy tiếp
        DifficultyManager.Instance.CurrentDifficulty = level;
        _activeModeLoader.LoadLevel();
    }
}