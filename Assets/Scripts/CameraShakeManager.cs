using UnityEngine;
using JUTPS.FX; // Import namespace của Shaker

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    // Tham chiếu đến ShakeOneTime và Shaker 
    private ShakeOneTime _shakeTrigger;
    private Shaker _shaker;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Nếu đã tồn tại, hủy đối tượng mới này
            Destroy(gameObject);
        }
        else
        {
            Instance = this;

            // Lấy tham chiếu các component
            _shakeTrigger = GetComponent<ShakeOneTime>();
            _shaker = GetComponent<Shaker>();

            if (_shakeTrigger == null || _shaker == null)
            {
                Debug.LogError("CameraShakeManager requires both ShakeOneTime and Shaker components on the same GameObject!");
            }
        }
    }

    public void TriggerExplosionShake(float explosionRadius)
    {
        if (_shakeTrigger != null)
        {
            // Truyền bán kính của vụ nổ vào hàm Shake
            _shakeTrigger.Shake(explosionRadius);
            Debug.Log($"Singleton Shaker triggered with radius: {explosionRadius}");
        }
        else
        {
            Debug.LogError("Cannot trigger shake: ShakeOneTime component is missing.");
        }
    }
}