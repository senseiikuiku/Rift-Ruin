using UnityEngine;
using JUTPS;
using JUTPS.AI;
using JU.CharacterSystem.AI; // Namespace chính xác của FieldOfView

namespace JUTPS.AI
{
    public class ZombieDifficultyAdapter : MonoBehaviour
    {
        void Start()
        {
            ApplyDifficulty();
        }

        public void ApplyDifficulty()
        {
            if (DifficultyManager.Instance == null) return;

            ZombieDifficultySettings settings = DifficultyManager.Instance.GetCurrentSettings();

            // 1. ÁP DỤNG MÁU
            if (TryGetComponent(out JUHealth health))
            {
                // Nên dùng dấu = để gán trực tiếp con số chính xác từ Manager
                health.MaxHealth = settings.MaxHealth;
                health.Health = settings.MaxHealth; // Đặt máu hiện tại bằng máu tối đa
                health.CheckHealthState();
            }

            // 2. ÁP DỤNG TẦM NHÌN
            if (TryGetComponent(out JU_AI_Zombie aiScript))
            {
                if (aiScript.FieldOfView != null)
                {
                    // Tương tự, dùng dấu = để kiểm soát chính xác khoảng cách nhìn
                    aiScript.FieldOfView.Distance = settings.FOVDistance;
                    aiScript.FieldOfView.Angle = settings.FOVAngle;

                    // Cần gọi Setup để AI khởi tạo lại mảng dò tìm với khoảng cách mới
                    aiScript.FieldOfView.Setup(aiScript);
                }
            }

            // 3. ÁP DỤNG SÁT THƯƠNG
            Damager[] damagers = GetComponentsInChildren<Damager>();
            foreach (Damager d in damagers)
            {
                d.Damage = settings.DamagePower;
            }

            Debug.Log($"[Difficulty] {gameObject.name} thiết lập mức {settings.Name}: Máu {settings.MaxHealth}, Damage {settings.DamagePower}");
        }
    }
}