using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using JUTPS.InputEvents;
using JUTPS.GameSettings;
using JU;
using UnityEngine.SceneManagement; // Cần thiết để lấy tên Scene

namespace JUTPS.UI
{
    /// <summary>
    /// Màn hình thiết lập cài đặt trong trò chơi (Game settings screen).
    /// </summary>
    public class JU_UISettings : MonoBehaviour
    {
        /// <summary>
        /// Màn hình cài đặt điều khiển (Controls settings screen).
        /// </summary>
        [System.Serializable]
        public class ControlsUI
        {
            /// <summary>
            /// Độ nhạy xoay camera tối thiểu, không thể lớn hơn MaxRotationSensitive.
            /// </summary>
            [Min(0.1f)] public float MinRotationSensitive;

            /// <summary>
            /// Độ nhạy xoay camera tối đa, không thể nhỏ hơn MinRotationSensitive.
            /// </summary>
            [Min(0.2f)] public float MaxRotationSensitive;

            /// <summary>
            /// Thanh trượt (Slider) UI để điều chỉnh độ nhạy xoay camera.
            /// </summary>
            public Slider RotationSensitive;

            /// <summary>
            /// Nút gạt (Toggle) để đảo ngược hướng xoay dọc của camera.
            /// </summary>
            public Toggle InvertVertical;

            /// <summary>
            /// Nút gạt (Toggle) để đảo ngược hướng xoay ngang của camera.
            /// </summary>
            public Toggle InvertHorizontal;

            // Khởi tạo giá trị mặc định cho độ nhạy
            public ControlsUI()
            {
                MinRotationSensitive = 0.1f;
                MaxRotationSensitive = 10f;
            }

            // Thiết lập các giá trị ban đầu từ JUGameSettings vào UI
            internal void Setup()
            {
                if (RotationSensitive)
                {
                    RotationSensitive.minValue = MinRotationSensitive;
                    RotationSensitive.maxValue = MaxRotationSensitive;
                    RotationSensitive.value = JUGameSettings.CameraSensibility;
                    // Lắng nghe sự kiện khi kéo Slider để cập nhật cài đặt
                    RotationSensitive.onValueChanged.AddListener(OnChangeCameraSensitive);
                }

                if (InvertVertical)
                {
                    InvertVertical.isOn = JUGameSettings.CameraInvertVertical;
                    InvertVertical.onValueChanged.AddListener(OnToggleInvertCameraVertical);
                }

                if (InvertHorizontal)
                {
                    InvertHorizontal.isOn = JUGameSettings.CameraInvertHorizontal;
                    InvertHorizontal.onValueChanged.AddListener(OnToggleInvertCameraHorizontal);
                }
            }

            // Các phương thức cập nhật trực tiếp vào hệ thống JUGameSettings
            private void OnChangeCameraSensitive(float sensitive) => JUGameSettings.CameraSensibility = sensitive;
            private void OnToggleInvertCameraVertical(bool invert) => JUGameSettings.CameraInvertVertical = invert;
            private void OnToggleInvertCameraHorizontal(bool invert) => JUGameSettings.CameraInvertHorizontal = invert;
        }

        /// <summary>
        /// Màn hình cài đặt đồ họa (Graphics settings screen).
        /// </summary>
        [System.Serializable]
        public class GraphicsUI
        {
            /// <summary>
            /// Tỷ lệ render tối thiểu (độ phân giải nội bộ), không thể lớn hơn MaxRenderScale.
            /// </summary>
            [Min(0.1f)] public float MinRenderScale;

            /// <summary>
            /// Tỷ lệ render tối đa, không thể nhỏ hơn MinRenderScale.
            /// </summary>
            [Min(0.2f)] public float MaxRenderScale;

            /// <summary>
            /// Danh sách thả xuống (Dropdown) để chọn chất lượng đồ họa tổng quát.
            /// </summary>
            public Dropdown Quality;

            /// <summary>
            /// Thanh trượt (Slider) để điều chỉnh tỷ lệ render (Render Scale).
            /// </summary>
            public Slider RenderScale;

            public GraphicsUI()
            {
                MinRenderScale = 0.25f;
                MaxRenderScale = 1;
            }

            internal void Setup()
            {
                if (Quality)
                {
                    Quality.value = JUGameSettings.GraphicsQuality;
                    Quality.onValueChanged.AddListener(OnChangeQuality);
                }

                if (RenderScale)
                {
                    RenderScale.minValue = MinRenderScale;
                    RenderScale.maxValue = MaxRenderScale;
                    RenderScale.value = JUGameSettings.RenderScale;
                    RenderScale.onValueChanged.AddListener(OnChangeRenderScale);
                }
            }

            private void OnChangeQuality(int qualityIndex) => JUGameSettings.GraphicsQuality = qualityIndex;
            private void OnChangeRenderScale(float scale) => JUGameSettings.RenderScale = scale;
        }

        /// <summary>
        /// Màn hình cài đặt âm thanh (Audio settings screen).
        /// </summary>
        [System.Serializable]
        public class AudioUI
        {
            /// <summary>
            /// Thùng chứa loại âm thanh (Audio container).
            /// Dùng để định nghĩa slider cho từng nhóm như SFX, Music, UI...
            /// </summary>
            [System.Serializable]
            public struct AudioTypeContainer
            {
                [SerializeField] internal string Name; // Tên hiển thị trong Inspector

