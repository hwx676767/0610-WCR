using Mirror;
using UnityEngine;

public class PlayerChat : NetworkBehaviour
{
    [SerializeField]
    private int maxMessageLength = 200;

    [SerializeField]
    private int bubbleMaxMessageLength = 80;

    public void PostToBoard(string message)
    {
        if (!isLocalPlayer)
            return;

        if (!TryPrepareMessage(message, out string trimmed, maxMessageLength))
            return;

        CmdPostToBoard(trimmed);
    }

    public void Say(string message)
    {
        if (!isLocalPlayer)
            return;

        if (!TryPrepareMessage(message, out string trimmed, bubbleMaxMessageLength))
            return;

        CmdSay(trimmed);
    }

    private bool TryPrepareMessage(string message, out string trimmed, int maxLength)
    {
        trimmed = message?.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return false;

        if (trimmed.Length > maxLength)
            trimmed = trimmed.Substring(0, maxLength);

        return true;
    }

    [Command]
    private void CmdPostToBoard(string message)
    {
        RpcAddBoardEntry(GetDisplayName(), message);
    }

    [Command]
    private void CmdSay(string message)
    {
        RpcShowBubble(netId, message);
    }

    [ClientRpc]
    private void RpcAddBoardEntry(string playerName, string message)
    {
        if (ChatManager.Instance == null)
            return;

        ChatManager.Instance.AddBoardMessage(playerName, message);
    }

    [ClientRpc]
    private void RpcShowBubble(uint playerNetId, string message)
    {
        if (!NetworkClient.spawned.TryGetValue(playerNetId, out NetworkIdentity identity))
            return;

        PlayerHeadBubble bubble = identity.GetComponent<PlayerHeadBubble>();
        if (bubble != null)
            bubble.Show(message);
    }

    private string GetDisplayName()
    {
        if (connectionToClient != null)
            return $"Player{connectionToClient.connectionId}";

        return name;
    }
}
