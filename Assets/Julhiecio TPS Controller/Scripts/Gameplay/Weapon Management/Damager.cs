using System;
using System.Linq;
using UnityEngine;
using JUTPS.FX;
using JUTPS.ArmorSystem;
using JUTPSEditor.JUHeader;
using System.Collections.Generic;
using JUTPS.CharacterBrain;

namespace JUTPS
{
    /// <summary>
    /// Một bộ dò va chạm gây sát thương cho một bộ va chạm khác với <see cref="JUHealth"/> hoặc <see cref="DamageableBodyPart"/>.
    /// </summary>
    [AddComponentMenu("JU TPS/Armor System/JU Damager")]
    public class Damager : MonoBehaviour
    {
        private JUCharacterController _characterOwner;

        private float _currentHitTime;
        private Vector3 _lastPosition;
        private Vector3 _startLocalPosition;
        private Collider _oldHit;

        /// <summary>
        /// Nếu đúng, hãy hiển thị chỉ báo giao diện người dùng với lực sát thương tác động lên bộ phận va chạm còn lại.
        /// </summary>
        public bool ShowHitMarker;

        /// <summary>
        /// Nếu đúng, hãy hiển thị chỉ báo giao diện người dùng với lực sát thương tác động lên bộ phận va chạm còn lại.
        /// </summary>
        public bool DisableOnStart;

        /// <summary>
        /// Lực gây thiệt hại.
        /// </summary>
        [JUHeader("Damager Settings")]
        public float Damage;

        /// <summary>
        /// Dùng để tránh nhiều lần gọi gây sát thương, thiết lập khoảng thời gian gây sát thương cho mỗi lần tấn công.
        /// </summary>
        public float HitMinTime = 0.5f;

        /// <summary>
        /// Phát hiện va chạm bằng cách sử dụng raycast.
        /// </summary>
        [JUHeader("Damage Detection Settings")]
        public bool RaycastingMode;
        public float RaycastDistance;
        public LayerMask RaycastLayer;

        [JUHeader("Collision Detection Mode Settings")]
        public bool IgnoreRootColliders;
        public bool LockStartPosition;
        public Collider[] AllCollidersToIgnore;

        [JUHeader("FX Settings")]
        public string[] TagsToDamage = { "Untagged", "Skin", "Player", "Enemy" };
        public SurfaceAudiosWithFX[] HitParticlesList;
        public AudioSource HitSoundsAudioSource;

        [Header("Zombie can hit zombie")]
        public bool FriendlyFire = false;

        [Header("Audio")]
        public AudioClip HitPlayerSound;
        private AudioSource mAudioSource;

