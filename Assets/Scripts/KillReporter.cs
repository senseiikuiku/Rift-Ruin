using JUTPS;
using UnityEngine;

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
                UIManager.Instance.IsUIWin(true);
            }
        }
    }

}
