using System;
using kcp2k;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup;

namespace Roadmans_Fortnite.Scripts.Server_Classes.MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {

        private NetworkManager _playerNetworkManager;
        private GameServer _gameServer;

        private void Start()
        {
            _playerNetworkManager = FindObjectOfType<NetworkManager>();
            _gameServer = FindObjectOfType<GameServer>();
        }

        public void EnterLobby()
        {
            // Load the lobby scene
            Debug.Log("Loading lobby scene...");
            //_gameServer.LoadLobbyScene(); // Load the lobby scene
            
            
        }
        
    }
}
