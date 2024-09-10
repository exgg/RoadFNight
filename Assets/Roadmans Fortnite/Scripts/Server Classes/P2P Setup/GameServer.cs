using Mirror;
using kcp2k;
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
            StartGameServer();
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

            _cashedServerName = msg.GameServerAddress;
            
            // Set the network address and port to connect to the available server
            networkAddress = msg.GameServerAddress;
            var transport = GetComponent<KcpTransport>();
            transport.Port = msg.GameServerPort;

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

            // Send a request to the Master Server to check for available servers
            var request = new GameServerAvailabilityRequestMessage();
            NetworkClient.Send(request);
            Debug.Log("Request for available game server sent to the Master Server.");
        }

        private string GenerateServerName()
        {
            return "GameServer_" + System.DateTime.Now.Ticks + "_" + UnityEngine.Random.Range(1, 9999);
        }
    }
}
