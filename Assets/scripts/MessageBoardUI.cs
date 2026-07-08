using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MessageBoardUI : MonoBehaviour
{
    [SerializeField]
    private ScrollRect scrollRect;

    [SerializeField]
    private RectTransform contentRoot;

    [SerializeField]
    private MessageBoardEntryUI entryPrefab;

    [SerializeField]
    private int maxEntries = 50;

    [SerializeField]
    private bool scrollToBottomOnNewMessage = true;

    private void Awake()
    {
        ResolveReferences();
        EnsureContentLayout();
    }

    public void ResolveReferences()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollRect == null)
            scrollRect = FindFirstObjectByType<ScrollRect>(FindObjectsInactive.Include);

        if (scrollRect != null && contentRoot == null)
            contentRoot = scrollRect.content;

        if (entryPrefab == null)
            entryPrefab = Resources.Load<MessageBoardEntryUI>("MessageBoardEntry");

#if UNITY_EDITOR
        if (entryPrefab == null)
        {
            entryPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<MessageBoardEntryUI>(
                "Assets/Prefab/MessageBoardEntry.prefab");
        }
#endif

        if (contentRoot == null)
            Debug.LogError("[MessageBoardUI] 未找到 Content，请在 Scroll View 上挂载本组件并指定 Content。");

        if (entryPrefab == null)
            Debug.LogError("[MessageBoardUI] 未找到 MessageBoardEntry 预制体。");
    }

    public void EnsureContentLayout()
    {
        if (contentRoot == null)
            return;

        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();

        layoutGroup.padding = new RectOffset(4, 4, 4, 4);
        layoutGroup.spacing = 3;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void AddEntry(string playerName, string message)
    {
        if (entryPrefab == null || contentRoot == null)
            ResolveReferences();

        EnsureContentLayout();

        if (entryPrefab == null || contentRoot == null)
        {
            Debug.LogError("[MessageBoardUI] 无法添加留言：引用未就绪。");
            return;
        }

        MessageBoardEntryUI entry = Instantiate(entryPrefab, contentRoot);
        entry.gameObject.SetActive(true);
        entry.Setup(playerName, message, GetNameColor(playerName));

        TrimOldEntries();

        if (scrollToBottomOnNewMessage && scrollRect != null)
            StartCoroutine(ScrollToBottomNextFrame());
    }

    private void TrimOldEntries()
    {
        while (contentRoot.childCount > maxEntries)
            Destroy(contentRoot.GetChild(0).gameObject);
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private static Color GetNameColor(string playerName)
    {
        unchecked
        {
            int hash = playerName.GetHashCode();
            float hue = (hash & 0x7FFFFFFF) % 360 / 360f;
            return Color.HSVToRGB(hue, 0.55f, 1f);
        }
    }
}
