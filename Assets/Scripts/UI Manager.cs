using JUTPS;
using JUTPS.CameraSystems;
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

    [SerializeField] private GameObject UIWinOrLose; // Giao diện win hoặc lose
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
        if (isUIWinOrLose)
        {
            UIWinOrLose.SetActive(true);
            // Mở menu gốc JU TPS
            if (!JUPauseGame.IsPaused)
                JUPauseGame.Pause();

            JUCameraController.LockMouse(false, false); // Mở chuột để bấm nút
        }
        else
        {
            UITextWinOrLose.text = "";
            UIWinOrLose.SetActive(false);
        }
    }


}
