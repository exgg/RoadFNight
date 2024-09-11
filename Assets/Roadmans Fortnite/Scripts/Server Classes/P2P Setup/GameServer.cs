using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using kcp2k;
using Roadmans_Fortnite.Scripts.Server_Classes.Account_Management;
using Roadmans_Fortnite.Scripts.Server_Classes.Session_Creation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup
{
    
    public struct GameServerRegistrationMessage : NetworkMessage
    {
        public string ServerAddress;
        public int maxPlayers;
    }
    public class GameServer : NetworkManager
    {
        public string masterServerAddress = "localhost"; // Master Server IP
        public int masterServerPort = 8888; // Master Server port
        
        [SerializeField]
        private string _cashedServerName;
        
        public override void Start()
        {
            base.Start();
            Debug.Log("Game Server starting...");

            // Try to find an available server or register as a new one
            //StartGameServer();
            
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

        // Check if there's an available game server on the Master Server
        private void ServerHostAvailable()
        {
            Debug.Log("Searching for a host");

            // Configure transport to connect to the Master Server
            networkAddress = masterServerAddress;
            var transport = GetComponent<KcpTransport>();
            transport.Port = (ushort)masterServerPort;

            // Start the client to connect to the Master Server
            if (!NetworkClient.isConnected)
            {
                StartClient(); // Attempt to connect to the Master Server
                Debug.Log("Client connecting to Master Server...");
            }
        }

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
                RegisterWithMasterServer();
            }
        }

        // Connect to an available host server
        private void ConnectToAvailableServer(GameServerAvailabilityResponseMessage msg)
        {
            // Set the network address and connect to the available server
            Debug.Log($"Connecting to game server at {msg.GameServerAddress}:{msg.GameServerPort}");

            Debug.Log($"NetworkAddress : {networkAddress}");
            
            _cashedServerName = msg.GameServerAddress;
            
            // Set the network address and port to connect to the available server
            networkAddress = msg.GameServerAddress;
            var transport = GetComponent<KcpTransport>();
            transport.Port = msg.GameServerPort;
            
            
            Debug.Log($"Connecting to the server the Port is : {msg.GameServerPort}");

            // Now, connect the client to the available server
            if (!NetworkClient.isConnected)
            {
                NetworkClient.Connect(msg.GameServerAddress);
                Debug.Log("Connected to available game server.");
            }
            else
            {
                var gameServerJoinedMessage = new GameServerPlayerJoinedServer
                {
                    ServerAddress = _cashedServerName,
                    PlayerName = FindObjectOfType<AccountManager>().setupAccountData.username,
                    IsHost = false
                };
                
                NetworkClient.Send(gameServerJoinedMessage); // Send the message to update the servers player count
            }
        }

        // Register the Game Server with the Master Server
        private void RegisterWithMasterServer()
        {
            Debug.Log($"Attempting to register the Game Server with the Master Server at {masterServerAddress}:{masterServerPort}");

            // Configure the transport to use the Master Server's address and port

            var generatedServerName = GenerateServerName();

            _cashedServerName = generatedServerName;
            
            networkAddress = masterServerAddress;
            var transport = GetComponent<KcpTransport>();
            transport.Port = (ushort)masterServerPort;

            // Send registration message to the Master Server after starting the host
            if (NetworkClient.isConnected)
            {
                var registrationMessage = new GameServerRegistrationMessage
                {
                    ServerAddress = generatedServerName,
                    maxPlayers = 8
                };

                var gameServerJoinedMessage = new GameServerPlayerJoinedServer
                {
                    ServerAddress = _cashedServerName,
                    PlayerName = FindObjectOfType<AccountManager>().setupAccountData.username,
                    IsHost = true,
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

        // This method is called when the client successfully connects to the Master Server
        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            Debug.Log("Game Server successfully connected to the Master Server.");

            // Register handler to listen for available game servers after the connection is established
            NetworkClient.RegisterHandler<GameServerAvailabilityResponseMessage>(OnServerAvailabilityResponse);
            
            // register handler for response of joining game
            NetworkClient.RegisterHandler<PlayerJoinedSuccessfullyResponse>(OnPlayerJoinedResponse);
            
            // register handler for response of leaving, which will push to leave the connection
            NetworkClient.RegisterHandler<PlayerLeavingResponseMessage>(OnPlayerLeavingResponse);
            
            // Send a request to the Master Server to check for available servers
            var request = new GameServerAvailabilityRequestMessage();
            NetworkClient.Send(request);
            Debug.Log("Request for available game server sent to the Master Server.");
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            
            Debug.Log($"Player {conn.connectionId} has disconnected from the game, notifying the server");
            
            var _accountManager = FindObjectOfType<AccountManager>();
            
            NotifyMasterServerOfAbsence(_accountManager.setupAccountData.username);
        }

        public bool WantsToQuit()
        {
            Debug.Log("User clicked to close the game down trying to catch it to send master server a message");
            
            if (NetworkClient.isConnected)
            {
                Debug.Log("Sending disconnection message before quitting...");
                
                var _accountManager = FindObjectOfType<AccountManager>();
                
                // send master server the message of leaving
                NotifyMasterServerOfAbsence(_accountManager.setupAccountData.username);
                
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
        
        private void NotifyMasterServerOfAbsence(string playerName)
        {
            var PlayerLeftMessage = new GameServerPlayerLeftServerMessage
            {
                ServerAddress = _cashedServerName,
                PlayerUsername = playerName,
            };


            if (NetworkClient.isConnected)
            {
                Debug.Log($"Attempting to send message to the server about player : {playerName} leaving");
                
                Debug.Log($"The client is connected to the server {NetworkClient.isConnected}");
                NetworkClient.Send(PlayerLeftMessage);    
                
                Debug.Log($"Message to the server {PlayerLeftMessage.ServerAddress} {PlayerLeftMessage.PlayerUsername} Has left sent successfully");
            }
            else
            {
                Debug.Log("The connection to the server is unavailable");
            }
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
        
        #region Tools
        private string GenerateServerName()
        {
            return "GameServer_" + System.DateTime.Now.Ticks + "_" + UnityEngine.Random.Range(1, 9999);
        }
        
        #endregion

        #region Debug Tools

        private void OnPlayerJoinedResponse(PlayerJoinedSuccessfullyResponse msg)
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
        
        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            
            Debug.Log("The Client has Disconnected?");
        }
        
        #endregion
    }
}
