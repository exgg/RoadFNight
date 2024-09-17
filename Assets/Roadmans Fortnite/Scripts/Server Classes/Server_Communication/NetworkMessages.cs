using Mirror;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Server_Communication
{
    #region Requests E2E

    /// <summary>
    /// Used to find whether a server is available with free spaces, if not it will make one
    /// </summary>
    public struct GameServerAvailabilityRequestMessage : NetworkMessage
    {
        public string PlayerIP;
    }
    
    /// <summary>
    /// Once a server has been created or found it will then request to join the server, telling the server
    /// whether this player is the host: The host side is redundant atm however this is fine for now
    /// </summary>
    public struct GameServerPlayerJoinedServerRequestMessage : NetworkMessage
    {
        public string ServerName;
        public string ServerIp;
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


    /// <summary>
    /// This will send a request to check if all the players are ready, which in turn will allow the procedure
    /// of finding the best host before host migration
    /// </summary>
    public struct GameServerPlayerReadyCheckMessageRequest : NetworkMessage
    {
        public string ServerAddress;

        // Send request to check all players in the server
    }

    /// <summary>
    ///  Used to send a request to the host of a server. This will then feed back that the server is active mid-way through. But through another form
    /// Of communication
    /// </summary>
    public struct GameServerStartP2PHostingMessageRequest : NetworkMessage
    {
        // No information needed
    }
    
    
    /// <summary>
    /// Request to the master server to tell the server to change the IP address of the currently connected server
    /// to the IP address of the upcoming new host
    /// </summary>
    public struct GameServerSwapNetworkAddressForHostMessageRequest : NetworkMessage
    {
        public string NewNetAddress;
    }
    
    #endregion

    #region Responses E2E

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
    /// This is a response so that the client can perform an action once the server has received the ready up
    /// </summary>
    public struct PlayerReadyResponseMessage : NetworkMessage
    {
        // add message area if needed
        public bool IsReady;
    }
    
    /// <summary>
    /// Sends back information if the game can start or not, this is currently routed through the master server. This will
    /// need to be moved to the handling of the P2P host instead, to then begin the game.
    /// </summary>
    public struct PlayersReadyCheckResponseMessage : NetworkMessage
    {
        public bool AllPlayersReady;
        public bool EnoughPlayersToStart;
    }

    /// <summary>
    /// Tells the client whether the player who has joined is to be classed as the host of the lobby
    /// </summary>
    public struct PlayerJoinedIsHostResponseMessage : NetworkMessage
    {
        public bool IsHost;
    }
    
    /// <summary>
    /// This is a response for the address swap, telling the client whom is the host to then begin the host migration.
    /// this will now have the IP address setup in the master server for new players to connect to this server instead.
    ///
    /// IMPORTANT. SOMEHOW WE ARE GOING TO HAVE TO TELL THE MASTER SERVER PLAYERS HAVE LEFT???!!
    /// </summary>
    public struct GameServerSwapNetworkAddressForHostMessageResponse : NetworkMessage
    {
    }
    
    #endregion

    #region Commands E2E

    /// <summary>
    /// this will push to the client that joined an available server to the lobby area
    /// </summary>
    public struct GameServerCommandPushToLobby : NetworkMessage
    {
        // Send players to lobby
    }

    #endregion

    #region Debug Messages P2P

    public struct TellHostYouHaveArrivedMessage : NetworkMessage
    {
        public string MessageToHost;
    }

    #endregion
  
    //TODO: 
        // Create messages to choose whether this is game start or game end
        // more messages for handling different aspects of the switching into p2p or back into master server feed 
        // make the master server handle the lobby controls
    
    
        
}