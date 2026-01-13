using UnityEngine;
using JU.CharacterSystem.AI.HearSystem;

namespace JU.CharacterSystem.AI.Examples
{
    /// <summary>
    /// Example of <see cref="HearSystem.HearSensor"/> AI sensor.
    /// </summary>
    [AddComponentMenu("JU TPS/AI/Examples/JU AI Hear Sensor Example")]
    public class JU_AI_HearActionExample : JUCharacterAIBase
    {
        private Vector3 _heardSoundPosition;

        /// <summary>
        /// Cam biến nghe âm thanh của nhân vật AI.
        /// </summary>
        public HearSensor HearSensor;

        /// <summary>
        /// Hành động này điều khiển trí tuệ nhân tạo di chuyển đến vị trí phát ra âm thanh.
        /// </summary>
        public FollowPoint FollowHearPosition;

        // Khởi tạo trí tuệ nhân tạo và thiết lập cảm biến nghe âm thanh.
        protected override void Start()
        {
            base.Start();

            _heardSoundPosition = Character.transform.position;

            HearSensor.Setup(this);
            HearSensor.OnHear.AddListener(OnHear);
            FollowHearPosition.Setup(this);
        }

        // Dọn dẹp khi trí tuệ nhân tạo bị hủy.
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        // Cập nhật trí tuệ nhân tạo mỗi khung hình.
        protected override void Update()
        {
            base.Update();

            AIControlData control = new AIControlData();

            // Move to the heard sound position.
            FollowHearPosition.Update(_heardSoundPosition, ref control);
            Control = control;
        }

        private void OnHear(Vector3 position, GameObject source)
        {
            _heardSoundPosition = position;
            FollowHearPosition.ForceRecalculatePath(_heardSoundPosition);
        }
    }
}