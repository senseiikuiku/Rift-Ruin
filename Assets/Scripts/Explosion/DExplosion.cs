using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.DestructibleSystem;
using UnityEngine.Events;

namespace JUTPS.PhysicsScripts
{
    public class DExplosion : MonoBehaviour
    {
        [Header("Explosion Settings")]
        public bool ExplodeOnAwake; // Nổ ngay khi đối tượng được khởi tạo
        public float ExplosionForce = 5f; // Lực nổ
        public float ExplosionUpForce = 3f; // Lực đẩy lên phía trên khi nổ
        public float ExplosionRadious = 5f; // Bán kính nổ

        [Header("Audio")]
        public AudioClip ExplosionSound; // Âm thanh nổ
        private AudioSource mAudioSource;

        [Header("Damage Characters")]
        public bool DamageCharacters = false; // Có gây sát thương cho nhân vật không
        public LayerMask CharacterLayer; // Lớp (Layer) của nhân vật
        public float Damage = 100; // Sát thương cơ bản

        public UnityEvent OnExplode; // Sự kiện kích hoạt khi nổ

        void Start()
        {
            mAudioSource = GetComponent<AudioSource>();
            if (ExplodeOnAwake) Explode();
        }

        /// <summary>
        /// Tạo một lực nổ với các thiết lập truyền vào từ tham số
        /// </summary>
        public void AddExplode(float ExplosionForce, float ExplosionUpForce, float ExplosionRadious)
        {
            Vector3 explosionPos = transform.position;
            // Tìm tất cả các Collider trong phạm vi nổ
            Collider[] colliders = Physics.OverlapSphere(explosionPos, ExplosionRadious);
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                    // Áp dụng lực nổ vật lý
                    rb.AddExplosionForce(ExplosionForce, explosionPos, ExplosionRadious, ExplosionUpForce);
            }
        }

        /// <summary>
        /// Tạo một vụ nổ với các thiết lập hiện tại của Component
        /// </summary>
        public void Explode(GameObject owner = null)
        {
            // Phát âm thanh khi nổ
            if (ExplosionSound != null && mAudioSource != null)
            {
                // Sử dụng PlayOneShot. Nó sẽ tự động lấy Volume từ mAudioSource.
                mAudioSource.PlayOneShot(ExplosionSound);
                // Nếu muốn phát ra xa hơn, bạn vẫn có thể dùng PlayClipAtPoint,
                // nhưng sẽ phải lấy mAudioSource.volume để truyền vào:
                // AudioSource.PlayClipAtPoint(ExplosionSound, transform.position, mAudioSource.volume); 
            }

            // Gọi hàm thực thi lực nổ sau 0.1 giây
            Invoke(nameof(doExplosionForce), 0.1f);

            //>>> Xử lý gây sát thương cho nhân vật
            if (DamageCharacters == false) return;

            Vector3 selfPosition = transform.position;
            // Tìm các nhân vật trong phạm vi nổ dựa trên LayerMask
            Collider[] characters = Physics.OverlapSphere(selfPosition, ExplosionRadious, CharacterLayer);
            foreach (Collider hittedCharacter in characters)
            {
                // Lấy thông tin nhân vật và máu
                JUTPS.CharacterBrain.JUCharacterBrain character = hittedCharacter.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
                JUHealth health = hittedCharacter.GetComponent<JUHealth>();

                // Nếu đối tượng có thể phá hủy (Destructible), thực hiện phá vỡ nó
                if (hittedCharacter.TryGetComponent(out DestructibleObject destructible))
                {
                    destructible.FractureThisObject();
                }

                // Khởi tạo thông tin sát thương
                JUHealth.DamageInfo damageInfo = new JUHealth.DamageInfo
                {
                    HitPosition = hittedCharacter.bounds.ClosestPoint(selfPosition),
                    HitDirection = (selfPosition - hittedCharacter.bounds.center).normalized,
                    HitOriginPosition = selfPosition,
                    HitOwner = owner,
                };

                if (character != null)
                {
                    // Vẽ đường kẻ hỗ trợ debug trong Editor
                    Debug.DrawLine(character.transform.position, selfPosition, Color.yellow, 2f, true);

                    // Kiểm tra tầm nhìn (Visibility)
                    // Raycast để đảm bảo nhân vật không bị che khuất bởi tường/vật cản
                    RaycastHit viewHit;
                    Physics.Linecast(selfPosition, character.HumanoidSpine.position, out viewHit);

                    // Tránh gây sát thương cho nhân vật bị ẩn sau vật cản
                    if (viewHit.collider != null)
                    {
                        // Kiểm tra xem tia quét có trúng trực tiếp nhân vật hay không
                        if (viewHit.collider.gameObject == character.gameObject)
                        {
                            // Tính toán sát thương giảm dần theo khoảng cách (Lerp từ Max Damage về 1/10 Damage)
                            float damage = (int)Mathf.Lerp(Damage, Damage / 10, Vector3.Distance(character.transform.position, selfPosition) / ExplosionRadious);

                            if (character != null)
                            {
                                damageInfo.Damage = damage;
                                character.TakeDamage(damageInfo);

                                // =========================================================================
                                //                     VỊ TRÍ MỚI CHO RUNG LẮC
                                // =========================================================================
                                // Kiểm tra xem nhân vật này có phải là Player hay không
                                if (hittedCharacter.CompareTag("Player"))
                                {
                                    // === GỌI SINGLETON ĐỂ KÍCH HOẠT RUNG LẮC ===
                                    if (CameraShakeManager.Instance != null)
                                    {
                                        // Truyền bán kính nổ vào để tính độ rung
                                        CameraShakeManager.Instance.TriggerExplosionShake(ExplosionRadious);
                                    }
                                }
                            }
                        }
                    }
                }

                // Xử lý cho các đối tượng có Component Health nhưng không phải CharacterBrain
                if (character == null && health != null)
                {
                    // Tính toán sát thương
                    float damage = (int)Mathf.Lerp(Damage, Damage / 10, Vector3.Distance(health.transform.position, selfPosition) / ExplosionRadious);
                    damageInfo.Damage = damage;
                    health.DoDamage(damageInfo);

                    if (hittedCharacter.CompareTag("Player"))
                    {
                        if (CameraShakeManager.Instance != null)
                        {
                            // Rung lắc chỉ khi Player bị dính nổ (Trường hợp này không kiểm tra tầm nhìn)
                            CameraShakeManager.Instance.TriggerExplosionShake(ExplosionRadious);
                        }
                    }
                }
            }

            // Kích hoạt sự kiện Unity Event
            OnExplode.Invoke();
        }

        /// <summary>
        /// Thực thi áp dụng lực nổ vật lý lên các Rigidbody
        /// </summary>
        public void doExplosionForce()
        {
            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, ExplosionRadious);
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                    // Sử dụng ForceMode.Impulse để tạo lực đẩy tức thời
                    rb.AddExplosionForce(ExplosionForce, explosionPos, ExplosionRadious, ExplosionUpForce, ForceMode.Impulse);
            }
        }

        // Vẽ vòng tròn đại diện bán kính nổ trong cửa sổ Scene (Gizmos)
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, ExplosionRadious);
        }
    }
}