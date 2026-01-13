using JUTPS.WeaponSystem;
using UnityEngine;

namespace JU.CharacterSystem.AI.HearSystem
{
    /// <summary>
    /// Phát ra cảnh báo nếu <see cref="Weapon"/> các bức ảnh dành cho nhân vật AI gần nhất có <see cref="HearSensor"/>.
    /// </summary>
    [AddComponentMenu("JU TPS/AI/Hear Sensor/Weapon Sound Source")]
    [RequireComponent(typeof(Weapon))]
    public class JU_AI_WeaponSoundSource : MonoBehaviour
    {
        private Weapon _weapon;

        /// <summary>
        /// Khoảng cách tối đa mà trí tuệ nhân tạo có thể phát hiện được khi vũ khí khai hỏa.
        /// </summary>
        public float MaxSoundDistance;

        /// <summary>
        /// Tag âm thanh.
        /// </summary>
        public JUTag SoundTag;

        /// <summary>
        /// Tạo một nguồn âm thanh vũ khí mới.
        /// </summary>
        public JU_AI_WeaponSoundSource()
        {
            MaxSoundDistance = 20;
        }

        // Khởi tạo và đăng ký sự kiện bắn súng
        private void Start()
        {
            _weapon = GetComponent<Weapon>();
            if (!_weapon)
                return;

            _weapon.OnShot.AddListener(OnShot);
        }

        // Xử lý sự kiện bắn súng
        private void OnShot()
        {
            HearSensor.AddSoundSource(_weapon.transform.position, MaxSoundDistance, _weapon.TPSOwner.gameObject, SoundTag);
        }
    }
}