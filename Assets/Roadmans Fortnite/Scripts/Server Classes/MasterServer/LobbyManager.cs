using System;
using UnityEngine;
using System.Collections.Generic;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.Lobby;

namespace Roadmans_Fortnite.Scripts.Server_Classes.MasterServer
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance;

        private List<LobbyInfo> _lobbies = new List<LobbyInfo>();
        
        public const int MaxPlayersPerLobby = 8;
        public const int MinPlayersPerLobby = 4;
        
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            NetworkManager.singleton.StartServer();
        }

        [System.Serializable]
        public class LobbyInfo
        {
            public string lobbyId;
            public List<PlayerInfo> Players;
            public bool isGameStarted;

            public LobbyInfo(string tLobbyId)
            {
                this.lobbyId = tLobbyId;
                Players = new List<PlayerInfo>();
            }

            public int PlayerCount()
            {
                return Players.Count;
            }

            public bool IsFull()
            {
                return Players.Count >= MaxPlayersPerLobby;
            }
        }

        [System.Serializable]
        public class PlayerInfo
        {
            public int connectionId;
            public string playerName;

            public PlayerInfo(int conn,string pN)
            {
                connectionId = connectionId;
                playerName = pN;
            }
        }

        public void HandleJoinRequest(NetworkConnection conn)
        {
            LobbyInfo availableLobby = _lobbies.Find(_lobbies => !_lobbies.IsFull());

            if (availableLobby == null)
            {
                // create a new lobby
                CreateLobby(conn);
            }
            else
            {
                // add the player to that lobby
                PlayerInfo playerInfo = new PlayerInfo(conn.connectionId, $"Player{conn.connectionId}");
                availableLobby.Players.Add(playerInfo);
                
                Debug.Log($"Player {playerInfo.playerName} joined the lobby: {availableLobby.lobbyId}");
            }
        }

        private void CreateLobby(NetworkConnection conn)
        {
            string newLobbyId = Guid.NewGuid().ToString();
            LobbyInfo newLobby = new LobbyInfo(newLobbyId);

            PlayerInfo hostPlayer = new PlayerInfo(conn.connectionId, $"Host {conn.connectionId}");
            newLobby.Players.Add(hostPlayer);
            
            _lobbies.Add(newLobby);
            Debug.Log($"A new lobby has been created {newLobbyId} with the host player being {hostPlayer.playerName}");
        }

        private void StartGameForLobby(LobbyInfo lobby)
        {
            lobby.isGameStarted = true;

            foreach (PlayerInfo player in lobby.Players)
            {
                // TODO: Send them to the correct scene using mirror network manager
                // find the best player host for this game
            }
        }
    }
}
