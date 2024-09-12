using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Roadmans_Fortnite.Scripts.Server_Classes.NetworkMessenger
{
    public class MasterServerMessages : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(gameObject);
            
            StartCoroutine(SendPostRequest());
        }

        IEnumerator SendPostRequest()
        {
            string jsonData = "{\"message\": \"Player has migrated host\"}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest("http://localhost:8088", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Send the request and wait for the response
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {request.error}");
                }
                else
                {
                    Debug.Log($"Response: {request.downloadHandler.text}");
                }
            }
        }
    }
}
