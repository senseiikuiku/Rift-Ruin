using JUTPS;
using UnityEngine;
using System.Collections;
public class KillReporter : MonoBehaviour
{
    private JUHealth juHealth;

    void Start()
    {
        juHealth = GetComponent<JUHealth>();
        if (juHealth != null)
        {
            juHealth.OnDeath.AddListener(ReportKill);
        }
    }

    private void ReportKill()
    {
        if (KillManager.Instance != null)
        {
            KillManager.Instance.AddKill();

            // Kiểm tra win ngay sau khi kill được cộng
            if (UIManager.Instance != null &&
                KillManager.Instance.kills >= UIManager.Instance.targetKills)
            {
                // KÍCH HOẠT CỜ TẠM DỪNG ĐẾM KILL
                KillManager.Instance.SetFinisherActive(true);

                foreach (GameObject spawner in EnemyFinisher.Instance.EnemySpawner)
                {
                    spawner.SetActive(false);
                }

                // Slowmotion khi giết con cuối cùng
                JUTPS.FX.JUSlowmotion.DoSlowMotion(0.1f, 3f);

                // KÍCH HOẠT CHỨC NĂNG GIẢM MÁU TỪ TỪ CHO ZOMBIE CÒN LẠI
                // (Sẽ tìm và áp dụng sát thương liên tục lên tất cả JUHealth còn lại)
                EnemyFinisher.Instance.ExecuteFinisher();

                // Gọi coroutine để hiển thị UI Win sau 3 giây
                StartCoroutine(ShowWinAfterDelay(5f));
            }
        }
    }

    private IEnumerator ShowWinAfterDelay(float delay)
    {
        // Dùng WaitForSecondsRealtime để không bị ảnh hưởng bởi Time.timeScale = 0.1
        yield return new WaitForSecondsRealtime(delay);

        // Sau 3s mới hiện UI Win
        UIManager.Instance.UIWinOrLoseButton[0].SetActive(false); // nút tiếp tục ẩn
        UIManager.Instance.UIWinOrLoseButton[1].SetActive(true);  // nút chơi lại hiện
        UIManager.Instance.UITextWinOrLose.text = "You Win !!!";
        UIManager.Instance.IsUIWinOrLose(true);
    }



}
