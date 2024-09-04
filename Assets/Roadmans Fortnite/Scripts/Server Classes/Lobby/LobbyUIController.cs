using System;
using Mirror;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Lobby
{
    public class LobbyUIController : NetworkBehaviour
    {
        public LobbyManagement lobbyManager;
        

        [ClientRpc]
        public void UpdateLobbyUI()
        {
            // Update all UI for the lobby
            // include player names, characters, levels? 

            foreach (PlayerInfo player in lobbyManager.PlayersInLobby)
            {
                Debug.Log($"There are {lobbyManager.PlayersInLobby.Count} in the lobby");
            }
        }
    }
}
