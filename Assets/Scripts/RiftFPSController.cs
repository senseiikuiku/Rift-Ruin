using JUTPS;
using UnityEngine;

public class RiftFPSController : JUCharacterController
{
    [Header("FPS Custom Settings")]
    public bool EnableSprintInFireMode = true;
    public bool EnableRollInFireMode = true;

    protected override void LocomotionModeController()
    {
        // FPS Mode: Fire Mode chỉ bật khi aim hoặc shoot, không ép luôn
        if (Inputs.IsAimPressed || Inputs.IsShotPressed)
        {
            FiringMode = true;
            FiringModeIK = true;
        }
        else if (EnableSprintInFireMode && (IsSprinting || IsRunning))
        {
            // Giữ Fire Mode khi sprint để IK tay đẹp, nhưng Legs layer sẽ override dưới
            FiringMode = true;
        }
        else
        {
            FiringMode = false;
            FiringModeIK = false;
        }

        // Roll luôn được phép, tạm tắt Fire Mode IK
        if (EnableRollInFireMode && IsRolling)
        {
            FiringModeIK = false;
        }
    }

    protected override void SetupDefaultLayersWeights()
    {
        base.SetupDefaultLayersWeights();

        // QUAN TRỌNG: Tắt Legs Layer khi Sprint/Roll để dùng animation Base Layer
        if ((EnableSprintInFireMode && IsSprinting) || (EnableRollInFireMode && IsRolling))
        {
            LegsLayerWeight = Mathf.Lerp(LegsLayerWeight, 0f, 10f * Time.deltaTime);
        }
        else if (FiringMode)
        {
            // Chỉ bật Legs Layer khi Fire Mode THẬT SỰ (không sprint/roll)
            LegsLayerWeight = Mathf.Lerp(LegsLayerWeight, 1f, 5f * Time.deltaTime);
        }
        else
        {
            LegsLayerWeight = Mathf.Lerp(LegsLayerWeight, 0f, 5f * Time.deltaTime);
        }
    }

    public override void ControllerInputs()
    {
        base.ControllerInputs();

        // Force Sprint khi nhấn Run + di chuyển (cho FPS mượt)
        if (EnableSprintInFireMode && Inputs.IsRunPressed && (Mathf.Abs(HorizontalX) > 0.5f || Mathf.Abs(VerticalY) > 0.5f))
        {
            IsSprinting = true;
            IsRunning = true;
            ReachedMaxSprintSpeed = false; // Infinite sprint nếu cần
        }
    }

    protected override void Movement()
    {
        base.Movement();

        // Tăng tốc độ Sprint cho FPS (tùy chỉnh)
        if (IsSprinting)
        {
            VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, 2.5f, 5f * Time.deltaTime); // 2.5x speed
        }
    }
}