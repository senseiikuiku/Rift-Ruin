using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Slow Motion")]
    public class JUSlowmotion : MonoBehaviour
    {
        // Singleton Instance để có thể gọi hiệu ứng từ bất cứ đâu mà không cần tham chiếu trực tiếp
        public static JUSlowmotion Instance;

        [Header("Slowmotion Settings")]
        // Bật hoặc tắt khả năng sử dụng hiệu ứng làm chậm thời gian
        public bool EnableSlowmotion = true;

        // Tỷ lệ thời gian khi bắt đầu làm chậm (ví dụ 0.05 là chậm gấp 20 lần)
        float SlowDownFactor = 0.05f;

        // Độ dài/Thời gian kéo dài của hiệu ứng làm chậm
        float SlowDownLenght = 1;

        protected virtual void Start()
        {
            // Khởi tạo Singleton
            Instance = this;

            // Thiết lập giá trị mặc định cho bước thời gian vật lý (Fixed Timestep)
            Time.fixedDeltaTime = 0.015f;
        }

        // Update được gọi mỗi khung hình
        protected virtual void Update()
        {
            // Nếu không cho phép slowmotion thì thoát
            if (!EnableSlowmotion) { return; }

            // Tự động tăng Time.timeScale dần dần trở lại mức 1.0 (bình thường) theo thời gian thực
            // Việc sử dụng unscaledDeltaTime giúp quá trình hồi phục này mượt mà ngay cả khi thời gian đang bị làm chậm
            Time.timeScale += (1f / SlowDownLenght) * Time.unscaledDeltaTime;

            // Giới hạn giá trị timeScale luôn nằm trong khoảng từ 0 (dừng lại) đến 1 (bình thường)
            Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);

            // Giới hạn FixedDeltaTime để tránh lỗi tính toán vật lý khi thời gian thay đổi
            Time.fixedDeltaTime = Mathf.Clamp(Time.fixedDeltaTime, 0.01f, 0.333f);
        }

        /// <summary>
        /// Thực hiện hiệu ứng làm chậm thời gian (Slow motion)
        /// </summary>
        /// <param name="timescale"> tỷ lệ thời gian khi bắt đầu (mặc định 0.1) </param>
        /// <param name="duration"> thời gian kéo dài của hiệu ứng </param>
        public static void DoSlowMotion(float timescale = 0.1f, float duration = 2)
        {
            if (Instance == null) return;

            // Kiểm tra xem hiệu ứng có đang bị vô hiệu hóa trong cài đặt không
            if (Instance.EnableSlowmotion == false)
            {
                Debug.LogWarning("Đã gọi hiệu ứng Slow Motion nhưng nó đang bị tắt (EnableSlowmotion = false)");
                return;
            }

            // Gán các thông số làm chậm
            Instance.SlowDownFactor = timescale;
            Instance.SlowDownLenght = duration;

            // Thay đổi tỷ lệ thời gian ngay lập tức
            Time.timeScale = timescale;

            // Cập nhật FixedDeltaTime tương ứng với timeScale để vật lý không bị giật (stuttering)
            Time.fixedDeltaTime = Time.timeScale * .01f;

            // Sau khoảng 40% thời gian hiệu ứng, gọi hàm để bắt đầu reset các thông số về mặc định
            Instance.Invoke("DisableSlowmotion", 0.4f * duration);
        }

        /// <summary>
        /// Thực hiện hiệu ứng làm chậm thời gian với các thông số mặc định (timescale = 0.1, duration = 2)
        /// </summary>
        public static void DoSlowMotion()
        {
            if (Instance == null) return;
            if (Instance.EnableSlowmotion == false)
            {
                Debug.LogWarning("Đã gọi hiệu ứng Slow Motion nhưng nó đang bị tắt (EnableSlowmotion = false)");
                return;
            }

            Instance.SlowDownFactor = 0.1f;
            Instance.SlowDownLenght = 2;
            Time.timeScale = Instance.SlowDownFactor;
            Time.fixedDeltaTime = Time.timeScale * .01f;

            // Tự động tắt hiệu ứng sau một khoảng thời gian dựa trên SlowDownLenght
            Instance.Invoke("DisableSlowmotion", 0.4f * Instance.SlowDownLenght);
        }

        /// <summary>
        /// Tắt hiệu ứng làm chậm và đặt lại Fixed Time Step về giá trị chuẩn 0.015f
        /// </summary>
        public void DisableSlowmotion()
        {
            SlowDownFactor = 1;
            SlowDownLenght = 1;
            Time.timeScale = 1;
            Time.fixedDeltaTime = 0.015f;
        }
    }
}