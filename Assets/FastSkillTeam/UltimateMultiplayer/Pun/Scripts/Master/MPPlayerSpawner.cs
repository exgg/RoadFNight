/////////////////////////////////////////////////////////////////////////////////
//
//  MPPlayerSpawner.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	Handles multiplayer instantiation and destruction of player
//					gameobjects, plus the allocation of spawnpoints for - and
//					respawning of - players, along with team allocation.
//
//					this component has an inspector list of available player types
//					(local + remote prefab combos for in-game visual representation).
//					only player types in this list will be able to spawn in multiplayer.
//
//					the 'Add Prefabs' and 'Add Components' foldouts lists objects that
//					will be auto-added to local vs. remote players immediately after
//					instantiation. this is a workflow optimization feature that allows
//					using a single prefab without further adjustments, potentially saving
//					you tons of time.
//
//					IMPORTANT: this component should be added to the gameobject _before_
//					the MPMaster component
//
/////////////////////////////////////////////////////////////////////////////////


namespace FastSkillTeam.UltimateMultiplayer.Pun
{
	using UnityEngine;
	using System.Collections.Generic;
	using ExitGames.Client.Photon;
	using Photon.Pun;
	using Photon.Realtime;
	using Opsive.Shared.Game;
	using Opsive.UltimateCharacterController.Game;
	using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun;
	using Opsive.Shared.Utility;
	using Opsive.Shared.Events;
	using Opsive.UltimateCharacterController.Character;
	using Hashtable = ExitGames.Client.Photon.Hashtable;
	using Opsive.UltimateCharacterController.Traits;
#if UNITY_EDITOR
	using UnityEditor;
#endif
	public class MPPlayerSpawner : MonoBehaviourPunCallbacks, IOnEventCallback
	{
		[Tooltip("A reference to the character that PUN should spawn when there is no teams or as a last resort when team character is null(for fast prototyping). This character must be setup using the PUN Multiplayer Manager.")]
		[SerializeField] protected GameObject m_DefaultCharacter;
		[Tooltip ("If spawning via a spawn panel or some other custom way, this should be false. When true characters will be spawned as soon as the game scene has loaded.")]
		[SerializeField] protected bool m_SpawnOnJoin = false;
		[Tooltip("Specifies the location that the character should spawn.")]
		[SerializeField] protected SpawnMode m_Mode;
		[Tooltip("The position the character should spawn if the SpawnMode is set to FixedLocation.")]
		[SerializeField] protected Transform m_SpawnLocation;
		[Tooltip("The offset to apply to the spawn position multiplied by the number of characters within the room.")]
		[SerializeField] protected Vector3 m_SpawnLocationOffset = new Vector3(2, 0, 0);
		[Tooltip("The grouping index to use when spawning to a spawn point. A value of -1 will ignore the grouping value.")]
		[SerializeField] protected int m_SpawnPointGrouping = -1;
		[Tooltip("The amount of time it takes until an inactive player is removed from the room.")]
		[SerializeField] protected float m_InactiveTimeout = 60;

		public bool SpawnOnJoin => m_SpawnOnJoin;
		// Specifies the location that the character should spawn.
		public enum SpawnMode
		{
			FixedLocation,  // Always spawns the character in a fixed location.
			SpawnPoint      // Uses the Spawn Point system.
		}
		public SpawnMode Mode { get { return m_Mode; } set { m_Mode = value; } }
		public Transform SpawnLocation { get { return m_SpawnLocation; } set { m_SpawnLocation = value; } }
		public Vector3 SpawnLocationOffset { get { return m_SpawnLocationOffset; } set { m_SpawnLocationOffset = value; } }
		public int SpawnPointGrouping { get { return m_SpawnPointGrouping; } set { m_SpawnPointGrouping = value; } }

		/// <summary>
		/// Stores the data about the player that became inactive.
		/// </summary>
		private struct InactivePlayer
		{
			public int PlayerIndex;
			public Vector3 Position;
			public Quaternion Rotation;
			public ScheduledEventBase RemoveEvent;

			/// <summary>
			/// Constructor for the InactivePlayer struct.
			/// </summary>
			/// <param name="index">The index within the Player array.</param>
			/// <param name="position">The last position of the player.</param>
			/// <param name="rotation">The last rotation of the player.</param>
			/// <param name="removeEvent">The event that will remove the player from the room.</param>
			public InactivePlayer(int index, Vector3 position, Quaternion rotation, ScheduledEventBase removeEvent)
			{
				PlayerIndex = index;
				Position = position;
				Rotation = rotation;
				RemoveEvent = removeEvent;
			}
		}
		private ResizableArray<PhotonView> m_Players;
		private SendOptions m_ReliableSendOption;
		private RaiseEventOptions m_RaiseEventOptions;
		private Dictionary<int, int> m_ActorNumberByPhotonViewIndex;
		private Dictionary<Player, InactivePlayer> m_InactivePlayers;
		public GameObject DefaultCharacter { get { return m_DefaultCharacter; } set { m_DefaultCharacter = value; } }

