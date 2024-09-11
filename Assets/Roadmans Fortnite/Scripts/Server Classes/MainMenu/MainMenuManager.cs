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
        private MasterServerLinkConnector _masterServerLinkConnector;
        
        
        private void Start()
        {
            _playerNetworkManager = FindObjectOfType<NetworkManager>();
            _masterServerLinkConnector = FindObjectOfType<MasterServerLinkConnector>();
        }

        public void EnterLobby()
        {
            // Load the lobby scene
            Debug.Log("Loading lobby scene...");
            //_gameServer.LoadLobbyScene(); // Load the lobby scene
            
            _masterServerLinkConnector.StartGameServer();    
        }

        public void TestSignalToDisconnect()
        {
            _masterServerLinkConnector.WantsToQuit();
        }

        public void TestReadyUp()
        {
            _masterServerLinkConnector.ReadyUpButtonPressed();
        }
        
    }
}
