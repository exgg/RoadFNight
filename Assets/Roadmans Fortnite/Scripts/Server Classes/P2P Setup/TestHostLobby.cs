using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup
{
    public class TestHostLobby : MonoBehaviour
    {
        [FormerlySerializedAs("masterServerLinkConnector")] [FormerlySerializedAs("_gameServer")] [SerializeField]
        private NetworkingConnectionManager networkingConnectionManager;
        
        private void Start()
        {
            networkingConnectionManager = FindObjectOfType<NetworkingConnectionManager>();
            
            //_gameServer.StartHostOnLobby();
        }
        
        
    }
}
