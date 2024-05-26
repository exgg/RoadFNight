using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public delegate void OnPlayerConnected(NetworkConnection conn);
    public OnPlayerConnected NetworkManagerEvent_OnPlayerConnected;

    public static CustomNetworkManager Instance;

    public override void Awake()
    {
        Instance = this;
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        base.OnServerAddPlayer(conn);
        NetworkManagerEvent_OnPlayerConnected?.Invoke(conn);
    }

    public override void OnServerDisconnect(NetworkConnection conn) 
    {
        conn.identity.gameObject.GetComponent<PlayerInteraction>().Disconnect();

        //clears connection and destroys client owned objects
        base.OnServerDisconnect(conn);
    }

}
