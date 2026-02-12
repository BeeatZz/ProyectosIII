using UnityEngine;
using Steamworks;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviour
{
    public Transform playerListContainer;
    public GameObject playerDisplayPrefab;

    private Dictionary<ulong, GameObject> playerDisplays = new Dictionary<ulong, GameObject>();

    public void UpdatePlayerDisplay(PlayerLobbyInfo player)
    {
        if (player.steamID == 0) return; 

        if (!playerDisplays.ContainsKey(player.steamID))
        {
            GameObject display = Instantiate(playerDisplayPrefab, playerListContainer);
            playerDisplays[player.steamID] = display;
        }

        PlayerDisplayUI displayUI = playerDisplays[player.steamID].GetComponent<PlayerDisplayUI>();
        displayUI.SetPlayerInfo(player.steamID, player.playerName);
    }

    public void RemovePlayerDisplay(ulong steamID)
    {
        if (playerDisplays.ContainsKey(steamID))
        {
            Destroy(playerDisplays[steamID]);
            playerDisplays.Remove(steamID);
        }
    }
}