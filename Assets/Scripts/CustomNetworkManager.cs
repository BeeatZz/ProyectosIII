using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections.Generic;

public class CustomNetworkManager : NetworkManager
{
    public string lobbySceneName = "LobbyScene";
    public string gameSceneName = "ColocacionObjetos3D";
    public GameObject lobbyPlayerPrefab;

    private Dictionary<NetworkConnectionToClient, PlayerData> playerData = new();

    private struct PlayerData
    {
        public string playerName;
        public ulong steamID;
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                PlayerLobbyInfo info = conn.identity.GetComponent<PlayerLobbyInfo>();
                if (info != null)
                {
                    playerData[conn] = new PlayerData
                    {
                        playerName = info.playerName,
                        steamID = info.steamID
                    };
                    Debug.Log($"[NetworkManager] Saved player data: {info.playerName}");
                }

                NetworkServer.Destroy(conn.identity.gameObject);
            }
        }

        base.OnServerChangeScene(newSceneName);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == lobbySceneName)
            SpawnLobbyPlayer(conn);
        else if (scene == gameSceneName)
            SpawnGamePlayer(conn);
    }

    private void SpawnLobbyPlayer(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
            NetworkServer.Destroy(conn.identity.gameObject);

        GameObject lobbyPlayer = Instantiate(lobbyPlayerPrefab);
        bool success = NetworkServer.AddPlayerForConnection(conn, lobbyPlayer);
        if (!success)
            Destroy(lobbyPlayer);
    }

    private void SpawnGamePlayer(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
            NetworkServer.Destroy(conn.identity.gameObject);

        Transform startPos = GetStartPosition();
        GameObject player = Instantiate(
            playerPrefab,
            startPos != null ? startPos.position : Vector3.zero,
            Quaternion.identity
        );

        if (playerData.TryGetValue(conn, out PlayerData data))
        {
            PlayerLobbyInfo info = player.GetComponent<PlayerLobbyInfo>();
            if (info != null)
            {
                info.steamID = data.steamID;
                info.playerName = data.playerName;
                Debug.Log($"[NetworkManager] Restored player data: {data.playerName}");
            }

            playerData.Remove(conn);
        }

        bool success = NetworkServer.AddPlayerForConnection(conn, player);
        if (!success)
            Destroy(player);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        playerData.Remove(conn); 
        base.OnServerDisconnect(conn);
    }
}