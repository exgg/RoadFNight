using Mirror;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.PlayerConnection
{
    public class PlayerConnectionManager : NetworkManager
    {
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            // Instantiate and add the player object for the connection
            GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity); // Spawn at a default position
            NetworkServer.AddPlayerForConnection(conn, player);

            Debug.Log($"Player {conn.connectionId} spawned in the game.");
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            // Handle player disconnection logic here
            Debug.Log($"Player {conn.connectionId} disconnected from the server.");
            base.OnServerDisconnect(conn);
        }
    }
}
