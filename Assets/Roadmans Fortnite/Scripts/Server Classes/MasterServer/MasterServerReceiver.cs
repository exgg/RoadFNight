using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.MasterServer
{
    public class MasterServerReceiver : MonoBehaviour
    {
        private HttpListener _httpListener;
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

                        // Handle the received data here (JSON, messages, etc.)
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

        #endregion


    }
}
