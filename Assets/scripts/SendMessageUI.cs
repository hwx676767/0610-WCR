using Mirror;
using TMPro;
using UnityEngine;

public class SendMessageUI : MonoBehaviour
{
    public TMP_InputField inputField;

    private void Awake()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

        BindSubmit();
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

    public void Send()
    {
        if (inputField == null)
            return;

        string text = inputField.text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!NetworkClient.active)
        {
            Debug.LogWarning("[SendMessageUI] 尚未联网，请先点击 Host 或 Join。");
            return;
        }

        if (NetworkClient.localPlayer == null)
        {
            Debug.LogWarning("[SendMessageUI] 本地玩家尚未生成，请 Host/Join 后稍等再发送。");
            return;
        }

        PlayerChat chat = NetworkClient.localPlayer.GetComponent<PlayerChat>();
        if (chat == null)
        {
            Debug.LogError("[SendMessageUI] 玩家 Prefab 上缺少 PlayerChat 组件。");
            return;
        }

        chat.PostToBoard(text);
        inputField.text = "";
    }
}
