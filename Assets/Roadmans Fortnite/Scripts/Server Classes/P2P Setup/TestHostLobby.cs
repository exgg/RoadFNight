using System;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup
{
    public class TestHostLobby : MonoBehaviour
    {
        [SerializeField]
        private GameServer _gameServer;
        
        private void Start()
        {
            _gameServer = FindObjectOfType<GameServer>();
            
            //_gameServer.StartHostOnLobby();
        }
        
        
    }
}
