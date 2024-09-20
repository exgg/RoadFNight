/////////////////////////////////////////////////////////////////////////////////
//
//  MPMaster.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	This component is king of the multiplayer game!
//					it manages overall game logic of the master client. implements
//					multiplayer game time cycles, assembles and broadcasts full
//					or partial game states with game phase, clock and player stats.
//					allocates team and initial spawnpoint to joining players, plus
//					broadcasts our own version of the simulation in case of a local
//					master client handover
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using UnityEngine;
    using System.Collections.Generic;
    using Hashtable = ExitGames.Client.Photon.Hashtable;
    using UnityEngine.SceneManagement;
    using Photon.Pun;
    using Photon.Realtime;
    using Opsive.Shared.Game;
    using Opsive.Shared.Events;
    using Opsive.UltimateCharacterController.Game;
    using FastSkillTeam.UltimateMultiplayer.Shared;
    using FastSkillTeam.UltimateMultiplayer.Shared.Game;
#if ULTIMATE_SEATING_CONTROLLER
    using FastSkillTeam.UltimateSeatingController;
    using Opsive.UltimateCharacterController.Traits;
#endif

    public class MPMaster : MonoBehaviourPunCallbacks
	{
		[Tooltip("Initial delay for starting the match (-1 for no delay). Can be used in combination with Min Start Players.")]
		[SerializeField] protected float m_GameStartDelay = -1f;
		[Tooltip("Minimum amount of players required to start the match. Can be used in combination with Game Start Delay.")]
		[SerializeField] protected int m_MinStartPlayers = 1;
        [Tooltip("Specifies if players can free roam while waiting for a match to begin, players will be frozen by default (false).")]
        [SerializeField] protected bool m_FreeRoamWhileWaiting = false;
        [Tooltip("Specifies if the room will be closed upon match start.")]
        [SerializeField] protected bool m_CloseRoomWhenMatchStarts = false;
        [Tooltip("Maximum amount of players allowed in the match.")]
		[SerializeField] protected int m_MaxPlayers = 8;
		[Tooltip("How many rounds should be played in one game session? The game will be over after this many rounds are completed (-1 for endless rounds).")]
		[SerializeField] protected int m_RoundCount = 1;
		[Tooltip("How long each round plays out as a maximum (in seconds). \nNOTE: Objective completion can end a round before this time is up.")]
		[SerializeField] protected float m_RoundDuration = (5 * 60);     // default: 5 minutes
		[Tooltip("How long between each round (in seconds) an interval will be. The game is paused for this duration, and the score can be displayed.")]
		[SerializeField] protected float m_IntervalDuration = 20f;          // default: 20 seconds
		[Tooltip("For defining objective type game modes. Standard is for game modes with no objectives.")]
		[SerializeField] protected GameType m_GameType = GameType.Standard;
		//public bool autoScenes = true;//TODO finish me last to ensure all is accounted for in scene change
		public GameType CurrentGameType => m_GameType;
		private int m_StartPlayerCount = -1;
        private bool m_DidWait = false;
        private bool m_IsStopping = false;
		private Scene m_StartScene;
		private string m_CurrentLevel = "";
		public int MaxPlayers => m_MaxPlayers;
		public float RoundDuration { get => m_RoundDuration; set => m_RoundDuration = value; }
		public string CurrentLevel { get => m_CurrentLevel; set => m_CurrentLevel = value; } // current level loaded on the master and enforced on all clients. master will load this on login

		protected bool m_TookOverGame = false;  // will always be false as long as we're a regular client. will be true if we
												// join as master, or if we become master (as soon as our first full game
												// state has been broadcast) and will never go false again in the same game
		private static int m_CurrentRound = 0;
		private static MPMaster m_Instance;

		public static MPMaster Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = Component.FindObjectOfType<MPMaster>();
				return m_Instance;
			}
		}

		public enum GamePhase
		{
			NotStarted,
			WaitingForPlayers,
			RoundStarting,
			Playing,
			BetweenGames
        }

        //never set directly, use the Phase property instead.
        private static GamePhase m_Phase = GamePhase.NotStarted;
        public static GamePhase Phase
        {
            get => m_Phase;
            private set
            {
                if (m_Phase != value)
                {
                    m_Phase = value;
                    Instance.GamePhaseChanged(m_Phase);
                }
            }
        }

        protected virtual void GamePhaseChanged(GamePhase newPhase)
        {
            // Notify listeners
            EventHandler.ExecuteEvent<GamePhase>("OnGamePhaseChanged", newPhase);
        }

        protected static Dictionary<Transform, int> m_ViewIDsByTransform = new Dictionary<Transform, int>();
		protected static Dictionary<int, Transform> m_TransformsByViewID = new Dictionary<int, Transform>();
        private List<Player> m_InstantiatedPlayers = new List<Player>();
		public List<Player> InstantiatedPlayers => m_InstantiatedPlayers;

        protected virtual void Awake()
		{
			m_StartScene = SceneManager.GetActiveScene();
			DeactivateOtherMasters();
		}

		public override void OnEnable()
		{
			base.OnEnable();

			m_Instance = this;

			SceneManager.sceneLoaded += OnLevelLoad;

			// register empty damage and kill delegates to prevent any issues
			// in case the scene has no 'MPDamageCallbacks' component
			EventHandler.RegisterEvent("TransmitDamage", delegate (Transform targetTransform, Opsive.UltimateCharacterController.Traits.Damage.DamageData data) { });
			EventHandler.RegisterEvent("TransmitKill", delegate (Transform targetTransform, Vector3 position, Vector3 force, GameObject attacker) { });
            EventHandler.RegisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
            EventHandler.RegisterEvent<Player, GameObject>("OnPlayerLeftRoom", OnPlayerLeftRoom);
            //+ NetworkRespawn?
        }

        private void OnPlayerEnteredRoom(Player player, GameObject character)
        {
            if (!m_InstantiatedPlayers.Contains(player))
                m_InstantiatedPlayers.Add(player);
        }

        private void OnPlayerLeftRoom(Player player, GameObject character)
        {
            if (m_InstantiatedPlayers.Contains(player))
                m_InstantiatedPlayers.Remove(player);
        }

        public override void OnDisable()
		{
			base.OnDisable();

			m_Instance = null;

			SceneManager.sceneLoaded -= OnLevelLoad;

			// unregister empty damage and kill delegates to prevent any issues
			// in case the scene has no 'MPDamageCallbacks' component
			EventHandler.UnregisterEvent("TransmitDamage", delegate (Transform targetTransform, Opsive.UltimateCharacterController.Traits.Damage.DamageData data) { });
			EventHandler.UnregisterEvent("TransmitKill", delegate (Transform targetTransform, Vector3 position, Vector3 force, GameObject attacker) { });
            EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
            EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerLeftRoom", OnPlayerLeftRoom);

            m_InstantiatedPlayers.Clear();
            //+ NetworkRespawn?
        }
