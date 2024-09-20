/////////////////////////////////////////////////////////////////////////////////
//
//  MPPlayer.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	Base class for the UCC player object in multiplayer. implements
//					basic network functionality for respawn, firing, dying and teleport.
//					declares some fundamental stats and creates references to a number
//					of expected player components. this is the abstract base class of
//					MPLocalPlayer and MPRemotePlayer, acting as a bridge between
//					the photon cloud and every UCC player object in your scene (whether
//					remote or local). it is game mode agnostic
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
#if PHOTON_UNITY_NETWORKING
    using Photon.Pun;
#endif
#if ULTIMATE_SEATING_CONTROLLER
    using FastSkillTeam.UltimateSeatingController;
#endif
    using UnityEngine;
    using System.Collections.Generic;
    using Opsive.UltimateCharacterController.Traits;
    using Opsive.UltimateCharacterController.Character;
    using static Opsive.UltimateCharacterController.Traits.Respawner;
    using Opsive.Shared.Events;

    public abstract partial class MPPlayer :
#if PHOTON_UNITY_NETWORKING
		MonoBehaviourPun
#else
		MonoBehaviour
#endif
	{

		// fundamental stats
		private int m_ID = 1;
		public int ID// returns id of corresponding network player
		{
			get
			{
#if PHOTON_UNITY_NETWORKING
				if (PhotonNetwork.IsConnected)
				{
					if (m_PhotonView == null)
						m_PhotonView = photonView;
					if (m_PhotonView != null)
						m_ID = photonView.OwnerActorNr;
				}
				return m_ID;
#else
				return m_ID;
#endif
			}
			set { m_ID = value; }
		}
		//private int m_ModelIndex = 0;
		public int ModelIndex // determines the local and remote player prefabs to be used for visual representation
		{
			get
			{
				if (ModelManager == null)
					return 0;
				return m_ModelManager.ActiveModelIndex;
			}
			set
			{
				if (ModelManager == null)
					return;
				int modelIndex = value;
				if (modelIndex < 0)
					modelIndex = 0;
				if (modelIndex >= m_ModelManager.AvailableModels.Length)
					modelIndex = m_ModelManager.AvailableModels.Length - 1;
				m_ModelManager.ActiveModel = m_ModelManager.AvailableModels[modelIndex];
			}
		}           

		public int Grouping
        {
			get
			{
				if (Respawner == null)
					return -1;
				return m_Respawner.Grouping;
			}
			set
			{
				if (Respawner == null)
					return;
				m_Respawner.Grouping = value;
			}
		}

		public SpawnPositioningMode SpawnPositioningMode
		{
			get
			{
				if (Respawner == null)
					return SpawnPositioningMode.StartLocation;
				return m_Respawner.PositioningMode;
			}
			set
            {
				if (Respawner == null)
					return;
				m_Respawner.PositioningMode = value;
			}
		}

		private int m_TeamNumber = 0;
		/// <summary>
		/// Optional, can be used for competitive gameplay or just for organizing players into groups.
		/// </summary>
		public int TeamNumber
		{
			get
			{
				return m_TeamNumber;
			}
			set
			{
				m_TeamNumber = value;

#if ULTIMATE_SEATING_CONTROLLER
				if (m_BoardAbility != null)
					m_BoardAbility.TeamID = m_TeamNumber;
#endif

				if (MiniMapComponent != null)
					m_MiniMapComponent.Initialize(gameObject, ID, m_TeamNumber);
			}
		}

		/// <summary>
		/// Optional, can be used for competitive gameplay or just for organizing players into groups.
		/// </summary>
		public int SquadNumber { get; set; } = 0;

		/// <summary>
		/// The amount of times the player has spawned projectiles. used for establishing deterministic 
		/// random seeds that will be the same on all machines _without_ sending data over the network.
		/// NOTE: MPShotsFiredTracker module sets this value via MPPlayerStats. MPShotsFiredTracker generally added to the Fire Effects Modules section of the weapon.
		/// </summary>
		public int Shots { get; set; } = 0;

		/// <summary>
		/// Returns the client's current roundtrip time to the photon server.
		/// NOTE: this is reported locally by every client in 'MPConnection.UpdatePing'
		/// </summary>
		public int Ping
		{
			get
			{
#if PHOTON_UNITY_NETWORKING
				if (m_PhotonView == null)
					return 0;
				if (m_PhotonView.Owner == null)
					return 0;
				if (m_PhotonView.Owner.CustomProperties["Ping"] == null)
					return 0;
				return (int)m_PhotonView.Owner.CustomProperties["Ping"];
#else
				return 0;
#endif
			}
		}

		protected GameObject m_GameObject;
		public GameObject GameObject => m_GameObject;

		protected Transform m_Transform;
		public Transform Transform { get { if (m_Transform == null) m_Transform = transform; return m_Transform; } }
		
		/// <summary>
		/// The team this player belongs to.
		/// </summary>
		public MPTeam Team { get { return (MPTeamManager.Exists ? MPTeamManager.Instance.Teams[m_TeamNumber] : null); } }
		public new bool DontDestroyOnLoad = true;//REMINDER, Make accessible to user. Add it to spawner?? 

		// work variables
		public Vector3 LastMasterPosition { get; set; } = Vector3.zero;
		public Quaternion LastMasterRotation { get; set; } = Quaternion.identity;

		// --- required components ---
		protected UltimateCharacterLocomotion m_CharacterLocomotion = null;
		public UltimateCharacterLocomotion GetUltimateCharacterLocomotion
		{
			get
			{
				if (m_CharacterLocomotion == null)
				{
					if (m_Transform == null)
						return null;
					m_CharacterLocomotion = (UltimateCharacterLocomotion)m_Transform.GetComponentInChildren(typeof(UltimateCharacterLocomotion));
				}
				return m_CharacterLocomotion;
			}
		}

		protected MPPlayerStats m_Stats = null;
		public MPPlayerStats Stats
		{
			get
			{
				if (m_Stats == null)
				{
					if (Transform == null)
						return null;
					m_Stats = (MPPlayerStats)Transform.GetComponentInChildren(typeof(MPPlayerStats));
				}
				return m_Stats;
			}
		}

		private MiniMap.MiniMapSceneObject m_MiniMapComponent;
		public MiniMap.MiniMapSceneObject MiniMapComponent
		{
			get
			{
				if (!m_MiniMapComponent)
				{
					if (Transform == null)
						return null;
					m_MiniMapComponent = Transform.GetComponentInChildren<MiniMap.MiniMapSceneObject>();
				}
				return m_MiniMapComponent;
			}
		}

		// --- expected components ---

		Collider m_Collider = null;
		public Collider Collider
		{
			get
			{
				if (m_Collider == null)
				{
					if (m_Transform == null)
						return null;
					m_Collider = m_Transform.GetComponentInChildren<Collider>();
				}
				return m_Collider;
			}
		}

		protected MPCharacterHealth m_PlayerHealth = null;
		public MPCharacterHealth PlayerHealth
		{
			get
			{
				if (m_PlayerHealth == null)
				{
					if (m_Transform == null)
						return null;

					m_PlayerHealth = m_Transform.GetComponentInChildren<MPCharacterHealth>();
				}
				return m_PlayerHealth;
			}
		}

		protected Respawner m_Respawner = null;
		public Respawner Respawner
		{
			get
			{
				if (m_Respawner == null)
				{
					if (m_Transform == null)
						return null;
					m_Respawner = m_Transform.GetComponentInChildren<Respawner>();
				}
				return m_Respawner;
			}
		}

		protected ModelManager m_ModelManager = null;
		public ModelManager ModelManager
        {
			get
			{
				if (m_ModelManager == null)
				{
					if (m_Transform == null)
						return null;
					m_ModelManager = m_Transform.GetComponentInChildren<ModelManager>();
				}
				return m_ModelManager;
			}
		}

		protected CharacterLayerManager m_CharacterLayerManager;
		public CharacterLayerManager CharacterLayerManager { get { if (m_CharacterLayerManager == null) m_CharacterLayerManager = m_GameObject.GetComponent<CharacterLayerManager>(); return m_CharacterLayerManager; } }
		private LayerMask SolidObjectLayers
		{
			get
			{
				return CharacterLayerManager.SolidObjectLayers;
			}
		}

	//	protected Opsive.Shared.Networking.INetworkInfo m_NetworkInfo;
	//	public Opsive.Shared.Networking.INetworkInfo NetworkInfo { get { if (m_NetworkInfo == null) m_NetworkInfo = m_GameObject.GetComponent<Opsive.Shared.Networking.INetworkInfo>(); return m_NetworkInfo; } }

        // --- static dictionaries of player info and stats ---

        /// <summary>
        /// dictionary of players, stored by transform.
        /// a network player will be added to this dictionary on Awake,
        /// and removed by 'RefreshPlayers' when it no longer exists
        /// </summary>
        public static Dictionary<Transform, MPPlayer> Players
		{
			get
			{
				if (m_Players == null)
					m_Players = new Dictionary<Transform, MPPlayer>();
				return m_Players;
			}
		}
		protected static Dictionary<Transform, MPPlayer> m_Players = null;


		/// <summary>
		/// dictionary of players, stored by ID.
		/// a network player will be added to this dictionary by the 'Get(id)'
		/// method, and removed by 'RefreshPlayers' when it no longer exists.
		/// TODO: this dictionary should not be public since it is not as reliable
		/// as the public 'Players' dictionary, or as using the Get(id) method.
		/// </summary>
		public static Dictionary<int, MPPlayer> PlayersByID
		{
			get
			{
				if (m_PlayersByID == null)
					m_PlayersByID = new Dictionary<int, MPPlayer>();
				return m_PlayersByID;
			}
		}
		protected static Dictionary<int, MPPlayer> m_PlayersByID = null;


		/// <summary>
		/// a key collection returning the integer IDs of all players
		/// </summary>
		public static Dictionary<int, MPPlayer>.KeyCollection IDs
		{
			get
			{
				return PlayersByID.Keys;
			}
		}

#if PHOTON_UNITY_NETWORKING
		private PhotonView m_PhotonView;
#endif
#if ULTIMATE_SEATING_CONTROLLER
		protected Board m_BoardAbility;
#endif

		protected bool m_UsingOpsiveSync = false;

		/// <summary>
		/// 
		/// </summary>
		public virtual void Awake()
		{
			m_GameObject = gameObject;
			m_Transform = transform;
			m_CharacterLocomotion = m_GameObject.GetComponent<UltimateCharacterLocomotion>();

#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
			m_UsingOpsiveSync = m_GameObject.GetComponent<Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Character.PunCharacterTransformMonitor>();
#endif
			if (!m_UsingOpsiveSync)
				m_UsingOpsiveSync = m_GameObject.GetComponent<MPCharacterTransformMonitor>();

#if PHOTON_UNITY_NETWORKING
			m_PhotonView = photonView;
#endif

#if ULTIMATE_SEATING_CONTROLLER
			if (m_CharacterLocomotion != null)
			{
				m_BoardAbility = m_CharacterLocomotion.GetAbility<Board>();
				if (m_BoardAbility != null)
					m_BoardAbility.TeamID = m_TeamNumber;
			}
#endif

			// ensure that all network players ever created are added to the
			// player list. NOTE: we don't add players to the 'PlayerIDs'
			// dictionary here, since ID has not been assigned yet and is 0
			if (!Players.ContainsKey(m_Transform) && !Players.ContainsValue(this))
				Players.Add(m_Transform, this);

			if (DontDestroyOnLoad)
				UnityEngine.Object.DontDestroyOnLoad(m_Transform.gameObject);

            EventHandler.RegisterEvent(m_GameObject, "OnWillRespawn", OnWillRespawn);
        }
        private void OnWillRespawn()
        {
            GetUltimateCharacterLocomotion.SetMovingPlatform(null);
        }
        protected virtual void OnDestroy()
        {
            EventHandler.UnregisterEvent(m_GameObject, "OnWillRespawn", OnWillRespawn);
        }
        /// <summary>
        /// removes departed players and refreshes components of remaining
        /// players. this includes team color, spawnpoint targets, collider
        /// event logic, body materials and gameobject names in the editor
        /// </summary>
        public static void RefreshPlayers()
		{

			// --- removes departed players ---

			List<int> nullIDs = null;
			List<Transform> nullTransforms = null;
			List<GameObject> departedPlayers = null;

			// find network players whose photon players have left
			foreach (MPPlayer p in MPPlayer.Players.Values)
			{
				if (p == null)
					continue;
#if PHOTON_UNITY_NETWORKING
				if ((p.photonView == null) || (p.photonView.Owner == null))
				{
					if (departedPlayers == null)
						departedPlayers = new List<GameObject>();
					departedPlayers.Add(p.gameObject);
					Debug.Log("departed players true");
				}
#endif
			}

			if (departedPlayers != null)
			{
				foreach (GameObject g in departedPlayers)
				{
					UnityEngine.Object.DestroyImmediate(g);
				}
			}

			// find transforms that have no network player
			foreach (Transform key in Players.Keys)
			{
				MPPlayer player;
				Players.TryGetValue(key, out player);
				if (player == null)
				{
					if (nullTransforms == null)
						nullTransforms = new List<Transform>();
					nullTransforms.Add(key);
					continue;
				}

				if (!PlayersByID.ContainsValue(player))
				{
					if (player.ID != 0)
						PlayersByID.Add(player.ID, player);
				}

			}

			// find ids that have no player
			foreach (int key in PlayersByID.Keys)
			{
				PlayersByID.TryGetValue(key, out MPPlayer player);
				if (player == null)
				{
					if (nullIDs == null)
						nullIDs = new List<int>();
					nullIDs.Add(key);
				}
			}

			// remove null IDs and transforms
			if (nullTransforms != null)
				foreach (Transform t in nullTransforms)
					Players.Remove(t);


			if (nullIDs != null)
				foreach (int i in nullIDs)
					PlayersByID.Remove(i);


			// --- refresh components of remaining players ---

			foreach (MPPlayer p in MPPlayer.Players.Values)
			{
				if (p == null)
					continue;
				p.RefreshComponents();
			}

		}

        /// <summary>
        /// 
        /// </summary>
        protected virtual void RefreshComponents()
		{
#if UNITY_EDITOR
			// update gameobject name in editor hierarchy view with
			// local/remote and master/client status
			gameObject.name = ID.ToString()
#if PHOTON_UNITY_NETWORKING
				+ ((ID == PhotonNetwork.LocalPlayer.ActorNumber) ? " (LOCAL)" : "(REMOTE)")
				+ (photonView.Owner.IsMasterClient ? "(MASTER)" : "")
#endif
				;
			gameObject.name = gameObject.name.Replace(")(", ", ");
#endif
		}

		/// <summary>
		/// instantly teleports player to a position and rotation. remote
		/// player overrides this to prevent lerping its position
		/// </summary>
		public virtual void SetPositionAndRotation(Vector3 position, Quaternion rotation)
		{
			if (GetUltimateCharacterLocomotion == null)
				return;

			//GetUltimateCharacterLocomotion.SetPositionAndRotation(position, rotation, false, false, false);
		}

		/// <summary>
		/// instantly teleports player to a position. remote player overrides
		/// this to prevent lerping its position
		/// </summary>
		public virtual void SetPosition(Vector3 position)
		{
			if (GetUltimateCharacterLocomotion == null)
				return;

		//	GetUltimateCharacterLocomotion.SetPosition(position);
		}

		/// <summary>
		/// instantly sets player rotation
		/// </summary>
		public virtual void SetRotation(Quaternion rotation)
		{
			if (GetUltimateCharacterLocomotion == null)
				return;

		//	GetUltimateCharacterLocomotion.SetRotation(rotation);
		}

		/// <summary>
		/// returns the MPPlayer associated with a certain
		/// photon player id
		/// </summary>
		public static MPPlayer Get(int id)
		{
			if (!PlayersByID.TryGetValue(id, out MPPlayer player))
			{
				foreach (MPPlayer p in Players.Values)
				{
					if (p == null)
						continue;
					if (p.ID == id)
					{
						PlayersByID.Add(id, p);
						return p;
					}
				}
			}

			return player;
		}

		/// <summary>
		/// returns the MPPlayer associated with 'transform' (if any)
		/// </summary>
		public static MPPlayer Get(Transform transform)
		{
			if (Players.TryGetValue(transform, out MPPlayer player))
				return player;
			return null;
		}

		/// <summary>
		/// returns photon player name of photon player with 'playerID'
		/// </summary>
		public static string GetName(int playerID)
		{
			for (int p = 0; p < PhotonNetwork.PlayerList.Length; p++)
			{
				if (PhotonNetwork.PlayerList[p].ActorNumber == playerID)
				{
					if (PhotonNetwork.PlayerList[p].NickName == "Player")
						return "Player " + playerID.ToString();
					else
						return PhotonNetwork.PlayerList[p].NickName;
				}
			}

			return "Unknown";
		}

		/// <summary>
		/// 
		/// </summary>
		public static void TransmitUnFreezeAll()
		{
			if (!PhotonNetwork.IsMasterClient)
				return;

			// unfreeze all players on all machines
			MPMaster.Instance.photonView.RPC("ReceiveUnFreeze", RpcTarget.All);
		}

		public static void TransmitFreezeAll()
		{

			if (!PhotonNetwork.IsMasterClient)
				return;

			// freeze all players on all machines
			MPMaster.Instance.photonView.RPC("ReceiveFreeze", RpcTarget.All);

		}


		/// <summary>
		/// this method can be used to protect against general distance cheats
		/// (like sending an RPC to push a button when you're in fact nowhere
		/// near it). for example: can be called on the master to verify that a
		/// player is within range of an object that it wants to manipulate.
		/// by default the method will return true if the player is inside or
		/// less than 2 meters away from the bounding box of 'collider', and
		/// false if not
		/// </summary>
		public bool IsCloseTo(Collider otherCollider, float distance = 2f)
		{

			distance = Mathf.Max(distance, GetUltimateCharacterLocomotion.Radius + GetUltimateCharacterLocomotion.SkinWidth);

			if (Vector3.Distance(Collider.bounds.center, otherCollider.ClosestPointOnBounds(Collider.bounds.center)) < distance)
				return true;    // player is in proximity to, or touching the collider bounds

			if (otherCollider.bounds.Contains(Collider.bounds.center))
				return true;    // player center is inside collider bounds (addresses cases where the above
								// distance check might fail due to standing inside very large bounds)

			if (otherCollider.bounds.Contains(Collider.bounds.center - (Vector3.up * (GetUltimateCharacterLocomotion.Height * 0.5f))))
				return true;    // player is standing on collider bounds

			if (otherCollider.bounds.Contains(Collider.bounds.center + (Vector3.up * (GetUltimateCharacterLocomotion.Height * 0.5f))))
				return true;    // player head is touching collider bounds

			// player is not close to the collider
			return false;

		}


		/// <summary>
		/// iterates the 'Shots' variable by one. this triggers every time a
		/// remote player fires a weapon (once per weapon discharge) and is
		/// used as a seed for generating deterministic random bullet spread
		/// that will be the same on all machines _without_ sending possibly
		/// hacked data over the network. the 'Shots' variable is later fetched
		/// in FST_Shooter's 'GetFireSeed' method and this is hooked up in the
		/// 'InitShooters' method of MPLocalPlayer and MPRemotePlayer.
		/// in short: prevents accuracy hacks
		/// </summary>
		//	[PunRPC]
		//	public virtual void FireWeapon(int weaponIndex, Vector3 position, Quaternion rotation, PhotonMessageInfo info)
		//{

		// must be increased even if no shot is eventually fired, or bullet
		// simulation will go out of sync
		//	Shots++;

		//}


		/// <summary>
		/// when this RPC arrives, the player will die immediately because the
		/// master client says so
		/// </summary>
		[PunRPC]
		public virtual void ReceivePlayerKill(Vector3 position, Vector3 force, int attackerViewID, PhotonMessageInfo info)
		{
			Debug.Log(this.GetType() + ".ReceivePlayerKill");

			if (info.Sender != PhotonNetwork.MasterClient)
				return;

			//NOTE: Methods will need to change to use actor number (p.ID) instead of viewIDs, and the attacking object name will need to be included in the RPC.
			//		Currently this is done by damage within MPDMDamageCallbacks and will trigger one extra RPC for kill feed.
			//		For now it has to happen on master, as only master knows all!
			//	MPPlayer p = MPPlayer.Get(attackerID);
			//	EventHandler.ExecuteEvent<string, string, string>("ReportMPKill", MPPlayer.GetName(p.ID), attackingObject ? attackingObject.name : "FUBAR", MPPlayer.GetName(ID));

			// local master is not allowed to call 'DamageHandler.Die' on itself (infinite loop)
			if (PhotonNetwork.LocalPlayer == PhotonNetwork.MasterClient)
				return;

			PhotonView attacker = null;
			if (attackerViewID != -1)
				attacker = PhotonNetwork.GetPhotonView(attackerViewID);

			PlayerHealth.Die(position, force, attacker ? attacker.gameObject : null);
		}

		/// <summary>
		/// Does the respawn on the network by setting the position and rotation to the specified values.
		/// Enable the GameObject and let all of the listening objects know that the object has been respawned.
		/// </summary>
		/// <param name="position">The respawn position.</param>
		/// <param name="rotation">The respawn rotation.</param>
		/// <param name="transformChange">Was the position or rotation changed?</param>
		[PunRPC]
		public virtual void RespawnRPC(Vector3 position, Quaternion rotation, bool transformChange, PhotonMessageInfo info)
		{

		}
    }
}