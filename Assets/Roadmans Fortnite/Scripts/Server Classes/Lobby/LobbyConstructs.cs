using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Lobby
{
    public class LobbyConstructs : MonoBehaviour
    {
        private List<NetworkConnection> players = new List<NetworkConnection>();
        public int maxPlayers = 8;

        public void AddPlayerToLobby(NetworkConnection conn)
        {
            players.Add(conn);
            
            
        }
    }
}
