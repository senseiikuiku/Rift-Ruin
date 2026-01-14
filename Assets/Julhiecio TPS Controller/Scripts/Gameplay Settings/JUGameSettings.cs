using JU;
using UnityEngine;
using UnityEngine.Events;

namespace JUTPS.GameSettings
{
    /// <summary>
    /// Hệ thống cài đặt trò chơi, áp dụng các cấu hình trò chơi.
    /// </summary>
    public class JUGameSettings : MonoBehaviour
    {
        private static AudioListener _audioListener;

        private const string GRAPHICS_RENDER_SCALE_KEY = "SETTINGS_GRAPHICS_RENDER_SCALE";
        private const string GRAPHICS_QUALITY_KEY = "SETTINGS_GRAPHICS_QUALITY";
        private const string CONTROLS_CAMERA_INVERT_VERTICAL_KEY = "SETTINGS_CONTROLS_CAMERA_INVERT_VERTICAL";
        private const string CONTROLS_CAMERA_INVERT_HORIZONTAL_KEY = "SETTINGS_CONTROLS_CAMERA_INVERT_HORIZONTAL";
        private const string CONTROLS_CAMERA_SENSITIVE_KEY = "SETTINGS_CONTROLS_CAMERA_SENSITIVE";
        private const string AUDIO_GENERAL_VOLUME_KEY = "SETTINGS_GENERAL_AUDIO_VOLUME";

#if UNITY_ANDROID || UNITY_IOS
        private static bool IsMobile => true;
#else
        private static bool IsMobile => false;
#endif

        /// <summary>
        /// Được gọi khi các cài đặt thay đổi.
        /// </summary>
        public static event UnityAction OnChangeSettings;

        private static AudioListener AudioListener
        {
            get
            {
                if (!_audioListener || !_audioListener.isActiveAndEnabled)
                    _audioListener = FindAnyObjectByType<AudioListener>(FindObjectsInactive.Exclude);

                return _audioListener;
            }
        }

        /// <summary>
        /// Hệ số nhân độ phân giải (render scale), giá trị từ 0.1 đến 1 dựa trên kích thước cửa sổ.
        /// </summary>
        public static float RenderScale
        {
            get
            {
                return PlayerPrefs.GetFloat(GRAPHICS_RENDER_SCALE_KEY, IsMobile ? 0.75f : 1f);
            }
            set
            {
                value = Mathf.Clamp(value, 0.1f, 1f);
                PlayerPrefs.SetFloat(GRAPHICS_RENDER_SCALE_KEY, value);

                ApplyRenderScale(value);

                OnChangeSettings?.Invoke();
            }
        }

        /// <summary>
        /// Cài đặt chất lượng đồ họa hiện tại.
        /// </summary>
        public static int GraphicsQuality
        {
            get
            {
                return PlayerPrefs.GetInt(GRAPHICS_QUALITY_KEY, QualitySettings.GetQualityLevel());
            }
            set
            {
                PlayerPrefs.SetInt(GRAPHICS_QUALITY_KEY, value);
                ApplyQuality(value);

                OnChangeSettings?.Invoke();
            }
        }

        /// <summary>
        /// Đảo ngược hướng nhìn dọc của camera.
        /// </summary>
        public static bool CameraInvertVertical
        {
            get
            {
                return PlayerPrefs.GetInt(CONTROLS_CAMERA_INVERT_VERTICAL_KEY, 0) == 1 ? true : false;
            }
            set
            {
                PlayerPrefs.SetInt(CONTROLS_CAMERA_INVERT_VERTICAL_KEY, value ? 1 : 0);
                OnChangeSettings?.Invoke();
            }
        }

        /// <summary>
        /// Đảo ngược hướng nhìn ngang của camera.
        /// </summary>
        public static bool CameraInvertHorizontal
        {
            get
            {
                return PlayerPrefs.GetInt(CONTROLS_CAMERA_INVERT_HORIZONTAL_KEY, 0) == 1 ? true : false;
            }
            set
            {
                PlayerPrefs.SetInt(CONTROLS_CAMERA_INVERT_HORIZONTAL_KEY, value ? 1 : 0);
                OnChangeSettings?.Invoke();
            }
        }

