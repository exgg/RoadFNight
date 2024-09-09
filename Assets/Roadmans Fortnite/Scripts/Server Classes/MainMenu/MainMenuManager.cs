using System;
using kcp2k;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace Roadmans_Fortnite.Scripts.Server_Classes.MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {

        private NetworkManager _playerNetworkManager;

        private void Start()
        {
            _playerNetworkManager = FindObjectOfType<NetworkManager>();
        }

        public void EnterLobby()
        {
            if (_playerNetworkManager.isNetworkActive)
            {
                Debug.LogWarning("Client is already running!");
                return; // Avoid starting the client again if it's already active
            }

            // Set the network address and port
            _playerNetworkManager.networkAddress = "localhost";
            KcpTransport transport = _playerNetworkManager.GetComponent<KcpTransport>();

            if (transport != null)
            {
                transport.Port = 8888;
                Debug.Log($"Attempting to connect to server at localhost on port {transport.Port}");
            }
            else
            {
                Debug.LogError("KCPTransport not found on the client.");
                return;
            }

            // Start the client connection
            _playerNetworkManager.StartClient();

            // Log the connection state after attempting to start the client
            Debug.Log($"Client network active: {_playerNetworkManager.isNetworkActive}");
        }
        
    }
}
