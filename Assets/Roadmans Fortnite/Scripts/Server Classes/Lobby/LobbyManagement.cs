using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Lobby
{
    public class LobbyManagement : NetworkManager
    {
        // store connected players in list 
        //public readonly List<PlayerInfo> PlayersInLobby = new List<PlayerInfo>();

        public override void OnServerConnect(NetworkConnection conn)
        {
            // Assign new player a unique id and add them to the lobby list

            //PlayerInfo player = new PlayerInfo(conn.connectionId, "Player" + conn.connectionId);
            //PlayersInLobby.Add(player);
            
            //Debug.Log($"Players in lobby {PlayersInLobby.Count} {PlayersInLobby}");
            // send player list to all clients
            
        }

        [Server]
        public void StartGame()
        {
            ServerChangeScene("");
        }
    }

   
}
