using TMPro;
using UnityEngine;
public class KillManager : MonoBehaviour
{
    public static KillManager Instance; // Singleton để truy cập dễ
    public int kills = 0;

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
        kills++;
        Debug.Log("Kẻ thù chết! Tổng số: " + kills);

        if (killText != null)
        {
            killText.text = kills.ToString();
        }
    }

    // Reset khi cần
    public void ResetKills()
    {
        kills = 0;
        if (killText != null) killText.text = "0";
    }
}
