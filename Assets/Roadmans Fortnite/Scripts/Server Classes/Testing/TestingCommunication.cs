using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.Account_Management;
using Roadmans_Fortnite.Scripts.Server_Classes.NetworkMessenger;
using Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Testing
{
    public class TestingCommunication : MonoBehaviour
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


      
    }
}
