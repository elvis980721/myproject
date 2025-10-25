using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MenuUIManager : MonoBehaviour
{
    [Header("聊天介面 (要隱藏的 UI)")]
    [SerializeField] private GameObject chatUI;

    [Header("人物模型設定 (3D 角色)")]
    [SerializeField] private Transform characterModel; // ✅ 拖你的 3D 人物
    [SerializeField] private Vector3 defaultPosition;   // ✅ 對話模式位置
    [SerializeField] private Vector3 menuPosition;      // ✅ 開啟選單時位置
    [SerializeField] private float moveSpeed = 3f;      // ✅ 平滑移動速度

    [Header("主按鈕")]
    [SerializeField] private Button mainButton;

    [Header("選單面板")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private CanvasGroup panelMenuCanvasGroup;
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
        // 初始化 UI 狀態
        panelMenu.SetActive(false);
        panelContent.SetActive(false);

        if (characterModel != null)
            defaultPosition = characterModel.position; // 記錄角色初始位置

        // 綁定按鈕事件
        mainButton.onClick.AddListener(ToggleMenu);
        if (btnCloseMenu != null)
            btnCloseMenu.onClick.AddListener(CloseMenuPanel);

        btnDeptIntro.onClick.AddListener(() => ShowContent("歷史發展", deptIntroText));
        btnSelfIntro.onClick.AddListener(() => ShowContent("課程介紹", selfIntroText));
        btnCourses.onClick.AddListener(() => ShowContent("畢業規定", courseIntroText));

        btnCloseContent.onClick.AddListener(() =>
        {
            panelContent.SetActive(false);
            ShowMenuPanel();
        });

        // 自動載入音檔
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

        // 角色平滑移動到右側
        if (characterModel != null)
            StartCoroutine(MoveCharacter(menuPosition));
    }

    private void CloseMenuPanel()
    {
        isMenuOpen = false;
        panelMenu.SetActive(false);
        if (chatUI != null) chatUI.SetActive(true);

        // 角色回原位
        if (characterModel != null)
            StartCoroutine(MoveCharacter(defaultPosition));
    }

    private void ShowContent(string title, string content)
    {
        isMenuOpen = false;
        panelMenu.SetActive(false);
        panelContent.SetActive(true);

        textTitle.text = title;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(content));

        PlayAudio(title);
    }

    private void PlayAudio(string title)
    {
        if (audioSource == null) return;

        string cleanTitle = title.Replace(" ", ""); // 移除空格
        if (audioClipMap.TryGetValue(cleanTitle, out AudioClip clip))
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"⚠ 沒有找到對應的音檔：{cleanTitle}");
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

    // ✅ 平滑移動角色
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
