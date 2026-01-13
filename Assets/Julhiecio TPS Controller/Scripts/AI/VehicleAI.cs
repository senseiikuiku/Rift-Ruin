using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using JUTPS.VehicleSystem;
using JUTPSEditor.JUHeader;

namespace JUTPS.AI
{
    [AddComponentMenu("JU TPS/AI/Vehicle AI")]
    public class VehicleAI : MonoBehaviour
    {
        private JUWheeledVehicle vehicle; // Tham chiếu đến thành phần điều khiển xe của JU TPS

        // Dùng để cập nhật trạng thái đang di chuyển (kiểm tra vị trí cũ)
        private Vector3 oldPosition;

        [HideInInspector] public int CurrentWayPointToFollow; // Chỉ số của điểm đường (waypoint) hiện tại đang hướng tới
        [HideInInspector] public Vector3[] PathToDestination; // Mảng các điểm tạo thành lộ trình đến đích

        [Header("Destination Settings")]
        public bool EnablePathfinding; // Bật/Tắt tìm đường tự động (sử dụng NavMesh hoặc hệ thống của JU)
        public float RecalculatePathRefreshRate = 1; // Tần suất tính toán lại đường đi (giây)
        public Vector3 Destination; // Vị trí đích đến
        [JUReadOnly("EnablePathfinding", true, false)] public WaypointPath WaypointPath; // Đường đi có sẵn (nếu không dùng pathfinding)


        [Header("Vehicle Path Locomotion Settings")]
        public float DistanceToContinuePath = 2; // Khoảng cách để xác nhận đã đi qua một điểm và chuyển sang điểm tiếp theo
        public float VehicleDesacelerationIntensity = 1; // Cường độ giảm tốc độ của xe khi vào cua
        public JUVehicle.VehicleRaycastCheck FrontCheck; // Hệ thống kiểm tra va chạm phía trước xe
        public bool CheckNearestPointOnPath; // Luôn kiểm tra điểm gần nhất trên đường để tránh xe bị đi chệch hướng quá xa
        public WaypointPath.OnEndPathAction OnEndPath = WaypointPath.OnEndPathAction.Stop; // Hành động khi đi hết đường (Dừng, quay lại, hoặc lặp lại)

        [Header("Events")]
        public UnityEvent OnStartPath; // Sự kiện khi bắt đầu hành trình
        public UnityEvent OnFollowing; // Sự kiện khi đang di chuyển trên đường
        public UnityEvent OnEnded;     // Sự kiện khi đã đến đích
        private bool Started, Following, Ended;

        void Start()
        {
            // Lấy thành phần điều khiển xe bánh lốp
            vehicle = GetComponent<JUWheeledVehicle>();

            if (EnablePathfinding || WaypointPath == null)
            {
                RecalculatePath();
            }
            else
            {
                PathToDestination = WaypointPath.WaypointPathPositions;
            }

            // Lặp lại việc tính toán lộ trình theo chu kỳ đã thiết lập
            InvokeRepeating("RecalculatePath", RecalculatePathRefreshRate, RecalculatePathRefreshRate);
        }

        // Thiết lập đích đến mới cho xe từ script khác
        public void SetVehicleDestination(Vector3 destination, bool recalculatePath = true)
        {
            Destination = destination;
            RecalculatePath();
        }

        // Hàm tính toán lại lộ trình bằng JUPathFinder
        public void RecalculatePath()
        {
            if (EnablePathfinding == false) return;
            // Tính toán mảng các điểm từ vị trí hiện tại đến đích
            PathToDestination = JUPathFinder.CalculatePath(transform.position, Destination);
            // Chia nhỏ đường đi để xe di chuyển mượt mà hơn (mỗi đoạn 5 đơn vị)
            WaypointUtilities.DividePath(ref PathToDestination, 5);

            CurrentWayPointToFollow = 0;
        }