        /// <summary>
        /// Độ nhạy xoay camera theo đầu vào của người dùng.
        /// </summary>
        public static float CameraSensibility
        {
            get
            {
                return PlayerPrefs.GetFloat(CONTROLS_CAMERA_SENSITIVE_KEY, 1f);
            }
            set
            {
                if (value == CameraSensibility)
                    return;

                value = Mathf.Min(value, 10);
                PlayerPrefs.SetFloat(CONTROLS_CAMERA_SENSITIVE_KEY, value);
                OnChangeSettings?.Invoke();
            }
        }

        /// <summary>
        /// Âm lượng âm thanh chung của trò chơi.
        /// </summary>
        public static float AudioGeneralVolume
        {
            get
            {
                return PlayerPrefs.GetFloat(AUDIO_GENERAL_VOLUME_KEY, 1f);
            }
            set
            {
                if (value == AudioGeneralVolume)
                    return;

                value = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(AUDIO_GENERAL_VOLUME_KEY, value);

                ApplyGeneralVolume(value);

                OnChangeSettings?.Invoke();
            }
        }

        private void Awake()
        {
            ApplySettings();
        }

        /// <summary>
        /// Áp dụng các cài đặt trò chơi.
        /// </summary>
        public static void ApplySettings()
        {
            ApplyRenderScale(RenderScale);
            ApplyQuality(GraphicsQuality);
            ApplyGeneralVolume(AudioGeneralVolume);

            OnChangeSettings?.Invoke();
        }

        /// <summary>
        /// Thiết lập âm lượng cho một loại âm thanh cụ thể, như nhạc (music), hiệu ứng (sfx), giao diện (ui)...
        /// </summary>
        /// <param name="audioTag"></param>
        /// <returns></returns>
        public static void SetAudioVolume(JUTag audioTag, float volume)
        {
            Debug.Assert(audioTag, "Thiếu Audio Tag");
            PlayerPrefs.SetFloat(GetAudioVolumeKey(audioTag), volume);
            OnChangeSettings?.Invoke();
        }

        /// <summary>
        /// Lấy âm lượng của một loại âm thanh cụ thể, như nhạc (music), hiệu ứng (sfx), giao diện (ui)...
        /// </summary>
        /// <param name="audioTag"></param>
        /// <returns></returns>
        public static float GetAudioVolume(JUTag audioTag)
        {
            Debug.Assert(audioTag, "Thiếu Audio Tag");
            return PlayerPrefs.GetFloat(GetAudioVolumeKey(audioTag), 1f);
        }

        private static string GetAudioVolumeKey(JUTag tag)
        {
            return $"SETTINGS_AUDIO_VOLUME_{tag.name}";
        }

        private static void ApplyRenderScale(float scale)
        {
            Resolution biggestResolution = Screen.resolutions[Screen.resolutions.Length - 1];
            Resolution currentResolution = Screen.currentResolution;
            Resolution targetResolution = new Resolution()
            {
                height = (int)(biggestResolution.height * scale),
                width = (int)(biggestResolution.width * scale),

#if UNITY_2022_3_OR_NEWER
                refreshRateRatio = currentResolution.refreshRateRatio
#else
                refreshRate = currentResolution.refreshRate
#endif

            };

            // Trên các thiết bị di động, chiều rộng và chiều cao bị đảo ngược
            if (!IsMobile)
            {
#if UNITY_2022_3_OR_NEWER
                Screen.SetResolution(targetResolution.width, targetResolution.height, Screen.fullScreenMode, targetResolution.refreshRateRatio);
#else
                Screen.SetResolution(targetResolution.width, targetResolution.height, Screen.fullScreen, targetResolution.refreshRate);
#endif
            }
            else
            {
#if UNITY_2022_3_OR_NEWER
                Screen.SetResolution(targetResolution.height, targetResolution.width, Screen.fullScreenMode, targetResolution.refreshRateRatio);
#else
                Screen.SetResolution(targetResolution.height, targetResolution.width, Screen.fullScreen, targetResolution.refreshRate);
#endif
            }
        }

        private static void ApplyQuality(int value)
        {
            QualitySettings.SetQualityLevel(value);
        }

        private static void ApplyGeneralVolume(float volume)
        {
            if (AudioListener)
                AudioListener.volume = volume;
        }

        /// <summary>
        /// Đặt lại Cài đặt trò chơi (Reset).
        /// Xóa tất cả dữ liệu PlayerPrefs.
        /// </summary>
        [ContextMenu("Reset Game Settings", false, 100)]
        public void ResetSettings()
        {
            PlayerPrefs.DeleteAll();

            if (Application.isPlaying)
                ApplySettings();
        }
    }
}