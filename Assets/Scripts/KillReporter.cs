using JU;
using JUTPS;
using JUTPS.GameSettings;
using System.Collections;
using UnityEngine;
public class KillReporter : MonoBehaviour
{
    private JUHealth juHealth;

    public AudioClip winSound;
    public JUTag sfxTag;// Gán tag SFX hoặc UI để lấy âm lượng từ Settings

    // Biến static dùng chung cho tất cả các Zombie, đảm bảo nhạc thắng chỉ phát 1 lần
    private static bool _hasPlayedWinSound = false;

    void Start()
    {
        // Reset lại biến khi bắt đầu màn chơi mới
        _hasPlayedWinSound = false;

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
            if (PlayerDeathHandler.Instance != null && !PlayerDeathHandler.Instance.checkPlayerLive)
                KillManager.Instance.AddKill();

            // Kiểm tra win ngay sau khi kill được cộng
            if (UIManager.Instance != null &&
                KillManager.Instance.kills >= UIManager.Instance.targetKills)
            {
                // KIỂM TRA: Nếu chưa phát nhạc thắng thì mới thực hiện logic thắng
                if (!_hasPlayedWinSound)
                {
                    _hasPlayedWinSound = true; // Khóa lại ngay lập tức

                    // 1. Dừng Spawner
                    KillManager.Instance.SetFinisherActive(true);
                    if (EnemyFinisher.Instance != null)
                    {
                        foreach (GameObject spawner in EnemyFinisher.Instance.EnemySpawner)
                        {
                            if (spawner != null) spawner.SetActive(false);
                        }
                        EnemyFinisher.Instance.ExecuteFinisher();
                    }

                    // 2. Slowmotion
                    JUTPS.FX.JUSlowmotion.DoSlowMotion(0.1f, 3f);

                    // 3. PHÁT ÂM THANH CHIẾN THẮNG THEO TAG
                    PlayWinSound();

                    // 4. Hiện UI sau delay
                    StartCoroutine(ShowWinAfterDelay(5f));
                }
            }
        }
    }

    private void PlayWinSound()
    {
        if (winSound == null) return;

        // Lấy âm lượng từ JUGameSettings dựa trên JUTag bạn đã chọn
        float volume = JUGameSettings.GetAudioVolume(sfxTag);

        // Tạo một GameObject tạm thời để phát âm thanh (đảm bảo nghe rõ hơn PlayClipAtPoint)
        GameObject soundObj = new GameObject("WinSoundTemp");
        AudioSource source = soundObj.AddComponent<AudioSource>();

        source.clip = winSound;
        source.volume = volume;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // Đặt là 0 để nhạc thắng phát đều 2 tai (2D), không bị nhỏ khi đứng xa xác zombie

        // --- QUAN TRỌNG: Bỏ qua lệnh Pause của hệ thống Audio ---
        source.ignoreListenerPause = true;

        source.Play();

        Debug.Log("Đang phát âm thanh chiến thắng với âm lượng: " + volume);

        // --- KHÔNG DÙNG Destroy(soundObj, winSound.length) vì game đã Pause ---
        // Phải dùng Coroutine xóa theo thời gian thực (Realtime)
        StartCoroutine(DestroyWinSoundRealtime(soundObj, winSound.length));
    }

    // Coroutine xóa Object âm thanh bất kể game có Pause hay không
    private IEnumerator DestroyWinSoundRealtime(GameObject obj, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (obj != null) Destroy(obj);
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
