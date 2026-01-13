using JUTPS;
using JUTPS.CameraSystems;
using JUTPS.CharacterBrain;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    private JUCharacterController juController;

    public override void OnNetworkSpawn()
    {
        juController = GetComponent<JUCharacterController>();

        if (IsOwner)
        {
            // Nếu là chủ sở hữu: Đảm bảo Camera và Mouse Lock hoạt động
            JUCameraController.LockMouse(true, true);
            Debug.Log("Đang điều khiển nhân vật cục bộ");
        }
        else
        {
            // Nếu KHÔNG phải chủ sở hữu: Vô hiệu hóa toàn bộ quyền điều khiển
            if (juController != null) juController.enabled = false;

            // Tắt Brain điều khiển của JU
            if (TryGetComponent<JUCharacterBrain>(out var brain))
                brain.enabled = false;

            // Tắt Input Manager (Rất quan trọng để tránh nhân vật tự ngắm bắn)
            // Do JUInputManager không phải là một thành phần công khai,
            // nên ta phải tìm nó bằng cách kiểm tra tên kiểu (Type Name)
            bool inputDisabled = false;
            foreach (var comp in GetComponents<Component>())
            {
                if (comp == null) continue;
                var t = comp.GetType();
                if (t.Name == "JUInputManager")
                {
                    if (comp is Behaviour b) b.enabled = false;
                    inputDisabled = true;
                    break;
                }
            }
            if (!inputDisabled)
            {
                foreach (var comp in GetComponentsInChildren<Component>())
                {
                    if (comp == null) continue;
                    var t = comp.GetType();
                    if (t.Name == "JUInputManager")
                    {
                        if (comp is Behaviour b) b.enabled = false;
                        break;
                    }
                }
            }

            // Tắt Camera con và AudioListener
            var playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);

            var audioListener = GetComponentInChildren<AudioListener>();
            if (audioListener != null) audioListener.enabled = false;
        }
    }
}