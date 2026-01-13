using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace JU.CharacterSystem.AI.HearSystem
{
    /// <summary>
    /// Hãy lắng nghe môi trường xung quanh để tìm kiếm mục tiêu.
    /// </summary>
    [System.Serializable]
    public class HearSensor
    {
        // Lưu trữ và cập nhật tất cả dữ liệu từ cảm biến thính giác AI.
        private class JU_AIHearManager : MonoBehaviour
        {
            // Số lượng tối đa các cảm biến có thể được cập nhật trên mỗi khung hình.
            // Trước đây, tính năng này không cập nhật số lượng lớn cảm biến trên cùng một khung hình.
            public const int MAX_SENSORS_PER_GROUP = 10;

            private int _currentGroupToUpdateIndex;
            private List<HearSensor> _currentGroupToUpdate;

            private List<SoundData> _sounds;

            /// <summary>
            /// Các nhóm cảm biến, mỗi nhóm chỉ có thể có <see cref="MAX_SENSORS_PER_GROUP"/> số lượng cảm biến.
            /// Điểm mấu chốt là nhóm chỉ số, và giá trị là nhóm chứa các cảm biến..
            /// </summary>
            private Dictionary<int, List<HearSensor>> _sensorsGroups;

            private void Update()
            {
                if (_sensorsGroups == null || _sounds == null)
                    return;

                if (_sounds.Count == 0)
                    return;

                // Tất cả các cảm biến đã bị vô hiệu hóa.

                // Cập nhật tất cả cảm biến trong nhóm hiện tại.
                foreach (var sensor in _currentGroupToUpdate)
                {
                    // Xóa cảm biến nếu không có AI.
                    if (!sensor.AI)
                    {
                        _currentGroupToUpdate.Remove(sensor);
                        break;
                    }

                    if (!sensor.Enabled || !sensor.AI.enabled)
                        continue;

                    // Kiểm tra tất cả các âm thanh hiện có.
                    foreach (var sound in _sounds)
                    {
                        // Cảm biến đang thu nhận âm thanh của nhân vật của bạn.
                        // Bỏ qua âm thanh này.
                        if (sound.Owner && sound.Owner == sensor.AI.gameObject)
                            continue;

                        if (Vector3.Distance(sensor.AI.Center, sound.Position) > sound.Distance)
                            continue;

                        // Bỏ qua âm thanh nếu có một thẻ để bỏ qua.
                        if (sound.Tag)
                        {
                            bool ignoreSound = false;
                            for (int i = 0; i < sensor.SoundsToIgnore.Length; i++)
                            {
                                if (sensor.SoundsToIgnore[i] == sound.Tag)
                                {
                                    ignoreSound = true;
                                    break;
                                }
                            }

                            if (ignoreSound)
                                continue;
                        }

                        sensor.Alert(sound);
                        break;
                    }
                }

                // Tiếp tục với nhóm cảm biến tiếp theo trong khung hình tiếp theo.

                if (_currentGroupToUpdateIndex == _sensorsGroups.Count - 1)
                {
                    _currentGroupToUpdateIndex = 0;
                    _sounds.Clear();
                }
                else
                    _currentGroupToUpdateIndex += 1;

                _currentGroupToUpdate = _sensorsGroups[_currentGroupToUpdateIndex];
            }

            /// <summary>
            /// Thêm âm thanh mới để nghe quá trình xử lý của các cảm biến.
            /// </summary>
            /// <param name="position">Vị trí của âm thanh.</param>
            /// <param name="distance">Khoảng cách âm thanh tối đa.</param>
            /// <param name="owner">Chủ sở hữu đối tượng của âm thanh.</param>
            /// <param name="soundTag">Tag âm thanh, được AI sử dụng để lọc ra những âm thanh cần được làm nóng..</param>
            public void AddSoundSource(Vector3 position, float distance, GameObject owner, JUTag soundTag)
            {
                if (_sounds == null)
                    _sounds = new List<SoundData>();

                _sounds.Add(new SoundData
                {
                    Position = position,
                    Distance = distance,
                    Owner = owner,
                    Tag = soundTag
                });
            }

            /// <summary>
            /// Thêm cảm biến âm thanh mới để lắng nghe môi trường xung quanh.
            /// </summary>
            /// <param name="sensor"></param>
            public void AddSensor(HearSensor sensor)
            {
                // Các cảm biến được phân chia theo nhóm, tất cả các cảm biến đều được cập nhật nhưng chỉ một nhóm duy nhất được cập nhật

                // mỗi khung hình để cải thiện hiệu suất.

                // Luôn thêm vào nhóm cuối cùng. Nếu nhóm đầy, hãy tạo một nhóm mới. 

                if (_sensorsGroups == null)
                    _sensorsGroups = new Dictionary<int, List<HearSensor>>();

                if (_sensorsGroups.Count == 0)
                {
                    _currentGroupToUpdate = new List<HearSensor>(MAX_SENSORS_PER_GROUP);
                    _sensorsGroups.Add(0, _currentGroupToUpdate);
                }

                // Thêm vào nhóm cuối cùng nếu chưa đầy.
                var lastGroup = _sensorsGroups[_sensorsGroups.Count - 1];
                if (lastGroup.Count < MAX_SENSORS_PER_GROUP)
                {
                    lastGroup.Add(sensor);
                    return;
                }

                // Thêm vào nhóm mới nếu nhóm cuối cùng đã đầy.
                _sensorsGroups.Add(_sensorsGroups.Count, new List<HearSensor>());
                _sensorsGroups[_sensorsGroups.Count - 1].Add(sensor);
            }
        }

        private struct SoundData
        {
            public Vector3 Position;
            public float Distance;
            public GameObject Owner;
            public JUTag Tag;
        }

        private static JU_AIHearManager _hearManager;

        /// <summary>
        /// Nếu được bật, cảm biến sẽ lắng nghe âm thanh.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Tags âm thanh để bỏ qua.
        /// </summary>
        public JUTag[] SoundsToIgnore;

        /// <summary>
        /// Nhân vật AI sở hữu cảm biến này.
        /// </summary>
        public JUCharacterAIBase AI { get; private set; }

        /// <summary>
        /// Đang xảy ra khi cảm biến nghe thấy âm thanh.
        /// Trả về vị trí âm thanh và chủ sở hữu âm thanh (nếu có).
        /// </summary>
        public UnityEvent<Vector3, GameObject> OnHear;

        /// <summary>
        /// Tạo một cảm biến nghe mới.
        /// </summary>
        public HearSensor()
        {
            Enabled = true;
        }

        /// <summary>
        /// Thiet lập cảm biến nghe cho một nhân vật AI cụ thể.
        /// </summary>
        /// <param name="ai"></param>
        public void Setup(JUCharacterAIBase ai)
        {
            CreateManagerIfNotHave();

            AI = ai;
            _hearManager.AddSensor(this);
        }

        private void Alert(SoundData sound)
        {
            OnHear.Invoke(sound.Position, sound.Owner);
        }

        /// <summary>
        /// Thêm một nguồn âm thanh mới mà một số người có thể nghe được. <see cref="JUCharacterAIBase"/> với <see cref="HearSensor"/> cảm biến.
        /// </summary>
        /// <param name="position">Vị trí của âm thanh.</param>
        /// <param name="distance">Khoảng cách âm thanh tối đa.</param>
        /// <param name="owner">Chủ sở hữu âm thanh.</param>
        /// <param name="soundTag">Tag âm thanh, được AI sử dụng để lọc ra những âm thanh cần được nghe.</param>
        public static void AddSoundSource(Vector3 position, float distance, GameObject owner, JUTag soundTag)
        {
            if (distance == 0)
                return;

            CreateManagerIfNotHave();

            _hearManager.AddSoundSource(position, distance, owner, soundTag);
        }

        private static void CreateManagerIfNotHave()
        {
            if (_hearManager)
                return;

            _hearManager = new GameObject("JU AI Hear Manager").AddComponent<JU_AIHearManager>();
            _hearManager.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}