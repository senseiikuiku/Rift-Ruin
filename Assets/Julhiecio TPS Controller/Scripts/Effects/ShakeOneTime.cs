using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.FX
{
    /// <summary>
    /// Component dùng để thực hiện hiệu ứng rung lắc (shake) một lần duy nhất.
    /// </summary>
    [AddComponentMenu("JU TPS/FX/Shake One Time")]
    public class ShakeOneTime : MonoBehaviour
    {
        // Đối tượng Shaker cụ thể sẽ thực hiện việc rung (thường là Camera Shaker)
        public Shaker ShakerToShake;

        // Nếu tick chọn, hiệu ứng rung sẽ tự động chạy ngay khi đối tượng được khởi tạo
        public bool ShakeOnAwake = true;

        // Cường độ rung tổng thể (tỉ lệ từ 0 đến 1)
        [Range(0, 1f)] public float ShakeIntensity = 1;

        // Cường độ rung lúc bắt đầu (độ mạnh ban đầu)
        [Range(0, 50)] public float ShakeStartIntensity = 50;

        // Cường độ rung lúc kết thúc (giảm dần về mức này)
        [Range(0, 20)] public float ShakeEndIntensity = 5f;

        // Tốc độ của chuyển động rung lắc (rung nhanh hay chậm)
        [Range(0, 20)] public float ShakeSpeed = 5f;

        // Góc xoay tối đa khi rung (độ lệch lớn nhất của camera)
        [Range(0, 20)] public float MaxAngle = 15f;

        // Thời gian diễn ra hiệu ứng rung (tính bằng giây)
        [Range(0, 20)] public float ShakeDuration = 1f;

        // Bán kính ảnh hưởng của rung lắc (vật ở càng xa tâm rung thì rung càng nhẹ)
        public float ShakeRadious = 50;

        public void Start()
        {
            // Kiểm tra nếu có thiết lập rung khi bắt đầu
            if (ShakeOnAwake)
            {
                Shake(ShakeRadious);
            }
        }

        /// <summary>
        /// Hàm thực hiện kích hoạt rung lắc với bán kính tùy chỉnh.
        /// </summary>
        public void Shake(float Radious = 10)
        {
            // Trường hợp 1: Nếu không gán đối tượng Shaker cụ thể trong Inspector
            if (ShakerToShake == null)
            {
                // Tìm instance Shaker hiện tại của Camera chính
                if (Shaker.GetCurrentCameraInstance() != null)
                {
                    Shaker shakerToShake = Shaker.GetCurrentCameraInstance();
                    Debug.Log("Khi không có component Shake.cs");

                    // TÍNH TOÁN THEO KHOẢNG CÁCH:
                    // Sử dụng Mathf.Lerp để giảm cường độ rung dựa trên khoảng cách từ tâm vụ nổ/rung đến Camera.
                    // Nếu Camera nằm ngoài bán kính (Radious), cường độ sẽ bằng 0.
                    float ShakeIntensityByDistance = Mathf.Lerp(1, 0, Vector3.Distance(shakerToShake.transform.position, transform.position) / Radious);

                    // Gọi hàm Shake trên camera với cường độ đã được tính toán theo khoảng cách
                    shakerToShake.Shake(ShakeSpeed, ShakeDuration, ShakeStartIntensity, ShakeEndIntensity, MaxAngle, ShakeIntensityByDistance * ShakeIntensity);
                }
            }
            // Trường hợp 2: Đã gán trực tiếp một Shaker cụ thể
            else
            {
                // Rung trực tiếp Shaker đó với cường độ cố định (không tính khoảng cách)
                ShakerToShake.Shake(ShakeSpeed, ShakeDuration, ShakeStartIntensity, ShakeEndIntensity, MaxAngle, ShakeIntensity);
                Debug.Log("Chạy hiệu ứng rung lắc.");
            }
        }
    }
}