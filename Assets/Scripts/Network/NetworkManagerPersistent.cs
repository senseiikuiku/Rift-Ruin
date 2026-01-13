using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkManagerPersistent : MonoBehaviour
{
    public static NetworkManagerPersistent Instance;

    private void Awake()
    {
        // Kiểm tra Singleton: Nếu đã có NetworkManager rồi thì xóa bản copy này
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Hàm này dùng để dọn dẹp NetworkManager khi bạn muốn thoát hẳn Mode Online
    public void ShutdownAndDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        Destroy(gameObject);
    }
}