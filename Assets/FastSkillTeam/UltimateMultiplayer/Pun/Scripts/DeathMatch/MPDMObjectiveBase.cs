/////////////////////////////////////////////////////////////////////////////////
//
//  MPDMObjectiveBase.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	The base class of all objectives for game modes. Bonus score
//                  is applied for players within the bonus zone trigger. Position
//                  and rotation of the root object can be synchronised.
//
//  TIP:            See MPDMObjectiveRush.cs, MPDMObjectiveConquest.cs and
//                  MPDMObjectiveCTF.cs for examples on how to extend this class.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
#if PHOTON_UNITY_NETWORKING
    using Photon.Pun;
#endif
#if ANTICHEAT
    using CodeStage.AntiCheat.ObscuredTypes;
#endif
#if ULTIMATE_SEATING_CONTROLLER
    using FastSkillTeam.UltimateSeatingController;
#endif
    using System.Collections.Generic;
    using UnityEngine;
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Game;
    using Opsive.UltimateCharacterController.Utility;
    using FastSkillTeam.UltimateMultiplayer.Shared.Game;

    public class MPDMObjectiveBase :
#if PHOTON_UNITY_NETWORKING
    MonoBehaviourPun, IPunObservable
#else
    MonoBehaviour
#endif
    {
#if !ANTICHEAT
        [Tooltip("Child spawnpoints will have grouping set to this value and be active when it is owned by the local team. UI Buttons for spawning utilise this.")]
        [SerializeField] protected int m_SpawnGrouping = -1;
        [Tooltip("The defending team number (-1 for no team).")]
        [SerializeField] protected int m_DefendingTeamNumber = -1;
        // [Tooltip("If true, objective renderer color will be set to the defending teams color as per MPTeamManager.\nIf false the renderer will be blue for defenders and red for everyone else (potential attackers).\nBest left at default (false) for clarity.")]
        // [SerializeField] protected bool m_UseDefendingTeamColor = false;
        [Tooltip("The bonus score awarded to the team when the objective is completed.")]
        [SerializeField] protected int m_TeamScoreAmount = 100;
        [Tooltip("The bonus score awarded for defending while in the objectives bonus trigger.")]
        [SerializeField] protected int m_DefendScoreAmount = 10;
        [Tooltip("The bonus score awarded for attacking while in the objectives bonus trigger.")]
        [SerializeField] protected int m_AttackScoreAmount = 10;
        [Tooltip("The score awarded while capturing the objective.")]
        [SerializeField] protected int m_CapturingScoreAmount = 10;
        [Tooltip("The score awarded for a capture of the objective.")]
        [SerializeField] protected int m_CaptureScoreAmount = 50;
        [Tooltip("For moving root of objectives only. Should be false if object is never to move, or a child of another moving object (saves data).")]
        [SerializeField] protected bool m_SyncPosition;
        [Tooltip("For moving root of objectives only. Should be false if object is never to move, or a child of another moving object (saves data).")]
        [SerializeField] protected bool m_SyncRotation;
/*#if ULTIMATE_SEATING_CONTROLLER
        //TODO: Vehicle Driver/Gunner bonuses for priority seats
        [SerializeField] protected int m_DriverBonus = 10;  // applies to any "seat" of type "Driver", if you want it more specific, message me your request please.
        [SerializeField] protected int m_GunnerBonus = 5;   // applies to any and all "seats" with a "vehicle weapon", if you want it more specific, message me your request please.
#endif*/
#else
        [Tooltip("Child spawnpoints will have grouping set to this value and be active when it is owned by the local team. UI Buttons for spawning utilise this.")]
        [SerializeField] protected ObscuredInt m_SpawnGrouping = -1;
        [Tooltip("The defending team number (-1 for no team).")]
        [SerializeField] protected ObscuredInt m_DefendingTeamNumber = -1;
        // [Tooltip("If true, objective renderer color will be set to the defending teams color as per MPTeamManager.\nIf false the renderer will be blue for defenders and red for everyone else (potential attackers).\nBest left at default (false) for clarity.")]
        // [SerializeField] protected ObscuredBool m_UseDefendingTeamColor = false;
        [Tooltip("The bonus score awarded to the team when the objective is completed.")]
        [SerializeField] protected ObscuredInt m_TeamScoreAmount = 100;
        [Tooltip("The bonus score awarded for defending while in the objectives bonus trigger.")]
        [SerializeField] protected ObscuredInt m_DefendScoreAmount = 10;
        [Tooltip("The bonus score awarded for attacking while in the objectives bonus trigger.")]
        [SerializeField] protected ObscuredInt m_AttackScoreAmount = 10;
        [Tooltip("The score awarded while capturing the objective.")]
        [SerializeField] protected ObscuredInt m_CapturingScoreAmount = 10;
        [Tooltip("The score awarded for a capture of the objective.")]
        [SerializeField] protected ObscuredInt m_CaptureScoreAmount = 50;
        [Tooltip("For moving root of objectives only. Should be false if object is never to move, or a child of another moving object (saves data).")]
        [SerializeField] protected ObscuredBool m_SyncPosition;
        [Tooltip("For moving root of objectives only. Should be false if object is never to move, or a child of another moving object (saves data).")]
        [SerializeField] protected ObscuredBool m_SyncRotation;
#endif

        [Tooltip("The layer mask to check within for bonus scoring.")]
        [SerializeField] protected LayerMask m_LayerMask = 1 << 0;
        [Tooltip("The objective gameObject, defaults to self if null.")]
        [SerializeField] protected GameObject m_ObjectiveGameObject;
        [Tooltip("The objective gameObjects renderer, defaults to first in objective gameobjects children if null.")]
        [SerializeField] protected Renderer[] m_ObjectiveRenderers;
        [Tooltip("An array of MiniMapSceneObjects representing the objects to be displayed on the minimap.")]
        [SerializeField] protected MiniMap.MiniMapSceneObject[] m_MiniMapSceneObjects = new MiniMap.MiniMapSceneObject[0];

        public int DefendingTeamNumber { get => m_DefendingTeamNumber; set => m_DefendingTeamNumber = value; }

        protected Transform m_Transform;
        protected Transform m_ObjectiveTransform;
        protected Color m_CurrentColor = Color.white;

        //players in bonus zone
        protected List<MPPlayer> m_Players = new List<MPPlayer>(0);

        //reset values
        protected Vector3 m_OriginalPosition;
        protected Quaternion m_OriginalRotation;
        private readonly List<Transform> m_OriginalTransforms = new List<Transform>(0);

        // Lag compensation
        protected float m_CurrentTime = 0;
        protected double m_CurrentPacketTime = 0;
        protected double m_LastPacketTime = 0;
        private Vector3 m_PositionAtLastPacket = Vector3.zero;
        private Quaternion m_RotationAtLastPacket;

        // Values that will be synced over network
        private Vector3 m_LatestPos;
        private Quaternion m_LatestRot;

        private SpawnPoint[] m_SpawnPoints;


        private int m_LastDefendingTeam = -1;
        private bool m_ColorInitialized;
        public virtual void Awake()
        {
            m_Transform = transform;
            if (m_ObjectiveGameObject == null)
                m_ObjectiveGameObject = gameObject;

            m_ObjectiveTransform = m_ObjectiveGameObject.transform;

            if (m_ObjectiveRenderers == null || (m_ObjectiveRenderers != null && m_ObjectiveRenderers.Length == 0))
            {
                m_ObjectiveRenderers = new Renderer[1];
                m_ObjectiveRenderers[0] = m_ObjectiveGameObject.GetComponentInChildren<Renderer>(true);
            }

            m_OriginalPosition = m_ObjectiveTransform.position;
            m_OriginalRotation = m_ObjectiveTransform.rotation;

            Transform[] ts = m_ObjectiveTransform.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < ts.Length; i++)
                m_OriginalTransforms.Add(ts[i]);

            m_LatestPos = m_ObjectiveTransform.position;
            m_CurrentTime = 0.0f;
#if PHOTON_UNITY_NETWORKING
            m_CurrentPacketTime = PhotonNetwork.Time;
#endif
            m_LastPacketTime = m_CurrentPacketTime;
            m_PositionAtLastPacket = m_ObjectiveTransform.position;

            m_SpawnPoints = GetComponentsInChildren<SpawnPoint>(true);

            EventHandler.RegisterEvent("OnMatchEnd", OnMatchEnd);
            EventHandler.RegisterEvent("OnResetGame", FullReset);
        }
        protected bool m_Active = true;
        protected virtual void OnMatchEnd()
        {
            m_Active = false;
        }

        protected virtual void OnEnable()
        {
            EventHandler.RegisterEvent<Photon.Realtime.Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
        }

        protected virtual void OnDisable()
        { 
            // If object will destroy in the end of current frame...
            //if (gameObject.activeInHierarchy)
            //    Debug.LogError("Log an error with a stack trace in debug mode for OnDestroy");

            EventHandler.UnregisterEvent<Photon.Realtime.Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
        }

        protected virtual void OnPlayerEnteredRoom(Photon.Realtime.Player player, GameObject character)
        {

        }
        protected virtual void SetColor(Color color)
        {
            if(m_ColorInitialized == false)
            {
                if (m_ObjectiveRenderers == null || (m_ObjectiveRenderers != null && m_ObjectiveRenderers.Length == 0))
                {
                    m_ObjectiveRenderers = new Renderer[1];
                    m_ObjectiveRenderers[0] = m_ObjectiveGameObject.GetComponentInChildren<Renderer>(true);
                }

                if (m_MiniMapSceneObjects.Length < 1)
                    m_MiniMapSceneObjects = m_ObjectiveGameObject.GetComponentsInChildren<MiniMap.MiniMapSceneObject>(true);

                for (int i = 0; i < m_MiniMapSceneObjects.Length; i++)
                    m_MiniMapSceneObjects[i].Initialize(m_MiniMapSceneObjects[i].gameObject, -1, -1);
            }

            m_CurrentColor = color;

            for (int i = 0; i < m_ObjectiveRenderers.Length; i++)
            {
                if (m_ObjectiveRenderers[i] == null)
                    continue;
                m_ObjectiveRenderers[i].material.color = m_CurrentColor;
            }


            for (int i = 0; i < m_MiniMapSceneObjects.Length; i++)
                m_MiniMapSceneObjects[i].SetTeamOwner(m_DefendingTeamNumber);

            m_ColorInitialized = true;
        }

