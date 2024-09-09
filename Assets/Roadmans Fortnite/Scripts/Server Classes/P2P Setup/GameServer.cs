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
        public string serverName;
        public int maxPlayers;
    }
    public class GameServer : NetworkManager
    {
        public string masterServerAddress = "localhost"; // Master Server IP
        public int masterServerPort = 8888; // Master Server port

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
            if (msg.isHostAvailable)
            {
                Debug.Log($"Host is available with open slots at {msg.gameServerAddress}:{msg.gameServerPort}");
                // Perform logic to connect the game server to the available host
                ConnectToAvailableServer(msg);
            }
            else
            {
                Debug.Log("No hosts available. Registering this game server.");
                // No other servers available, so register this GameServer with the Master Server
                RegisterWithMasterServer();
                StartHost();  // This starts both the client and the server for players
            }
        }

        // Connect to an available host server
        private void ConnectToAvailableServer(GameServerAvailabilityResponseMessage msg)
        {
            // Set the network address and connect to the available server
            Debug.Log($"Connecting to game server at {msg.gameServerAddress}:{msg.gameServerPort}");
            
            // Set the network address and port to connect to the available server
            networkAddress = msg.gameServerAddress;
            var transport = GetComponent<KcpTransport>();
            transport.Port = msg.gameServerPort;

            // Now, connect the client to the available server
            if (!NetworkClient.isConnected)
            {
                NetworkClient.Connect(msg.gameServerAddress);
                Debug.Log("Connected to available game server.");
            }
        }

        // Register the Game Server with the Master Server
        private void RegisterWithMasterServer()
        {
            Debug.Log($"Attempting to register the Game Server with the Master Server at {masterServerAddress}:{masterServerPort}");

            // Configure the transport to use the Master Server's address and port
            networkAddress = masterServerAddress;
            var transport = GetComponent<KcpTransport>();
            transport.Port = (ushort)masterServerPort;

            // Start the client to connect to the Master Server
            if (!NetworkClient.isConnected)
            {
                StartClient();
                Debug.Log("Client connecting to Master Server...");
            }
        }

        // This method is called when the client successfully connects to the Master Server
        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            Debug.Log("Game Server successfully connected to the Master Server.");

            // Register handler to listen for available game servers after the connection is established
            NetworkClient.RegisterHandler<GameServerAvailabilityResponseMessage>(OnServerAvailabilityResponse);

            // Send a request to the Master Server
            var request = new GameServerAvailabilityRequestMessage();
            NetworkClient.Send(request);
            Debug.Log("Request for available game server sent to the Master Server.");
        }

        // Send registration message to the Master Server
        private void SendRegistrationToMasterServer(NetworkConnection conn)
        {
            if (NetworkClient.isConnected)
            {
                // Create and send the registration message to the Master Server
                GameServerRegistrationMessage msg = new GameServerRegistrationMessage
                {
                    serverName = "My Game Server",
                    maxPlayers = 10
                };
                conn.Send(msg);

                Debug.Log("Game Server registration message sent to the Master Server.");
            }
            else
            {
                Debug.LogError("Cannot send registration message. Client is not connected to the Master Server.");
            }
        }
        // Optional response handler for acknowledgment from the Master Server
        private void OnMasterServerResponse(GameServerRegistrationMessage msg)
        {
            Debug.Log("Master Server acknowledged the registration.");
        }
    }
}
