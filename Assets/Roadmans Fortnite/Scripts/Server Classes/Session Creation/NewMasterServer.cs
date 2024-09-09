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
        public string serverName;
        public int maxPlayers;
        public int currentPlayers;

        public GameServerInfo(NetworkConnection conn, string name, int max)
        {
            connection = conn;
            serverName = name;
            maxPlayers = max;
            currentPlayers = 0; // start with 0 players
        }

    }
    
    public class NewMasterServer : NetworkManager
    {
        // List to track registered game servers
        private List<GameServerInfo> availableGameServers = new List<GameServerInfo>();

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
        }

        // Called when the Master Server receives a registration message from a Game Server
        private void OnGameServerRegistration(NetworkConnection conn, GameServerRegistrationMessage msg)
        {
            Debug.Log($"Received registration from game server: {msg.serverName} with max players: {msg.maxPlayers}");

            // Add the server's connection along with its player capacity
            var gameServerInfo = new GameServerInfo(conn, msg.serverName, msg.maxPlayers);
            availableGameServers.Add(gameServerInfo);

            Debug.Log($"Game server registered. Total available servers: {availableGameServers.Count}");
        }

        private void OnGameServerAvailabilityRequest(NetworkConnection conn, GameServerAvailabilityRequestMessage msg)
        {
            Debug.Log("Received request to find an available server.");
    
            bool hostAvailable = false;
            string serverAddress = string.Empty;
            ushort serverPort = 0;

            // Check for available servers and skip the server that sent the request
            foreach (var server in availableGameServers)
            {
                if (server.connection != conn && server.currentPlayers < server.maxPlayers)
                {
                    // Found an available server
                    hostAvailable = true;
                    serverAddress = server.serverName; // Use the actual address
                    serverPort = ((KcpTransport)GetComponent<Transport>()).Port;

                    Debug.Log($"Found an available game server: {server.serverName}");
                    break;
                }
            }

            // Send back response to the client
            var response = new GameServerAvailabilityResponseMessage
            {
                isHostAvailable = hostAvailable,
                gameServerAddress = serverAddress,
                gameServerPort = serverPort
            };

            conn.Send(response);
            Debug.Log($"Sent availability response: Host Available = {hostAvailable}");
        }

        public void CheckForAvailableGameServer(NetworkConnection playerconn)
        {
            foreach (var server in availableGameServers)
            {
                if (server.currentPlayers < server.maxPlayers)
                {
                    Debug.Log($"Directing player: {playerconn.connectionId} to game server {server.serverName}");
                    
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
                gameServerAddress = NetworkManager.singleton.networkAddress, // Get the current network address
                gameServerPort = ((KcpTransport)GetComponent<Transport>()).Port // Port remains the same as you're using
            };

            // Send the redirect message to the player
            playerConn.Send(message);
            Debug.Log($"Redirected player {playerConn.connectionId} to game server at {message.gameServerAddress}:{message.gameServerPort}");
        }
    }
    
    
    public struct GameServerAvailabilityRequestMessage : NetworkMessage
    {
        // You can add additional fields if necessary, but for now, this message is just a request
    }
    
    public struct GameServerRedirectMessage : NetworkMessage
    {
        public string gameServerAddress;
        public ushort gameServerPort;
    }
    
    public struct GameServerAvailabilityResponseMessage : NetworkMessage
    {
        public bool isHostAvailable;
        public string gameServerAddress;
        public ushort gameServerPort;
    }
}
