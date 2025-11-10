using JUTPS;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class EnemyFinisher : MonoBehaviour
{
    public static EnemyFinisher Instance;

    public GameObject[] EnemySpawner; // Mảng chứa các spawner của enemy


    [Header("Finisher Settings")]
    [Tooltip("Sát thương mỗi giây được áp dụng cho zombie còn lại.")]
    public float DamagePerSecond = 500f; // Đủ lớn để giết ngay cả khi máu còn nhiều

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ExecuteFinisher()
    {
        // Tìm TẤT CẢ các component JUHealth còn lại trong Scene
        JUHealth[] remainingHealths = FindObjectsByType<JUHealth>(FindObjectsSortMode.None);

        // LỌC THEO TAG "Enemy"
        // Chỉ nhắm vào những đối tượng: 
        // 1. Vẫn còn sống (!h.IsDead)
        // 2. Có Tag là "Enemy"
        var targets = remainingHealths.Where(h =>
            !h.IsDead &&
            h.gameObject.CompareTag("Enemy") // <-- ĐÃ SỬ DỤNG TAG "Enemy" CỦA BẠN
        ).ToList();

        if (targets.Count > 0)
        {
            Debug.Log($"Bắt đầu Finisher trên {targets.Count} mục tiêu còn lại.");
            StartCoroutine(ApplyDamageOverTime(targets));
        }
    }

    private IEnumerator ApplyDamageOverTime(List<JUHealth> targets)
    {
        // Sử dụng Time.timeScale để biết tốc độ giảm máu thực tế 
        float totalTime = 3f; // Giết trong 3 giây (tương đương với thời gian Slowmotion)
        float timer = 0f;

        // Tính toán sát thương mỗi lần cập nhật (FixedUpdate - mặc định 0.02s)
        // Chúng ta nên dùng Time.deltaTime để sát thương mượt hơn, nhưng Coroutine dễ quản lý hơn.

        while (timer < totalTime)
        {
            // Sát thương áp dụng trong frame này (DamagePerSecond * thời gian thực trôi qua)
            // LƯU Ý: Vì đang Slowmotion (Time.timeScale = 0.1), Time.deltaTime sẽ RẤT nhỏ.
            // Ví dụ: 0.02s * 0.1 = 0.002s. Sát thương áp dụng cũng sẽ rất nhỏ.
            float damageAmount = DamagePerSecond * Time.deltaTime;

            // Sử dụng một vòng lặp ngược để có thể xoá khỏi danh sách khi chết
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                JUHealth target = targets[i];

                if (target == null || target.IsDead)
                {
                    targets.RemoveAt(i); // Xóa khỏi danh sách nếu đã chết hoặc bị hủy
                    continue;
                }

                // Áp dụng sát thương
                // Không cần cung cấp DamageInfo chi tiết, chỉ cần lượng sát thương.
                target.DoDamage(damageAmount);
            }

            if (targets.Count == 0)
            {
                Debug.Log("Tất cả zombie còn lại đã chết.");
                yield break;
            }

            // Chờ đến Frame tiếp theo
            timer += Time.deltaTime;
            yield return null;
        }
    }

}
