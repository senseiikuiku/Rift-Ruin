using JUTPS;
using JUTPS.AI;
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


    [Header("Canvas Group Settings")]
    public CanvasGroup winLoseCanvasGroup; // Kéo Canvas Group của UIWinOrLose vào đây
    public float fadeDuration = 1.5f;     // Thời gian mờ dần (nên để lâu hơn setting một chút cho kịch tính)


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Đảm bảo có Canvas Group
        if (winLoseCanvasGroup == null && UIWinOrLose != null)
            winLoseCanvasGroup = UIWinOrLose.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        ZombieDifficultySettings settings = DifficultyManager.Instance.GetCurrentSettings();
        targetKills = settings.targetKills;
        targetkillText.text = targetKills.ToString();

        // --- KHÔNG DÙNG SETACTIVE ---
        // Đảm bảo Object luôn bật nhưng tàng hình và không thể bấm vào
        UIWinOrLose.SetActive(true);
        if (winLoseCanvasGroup != null)
        {
            winLoseCanvasGroup.alpha = 0;
            winLoseCanvasGroup.interactable = false;
            winLoseCanvasGroup.blocksRaycasts = false;
        }

        UITextWinOrLose.text = "";
        foreach (GameObject button in UIWinOrLoseButton)
        {

            button.SetActive(false);
            Debug.Log("Set button inactive at Start");
        }
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

            // Bật khả năng tương tác và chặn chuột
            if (winLoseCanvasGroup != null)
            {
                winLoseCanvasGroup.interactable = true;
                winLoseCanvasGroup.blocksRaycasts = true;
            }

            StopAllCoroutines();
            StartCoroutine(FadeUI(1));

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
            // Tắt khả năng tương tác ngay lập tức
            if (winLoseCanvasGroup != null)
            {
                winLoseCanvasGroup.interactable = false;
                winLoseCanvasGroup.blocksRaycasts = false;
            }

            // KÍCH HOẠT LẠI CHỨC NĂNG PAUSE/CONTINUE KHI TẮT UI
            JUPauseGame.AllowSetPaused = true; // (Cho trường hợp bạn muốn dùng lại UI này cho Menu tạm dừng)

            // Nút PlayAgainButton[0] trong JU_UIPause sẽ được HIỆN khi UIWinOrLose được TẮT
            if (uiPauseInstance != null)
            {
                uiPauseInstance.SetPlayAgainButtonVisible(true);
            }
            StopAllCoroutines();
            StartCoroutine(FadeUI(0, () =>
            {
                UITextWinOrLose.text = "";
            }));
        }
    }

    // Hàm Coroutine xử lý mờ dần
    private IEnumerator FadeUI(float targetAlpha, System.Action onComplete = null)
    {
        if (winLoseCanvasGroup == null) yield break;

        float startAlpha = winLoseCanvasGroup.alpha;
        float time = 0;

        // Thực hiện hiệu ứng mờ dần
        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime; // Sử dụng unscaledDeltaTime để không bị ảnh hưởng bởi Time.timeScale
            winLoseCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        winLoseCanvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}
