using UnityEngine;

namespace JU.CharacterSystem.AI.HearSystem
{
    /// <summary>
    /// Phát ra một âm thanh có thể cảnh báo các AI ở gần có sở hữu <see cref="HearSensor"/> (Cảm biến thính giác).
    /// </summary>
    [AddComponentMenu("JU TPS/AI/Hear Sensor/Sound Source")]
    public class JU_AI_SoundSource : MonoBehaviour
    {
        private float _timer;
        private float _playedTime;
        private bool _played;

        /// <summary>
        /// Khoảng cách mà AI có thể phát hiện ra âm thanh.
        /// </summary>
        [Header("Sound")]
        public float SoundDistance;

        /// <summary>
        /// Thẻ (Tag) của âm thanh.
        /// </summary>
        public JUTag SoundTag;

        /// <summary>
        /// Tự động phát âm thanh ngay khi vừa được tạo ra (instantiated).
        /// </summary>
        [Header("Automatic Play")]
        public bool PlayOnSpawn;

        /// <summary>
        /// Tự động phát âm thanh khi bị hủy kích hoạt (destroyed).
        /// </summary>
        public bool PlayOnDestroy;

        /// <summary>
        /// Tự động phát âm thanh khi component này được bật (enabled).
        /// </summary>
        public bool PlayOnEnable;

        /// <summary>
        /// Tự động phát âm thanh khi component này bị tắt (disabled).
        /// </summary>
        public bool PlayOnDisable;

        /// <summary>
        /// Thời gian lặp lại để phát âm thanh sau mỗi X giây.
        /// </summary>
        [Min(0)]
        public float RepeatRate;

        /// <summary>
        /// Thời gian tối thiểu để lặp lại âm thanh. Hữu ích khi phát âm thanh do va chạm
        /// để tránh việc gọi hàm quá nhiều lần liên tục.
        /// </summary>
        public float MinRepeatTime;

        /// <summary>
        /// Phát âm thanh khi đi vào vùng kích hoạt (trigger enter).
        /// </summary>
        [Header("Play On Collision")]
        public bool PlayOnTriggerEnter;

        /// <summary>
        /// Phát âm thanh khi rời khỏi vùng kích hoạt (trigger exit).
        /// </summary>
        public bool PlayOnTriggerExit;

        /// <summary>
        /// Phát âm thanh khi bắt đầu va chạm (collision enter).
        /// </summary>
        public bool PlayOnCollisionEnter;

        /// <summary>
        /// Phát âm thanh khi kết thúc va chạm (collision exit).
        /// </summary>
        public bool PlayOnCollisionExit;

        /// <summary>
        /// Các thẻ (tags) cần bỏ qua không tính va chạm với các đối tượng cụ thể.
        /// </summary>
        public string[] IgnoreCollisionTags;

        /// <summary>
        /// Nguồn âm thanh (AudioSource) để phát hiệu ứng, phải là một gameObject khác.
        /// </summary>
        [Header("SFX")]
        public AudioSource SfxSource;

        /// <summary>
        /// Thời gian tồn tại của hiệu ứng âm thanh (SFX) sau khi được tạo ra.
        /// </summary>
        public float SfxLifeTime;

        /// <summary>
        /// Khởi tạo thực thể.
        /// </summary>
        public JU_AI_SoundSource()
        {
            SoundDistance = 10;
            RepeatRate = 0;
            MinRepeatTime = 1;

            SfxLifeTime = 10;
        }

        private void OnEnable()
        {
            if (PlayOnEnable)
                Play();
        }

        private void OnDisable()
        {
            if (PlayOnDisable)
                Play();
        }

        private void Start()
        {
            if (PlayOnSpawn)
                Play();
        }

        private void OnDestroy()
        {
            if (PlayOnDestroy)
                Play();
        }

        private void Update()
        {
            if (_played)
                _playedTime += Time.deltaTime;

            if (RepeatRate > 0)
            {
                _timer += Time.deltaTime;
                if (_timer > RepeatRate)
                {
                    _timer = 0;
                    Play();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (PlayOnTriggerEnter && IsValidCollider(other))
                Play();
        }

        private void OnTriggerExit(Collider other)
        {
            if (PlayOnTriggerExit && IsValidCollider(other))
                Play();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!PlayOnCollisionEnter)
                return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (IsValidCollider(collision.contacts[i].otherCollider))
                {
                    Play();
                    return;
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!PlayOnCollisionExit)
                return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (IsValidCollider(collision.contacts[i].otherCollider))
                {
                    Play();
                    return;
                }
            }
        }

        private bool IsValidCollider(Collider collider)
        {
            for (int i = 0; i < IgnoreCollisionTags.Length; i++)
            {
                if (collider.CompareTag(IgnoreCollisionTags[i]))
                    return false;
            }

            return true;
        }

        public void Play()
        {
            if (_played && _playedTime < MinRepeatTime)
                return;

            _played = true;
            _playedTime = 0;
            HearSensor.AddSoundSource(transform.position, SoundDistance, gameObject, SoundTag);

            if (SfxSource)
            {
                var newSource = Instantiate(SfxSource, transform.position, transform.rotation);
                newSource.transform.SetParent(null, true);
                Destroy(newSource, SfxLifeTime);
            }
        }
    }
}