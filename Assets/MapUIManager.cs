using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapUIManager : MonoBehaviour
{
    [Header("主介面")]
    [SerializeField] private GameObject mainUI;

    [Header("地圖介面")]
    [SerializeField] private GameObject mapUI;
    [SerializeField] private GameObject mapArea; // 👈 這是地圖 Cube 區域

    [Header("建築物介紹")]
    [SerializeField] private CanvasGroup infoPanelGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Button closeInfoButton;

    [Header("按鈕")]
    [SerializeField] private Button openMapButton;
    [SerializeField] private Button closeMapButton;

    [Header("打字機效果設定")]
    [SerializeField] private float typingSpeed = 0.05f;

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
        if (mapArea != null) mapArea.SetActive(true); // 開地圖時顯示建築物區域
    }

    private void CloseMap()
    {
        mapUI.SetActive(false);
        mainUI.SetActive(true);
    }

    // 顯示建築物介紹
    public void ShowBuildingInfo(string title, string description)
    {
        if (mapArea != null) mapArea.SetActive(false); // ✅ 顯示介紹時隱藏地圖區域

        infoPanelGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(infoPanelGroup, 0, 1, 0.5f));

        titleText.text = title;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(description));
    }

    // 關閉建築物介紹
    public void HideBuildingInfo()
    {
        if (mapArea != null) mapArea.SetActive(true); // ✅ 關閉介紹時顯示回地圖區域
        StartCoroutine(FadeOutAndDisable(infoPanelGroup, 0.5f));
    }

    private IEnumerator TypeText(string fullText)
    {
        descriptionText.text = "";
        foreach (char c in fullText)
        {
            descriptionText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

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
