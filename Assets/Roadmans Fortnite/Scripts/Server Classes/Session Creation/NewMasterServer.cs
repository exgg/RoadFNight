using System.Collections.Generic;
using kcp2k;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Session_Creation
{
    public class GameServerInfo
    {
        public NetworkConnection connection;
        public string serverAddress;
        public int maxPlayers;
        public int currentPlayers;

        public bool initialHostMigaration;
        public bool finalHostMigration;
        
        
        
        public List<PlayerServerInfo> Players = new List<PlayerServerInfo>();
        public GameServerInfo(NetworkConnection conn, string address, int max)
        {
            connection = conn;
            serverAddress = address;
            maxPlayers = max;
            currentPlayers = 0; // start with 0 players
            initialHostMigaration = false;
            finalHostMigration = false;
        }

    }

    public class PlayerServerInfo
    {
        public string PlayerName;
        public bool IsHost;

        public PlayerServerInfo(string playerName, bool iH)
        {
            PlayerName = playerName;
            IsHost = iH;
        }
    }
    
    public class NewMasterServer : NetworkManager
    {
        // List to track registered game servers
        private readonly List<GameServerInfo> _availableGameServers = new List<GameServerInfo>();

        public override void Start()
        {
            base.Start();
            
            StartServer();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log($"Master Server started and listening for game servers on port {GetComponent<KcpTransport>().Port}");

            // Register handler for the game server registration message
            NetworkServer.RegisterHandler<GameServerRegistrationMessage>(OnGameServerRegistration);
            
            // Register handler for game server availability requests from clients
            NetworkServer.RegisterHandler<GameServerAvailabilityRequestMessage>(OnGameServerAvailabilityRequest);
            
            // register handler for game server joined
            NetworkServer.RegisterHandler<GameServerPlayerJoinedServer>(OnServerPlayerJoinedMessage);
            
            // Register handler for player leaving the server
            NetworkServer.RegisterHandler<GameServerPlayerLeftServerMessage>(OnPlayerLeftServerMessage);
        }

        // Called when the Master Server receives a registration message from a Game Server
        private void OnGameServerRegistration(NetworkConnection conn, GameServerRegistrationMessage msg)
        {
            Debug.Log($"Received registration from game server: {msg.ServerAddress} with max players: {msg.maxPlayers}");

            // Add the server's connection along with its player capacity
            var gameServerInfo = new GameServerInfo(conn, msg.ServerAddress, msg.maxPlayers);
            _availableGameServers.Add(gameServerInfo);

            Debug.Log($"Game server registered. Total available servers: {_availableGameServers.Count}");
        }

        private void OnGameServerAvailabilityRequest(NetworkConnection conn, GameServerAvailabilityRequestMessage msg)
        {
            Debug.Log("Received request to find an available server.");
    
            bool hostAvailable = false;
            string serverAddress = string.Empty;
            ushort serverPort = 0;

            // Check for available servers and skip the server that sent the request
            foreach (var server in _availableGameServers)
            {
                if (server.connection != conn && server.currentPlayers < server.maxPlayers)
                {
                    // Found an available server
                    hostAvailable = true;
                    serverAddress = server.serverAddress; // Use the actual address
                    serverPort = ((KcpTransport)GetComponent<Transport>()).Port;

                    Debug.Log($"Found an available game server: {server.serverAddress}");
                    break;
                }
            }

            // Send back response to the client
            var response = new GameServerAvailabilityResponseMessage
            {
                IsHostAvailable = hostAvailable,
                GameServerAddress = serverAddress,
                GameServerPort = serverPort
            };

            conn.Send(response);
            Debug.Log($"Sent availability response: Host Available = {hostAvailable}");
        }

        private void OnServerPlayerJoinedMessage(NetworkConnection conn, GameServerPlayerJoinedServer msg)
        {
            bool playerJoinSuccessful = false;
            string serverAddress = string.Empty;
    
            foreach (var server in _availableGameServers)
            {
                Debug.Log($"Comparing joined server address {msg.ServerAddress} with registered server {server.serverAddress}");
        
                if (msg.ServerAddress == server.serverAddress)
                {
                    server.currentPlayers++;
                    playerJoinSuccessful = true;
                    serverAddress = server.serverAddress;

                    var playerInfo = new PlayerServerInfo(msg.PlayerName, msg.IsHost)
                    {
                        PlayerName = msg.PlayerName,
                        IsHost = msg.IsHost,
                    };
                    
                    server.Players.Add(playerInfo);
                    
                    
                    
                    Debug.Log($"Player added to server connected Player Information : Player Name: {playerInfo.PlayerName}" +
                              $" Player Conn ID : {conn.connectionId} Player is Host : {playerInfo.IsHost}");
                    
                    Debug.Log($"Player has been added to the server: {server.serverAddress}. " +
                              $"{server.maxPlayers - server.currentPlayers} slots available.");
                    break;  // Stop after finding the matching server
                }
            }

            var response = new PlayerJoinedSuccessfullyResponse
            {
                PlayerJoinedSuccessful = playerJoinSuccessful,
                ServerAddress = serverAddress,
            };
    
            conn.Send(response);
            Debug.Log($"Sent join request response: Player Joined = {playerJoinSuccessful}");
        }
        
        public void CheckForAvailableGameServer(NetworkConnection playerconn)
        {
            foreach (var server in _availableGameServers)
            {
                if (server.currentPlayers < server.maxPlayers)
                {
                    Debug.Log($"Directing player: {playerconn.connectionId} to game server {server.serverAddress}");
                    
                    // send player connection to the game server
                    RedirectPlayerToGameServer(playerconn, server.connection);
                    
                    // exit method after completion
                }
            }
        }
        
        // method to redirect a player to a chosen game server logged by the master server

        private void RedirectPlayerToGameServer(NetworkConnection playerConn, NetworkConnection gameServerConn)
        {
            var message = new GameServerRedirectMessage()
            {
                // Use the networkAddress from the NetworkManager singleton
                GameServerAddress = NetworkManager.singleton.networkAddress, // Get the current network address
                GameServerPort = ((KcpTransport)GetComponent<Transport>()).Port // Port remains the same as you're using
            };

            // Send the redirect message to the player
            playerConn.Send(message);
            Debug.Log($"Redirected player {playerConn.connectionId} to game server at {message.GameServerAddress}:{message.GameServerPort}");
        }

        private void OnPlayerLeftServerMessage(NetworkConnection conn, GameServerPlayerLeftServerMessage msg)
        {
            Debug.Log("The Message to leave has been applied");
            
            foreach (var server in _availableGameServers)
            {
                if (msg.ServerAddress == server.serverAddress)
                {
                    server.currentPlayers--;
                    server.Players.RemoveAll(player => player.PlayerName == msg.PlayerUsername); // remove the player from the list of player infos
                    
                    Debug.Log($"Player {msg.PlayerUsername} has left the server: {server.serverAddress}" +
                              $" {server.maxPlayers - server.currentPlayers} slots now available");

                    var hostPlayer = server.Players.Find(player => player.IsHost);

                    if (hostPlayer == null)
                    {
                        Debug.Log($"The host has been disconnected, migrate server to a new player");
                        //TODO: 
                            // create logic here for the host transferal, or connect to the valid method required to perform this logic
                            // this will need to ping check, find lowest ping for each player, then migrate to that host/
                    }
                    break; // leave the loop
                }
            }
        }
    }
    
    
    public struct GameServerAvailabilityRequestMessage : NetworkMessage
    {
        // You can add additional fields if necessary, but for now, this message is just a request
    }
    
    public struct GameServerRedirectMessage : NetworkMessage
    {
        public string GameServerAddress;
        public ushort GameServerPort;
    }
    
    public struct GameServerAvailabilityResponseMessage : NetworkMessage
    {
        public bool IsHostAvailable;
        public string GameServerAddress;
        public ushort GameServerPort;
    }

    public struct GameServerPlayerJoinedServer : NetworkMessage
    {
        public string ServerAddress;
        public ushort ServerPort;
        public string PlayerName;
        public bool IsHost;
    }

    public struct PlayerJoinedSuccessfullyResponse : NetworkMessage
    {
        public bool PlayerJoinedSuccessful;
        public string ServerAddress;
    }

    public struct GameServerPlayerLeftServerMessage : NetworkMessage
    {
        public string ServerAddress;
        public string PlayerUsername;
    }
}
