using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MenuUIManager : MonoBehaviour
{
    [Header("聊天介面 (要隱藏的 UI)")]
    [SerializeField] private GameObject chatUI;

    [Header("主按鈕")]
    [SerializeField] private Button mainButton;

    [Header("選單面板")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private CanvasGroup panelMenuCanvasGroup; // ✅ CanvasGroup 控制淡入淡出
    [SerializeField] private Button btnDeptIntro;
    [SerializeField] private Button btnSelfIntro;
    [SerializeField] private Button btnCourses;
    [SerializeField] private Button btnCloseMenu;

    [Header("內容面板")]
    [SerializeField] private GameObject panelContent;
    [SerializeField] private Text textTitle;
    [SerializeField] private Text textContent;
    [SerializeField] private Button btnCloseContent;

    [Header("打字效果")]
    [SerializeField] private float typingSpeed = 0.05f;
    private Coroutine typingCoroutine;

    [Header("介紹內容設定 (Inspector 填寫)")]
    [SerializeField][TextArea] private string deptIntroText;
    [SerializeField][TextArea] private string selfIntroText;
    [SerializeField][TextArea] private string courseIntroText;

    [Header("音效播放")]
    [SerializeField] private AudioSource audioSource;
    private Dictionary<string, AudioClip> audioClipMap = new Dictionary<string, AudioClip>();

    private bool isMenuOpen = false;

    void Start()
    {
        // 預設隱藏
        panelMenu.SetActive(false);
        panelContent.SetActive(false);

        // 主按鈕 → 開關選單
        mainButton.onClick.AddListener(ToggleMenu);

        // 關閉選單按鈕
        if (btnCloseMenu != null)
            btnCloseMenu.onClick.AddListener(CloseMenuPanel);

        // 點選各個選項 (文字來自 Inspector，音檔自動對應)
        btnDeptIntro.onClick.AddListener(() => ShowContent("歷史發展", deptIntroText));
        btnSelfIntro.onClick.AddListener(() => ShowContent("課程介紹", selfIntroText));
        btnCourses.onClick.AddListener(() => ShowContent("畢業規定", courseIntroText));

        // 關閉介紹 → 回到選單
        btnCloseContent.onClick.AddListener(() =>
        {
            panelContent.SetActive(false);
            ShowMenuPanel();
        });

        // ✅ 自動載入 Resources/Audio 下的所有音檔
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
        foreach (var clip in clips)
        {
            if (!audioClipMap.ContainsKey(clip.name))
            {
                audioClipMap[clip.name] = clip;
                Debug.Log($"🎵 已載入音檔：{clip.name}");
            }
        }
    }

    private void ToggleMenu()
    {
        if (isMenuOpen)
            CloseMenuPanel();
        else
            ShowMenuPanel();
    }

    private void ShowMenuPanel()
    {
        isMenuOpen = true;
        panelMenu.SetActive(true);
        if (chatUI != null) chatUI.SetActive(false);
    }

    private void CloseMenuPanel()
    {
        isMenuOpen = false;
        panelMenu.SetActive(false);
        if (chatUI != null) chatUI.SetActive(true);
    }

    private void ShowContent(string title, string content)
    {
        isMenuOpen = false;
        panelMenu.SetActive(false);
        panelContent.SetActive(true);

        textTitle.text = title;

        // 打字機效果
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(content));

        // ✅ 播放對應音檔
        if (audioSource != null && audioClipMap.ContainsKey(title))
        {
            audioSource.Stop();
            audioSource.clip = audioClipMap[title];
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"⚠ 沒有找到對應的音檔：{title}");
        }
    }

    private IEnumerator TypeText(string content)
    {
        textContent.text = "";
        foreach (char c in content)
        {
            textContent.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
    }

    private IEnumerator FadeInPanel(CanvasGroup canvasGroup)
    {
        float duration = 0.5f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}
