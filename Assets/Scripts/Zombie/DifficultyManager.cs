using UnityEngine;

namespace JUTPS.AI
{
    // Cấu trúc lưu trữ các thiết lập độ khó cho Zombie
    [System.Serializable]
    public struct ZombieDifficultySettings
    {
        public string Name; // Tên cấu hình độ khó
        public float MaxHealth; // Máu tối đa của Zombie
        public float DamagePower; // Sức mạnh tấn công của Zombie
        public float FOVDistance; // Khoảng cách tầm nhìn
        public float FOVAngle; // Góc tầm nhìn
        public int targetKills; // Số kill cần đạt được
    }

    // Các mức độ khó có sẵn
    public enum DifficultyLevel { Easy, Medium, Hard }

    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance; // Singleton instance

        // Mức độ khó hiện tại
        public DifficultyLevel CurrentDifficulty;

        [Header("Settings Profiles")]
        public ZombieDifficultySettings EasySettings = new ZombieDifficultySettings { Name = "Easy", MaxHealth = 50, DamagePower = 10, FOVDistance = 15, FOVAngle = 60, targetKills = 30 };
        public ZombieDifficultySettings MediumSettings = new ZombieDifficultySettings { Name = "Medium", MaxHealth = 100, DamagePower = 20, FOVDistance = 25, FOVAngle = 90, targetKills = 50 };
        public ZombieDifficultySettings HardSettings = new ZombieDifficultySettings { Name = "Hard", MaxHealth = 200, DamagePower = 40, FOVDistance = 40, FOVAngle = 140, targetKills = 80 };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Giữ lại khi chuyển scene
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Lấy thiết lập hiện tại dựa trên mức độ khó
        public ZombieDifficultySettings GetCurrentSettings()
        {
            switch (CurrentDifficulty)
            {
                case DifficultyLevel.Easy: return EasySettings;
                case DifficultyLevel.Medium: return MediumSettings;
                case DifficultyLevel.Hard: return HardSettings;
                default: return MediumSettings;
            }
        }
    }
}