        private void Update()
        {
            // Chỉ hoạt động khi xe đang nổ máy và đang chạm đất
            if (!vehicle.IsOn || !vehicle.IsGrounded)
                return;

            // Kiểm tra vật cản phía trước
            FrontCheck.Check(vehicle.transform, transform.forward);

            // Hàm xử lý di chuyển xe theo lộ trình (Hàm quan trọng nhất)
            FollowPath(ref PathToDestination, vehicle, DistanceToContinuePath, VehicleDesacelerationIntensity, ref CurrentWayPointToFollow, OnEndPath, FrontCheck.IsCollided, CheckNearestPointOnPath);

            // Vẽ đường đi trong Scene view nếu đang bật Pathfinding
            if (EnablePathfinding)
            {
                JUPathFinder.VisualizePath(PathToDestination);
            }

            // Kiểm tra trạng thái di chuyển (Bắt đầu, Đang đi, Kết thúc)
            WaypointUtilities.FollowingState state = WaypointUtilities.GetPathFollowingState(transform, ref oldPosition, PathToDestination, CurrentWayPointToFollow, DistanceToContinuePath);

            // Kích hoạt các sự kiện Unity dựa trên trạng thái
            if (state == WaypointUtilities.FollowingState.Started && Started == false)
            {
                OnStartPath.Invoke();
                Started = true;
                Ended = false;
            }
            if (state == WaypointUtilities.FollowingState.Following)
            {
                OnFollowing.Invoke();
            }
            Following = (state == WaypointUtilities.FollowingState.Following);

            if (state == WaypointUtilities.FollowingState.Ended && Ended == false)
            {
                OnEnded.Invoke();
                Following = false;
                Started = false;
                Ended = true;
            }
        }

        /// <summary>
        /// Hàm tĩnh xử lý logic điều khiển xe (Gas, Phanh, Đánh lái) để bám theo đường
        /// </summary>
        public static void FollowPath(ref Vector3[] path, JUWheeledVehicle vehicle, float stoppingDistance, float desacelerationOnCurvesIntensity, ref int currentPathCornerId, WaypointPath.OnEndPathAction onPathEnd = WaypointPath.OnEndPathAction.Stop, bool TheresWallInVehicleFront = false, bool CheckClosestPoint = false)
        {
            if (!vehicle.IsOn || !vehicle.IsGrounded || path.Length == 0) return;

            // Reset điểm mục tiêu nếu ID vượt quá độ dài mảng
            if (path.Length - 1 < currentPathCornerId)
            {
                currentPathCornerId = 0;
            }

            // >>> TÍNH TOÁN CÁC THÔNG SỐ ĐIỀU KHIỂN <<<

            // Hướng tới điểm Waypoint hiện tại
            Vector3 TargetDirection = (path[currentPathCornerId] - vehicle.transform.position).normalized;
            // Hướng tới điểm gần nhất trên đường (để sửa lỗi xe đi chệch quá xa)
            Vector3 ClosestPointDirection = (WaypointUtilities.GetClosestPoint(vehicle.transform.position, path, 1) - vehicle.transform.position).normalized;
            // Khoảng cách tới điểm tiếp theo
            float DistanceToNextWaypoint = Vector3.Distance(vehicle.transform.position, path[currentPathCornerId]);
            // Góc lệch giữa hướng xe và hướng điểm mục tiêu (dùng để đánh lái)
            float AngleBetweenNormalAndVehicleForward = Vector3.SignedAngle(vehicle.transform.forward, TargetDirection, Vector3.up);

            // Kiểm tra xem xe có đang đi đúng hướng không (Dot product > 0 là đúng hướng)
            float RightDirectionIntensity = Vector3.Dot(vehicle.transform.forward, TargetDirection);

            // >>> KHỞI TẠO CÁC ĐẦU VÀO GIA GIẢ (Rewrited Inputs) <<<
            // Horizontal: Đánh lái trái/phải dựa trên góc lệch
            float HorizontalInput = Mathf.Clamp(AngleBetweenNormalAndVehicleForward, -90, 90) / 90 * (1 + Mathf.Clamp(RightDirectionIntensity, 0, 1));
            float VerticalInput = 0; // Gas/Lùi
            bool BrakeInput = false; // Phanh

            // Chuyển sang Waypoint tiếp theo nếu đã đủ gần điểm hiện tại
            if (DistanceToNextWaypoint + Mathf.Abs(vehicle.ForwardSpeed * 0.2f) < stoppingDistance && currentPathCornerId < path.Length - 1)
            {
                currentPathCornerId++;
            }

            // >>> XỬ LÝ DI CHUYỂN PHƯƠNG TIỆN <<<
            if (currentPathCornerId != path.Length - 1 && (DistanceToNextWaypoint * 2) > stoppingDistance)
            {
                // Tăng tốc xe
                float ClampedAngle = Mathf.Clamp(Mathf.Abs(AngleBetweenNormalAndVehicleForward), 0, 90);
                // Giảm ga khi vào cua: Góc càng lớn hoặc tốc độ càng cao thì càng giảm ga
                float DesacelerationValue = desacelerationOnCurvesIntensity * ((ClampedAngle / 360) + Mathf.Abs(vehicle.ForwardSpeed * 0.05f) / 4);
                VerticalInput = 1 - Mathf.Clamp(DesacelerationValue, -1f, 0.5f);

                VerticalInput = Mathf.Clamp(VerticalInput, -1, 1);
                BrakeInput = false;
            }

            // Phanh xe nếu xe đang đi sai hướng nghiêm trọng
            if (RightDirectionIntensity > 0.3f && vehicle.FinalVertical < -1f)
            {
                BrakeInput = true;
            }

            // Xử lý khi có vật cản phía trước: Tự động lùi và đánh lái ngược lại
            if (TheresWallInVehicleFront)
            {
                VerticalInput = -2f; // Cài số lùi
                HorizontalInput = (AngleBetweenNormalAndVehicleForward > 0) ? -1 : 1; // Đánh lái ngược hướng mục tiêu để lùi ra
                BrakeInput = false;
            }

            // Kiểm tra điểm gần nhất để "nhập làn" lại nếu xe bị văng ra khỏi lộ trình
            if (CheckClosestPoint)
            {
                Vector3 closestWaypoint = WaypointUtilities.GetClosestPoint(vehicle.transform.position, path);
                int closestCornerID = System.Array.IndexOf(path, closestWaypoint);
                if (Vector3.Distance(vehicle.transform.position, closestWaypoint) < DistanceToNextWaypoint && closestCornerID > currentPathCornerId && closestCornerID != path.Length - 1)
                {
                    currentPathCornerId = closestCornerID;
                }
            }

            // >>> XỬ LÝ KHI ĐẾN CUỐI ĐƯỜNG <<<
            if (currentPathCornerId >= path.Length - 1 && DistanceToNextWaypoint < stoppingDistance)
            {
                switch (onPathEnd)
                {
                    case WaypointPath.OnEndPathAction.Stop:
                        VerticalInput = 0;
                        BrakeInput = true;
                        break;
                    case WaypointPath.OnEndPathAction.ReversePath:
                        System.Array.Reverse(path); // Đảo ngược mảng điểm để đi về
                        currentPathCornerId = 0;
                        break;
                    case WaypointPath.OnEndPathAction.RestartPath:
                        currentPathCornerId = 0; // Quay lại điểm đầu tiên
                        break;
                }
            }

            // Gán các giá trị điều khiển giả lập vào component xe thực tế
            vehicle.Vertical = VerticalInput;
            vehicle.Horizontal = HorizontalInput;
            vehicle.Brake = BrakeInput ? 1 : 0;
        }

