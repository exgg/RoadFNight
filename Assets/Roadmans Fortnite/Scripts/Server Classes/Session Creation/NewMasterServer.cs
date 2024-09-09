using System.Collections.Generic;
using kcp2k;
using Mirror;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Session_Creation
{
    public class NewMasterServer : NetworkManager
    {
     // Dictionary to store all active instances;
        private readonly Dictionary<string, GameInstance> _activeGameInstances = new Dictionary<string, GameInstance>();

        // A list of connected players waiting for a game
        private readonly List<NetworkConnection> _waitingPlayers = new List<NetworkConnection>();

        // Max Players per instance
        public int maxPlayersPerGame;

        // Define the port you want to use for the Master Server
        public int masterServerPort = 8888;

        // Called when the server starts
        public override void Start()
        {
            base.Start();
            
            
            // Access the KCP Transport and set the port
            KcpTransport transport = GetComponent<KcpTransport>(); // Use KcpTransport instead
            if (transport != null)
            {
                transport.Port = (ushort)masterServerPort; // Set the port for the KCP transport
                Debug.Log($"Master Server running on port {transport.Port}");
            }
            else
            {
                Debug.LogError("KCPTransport not found! Ensure you have the correct transport attached.");
            }
        }

        // Remaining server logic (same as before)
        public override void OnServerConnect(NetworkConnection conn)
        {
            Debug.Log($"Player Connected: {conn.connectionId}");

            // Move player to the lobby scene once connected
            ServerChangeScene("LobbyScene"); // Make sure "LobbyScene" exists
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            Debug.Log($"Player Disconnected: {conn.connectionId}");
            base.OnServerDisconnect(conn);
        }

        private void TryCreateGameInstance()
        {
            if (_waitingPlayers.Count >= maxPlayersPerGame)
            {
                string gameId = System.Guid.NewGuid().ToString();
                GameInstance gameInstance = new GameInstance(gameId);
                _activeGameInstances.Add(gameId, gameInstance);

                for (int i = 0; i < maxPlayersPerGame; i++)
                {
                    NetworkConnection player = _waitingPlayers[0];
                    _waitingPlayers.RemoveAt(0);
                    gameInstance.AddPlayer(player);
                }
            }
        }

        private void RemovePlayerFromInstance(NetworkConnection conn)
        {
            foreach (var gameInstance in _activeGameInstances.Values)
            {
                if (gameInstance.RemovePlayer(conn))
                {
                    if (gameInstance.IsEmpty())
                    {
                        EndGameInstance(gameInstance.GameId);
                    }
                }
            }
        }

        private void EndGameInstance(string gameId)
        {
            if (_activeGameInstances.TryGetValue(gameId, out var gameInstance))
            {
                gameInstance.EndGame();
                _activeGameInstances.Remove(gameId);
            }
        }
        
    }
}
