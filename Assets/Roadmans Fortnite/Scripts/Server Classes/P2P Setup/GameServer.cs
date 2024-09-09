using Mirror;
using kcp2k;
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

            // Start the game server and host players
        }

        public override void OnStartHost()
        {
            base.OnStartHost();
            Debug.Log("Game Server started to host players.");

            
            
            // After starting the host for players, register with the Master Server
            RegisterWithMasterServer();
        }

        // This function starts the Game Server to host players
        public void StartGameServer()
        {
            Debug.Log("Starting the Game Server...");

            // Check if the server or client is already active and skip if true
            if (NetworkServer.active)
            {
                Debug.LogWarning("Server already started. Skipping StartHost().");
                RegisterWithMasterServer();
                return;
            }

            // Start the game server to host players
            StartHost();  // This starts both the client and the server for players
            Debug.Log("Game Server hosting started.");
        }

        // This function connects the Game Server to the Master Server and sends the registration message
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

        // This method will be called when the client successfully connects to the Master Server
        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            Debug.Log("Game Server successfully connected to the Master Server.");

            // Once connected, send the registration message to the Master Server
            SendRegistrationToMasterServer(conn);
        }

        // Send registration message to Master Server
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
