using System;
using System.Collections.Generic;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.Lobby;
using TMPro;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Network_Behaviour
{
    public class PlayerConnectionManager : NetworkBehaviour
    {
        private Dictionary<int, int> _playerPings = new Dictionary<int, int>();

        private GameObject _playerLobbyPrefab;
        private LobbyManagement _lobbyManagement;

        public List<GameObject> playerSlots; // slots for players
        
   
        
        public GameObject testLogic;
        
        private void Start()
        {
            AddPlayer("Fred", -1,testLogic);
        }

        public void AddPlayer(string playerName, int _playersAdded, GameObject playerLobbyPrefab)
        {
            _playersAdded++;

            var searchingSlot = playerSlots[_playersAdded].transform.Find("Searching").gameObject;
            
            searchingSlot.SetActive(false);
                
            GameObject lobbyPlayerClone = Instantiate(playerLobbyPrefab, playerSlots[_playersAdded].transform);
            lobbyPlayerClone.transform.Find("Player_Name_Area").transform.GetComponentInChildren<TextMeshPro>().text =
                playerName;
        }


        public override void OnStartClient()
        {
            base.OnStartClient();

            if (isLocalPlayer)
            {
                // this client is a player start some actions for ping checkign etc
            }
        }
        
        // Command to send over ping for clients
        [Command]
        public void CmdReportPingToServer(int ping)
        {
            if(!isServer)
                return;

            int connId = connectionToClient.connectionId;
            _playerPings[connId] = ping;
            Debug.Log($"Ping received from player {connId} : {ping}ms");
        }
        
        // example rpc to request all players to report their ping to the server
        [ClientRpc]
        public void RequestPingFromPlayers()
        {
            if (isLocalPlayer)
            {
                // calculate the ping (use network time.rtt * 1000)
                int ping = (int)(NetworkTime.rtt * 1000);
                // send ping to server
                CmdReportPingToServer(ping);
            }
        }
    }
}
