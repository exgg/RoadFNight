using System;
using UnityEngine;
using System.Collections.Generic;
using Mirror;

namespace Roadmans_Fortnite.Scripts.Server_Classes.MasterServer
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }
        
       
    }
}
