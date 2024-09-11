using Mirror;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Server_Communication
{
    #region Requests

    /// <summary>
    /// Used to find whether a server is available with free spaces, if not it will make one
    /// </summary>
    public struct GameServerAvailabilityRequestMessage : NetworkMessage
    {
        // You can add additional fields if necessary, but for now, this message is just a request
    }
    
    /// <summary>
    /// Once a server has been created or found it will then request to join the server, telling the server
    /// whether this player is the host: The host side is redundant atm however this is fine for now
    /// </summary>
    public struct GameServerPlayerJoinedServerRequestMessage : NetworkMessage
    {
        public string ServerAddress;
        public ushort ServerPort;
        public string PlayerName;
        public bool IsHost;
    }
    
    /// <summary>
    /// Sends a request to the server to force the player logged to leave then disconnect
    /// </summary>
    public struct GameServerPlayerLeftServerRequestMessage : NetworkMessage
    {
        public string ServerAddress;
        public string PlayerUsername;
    }

    /// <summary>
    /// Sends a request to the server to force the Ready Up so the server knows when it can be used to send over the players into the game 
    /// </summary>
    public struct GameServerPlayerReadiedUpMessageRequest : NetworkMessage
    {
        public string PlayerUsername;
        public string ServerAddress;
    }
    
    #endregion


    #region Responses

    /// <summary>
    /// Tells the client that there is a server available and allows them to join in
    /// </summary>
    public struct GameServerAvailabilityResponseMessage : NetworkMessage
    {
        public bool IsHostAvailable;
        public string GameServerAddress;
        public ushort GameServerPort;
    }

    /// <summary>
    /// Redirects the player to a server with the correct port
    /// </summary>
    public struct GameServerRedirectResponseMessage : NetworkMessage
    {
        public string GameServerAddress;
        public ushort GameServerPort;
    }
    
    /// <summary>
    /// Tells the client they have successfully joined, this realistically should then force then to join the same lobby as everyone else
    /// </summary>
    public struct PlayerJoinedSuccessfullyResponseMessage : NetworkMessage
    {
        public bool PlayerJoinedSuccessful;
        public string ServerAddress;
    }

    /// <summary>
    /// Sends a response message to the client to tell them to disconnect from the server it was connected to
    /// </summary>
    public struct PlayerLeavingResponseMessage : NetworkMessage
    {
        // Add message if i need it
    }

    /// <summary>
    /// This is a response so that the client can perform an action once the server has recieved the ready up
    /// </summary>
    public struct PlayerReadyResponseMessage : NetworkMessage
    {
        // add message area if needed
        public bool IsReady;
    }
    #endregion
    
}