/////////////////////////////////////////////////////////////////////////////////
//
//  MPRemotePlayer.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	This class represents a remote player in multiplayer. it extends
//					MPPlayer with all the functionality specific to remote
//					controlled player gameobjects in the scene.
//					there can be an arbitrary number of remote player objects in a scene.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
#if PHOTON_UNITY_NETWORKING
    using Photon.Pun;
#endif
    using UnityEngine;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Game;
    using FastSkillTeam.UltimateMultiplayer.Shared.Game;

    public class MPRemotePlayer : MPPlayer
    {
        // nametag
        protected UI.MPNameTag m_NameTag = null;
        public UI.MPNameTag NameTag
        {
            get
            {
                if (m_NameTag == null)
                    m_NameTag = transform.GetComponentInChildren<UI.MPNameTag>(true);
                return m_NameTag;
            }
        }

        /// <summary>
        /// Refreshes common components along with components that are specific to the remote player.
        /// The process is delayed if a MPLocalPlayer is not yet present in the scene.
        /// </summary>
        protected override void RefreshComponents()
        {
            base.RefreshComponents();

            if (MPLocalPlayer.Instance == null)
            {
                m_SafetyCheckCount = 0;
                //Unlikely at this point, but to be sure...
                Debug.LogWarning("No MPLocalPlayer.Instance could not be found in the scene. Will try again up to 25 times at 0.25s intervals.");
                Scheduler.Schedule(0.25f, RefreshComponentsInternal);
                return;
            }
            
            RefreshComponentsInternal();
        }

        private int m_SafetyCheckCount = 0;
        private void RefreshComponentsInternal()
        {
            if (MPLocalPlayer.Instance == null)
            {
                //Unlikely at this point, but to be sure...
                if (m_SafetyCheckCount < 25)
                {
                    m_SafetyCheckCount++;
                    Scheduler.Schedule(0.25f, RefreshComponentsInternal);
                }
                else Debug.LogError("No MPLocalPlayer.Instance could not be found in the scene. Tried 25 times at 0.25s intervals.");
                return;
            }

            // refresh nametag team color
            if (NameTag != null)
                m_NameTag.SetColor(TeamNumber);

            // set enemy layer for remotes that are not friendly
            if (TeamNumber != MPLocalPlayer.Instance.TeamNumber)
                m_GameObject.layer = LayerManager.Enemy;
        }

        /// <summary>
        /// this is called from the base class and used to prevent lerp
        /// movement upon teleport. local player does not override it
        /// </summary>
        public override void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            //NOTE: Not currently required.
            //GetUltimateCharacterLocomotion.SetPositionAndRotation(position, rotation, false, false, false);
        }


        /// <summary>
        /// this is called from the base class and used to prevent lerp
        /// rotation upon teleport. local player does not override it
        /// </summary>
        public override void SetRotation(Quaternion rotation)
        {

            //NOTE: Not currently required.

            //   if (GetUltimateCharacterLocomotion == null)
            //         return;

            //  GetUltimateCharacterLocomotion.SetRotation(rotation);


        }


        /// <summary>
        /// this is called from the base class and used to prevent lerp
        /// movement upon teleport. local player does not override it
        /// </summary>
        public override void SetPosition(Vector3 position)
        {
            //NOTE: Not currently required.

            // if (GetUltimateCharacterLocomotion.MovingPlatform != null)
            //   position = Vector3.zero;

            // Transform.position = position;
        }


        //TODO: Convert below to events, saving data sends!

        /// <summary>
        /// when this RPC arrives the player will die immediately because the
        /// master client says so. since this is a remote player, the nametag
        /// will fade out in one sec
        /// </summary>
#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        public override void ReceivePlayerKill(Vector3 position, Vector3 force, int attackerViewID, PhotonMessageInfo info)
        {

            //Debug.Log(this + "ReceivePlayerKill");

            if (info.Sender != PhotonNetwork.MasterClient)
                return;

            if (NameTag != null)
            {
                Scheduler.Schedule(1, () =>
                {
                    if ((this != null) && (NameTag != null))
                        NameTag.Visible = false;
                });
            }

            base.ReceivePlayerKill(position, force, attackerViewID, info);

        }

        /// <summary>
        /// when this RPC arrives the player will be instantly teleported
        /// to the position dictated by the master client and have its
        /// nametag instantly but temporarily made invisible
        /// </summary>
#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        public override void RespawnRPC(Vector3 position, Quaternion rotation, bool transformChange, PhotonMessageInfo info)
        {
            base.RespawnRPC(position, rotation, transformChange, info);
            // if this is a remote player, make our nametag temporarily invisible on
            // respawn (or we might reveal our respawn position) then fade back in
            if (NameTag != null)
            {
                NameTag.Alpha = 0.0f;   // snap to invisible
                NameTag.Visible = true; // fade back in
            }
            Respawner.Respawn(position, rotation, transformChange);
        }

        [PunRPC]
        private void ReceiveLocalDamageRPC(float damageAmount, int attackerID)
        {
            if (!Gameplay.IsMaster)
                return;
            Transform attacker = MPMaster.GetTransformOfViewID(attackerID);
            base.PlayerHealth.Damage(damageAmount, m_Transform.position, Vector3.zero, 0, attacker ? attacker.gameObject : m_GameObject);
        }
    }
}