        // Tính toán độ chính xác hướng đi của xe đối với mục tiêu (Sử dụng Vector3.Dot)
        public static float GetVehicleRightDirectionIntensity(VehicleAI vehicle, Vector3 currentTargetPathPosition)
        {
            Vector3 TargetDirection = (currentTargetPathPosition - vehicle.transform.position).normalized;
            float RightDirectionIntensity = Vector3.Dot(vehicle.transform.forward, TargetDirection);
            return RightDirectionIntensity;
        }

#if UNITY_EDITOR
        Color randomTargetIndicatorLineColor = Color.clear;

        private void OnDrawGizmos()
        {
            // Vẽ tia Raycast kiểm tra phía trước trong Editor
            JUVehicle.VehicleGizmo.DrawRaycastHit(FrontCheck, transform, transform.forward);

            if (Application.isPlaying == false)
            {
                if (EnablePathfinding) Gizmos.DrawWireSphere(Destination, 1);
            }
            else
            {
                if (PathToDestination.Length - 1 < CurrentWayPointToFollow) return;

                if (randomTargetIndicatorLineColor == Color.clear)
                {
                    randomTargetIndicatorLineColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                }

                // Vẽ đường nối từ xe đến điểm Waypoint hiện tại để dễ quan sát AI đang định đi đâu
                Gizmos.color = randomTargetIndicatorLineColor;
                Gizmos.DrawLine(transform.position, PathToDestination[CurrentWayPointToFollow]);

                // Hiển thị nhãn "Target" tại điểm đang hướng tới
                var NewGUIStyle = JUTPSEditor.CustomEditorStyles.Toolbar();
                NewGUIStyle.normal.textColor = randomTargetIndicatorLineColor;
                UnityEditor.Handles.Label(PathToDestination[CurrentWayPointToFollow] + Vector3.up * 1, "Target", NewGUIStyle);
            }
        }
#endif
    }
}