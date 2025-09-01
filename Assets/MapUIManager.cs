using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapUIManager : MonoBehaviour
{
    [Header("主介面")]
    [SerializeField] private GameObject mainUI;

    [Header("地圖介面")]
    [SerializeField] private GameObject mapUI;

    [Header("建築物介紹")]
    [SerializeField] private CanvasGroup infoPanelGroup; // ✅ 用 CanvasGroup 控制透明度
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Button closeInfoButton;

    [Header("按鈕")]
    [SerializeField] private Button openMapButton;
    [SerializeField] private Button closeMapButton;

    [Header("打字機效果設定")]
    [SerializeField] private float typingSpeed = 0.05f; // 打字間隔時間

    private Coroutine typingCoroutine;

    private void Start()
    {
        // 預設狀態
        mainUI.SetActive(true);
        mapUI.SetActive(false);
        infoPanelGroup.gameObject.SetActive(false);

        // 綁定按鈕
        openMapButton.onClick.AddListener(OpenMap);
        closeMapButton.onClick.AddListener(CloseMap);
        closeInfoButton.onClick.AddListener(HideBuildingInfo);
    }

    private void OpenMap()
    {
        mainUI.SetActive(false);
        mapUI.SetActive(true);
    }

    private void CloseMap()
    {
        mapUI.SetActive(false);
        mainUI.SetActive(true);
    }

    // 顯示建築物介紹 (帶淡入 + 打字機)
    public void ShowBuildingInfo(string title, string description)
    {
        infoPanelGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(infoPanelGroup, 0, 1, 0.5f));

        titleText.text = title;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(description));
    }

    // 關閉建築物介紹 (淡出)
    public void HideBuildingInfo()
    {
        StartCoroutine(FadeOutAndDisable(infoPanelGroup, 0.5f));
    }

    // 打字機效果
    private IEnumerator TypeText(string fullText)
    {
        descriptionText.text = "";  // 清空舊文字

        foreach (char c in fullText)
        {
            descriptionText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    // 淡入淡出控制
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator FadeOutAndDisable(CanvasGroup canvasGroup, float duration)
    {
        yield return FadeCanvasGroup(canvasGroup, 1, 0, duration);
        canvasGroup.gameObject.SetActive(false);
    }
}
