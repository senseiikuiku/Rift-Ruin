using UnityEngine;
using JUTPS.FX;

public class ExplosionShakeEffect : MonoBehaviour
{
    // Tham chiếu đến Shaker/ShakeOneTime trên cùng đối tượng này
    private ShakeOneTime shakeTrigger;

    [Header("Shake Parameters")]
    [Tooltip("Bán kính tối đa mà rung lắc còn ảnh hưởng. Dùng cho tính toán giảm cường độ.")]
    public float DefaultShakeRadius = 15f;

    void Start()
    {
        // Lấy component ShakeOneTime
        shakeTrigger = GetComponent<ShakeOneTime>();

        if (shakeTrigger == null)
        {
            Debug.LogError("ShakeOneTime component not found! Please attach it to the Camera Rig.");
        }
        else
        {
            // xác nhận script đã khởi tạo và tìm thấy Shaker
            Debug.Log("ExplosionShakeEffect initialized successfully on Camera Rig.");
        }
    }

    /// <summary>
    /// Hàm này được gọi bởi UnityEvent (OnExplode) của đối tượng Explosion.
    /// </summary>
    public void ReceiveExplosionSignal(float explosionRadius)
    {
        // Lấy lại tham chiếu ngay trước khi sử dụng
        ShakeOneTime currentShakeTrigger = GetComponent<ShakeOneTime>();

        if (currentShakeTrigger != null)
        {
            currentShakeTrigger.Shake(explosionRadius > 0 ? explosionRadius : DefaultShakeRadius);

            Debug.Log("Camera received explosion signal and initiated shake.");
        }
        else
        {
            Debug.LogError("ShakeOneTime component is missing or has been destroyed on the Camera Rig.");
            // Thay bằng Error để dễ theo dõi hơn
        }
    }

    // Biến thể nếu bạn muốn không cần truyền tham số
    public void ReceiveExplosionSignal()
    {
        ReceiveExplosionSignal(DefaultShakeRadius);
    }
}