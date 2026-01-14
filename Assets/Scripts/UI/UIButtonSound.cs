using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using JU; // Thêm namespace của JU

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Âm thanh")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("Cấu hình JU")]
    // Gán JUTag có tên là "SFX" vào đây trong Inspector
    public JUTag sfxTag;

    // Khi di chuột vào
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayJUSound(hoverSound);
    }

    // Khi click
    public void OnPointerClick(PointerEventData eventData)
    {
        PlayJUSound(clickSound);
    }

    private void PlayJUSound(AudioClip clip)
    {
        if (clip == null) return;

        // Lấy âm lượng SFX hiện tại từ cấu hình JUGameSettings
        // Nếu không có tag, mặc định sẽ là 1 (âm lượng tối đa)
        float sfxVolume = JUTPS.GameSettings.JUGameSettings.GetAudioVolume(sfxTag);

        // Phát âm thanh thông qua JU Audio System để nó tự quản lý
        // Cách đơn giản nhất là tạo một AudioSource tạm thời hoặc dùng Static Class của JU
        // Ở đây chúng ta phát tại vị trí camera hoặc vị trí nút với âm lượng đã nhân với sfxVolume
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, sfxVolume);

        // Ghi chú: Nếu JU có JUAudioController.Play(clip, tag), bạn nên dùng cái đó sẽ chuyên nghiệp hơn.
    }
}