/*        protected virtual void SetColor()
        {
            if (m_ColorInitialized == false)
            {
                if (m_ObjectiveRenderers == null || (m_ObjectiveRenderers != null && m_ObjectiveRenderers.Length == 0))
                {
                    m_ObjectiveRenderers = new Renderer[1];
                    m_ObjectiveRenderers[0] = m_ObjectiveGameObject.GetComponentInChildren<Renderer>(true);
                }

                if (m_MiniMapSceneObjects.Length < 1)
                    m_MiniMapSceneObjects = m_ObjectiveGameObject.GetComponentsInChildren<MiniMap.MiniMapSceneObject>(true);

                for (int i = 0; i < m_MiniMapSceneObjects.Length; i++)
                    m_MiniMapSceneObjects[i].Initialize(m_MiniMapSceneObjects[i].gameObject, -1, -1);
            }

            m_CurrentColor = m_UseDefendingTeamColor ? MPTeamManager.GetTeamColor(m_DefendingTeamNumber) : (MPLocalPlayer.Instance.TeamNumber == m_DefendingTeamNumber ? Color.blue : Color.red);

            for (int i = 0; i < m_ObjectiveRenderers.Length; i++)
            {
                if (m_ObjectiveRenderers[i] == null)
                    continue;
                m_ObjectiveRenderers[i].material.color = m_CurrentColor;
            }


            for (int i = 0; i < m_MiniMapSceneObjects.Length; i++)
                m_MiniMapSceneObjects[i].SetTeamOwner(m_DefendingTeamNumber);

            m_ColorInitialized = true;
        }*/

        protected virtual void OnDestroy()
        {

            EventHandler.UnregisterEvent("OnMatchEnd", OnMatchEnd);
            EventHandler.UnregisterEvent("OnResetGame", FullReset);
            //Debug.Log(this.GetType().ToString() + " OnDestroy()");
        }

        public virtual void Update()
        {
            if (Gameplay.IsMaster)
            {
                for (int i = 0; i < m_Players.Count; i++)
                {
                    if(m_Players[i].GameObject == null)
                    {
                        RemoveBonus(m_Players[i]);
                        continue;
                    }
                    if (m_Players[i].PlayerHealth && m_Players[i].PlayerHealth.IsAlive() == false)
                        RemoveBonus(m_Players[i]);
                }
            }

            if (MPLocalPlayer.Instance != null)
            {
                if (m_LastDefendingTeam != m_DefendingTeamNumber)
                {
                    for (int i = 0; i < m_SpawnPoints.Length; i++)
                    {
                        if (m_DefendingTeamNumber != MPLocalPlayer.Instance.TeamNumber)
                        {
                            m_SpawnPoints[i].Grouping = -1;
                            m_SpawnPoints[i].gameObject.SetActive(false);
                            continue;
                        }
                        m_SpawnPoints[i].gameObject.SetActive(true);
                        m_SpawnPoints[i].Grouping = m_SpawnGrouping;
                    }
                    m_LastDefendingTeam = m_DefendingTeamNumber;
                }
            }

            if (m_SyncPosition == false && m_SyncRotation == false)
                return;

            if (Gameplay.IsMaster == false)
            {
                // Lag compensation
                double timeToReachGoal = m_CurrentPacketTime - m_LastPacketTime;
                m_CurrentTime += Time.deltaTime;

                float interp = (float)(m_CurrentTime / timeToReachGoal);

                if (m_SyncPosition)
                    // Update position
                    m_Transform.position = Vector3.Lerp(m_PositionAtLastPacket, m_LatestPos, interp);

                if (m_SyncRotation)
                    m_Transform.rotation = Quaternion.Slerp(m_RotationAtLastPacket, m_LatestRot, interp);

                return;
            }
            else
            {
                if (m_SyncPosition)
                    m_LatestPos = m_Transform.position;

                if (m_SyncRotation)
                    m_LatestRot = m_Transform.rotation;
            }
        }

        public virtual void OnTriggerEnter(Collider other)
        {
            if (Gameplay.IsMaster == false)
                return;

            if (!MathUtility.InLayerMask(other.gameObject.layer, m_LayerMask))
                return;

#if ULTIMATE_SEATING_CONTROLLER
            BoardSource boardSource = other.transform.root.GetComponentInChildren<BoardSource>();

            if (boardSource != null)
            {
                MPPlayer[] players = boardSource.GetComponentsInChildren<MPPlayer>();
                if (players == null || players.Length == 0)
                    return;
                for (int i = 0; i < players.Length; i++)
                    AddBonus(players[i]);

                return;
            }
#endif

            MPPlayer p = other.transform.root.GetComponent<MPPlayer>();
            if (p == null)
                return;

            AddBonus(p);
        }

        public virtual void OnTriggerExit(Collider other)
        {
            if (Gameplay.IsMaster == false)
                return;

            if (!MathUtility.InLayerMask(other.gameObject.layer, m_LayerMask))
                return;

#if ULTIMATE_SEATING_CONTROLLER
            BoardSource boardSource = other.transform.root.GetComponentInChildren<BoardSource>();

            if (boardSource != null)
            {
                MPPlayer[] players = boardSource.GetComponentsInChildren<MPPlayer>();
                if (players == null || players.Length == 0)
                    return;
                for (int i = 0; i < players.Length; i++)
                    RemoveBonus(players[i]);

                return;
            }
#endif

            MPPlayer p = other.transform.root.GetComponent<MPPlayer>();
            if (p == null)
                return;
            RemoveBonus(p);
        }

        protected void AddBonus(MPPlayer player)
        {
            if (m_Players.Contains(player))
                return;

            if (player.PlayerHealth.IsAlive() == false)
                return;

            m_Players.Add(player);
#if PHOTON_UNITY_NETWORKING
            photonView.RPC("AddBonusRPC", RpcTarget.All, player.ID);
#endif
        }
        protected void RemoveBonus(MPPlayer player)
        {
            if (!m_Players.Contains(player))
                return;

            m_Players.Remove(player);
#if PHOTON_UNITY_NETWORKING
            if (player.GameObject != null)
                photonView.RPC("RemoveBonusRPC", RpcTarget.All, player.ID);
#endif
        }
