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
            switch (data.playerAction)
            {
                default:
                    Debug.Log($"There was an issue with the action : {data.playerAction} : passed to the server ");
                    break;
                case PlayerActions.PlayerJoined:
                    Debug.Log($"Player {data.playerName} joined the server. {data.serverName}");
                    // TODO: Add logic for player joining the game
                        // Pass information to the hosted master server
                        // Tell the name of the specific master server, use this to then implement the game has increased in players
                        // this is vital since once the first player has joined the game it will require a subsidiary messaging system
                    break;
                case PlayerActions.PlayerLeft:
                    Debug.Log($"Player {data.playerName} left the server. {data.serverName}");
                    // TODO: Add logic for player leaving push this to the master server within the network manager
                        // Pass this information through, force an event within the master server 
                        // parsing in the player name, and the server name. For it then to allocate a new spot availability
                        // for new players to join
                    break;
                case PlayerActions.PlayerBeganHosting:
                    Debug.Log($"Player {data.playerName} has began hosting a new server");

                    foreach (var server in _masterServer.AvailableGameServers)
                    {
                        if (server.ServerName == data.serverName)
                        {
                            Debug.Log($"We have found the server {data.serverName} in the available servers");
                        }
                    }
                    
                    // TODO: Add logic to direct the lobby IP to the server explaining it can now be joined
                        // This will need a bool value of something like IsSetup
                        // once it is setup then players will be allowed to join this server once in matchmaking
                        // eventually this will call for a revamp of the matchmaking due to 
                        // there being much more efficient ways of managing match making once the amount of
                        // players spike to much higher regular play rate
                    break;
            }
        }

        #endregion

    }
}
