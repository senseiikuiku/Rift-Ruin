using UnityEngine;
using System.Collections;
using JUTPS.UI;

public class JU_UITabFadeBridge : MonoBehaviour
{
    public float fadeDuration = 0.2f;
    private CanvasGroup lastActiveGroup;

    private void OnEnable()
    {
        // Đợi 1 khung hình để JU_UITabs thiết lập xong các Tab
        StartCoroutine(LateEnable());
    }

    private IEnumerator LateEnable()
    {
        yield return null; // Chờ 1 frame
        JU_UITabs uiTabs = GetComponent<JU_UITabs>();
        if (uiTabs != null && uiTabs.Tabs.Length > 0)
        {
            FadeTab(uiTabs.Tabs[uiTabs.CurrentTabIndex]);
        }
    }

    // Hàm này kết nối với OnChangeTab của JU_UITabs
    public void FadeTab(JU_UITabs.Tab selectedTab)
    {
        CanvasGroup currentGroup = selectedTab.TabScreen.GetComponent<CanvasGroup>();

        if (currentGroup == null) return;

        // 1. Đảm bảo TabScreen luôn Active vì chúng ta dùng Alpha để ẩn hiện
        selectedTab.TabScreen.SetActive(true);

        // 2. Ẩn Tab cũ (nếu có)
        if (lastActiveGroup != null && lastActiveGroup != currentGroup)
        {
            StartCoroutine(DoFade(lastActiveGroup, lastActiveGroup.alpha, 0, false));
        }

        // 3. Hiện Tab mới
        StopAllCoroutines();
        StartCoroutine(DoFade(currentGroup, currentGroup.alpha, 1, true));

        lastActiveGroup = currentGroup;
    }

    // Coroutine để thực hiện hiệu ứng fade
    private IEnumerator DoFade(CanvasGroup group, float start, float end, bool isShowing)
    {
        float counter = 0f;

        // Nếu hiện lên, bật tương tác. Nếu ẩn đi, tắt tương tác ngay lập tức.
        if (isShowing)
        {
            // Bật tương tác ngay lập tức
            group.blocksRaycasts = true;
            // Cho phép tương tác
            group.interactable = true;
        }
        else
        {
            // Tắt tương tác ngay lập tức
            group.blocksRaycasts = false;
            // Không cho phép tương tác
            group.interactable = false;
        }

        // Thực hiện hiệu ứng fade
        while (counter < fadeDuration)
        {
            counter += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, end, counter / fadeDuration);
            yield return null;
        }

        // Đảm bảo alpha cuối cùng đúng giá trị end
        group.alpha = end;

        // Nếu ẩn xong thì có thể tắt hẳn GameObject để tối ưu hiệu năng (tùy chọn)
        if (!isShowing) group.gameObject.SetActive(false);
    }
}