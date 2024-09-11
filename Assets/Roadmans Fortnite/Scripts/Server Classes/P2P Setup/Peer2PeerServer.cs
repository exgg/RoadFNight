using System;
using System.Collections;
using System.Linq;
using System.Net;
using Mirror;
using Telepathy;
using Unity.VisualScripting;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup
{
    
    public class P2PPlayerInfo
    {
        public NetworkConnection conn;
        public string playerIP;
        public int ping;

        public P2PPlayerInfo(NetworkConnection connection)
        {
            conn = connection;
            playerIP = GetPlayerIP(connection);
            ping = -1;  // Placeholder, ping is calculated later
        }

        private string GetPlayerIP(NetworkConnection connection)
        {
            try
            {
                // Get the endpoint of the connection
                string endpoint = connection.address;

                // Extract the IP address from the endpoint
                IPAddress ip = IPAddress.Parse(endpoint.Split(':')[0]);
                return ip.ToString();
            }
            catch
            {
                Debug.LogWarning($"Failed to get IP for player {connection.connectionId}");
                return "Unknown";
            }
        }
    }
    public class Peer2PeerServer : NetworkManager
    {
        public bool isHost;
        
        private string _serverAddress;
        private IPManager _ipManager;
        
        public override void Start()
        {
            base.Start();

            _ipManager = FindObjectOfType<IPManager>();

            networkAddress = _ipManager.GetLocalIPAddress();
            
            StartServer();
            
            StartClient();
        }

        #region Initialization

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            Debug.Log("Peer2Peer server has started");
            
            isHost = true;
        }
        
        #endregion
        
        #region Client Interaction (All Players)

        

        #endregion


        #region Server Interaction (Host Only)

        

        #endregion
       
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            base.OnServerAddPlayer(conn);

            Debug.Log($"OnServerAddPlayer: Player {conn.connectionId} joined");

            // Add the player info to the server
            P2PPlayerInfo playerInfo = new P2PPlayerInfo(conn);
    
            Debug.Log($"Player {conn.connectionId} with IP {playerInfo.playerIP} has joined");

            // Optional: Start pinging the player for network quality checks
            StartCoroutine(PingPlayer(playerInfo));
        }

        private IEnumerator PingPlayer(P2PPlayerInfo playerInfo)
        {
            while (true)
            {
                // workout logic to test for ping iterations
                
                // sub the ping to the player
                
                // wait 5 seconds to repeat
                yield return new WaitForSeconds(5f);
            }
        }
        
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            
            Debug.Log($"Player {conn.connectionId} disconnected");

            if (isHost)
            {
                // TODO Handle host migration
                
                CheckHostMigration();
            }
        }
        
        public override void OnServerConnect(NetworkConnection conn)
        {
            base.OnServerConnect(conn);

            Debug.Log($"OnServerConnect: Player Client with connectionId {conn.connectionId} connected");
        }

        public void StartGame()
        {
            if (isHost)
            {
                Debug.Log("Host is starting the game");
                ServerChangeScene("GameScene");
            }
        }

        private void CheckHostMigration()
        {
            // Check for host migration then handle transition
            if (NetworkServer.connections.Count > 0)
            {
                // Migrate to new host
                MigrateToNewHost();
            }
            else
            {
                Debug.Log("No players left to migrate, shutting down the P2P server");
                StopServer();
            }
        }

        private void MigrateToNewHost()
        {
            // elect new host based on ping

            NetworkConnection newHostConnection = FindBestCandidate();

            if (newHostConnection == null)
                return;
            
            Debug.Log($"Migrating to the new host {newHostConnection.connectionId}");
        }

        private NetworkConnection FindBestCandidate()
        {
            P2PPlayerInfo bestCandidate = null;

            foreach (var conn in NetworkServer.connections.Values)
            {
                P2PPlayerInfo playerInfo = new P2PPlayerInfo(conn);

                if (bestCandidate == null || playerInfo.ping < bestCandidate.ping)
                {
                    bestCandidate = playerInfo;
                }
            }

            return bestCandidate?.conn; // check if not null then send the best candidate back
        }
    }
}
