using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapUIManager : MonoBehaviour
{
    [Header("主介面")]
    [SerializeField] private GameObject mainUI;

    [Header("人物模型設定 (3D 角色)")]
    [SerializeField] private Transform characterModel; // ✅ 拖你的 3D 人物
    [SerializeField] private Vector3 defaultPosition;   // ✅ 對話模式位置
    [SerializeField] private Vector3 infoPanelPosition; // ✅ 介紹時角色的位置
    [SerializeField] private float moveSpeed = 3f;

    [Header("地圖介面")]
    [SerializeField] private GameObject mapUI;
    [SerializeField] private GameObject mapArea; // 👈 地圖 Cube 區域

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
    private Coroutine moveCoroutine;

    private void Start()
    {
        // 預設狀態
        mainUI.SetActive(true);
        mapUI.SetActive(false);
        infoPanelGroup.gameObject.SetActive(false);

        if (characterModel != null)
            defaultPosition = characterModel.position;

        // 綁定按鈕
        openMapButton.onClick.AddListener(OpenMap);
        closeMapButton.onClick.AddListener(CloseMap);
        closeInfoButton.onClick.AddListener(HideBuildingInfo);
    }

    private void OpenMap()
    {
        mainUI.SetActive(false);
        mapUI.SetActive(true);
        if (mapArea != null) mapArea.SetActive(true);
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

        // ✅ 移動角色到右側（infoPanelPosition）
        if (characterModel != null)
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveCharacter(infoPanelPosition));
        }
    }

    // 關閉建築物介紹
    public void HideBuildingInfo()
    {
        if (mapArea != null) mapArea.SetActive(true); // ✅ 關閉介紹時顯示回地圖區域
        StartCoroutine(FadeOutAndDisable(infoPanelGroup, 0.5f));

        // ✅ 回到預設位置
        if (characterModel != null)
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveCharacter(defaultPosition));
        }
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

    private IEnumerator MoveCharacter(Vector3 targetPos)
    {
        Vector3 startPos = characterModel.position;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * moveSpeed;
            characterModel.position = Vector3.Lerp(startPos, targetPos, elapsed);
            yield return null;
        }

        characterModel.position = targetPos;
    }
}
