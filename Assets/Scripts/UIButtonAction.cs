using JUTPS.CameraSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIButtonAction : MonoBehaviour
{
    public Button playAgainButton;
    public Button menuButton;

    void Start()
    {
        // Gắn sự kiện click cho nút
        playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        menuButton.onClick.AddListener(() => UIManager.Instance.IsUIMenu(true));
    }

    public void OnPlayAgainClicked()
    {
        // Mở lại time nếu game đang bị pause
        Time.timeScale = 1f;
        JUCameraController.LockMouse(true, true);

        // Load lại scene hiện tại
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

}
