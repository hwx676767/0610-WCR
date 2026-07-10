using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    [Header("玩家出生位置")]
    public Vector3 spawnPosition = new Vector3(0f, -2.5f, 0f);

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(
            playerPrefab,
            spawnPosition,
            Quaternion.identity
        );

        PlayerAppearance appearance = player.GetComponent<PlayerAppearance>();
        if (appearance != null && appearance.AppearanceCount > 0)
            appearance.AssignRandomAppearance();

        NetworkServer.AddPlayerForConnection(conn, player);

        Debug.Log($"玩家 {conn.connectionId} 已生成");
    }
}
