/////////////////////////////////////////////////////////////////////////////////
//
//  MiniMapSceneObject.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	MiniMapSceneObject is a component that is part of the MiniMap
//	                system. It represents an object in the scene that will be
//	                registered and displayed on the minimap.
//
//                  Add this component to the scene object to be tracked and displayed.  
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.MiniMap
{

#if ULTIMATE_SEATING_CONTROLLER
    using FastSkillTeam.UltimateSeatingController;
#endif
    using FastSkillTeam.UltimateMultiplayer.Pun;
    using Opsive.Shared.Events;
    using UnityEngine;
    using Opsive.UltimateCharacterController.Game;
    using UnityEngine.UI;
    using Opsive.Shared.Game;

    public class MiniMapData
    {
        public GameObject Owner { get; set; }
        // public bool ShowDetails { get; set; } = false;
        public Sprite Icon { get; set; }
        public bool RotateWithObject { get; set; }
        public Vector3Int Up { get; set; }
        public float Rotation { get; set; }
        public Vector2 Size { get; set; }
        public bool ClampIcon { get; set; }
        public float HideDistance { get; set; }
    }

    public class MiniMapSceneObject : MonoBehaviour
    {
        [Tooltip("Sets the owner to this gameobject and prevents checking for a player or boardsource as an owner.")]
        [SerializeField] protected bool m_OwnerIsGameObject;
        [Tooltip("Set true if a spawn button should be added to the map icon object.")]
        [SerializeField] protected bool m_IsSpawnZone;
        [Tooltip("The spawn grouping for the map icon object button.")]
        [SerializeField] protected int m_SpawnGrouping = -1;
        [Tooltip("Should the repawning character have the respawner grouping synced?" +
            "\n If true the player can automatically respawn at last selected spawn grouping, only if it is still valid, if it is not valid the respawner will reset to team grouping.")]
        [SerializeField] protected bool m_SyncRespawnerGrouping = true;
        [Tooltip("Should the component register itself?")]
        [SerializeField] protected bool m_RegisterOnEnable;
        [Tooltip("Should the map icon object be hidden when this gameobject is disabled?")]
        [SerializeField] protected bool m_SyncEnabled = true;
        [Tooltip("Should the Death event be registered? Will disable the map icon object on death if true.")]
        [SerializeField] protected bool m_RegisterDeath;
        [Tooltip("Should the Respawn event be registered? Will re-enable the map icon object on respawn if true.")]
        [SerializeField] protected bool m_RegisterRespawn;

        [Tooltip("Set true if the icon rotates with the gameobject.")]
        [SerializeField] protected OverrideType m_OverrideType = OverrideType.None;
        [Tooltip("Set the icon of this gameobject.")]
        [SerializeField] protected Sprite m_FriendlyIcon;
        [Tooltip("Set the icon of this gameobject.")]
        [SerializeField] protected Sprite m_EnemyIcon;
        [Tooltip("Set a custom icon for this gameobject, this will override Friendly and Enemy Icons, useful for scene objects.")]
        [SerializeField] protected Sprite m_CustomIcon;
        [Tooltip("If using a custom icon, the team color can be set when boarding, if false will use the boarding characters icon color.")]
        [SerializeField] protected bool m_TeamColorCustomIcon;
        [Tooltip("Set size of the icon.")]
        [SerializeField] protected Vector2 m_IconSize = new Vector2(20, 20);
        [Tooltip("Set true if the icon rotates with the gameobject.")]
        [SerializeField] protected bool m_RotateWithObject = false;
        [Tooltip("Adjust the up axis according to your gameobject. Values of each axis can be either -1, 0 or 1.")]
        [SerializeField] protected Vector3Int m_Up = new Vector3Int(0, 1, 0);
        [Tooltip("Adjust initial rotation of the icon.")]
        [SerializeField] protected float m_IconOffsetRotation;
        [Tooltip("If true the icons will be clamped within the border of the map display.")]
        [SerializeField] protected bool m_ClampIcon = true;
        [Tooltip("Set the distance from target after which the icon will not be shown. A value of 0 will always show the icon.")]
        [SerializeField] protected float m_HideDistance = 100;

        protected enum OverrideType { None, ForcedFriendly, ForcedEnemy }

        private MiniMapHandler m_Handler;
        private MiniMapData m_MiniMapData;
        private MiniMapUIObject m_MiniMapUIObject;
        private GameObject m_Owner;
        private int m_TeamNumber = 0;
        private bool m_Initialized = false;

        private Button m_SpawnButton;
        private int m_SyncedGrouping = -1;

#if ULTIMATE_SEATING_CONTROLLER
        private BoardSource m_BoardSource;
        private MiniMapSceneObject m_RCMiniMapSceneObject;
        private bool m_HideBoardedIcons;
        private bool m_RegisteredToBoardEvents = false;
#endif

        private bool m_RegisteredtoTeamEvents = false;

        private void Awake()
        {
            m_Handler = MiniMapHandler.Instance;
            if (!m_Handler)
            {
                Debug.LogError("No Mini Map Controller found! Please ensure there is a Mini Map Controller in your scene!");
                return;
            }

            if (m_OwnerIsGameObject)
                m_Owner = gameObject;

#if ULTIMATE_SEATING_CONTROLLER
            if (m_BoardSource == null)
                m_BoardSource = GetComponentInParent<BoardSource>();
            m_HideBoardedIcons = m_Handler.HideBoardedIcons;
            if (m_BoardSource != null && m_RegisteredToBoardEvents == false)
            {
                if (m_Owner == null)
                    m_Owner = m_BoardSource.GameObject;

                if (m_RCMiniMapSceneObject == null && m_BoardSource.RemoteControlledObject != null)
                    m_RCMiniMapSceneObject = m_BoardSource.RemoteControlledObject.GetComponentInChildren<MiniMapSceneObject>();

                if (m_CustomIcon != null)
                {
                    if (m_HideBoardedIcons)
                        EventHandler.RegisterEvent<SeatChangeData>(m_Owner, "OnSetSeat", OnSetSeat);
                    m_RegisteredToBoardEvents = true;
                    if (m_RegisteredtoTeamEvents == false)
                    {
                        EventHandler.RegisterEvent<int, bool>(m_Owner, "OnSetTeamOwner", OnSetTeamOwner);
                        m_RegisteredtoTeamEvents = true;
                    }
                }
            }
#endif
        }

        private void OnEnable()
        {
            if(m_RegisterOnEnable == true)
            {
                if (m_Initialized == false)
                {
                    if (m_OwnerIsGameObject)
                        m_Owner = gameObject;

                    if (m_Owner == null)
                    {
                        MPPlayer p = GetComponentInParent<MPPlayer>();
                        if (p)
                            m_Owner = p.GameObject;
                        else m_Owner = gameObject;
                    }
                    Initialize(m_Owner, -1, -1);
                }
            }

            if(m_SyncEnabled == true)
            {
                if (m_MiniMapUIObject)
                    m_MiniMapUIObject.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            if (m_SyncEnabled == true)
            {
                if (m_MiniMapUIObject)
                    m_MiniMapUIObject.gameObject.SetActive(false);
            }

            if (m_WaitForLocalPlayerEvent != null)
            {
                SchedulerBase.Cancel(m_WaitForLocalPlayerEvent);
                m_WaitForLocalPlayerEvent = null;
            }
        }

        public void Initialize(GameObject owner, int playerID, int teamNumber)
        {
            m_TeamNumber = teamNumber;

            if (m_Handler == null)
            {
                m_Handler = MiniMapHandler.Instance;
                if (m_Handler == null)
                {
                    Debug.LogError("No Mini Map Controller found! Please ensure there is a Mini Map Controller in your scene!");
                    return;
                }
            }

            if (m_Initialized)
                return;

            m_Owner = owner;

#if ULTIMATE_SEATING_CONTROLLER
            if (m_BoardSource == null)
                m_BoardSource = GetComponentInParent<BoardSource>();
            m_HideBoardedIcons = m_Handler.HideBoardedIcons;
            if (m_BoardSource != null)
            {
                if (m_Owner == null)
                    m_Owner = m_BoardSource.GameObject;

                if (m_RCMiniMapSceneObject == null && m_BoardSource.RemoteControlledObject != null)
                    m_RCMiniMapSceneObject = m_BoardSource.RemoteControlledObject.GetComponentInChildren<MiniMapSceneObject>();

                if (m_CustomIcon != null && m_RegisteredToBoardEvents == false)
                {
                    if (m_HideBoardedIcons)
                        EventHandler.RegisterEvent<SeatChangeData>(m_Owner, "OnSetSeat", OnSetSeat);
                    m_RegisteredToBoardEvents = true;
                }
            }
#endif
            if (m_RegisteredtoTeamEvents == false)
            {
                EventHandler.RegisterEvent<int, bool>(m_Owner, "OnSetTeamOwner", OnSetTeamOwner);
                m_RegisteredtoTeamEvents = true;
            }

            m_Initialized = true;

            bool friendly = m_CustomIcon == null && (m_OverrideType == OverrideType.ForcedFriendly
                || (m_OverrideType == OverrideType.None && (!(MPMaster.Instance is MPDMMaster) || MPTeamManager.Exists && m_TeamNumber == MPLocalPlayer.Instance.TeamNumber)));//TODO:MPTeamManager.Exists &&  is likely not required here, friendly will all be default in non team of 0 (non dm based)!

#if UNITY_EDITOR
            if (friendly == true)
            {
                if (m_FriendlyIcon == null)
                    Debug.LogError("Friendly Icon field must filled for friendly!");
            }
            else if (m_CustomIcon == null && m_EnemyIcon == null)
                Debug.LogError("Custom Icon or Enemy Icon field must filled for either custom or enemy respectively!");
#endif

            m_Up.Clamp(new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1));

            m_MiniMapData = new MiniMapData
            {
                Owner = m_Owner,
                Icon = m_CustomIcon != null ? m_CustomIcon : friendly ? m_FriendlyIcon : m_EnemyIcon,
                Rotation =/* playerID > 0 ? -player.transform.eulerAngles.y + m_IconOffsetRotation :*/ m_IconOffsetRotation,//for characters the offset needs to be altered to suit the spawned direction.
                Size = m_IconSize,
                Up = m_Up,
                RotateWithObject = m_RotateWithObject,
                ClampIcon = m_ClampIcon,
                HideDistance = m_HideDistance
            };

            m_MiniMapUIObject = m_Handler.RegisterMapObject(m_MiniMapData);
           
            if (m_IsSpawnZone && m_SpawnButton == null)
            {
                m_SpawnButton = m_MiniMapUIObject.gameObject.AddComponent<Button>();
                m_SpawnButton.onClick.AddListener(() => OnClick(m_SpawnGrouping));
            }

            if (m_RegisterDeath == true)
                EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(m_Owner, "OnDeath", OnDeath);
            if (m_RegisterRespawn == true)
                EventHandler.RegisterEvent(m_Owner, "OnRespawn", OnRespawn);

            OnSetTeamOwner(m_TeamNumber, false);
        }

        private void OnClick(int group)
        {
            /*if (m_FirstSpawn)
            {
                MPMaster.Instance.photonView.RPC("RequestInitialSpawnInfo", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, MPConnection.Instance.SelectedModelIndex, group);//to avoid using pun props (hackable), we send the SelectedModelIndex 
                m_FirstSpawn = false;
                //    gameObject.SetActive(false);
                return;
            }*/

            Vector3 pos = transform.position;
            var rot = transform.rotation;

            if (SpawnPointManager.GetPlacement(MPLocalPlayer.Instance.GameObject, group, ref pos, ref rot))
            {
                MPLocalPlayer.Instance.Respawner.CancelRespawn();
                if (m_SyncRespawnerGrouping)
                    MPLocalPlayer.Instance.Respawner.Grouping = group;
                MPLocalPlayer.Instance.Respawner.Respawn(pos, rot, true);
                m_SyncedGrouping = group;
            }
        }

        public void SetColor(Color color)
        {
            SetColorInternal(color);
        }
        private void SetColorInternal(Color color)
        {
            if (m_MiniMapUIObject == null)
                return;
            m_MiniMapUIObject.SetColor(color);
        }

        private void Update()
        {
            if (!m_IsSpawnZone)
                return;
            if (!m_SpawnButton)
                return;

            m_SpawnButton.enabled = MPLocalPlayer.Instance != null && m_TeamNumber == MPLocalPlayer.Instance.TeamNumber && MPLocalPlayer.Instance.PlayerHealth.IsAlive() == false;
        }
        public void SetTeamOwner(int teamNumber)
        {
            OnSetTeamOwner(teamNumber, false);
        }

        private ScheduledEventBase m_WaitForLocalPlayerEvent = null;
        private void OnSetTeamOwner(int teamNumber, bool transmit)
        {
            if (m_CustomIcon == null)
                return;

/*            if (teamNumber > 0 && MPLocalPlayer.Instance == null)
            {
                Debug.LogWarning("No local player instance (MPLocalPlayer) available. Will try again in 1 second.");
                if (m_WaitForLocalPlayerEvent == null)
                    m_WaitForLocalPlayerEvent = SchedulerBase.Schedule(1f, () => OnSetTeamOwner(teamNumber, transmit));
                return;
            }
            if (m_WaitForLocalPlayerEvent != null)
            {
                Debug.Log("Local player instance (MPLocalPlayer) now available.");
                SchedulerBase.Cancel(m_WaitForLocalPlayerEvent);
                m_WaitForLocalPlayerEvent = null;
            }*/

            m_TeamNumber = teamNumber;

            if(m_IsSpawnZone == true && m_SpawnButton != null)
            {
                //ensure respawner is up to date, if the respawner grouping is the same as a spawnpoint grouping that was lost (objective takeover) then it needs to be reset.
                if (m_SyncRespawnerGrouping && m_SyncedGrouping == m_SpawnGrouping 
                    && MPLocalPlayer.Instance != null && teamNumber != MPLocalPlayer.Instance.TeamNumber && MPLocalPlayer.Instance.Respawner.Grouping == m_SyncedGrouping)
                {
                    m_SyncedGrouping = MPTeamManager.GetTeamGrouping(MPLocalPlayer.Instance.TeamNumber);
                    MPLocalPlayer.Instance.Respawner.Grouping = m_SyncedGrouping;
                }
            }

            Color color = m_TeamNumber > 0 && m_TeamColorCustomIcon ? MPTeamManager.GetTeamColor(m_TeamNumber) : m_TeamNumber <= 0 ? Color.white : MPLocalPlayer.Instance != null && m_TeamNumber == MPLocalPlayer.Instance.TeamNumber ? Color.blue : Color.red;
            m_MiniMapUIObject.SetColor(color);
#if ULTIMATE_SEATING_CONTROLLER
            if (m_RCMiniMapSceneObject != null)
                m_RCMiniMapSceneObject.SetColorInternal(color);
#endif
        }

#if ULTIMATE_SEATING_CONTROLLER
        private void OnSetSeat(SeatChangeData data)
        {
            if (data.SeatActive)
            {
                MiniMapSceneObject m = data.Character.GetComponentInChildren<MiniMapSceneObject>();
                if (m && m.m_MiniMapUIObject)
                {
                    m.m_MiniMapUIObject.gameObject.SetActive(false);
                    if (data.Transmit)// If Transmit is true, then this was called from a local player. Update the handler temporarily so the map follows the BoardSource.
                                      // NOTE: At time of writing, only required for baked maps but makes sense to do it for either as it is harmless to dynamic maps and could be retrieved at some other point
                                      //       via the MiniMapHandler.
                        MiniMapHandler.Instance.LocalPlayerTransform = m_BoardSource.RemoteControlledObject ? m_BoardSource.RemoteControlledObject.transform : transform;
                }
            }
            else
            {
                MiniMapSceneObject m = data.Character.GetComponentInChildren<MiniMapSceneObject>(true);
                if (m && m.m_MiniMapUIObject)
                {
                    m.m_MiniMapUIObject.gameObject.SetActive(true);
                    if (data.Transmit)// If Transmit is true, then this was called from a local player. Update the handler temporarily so the map follows the BoardSource.
                                      // NOTE: At time of writing, only required for baked maps but makes sense to do it for either as it is harmless to dynamic maps and could be retrieved at some other point
                                      //       via the MiniMapHandler.
                        MiniMapHandler.Instance.LocalPlayerTransform = data.Character.transform;
                }
            }
        }
#endif
        private void OnRespawn()
        {
            if (m_MiniMapUIObject)
                m_MiniMapUIObject.gameObject.SetActive(true);
        }

        private void OnDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            if (m_MiniMapUIObject)
                m_MiniMapUIObject.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
#if ULTIMATE_SEATING_CONTROLLER
            if (m_CustomIcon != null && m_BoardSource != null)
            {
                if (m_HideBoardedIcons && m_RegisteredToBoardEvents == true)
                    EventHandler.UnregisterEvent<SeatChangeData>(m_Owner, "OnSetSeat", OnSetSeat);
            }
#endif
            if (m_RegisteredtoTeamEvents == true)
                EventHandler.UnregisterEvent<int, bool>(m_Owner, "OnSetTeamOwner", OnSetTeamOwner);

            if (m_Initialized == false)
                return;

            if (m_RegisterRespawn == true)
                EventHandler.UnregisterEvent(m_Owner, "OnRespawn", OnRespawn);
            if (m_RegisterDeath == true)
                EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(m_Owner, "OnDeath", OnDeath);
            if (m_Handler != null)
                m_Handler.UnregisterMapObject(gameObject, m_MiniMapUIObject);

            m_Initialized = false;
            m_RegisteredtoTeamEvents = false;

#if ULTIMATE_SEATING_CONTROLLER
            m_RegisteredToBoardEvents = false;
#endif
        }
    }
}