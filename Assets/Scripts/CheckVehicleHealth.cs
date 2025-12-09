using JUTPS;
using JUTPS.ActionScripts;
using JUTPS.InteractionSystem.Interactables;
using JUTPS.VehicleSystem;
using UnityEngine;

public class CheckVehicleHealth : MonoBehaviour
{
    private JUHealth JUHealth;

    void Start()
    {
        JUHealth = GetComponent<JUHealth>();
        if (JUHealth != null)
        {
            // Đăng ký phương thức HandleVehicleDeath vào sự kiện OnDeath
            JUHealth.OnDeath.AddListener(HandleVehicleDeath);
        }
        else
        {
            Debug.LogError("JUHealth component not found on Vehicle! Cannot set up death handling.");
        }
    }

    private void HandleVehicleDeath()
    {
        // Khóa khả năng tương tác (Vehicle Interaction)
        // Tìm và vô hiệu hóa tất cả các component JUVehicleInteractable (trên vật thể gốc và vật thể con)
        JUVehicleInteractable[] interactables = GetComponentsInChildren<JUVehicleInteractable>(true);
        foreach (JUVehicleInteractable interactable in interactables)
        {
            interactable.enabled = false;
        }

        // Thay đổi Tag và Layer cho vật thể và vật thể con
        ChangeTagAndLayerRecursively();

        // LOGIC BUỘC NGƯỜI CHƠI XUỐNG XE ===

        // Tìm script DriveVehicles trên Player
        DriveVehicles playerDriveVehicles = JUGameManager.PlayerController.GetComponent<DriveVehicles>();

        if (playerDriveVehicles != null && playerDriveVehicles.IsDriving)
        {
            // Lấy component JUVehicle của xe này (Xe đang gắn script CheckVehicleHealth)
            JUVehicle currentVehicle = GetComponent<JUVehicle>();

            // Kiểm tra xem Player có đang lái đúng chiếc xe này không
            if (currentVehicle != null && playerDriveVehicles.CurrentVehicle == currentVehicle)
            {
                // Nếu đang lái, buộc Player thoát khỏi xe
                // Hàm ExitVehicle() sẽ xử lý mọi logic liên quan đến việc Player xuống xe
                playerDriveVehicles.ExitVehicle();
                Debug.Log("Xe bị phá hủy, Player đã bị buộc xuống xe.");
            }
        }

        // Vô hiệu hóa Collider và Vật lý trên vật thể gốc
        DisableRootPhysics();

        Debug.Log($"Xe {gameObject.name} đã bị phá hủy. Đã vô hiệu hóa tương tác, đổi Tag/Layer và tắt vật lý.");
    }

    // Hàm riêng để đổi Tag/Layer 
    private void ChangeTagAndLayerRecursively()
    {
        int defaultLayer = LayerMask.NameToLayer("Default");

        if (defaultLayer == -1)
        {
            Debug.LogError("Không tìm thấy Layer 'Default'. Không thể đổi Layer.");
            return;
        }

        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);

        foreach (Transform childTransform in allTransforms)
        {
            childTransform.gameObject.tag = "Untagged";
            childTransform.gameObject.layer = defaultLayer;
        }
    }

    // Hàm riêng để tắt vật lý 
    private void DisableRootPhysics()
    {
        // Kiểm tra an toàn trước khi GetComponent
        Collider rootCollider = GetComponent<Collider>();
        if (rootCollider != null)
        {
            rootCollider.enabled = false;
        }

        Rigidbody rootRigidbody = GetComponent<Rigidbody>();
        if (rootRigidbody != null)
        {
            rootRigidbody.isKinematic = true;
        }
    }
}