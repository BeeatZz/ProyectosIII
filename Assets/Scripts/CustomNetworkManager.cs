using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    public string lobbySceneName = "LobbyScene";
    public string gameSceneName = "TestScene";

    public GameObject lobbyPlayerPrefab;

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (sceneName == lobbySceneName)
            {
                SpawnLobbyPlayer(conn);
            }
            else if (sceneName == gameSceneName)
            {
                SpawnGamePlayer(conn);
            }
        }
    }

    private void SpawnLobbyPlayer(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
            NetworkServer.Destroy(conn.identity.gameObject);

        GameObject lobbyPlayer = Instantiate(lobbyPlayerPrefab);
        NetworkServer.AddPlayerForConnection(conn, lobbyPlayer);
    }

    private void SpawnGamePlayer(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
            NetworkServer.Destroy(conn.identity.gameObject);

        Transform startPos = GetStartPosition();
        Vector3 spawnPos = startPos != null ? startPos.position : Vector3.zero;

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == lobbySceneName)
        {
            SpawnLobbyPlayer(conn);
        }
        else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == gameSceneName)
        {
            SpawnGamePlayer(conn);
        }
    }
}