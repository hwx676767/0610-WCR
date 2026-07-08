using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    [Header("玩家出生位置")]
    public Vector3 spawnPosition = new Vector3(0, 0, 0);

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // 生成玩家
        GameObject player = Instantiate(
            playerPrefab,
            spawnPosition,
            Quaternion.identity
        );

        // 随机颜色
        PlayerColor colorComp = player.GetComponent<PlayerColor>();

        if (colorComp != null)
        {
            Color randomColor = new Color(
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f)
            );

            colorComp.playerColor = randomColor;
        }

        // 添加到网络
        NetworkServer.AddPlayerForConnection(conn, player);

        Debug.Log($"玩家 {conn.connectionId} 已生成");
    }
}