        public bool CanHit { get; private set; }
        public bool IsColliding { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        // Khởi tạo các giá trị mặc định cho Damager
        public Damager()
        {
            CanHit = true;

            Damage = 20;
            HitMinTime = 0.5f;
            DisableOnStart = true;
            ShowHitMarker = false;

            RaycastingMode = true;
            RaycastDistance = 0.9f;
            RaycastLayer = 1;

            IgnoreRootColliders = true;
            LockStartPosition = true;

            TagsToDamage = new string[] { "Untagged", "Skin", "Player", "Enemy" };
            HitParticlesList = new SurfaceAudiosWithFX[0];
            HitSoundsAudioSource = null;
        }

        // Khởi tạo các thành phần và thiết lập ban đầu
        private void Awake()
        {
            _startLocalPosition = transform.localPosition;
            Rigidbody = GetComponent<Rigidbody>();

            if (transform.root)
                _characterOwner = transform.root.GetComponentInChildren<JUCharacterController>();

            if (IgnoreRootColliders)
                SetupCollidersToIgnore();

            // Lấy AudioSource nếu chưa có
            if (!mAudioSource)
                mAudioSource = GetComponent<AudioSource>();
        }
        private void Start()
        {
            if (DisableOnStart)
                gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
        }

        private void OnDisable()
        {
            _oldHit = null;
        }

        private void Update()
        {
            if (LockStartPosition)
            {
                transform.localPosition = _startLocalPosition;
                if (Rigidbody)
                {
                    Rigidbody.linearVelocity = Vector3.zero;
                    Rigidbody.isKinematic = false;
                }
            }

            if (RaycastingMode)
                CheckRaycastHit();
        }

        // Xử lý va chạm vật lý
        private void OnCollisionEnter(Collision collision)
        {
            Collider collider = collision.collider;
            Vector3 point = collision.contacts[0].point;
            Vector3 normal = collision.contacts[0].normal;

            if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
                point = collider.ClosestPoint(point);

            CheckCollisionHit(collision.collider, point, normal);
            Debug.Log("Hit player collisionEnter");
        }

        // Xử lý va chạm với trigger
        private void OnTriggerEnter(Collider other)
        {
            Vector3 point = transform.position;
            Vector3 normal = -transform.forward;

            if (other is BoxCollider || other is SphereCollider || other is CapsuleCollider)
                point = other.ClosestPoint(point);

            CheckCollisionHit(other, point, normal);
            Debug.Log("Hit player triggerEnter");

        }

        // Xử lý va chạm và gây sát thương
        private void CheckCollisionHit(Collider other, Vector3 point, Vector3 normal)
        {
            if (!CanHit)
                return;

            if (!TagsToDamage.Contains(other.tag))
                return;

            // Nếu không cho phép gây sát thương đồng đội và đối tượng trúng có tag Enemy ở Root
            if (FriendlyFire == false && other.transform.root.CompareTag("Enemy"))
            {
                return; // Bỏ qua, không xử lý các dòng code gây dame bên dưới
            }

            // KIỂM TRA ĐIỀU KIỆN PHÁT ÂM THANH
            // Kiểm tra Tag là "Player" VÀ Layer là 9 (Characters layer)
            if (other.tag == "Player" && other.gameObject.layer == 9)
            {
                if (mAudioSource && HitPlayerSound)
                {
                    mAudioSource.PlayOneShot(HitPlayerSound);
                }
            }

            Debug.Log("Trúng layer= 9 trong hàm CheckCollisionHit");

            if (other.gameObject.layer == 9) // Characters layer
            {
                if (other.gameObject.GetComponentInChildren<DamageableBodyPart>() != null)
                    return;
            }

            IsColliding = true;
            DoDamage(other, point, normal, Damage, HitParticlesList, HitSoundsAudioSource);
            Invoke(nameof(DisableCollidedState), 0.1f);
            DisableDamagingForSeconds(HitMinTime);
        }

        private void CheckRaycastHit()
        {
            if (!CanHit || RaycastDistance == 0)
                return;

            var hits = Physics.RaycastAll(_lastPosition, transform.forward, RaycastDistance, RaycastLayer);
            _lastPosition = transform.position;

            var hitsCount = hits.Length;
            for (int i = 0; i < hitsCount; i++)
            {
                var hitCollider = hits[i].collider;

                // Don't apply damage on the same object multiple times.    
                if (hitCollider == _oldHit)
                    continue;

                if (AllCollidersToIgnore.Contains(hitCollider))
                    continue;

                if (TagsToDamage.Contains(hitCollider.tag))
                {
                    // Nếu không cho phép gây sát thương đồng đội và đối tượng trúng có tag Enemy ở Root
                    if (FriendlyFire == false && hitCollider.transform.root.CompareTag("Enemy"))
                    {
                        continue; // Bỏ qua Zombie này, tìm mục tiêu tiếp theo trong tia Raycast
                    }

                    IsColliding = true;

                    // KIỂM TRA ĐIỀU KIỆN PHÁT ÂM THANH
                    // Kiểm tra Tag là "Player" VÀ Layer là 9
                    if (hitCollider.tag == "Player" && hitCollider.gameObject.layer == 9)
                    {
                        if (mAudioSource && HitPlayerSound)
                        {
                            mAudioSource.PlayOneShot(HitPlayerSound);
                        }
                    }
                    Debug.Log("Trúng layer= 9 trong hàm CheckRaycastHit");


                    _oldHit = hitCollider;
                    DoDamage(hitCollider, hits[i].point, hits[i].normal, Damage, HitParticlesList, HitSoundsAudioSource);
                    Invoke(nameof(DisableCollidedState), 0.1f);
                    DisableDamagingForSeconds(HitMinTime);
                    break;
                }
            }

            if (hitsCount == 0)
                _oldHit = null;
        }

        // Thiết lập các collider để bỏ qua va chạm
        private void SetupCollidersToIgnore()
        {
            Collider thisCollider = GetComponent<Collider>();

            // Thiêt lập các collider cụ thể để bỏ qua va chạm
            if (!IgnoreRootColliders && thisCollider)
            {
                for (int i = 0; i < AllCollidersToIgnore.Length; i++)
                    if (AllCollidersToIgnore[i])
                        Physics.IgnoreCollision(AllCollidersToIgnore[i], thisCollider, true);

                return;
            }
            else if (!IgnoreRootColliders)
                return;

            // Lấy tất cả collider từ root object
            var rootCollidersList = transform.root.GetComponentsInChildren<Collider>().ToList();
            if (thisCollider)
                rootCollidersList.Remove(thisCollider);

            // Lấy tất cả collider từ root object và thêm vào danh sách bỏ qua va chạm
            var rootColliders = rootCollidersList.ToArray();
            int oldLength = AllCollidersToIgnore.Length;
            int rootLength = rootColliders.Length;

            if (oldLength > 0)
            {
                // Kết hợp hai mảng collider lại với nhau
                Array.Resize(ref AllCollidersToIgnore, oldLength + rootLength - 1);
                for (int i = 0; i < rootLength; i++)
                    AllCollidersToIgnore[Mathf.Max(oldLength - 1, 0) + i] = rootColliders[i];
            }
            else
                AllCollidersToIgnore = rootColliders;

            // Loại bỏ các collider trùng lặp
            AllCollidersToIgnore = new HashSet<Collider>(AllCollidersToIgnore).ToArray();

            // Thiêt lập các collider cụ thể để bỏ qua va chạm
            if (thisCollider)
            {
                foreach (Collider collider in AllCollidersToIgnore)
                    if (collider && collider != thisCollider)
                        Physics.IgnoreCollision(collider, thisCollider, true);
            }
        }

        // Vô hiệu hóa trạng thái va chạm sau khi đã xử lý
        private void DisableCollidedState()
        {
            _oldHit = null;
            IsColliding = false;
        }

        // Kích hoạt lại khả năng gây sát thương sau khi bị vô hiệu hóa
        private void EnableDamaging()
        {
            CanHit = true;
        }

        // Tạm thời vô hiệu hóa khả năng gây sát thương trong một khoảng thời gian
        public void DisableDamagingForSeconds(float disabledSeconds)
        {
            if (IsInvoking(nameof(EnableDamaging)))
                return;

            CanHit = false;
            Invoke(nameof(EnableDamaging), disabledSeconds);
        }

        // Gây sát thương cho collider được truyền vào khi được hit
        public void DoDamage(Collider collider, Vector3 point, Vector3 normal, float damage, SurfaceAudiosWithFX[] hitParticles, AudioSource hitAudioSource)
        {
            DamageableBodyPart bodyPart = collider.GetComponentInChildren<DamageableBodyPart>();
            float realDamage = damage;

            // Tạo thông tin sát thương
            JUHealth.DamageInfo damageInfo = new JUHealth.DamageInfo
            {
                Damage = damage,
                HitDirection = normal,
                HitPosition = point,
                HitOriginPosition = transform.position,
                HitOwner = _characterOwner ? _characterOwner.gameObject : null
            };

            // Nếu không có body part, áp dụng sát thương trực tiếp lên JUHealth
            if (!bodyPart)
            {
                JUHealth health = collider.GetComponentInParent<JUHealth>();

                if (health)
                {
                    health.DoDamage(damageInfo);
                    if (ShowHitMarker)
                        if (!health.IsDead && realDamage > 0)
                            HitMarkerEffect.HitCheck(health.transform.tag, point, realDamage);
                }
            }
            else
            {
                realDamage = bodyPart.DoDamage(damageInfo);
                if (ShowHitMarker)
                {
                    if (!bodyPart.Health.IsDead && realDamage > 0)
                        HitMarkerEffect.HitCheck(bodyPart.transform.tag, point, realDamage);
                }
            }

            // Khi va chạm, phát hiệu ứng hạt và âm thanh
            Quaternion particleRotation = Quaternion.LookRotation(normal);
            string tag = collider.tag;

            GameObject fx = SurfaceAudiosWithFX.Play(hitAudioSource, hitParticles, point, particleRotation, null, tag);

            if (fx)
                fx.transform.parent = collider.transform;
        }

        // Vẽ Gizmos để hiển thị phạm vi phát hiện sát thương
        private void OnDrawGizmos()
        {
            // Nêu đang ở chế độ Raycasting, vẽ một đường thẳng để biểu diễn tia raycast
            if (RaycastingMode)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + (transform.forward * RaycastDistance));
            }
            else
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
                Gizmos.color = new Color(1, 1, 1, 0.25f);
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }
    }

}