		// --- properties ---

		private static MPPlayerSpawner m_Instance = null;
		public static MPPlayerSpawner Instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = Component.FindObjectOfType(typeof(MPPlayerSpawner)) as MPPlayerSpawner;
					//if (m_Instance == null)
					//	Debug.LogError("Error (MPPlayerSpawner) Found no player spawner object. This is bad!");
				}
				return m_Instance;
			}
		}

		// these classes represent objects that will be dynamically added
		// to a player after instantiation

		[System.Serializable]
		public class AddedPrefab
		{
			public GameObject Prefab = null;
			public string ParentName = "";
		}

		[System.Serializable]
		public class AddedComponent
		{

			public string ComponentName = "";
			public string TransformName = "";

			public AddedComponent(string componentName, string transformName = "")
			{
				ComponentName = componentName;
				TransformName = transformName;
			}

		}

		////////////// 'Add Prefabs' section ////////////////
		[System.Serializable]
		public class AddPrefabsSection
		{
			public List<AddedPrefab> Local = new List<AddedPrefab>();
			public List<AddedPrefab> Remote = new List<AddedPrefab>();
		}
		[Tooltip("Names of prefabs that will be childed to every Local vs. Remote player prefab on instantiation, along with the name of a parent transform inside the hierarchy (empty = root)." +
			"\n Use this for components that you wish to initialize with non-default values, such as fonts or textures. Do NOT use for weapons.")]
		[SerializeField]
		protected AddPrefabsSection m_AddPrefabs;

		////////////// 'Add Components' section ////////////////
		[System.Serializable]
		public class AddComponentsSection
		{
			public List<AddedComponent> Local = new List<AddedComponent>(new AddedComponent[] { new AddedComponent("FastSkillTeam.UltimateMultiplayer.Pun.MPPlayerStats") });
			public List<AddedComponent> Remote = new List<AddedComponent>(new AddedComponent[] { new AddedComponent("FastSkillTeam.UltimateMultiplayer.Pun.MPPlayerStats") });
		}
		[Tooltip("Names of components that will be added to every Local vs. Remote player prefab on instantiation, along with the name of a target transform inside the hierarchy (empty = root). " +
			"\n NOTE: All components will have their default values.")]
		[SerializeField] protected AddComponentsSection m_AddComponents;


		/// <summary>
		/// 
		/// </summary>
		protected virtual void Awake()
		{
			// disable any player objects present in the scene by default.
			// in multiplayer, spawning is the only way to go
			// DeactivateScenePlayers();

			m_Players = new ResizableArray<PhotonView>();
			m_ActorNumberByPhotonViewIndex = new Dictionary<int, int>();

			// Cache the raise event options.
			m_ReliableSendOption = new SendOptions { Reliability = true };
			m_RaiseEventOptions = new RaiseEventOptions();
			m_RaiseEventOptions.CachingOption = EventCaching.DoNotCache;
			m_RaiseEventOptions.Receivers = ReceiverGroup.Others;

			// disable any player objects present in the scene by default.
			// in multiplayer, spawning is the only way to go
			DeactivateScenePlayers();
		}

		public override void OnEnable()
		{
			m_Instance = this;
			base.OnEnable();
		}

		public override void OnDisable()
		{
			m_Instance = null;
			base.OnDisable();
		}

		/// <summary>
		/// deactivates any players (local or remote) that are present
		/// in the scene upon Awake. BACKGROUND: player objects may be
		/// placed in the scene to facilitate updating their prefabs,
		/// however once a multiplayer session starts only instantiated
		/// players are allowed
		/// </summary>
		protected virtual void DeactivateScenePlayers()
		{

			UltimateCharacterLocomotion[] players = FindObjectsOfType<UltimateCharacterLocomotion>();
			foreach (UltimateCharacterLocomotion p in players)
				p.gameObject.SetActive(false);

		}


		/// <summary>
		/// respawns the network player of transform 't' at a random, team
		/// based placement
		/// </summary>
		public static void TransmitRespawn(Transform t, Vector3 position, Quaternion rotation, bool transformChange)
		{
			Debug.Log("transmitting player respawn");
			if (!PhotonNetwork.IsMasterClient)
				return;

			MPPlayer p = MPPlayer.Get(t);
			if ((p != null) && MPTeamManager.Exists && (p.TeamNumber > 0))
			{
				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;
				if (!SpawnPointManager.GetPlacement(t.gameObject, MPTeamManager.GetTeamGrouping(p.TeamNumber), ref pos, ref rot))
				{
					Debug.LogError("Failed to get placement for grouping: " + MPTeamManager.GetTeamGrouping(p.TeamNumber));
					return;
				}
				TransmitPlayerRespawn(t, pos, rot, true);
			}
			else
			{
				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;
				if (!SpawnPointManager.GetPlacement(t.gameObject, -1, ref pos, ref rot))
				{
					Debug.LogError("Failed to get placement for grouping: -1");
					return;
				}
				TransmitPlayerRespawn(t, pos, rot, true);
			}

		}


		/// <summary>
		/// respawns the network player of 'transform' at 'placement'
		/// </summary>
		public static void TransmitPlayerRespawn(Transform transform, Vector3 position, Quaternion rotation, bool transformChange)
		{
			Debug.Log("4: transmitting player respawn over network");

			if (!PhotonNetwork.IsMasterClient)
				return;

			//UnityEngine.Debug.Log("respawning " + t.gameObject.name);

			MPPlayer.RefreshPlayers();

			MPPlayer player = MPPlayer.Get(transform);
			if (player != null)
					//player.photonView.RPC("ReceivePlayerRespawn", RpcTarget.All);
				player.Respawner.Respawn(position, rotation, transformChange);

		}


		/// <summary>
		/// this RPC constitutes a 'permission to spawn' sent to us by the
		/// master client in response to a request of such info that every
		/// player makes in 'MPConnection -> OnJoinedRoom' or via the Spawn panel.
		/// The initial spawn info (excluding selected character model) can only be issued by the master client.
		/// </summary>
		[PunRPC]
		private void ReceiveInitialSpawnInfo(Player player, int characterModelIndex, int grouping, PhotonMessageInfo info)
		{
			if ((info.Sender != PhotonNetwork.MasterClient) &&
				(info.Sender != PhotonNetwork.LocalPlayer))
				return;
			
			SpawnPlayer(player, characterModelIndex, grouping);
		}

		public void SpawnPlayer(Player newPlayer, int characterModelIndex, int grouping)
		{
		
			// Only the master client can spawn new players.
			if (!PhotonNetwork.IsMasterClient)
				return;

			//	Debug.Log("RecieveInitialSpawnInfo()");

			//allocate team number
			int teamNumber = 0;
			int spawnGrouping = m_SpawnPointGrouping;
			int modelIndex = characterModelIndex;  //extract selected character model index.

			if (MPTeamManager.Exists)
			{
				// we have teams! If the grouping has not been defined by spawn panel (an objective may be captured to spawn from), then use the team grouping.
				teamNumber = (MPTeamManager.Instance.Teams.Count <= 1) ? 0 : MPTeamManager.Instance.GetSmallestTeam();
				if (grouping == -1)
					spawnGrouping = MPTeamManager.GetTeamGrouping(teamNumber);
				int teamOverideModelIndex = MPTeamManager.GetTeamModelIndex(teamNumber);
				if (teamOverideModelIndex != -1)
					modelIndex = teamOverideModelIndex;

			}
			else if (grouping != -1)//No teams. If the grouping has been defined by spawn panel, then use the allocated grouping, else keep the default provided.
				spawnGrouping = grouping;

			//Let everyone know about the joining player.
			AnnounceArrival(newPlayer, teamNumber);

			// Define the spawn position and rotation...
			Vector3 spawnPosition = Vector3.zero;
			Quaternion spawnRotation = Quaternion.identity;

			//If the player was inactive it shlould spawn where it left off.
			InactivePlayer inactivePlayer;
			if (m_InactivePlayers != null && m_InactivePlayers.TryGetValue(newPlayer, out inactivePlayer))
			{
				// The player has rejoined the game. The character does not need to go through the full spawn procedure.
				Scheduler.Cancel(inactivePlayer.RemoveEvent);
				m_InactivePlayers.Remove(newPlayer);

				// The spawn location is determined by the last disconnected location.
				spawnPosition = inactivePlayer.Position;
				spawnRotation = inactivePlayer.Rotation;
			}
			else
			{
				//...Alter it if spawnmode is fixed location...
				if (m_Mode == SpawnMode.FixedLocation)
				{
					if (m_SpawnLocation != null)
					{
						spawnPosition = m_SpawnLocation.position;
						spawnRotation = m_SpawnLocation.rotation;
					}
					spawnPosition += m_Players.Count * m_SpawnLocationOffset;
				}
				else
				{
					// Allocate a spawn position via spawnpoint.
					SpawnPointManager.GetPlacement(null, spawnGrouping, ref spawnPosition, ref spawnRotation);
				}
			}

			//  Newborn players need to be initialized with the mandatory 'on-join-stats' so lets create that now.
			Hashtable playerStats = new Hashtable
			{
				["Position"] = spawnPosition,
				["Rotation"] = spawnRotation,
				["Team"] = teamNumber,
				["ModelIndex"] = modelIndex,
				["Grouping"] = spawnGrouping
			};
			//	Debug.Log("initial stats for player " + newPlayer.ActorNumber + ": " + playerStats.ToString().Replace("(System.String)", ""));

			// Instantiate the player...
			GameObject player;
			player = GameObject.Instantiate(GetCharacterPrefab(teamNumber), spawnPosition, spawnRotation);
			// ... and let the PhotonNetwork know of the new character.
			var photonView = player.GetComponent<PhotonView>();
			photonView.ViewID = PhotonNetwork.AllocateViewID(newPlayer.ActorNumber);

			if (photonView.ViewID > 0)
			{
				// As of PUN 2.19, when the ViewID is allocated the Owner is not set. Set the owner to null and then to the player so the owner will correctly be assigned.
				photonView.TransferOwnership(null);
				photonView.TransferOwnership(newPlayer);

				// The character has been created. All other clients need to instantiate the character as well.
				var data = new object[]
				{
					player.transform.position, player.transform.rotation, photonView.ViewID, newPlayer.ActorNumber, playerStats
				};
				m_RaiseEventOptions.TargetActors = null;
				PhotonNetwork.RaiseEvent(PhotonEventIDs.PlayerInstantiation, data, m_RaiseEventOptions, m_ReliableSendOption);

				//the newly spawned character is on the master machine.
				if (newPlayer == PhotonNetwork.LocalPlayer)
				{
					//	MPDebug.Log("Master Machine Spawned a Local for : " + photonView.OwnerActorNr);
					//	Debug.Log("Master Machine Spawned a Local for : " + photonView.OwnerActorNr);
					MPRemotePlayer r = player.GetComponent<MPRemotePlayer>();
					if (r != null)
						Component.Destroy(r);
					MPLocalPlayer l = player.GetComponent<MPLocalPlayer>();
					if (l == null)
						l = player.AddComponent<MPLocalPlayer>();

					Instance.AddPrefabs(player.transform, Instance.m_AddPrefabs.Local);
					Instance.AddComponents(player.transform, Instance.m_AddComponents.Local);

					l.Stats.SetFromHashtable(playerStats);
					l.SpawnPositioningMode = m_Mode == SpawnMode.FixedLocation ? Respawner.SpawnPositioningMode.StartLocation : Respawner.SpawnPositioningMode.SpawnPoint;
					l.Grouping = spawnGrouping;
					l.TeamNumber = teamNumber;
				}

				// The new player was just spawned on the master as a remote and should instantiate all existing remote characters in addition to their local character.
				if (newPlayer != PhotonNetwork.LocalPlayer)
				{
					//	MPDebug.Log("Master Machine Spawned a Remote for : " + photonView.OwnerActorNr);
					//	Debug.Log("Master Machine Spawned a Remote for : " + photonView.OwnerActorNr);

					// Deactivate the character until the remote machine has the chance to create it's Local character. This will prevent the character from
					// being active on the Master Client without being able to be controlled.
					player.SetActive(false);

					MPLocalPlayer l = player.GetComponent<MPLocalPlayer>();
					if (l != null)
						Component.Destroy(l);
					MPRemotePlayer r = player.GetComponent<MPRemotePlayer>();
					if (r == null)
						r = player.AddComponent<MPRemotePlayer>();
					Instance.AddPrefabs(player.transform, Instance.m_AddPrefabs.Remote);
					Instance.AddComponents(player.transform, Instance.m_AddComponents.Remote);

					r.Stats.SetFromHashtable(playerStats);
					r.SpawnPositioningMode = m_Mode == SpawnMode.FixedLocation ? Respawner.SpawnPositioningMode.StartLocation : Respawner.SpawnPositioningMode.SpawnPoint;
					r.Grouping = spawnGrouping;
					r.TeamNumber = teamNumber;

					//Get other players data and entire player stats...
					data = new object[m_Players.Count * 5];
					for (int i = 0; i < m_Players.Count; ++i)
					{
						data[i * 5] = m_Players[i].transform.position;
						data[i * 5 + 1] = m_Players[i].transform.rotation;
						data[i * 5 + 2] = m_Players[i].ViewID;
						data[i * 5 + 3] = m_Players[i].Owner.ActorNumber;
						data[i * 5 + 4] = m_Players[i].GetComponent<MPPlayer>().Stats.All;
					}

					//... and send it along with the instantiation event so the newly created player will have all the required start data and stats.
					m_RaiseEventOptions.TargetActors = new int[] { newPlayer.ActorNumber };
					PhotonNetwork.RaiseEvent(PhotonEventIDs.PlayerInstantiation, data, m_RaiseEventOptions, m_ReliableSendOption);
				}

				AddPhotonView(photonView);

				//-------------------------------------------------------------------------------------------------------------------------------------
				//NOTE: This block has been added in case of updates that customers have missed keeping up with, will be removed in the future
				MPCharacterTransformMonitor characterTransformMonitor = photonView.GetComponent<MPCharacterTransformMonitor>();
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
				if (characterTransformMonitor == null)
				{
					var punCharacterTransformMonitor = photonView.GetComponent<Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Character.PunCharacterTransformMonitor>();
					if (punCharacterTransformMonitor == null)
						photonView.gameObject.AddComponent<MPCharacterTransformMonitor>();
				}
#else
					if (characterTransformMonitor == null)
						photonView.gameObject.AddComponent<MPCharacterTransformMonitor>();
#endif
				//-------------------------------------------------------------------------------------------------------------------------------------

				photonView.FindObservables();
				EventHandler.ExecuteEvent("OnPlayerEnteredRoom", photonView.Owner, photonView.gameObject);
			}
			else
			{
				Debug.LogError("Failed to allocate a ViewId.");
				Destroy(player);
			}
		}

		/// <summary>
		/// A event from Photon has been sent.
		/// </summary>
		/// <param name="photonEvent">The Photon event.</param>
		public void OnEvent(EventData photonEvent)
		{
			if (photonEvent.Code == PhotonEventIDs.PlayerInstantiation)
			{
				// The Master Client has instantiated a character. Create that character on the local client as well.
				var data = (object[])photonEvent.CustomData;
				for (int i = 0; i < data.Length / 5; ++i)
				{
					var viewID = (int)data[i * 5 + 2];
					if (PhotonNetwork.GetPhotonView(viewID) != null)
					{
						continue;
					}
					var player = PhotonNetwork.CurrentRoom.GetPlayer((int)data[i * 5 + 3]);

					// we need to extract a few stats in advance from 'playerStats' before
					// a networkplayer can be created: namely playertype, position and rotation.
					// with that info we can spawn the player prefab, and use 'SetStats' to
					// refresh that prefab with the _entire_ provided 'playerStats'
					var playerStats = (Hashtable)data[i * 5 + 4];
					//	Debug.Log("recieved stats for player " + player.ActorNumber + ": " + playerStats.ToString().Replace("(System.String)", ""));
					int spawnGrouping = GetGroupingFromHashtable(playerStats);
					int teamNumber = GetTeamNumberFromHashtable(playerStats);
					//int modelIndex = GetModelIndexFromHashtable(playerStats);
					var character = Instantiate(GetCharacterPrefab(teamNumber), (Vector3)data[i * 5], (Quaternion)data[i * 5 + 1]);
					var photonView = character.GetCachedComponent<PhotonView>();
					photonView.ViewID = viewID;
					// As of PUN 2.19, when the ViewID is set the Owner is not set. Set the owner to null and then to the player so the owner will correctly be assigned.
					photonView.TransferOwnership(null);
					photonView.TransferOwnership(player);
					AddPhotonView(photonView);
					// If the instantiated character is a local player then the Master Client is waiting for it to be created on the client. Notify the Master Client
					// that the character has been created so it can be activated.
					if (photonView.IsMine)
					{
						//  MPDebug.Log("Remote Machine Spawned a Local for : " + photonView.OwnerActorNr);
						//	Debug.Log("Remote Machine Spawned a Local for : " + photonView.OwnerActorNr);
						MPRemotePlayer r = character.GetComponent<MPRemotePlayer>();
						if (r != null)
							Component.Destroy(r);
						MPLocalPlayer l = character.GetComponent<MPLocalPlayer>();
						if (l == null)
							l = character.AddComponent<MPLocalPlayer>();
						Instance.AddPrefabs(character.transform, Instance.m_AddPrefabs.Local);
						Instance.AddComponents(character.transform, Instance.m_AddComponents.Local);
						l.Stats.SetFromHashtable(playerStats);
						l.SpawnPositioningMode = m_Mode == SpawnMode.FixedLocation ? Respawner.SpawnPositioningMode.StartLocation : Respawner.SpawnPositioningMode.SpawnPoint;
						l.Grouping = spawnGrouping;
						l.TeamNumber = teamNumber;

						int ownerActor = photonView.Owner.ActorNumber;
						m_RaiseEventOptions.TargetActors = new int[] { PhotonNetwork.MasterClient.ActorNumber };
						PhotonNetwork.RaiseEvent(PhotonEventIDs.RemotePlayerInstantiationComplete, ownerActor, m_RaiseEventOptions, m_ReliableSendOption);
					}
					else
					{
						//	MPDebug.Log("Remote Machine Spawned a Remote for : " + photonView.OwnerActorNr);
						//	Debug.Log("Remote Machine Spawned a Remote for : " + photonView.OwnerActorNr);
						MPLocalPlayer l = character.GetComponent<MPLocalPlayer>();
						if (l != null)
							Component.Destroy(l);
						MPRemotePlayer r = character.GetComponent<MPRemotePlayer>();
						if (r == null)
							r = character.AddComponent<MPRemotePlayer>();

						Instance.AddPrefabs(character.transform, Instance.m_AddPrefabs.Remote);
						Instance.AddComponents(character.transform, Instance.m_AddComponents.Remote);
						r.Stats.SetFromHashtable(playerStats);
						r.SpawnPositioningMode = m_Mode == SpawnMode.FixedLocation ? Respawner.SpawnPositioningMode.StartLocation : Respawner.SpawnPositioningMode.SpawnPoint;
						r.Grouping = spawnGrouping;
						r.TeamNumber = teamNumber;

						// Call start manually before any events are received. This ensures the remote character has been initialized.
						var characterLocomotion = character.GetCachedComponent<UltimateCharacterLocomotion>();
						characterLocomotion.Start();
					}

					//-------------------------------------------------------------------------------------------------------------------------------------
					//NOTE: This block has been added in case of updates that customers have missed keeping up with, will be removed in the future
					MPCharacterTransformMonitor characterTransformMonitor = photonView.GetComponent<MPCharacterTransformMonitor>();
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
					if (characterTransformMonitor == null)
					{
						var punCharacterTransformMonitor = photonView.GetComponent<Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Character.PunCharacterTransformMonitor>();
						if (punCharacterTransformMonitor == null)
							photonView.gameObject.AddComponent<MPCharacterTransformMonitor>();
					}
#else
					if (characterTransformMonitor == null)
						photonView.gameObject.AddComponent<MPCharacterTransformMonitor>();
#endif
					//-------------------------------------------------------------------------------------------------------------------------------------

					photonView.FindObservables();
					EventHandler.ExecuteEvent("OnPlayerEnteredRoom", photonView.Owner, photonView.gameObject);
					AnnounceArrival(player, teamNumber);
				}
				MPPlayer.RefreshPlayers();
			}
			else if (photonEvent.Code == PhotonEventIDs.RemotePlayerInstantiationComplete)
			{
				// The remote machine has instantiated the local character. The remote can now be enabled (on the Master Client).
				int ownerActor = (int)photonEvent.CustomData;

				for (int i = 0; i < m_Players.Count; ++i)
				{
					if (m_Players[i].Owner.ActorNumber == ownerActor)
					{
						m_Players[i].gameObject.SetActive(true);
						// send the entire game state to the joining player
						// NOTE: we don't need to send the game state of the joinee to all
						// the other players since it has just spawned in the form of a
						// fresh, clean copy of the remote player prefab in question
						MPMaster.Instance.TransmitGameState(m_Players[i].Owner);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Adds the PhotonView to the player list.
		/// </summary>
		/// <param name="photonView">The PhotonView that should be added.</param>
		private void AddPhotonView(PhotonView photonView)
		{
			//if (!m_Players.Contains(photonView))
				m_Players.Add(photonView);
			//if (!m_ActorNumberByPhotonViewIndex.ContainsKey(photonView.OwnerActorNr))
				m_ActorNumberByPhotonViewIndex.Add(photonView.OwnerActorNr, m_Players.Count - 1);
		}

		/// <summary>
		/// Abstract method that allows for a character to be spawned based on the game logic.
		/// </summary>
		/// <param name="newPlayer">The player that entered the room.</param>
		/// <returns>The character prefab that should spawn.</returns>
		protected GameObject GetCharacterPrefab(int teamNumber)
		{
			if (teamNumber != -1)
				return MPTeamManager.GetTeamCharacter(teamNumber);

			// Return the same character for all instances.
			return m_DefaultCharacter;
		}

		/// <summary>
		/// adds standard prefabs to every player upon spawn, as defined under
		/// the editor 'Add Prefabs' foldout
		/// </summary>
		protected virtual void AddPrefabs(Transform rootTransform, List<AddedPrefab> prefabs)
		{
			if (rootTransform == null)
				return;

			foreach (AddedPrefab o in prefabs)
			{

				if (o.Prefab == null)
					continue;
#if UNITY_EDITOR   //'PrefabUtility' is only available in the editor
				if (PrefabUtility.GetPrefabAssetType(o.Prefab) == PrefabAssetType.NotAPrefab)
				{
					Debug.LogError("Error (" + this + ") The gameobject '" + o.Prefab.name + "' is not a prefab! Scene objects are not allowed as auto-added components.");
					continue;
				}
#endif
				Transform t = null;
				if (!string.IsNullOrEmpty(o.ParentName))
					t = Utility.Utility.GetTransformByNameInChildren(rootTransform, o.ParentName, true);
				else t = rootTransform;
			//	if (t == null)
			//	{
					// TIP: uncomment this if missing target gameobject should not be allowed
					//Debug.LogError("Error (" + this + ") 'AddPrefabs' found no transform named '" + o.ParentName + "' in " + rootTransform + ".");
			//		continue;
			//	}

				GameObject n = (GameObject)GameObject.Instantiate(o.Prefab);
				n.transform.parent = t;
				n.transform.localPosition = Vector3.zero;
			}

		}

		private readonly Dictionary<string, System.Type> m_TypeCache = new Dictionary<string, System.Type>();
		/// <summary>
		/// adds standard components to every player upon spawn, as defined under
		/// the editor 'Add Components' foldout
		/// </summary>
		protected virtual void AddComponents(Transform rootTransform, List<AddedComponent> components)
		{

			if (rootTransform == null)
				return;

			foreach (AddedComponent o in components)
			{
				if (string.IsNullOrEmpty(o.ComponentName))
					continue;

				Transform t = null;
				if (!string.IsNullOrEmpty(o.TransformName))
					t = Utility.Utility.GetTransformByNameInChildren(rootTransform, o.TransformName, true);
				if (t == null)
					t = rootTransform.transform;

				System.Type type;
				if (m_TypeCache.ContainsKey(o.ComponentName))
				{
					type = m_TypeCache[o.ComponentName];
				}
				else
				{
					type = System.Type.GetType(o.ComponentName);
					if (type != null)
						m_TypeCache[o.ComponentName] = type;
				}

				if (type == null)
				{
					Debug.LogError("Error (" + this + ") '" + o.ComponentName + "' does not exist or is not of type Component and cannot be added to a player.");
					continue;
				}

				Component res = t.gameObject.AddComponent(type);
				if (res == null)
				{
					Debug.LogError("Error (" + this + ") '" + o.ComponentName + "' does not exist or is not of type Component and cannot be added to a player.");
					continue;
				}
			}
		}

		static int GetTeamNumberFromHashtable(Hashtable table)
		{
			int team = (int)MPPlayerStats.GetFromHashtable(table, "Team");
			return team;
		}

		static int GetGroupingFromHashtable(Hashtable table)
		{
			int group = (int)MPPlayerStats.GetFromHashtable(table, "Grouping");
			return group;
		}

		/// <summary>
		/// extracts the CharacterModelIndex stat from the passed hashtable
		/// </summary>
		static int GetModelIndexFromHashtable(Hashtable table)
		{
			int modelIndex = (int)MPPlayerStats.GetFromHashtable(table, "ModelIndex");
			if (modelIndex < 0)
				return 0;

			return modelIndex;
		}

		/// <summary>
		/// returns a photon player by its ID
		/// </summary>
		public static Player GetPhotonPlayerById(int id)
		{
			foreach (Player p in PhotonNetwork.PlayerList)
			{
				if (p.ActorNumber == id)
					return p;
			}
			return null;
		}

		private void AnnounceArrival(Player player, int teamNumber)
		{
			// announce arrival of new player
			if (player.ActorNumber > PhotonNetwork.LocalPlayer.ActorNumber)
			{
				MPDebug.Log(player.NickName + " joined the game"
					+ ((MPTeamManager.Exists && (teamNumber > 0)) ? " team : " + MPTeamManager.GetTeamName(teamNumber).ToUpper() : "No Teams")
					);
			}
			else if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
			{
				Scheduler.Schedule(0.1f, delegate ()
				{
					//Debug.Log("teamNumber: " + teamNumber);

					try
					{
						MPDebug.Log(
							"Welcome to '"
							+ PhotonNetwork.CurrentRoom.Name
							+ "' with "
							+ PhotonNetwork.CurrentRoom.PlayerCount.ToString()
							+ ((PhotonNetwork.CurrentRoom.PlayerCount == 1) ? " player (you)" : " players")
							+ "."
							);
						//MPDebug.Log("Max players for room: " + PhotonNetwork.room.maxPlayers + ".");
						//MPDebug.Log("Total players using app: " + PhotonNetwork.countOfPlayers);
						if (MPTeamManager.Exists && (teamNumber > 0))
							MPDebug.Log("Your team is: " + MPTeamManager.GetTeamName(teamNumber).ToUpper());
					}
					catch
					{
						if (PhotonNetwork.CurrentRoom == null)
							Debug.Log("PhotonNetwork.room = null");
						else if (PhotonNetwork.CurrentRoom.Name == null)
							Debug.Log("PhotonNetwork.room.name = null");
						if (MPTeamManager.Instance == null)
							Debug.Log("MPTeamManager.Instance = null");
						else if (MPTeamManager.GetTeamName(teamNumber) == null)
							Debug.Log("MPMaster.GetTeamName(teamNumber) = null");
					}

				});
			}
		}

		/// <summary>
		/// Called when a local player leaves the room.
		/// </summary>
		public override void OnLeftRoom()
		{
			base.OnLeftRoom();

			// The local player has left the room. Cleanup like the player has permanently disconnected. If they rejoin at a later time the normal initialization process will run.
			for (int i = 0; i < m_Players.Count; ++i)
			{
				if (m_Players[i] == null)
				{
				//	Debug.Log(i + " is NULL");
					continue;
				}
				EventHandler.ExecuteEvent("OnPlayerLeftRoom", m_Players[i].Owner, m_Players[i].gameObject);
				GameObject.Destroy(m_Players[i].gameObject);
			}
			//Debug.Log("Local Left");
			m_Players.RemoveAll();
			m_ActorNumberByPhotonViewIndex.Clear();
		}

		/// <summary>
		/// Called when a remote player left the room or became inactive. Check otherPlayer.IsInactive.
		/// </summary>
		/// <param name="otherPlayer">The player that left the room.</param>
		public override void OnPlayerLeftRoom(Player otherPlayer)
		{
			base.OnPlayerLeftRoom(otherPlayer);
			Debug.LogFormat("Remote {0} Left as {1}", otherPlayer.ActorNumber, otherPlayer.IsInactive);
			// Notify others that the player has left the room.
			if (m_ActorNumberByPhotonViewIndex.TryGetValue(otherPlayer.ActorNumber, out int index))
			{
				var photonView = m_Players[index];
				// Inactive players may rejoin. Remember the last location of the inactive player.
				if (otherPlayer != null && photonView)
				{
					if (otherPlayer.IsInactive)
					{
						if (m_InactivePlayers == null)
						{
							m_InactivePlayers = new Dictionary<Player, InactivePlayer>();
						}
						var removeEvent = Scheduler.Schedule<Player>(m_InactiveTimeout, (Player player) => { m_InactivePlayers.Remove(player); }, otherPlayer);
						m_InactivePlayers.Add(otherPlayer, new InactivePlayer(index, photonView.transform.position, photonView.transform.rotation, removeEvent));
					}

					EventHandler.ExecuteEvent("OnPlayerLeftRoom", otherPlayer, photonView.gameObject);
					m_ActorNumberByPhotonViewIndex.Remove(otherPlayer.ActorNumber);
				}
				if (photonView)
				{
					GameObject.Destroy(photonView.gameObject);
				}
				m_Players.RemoveAt(index);
				//else m_Players.Clear();
				for (int j = index; j < m_Players.Count - 1; ++j)
				{
					m_ActorNumberByPhotonViewIndex[m_Players[j + 1].Owner.ActorNumber] = j;
				}
			}

			MPPlayer.RefreshPlayers();
		}

        public void ResetPlayers()
        {
			m_Players.Clear();
			m_ActorNumberByPhotonViewIndex.Clear();
		}
    }
}