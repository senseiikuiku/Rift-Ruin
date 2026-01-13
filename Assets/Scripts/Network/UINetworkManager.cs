using JUTPS.CameraSystems;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class UINetworkManager : MonoBehaviour
{
    [Header("Cấu hình Scene")]
    [SerializeField] private string gameSceneName = "GAME";

    [Header("UI Loading Settings")]
    public GameObject LoadingPanel;
    public Slider LoadingBar;
    public Text ModeNameText;

    [Tooltip("Tốc độ chạy thanh Loading (0.5 = 2 giây để đầy)")]
    public float FillSpeed = 0.5f;
    public float DelayAfterFull = 1f;

    [Header("Prefab Network Manager")]
    [SerializeField] private GameObject networkManagerPrefab;

    [Header("UI Network Main")]
    public GameObject image;

    [Header("UI Buttons Network")]
    public Button hostButton;
    public Button clientButton;
    public Button serverButton;

    void Awake()
    {
        if (LoadingPanel != null) LoadingPanel.SetActive(false);
    }

    private void Start()
    {
        JUCameraController.LockMouse(false, false);

        if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
        if (clientButton != null) clientButton.onClick.AddListener(OnClientClicked);
        if (serverButton != null) serverButton.onClick.AddListener(OnServerClicked);
    }

    // desiredRole: "host", "client" hoặc "server" - dùng để quyết định tái sử dụng hay tạo mới NetworkManager
    private bool EnsureNetworkManagerExists(string desiredRole)
    {
        if (NetworkManager.Singleton == null)
        {
            if (networkManagerPrefab == null)
            {
                Debug.LogError("Chưa gán NetworkManager Prefab vào script UINetworkManager!");
                return false;
            }

            Instantiate(networkManagerPrefab);
            Debug.Log("Đã khởi tạo NetworkManager từ Prefab.");
            return true;
        }

        // Đã có một NetworkManager singleton đang tồn tại trong tiến trình này.
        // Quyết định cách xử lý tùy thuộc vào trạng thái hiện tại và vai trò mong muốn.
        var nm = NetworkManager.Singleton;

        // Nếu nó chưa lắng nghe (chưa Start) - tái sử dụng nó luôn
        if (!nm.IsListening)
        {
            Debug.Log("NetworkManager singleton tồn tại nhưng chưa Start -> dùng lại nó.");
            return true;
        }

        // Nếu nó đang chạy và vai trò mong muốn khớp với chế độ hiện tại, tái sử dụng nó.
        if (desiredRole.Equals("host", StringComparison.OrdinalIgnoreCase) && nm.IsHost)
        {
            Debug.Log("NetworkManager đã chạy ở chế độ Host. Dùng lại instance Host hiện có.");
            return true;
        }
        if (desiredRole.Equals("server", StringComparison.OrdinalIgnoreCase) && nm.IsServer && !nm.IsClient)
        {
            Debug.Log("NetworkManager đã chạy ở chế độ Server. Dùng lại instance Server hiện có.");
            return true;
        }
        if (desiredRole.Equals("client", StringComparison.OrdinalIgnoreCase) && nm.IsClient && !nm.IsServer)
        {
            Debug.Log("NetworkManager đã chạy ở chế độ Client. Dùng lại instance Client hiện có.");
            return true;
        }

        // Nếu chạy đến đây nghĩa là có xung đột: NetworkManager đang chạy ở chế độ khác với mong muốn.
        // Thường gặp khi test local: một Host đang chạy và bạn cố tình nhấn Start Client trên cùng máy đó.
        // Để cho phép chạy vai trò mới, hủy singleton cũ và tạo lại từ prefab.
        // LƯU Ý: Hủy NetworkManager đang chạy sẽ dừng mọi kết nối hiện tại trên máy này.
        Debug.LogWarning($"NetworkManager đang chạy ở chế độ {(nm.IsHost ? "Host" : nm.IsServer ? "Server" : "Client")}. " +
                         $"Vai trò yêu cầu = {desiredRole}. Đang khởi tạo lại NetworkManager...");

        try
        {
            // Tắt trước nếu nó đang lắng nghe
            if (nm.IsListening)
            {
                nm.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Lỗi khi tắt NetworkManager cũ: " + ex.Message);
        }

        // Hủy GameObject singleton cũ và tạo lại từ prefab
        try
        {
            Destroy(nm.gameObject);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Lỗi khi hủy GameObject NetworkManager: " + ex.Message);
        }

        if (networkManagerPrefab == null)
        {
            Debug.LogError("Chưa gán NetworkManager Prefab! Không thể tạo mới.");
            return false;
        }

        Instantiate(networkManagerPrefab);
        Debug.Log("Đã tạo lại NetworkManager từ Prefab cho vai trò: " + desiredRole);
        return true;
    }

    private void OnHostClicked()
    {
        if (!EnsureNetworkManagerExists("host")) return;

        SetButtonsInteractable(false);

        StartNetworkLoading(() =>
        {
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;

            if (!NetworkManager.Singleton.StartHost())
            {
                SetButtonsInteractable(true);
                NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
                if (LoadingPanel != null) LoadingPanel.SetActive(false);
                Debug.LogWarning("StartHost() thất bại");
            }
        });
    }

    private void OnClientClicked()
    {
        if (!EnsureNetworkManagerExists("client")) return;

        SetButtonsInteractable(false);

        StartNetworkLoading(() =>
        {
            // Đăng ký sự kiện để phản hồi kết quả kết nối
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            bool started = NetworkManager.Singleton.StartClient();
            Debug.Log("Client đang kết nối... StartClient trả về: " + started);

            if (!started)
            {
                // Nếu không start được, hủy đăng ký sự kiện và khôi phục UI
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                SetButtonsInteractable(true);
                if (LoadingPanel != null) LoadingPanel.SetActive(false);
            }
        });
    }

    private void OnClientConnected(ulong clientId)
    {
        // Chỉ quan tâm đến kết nối của chính máy này (Local Client)
        if (NetworkManager.Singleton == null) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        Debug.Log("Client local đã kết nối thành công: " + clientId);

        // Hủy đăng ký sự kiện sau khi đã kết nối xong
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

        // Ẩn UI và khóa chuột cho người chơi local
        HideUI();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Nếu client local bị ngắt kết nối, mở lại các nút UI để người dùng thử lại
        if (NetworkManager.Singleton == null) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        Debug.LogWarning("Client local bị ngắt kết nối: " + clientId);
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        SetButtonsInteractable(true);
        if (LoadingPanel != null) LoadingPanel.SetActive(false);
    }

    private void OnServerClicked()
    {
        if (!EnsureNetworkManagerExists("server")) return;
        SetButtonsInteractable(false);

        StartNetworkLoading(() =>
        {
            NetworkManager.Singleton.StartServer();
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        });
    }

    private void HideUI()
    {
        if (image != null) image.SetActive(false);
        JUCameraController.LockMouse(true, true);
    }

    public void StartNetworkLoading(Action onComplete)
    {
        if (image != null && image != this.gameObject)
        {
            image.SetActive(false);
        }
        else if (image == this.gameObject)
        {
            Debug.LogWarning("Không nên gán chính GameObject này vào ô Image!");
        }

        UpdateModeText(4);
        if (this.gameObject.activeInHierarchy)
        {
            StartCoroutine(NetworkLoadingRoutine(onComplete));
        }
    }

    private IEnumerator NetworkLoadingRoutine(Action onComplete)
    {
        if (LoadingPanel != null) LoadingPanel.SetActive(true);
        if (LoadingBar != null) LoadingBar.value = 0;

        float visualProgress = 0f;
        while (visualProgress < 1f)
        {
            visualProgress = Mathf.MoveTowards(visualProgress, 1f, FillSpeed * Time.deltaTime);
            if (LoadingBar != null) LoadingBar.value = visualProgress;
            yield return null;
        }

        yield return new WaitForSeconds(DelayAfterFull);

        onComplete?.Invoke();

        // GHI CHÚ: Với client, việc HideUI thật sự sẽ được kích hoạt khi kết nối thành công qua OnClientConnected().
        // Chúng ta vẫn gọi HideUI ở đây cho Server/Host và các trường hợp lỗi.
        HideUI();
    }

    private void HandleServerStarted()
    {
        Debug.Log("Server/Host đã khởi động. Đang chuyển sang scene: " + gameSceneName);
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
    }

    private void UpdateModeText(int id)
    {
        if (ModeNameText == null) return;
        switch (id)
        {
            case 1: ModeNameText.text = "MODE TPS"; break;
            case 2: ModeNameText.text = "MODE FPS"; break;
            case 3: ModeNameText.text = "MODE GRAVITY SWITCH"; break;
            case 4: ModeNameText.text = "MODE ONLINE"; break;
            default: ModeNameText.text = "MODE ONLINE"; break;
        }
    }

    private void SetButtonsInteractable(bool value)
    {
        if (hostButton != null) hostButton.interactable = value;
        if (clientButton != null) clientButton.interactable = value;
        if (serverButton != null) serverButton.interactable = value;
    }
}