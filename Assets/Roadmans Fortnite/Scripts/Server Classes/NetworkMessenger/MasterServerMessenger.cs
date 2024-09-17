using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Roadmans_Fortnite.Scripts.Server_Classes.NetworkMessenger
{
    public enum PlayerActions
    {
        PlayerJoined,
        PlayerLeft,
        PlayerCheating,
        PlayerBeganHosting
    }

    public class MasterServerMessenger : MonoBehaviour
    {
        private string _portAddress = "http://localhost:8088";
        
        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void NotifyMasterServerOfPlayerAction(string playerName, PlayerActions playerAction, string serverName)
        {
            StartCoroutine(SendPostRequest(playerName, playerAction, serverName));
        }
        IEnumerator SendPostRequest(string playerName, PlayerActions playerAction, string serverName)
        {
            // Serialize the enum as an integer, not a string
            string jsonData = $"{{\"playerName\" :\"{playerName}\", \"playerAction\" :{(int)playerAction}, \"serverName\" :\"{serverName}\"}}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using UnityWebRequest request = new UnityWebRequest(_portAddress, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send request and wait for the response
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error : {request.error}");
            }
            else
            {
                Debug.Log($"Response : {request.downloadHandler.text}");
            }
        }
    }
            
}