/*
        // SNIPPET: here is an example of loading a level on the master. all clients  
        // will automatically load the new level and the game mode will reset.
        void FreshScene(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= FreshScene;
            //MPPlayerSpawner.Instance.ResetPlayers();
            MPPlayer.Players.Clear();
            MPPlayer.PlayersByID.Clear();
            if (PhotonNetwork.IsMasterClient)
                MPMaster.Instance.TransmitInitialSpawnInfo(PhotonNetwork.LocalPlayer, MPConnection.Instance.SelectedModelIndex);
            else
                photonView.RPC("RequestInitialSpawnInfo", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, MPConnection.Instance.SelectedModelIndex, -1);//to avoid using pun props (hackable), we send the selected model index

            //ResetGame();
            StartGame();
        }
        void LoadNextScene()
        {
            // explicitly destroy all player objects (these usually survive a level load)
            MPPlayer[] players = FindObjectsOfType<MPPlayer>();
            foreach (MPPlayer p in players)
            {
                if (p.DontDestroyOnLoad == true)
                    p.Transform.SetParent(new GameObject().transform);
            }

            SceneManager.sceneLoaded += FreshScene;
            Phase = GamePhase.NotStarted;
            m_CurrentRound = 0;
            Debug.Log("loading next MP scene " + FST_Utils.GetNextSceneName(1, 2));
            PhotonNetwork.LoadLevel(FST_Utils.GetNextSceneName(1, 2));
            //TransmitLoadLevel(FST_Utils.GetNextSceneName(1, 2));
        }
*/
        protected virtual void Update()
		{
			// set round over
			if ((Phase == GamePhase.Playing) && (MPClock.Running == false))
				StopGame();
/*
            // SNIPPET: here is an example of loading a level on the master. all clients  
            // will automatically load the new level and the game mode will reset.
            if (Input.GetKeyUp(KeyCode.L))
            {
                LoadNextScene();
                return;
            }
*/
            if (Phase == GamePhase.RoundStarting && MPClock.Running == false)
				StartGame();

			if ((Phase == GamePhase.BetweenGames) && (MPClock.Running == false))
			{
				if (m_RoundCount != -1 && m_CurrentRound >= m_RoundCount)
				{
					m_CurrentRound = 0;
					Phase = GamePhase.NotStarted;
					EventHandler.ExecuteEvent("OnMatchEnd");
					return;
				}
				ResetGame();
				StartGame();
			}

			if (m_TookOverGame && (PhotonNetwork.NetworkClientState != ClientState.Joined))
				m_TookOverGame = false;
		}

		/// <summary>
		/// respawns all players, unfreezes the local player, restarts
		/// the game clock and broadcasts the game state
		/// </summary>
		public void StartGame()
		{
			m_IsStopping = false;
			if (!Gameplay.IsMaster)
				return;

			if (m_CloseRoomWhenMatchStarts == true && PhotonNetwork.CurrentRoom.IsOpen)
			{
				PhotonNetwork.CurrentRoom.IsOpen = false; 
				PhotonNetwork.CurrentRoom.IsVisible = false;
			}

			m_CurrentRound++;

			MPPlayer.TransmitUnFreezeAll();

			Phase = GamePhase.Playing;
			MPClock.Set(m_RoundDuration);

			TransmitGameState();

			MPDebug.Log("Its On! Go gettem!");

			Debug.Log("StartGame @ " + MPClock.LocalTime + " with end time: " + (MPClock.LocalTime + MPClock.Duration) + ". time left is: " + MPClock.TimeLeft);
		}

		/// <summary>
		/// freezes all players that enter the game, Starts the check for enough players to begin the round
		/// </summary>
		public void PrepareGame()
		{
			if (!Gameplay.IsMaster)
				return;

            Debug.Log("Prepare Game @ " + MPClock.LocalTime + " with end time: " + (MPClock.LocalTime + MPClock.Duration) + ". time left is: " + MPClock.TimeLeft);

			if (IsEnoughPlayersToStart())
			{
				if (m_GameStartDelay == -1)
					StartGame();
				else
					StartTimer();
			}
            else
            {
                if (m_FreeRoamWhileWaiting == false)
                    MPPlayer.TransmitFreezeAll();

				MPMaster.Phase = MPMaster.GamePhase.WaitingForPlayers;

				TransmitGameState();

				InvokeRepeating("WaitForMorePlayers", 0, 1);
			}

		}

        /// <summary>
        /// Invoked by PrepareGame, will start the round start timer if enough players to start or not using min start players
        /// </summary>
        public void WaitForMorePlayers()
		{
			Debug.Log("Wait for players");

            m_DidWait = true;

            if (IsEnoughPlayersToStart())
			{
				if (m_GameStartDelay == -1)
					StartGame();
				else
					StartTimer();
				CancelInvoke("WaitForMorePlayers");
			}
		}

		public bool IsEnoughPlayersToStart()
		{
			if (m_MinStartPlayers <= 1)
				return true;

            if (PhotonNetwork.PlayerList == null || m_InstantiatedPlayers == null)
                return false;

            int count = m_InstantiatedPlayers.Count;
			if (count < m_MinStartPlayers)
			{
				if (m_StartPlayerCount != count)
				{
					//  Debug.Log("Players needed to start round: " + (m_MinStartPlayers - count));
					MPDebug.Log("Players needed to start round: " + (m_MinStartPlayers - count));
					m_StartPlayerCount = count;
				}
				return false;
			}
			m_StartPlayerCount = -1;
			return true;
		}

		public void StartTimer()
		{
			if (!PhotonNetwork.IsMasterClient)
				return;
            if (m_DidWait && m_FreeRoamWhileWaiting == true)
                ResetGame();
            MPPlayer.TransmitFreezeAll();
            m_DidWait = false;
            Phase = GamePhase.RoundStarting;
			MPClock.Set(m_GameStartDelay);
			TransmitGameState();

		//	MPDebug.Log("Starting Game Start Timer");
		//	MPDebug.Log("Game starts in " + m_GameStartDelay + " seconds");
		}

		/// <summary>
		/// freezes the local player, pauses game time and broadcasts
		/// the game state
		/// </summary>
		public void StopGame()
		{
			if (m_IsStopping == true)
				return;
			m_IsStopping = true;
			Debug.Log("StopGame()");
			if (MPTeamManager.Exists)
				MPTeamManager.Instance.RefreshTeams();
			EventHandler.ExecuteEvent("OnStopGame");
			if (!Gameplay.IsMaster)
				return;
			photonView.RPC("ReceiveFreeze", RpcTarget.All);
			Phase = GamePhase.BetweenGames;
			MPClock.Set(m_IntervalDuration);

			TransmitGameState();
		}

		/// <summary>
		/// Should be called on ALL machines. Restores health, shots, inventory and death on all players, and
		/// cleans up the scene for the next round, along with sending "OnResetGame" for any object listening.
		/// </summary>
		public void ResetGame()
		{
			//as this is called on every machine we need to flag the game as not started, Start Game will send the updated gamestate next.
			Phase = GamePhase.NotStarted;

			MPPlayerStats.FullResetAll();

			photonView.RPC("ReceiveSceneCleanup", RpcTarget.All);

#if ULTIMATE_SEATING_CONTROLLER
			BoardSource[] allBoardSources = Component.FindObjectsOfType<BoardSource>();
			for (int i = 0; i < allBoardSources.Length; i++)
			{
				Respawner r = allBoardSources[i].GameObject.GetCachedComponent<Respawner>();
				if (r)
					r.Respawn();
			}
#endif

			//Notify those interested. (MPPlayers will respawn)
			EventHandler.ExecuteEvent("OnResetGame");

		}

		/// <summary>
		/// caches and returns the photonview id of the given transform.
		/// ids are stored in a dictionary that resets on level load
		/// </summary>
		public static int GetViewIDOfTransform(Transform t)
		{

			if (t == null)
				return 0;

			if (!m_ViewIDsByTransform.TryGetValue(t, out int id))
			{
				PhotonView p = t.GetComponent<PhotonView>();
				if (p != null)
					id = p.ViewID;
				m_ViewIDsByTransform.Add(t, id);    // add (even if '0' to prevent searching again)
			}

			return id;

		}

		/// <summary>
		/// caches and returns the transform of the given photonview id.
		/// transforms are stored in a dictionary that resets on level load
		/// </summary>
		public static Transform GetTransformOfViewID(int id)
		{
			if (!m_TransformsByViewID.TryGetValue(id, out Transform t))
			{
				foreach (PhotonView p in FindObjectsOfType<PhotonView>())
				{
					if (p.ViewID == id)
					{
						t = p.transform;
						m_TransformsByViewID.Add(id, p.transform);
						return p.transform;
					}
				}
				m_TransformsByViewID.Add(id, t);    // add (even if not found, to avoid searching again)
			}

			return t;
		}

