using UnityEngine;
using UnityEngine.UI;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/UI/FPS Counter")]
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField] private Text FPSText;
        public float RefreshRate;
        void Start()
        {
            InvokeRepeating("UpdateFrameRateOnScreen", 0, RefreshRate);

            // Nếu FPSText chưa được gán trong Inspector, cố gắng lấy component Text trên cùng GameObject
            if (FPSText == null && GetComponent<Text>() != null) { FPSText = GetComponent<Text>(); }
        }
        public void UpdateFrameRateOnScreen()
        {
            if (FPSText != null)
            {
                FPSText.text = GetFrameRate() + "FPS";
                FPSText.color = Color.Lerp(Color.red, Color.green, GetFrameRate() / 60f);
            }
        }
        /// <summary>
        /// Lấy số khung hình trên giây hiện tại
        /// </summary>
        /// <returns></returns>
        public static int GetFrameRate()
        {
            int fps = (int)(1f / Time.unscaledDeltaTime);
            return fps;
        }
    }
}