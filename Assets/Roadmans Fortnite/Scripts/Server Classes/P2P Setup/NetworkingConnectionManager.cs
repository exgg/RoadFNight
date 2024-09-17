using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using kcp2k;
using Roadmans_Fortnite.Scripts.Server_Classes.Account_Management;
using Roadmans_Fortnite.Scripts.Server_Classes.NetworkMessenger;
using Roadmans_Fortnite.Scripts.Server_Classes.Server_Communication;
using Roadmans_Fortnite.Scripts.Server_Classes.Session_Creation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup
{
    
    public struct GameServerRegistrationMessage : NetworkMessage
    {
        public string ServerAddress;
        public int maxPlayers;
        public string hostPlayerIp;
    }
    public class P2PPlayerInfo
    {
        public int connId;
        public int ping;
        public bool isHost;
    }

    public class NetworkingConnectionManager : NetworkManager
    {
        public string masterServerAddress = "localhost"; // Master Server IP
        public int masterServerPort = 8888; // Master Server port

        public string hostServerAddress;

        public bool isHost;
        
        public string cashedServerName;
        
        private IPManager _ipManager;
        private MasterServerMessenger _masterServerMessenger;

        private bool _foundHost;

        private Dictionary<int, P2PPlayerInfo> _connectedClients = new Dictionary<int, P2PPlayerInfo>();
        
        public override void Start()
        {
            base.Start();
            Debug.Log("Game Server starting...");

            _ipManager = GetComponent<IPManager>();
            _masterServerMessenger = FindObjectOfType<MasterServerMessenger>();
            // Try to find an available server or register as a new one
            //StartGameServer();
            
            Debug.Log($"Testing IP Connection Finder {_ipManager.GetLocalIPAddress()}");

            hostServerAddress = _ipManager.GetLocalIPAddress();
            
            Application.wantsToQuit += WantsToQuit; // register an event to the X button on the windowed mode
            
        }
        
        // This function starts the Game Server to host players
        public void StartGameServer()
        {
            Debug.Log("Starting the Game Server...");

            // If the server is already active, skip starting it again
            if (NetworkServer.active)
            {
                Debug.LogWarning("Server already started. Skipping StartHost().");
                ServerHostAvailable();
                return;
            }

            // Search for available hosts
            ServerHostAvailable();
        }

        #region Lobby Controls

        

        #endregion
        
        #region Request/Command/Response Handler Client

        // This method is called when the client successfully connects to the Master Server
        public override void OnClientConnect(NetworkConnection conn)
        {
            Debug.Log($"A Client has connected {conn.connectionId} at the server ID of {networkAddress}");
            
            base.OnClientConnect(conn);
            Debug.Log("Game Server successfully connected to the Master Server.");

            // Register handler to listen for available game servers after the connection is established
            NetworkClient.RegisterHandler<GameServerAvailabilityResponseMessage>(OnServerAvailabilityResponse);
            
            // register handler for response of joining game
            NetworkClient.RegisterHandler<PlayerJoinedSuccessfullyResponseMessage>(OnPlayerJoinedResponse);
            
            // register handler for response of leaving, which will push to leave the connection
            NetworkClient.RegisterHandler<PlayerLeavingResponseMessage>(OnPlayerLeavingResponse);
            
            // Register the handler for the response of the player readying up
            NetworkClient.RegisterHandler<PlayerReadyResponseMessage>(OnPlayerReadyResponse);
            
            //Register handler for the response of the ready check
            NetworkClient.RegisterHandler<PlayersReadyCheckResponseMessage>(OnPlayersReadyCheckResponse);
            
            // Register handler for response for joining and checking if they are the host
            NetworkClient.RegisterHandler<PlayerJoinedIsHostResponseMessage>(OnPlayerIsHostResponse);
            
            // Register handler for Command of the LobbyScene Change
            NetworkClient.RegisterHandler<GameServerCommandPushToLobby>(OnLobbySceneCommand);
            
         
            
            // Send a request to the Master Server to check for available servers
            var request = new GameServerAvailabilityRequestMessage();

            if (!_foundHost)
            {
                NetworkClient.Send(request);
                Debug.Log("Request for available game server sent to the Master Server.");
            }
            else
            {
                Debug.Log("I have already found a server and a host");

                var messageToHost = new TellHostYouHaveArrivedMessage
                {
                    MessageToHost = "I have arrived after being a dickhead"
                };
                
                NetworkClient.Send(messageToHost);
            }
        }

        #endregion

        #region Request/Command/Response Handler Server

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            
            // Register Debug Message for joining server
            NetworkServer.RegisterHandler<TellHostYouHaveArrivedMessage>(OnMessageDebugToHost);
        }

        #endregion

        #region P2P Debugging

        private void OnMessageDebugToHost(NetworkConnection conn, TellHostYouHaveArrivedMessage msg)
        {
            Debug.Log(msg.MessageToHost);
        }

        #endregion

        #region P2P Connection

        // TODO: Hook up a message to send all players the IP address for the hosting player

        public void StartP2PHosting()
        {
            Debug.Log("Starting P2P Host");
    
            _foundHost = true;
    
            // Disconnect from the master server
            NetworkClient.Disconnect();
            StopServer();
            StopClient();
    
            // Explicitly shutdown the transport layer
            var kcpTransport = GetComponent<KcpTransport>();
            kcpTransport.Shutdown();  // Ensure the transport is shut down

            StartCoroutine(DelayedStartHost());
        }

        private IEnumerator DelayedStartHost()
        {
            yield return new WaitForSeconds(0.1f);  // Increased delay

            networkAddress = _ipManager.GetLocalIPAddress();

            int hostPort = 999999;
            var kcpTransport = GetComponent<KcpTransport>();
            kcpTransport.Port = (ushort)hostPort;

            StartHost();  // start both server and client on the host device

            AccountManager accountManager = FindObjectOfType<AccountManager>();

            _masterServerMessenger.NotifyMasterServerOfPlayerAction(accountManager.setupAccountData.username, PlayerActions.PlayerBeganHosting, cashedServerName);

            Debug.Log("Started P2P host");
        }
        
        #endregion

        #region Host Migration / Find Best Host

        private void StartGameTimer()
        {
            // Start a timer to begin finding the best host
            // Iterate through connected clients and send a ping request
            // Each client responds with their ping data
            // Collect and evaluate these results
            OnFindBestHost(); // trigger the process to find the best host
        }
        
        private void OnFindBestHost()
        {
            // Todo : Create logic to push to each client what ping they have for the server. Once this has been declared. Push it all out to the server host server into a list/dictionary
        }

        private void OnSwitchToBestHost()
        {
            // Todo once all messages have been received find the best ping (lowest ms)
                // need a way to store all the players and log all of these players
                // then once all stored and logged they all need to join the new host... This might need to make them pushed through the master again ?
        }

        #endregion
        public void JoinP2PHost()
        {
            _foundHost = true;

            NetworkClient.Disconnect();
            StopServer();
            StopClient();
    
            // Explicitly shutdown the transport layer
            var kcpTransport = GetComponent<KcpTransport>();
            kcpTransport.Shutdown();  // Ensure the transport is shut down

            StartCoroutine(DelayConnect());
        }

        private IEnumerator DelayConnect()
        {
            yield return new WaitForSeconds(0.1f);
            
            // Configure transport to connect to the host server
            networkAddress = hostServerAddress;
            
            int hostPort = 999999;
            var kcpTransport = GetComponent<KcpTransport>();
            kcpTransport.Port = (ushort)hostPort;
            
            StartClient();
            Debug.Log($"Connected to the P2P host at {hostServerAddress}:{hostPort}");
        }
        
        #region Actions
        
        public bool WantsToQuit()
        {
            Debug.Log("User clicked to close the game down trying to catch it to send master server a message");
            
            if (NetworkClient.isConnected)
            {
                Debug.Log("Sending disconnection message before quitting...");
                
                var accountManager = FindObjectOfType<AccountManager>();
                
                // send master server the message of leaving
                NotifyMasterServerOfAbsence(accountManager.setupAccountData.username);
                
                // add delay to prevent application close

                StartCoroutine(DelayedShutdown());

                return false;
            }
            else
            {
                Debug.Log("Apparently the client is not connected??");
            }

            return true;
        }
        
        IEnumerator DelayedShutdown()
        {
            yield return new WaitForSeconds(2f);
            
            Debug.Log("Application will quit after a delay");
            
            Application.Quit();
        }

        #endregion
        
        #region Connection

        // Check if there's an available game server on the Master Server
        private void ServerHostAvailable()
        {
            Debug.Log("Searching for a host");

            // Configure transport to connect to the Master Server
            networkAddress = masterServerAddress;
            var kcpTransport = GetComponent<KcpTransport>();
            kcpTransport.Port = (ushort)masterServerPort;

            // Start the client to connect to the Master Server
            if (!NetworkClient.isConnected)
            {
                StartClient(); // Attempt to connect to the Master Server
                Debug.Log("Client connecting to Master Server...");
            }
        }
        
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            
            Debug.Log($"Player {conn.connectionId} has disconnected from the game, notifying the server");
            
            var accountManager = FindObjectOfType<AccountManager>();
            
            NotifyMasterServerOfAbsence(accountManager.setupAccountData.username);

            if (_connectedClients.ContainsKey(conn.connectionId))
            {
                _connectedClients.Remove(conn.connectionId);
            }
            
            Debug.Log($"Player {conn.connectionId} Has left the server");
        }


        public override void OnServerConnect(NetworkConnection conn)
        {
            base.OnServerConnect(conn);

            _connectedClients[conn.connectionId] = new P2PPlayerInfo
            {
                connId = conn.connectionId,
                ping = 0,
                isHost = false
            };
            
            Debug.Log($"Player Connected {conn.connectionId}");
        }
        
        #endregion
        
        #region Request Push

        public void ReadyUpButtonPressed()
        {
            if (NetworkClient.isConnected)
            {
                var accountManager = FindObjectOfType<AccountManager>();

                var playerReadyPressedMessage = new GameServerPlayerReadiedUpMessageRequest
                {
                    PlayerUsername = accountManager.setupAccountData.username,
                    ServerAddress = cashedServerName
                };

                Debug.Log("Sending message that the ready button has been pressed");
                NetworkClient.Send(playerReadyPressedMessage);


                var playersReadyCheckRequest = new GameServerPlayerReadyCheckMessageRequest
                {
                    ServerAddress = cashedServerName
                };
                
                
                // Check if we are ready to start the game
                // if this comes back as true we will use a handler to begin the migration to the best host.
                NetworkClient.Send(playersReadyCheckRequest);
                
                //Start the P2P server up
                //ReadyToStartP2P();
            }
        }
        
        private void NotifyMasterServerOfAbsence(string playerName)
        {
            var playerLeftMessage = new GameServerPlayerLeftServerRequestMessage
            {
                ServerAddress = cashedServerName,
                PlayerUsername = playerName,
            };


            if (NetworkClient.isConnected)
            {
                Debug.Log($"Attempting to send message to the server about player : {playerName} leaving");
                
                Debug.Log($"The client is connected to the server {NetworkClient.isConnected}");
                NetworkClient.Send(playerLeftMessage);    
                
                Debug.Log($"Message to the server {playerLeftMessage.ServerAddress} {playerLeftMessage.PlayerUsername} Has left sent successfully");
            }
            else
            {
                Debug.Log("The connection to the server is unavailable");
            }
        }

        // Connect to an available host server
        private void ConnectToAvailableServer(GameServerAvailabilityResponseMessage msg)
        {
            // Set the network address and connect to the available server
            Debug.Log($"Connecting to game server at {msg.GameServerAddress}:{msg.GameServerPort}");

            Debug.Log($"NetworkAddress : {networkAddress}");
            
            cashedServerName = msg.GameServerAddress;
            
            // Set the network address and port to connect to the available server
            networkAddress = msg.GameServerAddress;
            var kcpTransport = GetComponent<KcpTransport>();
            kcpTransport.Port = msg.GameServerPort;
            
            
            Debug.Log($"Connecting to the server the Port is : {msg.GameServerPort}");

            // Now, connect the client to the available server
            if (!NetworkClient.isConnected)
            {
                NetworkClient.Connect(msg.GameServerAddress);
                Debug.Log("Connected to available game server.");
            }
            else
            {
                var gameServerJoinedMessage = new GameServerPlayerJoinedServerRequestMessage
                {
                    ServerName = cashedServerName,
                    PlayerName = FindObjectOfType<AccountManager>().setupAccountData.username,
                    IsHost = false,
                    ServerIp = string.Empty
                };
                
                NetworkClient.Send(gameServerJoinedMessage); // Send the message to update the servers player count
            }
        }
        
        // Register the Game Server with the Master Server
        private void RegisterWithMasterServerRequest()
        {
            Debug.Log($"Attempting to register the Game Server with the Master Server at {masterServerAddress}:{masterServerPort}");

            // Configure the transport to use the Master Server's address and port

            var generatedServerName = GenerateServerName();

            cashedServerName = generatedServerName;
            
            networkAddress = masterServerAddress;
            var kcpTransport = GetComponent<KcpTransport>();
            kcpTransport.Port = (ushort)masterServerPort;

            // Send registration message to the Master Server after starting the host
            if (NetworkClient.isConnected)
            {
                var registrationMessage = new GameServerRegistrationMessage
                {
                    ServerAddress = generatedServerName,
                    maxPlayers = 8,
                    hostPlayerIp = _ipManager.GetLocalIPAddress()
                };

                var gameServerJoinedMessage = new GameServerPlayerJoinedServerRequestMessage
                {
                    ServerName = cashedServerName,
                    PlayerName = FindObjectOfType<AccountManager>().setupAccountData.username,
                    IsHost = true,
                    ServerIp = _ipManager.GetLocalIPAddress()
                };
                
                NetworkClient.Send(registrationMessage);  // Send the registration message to the Master Server
                NetworkClient.Send(gameServerJoinedMessage); // Send the message to update the servers player count
                Debug.Log("Game Server registration message sent to the Master Server.");
            }
            else
            {
                Debug.LogError("Cannot send registration message. Client is not connected to the Master Server.");
            }
        }
        
        #endregion

        #region Response Actions

        // Handler for when server availability info is received from the Master Server
        private void OnServerAvailabilityResponse(GameServerAvailabilityResponseMessage msg)
        {
            if (msg.IsHostAvailable)
            {
                Debug.Log($"Host is available with open slots at {msg.GameServerAddress}:{msg.GameServerPort}");
                // Perform logic to connect the game server to the available host
                ConnectToAvailableServer(msg);
            }
            else
            {
                Debug.Log("No hosts available. Registering this game server.");

                // No other servers available, so register this GameServer with the Master Server
                StartHost();  // This starts both the client and the server for players
                
                // Once the game server is started, register it with the Master Server
                RegisterWithMasterServerRequest();
            }
        }

        private void OnPlayerIsHostResponse(PlayerJoinedIsHostResponseMessage msg)
        {
            isHost = msg.IsHost;
            
            Debug.Log($"This player is classified as the host {msg.IsHost}");
        }
        private void OnPlayerLeavingResponse(PlayerLeavingResponseMessage msg)
        {
            if (NetworkClient.isConnected)
            {
                Debug.Log($"Player will now disconnect from the server and switch port");
                
                NetworkClient.Disconnect();
                
                Debug.Log("Player has now disconnected");
            }
        }
        
        private void OnPlayerJoinedResponse(PlayerJoinedSuccessfullyResponseMessage msg)
        {
            if (msg.PlayerJoinedSuccessful)
            {
                Debug.Log($"Player successfully joined the server at {msg.ServerAddress}");
                // Add logic here to transition the player to the game scene, update UI, etc.
            }
            else
            {
                Debug.LogError("Player failed to join the server.");
                // Handle failed join attempts here (e.g., retry or show an error message)
            }
        }

        private void OnPlayerReadyResponse(PlayerReadyResponseMessage msg)
        {
            //TODO: Create logic to force the ready up tab to the same as the ready on the server
            
            Debug.Log($"The Player is Ready: {msg.IsReady}");
        }

        private void OnPlayersReadyCheckResponse(PlayersReadyCheckResponseMessage msg)
        {
            Debug.Log($"All the players are ready {msg.AllPlayersReady}");

            if (msg.AllPlayersReady & msg.EnoughPlayersToStart)
            {
                // begin force to attempt host migration
                
                Debug.Log($"All players are ready and there are enough players to begin the game");
            }
            else
            {
                Debug.Log($"There are enough players {msg.EnoughPlayersToStart} and all players are ready {msg.AllPlayersReady}");
            }
        }
        
        #endregion

        #region Forced Actions

        private void OnLobbySceneCommand(GameServerCommandPushToLobby msg)
        {
            SceneManager.LoadScene("Lobby");
            
            if (isHost)
            {
                Debug.Log("Connected to master time to start P2P");
                StartP2PHosting();
            }
            else
            {
                Debug.Log($"Connecting to P2P Host");
                JoinP2PHost();
            }
            
        }

        #endregion
        
        #region Tools
        private string GenerateServerName()
        {
            return "GameServer_" + System.DateTime.Now.Ticks + "_" + UnityEngine.Random.Range(1, 9999);
        }
        
        #endregion

        #region Debug Tools
        
        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            
            Debug.Log("The Client has Disconnected?");
        }
        
        #endregion
    }
}
