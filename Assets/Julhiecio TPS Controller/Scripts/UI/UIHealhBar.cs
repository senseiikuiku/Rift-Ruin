using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JUTPS;

namespace JUTPS.UI
{

    [AddComponentMenu("JU TPS/UI/UI Health Bar")]
    public class UIHealhBar : MonoBehaviour
    {
        [Header("UI Health Bar Settings")]
        [SerializeField] private JUHealth HealthComponent;
        [SerializeField] private bool IsPlayerHealthBar = true;
        [SerializeField] private Image HealthBarImage;
        [SerializeField] private float Speed = 6;
        [SerializeField] private Text HealthPointsText;

        [Header("Health Bar Color Change")]
        [SerializeField] private Color EmptyHPColor = Color.red;
        [SerializeField] private Color FullHPColor = Color.green;
        [SerializeField] private Color HPHealingColor = Color.cyan;
        [SerializeField] private Color HPLossColor = Color.yellow;
        [SerializeField] private bool ChangeHPTextColorToo = true;

        private float oldFillAmount;
        void Start()
        {
            //if (IsPlayerHealthBar)
            //{
            //    GameObject pl = GameObject.FindGameObjectWithTag("Player");
            //    HealthComponent = pl.GetComponent<JUHealth>();
            //}

            //oldFillAmount = HealthBarImage.fillAmount;

            // Dọn dẹp Start để tránh lỗi Null ban đầu
            if (HealthBarImage != null)
            {
                oldFillAmount = HealthBarImage.fillAmount;
            }
        }

        void Update()
        {
            // KIỂM TRA AN TOÀN: Nếu là thanh máu người chơi và chưa có HealthComponent
            if (IsPlayerHealthBar && HealthComponent == null)
            {
                // Sử dụng JUGameManager để lấy Player đã được Assign bởi MultiplayerCameraAssigner
                if (JUGameManager.PlayerController != null)
                {
                    HealthComponent = JUGameManager.PlayerController.GetComponent<JUHealth>();
                }

                // Nếu vẫn chưa tìm thấy thì thoát để chờ khung hình sau
                if (HealthComponent == null) return;
            }

            // Kiểm tra Image để tránh lỗi logic nếu quên kéo thả vào Inspector
            if (HealthComponent == null || HealthBarImage == null) return;

            // Logic tính toán thanh máu
            float healthValueNormalized = HealthComponent.Health / HealthComponent.MaxHealth;
            HealthBarImage.fillAmount = Mathf.MoveTowards(HealthBarImage.fillAmount, healthValueNormalized, Speed * Time.deltaTime);

            // Cập nhật màu sắc cơ bản
            HealthBarImage.color = Color.Lerp(EmptyHPColor, FullHPColor, HealthBarImage.fillAmount);

            // Cập nhật Text điểm máu
            if (HealthPointsText != null)
            {
                HealthPointsText.text = HealthComponent.Health.ToString("000") + "/" + HealthComponent.MaxHealth;
                if (ChangeHPTextColorToo) HealthPointsText.color = Color.Lerp(HealthBarImage.color, Color.white, 0.6f);
            }

            // Hiệu ứng đổi màu khi mất máu/hồi máu
            if (oldFillAmount != HealthBarImage.fillAmount)
            {
                //Health Healing
                if (oldFillAmount < HealthBarImage.fillAmount)
                {
                    HealthBarImage.color = HPHealingColor;
                }
                //Health Loss
                if (oldFillAmount > HealthBarImage.fillAmount)
                {
                    HealthBarImage.color = HPLossColor;
                }

                oldFillAmount = HealthBarImage.fillAmount;
            }

        }
    }

}