using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHeadBubble : MonoBehaviour
{
    private const string DefaultBubbleSpritePath = "Assets/AART/ui/speak.png";

    [SerializeField]
    private float displayDuration = 5f;

    [SerializeField]
    private Vector3 localOffset = new Vector3(6.4f, 6.9f, -0.1f);

    [SerializeField]
    private Vector2 bubbleSize = new Vector2(840f, 220f);

    [SerializeField]
    private float worldScale = 0.015f;

    [SerializeField]
    private Sprite bubbleSprite;

    private GameObject root;
    private TMP_Text label;
    private Coroutine hideRoutine;

    private void Awake()
    {
        ResolveBubbleSprite();
        BuildBubble();
        HideImmediate();
    }

    public void Show(string message)
    {
        if (label == null)
            return;

        label.text = message;
        root.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay(displayDuration));
    }

    private void HideImmediate()
    {
        if (root != null)
            root.SetActive(false);
    }

    private IEnumerator HideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideImmediate();
        hideRoutine = null;
    }

    private void LateUpdate()
    {
        if (root == null || !root.activeSelf)
            return;

        Camera cam = Camera.main;
        if (cam != null)
            root.transform.rotation = cam.transform.rotation;
    }

    private void ResolveBubbleSprite()
    {
        if (bubbleSprite != null)
            return;

#if UNITY_EDITOR
        bubbleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(DefaultBubbleSpritePath);
#endif
    }

    private void BuildBubble()
    {
        root = new GameObject("HeadBubble");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = localOffset;
        root.transform.localScale = Vector3.one * worldScale;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        root.AddComponent<GraphicRaycaster>();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = bubbleSize;

        GameObject background = new GameObject("Background");
        background.transform.SetParent(root.transform, false);

        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = background.AddComponent<Image>();
        bgImage.raycastTarget = false;
        bgImage.color = Color.white;

        if (bubbleSprite != null)
        {
            bgImage.sprite = bubbleSprite;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = true;
        }
        else
        {
            bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        }

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(root.transform, false);

        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(48f, 56f);
        textRect.offsetMax = new Vector2(-32f, -24f);

        label = textGo.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 100;
        label.color = new Color(0.12f, 0.12f, 0.12f);
        label.alignment = TextAlignmentOptions.Top;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
    }

    private void Start()
    {
        if (root == null)
            return;

        Canvas canvas = root.GetComponent<Canvas>();
        if (canvas != null && Camera.main != null)
            canvas.worldCamera = Camera.main;
    }
}
