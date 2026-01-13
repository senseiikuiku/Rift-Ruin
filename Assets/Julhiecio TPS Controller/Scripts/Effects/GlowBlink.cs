using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Flashing Glow")]
    public class GlowBlink : MonoBehaviour
    {
        private Renderer[] Meshes; // Danh sách các lưới (mesh) của đối tượng và con của nó

        public Color EmissiveColor = Color.white; // Màu của ánh sáng phát ra

        [Range(0, 10)]
        public float EmissiveIntensity = 0.5f; // Cường độ phát sáng tối đa

        public float Interval = 2; // Khoảng thời gian nghỉ giữa các lần nhấp nháy
        public float Speed = 5;    // Tốc độ chuyển đổi (tăng/giảm độ sáng)

        private float EmissiveValue; // Giá trị phát sáng hiện tại (chạy từ 0 đến 1)
        private bool IsBlinking;     // Trạng thái đang nhấp nháy hay đang tắt
        private float currentime;    // Bộ đếm thời gian

        void Start()
        {
            // Tìm tất cả các thành phần Renderer (MeshRenderer hoặc SkinnedMeshRenderer)
            Meshes = transform.GetComponentsInChildren<Renderer>();

            foreach (var mesh in Meshes)
            {
                // Lặp qua tất cả vật liệu trên mỗi Mesh
                for (int i = 0; i < mesh.sharedMaterials.Length; i++)
                {
                    // Tạo một bản sao độc lập của vật liệu để tránh thay đổi vật liệu gốc trong Project
                    Material newCopyFromOriginalMaterial = Instantiate(mesh.sharedMaterials[i]);
                    mesh.sharedMaterials[i] = newCopyFromOriginalMaterial;

                    // Kích hoạt tính năng Emission (phát sáng) trên Shader
                    mesh.sharedMaterials[i].EnableKeyword("_EMISSION");
                }
            }
        }

        // Update được gọi mỗi khung hình
        void Update()
        {
            // Xử lý tăng hoặc giảm giá trị phát sáng theo thời gian thực
            if (IsBlinking)
            {
                // Tăng dần EmissiveValue lên 1
                EmissiveValue = Mathf.MoveTowards(EmissiveValue, 1, Speed * Time.deltaTime);
            }
            else
            {
                // Giảm dần EmissiveValue về 0
                EmissiveValue = Mathf.MoveTowards(EmissiveValue, 0, Speed * Time.deltaTime);
            }

            // Bộ đếm thời gian để kích hoạt hiệu ứng
            if (currentime < Interval)
            {
                currentime += Time.deltaTime;
                // Nếu đã đạt độ sáng tối đa thì chuẩn bị tắt đi
                if (EmissiveValue >= 1) IsBlinking = false;
            }
            else
            {
                // Bắt đầu quá trình nhấp nháy khi hết thời gian chờ
                IsBlinking = true;
                currentime = 0;
            }

            // Áp dụng màu sắc và cường độ phát sáng vào từng vật liệu
            foreach (var meshes in Meshes)
            {
                foreach (Material mat in meshes.materials)
                {
                    // Công thức: Màu sắc * (Tỷ lệ hiện tại * Cường độ thiết lập)
                    mat.SetColor("_EmissionColor", EmissiveColor * (EmissiveValue * EmissiveIntensity));
                }
            }
        }

        /// <summary>
        /// Tắt hoàn toàn hiệu ứng phát sáng và đặt lại màu sắc về trạng thái rỗng
        /// </summary>
        public void DisableEmission()
        {
            if (Meshes == null) return;
            foreach (var meshes in Meshes)
            {
                foreach (Material mat in meshes.sharedMaterials)
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.clear);
                }
            }
        }

        private void OnDestroy()
        {
            // Dọn dẹp hiệu ứng khi đối tượng bị hủy
            DisableEmission();
        }

        private void OnEnable()
        {
            // Đảm bảo bắt đầu từ trạng thái không phát sáng khi script được kích hoạt lại
            DisableEmission();
        }
    }
}