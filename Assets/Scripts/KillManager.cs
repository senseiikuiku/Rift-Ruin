using TMPro;
using UnityEngine;
public class KillManager : MonoBehaviour
{
    public static KillManager Instance; // Singleton để truy cập dễ
    public int kills = 0;

    //  Dùng để tạm dừng đếm kill khi hiệu ứng kết thúc đang chạy
    private bool isFinisherActive = false;

    [Header("UI (Optional)")]
    public TMP_Text killText; // Hiển thị lên UI

    private void Awake()
    {
        // Đảm bảo chỉ có một KillManager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Hàm này sẽ được gọi khi zombie chết
    public void AddKill()
    {
        // Nếu Finisher đang chạy, không cộng kill.
        if (isFinisherActive)
        {
            return;
        }

        kills++;
        Debug.Log("Kẻ thù chết! Tổng số: " + kills);

        if (killText != null)
        {
            killText.text = kills.ToString();
        }
    }

    // Kích hoạt/Vô hiệu hóa trạng thái Finisher
    public void SetFinisherActive(bool active)
    {
        isFinisherActive = active;
        if (active)
        {
            Debug.Log("FINISHER MODE: Tạm dừng đếm kill.");
        }
    }

    // Reset khi cần
    public void ResetKills()
    {
        kills = 0;
        isFinisherActive = false; // Đảm bảo reset cờ
        if (killText != null) killText.text = "0";
    }
}
