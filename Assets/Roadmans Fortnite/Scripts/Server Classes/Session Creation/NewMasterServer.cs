using System.Collections.Generic;
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

        // Called when new client connects to a server overriding mirror on server connect

        // ReSharper disable Unity.PerformanceAnalysis
        public override void OnServerConnect(NetworkConnection conn)
        {
            Debug.Log($"Player Connected: {conn.connectionId}");
            _waitingPlayers.Add(conn);
            TryCreateGameInstance();
        }
        
        // Called when new client Disconnects from a server overriding mirror on server Disconnect 

        // ReSharper disable Unity.PerformanceAnalysis
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            Debug.Log($"Player Disconnected: {conn.connectionId}");
             
            RemovePlayerFromInstance(conn);
            base.OnServerDisconnect(conn);
        }
        
        // tries to create a new game instance if there are enough players

        private void TryCreateGameInstance()
        {
            if (_waitingPlayers.Count >= maxPlayersPerGame)
            {
                // create a new game ID
                string gameId = System.Guid.NewGuid().ToString();
                
                // create the game instance
                GameInstance gameInstance = new GameInstance(gameId);
                
                // add new instance to dictionary
                _activeGameInstances.Add(gameId, gameInstance);
                
                // assign players to this game instance
                for (int i = 0; i < maxPlayersPerGame; i++)
                {
                    NetworkConnection player = _waitingPlayers[0];
                    _waitingPlayers.RemoveAt(0);
                    gameInstance.AddPlayer(player);
                }
            }
        }

        // remove player from game if they leave, if this is the last player then shutdown that sub server
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

        // handle removal of instance once the final player has left the game
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
