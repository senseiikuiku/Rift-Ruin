using UnityEngine;
using UnityEngine.Rendering;

public class Teleporter : MonoBehaviour
{
    // Biến công khai để bạn kéo thả (Drag & Drop) điểm đến trong Inspector
    // Kéo thả Empty GameObject bạn đã tạo ở Bước 1 vào đây
    [Header("Destination Settings")]
    [Tooltip("Vị trí mà nhân vật sẽ được dịch chuyển tới.")]
    public Transform destination;

    private void OnTriggerEnter(Collider other)
    {
        // 1. Kiểm tra xem vật thể đi vào có phải là Nhân vật (Player) không
        // Giả sử nhân vật của bạn có Tag là "Player"
        if (other.gameObject.CompareTag("Player"))
        {
            // 2. Kiểm tra xem điểm đến đã được thiết lập chưa
            if (destination != null)
            {
                // 3. Thực hiện dịch chuyển
                // Lấy đối tượng gốc (root) của nhân vật
                Transform playerRoot = other.transform.root;

                // Dịch chuyển nhân vật đến vị trí của điểm đến
                playerRoot.position = destination.position;

                // Tùy chọn: Xoay nhân vật để hướng về một hướng cụ thể
                playerRoot.rotation = destination.rotation;

                Debug.Log($"Đã dịch chuyển nhân vật {playerRoot.name} tới {destination.name}!");
            }
            else
            {
                Debug.LogError("Điểm đến (Destination) chưa được thiết lập trên Teleporter!");
            }
        }
    }
}
