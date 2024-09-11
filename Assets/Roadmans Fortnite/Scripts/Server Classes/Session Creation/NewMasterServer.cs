using System.Collections.Generic;
using kcp2k;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup;
using Roadmans_Fortnite.Scripts.Server_Classes.Server_Communication;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Session_Creation
{
    public class GameServerInfo
    {
        public NetworkConnection Connection;
        public string ServerAddress;
        public int MaxPlayers;
        public int CurrentPlayers;

        public int MinimumPlayers;
        public bool HostMigrationCompleted;
        
        public List<PlayerServerInfo> Players = new List<PlayerServerInfo>();
        public GameServerInfo(NetworkConnection conn, string address, int max)
        {
            Connection = conn;
            ServerAddress = address;
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
        private readonly List<GameServerInfo> _availableGameServers = new List<GameServerInfo>();

        public override void Start()
        {
            base.Start();
            
            StartServer();
        }
        
        public void CheckForAvailableGameServer(NetworkConnection playerconn) // this seems redundant but I will look over if I have used it
        {
            foreach (var server in _availableGameServers)
            {
                if (server.CurrentPlayers < server.MaxPlayers)
                {
                    Debug.Log($"Directing player: {playerconn.connectionId} to game server {server.ServerAddress}");
                    
                    // send player connection to the game server
                    RedirectPlayerToGameServer(playerconn, server.Connection);
                    
                    // exit method after completion
                }
            }
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
        }

        #endregion
        
        #region Requests

        private void OnGameServerRegistrationMessageRequest(NetworkConnection conn, GameServerRegistrationMessage msg)
        {
            Debug.Log($"Received registration from game server: {msg.ServerAddress} with max players: {msg.maxPlayers}");

            // Add the server's connection along with its player capacity
            var gameServerInfo = new GameServerInfo(conn, msg.ServerAddress, msg.maxPlayers);
            _availableGameServers.Add(gameServerInfo);

            Debug.Log($"Game server registered. Total available servers: {_availableGameServers.Count}");
        }

        private void OnGameServerAvailabilityMessageRequest(NetworkConnection conn, GameServerAvailabilityRequestMessage msg)
        {
            Debug.Log("Received request to find an available server.");
    
            bool hostAvailable = false;
            string serverAddress = string.Empty;
            ushort serverPort = 0;

            // Check for available servers and skip the server that sent the request
            foreach (var server in _availableGameServers)
            {
                if (server.Connection != conn && server.CurrentPlayers < server.MaxPlayers)
                {
                    // Found an available server
                    hostAvailable = true;
                    serverAddress = server.ServerAddress; // Use the actual address
                    serverPort = ((KcpTransport)GetComponent<Transport>()).Port;

                    Debug.Log($"Found an available game server: {server.ServerAddress}");
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
    
            foreach (var server in _availableGameServers)
            {
                Debug.Log($"Comparing joined server address {msg.ServerAddress} with registered server {server.ServerAddress}");
        
                if (msg.ServerAddress == server.ServerAddress)
                {
                    server.CurrentPlayers++;
                    playerJoinSuccessful = true;
                    serverAddress = server.ServerAddress;

                    var playerInfo = new PlayerServerInfo(msg.PlayerName, msg.IsHost)
                    {
                        PlayerName = msg.PlayerName,
                        IsHost = msg.IsHost,
                    };
                    
                    server.Players.Add(playerInfo);

                    if (playerInfo.IsHost)
                    {
                        // TODO: Host Migration : SIDE NOTE: This is no longer required until the server game is ready to start
                            // This will require the game to shoot forwards back to the master server to log this again somehow
                            // or we could actually assign a log for the game server to continue the process for this. This is going to be interesting to say the least for this...
                    }
                    
                    Debug.Log($"Player added to server connected Player Information : Player Name: {playerInfo.PlayerName}" +
                              $" Player Conn ID : {conn.connectionId} Player is Host : {playerInfo.IsHost}");
                    
                    Debug.Log($"Player has been added to the server: {server.ServerAddress}. " +
                              $"{server.MaxPlayers - server.CurrentPlayers} slots available.");
                    break;  // Stop after finding the matching server
                }
            }

            var response = new PlayerJoinedSuccessfullyResponseMessage
            {
                PlayerJoinedSuccessful = playerJoinSuccessful,
                ServerAddress = serverAddress,
            };
    
            conn.Send(response);
            Debug.Log($"Sent join request response: Player Joined = {playerJoinSuccessful}");
        }

        private void OnPlayerLeftServerMessageRequest(NetworkConnection conn, GameServerPlayerLeftServerRequestMessage msg)
        {
            Debug.Log("The Message to leave has been applied");
            
            foreach (var server in _availableGameServers)
            {
                if (msg.ServerAddress == server.ServerAddress)
                {
                    server.CurrentPlayers--;
                    server.Players.RemoveAll(player => player.PlayerName == msg.PlayerUsername); // remove the player from the list of player infos
                    
                    Debug.Log($"Player {msg.PlayerUsername} has left the server: {server.ServerAddress}" +
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
                        Debug.Log($"All players in server {server.ServerAddress} have left the lobby" +
                                  $"Proceeding to shut down the server");

                        _availableGameServers.Remove(server);
                        
                        Debug.Log($"Server has now been removed there are now {_availableGameServers.Count} servers " +
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
            //TODO: Handle logic to perceive the player is ready
                // Look for player name, find the player name with lambda,
                // Once player has assigned to be ready use a toggler
                    // Toggle = Ready = !Ready - Allowing a flicker switch up

                    foreach (var server in _availableGameServers)
                    {
                        if (server.ServerAddress == msg.ServerAddress)
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
                                IsReady = targetPlayer.PlayerReady
                            };
                            
                            conn.Send(readyResponse);
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
