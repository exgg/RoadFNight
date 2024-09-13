using System.Collections.Generic;
using kcp2k;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup;
using Roadmans_Fortnite.Scripts.Server_Classes.Server_Communication;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.MasterServer
{
    public class GameServerInfo
    {
        public NetworkConnection Connection;
        public string ServerName;
        public string ServerIp;
        
        public int MaxPlayers;
        public int CurrentPlayers;
        public int MinimumPlayers;

        public bool InitialHosting = false;
        public bool HostMigrationCompleted;
        
        public List<PlayerServerInfo> Players = new List<PlayerServerInfo>();
        public GameServerInfo(NetworkConnection conn, string name, int max, string sIp)
        {
            Connection = conn;
            ServerName = name;
            ServerIp = sIp;
            MaxPlayers = max;
            MinimumPlayers = MaxPlayers / 2;
            CurrentPlayers = 0; // start with 0 players
            HostMigrationCompleted = false;
        }
    }

    public class PlayerServerInfo
    {
        public string PlayerName;
        public bool IsHost;
        public bool PlayerReady = false;
        
        public PlayerServerInfo(string playerName, bool iH)
        {
            PlayerName = playerName;
            IsHost = iH;
        }
    }
    
    public class NewMasterServer : NetworkManager
    {
        // List to track registered game servers
        public List<GameServerInfo> AvailableGameServers = new List<GameServerInfo>();
        
        public override void Start()
        {
            base.Start();
            StartServer();
        }

   
        #region Request Handler

        // Called when the Master Server receives a registration message from a Game Server
        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log($"Master Server started and listening for game servers on port {GetComponent<KcpTransport>().Port}");

            // Register handler for the game server registration message
            NetworkServer.RegisterHandler<GameServerRegistrationMessage>(OnGameServerRegistrationMessageRequest);
            
            // Register handler for game server availability requests from clients
            NetworkServer.RegisterHandler<GameServerAvailabilityRequestMessage>(OnGameServerAvailabilityMessageRequest);
            
            // register handler for game server joined
            NetworkServer.RegisterHandler<GameServerPlayerJoinedServerRequestMessage>(OnServerPlayerJoinedMessageRequest);
            
            // Register handler for player leaving the server
            NetworkServer.RegisterHandler<GameServerPlayerLeftServerRequestMessage>(OnPlayerLeftServerMessageRequest);
            
            // Register handler for player readying up on the lobby
            NetworkServer.RegisterHandler<GameServerPlayerReadiedUpMessageRequest>(OnServerPlayerReadyUpMessageRequest);
            
            // Register handler for players ready check
            NetworkServer.RegisterHandler<GameServerPlayerReadyCheckMessageRequest>(OnServerReadyCheckMessageRequest);
        }

        #endregion
        
        #region Requests

        private void OnGameServerRegistrationMessageRequest(NetworkConnection conn, GameServerRegistrationMessage msg)
        {
            Debug.Log($"Received registration from game server: {msg.ServerAddress} with max players: {msg.maxPlayers} the hosts IP will be {msg.hostPlayerIp}");

            // Add the server's connection along with its player capacity
            var gameServerInfo = new GameServerInfo(conn, msg.ServerAddress, msg.maxPlayers, msg.hostPlayerIp);
            AvailableGameServers.Add(gameServerInfo);

            Debug.Log($"Game server registered. Total available servers: {AvailableGameServers.Count}");
        }

        private void OnGameServerAvailabilityMessageRequest(NetworkConnection conn, GameServerAvailabilityRequestMessage msg)
        {
            Debug.Log("Received request to find an available server.");
    
            bool hostAvailable = false;
            string serverAddress = string.Empty;
            ushort serverPort = 0;

            // Check for available servers and skip the server that sent the request
            foreach (var server in AvailableGameServers)
            {
                if (server.Connection != conn && server.CurrentPlayers < server.MaxPlayers)
                {
                    // Found an available server
                    hostAvailable = true;
                    serverAddress = server.ServerName; // Use the actual address
                    serverPort = ((KcpTransport)GetComponent<Transport>()).Port;

                    Debug.Log($"Found an available game server: {server.ServerName}");
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

        private void OnServerPlayerJoinedMessageRequest(NetworkConnection conn, GameServerPlayerJoinedServerRequestMessage msg)
        {
            bool playerJoinSuccessful = false;
            string serverAddress = string.Empty;
    
            foreach (var server in AvailableGameServers)
            {
                Debug.Log($"Comparing joined server address {msg.ServerName} with registered server {server.ServerName}");
        
                if (msg.ServerName == server.ServerName)
                {
                    server.CurrentPlayers++;
                    playerJoinSuccessful = true;
                    serverAddress = server.ServerName;

                    var playerInfo = new PlayerServerInfo(msg.PlayerName, msg.IsHost)
                    {
                        PlayerName = msg.PlayerName,
                        IsHost = msg.IsHost,
                    };
                    
                    server.Players.Add(playerInfo);

                    if (playerInfo.IsHost) // send a message to the client who is classified as the host. This will then allow for the initial host migration
                    {

                        var isHostResponseMessage = new PlayerJoinedIsHostResponseMessage
                        {
                            IsHost = playerInfo.IsHost,
                        };
                        
                        conn.Send(isHostResponseMessage);
                    }
                    
                    Debug.Log($"Player added to server connected Player Information : Player Name: {playerInfo.PlayerName}" +
                              $" Player Conn ID : {conn.connectionId} Player is Host : {playerInfo.IsHost}");
                    
                    Debug.Log($"Player has been added to the server: {server.ServerName}. " +
                              $"{server.MaxPlayers - server.CurrentPlayers} slots available. The host connection IP is : {server.ServerIp}");
                    break;  // Stop after finding the matching server
                }
            }

            var response = new PlayerJoinedSuccessfullyResponseMessage
            {
                PlayerJoinedSuccessful = playerJoinSuccessful,
                ServerAddress = serverAddress,
            };

            var commandSwitch = new GameServerCommandPushToLobby();
            
            conn.Send(response);
            conn.Send(commandSwitch);
            Debug.Log($"Sent join request response: Player Joined = {playerJoinSuccessful}");
        }

        private void OnPlayerLeftServerMessageRequest(NetworkConnection conn, GameServerPlayerLeftServerRequestMessage msg)
        {
            Debug.Log("The Message to leave has been applied");
            
            foreach (var server in AvailableGameServers)
            {
                if (msg.ServerAddress == server.ServerName)
                {
                    server.CurrentPlayers--;
                    server.Players.RemoveAll(player => player.PlayerName == msg.PlayerUsername); // remove the player from the list of player infos
                    
                    Debug.Log($"Player {msg.PlayerUsername} has left the server: {server.ServerName}" +
                              $" {server.MaxPlayers - server.CurrentPlayers} slots now available");

                    var hostPlayer = server.Players.Find(player => player.IsHost);

                    if (hostPlayer == null && server.CurrentPlayers > 0)
                    {
                        Debug.Log($"The host has been disconnected, migrate server to a new player");
                        //TODO: Host Migrate Again.
                            // hmm, maybe push all players to reconnect to the master server, find a host then reapply the travel to the new host ? since all the logic is here already
                                // create logic here for the host transferal, or connect to the valid method required to perform this logic
                                // this will need to ping check, find lowest ping for each player, then migrate to that host/
                    }
                    else if (server.CurrentPlayers <= 0)
                    {
                        Debug.Log($"All players in server {server.ServerName} have left the lobby" +
                                  $"Proceeding to shut down the server");

                        AvailableGameServers.Remove(server);
                        
                        Debug.Log($"Server has now been removed there are now {AvailableGameServers.Count} servers " +
                                  $"available");
                    }
                    break; // leave the loop
                }
            }

            var playerLeavingResponse = new PlayerLeavingResponseMessage();
            
            conn.Send(playerLeavingResponse);
            
            Debug.Log("Player is leaving the game");
        }

        private void OnServerPlayerReadyUpMessageRequest(NetworkConnection conn, GameServerPlayerReadiedUpMessageRequest msg)
        {
         
            foreach (var server in AvailableGameServers)
            {
                if (server.ServerName == msg.ServerAddress)
                {
                    Debug.Log($"Server {msg.ServerAddress} found in servers ");

                    var targetPlayer = server.Players.Find(player => player.PlayerName == msg.PlayerUsername);

                    if (targetPlayer != null)
                    {
                        targetPlayer.PlayerReady = !targetPlayer.PlayerReady; // toggle the ready
                        Debug.Log($"Found Player {targetPlayer.PlayerName} player is ready : {targetPlayer.PlayerReady}");
                    }
                    else
                    {
                        Debug.Log("The player was not found");
                    }

                    var readyResponse = new PlayerReadyResponseMessage
                    {
                        IsReady = targetPlayer is { PlayerReady: true }
                    };
                    
                    conn.Send(readyResponse);
                }
            }
        }

        private void OnServerReadyCheckMessageRequest(NetworkConnection conn, GameServerPlayerReadyCheckMessageRequest msg)
        {
            Debug.Log($"Received Ready check for the players");

            foreach (var server in AvailableGameServers)
            {
                if (server.ServerName == msg.ServerAddress)
                {
                    bool allPlayersReady = true;

                    foreach (var player in server.Players)
                    {
                        if (!player.PlayerReady)
                        {
                            allPlayersReady = false;
                            break;
                        }
                    }
                    // check if all players are ready

                    var playersReadyCheckResponse = new PlayersReadyCheckResponseMessage
                    {
                        AllPlayersReady = allPlayersReady,
                        EnoughPlayersToStart = server.Players.Count >= server.MinimumPlayers,
                    };
                    
                    conn.Send(playersReadyCheckResponse);
                    
                    break;
                }
            }
        }
        
        #endregion

        #region Redirection

        // method to redirect a player to a chosen game server logged by the master server
        private void RedirectPlayerToGameServer(NetworkConnection playerConn, NetworkConnection gameServerConn)
        {
            var message = new GameServerRedirectResponseMessage()
            {
                // Use the networkAddress from the NetworkManager singleton
                GameServerAddress = NetworkManager.singleton.networkAddress, // Get the current network address
                GameServerPort = ((KcpTransport)GetComponent<Transport>()).Port // Port remains the same as you're using
            };

            // Send the redirect message to the player
            playerConn.Send(message);
            Debug.Log($"Redirected player {playerConn.connectionId} to game server at {message.GameServerAddress}:{message.GameServerPort}");
        }

        #endregion
        
    }
}
