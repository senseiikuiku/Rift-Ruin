using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Shaker")]
    public class Shaker : MonoBehaviour
    {
        // >>> Thuộc tính trong cửa sổ Inspector

        // Đối tượng sẽ bị rung (thường là Camera hoặc một Transform cha của Camera)
        public Transform ShakeTarget;

        // Góc xoay tối đa (độ) mà đối tượng có thể bị lệch khi rung
        [Range(0, 60)] public float MaxAngle = 5;

        // Cường độ rung tổng thể (từ 0 đến 1)
        [Range(0, 1)] public float ShakeIntensity = 1;

        // Tốc độ tăng cường độ khi bắt đầu rung (giúp hiệu ứng mượt hơn)
        [Range(0, 20)] public float ShakeStartIntensity = 3;

        // Tốc độ giảm cường độ khi kết thúc rung (giúp hiệu ứng dừng lại từ từ)
        [Range(0, 20)] public float ShakeEndIntensity = 3f;

        // Tốc độ di chuyển của rung lắc (tần số dao động)
        [Range(0, 20)] public float ShakeSpeed = 2f;

        private float CurrentTime;   // Thời gian đã trôi qua kể từ khi bắt đầu rung
        private float ShakeDuration; // Tổng thời gian rung mong muốn

        // Nếu bật, đối tượng sẽ luôn luôn rung không ngừng
        public bool AwaysShaking;

        // >>> Các thuộc tính xử lý lúc Runtime

        // Cường độ rung hiện tại (được tính toán dựa trên Start/End Intensity)
        private float CurrentShakeIntensity;

        // Trạng thái đang rung hay không (ẩn trong Inspector)
        [HideInInspector] public bool IsShaking;

        // Tọa độ ngẫu nhiên để tính toán thuật toán Perlin Noise (giúp rung ngẫu nhiên)
        private float CoordX, CoordY, CoordZ;

        // Biến lưu trữ giá trị xoay Euler cục bộ
        private float RotX, RotY, RotZ;
        private Vector3 ShakingEulerRotation;

        // Các thuộc tính Getter để lấy giá trị xoay hiện tại từ bên ngoài
        public Vector3 GetShakeLocalEulerRotation { get => ShakingEulerRotation; }
        public Quaternion GetShakeLocalRotation { get => Quaternion.Euler(ShakingEulerRotation); }

        void Start()
        {
            // Khởi tạo tọa độ ngẫu nhiên để đảm bảo mỗi lần chạy là một kiểu rung khác nhau
            CoordX = Random.Range(-1000, 1000);
            CoordY = Random.Range(-1000, 1000);
            CoordZ = Random.Range(-1000, 1000);

            // Nếu không gán ShakeTarget, mặc định sẽ là chính Transform của GameObject này
            if (ShakeTarget == null) ShakeTarget = transform;
        }

        void Update()
        {
            // Giới hạn giá trị cường độ luôn nằm trong khoảng 0-1 để tránh lỗi toán học
            CurrentShakeIntensity = Mathf.Clamp(CurrentShakeIntensity, 0, 1);

            // Sử dụng hàm bậc hai (x^2) để làm cho đường cong cường độ mượt mà hơn
            float IntensityQuadratic = CurrentShakeIntensity * CurrentShakeIntensity;

            // Tính toán thời gian dựa trên tốc độ rung
            float time = Time.time * ShakeSpeed;

            // ÁP DỤNG THUẬT TOÁN RUNG:
            // Cường độ tổng thể * Bình phương cường độ hiện tại * Góc tối đa * Giá trị ngẫu nhiên Perlin
            RotX = (ShakeIntensity * IntensityQuadratic) * MaxAngle * PerlinNoise(CoordX, time);
            RotY = (ShakeIntensity * IntensityQuadratic) * MaxAngle * PerlinNoise(CoordY, time);
            RotZ = (ShakeIntensity * IntensityQuadratic) * MaxAngle * PerlinNoise(CoordZ, time);

            // Gán giá trị xoay vào đối tượng mục tiêu
            ShakingEulerRotation.Set(RotX, RotY, RotZ);
            ShakeTarget.localEulerAngles = ShakingEulerRotation;

            // Kiểm tra logic để bắt đầu hoặc kết thúc rung
            if (!AwaysShaking)
            {
                switch (IsShaking)
                {
                    case true:
                        StartShaking();
                        break;
                    case false:
                        EndShaking();
                        break;
                }
            }
            else
            {
                StartShaking();
            }

            // Quản lý bộ đếm thời gian rung
            if (CurrentTime < ShakeDuration)
            {
                CurrentTime += Time.deltaTime;
                IsShaking = true;
            }
            else
            {
                IsShaking = false;
            }
        }

        // Giảm dần cường độ CurrentShakeIntensity về 0
        private void EndShaking()
        {
            CurrentShakeIntensity -= ShakeEndIntensity * Time.deltaTime;
        }

        // Tăng dần cường độ CurrentShakeIntensity lên 1
        private void StartShaking()
        {
            CurrentShakeIntensity += ShakeStartIntensity * Time.deltaTime;
        }

        // >>> Hệ thống Singleton/Static để truy cập nhanh từ các script khác
        private static Shaker currentCameraInstance;
        public static Shaker GetCurrentCameraInstance()
        {
            if (currentCameraInstance == null)
            {
                if (Camera.current != null)
                {
                    currentCameraInstance = Camera.current.GetComponent<Shaker>();
                    return currentCameraInstance;
                }
                else
                {
                    // Tìm kiếm Shaker trong toàn bộ Scene nếu không thấy camera hiện tại
                    return FindFirstObjectByType<Shaker>(FindObjectsInactive.Include);
                }
            }
            else
            {
                // Cập nhật instance nếu camera chính thay đổi
                if (Camera.current != null && Camera.current != currentCameraInstance.GetComponent<Camera>())
                {
                    currentCameraInstance = Camera.current.GetComponent<Shaker>();
                    return currentCameraInstance;
                }
                else
                {
                    return currentCameraInstance;
                }
            }
        }

        /// <summary>
        /// Hàm kích hoạt rung lắc với các tham số tùy chỉnh.
        /// </summary>
        public void Shake(float Speed = 3, float Duration = 0.5f, float StartIntensity = 15, float EndIntensity = 3, float MaxRotationAngle = 5, float Intensity = 1)
        {
            CurrentTime = 0; // Đặt lại thời gian về 0 để bắt đầu chu kỳ mới
            ShakeSpeed = Speed;
            ShakeDuration = Duration;
            ShakeStartIntensity = StartIntensity;
            ShakeEndIntensity = EndIntensity;
            MaxAngle = MaxRotationAngle;
            ShakeIntensity = Intensity;
            Debug.Log("Sau khi mém boom chạy Shake");
        }

        /// <summary>
        /// Hàm tạo giá trị nhiễu Perlin trong khoảng từ -1 đến 1.
        /// Giúp chuyển động rung trông tự nhiên hơn là sử dụng Random đơn thuần.
        /// </summary>
        public float PerlinNoise(float coordinate, float time)
        {
            return (1 - 2 * Mathf.PerlinNoise(coordinate + time, coordinate + time));
        }
    }
}