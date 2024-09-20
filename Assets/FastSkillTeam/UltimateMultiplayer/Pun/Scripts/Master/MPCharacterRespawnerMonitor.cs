/////////////////////////////////////////////////////////////////////////////////
//
//  MPCharacterRespawnerMonitor.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	Add to a character with a Respawner to sync respawns online.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Networking.Traits;
    using UnityEngine;
#if PHOTON_UNITY_NETWORKING
    using Photon.Pun;
    [RequireComponent(typeof(PhotonView))]
#endif
    public class MPCharacterRespawnerMonitor : MonoBehaviour, INetworkRespawnerMonitor
    {
#if PHOTON_UNITY_NETWORKING
        private PhotonView m_PhotonView;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        private void Awake()
        {
            m_PhotonView = gameObject.GetCachedComponent<PhotonView>();
        }
#endif
        /// <summary>
        /// Does the respawn by setting the position and rotation to the specified values.
        /// Enable the GameObject and let all of the listening objects know that the object has been respawned.
        /// </summary>
        /// <param name="position">The respawn position.</param>
        /// <param name="rotation">The respawn rotation.</param>
        /// <param name="transformChange">Was the position or rotation changed?</param>
        public void Respawn(Vector3 position, Quaternion rotation, bool transformChange)
        {
#if PHOTON_UNITY_NETWORKING
            m_PhotonView.RPC("RespawnRPC", RpcTarget.Others, position, rotation, transformChange);
#endif
        }
    }
}