#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        protected virtual void AddBonusRPC(int playerID)
        {
            MPPlayer p = MPPlayer.Get(playerID);
            if (p == null)
                return;
            //TODO +-m_DefendScoreAmount || m_AttackScoreAmount with m_DriverBonus || m_GunnerBonus if USC is defined.
            p.Stats.Set("BonusScore", (int)p.Stats.Get("BonusScore") + ((m_DefendingTeamNumber == -1 || p.TeamNumber == m_DefendingTeamNumber) ? m_DefendScoreAmount : m_AttackScoreAmount));
        }
#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        protected virtual void RemoveBonusRPC(int playerID)
        {
            MPPlayer p = MPPlayer.Get(playerID);
            if (p == null)
                return;
            //TODO +-m_DefendScoreAmount || m_AttackScoreAmount with m_DriverBonus || m_GunnerBonus if USC is defined.
            p.Stats.Set("BonusScore", (int)p.Stats.Get("BonusScore") - ((m_DefendingTeamNumber == -1 || p.TeamNumber == m_DefendingTeamNumber) ? m_DefendScoreAmount : m_AttackScoreAmount));
        }
#if PHOTON_UNITY_NETWORKING
        public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                if (m_SyncPosition)
                    stream.SendNext(m_Transform.position);

                if (m_SyncRotation)
                    stream.SendNext(m_Transform.rotation);

            }
            else
            {
                // Network player, receive data
                if (m_SyncPosition)
                {
                    m_PositionAtLastPacket = m_Transform.position;
                    m_LatestPos = (Vector3)stream.ReceiveNext();
                }
                if (m_SyncRotation)
                {
                    m_RotationAtLastPacket = m_Transform.rotation;
                    m_LatestRot = (Quaternion)stream.ReceiveNext();
                }
                // Lag compensation
                m_CurrentTime = 0.0f;
                m_LastPacketTime = m_CurrentPacketTime;
                m_CurrentPacketTime = info.SentServerTime;
            }
        }
#endif
        public virtual void FullReset()
        {
            m_ObjectiveTransform.position = m_OriginalPosition;
            m_ObjectiveTransform.rotation = m_OriginalRotation;

            Transform[] ts = m_ObjectiveTransform.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < ts.Length; i++)
            {
                if (m_OriginalTransforms.Contains(ts[i]))
                    continue;

                if (ObjectPoolBase.InstantiatedWithPool(ts[i].gameObject))
                    ObjectPoolBase.Destroy(ts[i].gameObject);
                else Object.Destroy(ts[i].gameObject);
            }

            m_Active = true;
        }
    }
}