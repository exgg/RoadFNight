/////////////////////////////////////////////////////////////////////////////////
//
//  MPChatConnection.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	The base class used for main chat within lobby, and "under 
//                  hood" operations throughout the game. Friends play is largely
//                  based within this script.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using System.Collections.Generic;
    using UnityEngine;
    using Photon.Chat;
    using ExitGames.Client.Photon;
    using Opsive.Shared.Game;
    using Opsive.Shared.Events;

    public class MPChatConnection : MonoBehaviour, IChatClientListener
    {
        public static MPChatConnection Instance;

        [Tooltip("after this amount of time if connection attempt fails, the script will attempt to reconnect.")]
        [SerializeField] protected float m_LogOnTimeOut = 30f;// if a stage in the initial connection process stalls for more than this many seconds, the connection will be restarted
        [Tooltip("after this many connection attempts, the script will abort and return to main menu, 0 = unlimited")]
        [SerializeField] protected int m_MaxConnectionAttempts = 0;// after this many connection attempts, the script will abort, 0 = unlimited

        //cache of last active PWF challenge players
        private string[] m_TempPWFlog = new string[2];

        //main chat setup reqs
        private readonly ConnectionProtocol m_ConnectProtocol = ConnectionProtocol.Udp;
        private ChatClient m_ChatClient;

        //base friends channel
        private const string k_FriendsChannel = "friends";
        private const string k_GlobalChannel = "Global";

        protected int m_ConnectionAttempts = 0;//tracking
        public static bool StayConnected = false;
        public static bool Connected = false;
        protected ChatState m_LastChatState = ChatState.Disconnected;
        protected static ScheduledEventBase m_ConnectionTimer = null;

        #region MONOBEHAVIOUR CALLBACKS
        private void OnEnable()
        {
            Instance = this;
            EventHandler.RegisterEvent("OnConnected", Connect);
        }
        private void OnDisable()
        {
            Instance = null;
            EventHandler.UnregisterEvent("OnConnected", Connect);
        }

        void OnApplicationQuit()
        {
            Disconnect();
        }

        void Update()
        {
            if (m_ChatClient != null)
            {
                m_ChatClient.Service();
            }

            UpdateConnectionState();
        }
        #endregion

        public void Disconnect()
        {
            if (m_ChatClient != null) { m_ChatClient.Disconnect(); }
        }

        /// <summary>
        ///	detects cases where the connection process has stalled,
        ///	disconnects and tries to connect again
        /// </summary>
        protected virtual void UpdateConnectionState()
        {
            if (MPConnection.InternetReachability == NetworkReachability.NotReachable)
                return;

            if (!StayConnected)
                return;

            if (m_ChatClient == null)
                return;

            if (m_ChatClient.State != m_LastChatState)
            {
                string s = "Chat State-" + m_ChatClient.State.ToString();
                s = ((m_ChatClient.State == ChatState.ConnectedToFrontEnd) ? "--- " + s + " ---" : s);
                if (s == "ConnectingToFrontEnd")
                    s = "Connecting to chat ...";
               // Debug.Log(s);
              //  MPDebug.Log(s);
            }

            Connected = m_ChatClient.CanChat;

            if (Connected)
            {
                if (m_ConnectionTimer != null)
                {
                    //  Debug.Log("MPChatConnection -Reset Connection Timer");
                    Scheduler.Cancel(m_ConnectionTimer);
                    m_ConnectionTimer = null;
                    m_ConnectionAttempts = 0;
                }
            }
            else if ((m_ChatClient.State != m_LastChatState) && m_ConnectionTimer == null)
            {
                Reconnect();
              
            }

            m_LastChatState = m_ChatClient.State;
        }

        /// <summary>
        /// used internally to disconnect and immediately reconnect
        /// </summary>
        protected virtual void Reconnect()
        {
            if (Connected)
                return;
            StayConnected = true;
            Debug.Log("Chat -Reconnect()");
            if (m_ChatClient.State != ChatState.Disconnected
                && m_ChatClient.State != ChatState.ConnectingToFrontEnd)
            {
                Debug.Log("Chat -Reconnect() > Disconnecting before Connect()");
                Disconnect();
            }

            Connect();
        }

        public void Connect()
        {
            if (Connected)
                return;
            if (m_ConnectionTimer != null)
                return;

            string appID = Photon.Pun.PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat;

            if (string.IsNullOrEmpty(appID))
            {
                Debug.LogError("App Chat ID needs to be entered!");
                return;
            }

            StayConnected = true;
            //create our chatclient
            m_ChatClient = new ChatClient(this, m_ConnectProtocol);
            //m_ChatClient.UseBackgroundWorkerForSending = true;
            // if we like we can set our region..
            // chatClient.ChatRegion = "US";

            //Make sure to use app version for distinction between updates.
            string appVersion = Photon.Pun.PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion;

            //use our custom authValues
            AuthenticationValues authValues = new AuthenticationValues
            {
                UserId = Photon.Pun.PhotonNetwork.AuthValues.UserId,
                AuthType = CustomAuthenticationType.None
            };

            //connect to the server
            m_ChatClient.Connect(appID, appVersion, authValues);

            //schedule a connection timeout, and retry if it fails and theres remaining connecvtion attempts.
            m_ConnectionTimer = Scheduler.Schedule(m_LogOnTimeOut, delegate ()
            {
                m_ConnectionAttempts++;
                if (m_ConnectionAttempts < m_MaxConnectionAttempts || m_MaxConnectionAttempts == 0)
                {
                    Debug.Log("Chat -Retrying (" + m_ConnectionAttempts + ") ...");
                    MPDebug.Log("Chat -Retrying (" + m_ConnectionAttempts + ") ...");
                    Reconnect();
                }
                else
                {
                    Debug.Log("Chat -Failed to connect (tried " + m_ConnectionAttempts + " times).");
                    MPDebug.Log("Chat -Failed to connect (tried " + m_ConnectionAttempts + " times).");
                    Disconnect();
                }
            });
        }

        /// <summary>
        /// Transmits a message over the global chat
        /// </summary>
        /// <param name="message">the message to send</param>
        /// <param name="channel">the channel to send on : Default is friends channel </param>
        public void Send(string message, string channel = k_FriendsChannel)
        {
            if (Check(message, channel))
                m_ChatClient.PublishMessage(channel, message);
            //else Debug.Log("Message Could not be published");
        }
        /// <summary>
        /// We perform actions we need upon connection here
        /// </summary>
        public void OnConnected()
        {
          //  Debug.Log("Lobby chat Connected");

            m_ChatClient.Subscribe(new string[] { k_FriendsChannel, k_GlobalChannel }); //subscribe to chat channel once connected to server, not needed for privates, for now we will have "friends chat" on hand if need be
        }

        public void OnDisconnected()
        {
            //  Debug.Log("Lobby chat Disconnected");
            channelCount = 0;
        }
        /// <summary>
        /// sends a PWF match request to a friend
        /// </summary>
        /// <param name="receiverId">the challenged friends player ID</param>
        public void SendPWFRequestTo(string receiverId)
        {
            if (CMD_SendMatchRequest(receiverId, out string r))
            {
                //log this request, we use the data for accept/decline + making rooms
                LogPWFRequest(Photon.Pun.PhotonNetwork.AuthValues.UserId, receiverId);

                if (!Check(r, /*k_FriendsChannel*/"*" + receiverId))
                    return;

                //TEST THIS FURTHER : USE PRIVATE CHANNELS
                m_ChatClient.SendPrivateMessage(receiverId, r);
                // m_ChatClient.PublishMessage(k_FriendsChannel, r);
            }
        }

        public void StartPWFChallenge()
        {
            m_ChatClient.SendPrivateMessage(m_TempPWFlog[1], "StartMatch");
        }

        /// <summary>
        /// Declines a PWF match request by sending the player that sent the original request a decline reply
        /// </summary>
        public void DeclinePWFRequest()
        {
            if (!Check(CMD_Send_DeclineMatchRequest(), "*" + m_TempPWFlog[0]))
                return;

            m_ChatClient.SendPrivateMessage(m_TempPWFlog[0], CMD_Send_DeclineMatchRequest());
        }
        /// <summary>
        /// Accepts a PWF match request by sending the player that sent the original request an accept reply
        /// </summary>
        public void AcceptPWFRequest()
        {
            if (!Check(CMD_Send_AcceptMatchRequest(), "*" + m_TempPWFlog[0]))
                return;

            m_ChatClient.SendPrivateMessage(m_TempPWFlog[0], CMD_Send_AcceptMatchRequest());
        }
        /// <summary>
        /// returns true if sendable, else will cache for later when connected and return false
        /// </summary>
        /// <param name="mssg">the message that we wish to send</param>
        /// <returns></returns>
        private bool Check(string mssg, string channel)
        {
            if (m_ChatClient == null || !m_ChatClient.CanChat/*CanChatInChannel(channel)*/)
            {
                Debug.Log("Message Could not be published: Chat Client has not been created yet! Caching the message!");
                //Connection is not yet ready for chat, but we can cache what was wanted to be sent, then send it all once connected.
                if (!mssgCache.Contains(new string[] { mssg, channel }))
                {
                    mssgCache.Add(new string[] { mssg, channel });
                }
                return false;
            }
            return true;
        }

        private void SendCachedCommands()
        {
            for (int i = 0; i < mssgCache.Count; i++)
            {
                //first check if match request
                if (mssgCache[i][1].StartsWith("*"))
                {
                    string playerId = mssgCache[i][1].Remove(0);
                    m_ChatClient.SendPrivateMessage(playerId, mssgCache[i][0]);
                    continue;
                }

                m_ChatClient.PublishMessage(mssgCache[i][1], mssgCache[i][0]);
            }

            mssgCache.Clear();

            //  Debug.Log("Sent and cleared message cache...");
        }

        private List<string[]> mssgCache = new List<string[]>();

        /// <summary>
        /// returns true if the reciever ID is valid
        /// </summary>
        /// <param name="recieverID">the player we want to challenge</param>
        /// <param name="result">the command required to send the request</param>
        /// <returns></returns>
        private bool CMD_SendMatchRequest(string recieverID, out string result)
        {
            result = "";
            if (string.IsNullOrEmpty(recieverID))
                return false;
            result = "pwfReq/" + recieverID;
            return true;
        }

        private string CMD_Send_AcceptMatchRequest()
        {
            return "pwfAcc/" + Photon.Pun.PhotonNetwork.AuthValues.UserId;
        }
        private string CMD_Send_DeclineMatchRequest()
        {
            return "pwfDec/" + Photon.Pun.PhotonNetwork.AuthValues.UserId;
        }
        private string CMD_Reply_AcceptPWFMatchRequest(string sender)
        {
            return "pwfAcc/" + sender;
        }
        private string CMD_Reply_DeclinedPWFMatchRequest(string sender)
        {
            return "pwfDec/" + sender;
        }
        public void LogPWFRequest(string player1, string player2)
        {
            MPDebug.Log("LogPWFRequest player1: " + player1 + " -- player2:" + player2);
            Debug.Log("LogPWFRequest player1: " + player1 + " -- player2:" + player2);

            m_TempPWFlog[0] = player1;
            m_TempPWFlog[1] = player2;
        }

        private string IncomingPWFMatchRequest { get { return "pwfReq/" + Photon.Pun.PhotonNetwork.AuthValues.UserId; } }

        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {

            //   Debug.Log(channelName);
            int msgCount = messages.Length;
            if (channelName == k_GlobalChannel)
            {
                for (int i = 0; i < msgCount; i++)
                    EventHandler.ExecuteEvent<string, MPChatInput.MessageType, bool>("OnGetMessage", (string)messages[i], senders[i] == Photon.Pun.PhotonNetwork.AuthValues.UserId ? MPChatInput.MessageType.Player : MPChatInput.MessageType.Remote, true);
                return;
            }

            for (int i = 0; i < msgCount; i++)
            {
                //go through each received msg
                string sender = senders[i];
                string msg = (string)messages[i];
                Debug.Log(sender + " : " + msg);
            }
        }
        public static bool blockChallenge;
        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            string msg = (string)message;

            Debug.Log(sender + " : " + msg);


            if (msg == IncomingPWFMatchRequest)
            {
                if (!blockChallenge)
                {
                    //cache this request, this is a replica of what we want when player send this request, so sender is first, we use the data for accept/decline + making rooms
                    LogPWFRequest(sender, Photon.Pun.PhotonNetwork.AuthValues.UserId);
                    //ShowReceiveChallengePopup(true);
                    Debug.Log("GOT PUN PRIVATE FRIENDS REQUEST");
                }
                else blockChallenge = false;
            }
            if (msg == "StartMatch")
            {
                if (Photon.Pun.PhotonNetwork.AuthValues.UserId == sender)
                { }
                else
                {
                    //ShowChallengeHasSent(false);
                    MPConnection.Instance.TryPlayWithFriends(m_TempPWFlog);
                }

                Debug.Log("GOT PUN PRIVATE FRIENDS REQUEST REPLY : MATCH START!");
            }
            //NOTE: here, we check both players. TryPlayWithFriends() makes the decision on who does what with the log info
            if (msg == CMD_Reply_AcceptPWFMatchRequest(m_TempPWFlog[0]) || msg == CMD_Reply_AcceptPWFMatchRequest(m_TempPWFlog[1]))
            {
                if (Photon.Pun.PhotonNetwork.AuthValues.UserId == sender)
                {
                    //ShowChallengeHasSent(true);
                }
                else
                {
                    //ShowChallengeHasSent(false);
                    //ShowChallengeAcceptedPopup(true);
                }
                Debug.Log("GOT PUN PRIVATE FRIENDS REQUEST REPLY : Accepted!");
            }
            if (msg == CMD_Reply_DeclinedPWFMatchRequest(m_TempPWFlog[1]))
            {
                Debug.Log("GOT PUN PRIVATE FRIENDS REQUEST REPLY : Declined!");
                /* IsChallengeActive = false;

                     if (Photon.Pun.PhotonNetwork.AuthValues.UserId == m_TempPWFlog[0])
                     {
                         ShowCantPlayPopup(true);
                         ShowMessage("Player declined the match!");
                     }

                 ShowChallengeHasSent(false);
                 ShowChallengeAcceptedPopup(false);*/
            }
        }

        public void DebugReturn(DebugLevel level, string message)
        {
          //  MPDebug.Log("MPChatConnection.cs > DebugReturn:" + message);
          //  Debug.Log("MPChatConnection.cs > DebugReturn:" + message);
        }
        private int channelCount = 0;
        public void OnSubscribed(string[] channels, bool[] results)
        {
            for (int i = 0; i < channels.Length; i++)
                channelCount++;

            if (channelCount <= 1)
                return;

            MPChatInput.Initialize();

            SendCachedCommands();
        }
        public void OnUserSubscribed(string channel, string user)
        {
            // Debug.Log("user : " + user + " Subscribed to channel : " + channel);
        }
        public void OnChatStateChange(ChatState state)
        {
           // MPDebug.Log("Chat state: " + state);
           // Debug.Log("Chat state: " + state);

            if (state == ChatState.Disconnected)
            {
                Connect();
            }
        }
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
        {
        
        }

        public void OnUnsubscribed(string[] channels)
        {
            channelCount--;
        }

        public void OnUserUnsubscribed(string channel, string user)
        {
            // Debug.Log("user : " + user + " Unsubscribed from channel : " + channel);
        }
    }
}