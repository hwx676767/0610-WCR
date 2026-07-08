using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BubbleChatInputUI : MonoBehaviour
{
    public static bool IsBubbleInputOpen { get; private set; }

    [SerializeField]
    private GameObject inputPanel;

    [SerializeField]
    private TMP_InputField inputField;

    private void Awake()
    {
        EnsureUI();
        BindSubmit();
    }

    private void Start()
    {
        if (inputPanel != null)
            inputPanel.SetActive(false);

        IsBubbleInputOpen = false;
    }

    private void BindSubmit()
    {
        if (inputField == null)
            return;

        inputField.onSubmit.RemoveListener(HandleSubmit);
        inputField.onSubmit.AddListener(HandleSubmit);
    }

    private void HandleSubmit(string _)
    {
        Send();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (inputPanel != null && inputPanel.activeSelf)
                ClosePanel();
            else if (!IsOtherInputFocused())
                OpenPanel();
        }

        if (inputPanel == null || !inputPanel.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            ClosePanel();
    }

    public void OpenPanel()
    {
        if (!NetworkClient.active)
            return;

        if (NetworkClient.localPlayer == null)
            return;

        EnsureUI();

        if (inputPanel == null)
            return;

        inputPanel.SetActive(true);
        IsBubbleInputOpen = true;

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    public void ClosePanel()
    {
        if (inputPanel != null)
            inputPanel.SetActive(false);

        IsBubbleInputOpen = false;
    }

    public void Send()
    {
        if (inputField == null || NetworkClient.localPlayer == null)
            return;

        string text = inputField.text;
        if (string.IsNullOrWhiteSpace(text))
        {
            ClosePanel();
            return;
        }

        PlayerChat chat = NetworkClient.localPlayer.GetComponent<PlayerChat>();
        if (chat != null)
            chat.Say(text);

        inputField.text = "";
        ClosePanel();
    }

    private bool IsOtherInputFocused()
    {
        GameObject selected = EventSystem.current?.currentSelectedGameObject;
        if (selected == null)
            return false;

        TMP_InputField other = selected.GetComponentInParent<TMP_InputField>();
        if (other == null)
            return false;

        return inputField == null || other != inputField;
    }

    private void EnsureUI()
    {
        if (inputPanel != null && inputField != null)
            return;

        RectTransform canvasRect = GameObject.Find("CanvasChat")?.GetComponent<RectTransform>();
        if (canvasRect == null)
            canvasRect = FindFirstObjectByType<Canvas>()?.GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            Debug.LogError("[BubbleChatInputUI] 未找到 CanvasChat，无法创建气泡输入框。");
            return;
        }

        inputPanel = new GameObject(
            "BubbleChatPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        inputPanel.transform.SetParent(canvasRect, false);

        RectTransform panelRect = inputPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 120f);
        panelRect.sizeDelta = new Vector2(640f, 72f);

        Image panelImage = inputPanel.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.88f);

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(inputPanel.transform, false);

        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(16f, 10f);
        textAreaRect.offsetMax = new Vector2(-16f, -10f);

        GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
        placeholderGo.transform.SetParent(textArea.transform, false);

        RectTransform placeholderRect = placeholderGo.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        TextMeshProUGUI placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
        placeholder.font = TMP_Settings.defaultFontAsset;
        placeholder.text = "Press Enter to send, Esc to close";
        placeholder.fontSize = 24;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(1f, 1f, 1f, 0.45f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(textArea.transform, false);

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 26;
        text.color = Color.white;

        inputField = inputPanel.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.characterLimit = 80;

        BindSubmit();
    }
}
