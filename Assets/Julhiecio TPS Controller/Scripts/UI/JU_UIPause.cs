using System;
using System.Collections;
using JU.Editor;
using JUTPS.CameraSystems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JUTPS.UI
{
    /// <summary>
    /// Trò chơi tạm dừng hoàn toàn.
    /// </summary>
    public class JU_UIPause : MonoBehaviour
    {
        public static JU_UIPause Instance;

        private bool _defaultMouseVisible;
        private bool _defaultMouseLock;

        // Thêm biến này vào phần Header Screens
        [Header("Canvas Group Settings")]
        public CanvasGroup pauseCanvasGroup;
        public float fadeDuration = 0.3f;

        // Thêm Canvas Group cho Settings để bỏ SetActive
        private CanvasGroup settingsCanvasGroup;

        /// <summary>
        /// Tên cảnh của cảnh menu, được sử dụng khi <see cref="MainMenuButton"/> được nhấn.
        /// </summary>
        [Header("Scenes")]
        [SerializeField] private string MainMenuScene;

        /// <summary>
        /// Trò chơi tạm dừng hoàn toàn.
        /// </summary>
        [Header("Screens")]
        public GameObject PauseScreen;

        /// <summary>
        /// Bạn có thể truy cập màn hình cài đặt trò chơi thông qua màn hình tạm dừng.
        /// </summary>
        public JU_UISettings SettingsScreen;

        /// <summary>
        /// Nút "tiếp tục trò chơi" được dùng để bỏ tạm dừng trò chơi. <seealso cref="JUPauseGame.Continue"/>.
        /// </summary>
        [Header("Buttons")]
        public Button ContinueButton;
        public Button[] PlayAgainButton;

        /// <summary>
        /// Nút tạm dừng trên giao diện trò chơi.
        /// </summary>
        public Button PauseButton;

        /// <summary>
        ///Nút "cài đặt trò chơi" sẽ hiển thị màn hình cài đặt. <para/>
        /// See <seealso cref="JU_UISettings"/>
        /// </summary>
        public Button SettingsButton;

        /// <summary>
        /// Nút này dùng để quay lại menu chính của trò chơi.
        /// </summary>
        public Button MainMenuButton;

        /// <summary>
        /// Nút này dùng để đóng ứng dụng trò chơi.
        /// </summary>
        public Button ExitGameButton;

        /// <summary>
        /// Hệ thống tạm dừng trò chơi.
        /// </summary>
        public JUPauseGame PauseManager
        {
            get => JUPauseGame.Instance;
        }

        // Kiểm tra xem trò chơi có đang được tập trung hay không.
        private bool IsGameFocused
        {
#if UNITY_EDITOR
            get => JUEditor.IsGameFocused;
#else
            get => true;
#endif
        }

        // Khởi tạo và thiết lập các sự kiện.
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Lấy Canvas Group của Settings nếu có
            if (SettingsScreen != null)
                settingsCanvasGroup = SettingsScreen.GetComponent<CanvasGroup>();

            Setup();

            // Không thể thực hiện trong OnPause vì trình biên tập hiển thị con trỏ khi nhấn Escape, điều này làm hỏng logic.
            InvokeRepeating(nameof(CheckCursorVisibility), 0.1f, 0.1f);

            // Đăng ký sự kiện nhập để phát hiện khi người chơi nhấn phím.
            InputSystem.onEvent += OnPressSomething;
        }

        // Chờ đến cuối khung hình để kiểm tra trạng thái con trỏ.
        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            CheckCursorVisibility();
            StartCoroutine(FixCursorVisibility());
        }

        // Hủy đăng ký các sự kiện khi đối tượng bị hủy.
        private void OnDestroy()
        {
            Unsetup();
            InputSystem.onEvent -= OnPressSomething;
        }

        // Sửa lỗi hiển thị con trỏ khi trò chơi được tập trung lại.
        IEnumerator FixCursorVisibility()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() =>
            {
                return IsGameFocused;
            });

            if (!JUPauseGame.IsPaused)
            {
                Cursor.lockState = _defaultMouseLock ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = _defaultMouseVisible;
            }
        }

        // Xử lý sự kiện khi có bất kỳ phím nào được nhấn.
        private void OnPressSomething(InputEventPtr eventPtr, InputDevice device)
        {
            if (!(device is Keyboard keyboard))
                return;

            if (!IsGameFocused)
            {
                StartCoroutine(FixCursorVisibility());
            }
        }

        // Thiết lập các sự kiện cho các nút
        private void Setup()
        {
            // Thiết lập Pause Screen
            if (pauseCanvasGroup != null)
            {
                pauseCanvasGroup.alpha = 0;
                pauseCanvasGroup.interactable = false;
                pauseCanvasGroup.blocksRaycasts = false;
                PauseScreen.SetActive(true);
            }

            // Thiết lập Settings Screen (Dùng Canvas Group thay vì SetActive false)
            if (settingsCanvasGroup != null)
            {
                settingsCanvasGroup.alpha = 0;
                settingsCanvasGroup.interactable = false;
                settingsCanvasGroup.blocksRaycasts = false;
                SettingsScreen.gameObject.SetActive(true);
            }

            if (ContinueButton) ContinueButton.onClick.AddListener(OnPressContinueButton);
            // Vòng lặp cho Mảng Nút PlayAgain
            if (PlayAgainButton != null)
            {
                foreach (Button button in PlayAgainButton)
                {
                    if (button != null)
                    {
                        button.onClick.AddListener(OnPressPlayAgainButton);
                    }
                }
            }
            if (PauseButton) PauseButton.onClick.AddListener(OnPressPauseButton);
            if (SettingsButton) SettingsButton.onClick.AddListener(OnPressSettingsButton);
            if (MainMenuButton) MainMenuButton.onClick.AddListener(OnPressMainMenuButton);
            if (ExitGameButton) ExitGameButton.onClick.AddListener(OnPressExitGameButton);

            if (PauseManager)
            {
                PauseManager.OnPause.AddListener(OnPauseGame);
                PauseManager.OnContinue.AddListener(OnContinueGame);
            }

            if (SettingsScreen)
            {
                // Xóa các Listener cũ nếu có để tránh trùng lặp
                SettingsScreen.OnClose.RemoveAllListeners();
                SettingsScreen.OnClose.AddListener(OnCloseSettingsScreen);
            }
        }

        // Hủy đăng ký các sự kiện đã thiết lập
        private void Unsetup()
        {
            if (PauseManager)
            {
                PauseManager.OnPause.RemoveListener(OnPauseGame);
                PauseManager.OnContinue.RemoveListener(OnContinueGame);
            }
        }

        // Hàm xử lý khi đóng màn hình
        private void OnCloseSettingsScreen()
        {
            StopAllCoroutines();
            // Ẩn Settings mượt mà
            if (settingsCanvasGroup) StartCoroutine(FadeCanvas(settingsCanvasGroup, 0, false));
            // Hiện Pause mượt mà
            if (pauseCanvasGroup) StartCoroutine(FadeCanvas(pauseCanvasGroup, 1, true));

            if (PauseManager)
                PauseManager.ControlsEnabled = true;

        }

        // Xử lý khi trò chơi bị tạm dừng (Bấm Esc lần 1)
        private void OnPauseGame()
        {
            // CƯỠNG ÉP: Đảm bảo Settings luôn Active để không bị mất hiệu ứng Fade
            if (SettingsScreen != null)
            {
                SettingsScreen.gameObject.SetActive(true);
                if (settingsCanvasGroup != null)
                {
                    // Khi mới bấm Esc để vào Pause chính, ta ép Alpha Settings về 0
                    settingsCanvasGroup.alpha = 0;
                    settingsCanvasGroup.interactable = false;
                    settingsCanvasGroup.blocksRaycasts = false;
                }
            }

            if (!pauseCanvasGroup) return;

            JUCameraController.LockMouse(false, false);
            StopAllCoroutines();

            // Hiện bảng Pause chính
            StartCoroutine(FadeCanvas(pauseCanvasGroup, 1, true));
        }

        // Xử lý khi thoát Pause (Bấm Esc lần 2 hoặc bấm Continue)
        private void OnContinueGame()
        {
            // CƯỠNG ÉP: Giữ Settings Active để nó kịp chạy Fade Out
            if (SettingsScreen != null) SettingsScreen.gameObject.SetActive(true);

            if (!pauseCanvasGroup) return;

            JUCameraController.LockMouse(Lock: _defaultMouseLock, Hide: !_defaultMouseVisible);

            if (UIManager.Instance != null)
                UIManager.Instance.IsUIWinOrLose(false);

            StopAllCoroutines();

            // Fade cả Pause và Settings về 0 mượt mà
            StartCoroutine(FadeCanvas(pauseCanvasGroup, 0, false));
            if (settingsCanvasGroup) StartCoroutine(FadeCanvas(settingsCanvasGroup, 0, false));
        }



        // Xử lý khi nút tiếp tục được nhấn
        private void OnPressContinueButton()
        {
            JUPauseGame.Continue();
        }

        // Xử lý khi nút chơi lại được nhấn
        private void OnPressPlayAgainButton()
        {
            if (JUPauseGame.IsPaused)
                JUPauseGame.Continue();

            var currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
            gameObject.SetActive(false);
        }

        // Ẩn nút PlayAgain trong phân Paused nếu game đã thua hoặc thắng
        public void SetPlayAgainButtonVisible(bool isVisible)
        {
            // Đảm bảo mảng tồn tại và có ít nhất 1 phần tử
            if (PlayAgainButton != null && PlayAgainButton.Length > 0 && PlayAgainButton[0] != null)
            {
                // Kiểm soát trạng thái Active của nút [0]
                PlayAgainButton[0].gameObject.SetActive(isVisible);
            }
        }

        // Xử lý khi nút tạm dừng được nhấn
        private void OnPressPauseButton()
        {
            JUPauseGame.Pause();
        }

        // Xử lý khi bấm nút Settings (Từ bảng Pause chuyển sang Settings)
        private void OnPressSettingsButton()
        {
            if (SettingsScreen != null) SettingsScreen.gameObject.SetActive(true);
            StopAllCoroutines();

            // Hiện Settings mượt mà
            if (settingsCanvasGroup) StartCoroutine(FadeCanvas(settingsCanvasGroup, 1, true));
            // Ẩn Pause mượt mà
            if (pauseCanvasGroup) StartCoroutine(FadeCanvas(pauseCanvasGroup, 0, false));

            // Cực kỳ quan trọng: Để ControlsEnabled = true để phím Esc không bị vô hiệu hóa
            if (PauseManager) PauseManager.ControlsEnabled = true;
        }

        // Xử lý khi nút menu chính được nhấn
        private void OnPressMainMenuButton()
        {
            if (string.IsNullOrEmpty(MainMenuScene))
                return;
            Time.timeScale = 1f;
            if (JUPauseGame.Instance != null)
            {
                JUPauseGame.Continue();
            }
            SceneManager.LoadSceneAsync(MainMenuScene);
            // Disable the screen to avoid any user interaction when the game is loading another scene.
            gameObject.SetActive(false);
        }

        // Xử lý khi nút thoát trò chơi được nhấn
        private void OnPressExitGameButton()
        {
            Application.Quit();
        }

        // Kiểm tra và lưu trạng thái hiển thị con trỏ chuột
        private void CheckCursorVisibility()
        {
            if (JUPauseGame.IsPaused || !IsGameFocused)
                return;

            _defaultMouseVisible = Cursor.visible;
            _defaultMouseLock = Cursor.lockState != CursorLockMode.None;
        }

        // Thêm hàm Coroutine xử lý Fade
        private IEnumerator FadeCanvas(CanvasGroup cg, float targetAlpha, bool interactable)
        {
            cg.interactable = interactable;
            cg.blocksRaycasts = interactable;

            float startAlpha = cg.alpha;
            float time = 0;
            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
                yield return null;
            }
            cg.alpha = targetAlpha;

            // Nếu mục tiêu là ẩn đi (alpha = 0), thì sau khi fade xong có thể tắt hẳn để tối ưu
            if (targetAlpha <= 0)
            {
                // cg.gameObject.SetActive(false); // Cân nhắc dòng này nếu bạn muốn tắt hẳn
            }
        }
    }

}