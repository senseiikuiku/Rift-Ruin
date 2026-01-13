using System.Collections;
using JUTPS.CameraSystems;
using JUTPS;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerCameraAssigner : NetworkBehaviour
{
    [Header("Cài đặt thử lại (Dùng nếu Camera sinh ra sau Nhân vật)")]
    [SerializeField] private int soLanThuToiDa = 8;
    [SerializeField] private float thoiGianChoMoiLanThu = 0.25f;

    private Coroutine assignCoroutine;

    public override void OnNetworkSpawn()
    {
        // Chỉ chạy cho người chơi cục bộ (Local Player)
        if (!IsOwner) return;

        // Thử gán ngay lập tức, nếu không được thì mới bắt đầu thử lại (Coroutine)
        if (!TryAssignCamera())
        {
            assignCoroutine = StartCoroutine(AssignCameraWithRetries());
        }
    }

    private IEnumerator AssignCameraWithRetries()
    {
        int attempts = 0;
        while (attempts < soLanThuToiDa)
        {
            if (TryAssignCamera())
            {
                yield break;
            }

            attempts++;
            yield return new WaitForSeconds(thoiGianChoMoiLanThu);
        }

        Debug.LogWarning($"MultiplayerCameraAssigner: Không tìm thấy TPSCameraController sau {soLanThuToiDa} lần thử.");
    }

    private bool TryAssignCamera()
    {
        // Tìm bất kỳ TPSCameraController nào trong Scene
        TPSCameraController juCam = FindTPSCameraController();
        if (juCam == null) return false;

        // Nếu camera đã đang theo dõi nhân vật này rồi thì thoát
        if (juCam.TargetToFollow == this.transform) return true;

        // Gán mục tiêu theo dõi
        juCam.TargetToFollow = this.transform;

        // Nếu có thành phần JUCharacterController, ưu tiên theo dõi xương cột sống (Spine) để mượt hơn
        var juChar = GetComponent<JUCharacterController>();
        if (juChar != null)
        {
            juCam.characterTarget = juChar;

            // Gán PlayerController cho GameManager ---
            // Việc này giúp các script UI và Hệ thống Input của JUTPS biết ai là người chơi chính
            JUGameManager.PlayerController = juChar;

            if (juChar.HumanoidSpine != null)
                juCam.TargetToFollow = juChar.HumanoidSpine;
        }

        // 3. Khớp góc quay của Camera với góc quay của nhân vật ngay lập tức
        juCam.SetCameraRotation(0, this.transform.eulerAngles.y, false);

        // Tự động kích hoạt UI Message nếu nó đang bị tắt ---
        // Dùng FindAnyObjectByType thay cho FindObjectOfType để tối ưu tốc độ
        var uiMessage = Object.FindAnyObjectByType<JUTPS.UI.UIInteractMessages>(FindObjectsInactive.Include);
        if (uiMessage != null) uiMessage.enabled = true;

        Debug.Log("MultiplayerCameraAssigner: Đã gán Camera và cập nhật GameManager.");
        return true;
    }

    private TPSCameraController FindTPSCameraController()
    {
        // Dùng FindObjectsByType thay cho FindObjectsOfType để tối ưu tốc độ
#if UNITY_2021_3_OR_NEWER
        var cams = Object.FindObjectsByType<TPSCameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var cams = Object.FindObjectsOfType<TPSCameraController>(true);
#endif
        if (cams != null && cams.Length > 0) return cams[0];

        if (Camera.main != null)
        {
            var camComp = Camera.main.GetComponent<TPSCameraController>();
            if (camComp != null) return camComp;
        }
        return null;
    }

    // Thêm 'override' để không ghi đè member của NetworkBehaviour
    public override void OnDestroy()
    {
        if (assignCoroutine != null)
            StopCoroutine(assignCoroutine);

        // Luôn gọi base.OnDestroy() khi làm việc với NetworkBehaviour
        base.OnDestroy();
    }
}