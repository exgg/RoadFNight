/////////////////////////////////////////////////////////////////////////////////
//
//  MPDMObjectiveCTF.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	An example of how to extend MPDMObjectiveBase.cs for Capture
//                  The Flag/Capture the Objective style gameplay.
//
/////////////////////////////////////////////////////////////////////////////////

using Photon.Realtime;

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
#if PHOTON_UNITY_NETWORKING
    using Photon.Realtime;
    using Photon.Pun;
#endif
#if ANTICHEAT
    using CodeStage.AntiCheat.ObscuredTypes;
#endif
    using Opsive.Shared.Events;
    using System.Collections.Generic;
    using UnityEngine;
    using FastSkillTeam.UltimateMultiplayer.Shared;

    public class MPDMObjectiveCTF : MPDMObjectiveBase
    {
#if !ANTICHEAT
        [Tooltip("This flag needs to be captured this many times for a round to be won.")]
        [SerializeField] protected int m_CapturesToWin = 3;
        [Tooltip("Should the flag return to its start position if the carrier is killed? If true the object will be respawned, if false the object will be dropped at the kill position.")]
        [SerializeField] protected bool m_RespawnOnCarrierDeath = false;

        [Tooltip("If true, objective renderer color will be set to the defending teams color as per MPTeamManager.\nIf false the renderer will be blue for defenders and red for everyone else (potential attackers).\nBest left at default (true) for clarity for CTF with multiple flags.")]
        [SerializeField] protected bool m_UseDefendingTeamColor = true;
#else
        [Tooltip("This flag needs to be captured this many times for a round to be won.")]
        [SerializeField] protected ObscuredInt m_CapturesToWin = 3;

        [SerializeField] protected ObscuredBool m_RespawnOnCarrierDeath = false;

        [Tooltip("If true, objective renderer color will be set to the defending teams color as per MPTeamManager.\nIf false the renderer will be blue for defenders and red for everyone else (potential attackers).\nBest left at default (true) for clarity for CTF with multiple flags.")]
        [SerializeField] protected ObscuredBool m_UseDefendingTeamColor = true;
#endif
        [Tooltip("The collider that will trigger the pickup.")]
        [SerializeField] protected Collider m_PickupCollider;
        [Tooltip("The bonus zone collider if any.")]
        [SerializeField] protected Collider m_BonusCollider;

        private readonly Dictionary<int, int> m_CaptureCounts = new Dictionary<int, int>();//Count of captures for each team.
        //  public Transform[] m_SpawnPoints;
        private int m_TeamId;
        private Transform m_CarryPosition = null;
        private bool m_IsCarried = false;

        private Rigidbody m_Rigidbody;
        private MPPlayer m_Owner;

        public override void Awake()
        {
            if (MPMaster.Instance is MPDMMaster)
            {
                if ((MPMaster.Instance as MPDMMaster).CurrentGameType != GameType.CTF)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            base.Awake();

            m_SyncPosition = m_SyncRotation = true;
        }

        private void Start()
        {
            if (m_PickupCollider == null)
                m_PickupCollider = GetComponent<Collider>();

            m_PickupCollider.enabled = true;

            if (m_BonusCollider)
                m_BonusCollider.enabled = false;
            m_Rigidbody = m_ObjectiveTransform.GetComponent<Rigidbody>();
           base.SetColor(Color.white);
        }

        public void Pickup(MPPlayer owner)
        {
#if PHOTON_UNITY_NETWORKING
            photonView.RPC("PickUpFlagRPC", RpcTarget.Others, owner.ID);
#endif
            PickUpInternal(owner);
        }

        private void PickUpInternal(MPPlayer owner)
        {
            m_Owner = owner;
            
            EventHandler.RegisterEvent<Player, GameObject>("OnPlayerLeftRoom", OnPlayerLeftRoom);

            m_Owner.Stats.Set("Score", (int)m_Owner.Stats.Get("Score") + m_CapturingScoreAmount);

            m_IsCarried = true;
            m_PickupCollider.enabled = false;
            if (m_BonusCollider)
                m_BonusCollider.enabled = true;
            m_Rigidbody.isKinematic = true;
            m_ObjectiveTransform.SetParent(m_Owner.Transform);
            MPCTFAbility flagAbility = m_Owner.GetUltimateCharacterLocomotion.GetAbility<MPCTFAbility>();
            m_CarryPosition = flagAbility.CarryPosition;
            m_ObjectiveTransform.SetPositionAndRotation(m_CarryPosition.position, m_CarryPosition.rotation);
            m_TeamId = m_Owner.TeamNumber;
            m_DefendingTeamNumber = m_TeamId;
            base.SetColor(m_UseDefendingTeamColor ? MPTeamManager.GetTeamColor(m_DefendingTeamNumber) : (MPLocalPlayer.Instance.TeamNumber == m_DefendingTeamNumber ? Color.blue : Color.red));

            m_SyncPosition = m_SyncRotation = false;

            MPTeamManager.Instance.RefreshTeams();
        }
#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        private void PickUpFlagRPC(int ownerActorNumber)
        {
            m_Owner = MPPlayer.Get(ownerActorNumber);
            if (m_Owner == null)
                return;
            PickUpInternal(m_Owner);
        }

        private void OnPlayerLeftRoom(Player player, GameObject character)
        {
            if (!m_IsCarried)
                return;
          //  Debug.Log("Flag carrier left the game");
            if (m_Owner.GameObject == character)
            {
                if (!player.IsLocal)//local has quit, prevent dangling objects. Only drop on remote.
                    Drop(null);
            }
        }

        public void Drop(GameObject attacker)
        {
            if (attacker)
            {
                MPPlayer p = MPPlayer.Get(attacker.transform);
#if PHOTON_UNITY_NETWORKING
                if (p)
                    photonView.RPC("DropRPC", RpcTarget.Others, p.ID);
#endif
            }

            DropInternal();
        }

        private void DropInternal()
        {
            EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerLeftRoom", OnPlayerLeftRoom);
            m_ObjectiveTransform.SetParent(null);

            if(m_RespawnOnCarrierDeath == true)
            {   
                // Respawn the flag
                m_ObjectiveTransform.position = m_OriginalPosition;
                if (m_Rigidbody)
                {
                    m_Rigidbody.position = m_ObjectiveTransform.position;
                    m_Rigidbody.ResetInertiaTensor();
                    m_Rigidbody.velocity = m_Rigidbody.angularVelocity = Vector3.zero;
                }
            }
            m_IsCarried = false;
            m_PickupCollider.enabled = true;
            if (m_BonusCollider)
                m_BonusCollider.enabled = false;
            m_Rigidbody.isKinematic = false;
            m_CarryPosition = null;

            m_DefendingTeamNumber = -1;
            base.SetColor(Color.white);

            m_SyncPosition = m_SyncRotation = true;
        }

#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        private void DropRPC(int playerID)
        {
            MPPlayer p = MPPlayer.Get(playerID);
            if (p)
            {
                Debug.Log("Attacker is " + MPPlayer.GetName(p.ID));
                p.Stats.Set("Score", (int)p.Stats.Get("Score") + m_AttackScoreAmount);
                MPTeamManager.Instance.RefreshTeams();
            }
            else Debug.LogWarning("Attacker was null! No score will be awarded to player.");

            DropInternal();
        }

        public void CaptureFlag()
        {
            if (!m_IsCarried)
                return;
#if PHOTON_UNITY_NETWORKING
            photonView.RPC("CaptureFlagRPC", RpcTarget.All);
#endif
        }

#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        public void CaptureFlagRPC()
        {
            if (m_Owner != null)
                m_Owner.Stats.Set("Score", (int)m_Owner.Stats.Get("Score") + m_CaptureScoreAmount);
            else Debug.LogWarning("Owner was null upon flag capture! No score will be awarded to player.");

            // Respawn the flag
            m_ObjectiveTransform.position = m_OriginalPosition;
            //  m_ObjectiveTransform.position = m_SpawnPoints(teamId);
            Drop(null);

            // Award points to the capturing team
            if (m_CaptureCounts.ContainsKey(m_TeamId))
                m_CaptureCounts[m_TeamId]++;
            else m_CaptureCounts.Add(m_TeamId, 1);

            MPTeamManager.Instance.RefreshTeams();

            if (m_CaptureCounts[m_TeamId] >= m_CapturesToWin)
            {
                Debug.Log("Capture count reached");
                MPMaster.Instance.StopGame();
                m_CaptureCounts.Clear();
                return;
            }
        }

        /// <summary>
        /// When the round resets, reset the objective.
        /// </summary>
        public override void FullReset()
        {
            if (m_IsCarried)
                Drop(null);

            base.FullReset();

            if(m_Rigidbody)
            {
                m_Rigidbody.position = m_ObjectiveTransform.position;
                m_Rigidbody.ResetInertiaTensor();
                m_Rigidbody.velocity = m_Rigidbody.angularVelocity = Vector3.zero;
            }
            m_CaptureCounts.Clear();
            m_IsCarried = false;
        }
    }
}