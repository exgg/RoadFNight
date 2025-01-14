using System;
using kcp2k;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.Account_Management;
using Roadmans_Fortnite.Scripts.Server_Classes.NetworkMessenger;
using Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup;

namespace Roadmans_Fortnite.Scripts.Server_Classes.MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {

        private NetworkManager _playerNetworkManager;
        private NetworkingConnectionManager _networkingConnectionManager;
        private MasterServerMessenger _masterServerMessenger;
        private AccountManager _accountManager;
        
        private void Start()
        {
            _playerNetworkManager = FindObjectOfType<NetworkManager>();
            _networkingConnectionManager = FindObjectOfType<NetworkingConnectionManager>();
            _masterServerMessenger = FindObjectOfType<MasterServerMessenger>();
            _accountManager = FindObjectOfType<AccountManager>();
        }

        public void EnterLobby()
        {
            // Load the lobby scene
            Debug.Log("Loading lobby scene...");
            //_gameServer.LoadLobbyScene(); // Load the lobby scene
            _networkingConnectionManager.StartGameServer();    
        }

        public void TestSignalToDisconnect()
        {
            _networkingConnectionManager.WantsToQuit();
        }

        public void TestReadyUp()
        {
            _networkingConnectionManager.ReadyUpButtonPressed();
        }
        
    }
}
