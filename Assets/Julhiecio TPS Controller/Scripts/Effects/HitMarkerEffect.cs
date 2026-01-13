using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JUTPS.FX
{
    public class HitMarkerEffect : MonoBehaviour
    {
        // Singleton Instance để các script khác (như súng hoặc lựu đạn) có thể gọi nhanh
        public static HitMarkerEffect instance;

        private Image HitImage;     // Hình ảnh tâm báo trúng (Hitmarker UI)
        private AudioSource HitSound; // Nguồn âm thanh để phát tiếng "tách" khi bắn trúng

        [Header("Hit Effect")]
        // Bật/Tắt hiệu ứng hiện tâm khi bắn trúng
        public bool EnableHitEffect = true;

        // Âm thanh phát ra khi bắn trúng mục tiêu thông thường
        public AudioClip HitAudioClip;

        // Danh sách các Tag (nhãn) của đối tượng mà khi bắn trúng sẽ hiện hiệu ứng (VD: "Enemy", "Player")
        public string[] HitTags;

        // Màu sắc của tâm báo trúng khi vừa xuất hiện
        public Color HitColor = Color.white;

        // Tốc độ mờ dần của tâm báo
        public float Speed = 5;

        // Màu trắng hoàn toàn trong suốt dùng để tính toán hiệu ứng ẩn đi
        private Color ClearWhite = new Color(1, 1, 1, 0);

        [Header("Damage Count")]
        // Bật/Tắt việc hiển thị con số sát thương trên màn hình
        public bool ShowDamage;

        // Âm thanh đặc biệt phát ra khi gây sát thương chí mạng (Critical)
        public AudioClip CriticalDamageAudioClip;

        // Thành phần UI Text để hiển thị con số sát thương
        public Text DamageText;

        // Ngưỡng sát thương để được coi là chí mạng (Ví dụ: trên 50 sát thương là Critical)
        public float CriticalHitMax = 50;

        // Tốc độ mờ dần của con số sát thương
        public float TextFadeSpeed = 3;

        // Màu sắc cho số sát thương thường và số sát thương chí mạng
        public Color NormalHitColor = Color.white, CriticalHitColor = Color.red;

        // Vị trí trên thế giới 3D nơi sát thương xảy ra (để hiện con số đúng chỗ đó)
        private Vector3 HitDamagePosition;

        // Lưu trữ lượng sát thương hiện tại để xử lý hiển thị
        private float CurrentDamage;

        void Awake()
        {
            // Tự động lấy các component cần thiết trên cùng GameObject
            HitSound = GetComponent<AudioSource>();
            HitImage = GetComponent<Image>();

            // Ẩn con số sát thương khi bắt đầu game
            if (DamageText != null) DamageText.color = Color.clear;
        }

        private void OnEnable()
        {
            // Thiết lập instance khi script được kích hoạt
            instance = this;
        }

        // Update được gọi mỗi khung hình
        void Update()
        {
            // Xử lý hiệu ứng mờ dần (Fade out) cho hình ảnh tâm báo trúng
            if (HitImage != null && EnableHitEffect)
            {
                HitImage.color = Color.Lerp(HitImage.color, ClearWhite, Speed * Time.deltaTime);
            }

            // Xử lý hiệu ứng hiển thị và mờ dần cho con số sát thương
            if (ShowDamage && DamageText != null)
            {
                if (DamageText.color != ClearWhite)
                {
                    // Cập nhật vị trí của UI Text theo vị trí 3D của mục tiêu bị trúng đạn
                    JUTPS.UI.UIElementToWorldPosition.SetUIWorldPosition(DamageText.gameObject, HitDamagePosition, Vector3.zero);

                    // Làm con số sát thương mờ dần theo thời gian
                    DamageText.color = Color.Lerp(DamageText.color, ClearWhite, TextFadeSpeed * Time.deltaTime);
                }
            }
        }

        /// <summary>
        /// Hàm nội bộ thực hiện kích hoạt hiệu ứng hình ảnh và âm thanh.
        /// </summary>
        private void Hit()
        {
            // Hiện tâm báo trúng và phát âm thanh
            if (HitImage != null)
            {
                HitImage.color = HitColor; // Đặt lại màu gốc (đậm nhất) trước khi mờ dần ở Update
                HitSound.PlayOneShot(HitAudioClip);
            }

            // Xử lý con số sát thương
            if (DamageText != null && ShowDamage)
            {
                // Kiểm tra xem có phải là sát thương chí mạng không
                bool IsCriticalHit = CurrentDamage > CriticalHitMax;

                // Cập nhật nội dung text thành con số sát thương (ép kiểu sang int để không hiện số thập phân)
                DamageText.text = ((int)CurrentDamage).ToString();

                // Đổi màu chữ dựa trên loại sát thương
                DamageText.color = IsCriticalHit ? CriticalHitColor : NormalHitColor;

                // Nếu là chí mạng, ngừng âm thanh cũ và phát âm thanh chí mạng đặc trưng
                if (CriticalDamageAudioClip != null && IsCriticalHit && HitSound != null)
                {
                    HitSound.Stop();
                    HitSound.PlayOneShot(CriticalDamageAudioClip);
                }
            }
        }

        /// <summary>
        /// Hàm Static để kiểm tra va chạm từ các script bên ngoài.
        /// </summary>
        /// <param name="CollidedObjectTag">Tag của đối tượng bị bắn trúng</param>
        /// <param name="hitPosition">Vị trí điểm va chạm</param>
        /// <param name="Damage">Lượng sát thương gây ra</param>
        public static void HitCheck(string CollidedObjectTag, Vector3 hitPosition = default(Vector3), float Damage = 0)
        {
            // Nếu không có instance nào trong Scene thì thoát
            if (!instance)
                return;

            // Kiểm tra xem Tag của đối tượng bị bắn trúng có nằm trong danh sách HitTags không
            foreach (string tag in instance.HitTags)
            {
                if (CollidedObjectTag == tag)
                {
                    // Nếu khớp Tag, lưu thông tin và kích hoạt hiệu ứng
                    instance.HitDamagePosition = hitPosition;
                    instance.CurrentDamage = Damage;
                    instance.Hit();
                }
            }
        }
    }
}