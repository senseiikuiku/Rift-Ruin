using UnityEngine;

public class GravitySwitcher : JUTPSActions.JUTPSAction
{
    [Header("Cài đặt vị trí")]
    public bool EnabledGroundPlacement = true;
    public float Speed = 6f;
    public float ToGroundForce = 30;

    [Header("Cài đặt trọng lực")]
    public bool DisableGravityOnDirectionChange = true;
    // Sử dụng FixedUpdate cho các thao tác vật lý (Rigidbody)
    void FixedUpdate()
    {
        DoGroundPlacement();
    }

    protected virtual void DoGroundPlacement()
    {
        // 1. Xử lý hướng Up (Làm mượt hướng của nhân vật theo bề mặt)
        if (EnabledGroundPlacement)
        {
            // Nếu không chạm đất (GroundNormal == zero) thì trả về Vector3.up, ngược lại đi theo độ nghiêng mặt đất
            Vector3 targetUp = (TPSCharacter.GroundNormal == Vector3.zero) ? Vector3.up : TPSCharacter.GroundNormal;

            TPSCharacter.UpDirection = Vector3.Lerp(TPSCharacter.UpDirection, targetUp, Speed * Time.fixedDeltaTime);

            // 2. Xử lý lực hút xuống đất (Giúp leo dốc/cầu thang không bị nảy)
            if (TPSCharacter.IsGrounded && !TPSCharacter.IsJumping && TPSCharacter.IsMoving)
            {
                Vector3 downDirection = (TPSCharacter.GroundNormal == Vector3.zero) ? -Vector3.up : -TPSCharacter.GroundNormal;

                // Sử dụng AddForce hoặc cộng trực tiếp vận tốc
                rb.linearVelocity += downDirection * ToGroundForce * Time.fixedDeltaTime;
            }
        }
        else
        {
            // Khi không ở trên đất, trả hướng Up về mặc định
            if (!TPSCharacter.IsGrounded)
            {
                TPSCharacter.UpDirection = Vector3.Lerp(TPSCharacter.UpDirection, Vector3.up, Speed * Time.fixedDeltaTime);
            }
        }

        // 3. Tự động quản lý trọng lực hệ thống
        if (DisableGravityOnDirectionChange)
        {
            // Nếu góc nghiêng quá lớn (Dot product < 0.8), tắt trọng lực mặc định để dùng lực tùy chỉnh ở trên
            rb.useGravity = Vector3.Dot(TPSCharacter.UpDirection, Vector3.up) > 0.8f;
        }
    }
}
