using System;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.Account_Management;
using UnityEngine;
using UnityEngine.UI;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Lobby
{
    public class LobbyPlayerController : NetworkBehaviour
    {
        [SyncVar] public string playerName;
        [SyncVar] public bool isReady;

        public AccountManager accountManager;

        [SerializeField] private RawImage readyImage;
        [SerializeField] private RawImage notReadyImage;
        
        private void SetupPlayerInfo(string pName)
        {
            if (isServer)
            {
                playerName = pName;
            }
        }

        private void Start()
        {
            accountManager = FindObjectOfType<AccountManager>();

            SetupPlayerInfo(accountManager.setupAccountData.username);
        }


        public void ToggleReady()
        {
            if (isServer)
            {
                isReady = !isReady;
                
                Debug.Log($"Ready Changed {isReady}");
            }
        }

        public void OnPlayerLeave()
        {
            if (isServer)
            {
                
            }
        }

        public void OnPlayerJoin()
        {
            if (isServer)
            {
                
            }
        }
        
    }
}
