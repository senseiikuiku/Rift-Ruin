using JUTPS;
using JUTPS.CameraSystems;
using TMPro;
using UnityEngine;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public int targetKills;

    [Header("UI")]
    public TMP_Text targetkillText;

    [SerializeField] private GameObject UIWin;
    [SerializeField] private GameObject UIMenu;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        targetkillText.text = targetKills.ToString();
        UIWin.SetActive(false);
        UIMenu.SetActive(false);
    }

    // Khi bấm nút Menu trong UIWin
    public void IsUIMenu(bool isShowMenu)
    {
        if (isShowMenu)
        {
            // Ẩn UI Win
            IsUIWin(false);

            // Mở menu gốc JU TPS
            if (!JUPauseGame.IsPaused)
                JUPauseGame.Pause();
        }
        else
        {
            // Tiếp tục game
            if (JUPauseGame.IsPaused)
            {
                JUPauseGame.Continue();
            }
            // Ép khóa chuột lại sau khi bấm Continue
            StartCoroutine(ReLockMouseAfterContinue());
        }
    }

    private IEnumerator ReLockMouseAfterContinue()
    {
        yield return null; // đợi 1 frame để JU xử lý xong OnContinueGame()
        JUCameraController.LockMouse(true, true);
    }

    // Hiện UI Win
    public void IsUIWin(bool isUIWin)
    {
        if (isUIWin)
        {
            UIWin.SetActive(true);
            Time.timeScale = 0;
            JUCameraController.LockMouse(false, false); // Mở chuột để bấm nút
        }
        else
        {
            UIWin.SetActive(false);
        }
    }


}
