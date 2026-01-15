using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using JU;
using JUTPS.GameSettings; // Đảm bảo có namespace này

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Âm thanh")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("Cấu hình JU")]
    public JUTag sfxTag;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayJUSound(hoverSound);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayJUSound(clickSound);
    }

    private void PlayJUSound(AudioClip clip)
    {
        if (clip == null) return;

        // Lấy âm lượng SFX từ JUGameSettings
        float sfxVolume = JUGameSettings.GetAudioVolume(sfxTag);

        // TẠO OBJECT ÂM THANH TẠM THỜI (Tối ưu hơn PlayClipAtPoint)
        GameObject soundObj = new GameObject("ButtonSoundTemp");
        AudioSource source = soundObj.AddComponent<AudioSource>();

        source.clip = clip;
        source.volume = sfxVolume;
        source.spatialBlend = 0f; // Đặt là âm thanh 2D để đứng đâu cũng nghe rõ

        // QUAN TRỌNG: Giúp âm thanh nút bấm kêu ngay cả khi Game đang Pause
        source.ignoreListenerPause = true;

        source.Play();

        // Xóa object sau khi phát xong (Sử dụng Coroutine để xóa theo thời gian thực)
        StartCoroutine(DestroySoundRealtime(soundObj, clip.length));
    }

    private System.Collections.IEnumerator DestroySoundRealtime(GameObject obj, float delay)
    {
        // Đợi theo thời gian thực (không bị ảnh hưởng bởi pause game)
        yield return new WaitForSecondsRealtime(delay);
        if (obj != null) Destroy(obj);
    }
}