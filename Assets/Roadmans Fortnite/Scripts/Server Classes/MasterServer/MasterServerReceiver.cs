using System;
using System.IO;
using System.Net;
using System.Text;
using Roadmans_Fortnite.Scripts.Server_Classes.NetworkMessenger;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.MasterServer
{
    [Serializable]
    public class PlayerActionData
    {
        public string playerName;
        public PlayerActions playerAction;
        public string serverName;
    }
    
    public class MasterServerReceiver : MonoBehaviour
    {
        private HttpListener _httpListener;
        private NewMasterServer _masterServer;
         
        private void Start()
        {
            StartHttpServer();
            _masterServer = FindObjectOfType<NewMasterServer>();
        }

        #region HTTP Requests
        
        private void StartHttpServer()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:8088/");
            _httpListener.Start();
            Debug.Log("Master server listening for HTTP POST requests...");

            // Run the HTTP listener as a background task
            System.Threading.Tasks.Task.Run(() => ListenForHttpRequests());
        }

        private async System.Threading.Tasks.Task ListenForHttpRequests()
        {
            while (true)
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                HttpListenerRequest request = context.Request;

                if (request.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string body = await reader.ReadToEndAsync();
                        Debug.Log($"Received HTTP POST data: {body}");

                        // Assuming the received data is in JSON format
                        var playerData = JsonUtility.FromJson<PlayerActionData>(body);
                        HandlePlayerAction(playerData);

                        // Send response back to the client
                        byte[] responseBuffer = Encoding.UTF8.GetBytes("Message received");
                        context.Response.ContentLength64 = responseBuffer.Length;
                        context.Response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
                    }

                    context.Response.OutputStream.Close();
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
        }

        private void HandlePlayerAction(PlayerActionData data)
        {
            Debug.Log($"THE PLAYER ACTION IS {data.playerAction}");

            switch (data.playerAction)
            {
                case PlayerActions.PlayerJoined:
                    Debug.Log($"Player {data.playerName} joined the server. {data.serverName}");
                    //TODO: Add logic for adding player to the master server
                    
                    break;
                case PlayerActions.PlayerLeft:
                    Debug.Log($"Player {data.playerName} left the server. {data.serverName}");
                    // TODO : Add logic for telling master server someone left the lobby
                    
                    break;
                case PlayerActions.PlayerBeganHosting:
                    Debug.Log($"Player {data.playerName} has begun hosting a new server");
                    // TODO : Tell server that the game began initial hosting
                    foreach (var server in _masterServer.AvailableGameServers)
                    {
                        if (server.ServerName == data.serverName)
                        {
                            Debug.Log("I found the server that is starting host");
                            server.InitialHosting = true;
                            
                            Debug.Log($"The servers initial hosting is complete : {server.InitialHosting}");
                        }
                    }
                    break;
                case PlayerActions.PlayerCheating:
                    Debug.Log($"Player {data.playerName} is flagged for cheating on server {data.serverName}");
                    // TODO: Add logic to blacklist player, reset account etc
                    break;
                default:
                    Debug.Log($"There was an issue with the action : {data.playerAction} : passed to the server");
                    break;
            }
        }

        #endregion

    }
}
