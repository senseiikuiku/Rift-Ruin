using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{

    [Header("Gán loa trung tâm vào đây")]
    public AudioSource globalAudioSource;

    [Header("Âm thanh")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    void Awake()
    {
        // Tự động thiết lập AudioSource
        globalAudioSource.playOnAwake = false;
    }

    // Khi di chuột vào
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (globalAudioSource != null && hoverSound != null)
        {
            globalAudioSource.PlayOneShot(hoverSound);
        }
    }

    // Khi click
    public void OnPointerClick(PointerEventData eventData)
    {
        if (globalAudioSource != null && clickSound != null)
        {
            globalAudioSource.PlayOneShot(clickSound);
        }
    }
}