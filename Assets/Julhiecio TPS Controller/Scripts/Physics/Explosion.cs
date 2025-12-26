using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.DestructibleSystem;
using UnityEngine.Events;

namespace JUTPS.PhysicsScripts
{

    [AddComponentMenu("JU TPS/Physics/Explosion")]
    public class Explosion : MonoBehaviour
    {
        [Header("Explosion Settings")]
        public bool ExplodeOnAwake;
        public float ExplosionForce = 5f;
        public float ExplosionUpForce = 3f;
        public float ExplosionRadious = 5f;

        [Header("Audio")]
        public AudioClip ExplosionSound;
        private AudioSource mAudioSource;

        [Header("Damage Characters")]
        public bool DamageCharacters = false;
        public LayerMask CharacterLayer;
        public float Damage = 100;

        public UnityEvent OnExplode;

        void Start()
        {
            mAudioSource = GetComponent<AudioSource>();
            if (ExplodeOnAwake) Explode();
        }
        /// <summary>
        /// Create a explosion force with settings of arguments
        /// </summary>
        public void AddExplode(float ExplosionForce, float ExplosionUpForce, float ExplosionRadious)
        {
            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, ExplosionRadious);
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                    rb.AddExplosionForce(ExplosionForce, explosionPos, ExplosionRadious, ExplosionUpForce);
            }
        }
        /// <summary>
        /// Create a explosion force with current settings
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



            Invoke(nameof(doExplosionForce), 0.1f);
            //>>> Character Damaging
            if (DamageCharacters == false) return;

            Vector3 selfPosition = transform.position;
            Collider[] characters = Physics.OverlapSphere(selfPosition, ExplosionRadious, CharacterLayer);
            foreach (Collider hittedCharacter in characters)
            {
                //Get character
                JUTPS.CharacterBrain.JUCharacterBrain character = hittedCharacter.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
                JUHealth health = hittedCharacter.GetComponent<JUHealth>();

                if (hittedCharacter.TryGetComponent(out DestructibleObject destructible))
                {
                    destructible.FractureThisObject();
                }

                JUHealth.DamageInfo damageInfo = new JUHealth.DamageInfo
                {
                    HitPosition = hittedCharacter.bounds.ClosestPoint(selfPosition),
                    HitDirection = (selfPosition - hittedCharacter.bounds.center).normalized,
                    HitOriginPosition = selfPosition,
                    HitOwner = owner,
                };

                if (character != null)
                {
                    Debug.DrawLine(character.transform.position, selfPosition, Color.yellow, 2f, true);

                    //Check visibility
                    //Ray rayToCharacter = new Ray(transform.position + Vector3.up * 0.05f, (character.transform.position - transform.position).normalized);
                    RaycastHit viewHit; Physics.Linecast(selfPosition, character.HumanoidSpine.position, out viewHit);

                    //Avoid damage a hidden character
                    if (viewHit.collider != null)
                    {
                        //Is visible ? 
                        if (viewHit.collider.gameObject == character.gameObject)
                        {
                            float damage = (int)Mathf.Lerp(Damage, Damage / 10, Vector3.Distance(character.transform.position, selfPosition) / ExplosionRadious);
                            if (character != null)
                            {
                                damageInfo.Damage = damage;
                                character.TakeDamage(damageInfo);

                                // =========================================================================
                                //                         VỊ TRÍ MỚI CHO RUNG LẮC
                                // =========================================================================
                                // Kiểm tra xem nhân vật này có phải là Player hay không
                                if (hittedCharacter.CompareTag("Player"))
                                {
                                    // === GỌI SINGLETON ĐỂ KÍCH HOẠT RUNG LẮC ===
                                    if (CameraShakeManager.Instance != null)
                                    {
                                        // Lấy bán kính nổ từ chính Explosion.cs và truyền vào hàm TriggerExplosionShake
                                        CameraShakeManager.Instance.TriggerExplosionShake(ExplosionRadious);
                                    }
                                }
                            }
                        }
                    }
                }

                if (character == null && health != null)
                {
                    //Calculate Damage
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

            OnExplode.Invoke();

        }
        public void doExplosionForce()
        {
            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, ExplosionRadious);
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                    rb.AddExplosionForce(ExplosionForce, explosionPos, ExplosionRadious, ExplosionUpForce, ForceMode.Impulse);
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, ExplosionRadious);
        }
    }

}