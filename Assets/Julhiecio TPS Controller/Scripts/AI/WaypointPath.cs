using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.AI
{
    public class WaypointPath : MonoBehaviour
    {
        // Danh sách các Transform của các điểm nút (Waypoints) con
        [HideInInspector] public List<Transform> WaypointsTransforms = new List<Transform>();
        // Mảng chứa vị trí (Vector3) của các điểm nút để tính toán di chuyển
        [HideInInspector] public Vector3[] WaypointPathPositions;

        [Header("Waypoints Settings")]
        // Xóa các GameObject điểm nút sau khi đã lấy được vị trí (để tối ưu bộ nhớ khi chạy game)
        public bool ClearWaypointsAfterGettingPathPositions;
        // Đảo ngược lộ trình ngay khi bắt đầu
        public bool ReverseOnStart;

        [Header("Path Gizmo Visualization")]
        // Bật/Tắt việc vẽ đường nối giữa các điểm trong cửa sổ Scene
        public bool DrawPath = true;
        public Color LineColor = new Color(1, 1, 1, 0.2f), CornerColor = new Color(0, 1, 0, 0.5f);

        // Các hành động khi đối tượng đi đến điểm cuối của lộ trình
        public enum OnEndPathAction { Stop, ReversePath, RestartPath }

        void Awake()
        {
            // Làm mới danh sách điểm nút khi bắt đầu
            RefreshWaypoints();
            if (ReverseOnStart)
            {
                System.Array.Reverse(WaypointPathPositions);
            }
        }

        private Bounds waypointBounds; // Biến lưu trữ vùng bao quanh tất cả các điểm nút

        /// <summary>
        /// Tính toán và trả về vị trí trung tâm của toàn bộ hệ thống đường dẫn
        /// </summary>
        public Vector3 GetWaypointCenter()
        {
            if (waypointBounds.center == Vector3.zero)
            {
                waypointBounds = new Bounds(WaypointPathPositions[0], Vector3.zero);
            }
            else
            {
                return waypointBounds.center;
            }

            // Mở rộng vùng bao để chứa tất cả các điểm nút
            for (int i = 1; i < WaypointPathPositions.Length; i++)
            {
                waypointBounds.Encapsulate(WaypointPathPositions[i]);
            }

            return waypointBounds.center;
        }

        /// <summary>
        /// Tìm kiếm các điểm nút con và cập nhật lại mảng vị trí
        /// </summary>
        public void RefreshWaypoints()
        {
            // Nếu chưa có điểm nào và đang ở chế độ chỉnh sửa, tự động tạo ra 2 điểm mẫu
            if (WaypointsTransforms.Count == 0 && Application.isPlaying == false)
            {
                Transform t0 = new GameObject("Waypoint").transform;
                Transform t1 = new GameObject("Waypoint (1)").transform;
                t0.position = transform.position;
                t1.position = transform.position + transform.forward * 2f;
                t0.parent = transform; t1.parent = transform;
            }

            // Sử dụng WaypointUtilities để lấy danh sách Transform và Vị trí từ các đối tượng con
            WaypointsTransforms = WaypointUtilities.GetAllWaypointsChilds(transform);
            WaypointPathPositions = WaypointUtilities.GetWaypointsPositions(transform);

            // Nếu cài đặt cho phép, xóa các GameObject con để tối ưu hiệu năng trong lúc chơi
            if (ClearWaypointsAfterGettingPathPositions == false || Application.isPlaying == false) return;

            foreach (Transform t in WaypointsTransforms)
            {
                Destroy(t.gameObject);
            }

            // Reset và tính toán lại tâm điểm của lộ trình
            waypointBounds.center = Vector3.zero;
            GetWaypointCenter();
        }

        /// <summary>
        /// Hàm Static giúp di chuyển một đối tượng bất kỳ bám theo lộ trình này
        /// </summary>
        /// <param name="gameObjectToMove">Đối tượng cần di chuyển</param>
        /// <param name="path">Mảng các vị trí điểm nút</param>
        /// <param name="currentPathCornerId">ID của điểm nút hiện tại (truyền tham chiếu)</param>
        /// <param name="Speed">Tốc độ di chuyển</param>
        /// <param name="onPathEnd">Hành động khi hết đường</param>
        public static void FollowPathTowards(GameObject gameObjectToMove, ref Vector3[] path, ref int currentPathCornerId, float Speed = 10, OnEndPathAction onPathEnd = OnEndPathAction.ReversePath)
        {
            if (path.Length == 0 || gameObjectToMove == null) return;

            // Khoảng cách tối thiểu để coi như đã chạm tới điểm nút
            float stoppingDistance = 0.1f;
            // Tính khoảng cách hiện tại từ đối tượng tới điểm nút mục tiêu
            float DistanceToNextWaypoint = Vector3.Distance(gameObjectToMove.transform.position, path[currentPathCornerId]);

            // Kiểm tra lỗi nếu ID vượt quá số lượng điểm nút
            if (path.Length - 1 < currentPathCornerId)
            {
                currentPathCornerId = 0;
            }

            // Nếu đã đủ gần điểm hiện tại, chuyển sang điểm tiếp theo trong mảng
            if (DistanceToNextWaypoint < stoppingDistance && currentPathCornerId < path.Length - 1)
            {
                currentPathCornerId++;
            }

            // Thực hiện di chuyển đối tượng dần dần tới điểm nút mục tiêu
            gameObjectToMove.transform.position = Vector3.MoveTowards(gameObjectToMove.transform.position, path[currentPathCornerId], Speed * Time.deltaTime);

            // >>> Xử lý khi đối tượng đã chạm tới điểm cuối cùng của lộ trình <<<
            if (currentPathCornerId >= path.Length - 1 && DistanceToNextWaypoint < stoppingDistance)
            {
                switch (onPathEnd)
                {
                    case OnEndPathAction.Stop:
                        // Không làm gì thêm, đối tượng sẽ dừng tại chỗ
                        break;
                    case OnEndPathAction.ReversePath:
                        // Đảo ngược mảng vị trí và bắt đầu lại từ đầu (tạo vòng lặp đi-về)
                        System.Array.Reverse(path);
                        currentPathCornerId = 0;
                        break;
                    case OnEndPathAction.RestartPath:
                        // Quay lại điểm xuất phát ban đầu ngay lập tức
                        currentPathCornerId = 0;
                        break;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!DrawPath) return;

            // Nếu không trong chế độ chạy game, tự động cập nhật lại các điểm nếu có sự thay đổi trong Hierarchy
            if (Application.isPlaying == false)
            {
                if (transform.childCount == 0) { RefreshWaypoints(); return; }

                if (transform.childCount != WaypointsTransforms.Count || WaypointPathPositions[transform.childCount - 1] != WaypointsTransforms[transform.childCount - 1].position)
                {
                    RefreshWaypoints();
                }
            }
            // Vẽ đường nối và các góc trong cửa sổ Editor
            WaypointUtilities.DrawPath(WaypointPathPositions, LineColor, CornerColor);
        }
    }
}