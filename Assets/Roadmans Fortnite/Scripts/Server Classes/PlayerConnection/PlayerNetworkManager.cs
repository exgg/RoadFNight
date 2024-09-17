using Mirror;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.PlayerConnection
{
    public class PlayerNetworkManager : NetworkManager
    {
        // Called when a player is added to the server (this will happen when they join the game scene)
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            // Instantiate the player prefab for the connected client in the game scene
            GameObject player =
                Instantiate(playerPrefab, Vector3.zero, Quaternion.identity); // Spawns at default position

            // Assign this player object to the client connection
            NetworkServer.AddPlayerForConnection(conn, player);

            Debug.Log($"Player {conn.connectionId} has been added to the game.");
        }

        // Called when a player disconnects from the server
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            // Handle the player's disconnection (removal, cleanup, etc.)
            Debug.Log($"Player {conn.connectionId} has disconnected from the game.");

            // Remove the player object from the game scene
            base.OnServerDisconnect(conn);
        }

        // You can add more player-specific logic here if needed
    }
}
