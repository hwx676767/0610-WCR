using Mirror;
using UnityEngine;

public class PlayerColor : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // 或者 GetComponentInChildren<SpriteRenderer>()
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyColor(); // 客户端启动时应用颜色
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = playerColor;
        }
    }

    // 本地玩家可以调用这个来请求随机颜色（可选）
    public void RandomizeColor()
    {
        if (!isLocalPlayer) return;

        Color randomColor = new Color(
            Random.Range(0.3f, 1f),  // 避免太暗
            Random.Range(0.3f, 1f),
            Random.Range(0.3f, 1f)
        );

        CmdSetColor(randomColor);
    }

    [Command]
    private void CmdSetColor(Color newColor)
    {
        playerColor = newColor;   // SyncVar 自动同步给所有客户端
    }
}