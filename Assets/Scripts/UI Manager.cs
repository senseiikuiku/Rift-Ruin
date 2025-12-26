using JUTPS;
using JUTPS.CameraSystems;
using JUTPS.UI;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public int targetKills;// Số kill cần đạt được
    [Header("UI")]
    public TMP_Text targetkillText; // Giao diện số kill cần đạt được

    public GameObject UIWinOrLose; // Giao diện win hoặc lose
    public TMP_Text UITextWinOrLose; // Chữ You Win hoặc You Lose

    public GameObject[] UIWinOrLoseButton; // Mảng chứa nút Continue hoặc Play Again trong Menu



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        targetkillText.text = targetKills.ToString();
        UIWinOrLose.SetActive(false);
        UITextWinOrLose.text = "";
        foreach (GameObject button in UIWinOrLoseButton)
            button.SetActive(false);
        // Nếu Số kill nhỏ hơn mục tiêu thì Nút Tiếp tục active true
        if (KillManager.Instance.kills < targetKills)
        {
            UIWinOrLoseButton[0].SetActive(true);
        }
    }

    // Hiện UI Win
    public void IsUIWinOrLose(bool isUIWinOrLose)
    {
        // Lấy tham chiếu đến JU_UIPause Instance
        JU_UIPause uiPauseInstance = JU_UIPause.Instance;

        if (isUIWinOrLose)
        {
            UIWinOrLose.SetActive(true);

            // Nút PlayAgainButton[0] trong JU_UIPause sẽ bị ẨN khi UIWinOrLose được BẬT
            if (uiPauseInstance != null)
            {
                uiPauseInstance.SetPlayAgainButtonVisible(false);
            }

            // Mở menu gốc JU TPS
            if (!JUPauseGame.IsPaused)
                JUPauseGame.Pause();

            JUCameraController.LockMouse(false, false); // Mở chuột để bấm nút

            // VÔ HIỆU HÓA CHỨC NĂNG PAUSE/CONTINUE TỪ INPUT/CODE KHÁC
            JUPauseGame.AllowSetPaused = false; // 
        }
        else
        {
            // KÍCH HOẠT LẠI CHỨC NĂNG PAUSE/CONTINUE KHI TẮT UI
            JUPauseGame.AllowSetPaused = true; // (Cho trường hợp bạn muốn dùng lại UI này cho Menu tạm dừng)

            // Nút PlayAgainButton[0] trong JU_UIPause sẽ được HIỆN khi UIWinOrLose được TẮT
            if (uiPauseInstance != null)
            {
                uiPauseInstance.SetPlayAgainButtonVisible(true);
            }
            UITextWinOrLose.text = "";
            UIWinOrLose.SetActive(false);
        }
    }

}
