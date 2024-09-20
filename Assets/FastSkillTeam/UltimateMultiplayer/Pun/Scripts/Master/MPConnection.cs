/////////////////////////////////////////////////////////////////////////////////
//
//  MPConnection.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	Initiates and manages the connection to Photon Cloud, regulates
//					room creation, max player count per room and logon timeout.
//					also keeps the 'IsMultiplayer' and 'IsMaster' flags up-to-date.
//					(these are quite often relied upon by core classes)	
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{

#if ANTICHEAT
    using CodeStage.AntiCheat.ObscuredTypes;
#endif
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Opsive.Shared.Game;
    using Opsive.Shared.Events;
    using FastSkillTeam.UltimateMultiplayer.Pun.UI;
    using FastSkillTeam.UltimateMultiplayer.Pun.Utility;
    using Photon.Pun;
    using Photon.Realtime;
    using Hashtable = ExitGames.Client.Photon.Hashtable;
    using FastSkillTeam.UltimateMultiplayer.Shared.Game;

    public class MPConnection : MonoBehaviourPunCallbacks
    {
        private static NetworkReachability m_InternetReachability;
        public static NetworkReachability InternetReachability => m_InternetReachability;

#if !ANTICHEAT
        [Tooltip("If a stage in the initial connection process stalls for more than this many seconds, the connection will be restarted.")]
        [SerializeField] protected float m_LogOnTimeOut = 20f;
        [Tooltip("After this many connection attempts, the script will abort and return to the main menu.")]
        [SerializeField] protected int m_MaxConnectionAttempts = 10;
        private int m_ConnectionAttempts = 0;
        public float LogOnTimeOut { get { return m_LogOnTimeOut; } set { m_LogOnTimeOut = value; } }
        public int MaxConnectionAttempts { get { return m_MaxConnectionAttempts; } set { m_MaxConnectionAttempts = value; } }
#else
        [Tooltip("If a stage in the initial connection process stalls for more than this many seconds, the connection will be restarted.")]
	    [SerializeField] protected ObscuredFloat m_LogOnTimeOut = 20f;
        [Tooltip("After this many connection attempts, the script will abort and return to the main menu.")]
	    [SerializeField] protected ObscuredInt m_MaxConnectionAttempts = 10;
	    private ObscuredInt m_ConnectionAttempts = 0;
        public ObscuredFloat LogOnTimeOut { get { return m_LogOnTimeOut; } set { m_LogOnTimeOut = value; } }
        public ObscuredInt MaxConnectionAttempts { get { return m_MaxConnectionAttempts; } set { m_MaxConnectionAttempts = value; } }
#endif
        [Tooltip("The scene that will be loaded when the 'Disconnect' method is executed.")]
        [SerializeField] protected string m_SceneToLoadOnDisconnect = "";     // this scene will be loaded when the 'Disconnect' method is executed
        [Tooltip("Enable debug logging for the MPConnection component.")]
        [SerializeField] protected bool m_Debug = false;
        [Tooltip("Enable debug logs to be displayed in the in-game chat.")]
        [SerializeField] protected bool m_DebugToGameChat = false;
        [Tooltip("Determines whether the MPConnection component should be preserved across scene changes.")]
        [SerializeField] protected bool m_DontDestroyOnLoad = true;
        [Tooltip("The interval at which ping reports are sent.")]
        [SerializeField] protected float m_PingReportInterval = 10.0f;
        [Tooltip("Determines whether the connection should be started automatically on scene load or object creation.")]
        [SerializeField] protected bool m_StartConnected = false;
        [Tooltip("Determines whether the scene should be loaded automatically when joining a room.")]
        [SerializeField] protected bool m_LoadSceneOnJoinRoom = true;
        [SerializeField] protected GameObject m_RoomInfoParent;
        [SerializeField] protected GameObject m_RoomInfoPrefab;
        [SerializeField] protected bool m_ExecuteRoomListUpdateEvent = false;
        public bool ExecuteRoomListUpdateEvent { get => m_ExecuteRoomListUpdateEvent; set => m_ExecuteRoomListUpdateEvent = value; }

        private bool m_CanJoinRooms = false;
        private bool m_EarlyJoin;
        private string m_LocalPlayerName = "Player";
        private string m_SceneToLoadName = "";// for room creation, keeps maps seperate and saves people loading unwanted maps // set via MPMenu
        private string m_MapDisplayName = "";
        private string m_GameModeDisplayName = "";//  for room creation, keeps gamemodes seperate and saves people loading unwanted gamemodes// set via MPMenu
        private int m_SelectedModelIndex = 0;
        private int m_LastPing = 0;
        private float m_NextAllowedPingTime = 0.0f;
        private ClientState m_LastClientState = ClientState.Disconnected;
        private Dictionary<string, RoomInfo> m_RoomInfos = new Dictionary<string, RoomInfo>();
        private Dictionary<string, GameObject> m_InstantiatedRoomInfos = new Dictionary<string, GameObject>();

        public string LocalPlayerName { get => m_LocalPlayerName; set => m_LocalPlayerName = value; }
        public string SceneToLoadName { get => m_SceneToLoadName; set => m_SceneToLoadName = value; }
        public string MapDisplayName { get => m_MapDisplayName; set => m_MapDisplayName = value; }
        public string GameModeDisplayName { get => m_GameModeDisplayName; set => m_GameModeDisplayName = value; }
        public int SelectedModelIndex { get => m_SelectedModelIndex; set => m_SelectedModelIndex = value; }
        public bool LoadSceneOnJoinRoom { get => m_LoadSceneOnJoinRoom; set => m_LoadSceneOnJoinRoom = value; }

        private static bool m_StayConnected = false;       // as long as this is true, this component will relentlessly try to reconnect to the photon cloud
        private static ScheduledEventBase m_ConnectionTimer = null;

        public static MPConnection Instance { get; private set; } = null;
        public static bool Connected { get; private set; } = false;

        /// <summary>
        /// A cached list of rooms recieved when roomlist updates on pun. This allows us to access this list at any time.
        /// </summary>
        public Dictionary<string, RoomInfo> RoomInfos => m_RoomInfos;

        /// <summary>
        /// for your custom lobbies, you can use for example => new TypedLobby("MainLobby", LobbyType.SqlLobby);
        /// </summary>
        public static TypedLobby Lobby { get => m_Lobby; set => m_Lobby = value; }

        private static TypedLobby m_Lobby = TypedLobby.Default;

        private void Awake()
        {
            //Only one MP connection is allowed!
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            EventHandler.RegisterEvent<string>("OnSetPlayerName", OnSetPlayerName);
            EventHandler.RegisterEvent<int>("OnSelectCharacter", OnSelectCharacter);
            EventHandler.RegisterEvent<string, string>("OnSelectMap", OnSelectMap);
            EventHandler.RegisterEvent<string>("OnSelectGameMode", OnSelectGameMode);
        }

        private void OnDestroy()
        {
            EventHandler.UnregisterEvent<string>("OnSetPlayerName", OnSetPlayerName);
            EventHandler.UnregisterEvent<int>("OnSelectCharacter", OnSelectCharacter);
            EventHandler.UnregisterEvent<string, string>("OnSelectMap", OnSelectMap);
            EventHandler.UnregisterEvent<string>("OnSelectGameMode", OnSelectGameMode);
        }

        private void OnSelectCharacter(int index)
        {
            m_SelectedModelIndex = index;
        }

        private void OnSetPlayerName(string newName)
        {
            if (m_LocalPlayerName.Equals(newName))
                return;
            
            m_LocalPlayerName = newName;
        }

        public override void OnEnable()
        {
            if (Instance != this)
                return;
            Gameplay.IsMultiplayer = true;
            base.OnEnable();
        }

        public override void OnDisable()
        {
            if (Instance != this)
                return;
            Gameplay.IsMultiplayer = false;
            base.OnDisable();
        }

        void OnSelectGameMode(string gameMode)
        {
            if (m_Debug)
                Debug.Log("OnSelectGameMode:" + gameMode);
            m_GameModeDisplayName = gameMode;
        }

        void OnSelectMap(string mapName, string sceneName)
        {
            m_MapDisplayName = mapName;
            m_SceneToLoadName = sceneName;
        }

        protected virtual void Start()
        {
            if (m_StartConnected)
                Connect();

            if (m_DontDestroyOnLoad)
                Object.DontDestroyOnLoad(transform.root.gameObject);
        }

        // private bool m_IsSimulatingLoss = false;//uncomment to test disconnect
        // private float m_DisconnectedTime = 0;//uncomment to test disconnect
        protected virtual void Update()
        {

            UpdateConnectionState();

            UpdatePing();

            // SNIPPET: uncomment to test disconnect
            //  if (Input.GetKeyUp(KeyCode.Alpha0))
            //  	Disconnect();

            // SNIPPET: uncomment to test connection loss
            //if (Input.GetKeyUp(KeyCode.K))
            //{
            //    m_IsSimulatingLoss = !m_IsSimulatingLoss;
            //    if (m_IsSimulatingLoss)
            //    {
            //        m_DisconnectedTime = 0;
            //        PhotonNetwork.NetworkingClient.SimulateConnectionLoss(true);
            //        Debug.Log("**************************** SIMULATE CONNECTION LOSS START ***********************************");
            //    }
            //    else
            //    {
            //        PhotonNetwork.NetworkingClient.SimulateConnectionLoss(false);
            //        Debug.Log("**************************** SIMULATE CONNECTION LOSS STOP ***********************************");
            //        m_DisconnectedTime = 0;

            //        if (!PhotonNetwork.InRoom)
            //            PhotonNetwork.ReconnectAndRejoin();
            //    }
            //}
            //if (m_IsSimulatingLoss)
            //{
            //    m_DisconnectedTime += Time.deltaTime;
            //    Debug.Log("DISCONNECTED FOR : " + m_DisconnectedTime);
            //}


            // SNIPPET: uncomment to test master switch
            //if (Input.GetKeyUp(KeyCode.M))
            //{
            //    if (Gameplay.IsMaster)
            //    {
            //        MPRemotePlayer r = FindObjectOfType<MPRemotePlayer>();
            //        if (r)
            //            PhotonNetwork.SetMasterClient(r.photonView.Owner);
            //    }
            //    else
            //    {
            //        PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
            //    }
            //    Debug.Log("**************************** FORCE SWITCH MASTER ***********************************");
            //}

        }


        /// <summary>
        ///	detects cases where the connection process has stalled,
        ///	disconnects and tries to connect again
        /// </summary>
        protected virtual void UpdateConnectionState()
        {
            m_InternetReachability = Application.internetReachability;

            if (m_InternetReachability == NetworkReachability.NotReachable)
                return;

            if (!m_StayConnected)
                return;

            if (PhotonNetwork.NetworkClientState == ClientState.Disconnected)
                Connect();

            if (PhotonNetwork.NetworkClientState != m_LastClientState)
            {
                string s = PhotonNetwork.NetworkClientState.ToString();
                s = ((PhotonNetwork.NetworkClientState == ClientState.Joined) ? "--- " + s + " ---" : s);
                if (s == "PeerCreated")
                    s = "Connecting to the best region's cloud ...";

                if (m_DebugToGameChat)
                    MPDebug.Log(s);
                if (m_Debug)
                    Debug.Log(s);
            }
            Connected = PhotonNetwork.IsConnected;
            if (Connected)
            {
                if (m_ConnectionTimer != null)
                {
                    if (m_DebugToGameChat)
                        MPDebug.Log("MPConnection -Reset Connection Timer");
                    if (m_Debug)
                        Debug.Log("MPConnection -Reset Connection Timer");
                    Scheduler.Cancel(m_ConnectionTimer);
                    m_ConnectionTimer = null;
                    m_ConnectionAttempts = 0;
                }
            }
            else if ((PhotonNetwork.NetworkClientState != m_LastClientState) && m_ConnectionTimer == null)
            {
                Reconnect();
            }

            m_LastClientState = PhotonNetwork.NetworkClientState;
        }

        /// <summary>
        /// reports ping every 10 (default) seconds by storing it as a custom player
        /// prefs value in the Photon Cloud. 'Ping' is defined as the roundtrip time to
        /// the Photon server and it is only reported if it has changed
        /// </summary>
        public virtual void UpdatePing()
        {
            if (!PhotonNetwork.InRoom)
                return;
            // only report ping every 10 (default) seconds
            if (Time.time < m_NextAllowedPingTime)
                return;
            m_NextAllowedPingTime = Time.time + m_PingReportInterval;

            // get the roundtrip time to the photon server
            int ping = PhotonNetwork.GetPing();

            // only report ping if it changed since last time
            if (ping == m_LastPing)
                return;
            m_LastPing = ping;

            // send the ping as a custom player property (the first time it will be
            // created, from then on it will be updated)
            Hashtable playerCustomProps = new Hashtable();
            playerCustomProps["Ping"] = ping;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerCustomProps);

        }


        /// <summary>
        /// this method smooths over a harmless error case where 'TryCreateRoom' fails
        /// because someone was creating the same room name at the exact same time as us,
        /// and 'TryCreateRoom' failed to sort it out. instead of pausing the editor and
        /// showing a scary crash dialog, we should keep calm, carry on and reconnect
        /// </summary>
        public override void OnCreateRoomFailed(short sh, string s)
        {
            base.OnCreateRoomFailed(sh, s);

            // unpause editor (if paused)
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPaused)
                UnityEditor.EditorApplication.isPaused = false;
#endif

        }

        public void TryPlayWithFriends(string[] expectedPlayerIDs)
        {
            if (!Gameplay.IsMultiplayer)
            {
                Gameplay.IsMultiplayer = true;
            }

            if (expectedPlayerIDs[0] == PhotonNetwork.AuthValues.UserId)
            {
                // Debug.Log("I SENT THE NOTIFICATION");
                TryJoinRoom(expectedPlayerIDs);
            }
            else
            {
                StartCoroutine(WaitForRoomCreation(expectedPlayerIDs));
            }
        }

        /// <summary>
        /// For PWF when the client accepts we need to wait a period of time before joining that room, this ensures player will in fact have a room to join
        /// </summary>
        /// <param name="expectedPlayerIDs"></param>
        /// <returns></returns>
        private IEnumerator WaitForRoomCreation(string[] expectedPlayerIDs)
        {
            float maxTime = Time.time + 10f;
 
            while (Time.time < maxTime && m_RoomInfos == null)
            {
                yield return 0;
            }

            TryJoinRoom(expectedPlayerIDs);
        }

        /// <summary>
        /// creates a new room named and numbered 'MapToLoad + current room count + 1', or joins
        /// that room if someone else has just created it
        /// 
        ///  MapToLoad is the "Room" replacement and namer
        /// </summary>
        public virtual void TryJoinRoom(string[] expectedPlayerIDs = null)
        {
            if (m_LastClientState == ClientState.Joining)
                return;

            if (!m_StayConnected || !m_CanJoinRooms)
            {
                m_EarlyJoin = true;
                m_StayConnected = true;
                return;
            }
            
            if (m_Debug)
                Debug.LogFormat("TryJoinRoom() -> Total players = {0} , Room count = {1} , Room Infos Count = {2} " , PhotonNetwork.CountOfPlayers, PhotonNetwork.CountOfRooms , (m_RoomInfos != null ? m_RoomInfos.Count.ToString() : "0"));
            
            if (m_DebugToGameChat)
                MPDebug.Log("Total players using app: " + PhotonNetwork.CountOfPlayers);


            foreach (var item in m_RoomInfos)
            {
                RoomInfo info = item.Value;
                if (info.PlayerCount >= MPMaster.Instance.MaxPlayers)
                    continue;

                if (!info.IsVisible)
                    continue;

                if (info.CustomProperties.TryGetValue("m", out object o) && (string)o == m_MapDisplayName)
                {
                    if (info.CustomProperties.TryGetValue("g", out o) && (string)o == m_GameModeDisplayName)
                    {
                        // we found a room we can join!
                        PhotonNetwork.JoinRoom(info.Name, expectedPlayerIDs);
                        return;
                    }
                }
            }

/*            if (m_RoomInfos != null && m_RoomInfos.Count > 0)
            {
                for (int i = 0; i < m_RoomInfos.Count; i++)
                {
                    if (m_RoomInfos[i].PlayerCount >= MPMaster.Instance.MaxPlayers)
                        continue;

                    if (!m_RoomInfos[i].IsVisible)
                        continue;

                    if (m_RoomInfos[i].CustomProperties.TryGetValue("m", out object o) && (string)o == m_MapDisplayName)
                    {
                        if (m_RoomInfos[i].CustomProperties.TryGetValue("g", out o) && (string)o == m_GameModeDisplayName)
                        {
                            // we found a room we can join!
                            PhotonNetwork.JoinRoom(m_RoomInfos[i].Name);
                            return;
                        }
                    }
                }
            }*/

            // noone else is creating the wanted room, so create it!
            RoomOptions roomOptions = new RoomOptions()
            {
                MaxPlayers = (byte)MPMaster.Instance.MaxPlayers,
                CleanupCacheOnLeave = true,
                CustomRoomPropertiesForLobby = new string[2] { "m", "g" },
                CustomRoomProperties = new Hashtable { ["m"] = m_MapDisplayName, ["g"] = m_GameModeDisplayName, ["s"] = m_SceneToLoadName }
            };

            string roomName = m_MapDisplayName + " " + m_GameModeDisplayName + " ("  +(PhotonNetwork.CountOfRooms + 1).ToString() + ")";

            if (m_Debug)
                Debug.Log("no matching room found so creating one ");

            if (PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, m_Lobby, expectedPlayerIDs))
            {
                if (m_Debug)
                    Debug.Log("create or join room success \n" + roomName + " " + roomOptions.CustomRoomProperties);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Connect()
        {
            if (Connected)
                return;
            if (m_ConnectionTimer != null)
                return;

            Gameplay.IsMultiplayer = true;
            //keep connection alive on dropouts
            m_StayConnected = true;

            if (m_InternetReachability == NetworkReachability.NotReachable)
            {
                //TODO: uncomment this later when connect is done with sign in....
                //   Debug.Log("No Internet, Abort Connect()");
                //  return;
            }

            //  PhotonNetwork.GameVersion = "1";
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.LocalPlayer.NickName = m_LocalPlayerName;


            m_ConnectionTimer = Scheduler.Schedule(m_LogOnTimeOut, delegate ()
            {
                m_ConnectionAttempts++;
                if (m_ConnectionAttempts < m_MaxConnectionAttempts)
                {
                    if (m_DebugToGameChat)
                        MPDebug.Log("Retrying (" + m_ConnectionAttempts + ") ...");

                    if (m_Debug)
                        Debug.Log("Retrying (" + m_ConnectionAttempts + ") ...");

                    Reconnect();
                }
                else
                {
                    if (m_DebugToGameChat)
                        MPDebug.Log("Failed to connect (tried " + m_ConnectionAttempts + " times).");

                    if (m_Debug)
                        Debug.Log("Failed to connect (tried " + m_ConnectionAttempts + " times).");

                    Disconnect();
                }
            });
        }


        /// <summary>
        /// used internally to disconnect and immediately reconnect
        /// </summary>
        protected virtual void Reconnect()
        {

            if (PhotonNetwork.NetworkClientState != ClientState.Disconnected
                && PhotonNetwork.NetworkClientState != ClientState.PeerCreated)
            {
                PhotonNetwork.Disconnect();
            }

            Connect();

            m_LastClientState = ClientState.Disconnected;

        }


        /// <summary>
        /// disconnects the player from an ongoing game, loads a blank level
        /// (if provided) and sends a globalevent informing external objects
        /// of the disconnect. TIP: call this method from anywhere using:
        /// MPConnection.Instance.Disconnect();
        /// </summary>
        public virtual void Disconnect()
        {
            Gameplay.IsMultiplayer = false;
            if (PhotonNetwork.NetworkClientState == ClientState.Disconnected)
                return;

            // explicitly destroy all player objects (these usually survive a level load)
            MPPlayer[] players = FindObjectsOfType<MPPlayer>();
            foreach (MPPlayer p in players)
            {
                if (ObjectPoolBase.InstantiatedWithPool(p.gameObject))
                {
                    // Debug.Log("Destroy POOLED player, ID is: " + p.ID);
                    ObjectPoolBase.Destroy(p.gameObject);
                }
                else
                {
                    // Debug.Log("Destroy player, ID is: " + p.ID);
                    Destroy(p.gameObject);
                }
            }

            if (MPScoreBoard.ShowScore)
                MPScoreBoard.ShowScore = false;

            m_ConnectionTimer.Active = false;

            // disable auto-reconnection and disconnect from Photon
            m_StayConnected = false;

            PhotonNetwork.Disconnect();
            m_ConnectionAttempts = 0;
            m_LastClientState = ClientState.Disconnected;

            // load a blank scene (if provided) to destroy the currently played level.
            // NOTE: by default some master gameplay objects (such
            // as this component) will survive
            if (!string.IsNullOrEmpty(m_SceneToLoadOnDisconnect))
                SceneManager.LoadScene(m_SceneToLoadOnDisconnect);

            // send a message to inform external objects that we have disconnected
            EventHandler.ExecuteEvent("Disconnected");
            if (m_Debug)
                Debug.Log("--- Disconnected ---");
            if (m_DebugToGameChat)
                MPDebug.Log("--- Disconnected ---");
        }

        public virtual void LeaveRoomAndGoToMenu(GameObject character)
        {
            if (PhotonNetwork.NetworkClientState == ClientState.Disconnected)
                return;

            // explicitly destroy all player objects (these usually survive a level load)
            MPPlayer[] players = FindObjectsOfType<MPPlayer>();
            foreach (MPPlayer p in players)
            {
                if (ObjectPoolBase.InstantiatedWithPool(p.gameObject))
                {
                   // Debug.Log("Destroy POOLED player, ID is: " + p.ID);
                    ObjectPoolBase.Destroy(p.gameObject);
                }
                else
                {
                   // Debug.Log("Destroy player, ID is: " + p.ID);
                    Destroy(p.gameObject);
                }
            }

            if (MPScoreBoard.ShowScore)
                MPScoreBoard.ShowScore = false;

            PhotonNetwork.LeaveRoom(false);

            m_ConnectionAttempts = 0;
            m_LastClientState = PhotonNetwork.NetworkClientState;

            // load a blank scene (if provided) to destroy the currently played level.
            // NOTE: by default some master gameplay objects (such
            // as this component) will survive
            if (!string.IsNullOrEmpty(m_SceneToLoadOnDisconnect))
                SceneManager.LoadScene(m_SceneToLoadOnDisconnect);

            if (m_DebugToGameChat)
                MPDebug.Log("--- Player Quit From Room ! ---");

            if (m_Debug)
                Debug.Log("--- Player Quit From Room ! ---");
        }

        public override void OnConnected()
        {
            base.OnConnected();
            PhotonNetwork.LocalPlayer.NickName = SettingsManager.PlayerName;
            EventHandler.ExecuteEvent("OnConnected");
        }

        public override void OnConnectedToMaster()//Server
        {
            base.OnConnectedToMaster();
            PhotonNetwork.LocalPlayer.NickName = SettingsManager.PlayerName;
            PhotonNetwork.JoinLobby(m_Lobby);
            EventHandler.ExecuteEvent("OnConnectedToMaster");
        }

        /// <summary>
        /// 
        /// </summary>
        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();

            // update name of this player in the cloud
            PhotonNetwork.LocalPlayer.NickName = SettingsManager.PlayerName;

            EventHandler.ExecuteEvent("OnJoinedLobby");
        }

        private void UpdateCachedRoomList(List<RoomInfo> roomList)
        {
            for (int i = 0; i < roomList.Count; i++)
            {
                GameObject g;
                RoomInfo info = roomList[i];
                if (info.RemovedFromList)
                {
                    m_RoomInfos.Remove(info.Name);
                    if (m_InstantiatedRoomInfos.TryGetValue(info.Name, out g))
                    {
                        Destroy(g);
                        m_InstantiatedRoomInfos.Remove(info.Name);
                    }
                }
                else
                {
                    m_RoomInfos[info.Name] = info;

                    if (m_RoomInfoParent != null && m_RoomInfoPrefab != null)
                    {
                        if (!m_InstantiatedRoomInfos.TryGetValue(info.Name, out g))
                        {
                            g = Instantiate(m_RoomInfoPrefab, m_RoomInfoParent.transform);
                            m_InstantiatedRoomInfos.Add(info.Name, g);
                        }
                    }
                }
            }

            //TODO: make into own component for modularity.
            if (m_RoomInfoParent != null && m_RoomInfoPrefab != null)
            {
                foreach (var item in m_InstantiatedRoomInfos)
                {
                    MPRoomInfoDataPun data = item.Value.GetCachedComponent<MPRoomInfoDataPun>();
                    RoomInfo info = m_RoomInfos[item.Key];
                    string mapName = (string)info.CustomProperties["m"];
                    string modeName = (string)info.CustomProperties["g"];
                    data.Initialize(delegate
                    {
                        m_MapDisplayName = mapName;
                        m_GameModeDisplayName = modeName;
                        Transform parent = MPMaster.Instance.transform.parent;
                        MPMaster[] masters = parent.GetComponentsInChildren<MPMaster>(true);
                        for (int i = 0; i < masters.Length; i++)
                            masters[i].gameObject.SetActive(m_GameModeDisplayName == masters[i].gameObject.name);
                        PhotonNetwork.JoinRoom(info.Name);
                        foreach (var item in m_InstantiatedRoomInfos)
                            Destroy(item.Value);
                        m_InstantiatedRoomInfos.Clear();
                    }
                    , mapName, modeName, info);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void OnRoomListUpdate(List<RoomInfo> roomInfos)
        {
            UpdateCachedRoomList(roomInfos);

            m_CanJoinRooms = true;

            if (m_ExecuteRoomListUpdateEvent == true)
                EventHandler.ExecuteEvent<Dictionary<string, RoomInfo>>("OnRoomListUpdate", m_RoomInfos);

            if (m_StayConnected && m_EarlyJoin)
                TryJoinRoom();

            //send to chat. 
            //TODO: use chat instead of debug send
            if (m_DebugToGameChat)
                MPDebug.Log("Rooms Updated => Total players using app: " + PhotonNetwork.CountOfPlayers);

            if (m_Debug)
                Debug.Log("Rooms Updated => Total players using app: " + PhotonNetwork.CountOfPlayers);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void OnJoinedRoom()
        {
            m_CanJoinRooms = false;
            m_EarlyJoin = false;

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.MaxPlayers = (byte)MPMaster.Instance.MaxPlayers;
             //   PhotonNetwork.CurrentRoom.PlayerTtl = 0;
            }

            MPMaster master = FindObjectOfType<MPMaster>();
            // send spawn request to master client
            // sent as RPC instead of in 'OnPhotonPlayerConnected' because the
            // MasterClient does not run the latter for itself + we don't want
            // to do the request on all clients
            if (master != null)    // in rare cases there might not be a MPMaster, for example: a chat lobby
            {
                if (MPPlayerSpawner.Instance.SpawnOnJoin)
                    master.photonView.RPC("RequestInitialSpawnInfo", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, m_SelectedModelIndex, -1);//to avoid using pun props (hackable), we send the selected model index 

                if (m_LoadSceneOnJoinRoom == true)
                {
                    if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("s", out object o))
                    {
                        m_SceneToLoadName = (string)o;
                        PhotonNetwork.LoadLevel(m_SceneToLoadName);
                    }
                    else Debug.LogError("No scene found in room custom properties. Ensure the 'Scene' property is defined when creating the room");
                  //  PhotonNetwork.LoadLevel((string)PhotonNetwork.CurrentRoom.CustomProperties["Scene"]);
                   
                }
            }

            EventHandler.ExecuteEvent("OnJoinedRoom");

            Gameplay.IsMaster = PhotonNetwork.IsMasterClient;

            // ensure that player names are unique in the room
            int count = 0;
            string n = PhotonNetwork.LocalPlayer.NickName;
            string tempName = n;
            List<string> usedNames = PhotonNetwork.PlayerListOthers.Select(p => p.NickName).ToList();
            while (usedNames.Contains(tempName))
            {
                count++;
                if (count == 1)
                    tempName = n + " (1)";
                else tempName = $"{n} ({count})";
            }
            PhotonNetwork.LocalPlayer.NickName = tempName;

            //NOTE: Not required just yet, but likely will be later!
            /*      Hashtable props = new Hashtable
                      {
                          {"PLAYER_LOADED_LEVEL", false}
                      };
                  PhotonNetwork.LocalPlayer.SetCustomProperties(props);*/
        }


        /// <summary>
        /// updates the 'IsMaster' flag which gets read by Base UCC classes.
        /// also, announces players leaving in the chat (if any)
        /// </summary>
        public override void OnPlayerLeftRoom(Player player)
        {
            Gameplay.IsMaster = PhotonNetwork.IsMasterClient;

            if (m_DebugToGameChat)
                MPDebug.Log(player.NickName + " left the game"); // NOTE: the 'joined' message is posted by MPPlayerSpawner which has extended team info

            if (m_Debug)
                Debug.Log(player.NickName + " left the game");
        }

        public override void OnDisconnected(DisconnectCause disconnectCause)
        {

            if (m_DebugToGameChat)
                MPDebug.Log("OnDisconnected >" + disconnectCause);
            if (m_Debug)
                Debug.Log("OnDisconnected >" + disconnectCause);

             m_CanJoinRooms = false;

            if (disconnectCause != DisconnectCause.DisconnectByClientLogic)
            {
                //   MPDebug.Log("D/C OPEN RECONNECT!!");
                //   Debug.Log("D/C OPEN RECONNECT!!");
                //   TODO: OpenReconnectingPanel();
            }

            switch (disconnectCause)
            {
                case DisconnectCause.None:
                    break;
                case DisconnectCause.ExceptionOnConnect:
                    break;
                case DisconnectCause.Exception:
                    break;
                case DisconnectCause.ServerTimeout:
                    break;
                case DisconnectCause.ClientTimeout:
                    break;
                case DisconnectCause.DisconnectByServerLogic:
                    break;
                case DisconnectCause.DisconnectByServerReasonUnknown:
                    break;
                case DisconnectCause.InvalidAuthentication:
                    break;
                case DisconnectCause.CustomAuthenticationFailed:
                    break;
                case DisconnectCause.AuthenticationTicketExpired:
                    break;
                case DisconnectCause.MaxCcuReached:
                    break;
                case DisconnectCause.InvalidRegion:
                    break;
                case DisconnectCause.OperationNotAllowedInCurrentState:
                    break;
                case DisconnectCause.DisconnectByClientLogic:
                    break;
                default:
                    break;
            }

        }

        public override void OnJoinRandomFailed(short sh, string s)
        {
            base.OnJoinRandomFailed(sh, s);

            if (m_DebugToGameChat)
                MPDebug.Log("join random failed");

            if (m_Debug)
                Debug.Log("join random failed");
        }

        public override void OnLeftLobby()
        {
            base.OnLeftLobby();

            if (m_DebugToGameChat)
                MPDebug.Log("left lobby");

            if (m_Debug)
                Debug.Log("left lobby");
        }
    }
}
