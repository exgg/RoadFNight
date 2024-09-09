using System.Collections.Generic;
using kcp2k;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Session_Creation
{
    public class NewMasterServer : NetworkManager
    {
        // List to track registered game servers
        private List<string> availableGameServers = new List<string>();

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
        }

        // Called when the Master Server receives a registration message from the Game Server
        private void OnGameServerRegistration(NetworkConnection conn, GameServerRegistrationMessage msg)
        {
            Debug.Log($"Received registration from game server: {msg.serverName} with max players: {msg.maxPlayers}");

            // Add the server's connection ID to the list of available game servers
            availableGameServers.Add(conn.connectionId.ToString());

            Debug.Log($"Game server registered. Total available servers: {availableGameServers.Count}");
        }
    }
}
