using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance;

    [SerializeField]
    private MessageBoardUI messageBoard;

    void Awake()
    {
        Instance = this;
        EnsureMessageBoard();
    }

    private void EnsureMessageBoard()
    {
        if (messageBoard != null)
        {
            messageBoard.ResolveReferences();
            return;
        }

        messageBoard = FindFirstObjectByType<MessageBoardUI>(FindObjectsInactive.Include);

        if (messageBoard != null)
        {
            messageBoard.ResolveReferences();
            return;
        }

        ScrollRect scrollRect = FindFirstObjectByType<ScrollRect>(FindObjectsInactive.Include);
        if (scrollRect == null)
        {
            Debug.LogError("[ChatManager] 场景中未找到 ScrollRect，请按文档创建 MessageBoardScroll。");
            return;
        }

        messageBoard = scrollRect.GetComponent<MessageBoardUI>();
        if (messageBoard == null)
            messageBoard = scrollRect.gameObject.AddComponent<MessageBoardUI>();

        messageBoard.ResolveReferences();
    }

    public void AddBoardMessage(string playerName, string message)
    {
        if (messageBoard == null)
            EnsureMessageBoard();

        if (messageBoard == null)
        {
            Debug.LogError("[ChatManager] MessageBoardUI 未就绪，留言未显示。");
            return;
        }

        messageBoard.AddEntry(playerName, message);
    }
}
