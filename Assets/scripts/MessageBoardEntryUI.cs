using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageBoardEntryUI : MonoBehaviour
{
    private const float NameColumnWidth = 100f;
    private const float TimestampColumnWidth = 52f;
    private const float EntryMinHeight = 40f;

    [SerializeField]
    private TMP_Text playerNameText;

    [SerializeField]
    private TMP_Text messageText;

    [SerializeField]
    private TMP_Text timestampText;

    private void Awake()
    {
        EnsureEntryLayout();
    }

    public void Setup(string playerName, string message, Color nameColor)
    {
        EnsureEntryLayout();
        ResetRootForContentLayout();

        if (playerNameText != null)
        {
            playerNameText.text = playerName;
            playerNameText.color = nameColor;
        }

        if (messageText != null)
            messageText.text = message;

        if (timestampText != null)
            timestampText.text = System.DateTime.Now.ToString("HH:mm");

        RefreshLayout();
    }

    private void EnsureEntryLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
            return;

        EnsureWhiteBackground();

        VerticalLayoutGroup oldVertical = GetComponent<VerticalLayoutGroup>();
        if (oldVertical != null)
            Destroy(oldVertical);

        HorizontalLayoutGroup layoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

        layoutGroup.padding = new RectOffset(10, 10, 8, 8);
        layoutGroup.spacing = 12;
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = true;

        ContentSizeFitter rootFitter = GetComponent<ContentSizeFitter>();
        if (rootFitter == null)
            rootFitter = gameObject.AddComponent<ContentSizeFitter>();

        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement rootLayout = GetComponent<LayoutElement>();
        if (rootLayout == null)
            rootLayout = gameObject.AddComponent<LayoutElement>();

        rootLayout.minHeight = EntryMinHeight;

        ConfigureNameColumn(playerNameText);
        ConfigureMessageColumn(messageText);
        ConfigureTimestampColumn(timestampText);
    }

    private void EnsureWhiteBackground()
    {
        Image background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();

        background.sprite = null;
        background.color = Color.white;
        background.type = Image.Type.Simple;
        background.raycastTarget = false;
    }

    private static void ConfigureNameColumn(TMP_Text text)
    {
        if (text == null)
            return;

        PrepareHorizontalChild(text.rectTransform);

        LayoutElement layoutElement = text.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = text.gameObject.AddComponent<LayoutElement>();

        layoutElement.minWidth = 80f;
        layoutElement.preferredWidth = NameColumnWidth;
        layoutElement.flexibleWidth = 0f;
        layoutElement.minHeight = EntryMinHeight - 16f;

        RemoveContentSizeFitter(text);

        text.fontSize = 20;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.horizontalAlignment = HorizontalAlignmentOptions.Left;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
    }

    private static void ConfigureMessageColumn(TMP_Text text)
    {
        if (text == null)
            return;

        PrepareHorizontalChild(text.rectTransform);

        LayoutElement layoutElement = text.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = text.gameObject.AddComponent<LayoutElement>();

        layoutElement.minWidth = 120f;
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = EntryMinHeight - 16f;

        ContentSizeFitter fitter = text.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = text.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        text.fontSize = 20;
        text.fontStyle = FontStyles.Normal;
        text.enableWordWrapping = true;
        text.horizontalAlignment = HorizontalAlignmentOptions.Left;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
    }

    private static void ConfigureTimestampColumn(TMP_Text text)
    {
        if (text == null)
            return;

        PrepareHorizontalChild(text.rectTransform);

        LayoutElement layoutElement = text.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = text.gameObject.AddComponent<LayoutElement>();

        layoutElement.minWidth = TimestampColumnWidth;
        layoutElement.preferredWidth = TimestampColumnWidth;
        layoutElement.flexibleWidth = 0f;
        layoutElement.minHeight = EntryMinHeight - 16f;

        RemoveContentSizeFitter(text);

        text.fontSize = 14;
        text.fontStyle = FontStyles.Normal;
        text.enableWordWrapping = false;
        text.horizontalAlignment = HorizontalAlignmentOptions.Right;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
    }

    private static void PrepareHorizontalChild(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 0f);
    }

    private static void RemoveContentSizeFitter(TMP_Text text)
    {
        ContentSizeFitter fitter = text.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            Object.Destroy(fitter);
    }

    private void ResetRootForContentLayout()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
            return;

        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 0f);
    }

    public void RefreshLayout()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
}
