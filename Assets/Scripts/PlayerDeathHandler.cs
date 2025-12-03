using JUTPS;
using JUTPS.CameraSystems;
using UnityEngine;
using System.Collections; // Cần thêm thư viện này để dùng Coroutine

public class PlayerDeathHandler : MonoBehaviour
{
    private JUHealth juHealth;

    void Start()
    {
        // Lấy component JUHealth của Player
        juHealth = GetComponent<JUHealth>();

        if (juHealth != null)
        {
            // Đăng ký phương thức HandlePlayerDeath vào sự kiện OnDeath
            juHealth.OnDeath.AddListener(HandlePlayerDeath);
        }
        else
        {
            Debug.LogError("JUHealth component not found on Player! Cannot set up death handling.");
        }
    }

    private void HandlePlayerDeath()
    {
        // Bắt đầu Coroutine để xử lý Slow Motion và hiển thị UI
        StartCoroutine(ShowLoseUIAfterSlowMotion(3f)); // Đợi 3 giây (duration của Slow Motion)
    }

    private IEnumerator ShowLoseUIAfterSlowMotion(float delay)
    {
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