                /// <summary>
                /// Slider điều khiển âm lượng của loại âm thanh này.
                /// </summary>
                public Slider VolumeSlider;

                /// <summary>
                /// Thẻ định danh (Tag) để hệ thống biết đây là loại âm thanh nào.
                /// </summary>
                public JUTag Tag;
            }

            /// <summary>
            /// Âm lượng tối thiểu (0-1).
            /// </summary>
            [Range(0, 1)] public float MinVolume;

            /// <summary>
            /// Âm lượng tối đa (0-1).
            /// </summary>
            [Range(0, 1)] public float MaxVolume;

            /// <summary>
            /// Slider điều khiển âm lượng tổng quát (Master Volume).
            /// </summary>
            public Slider GeneralVolume;

            /// <summary>
            /// Danh sách các slider cho từng loại âm thanh riêng biệt.
            /// </summary>
            public AudioTypeContainer[] Volumes;

            public AudioUI()
            {
                MinVolume = 0;
                MaxVolume = 1;
            }

            internal void Setup()
            {
                // Thiết lập Slider âm lượng tổng
                if (GeneralVolume)
                {
                    GeneralVolume.minValue = MinVolume;
                    GeneralVolume.maxValue = MaxVolume;
                    GeneralVolume.value = JUGameSettings.AudioGeneralVolume;

                    GeneralVolume.onValueChanged.AddListener(value =>
                    {
                        JUGameSettings.AudioGeneralVolume = value;
                    });
                }

                // Thiết lập Slider cho từng nhóm âm thanh cụ thể (như Nhạc nền, Hiệu ứng...)
                foreach (var volume in Volumes)
                {
                    var slider = volume.VolumeSlider;
                    var tag = volume.Tag;

                    if (!slider) continue;

                    // Kiểm tra lỗi nếu quên chưa gán Tag trong Unity Editor
                    Debug.Assert(tag, $"{nameof(JU_UISettings)}: Thiếu Audio Tag cho slider: {volume.Name}. " +
                                     "Tag này dùng để xác định đúng loại âm lượng như SFX, UI hoặc Music.");

                    slider.value = JUGameSettings.GetAudioVolume(tag);
                    slider.minValue = MinVolume;
                    slider.maxValue = MaxVolume;

                    // Lưu giá trị vào JUGameSettings mỗi khi slider thay đổi
                    slider.onValueChanged.AddListener(value =>
                    {
                        JUGameSettings.SetAudioVolume(tag, value);
                    });
                }
            }
        }

        /// <summary>
        /// Hành động nhấn phím (Input) để thoát màn hình cài đặt thay vì dùng chuột nhấn nút.
        /// Thường là phím ESC hoặc phím Back trên tay cầm.
        /// </summary>
        public MultipleActionEvent CloseScreenAction;

        /// <summary>
        /// Nút UI (Button) dùng để thoát cài đặt.
        /// </summary>
        public Button ExitButton;

        /// <summary>
        /// Sự kiện (Event) được gọi khi màn hình bắt đầu đóng lại.
        /// </summary>
        public UnityEvent OnClose;

        // Các biến tham chiếu đến 3 mảng cài đặt chính
        public ControlsUI ControlsScreen;
        public GraphicsUI GraphicsScreen;
        public AudioUI AudioScreen;

        private void Awake()
        {
            Setup(); // Khởi tạo toàn bộ UI khi object bắt đầu thức tỉnh
        }

        private void OnEnable()
        {
            CloseScreenAction.Enable(); // Kích hoạt lắng nghe phím bấm khi UI hiển thị
        }

        private void OnDisable()
        {
            CloseScreenAction.Disable(); // Tắt lắng nghe phím bấm khi UI ẩn
        }

        private void Setup()
        {
            // Đăng ký sự kiện click chuột cho nút thoát
            if (ExitButton)
                ExitButton.onClick.AddListener(OnPressExitButton);

            // Đăng ký sự kiện nhấn phím (phím tắt) cho hành động thoát
            CloseScreenAction.OnButtonsDown.AddListener(OnPressExitButton);

            // Chạy Setup cho từng phân mục cài đặt
            ControlsScreen.Setup();
            GraphicsScreen.Setup();
            AudioScreen.Setup();
        }

        private void OnPressExitButton()
        {
            Close();
        }

        /// <summary>
        /// Thực hiện đóng màn hình cài đặt nếu nó đang mở.
        /// </summary>
        public void Close()
        {
            // KIỂM TRA: Nếu đang ở scene MenuGame thì thoát luôn, không thực hiện ẩn/đóng
            if (SceneManager.GetActiveScene().name == "MenuGame")
            {
                // Debug.Log("Đang ở MenuGame, không cho phép dùng ESC để ẩn Settings!");
                return;
            }

            // Kiểm tra nếu object đang Active thì mới xử lý
            if (!gameObject.activeSelf)
                return;

            // Kiểm tra xem có Instance của màn hình Tạm dừng (Pause) hay không
            if (JUTPS.UI.JU_UIPause.Instance != null)
            {
                // Thay vì tắt object ngay lập tức, gọi Event để JU_UIPause thực hiện hiệu ứng Fade Out (mờ dần)
                OnClose.Invoke();
            }
            else
            {
                // Nếu không có hệ thống Pause đi kèm, tắt trực tiếp object này
                gameObject.SetActive(false);
            }
        }
    }
}