#if ULTIMATE_SEATING_CONTROLLER
		static Dictionary<BoardSource, int> m_ViewIDsByVehicle = new Dictionary<BoardSource, int>();
		static Dictionary<int, BoardSource> m_VehiclesByViewId = new Dictionary<int, BoardSource>();
		public static int GetViewIDOfVehicle(BoardSource vehicle)
		{

			if (vehicle == null)
				return 0;

			if (!m_ViewIDsByVehicle.TryGetValue(vehicle, out int id))
			{
				PhotonView p = vehicle.GetComponent<PhotonView>();
				if (p != null)
					id = p.ViewID;
				m_ViewIDsByVehicle.Add(vehicle, id);    // add (even if '0' to prevent searching again)
			}

			return id;

		}

		public static BoardSource GetVehicleOfViewID(int id)
		{
			if (!m_VehiclesByViewId.TryGetValue(id, out BoardSource t))
			{
				foreach (PhotonView p in FindObjectsOfType<PhotonView>())
				{
					if (p.ViewID == id)
					{
						t = p.GetComponent<BoardSource>();
						m_VehiclesByViewId.Add(id, p.GetComponent<BoardSource>());
						return p.GetComponent<BoardSource>();
					}
				}
				m_VehiclesByViewId.Add(id, t);    // add (even if not found, to avoid searching again)
			}

			return t;
		}
