/////////////////////////////////////////////////////////////////////////////////
//
//  MPCTFAbility.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	This ability allows the character to pickup and capture CTF
//                  flags/objectives.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using FastSkillTeam.UltimateMultiplayer.Shared;
    using Opsive.Shared.Events;
    using Opsive.UltimateCharacterController.Character.Abilities;
    using UnityEngine;
    
    [DefaultStartType(AbilityStartType.Automatic)]
    [DefaultStopType(AbilityStopType.Manual)]
    [Opsive.Shared.Utility.Group("Ultimate Multiplayer")]
    public class MPCTFAbility : Ability
    {
        [Tooltip("The carry position the objective will be placed at when it is picked up.")]
        [SerializeField] protected Transform m_CarryPosition;
        [Tooltip("The tag of the gameobject with a trigger collider that is a capture zone for the pickup. The objective is completed when the pickup is brought to the capture zone.")]
        [SerializeField] protected string m_CaptureZoneTag = "CaptureZone";
        [Tooltip("The tag of the gameobject with a trigger collider that is the pickup.")]
        [SerializeField] protected string m_PickupTag = "Flag";

        private MPDMObjectiveCTF m_CarryingFlag;
        public MPDMObjectiveCTF CarryingFlag { get => m_CarryingFlag; set => m_CarryingFlag = value; }
        //lazy init for MPPlayer as it is added at runtime when character is instantiated
        private MPPlayer m_NetworkPlayer = null;
        public MPPlayer NetworkPlayer { get { if (m_NetworkPlayer == null) m_NetworkPlayer = m_GameObject.GetComponent<MPPlayer>(); return m_NetworkPlayer; } }
        public Transform CarryPosition => m_CarryPosition;
        public override bool IsConcurrent => true;
        public override bool CanStayActivatedOnDeath => true;

        public override void Awake()
        {
            base.Awake();

            if (MPMaster.Instance is MPDMMaster)
            {
                if ((MPMaster.Instance as MPDMMaster).CurrentGameType != GameType.CTF)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            if (m_CarryPosition == null)
            {
                m_CarryPosition = new GameObject("FlagCarryPosition").transform;
                m_CarryPosition.SetParent(m_Transform);
                m_CarryPosition.position = m_Transform.position + (Vector3.up * 3f);
                m_CarryPosition.rotation = m_Transform.rotation;
            }

            EventHandler.RegisterEvent("OnResetGame", OnResetGame);
        //    EventHandler.RegisterEvent<string, string, string>("ReportMPKill", ReportKill);
            EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(m_GameObject, "OnDeath", OnDeath);

            m_NetworkPlayer = m_GameObject.GetComponent<MPPlayer>();

            m_CharacterLocomotion.TryStartAbility(this, true, true);
        }

        private void ReportKill(string attacker, string attackerWeapon, string target)
        {
            if(attacker == MPPlayer.GetName(NetworkPlayer.ID))
            {
                //TODO: Bonus for kills while carrying, or for killing the carrier
                Debug.Log("Scored A KILL");
            }
        }

        public override void OnDestroy()
        {
        //    EventHandler.UnregisterEvent<string, string, string>("ReportMPKill", ReportKill);
            EventHandler.UnregisterEvent("OnResetGame", OnResetGame);
            EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(m_GameObject, "OnDeath", OnDeath);
            base.OnDestroy();
        }
        private void OnResetGame()
        {
            if (m_CarryingFlag == null)
                return;

            m_CarryingFlag.Drop(null);
            m_CarryingFlag = null;
        }
        private void OnDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            if (m_CarryingFlag == null)
                return;
            Debug.Log("KILLED BY : " + attacker);
            m_CarryingFlag.Drop(attacker);
            m_CarryingFlag = null;
        }

        public override bool CanStartAbility()
        {
            return true;
        }
        public override bool CanStopAbility(bool force)
        {
            return false;
        }
        public override void OnTriggerEnter(Collider other)
        {
            if(NetworkPlayer == null)
            {
                return;
            }

            if (NetworkPlayer.PlayerHealth.IsAlive() == false)
                return;

            if (other.CompareTag(m_CaptureZoneTag) && m_CarryingFlag)
            {
                MPDMCaptureZoneCTF zone = other.GetComponent<MPDMCaptureZoneCTF>();
                if (zone == null)
                    return;
                if (m_CarryingFlag && zone.TeamId != NetworkPlayer.TeamNumber)
                {
                    Debug.Log("wrong team!");
                    //    CaptureFlag(zone.TeamId);//??Make friendly capture optional??
                    return;
                }
                Debug.Log(m_GameObject.name + "captured the flag");
                m_CarryingFlag.CaptureFlag();
                m_CarryingFlag = null;
            }
            else if (other.CompareTag(m_PickupTag) && other.transform.parent == null && !m_CarryingFlag)
            {
                Debug.Log(m_GameObject.name + "picked up the flag!");
                m_CarryingFlag = other.GetComponent<MPDMObjectiveCTF>();
                if (m_CarryingFlag != null)
                    m_CarryingFlag.Pickup(NetworkPlayer);
            }
        }
    }
}