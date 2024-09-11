using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup
{
    public class TestHostLobby : MonoBehaviour
    {
        [FormerlySerializedAs("_gameServer")] [SerializeField]
        private MasterServerLinkConnector masterServerLinkConnector;
        
        private void Start()
        {
            masterServerLinkConnector = FindObjectOfType<MasterServerLinkConnector>();
            
            //_gameServer.StartHostOnLobby();
        }
        
        
    }
}