#endif

		/// <summary>
		/// pushes the master client's version of the game state and all
		/// player stats onto another client. if 'player' is null, the game
		/// state will be pushed onto _all_ other clients. 'gameState' can
		/// optionally be provided for cases where only a partial game state
		/// (a few stats on a few players) needs to be sent. by default the
		/// method will assemble and broadcast all stats of all players.
		/// </summary>
		public void TransmitGameState(Player targetPlayer, Hashtable gameState = null)
		{
			if (!PhotonNetwork.IsMasterClient)
				return;

			// if no (partial) gamestate has been provided, assemble and
			// broadcast the entire gamestate
			if (gameState == null)
				gameState = AssembleGameState();

			//DumpGameState(gameState);

			if (targetPlayer == null)
			{
				//	Debug.Log("sending gamestate to all" + Time.time);
				photonView.RPC("ReceiveGameState", RpcTarget.Others, (Hashtable)gameState);
			}
			else
			{
				//	Debug.Log("sending gamestate to " + targetPlayer + "" + Time.time);
				photonView.RPC("ReceiveGameState", targetPlayer, (Hashtable)gameState);
			}

			if (MPTeamManager.Exists)
				MPTeamManager.Instance.RefreshTeams();
		}


		/// <summary>
		/// pushes the master client's version of the game state and all
		/// player stats onto all other clients. 'gameState' can optionally
		/// be provided for cases where only a partial game state (a few
		/// stats on a few players) needs to be sent. by default the method
		/// will assemble and broadcast all stats of all players.
		/// </summary>
		public void TransmitGameState(Hashtable gameState = null)
		{
			TransmitGameState(null, gameState);
		}


		/// <summary>
		/// broadcasts a game state consisting of a certain array of
		/// stats extracted from the specified players. parameters
		/// 2 and up should be strings identifying the included stats.
		/// the returned gamestate will report the same list of stat
		/// names for all players (NOTE: playerIDs are PhotonNetwork.player.IDs not viewIDs)
		/// </summary>
		public void TransmitPlayerState(int[] playerIDs, params string[] stats)
		{
			if (!PhotonNetwork.IsMasterClient)
				return;
			Debug.Log("TransmitPlayerState");
			Hashtable playerState = AssembleGameStatePartial(playerIDs, stats);
			if (playerState == null)
			{
				Debug.LogError("Error: (" + this + ") Failed to assemble partial gamestate.");
				return;
			}

			photonView.RPC("ReceivePlayerState", RpcTarget.Others, (Hashtable)playerState);

		}


		/// <summary>
		/// broadcasts a game state consisting of an individual array
		/// of stats extracted from each specified player. parameters
		/// 2 and up should be arrays of strings identifying the stats
		/// included for each respective player. the returned gamestate
		/// may include unique stat names for each player
		/// </summary>
		public void TransmitPlayerState(int[] playerIDs, params string[][] stats)
		{
			if (!PhotonNetwork.IsMasterClient)
				return;
			Debug.Log("TransmitPlayerState");
			Hashtable state = AssembleGameStatePartial(playerIDs, stats);
			if (state == null)
			{
				Debug.LogError("Error: (" + this + ") Failed to assemble partial gamestate.");
				return;
			}

			photonView.RPC("ReceivePlayerState", RpcTarget.Others, (Hashtable)state);

		}


		/// <summary>
		/// the game state can only be pushed out by the master client.
		/// however it is stored and kept in sync across clients in the
		/// form of the properties on the actual network players.
		/// in case a client becomes master it pushes out a new game
		/// state based on the network players in its own scene
		/// </summary>
		protected static Hashtable AssembleGameState()
		{

			// NOTE: don't add custom integer keys, since ints are used
			// for player identification. for example, adding a key '5'
			// might result in a crash when player 5 tries to join.
			// adding string (or other type) keys should be fine

			if (!PhotonNetwork.IsMasterClient)
				return null;

			MPPlayerStats.EraseStats();  // NOTE: sending an RPC with a re-used gamestate will crash! we must create new gamestates every time

			MPPlayer.RefreshPlayers();

			Hashtable state = new Hashtable();

			// -------- add game phase, game time and duration --------

			state.Add("Round", m_CurrentRound);
			state.Add("Phase", Phase);
			state.Add("TimeLeft", MPClock.TimeLeft);
			state.Add("Duration", MPClock.Duration);

			// -------- add the stats of all players (includes health) --------

			foreach (MPPlayer player in MPPlayer.Players.Values)
			{
				if (player == null)
					continue;
				// add a player stats hashtable with the key 'player.ID'
				Hashtable stats = player.Stats.All;
				if (stats != null
					//  && !state.ContainsKey(player.ID)// NOTE: extra check added here! 
					)
					state.Add(player.ID, stats);
			}


            // -------- add the health of all non-player damagehandlers --------

            foreach (MPHealth d in MPHealth.Instances.Values)
            {
                if (d is MPCharacterHealth)
                    continue;
                if (d == null)
                    continue;
                PhotonView p = d.GetComponent<PhotonView>();
                if (p == null)
                    continue;

                // add the view id for a damagehandler photon view, along with its health.
                // NOTE: we send and unpack the view id negative since some will potentially
                // be the same as existing player id:s in the hashtable (starting at 1)
                if (d.HealthAttribute != null)
                    state.Add(-p.ViewID, (float)d.HealthAttribute.Value);   // NOTE: cast to float required for Anti-Cheat Toolkit support
                if (d.ShieldAttribute != null)
                    state.Add(-p.ViewID * 2, (float)d.ShieldAttribute.Value);
            }

            if (state.Count == 0)
				Debug.LogError("Failed to get gamestate.");

			return state;

		}


		/// <summary>
		/// assembles a game state consisting of a certain array of
		/// stats extracted from the specified players. parameters
		/// 2 and up should be strings identifying the included stats.
		/// the returned gamestate will report the same array of stat
		/// names for all players
		/// </summary>
		protected virtual Hashtable AssembleGameStatePartial(int[] playerIDs, params string[] stats)
		{

			Hashtable state = new Hashtable();

			for (int v = 0; v < playerIDs.Length; v++)
			{
				if (state.ContainsKey(playerIDs[v]))    // safety measure in case int array has duplicate id:s
				{
					Debug.LogWarning("Warning (" + this + ") Trying to add same player twice to a partial game state (not good). Duplicates will be ignored.");
					continue;
				}
				state.Add(playerIDs[v], ExtractPlayerStats(MPPlayer.Get(playerIDs[v]), stats));
			}

			return state;

		}


		/// <summary>
		/// assembles a game state consisting of an individual array
		/// of stats extracted from each specified player. parameters
		/// 2 and up should be arrays of strings identifying the stats
		/// included for each respective player. the returned gamestate
		/// may include unique stat names for every player
		/// </summary>
		protected virtual Hashtable AssembleGameStatePartial(int[] playerIDs, params string[][] stats)
		{

			Hashtable state = new Hashtable();

			for (int v = 0; v < playerIDs.Length; v++)
			{
				if (state.ContainsKey(playerIDs[v]))    // safety measure in case int array has duplicate id:s
				{
					Debug.LogWarning("Warning (" + this + ") Trying to add same player twice to a partial game state (not good). Duplicates will be ignored.");
					continue;
				}
				state.Add(playerIDs[v], ExtractPlayerStats(MPPlayer.Get(playerIDs[v]), stats[v]));
			}

			return state;

		}


		/// <summary>
		/// creates a hashtable with only a few of the stats of a
		/// certain player
		/// </summary>
		protected virtual Hashtable ExtractPlayerStats(MPPlayer player, params string[] stats)
		{

			if (!PhotonNetwork.IsMasterClient)
				return null;

			// create a player hashtable with only the given stats
			Hashtable table = new Hashtable();
			//string str = "Extracting stats for player: " + MPPlayer.GetName(player.ID);
			foreach (string s in stats)
			{
				object o = player.Stats.Get(s);
				if (o == null)
				{
					Debug.LogError("Error: (" + this + ") Player stat '" + s + "' could not be retrieved from player " + player.ID + ".");
					continue;
				}
				table.Add(s, o);
				//str += ": " + s + "(" + o + ")";
			}

			//Debug.Log(str);

			return table;

		}


		/// <summary>
		/// broadcasts a command to load a new level on all clients (master
		/// included) then resets the game which will prompt all player stats
		/// to be reset and players to respawn on new spawnpoints.
		/// NOTES: 1) a scene being loaded must not have a MPConnection
		/// in it. the connection component should be loaded in a startup
		/// scene with 'DontDestroyOnLoad' set to true. 2) the game clock will
		/// be reset as a direct result of loading a new level
		/// </summary>
		public void TransmitLoadLevel(string levelName)
		{

			if (!PhotonNetwork.IsMasterClient)
				return;

			MPMaster.Phase = MPMaster.GamePhase.NotStarted;

			photonView.RPC("ReceiveLoadLevel", RpcTarget.All, levelName);

		}


		/// <summary>
		/// sends a command to load a new level to a specific client. this is
		/// typically sent to players who join an existing game. if the joining
		/// player is the master, the game state will start up for the first time
		/// </summary>
		public void TransmitLoadLevel(Player targetPlayer, string levelName)
		{

			if (!PhotonNetwork.IsMasterClient)
				return;

			if (string.IsNullOrEmpty(levelName))
			{
				Debug.LogError("Error (" + this + ") TransmitLoadlevel -> Level name was null or empty. Remember to set a default level name on the master component.");
				return;
			}

			photonView.RPC("ReceiveLoadLevel", targetPlayer, levelName);

		}


		/// <summary>
		/// loads a new level on this machine as commanded by the master. the
		/// game mode will typically be reset by the master when this happens,
		/// prompting a player stat reset and respawn
		/// </summary>
		[PunRPC]
		public void ReceiveLoadLevel(string levelName, PhotonMessageInfo info)
		{

			if (info.Sender != PhotonNetwork.MasterClient)
				return;

			m_CurrentLevel = levelName;

			//if (PhotonNetwork.IsMasterClient)
			//	PhotonNetwork.LoadLevel(m_CurrentLevel);

		}


		/// <summary>
		/// every client sends this RPC from 'MPConnection -> 'OnJoinedRoom'.
		/// its purpose is to allocate a team and player type to the joinee, use
		/// this info to figure out a matching spawn point, and respond with a
		/// 'permission to spawn' at a certain position, with a certain team and
		/// a suitable player prefab
		/// </summary>
		[PunRPC]
		public void RequestInitialSpawnInfo(Player player, int modelIndex, int grouping)//to avoid using pun props (hackable), we use the received modelIndex from MPConnection.
		{

			if (!PhotonNetwork.IsMasterClient)
				return;

			// make every joining player load the current level
			TransmitLoadLevel(player, m_CurrentLevel);

			if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
			{
				// for non-masters we send spawn info straight away. this assumes
				// the master has already loaded the level and knows the spawnpoints
				TransmitInitialSpawnInfo(player, modelIndex);
			}
			else
			{
				// if we're the FIRST player in the game we take over the game, but
				// we must wait sending spawn info to ourself until 'OnLevelLoad'
				// triggers (because there are no spawnpoints yet)
				m_TookOverGame = true;

				//In this case spawning is done via spawn panel, so we know the scene is loaded
				if (MPPlayerSpawner.Instance.SpawnOnJoin == false)
					TransmitInitialSpawnInfo(player, modelIndex, grouping);
			}

		}


		/// <summary>
		/// <paramref name="player"/> The PhotonPlayer,
		/// <paramref name="modelIndex"/>The Character Model Index.
		/// </summary>
		public void TransmitInitialSpawnInfo(Player player, int modelIndex, int grouping = -1)
		{
			// spawn
			photonView.RPC("ReceiveInitialSpawnInfo", RpcTarget.All, player, modelIndex, grouping);

			// if JOINING player is the master, refresh the game clock since
			// there are no other players and the game needs to get started
			if (player.IsMasterClient)
			{
				//Debug.Log("Preparing game from Master Client")
				PrepareGame();
				return;
			}

			// send the entire game state to the joining player
			// NOTE: we don't need to send the game state of the joinee to all
			// the other players since it has just spawned in the form of a
			// fresh, clean copy of the remote player prefab in question
			TransmitGameState(player);
		}

		/// <summary>
		/// the master client sends this RPC to push its version of the game
		/// state onto all other clients. 'game state' can mean the current
		/// game time and phase + all stats of all players, or it can mean a
		/// partial game state, such as an updated score + frag count for a
		/// sole player. also, instantiates any missing player prefabs
		/// </summary>
		[PunRPC]
		protected virtual void ReceiveGameState(Hashtable gameState, PhotonMessageInfo info)
		{
			//DumpGameState(gameState);

			if ((info.Sender != PhotonNetwork.MasterClient) ||
				(info.Sender.IsLocal))
				return;

			//      MPDebug.Log("Gamestate updated @ " + info.SentServerTime);
			//      Debug.Log("Gamestate updated @ " + info.SentServerTime);

			// -------- extract game phase, game time and duration --------

			if (gameState.TryGetValue("Round", out object round) && round != null)
				m_CurrentRound = (int)round;

			// TODO: make generic method 'ExtractStat' that does this
			if ((gameState.TryGetValue("Phase", out object phase) && (phase != null)))
				Phase = (GamePhase)phase;

			if ((gameState.TryGetValue("TimeLeft", out object timeLeft) && (timeLeft != null))
				&& (gameState.TryGetValue("Duration", out object duration) && (duration != null)))
				MPClock.Set((float)timeLeft - (float)(PhotonNetwork.Time - info.SentServerTime), (float)duration);

			// -------- refresh stats of all players --------

			ReceivePlayerState(gameState, info);

            // -------- refresh health of all non-player damage handlers --------

            foreach (MPHealth d in MPHealth.Instances.Values)
            {
                if (d == null)
                    continue;
                
				if (d is MPCharacterHealth)
                    continue;
                
				PhotonView p = d.GetComponent<PhotonView>();
                if (p == null)
                    continue;

                object currentHealth;
                if (gameState.TryGetValue(-p.ViewID, out currentHealth) && (currentHealth != null))
                {
                    if (d.HealthAttribute != null)
                    {
                        d.HealthAttribute.Value = (float)currentHealth;
                        if (d.HealthValue <= 0.0f)
                            d.gameObject.SetActive(false);
                    }
                }
                else
                    MPDebug.Log("Failed to extract health of damage handler " + p.ViewID + " from gamestate");

                object currentShield;
				if (gameState.TryGetValue(-p.ViewID * 2, out currentShield) && (currentShield != null))
				{
					if (d.ShieldAttribute != null)
					{
						d.ShieldAttribute.Value = (float)currentShield;
					}
				}
				//else //Expected on targets that dont require sheild, Uncomment for debuggging otherwise.
					//MPDebug.Log("Failed to extract shield of damage handler " + p.ViewID + " from gamestate");
			}

			// -------- refresh all teams --------

			if (MPTeamManager.Exists)
				MPTeamManager.Instance.RefreshTeams();

		}


		/// <summary>
		/// 
		/// </summary>
		[PunRPC]
		protected virtual void ReceivePlayerState(Hashtable gameState, PhotonMessageInfo info)
		{
			if ((info.Sender != PhotonNetwork.MasterClient) ||
				(info.Sender.IsLocal))
				return;
			//	Debug.Log("GOT PLAYER STATE! refreshing stats and teams for all players");
			// -------- refresh stats of all included players --------

			foreach (MPPlayer player in MPPlayer.Players.Values)
			{

				if (player == null)
					continue;

				if (gameState.TryGetValue(player.ID, out object stats) && (stats != null))
					player.Stats.SetFromHashtable((Hashtable)stats);
				else
				{
				//	Debug.Log("Failed to extract player " + player.ID + " stats from gamestate");
				//	MPDebug.Log("Failed to extract player " + player.ID + " stats from gamestate");
				}

			}

			// -------- refresh all teams --------

			if (MPTeamManager.Exists)
				MPTeamManager.Instance.RefreshTeams();

		}

		/// <summary>
		/// disarms, stops and locks the local player so that it
		/// cannot move. used when starting non-gameplay game phases,
		/// such as between deathmatch games
		/// </summary>
		[PunRPC]
		protected virtual void ReceiveFreeze(PhotonMessageInfo info)
		{
			if (!info.Sender.IsMasterClient)
				return;

			MPDebug.Log("Recieved Freeze");
			Debug.Log("Recieved Freeze");

			MPLocalPlayer.Freeze();
		}

		/// <summary>
		/// allows local player to move again and tries to wield the
		/// first weapon. used when ending non-gameplay game phases,
		/// such as when starting a new deathmatch game
		/// </summary>
		[PunRPC]
		protected virtual void ReceiveUnFreeze(PhotonMessageInfo info)
		{
			if (!info.Sender.IsMasterClient)
				return;

			MPLocalPlayer.UnFreeze();
		}

		/// <summary>
		/// removes all dropped pickups and all debris resulting from
		/// the fighting. intended for use when a new round starts
		/// </summary>
		[PunRPC]
		protected virtual void ReceiveSceneCleanup(PhotonMessageInfo info)
		{
            // despawn all dropped pickups
            Opsive.UltimateCharacterController.Objects.TrajectoryObject[] allTossedObjects = Component.FindObjectsOfType<Opsive.UltimateCharacterController.Objects.TrajectoryObject>();
            for (int v = (allTossedObjects.Length - 1); v > -1; v--)
            {
                if (allTossedObjects[v] == null)
                    continue;
				if(ObjectPoolBase.InstantiatedWithPool(allTossedObjects[v].gameObject))
					ObjectPoolBase.Destroy(allTossedObjects[v].gameObject);
				else Object.Destroy(allTossedObjects[v].gameObject);
			}

			// despawn all debris such as decals and explosion rubble
			// NOTE: this must iterate all objects in the scene (potentially slow in very complex scenes)
			GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
            for (int v = (allGameObjects.Length - 1); v > -1; v--)
            {
				if (allGameObjects[v].layer == LayerManager.VisualEffect)
				{
					if (allGameObjects[v].GetComponent<MPPersistantSceneObject>())
						continue;
					//Debug.Log("Destroying: " + allGameObjects[v].name);
					if (ObjectPoolBase.InstantiatedWithPool(allGameObjects[v]))
						ObjectPoolBase.Destroy(allGameObjects[v]);
					else Object.Destroy(allGameObjects[v]);
				}
            }
        }

		/// <summary>
		/// dumps a game state hashtable to the console
		/// </summary>
		public static void DumpGameState(Hashtable gameState)
		{

			string s = "--- GAME STATE ---\n(click to view)\n\n";

			if (gameState == null)
			{
				Debug.Log("DumpGameState: Passed gamestate was null: assembling full gamestate.");
				gameState = AssembleGameState();
			}

			foreach (object key in gameState.Keys)
			{
				if (key.GetType() == typeof(int))
				{
					if (gameState.TryGetValue(key, out object player))
						s += MPPlayer.GetName((int)key) + ":\n";

					foreach (object o in ((Hashtable)player).Keys)
					{
						s += "\t\t" + (o.ToString()) + ": ";
						if (((Hashtable)player).TryGetValue(o, out object val))
							s += val.ToString().Replace("(System.String)", "") + "\n";
					}
				}
				else
				{
					if (gameState.TryGetValue(key, out object val))
						s += key.ToString() + ": " + val.ToString();
				}
				s += "\n";
			}

			Debug.Log(s);

		}

		/// <summary>
		/// detects and responds to a master client handover
		/// </summary>
		public override void OnPlayerLeftRoom(Player player)
		{
			base.OnPlayerLeftRoom(player);
			// refresh master control of every MPRigidbody in the scene (done here
			// rather than in the objects themselves because we need to iterate any
			// deactivated / disabled ones too)
			MPRigidbody.RefreshMasterControlAll();

			if (!PhotonNetwork.IsMasterClient)
				return;

			if (PhotonNetwork.CurrentRoom != null)
				if (PhotonNetwork.CurrentRoom.PlayerCount < 1)
					return;

			// if this machine becomes master, broadcast our gamestate!
			if (!m_TookOverGame)
			{
				// reinitialize every respawner in this scene or anything not currently
				// alive will fail to respawn (respawn timer being local to previous master)
				//  Respawner.ResetAll(true);   // give a new respawn time to anything waiting to respawn//TODO: not neccessary?

				// force our gamestate onto everyone else
				TransmitGameState();

				// remember that we have taken over the game. this can only happen once
				m_TookOverGame = true;

			}

		}

		/// <summary>
		/// when a new level is loaded, clears any cached scene objects.
		/// also transmits initial spawn info to the first ever player
		/// </summary>
		protected void OnLevelLoad(Scene scene, LoadSceneMode mode)
		{
			if (scene == m_StartScene)
			{
				Phase = GamePhase.NotStarted;
				EventHandler.ExecuteEvent<MPMaster>("InitGameModes", this);
			}
			// abort this method if it runs too early which may happen in
			// a standalone build
			if (Gameplay.CurrentLevelName != m_CurrentLevel)
				return;

			// clear any cached objects from the previous scene
			m_ViewIDsByTransform.Clear();
			m_TransformsByViewID.Clear();
			MPRigidbody.Instances.Clear();
			MPPlayer.Players.Clear();
			MPPlayer.PlayersByID.Clear();

			if (PhotonNetwork.IsConnected == false)
				return;

			// the first player in the game must send itself spawn info here.
			// this can't be done in 'RequestInitialSpawnInfo' because the
			// spawnpoints are unknown until 'OnLevelLoad'
			if (MPPlayerSpawner.Instance.SpawnOnJoin == true)
				if ((!MPPlayer.PlayersByID.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber)) && (PhotonNetwork.CurrentRoom.PlayerCount == 1))
					TransmitInitialSpawnInfo(PhotonNetwork.LocalPlayer, MPConnection.Instance.SelectedModelIndex);//to avoid using pun props (hackable), we send the SelectedModelIndex 
		}

		/// <summary>
		/// makes sure this game state is the only one operating on this
		/// scene. for when using multiple game mode master prefabs in a
		/// scene and potentially forgetting to having just one enabled
		/// </summary>
		protected virtual void DeactivateOtherMasters()
		{
			if (!enabled)
				return;

			MPMaster[] masters = Component.FindObjectsOfType<MPMaster>() as MPMaster[];
			foreach (MPMaster g in masters)
			{
				if (g.gameObject != gameObject)
					g.gameObject.SetActive(false);  // there can be only one!
			}
		}

		/// <summary>
		/// dumps all network players to the console
		/// </summary>
		protected virtual void DebugDumpPlayers()
		{

			string debugMsg = "Players (excluding self): ";

			for (int p = 0; p < PhotonNetwork.PlayerList.Length; p++)
			{
				if (PhotonNetwork.PlayerList[p].ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
					continue;

				PhotonView[] views = Component.FindObjectsOfType(typeof(PhotonView)) as PhotonView[];
				bool hasView = false;
				foreach (PhotonView f in views)
				{

					if (f.OwnerActorNr == PhotonNetwork.PlayerList[p].ActorNumber)
						hasView = true;

				}

				debugMsg += PhotonNetwork.PlayerList[p].ActorNumber.ToString() + (hasView ? " (has view)" : " (has no view)") + ", ";

			}

			if (debugMsg.Contains(", "))
				debugMsg = debugMsg.Remove(debugMsg.LastIndexOf(", "));

			MPDebug.Log(debugMsg);

		}
	}
}
