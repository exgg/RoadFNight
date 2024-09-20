/////////////////////////////////////////////////////////////////////////////////
//
//  MPDamageCallbacks.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	Base class manager that responds to damage, kills and respawns on the
//					current master client by syncing the results to all other clients.
//					without this script 'there can be no death' for non-master clients.
//					override this script to create more complex responses.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using Photon.Pun;
    using Opsive.UltimateCharacterController.Traits;
    using Opsive.Shared.Events;
    using Opsive.UltimateCharacterController.Traits.Damage;
    using Opsive.Shared.Game;

    public class MPDamageCallbacks : MonoBehaviourPun
	{
        protected static Dictionary<int, MPHealth> m_DamageHandlersByViewID = new Dictionary<int, MPHealth>();
		protected static Dictionary<int, Respawner> m_RespawnersByViewID = new Dictionary<int, Respawner>();
		[Tooltip("Set this to true to always keep non-player Health/IDamageTargets in perfect sync on all machines (not necessarily needed unless true pro play sync is required, since master will force-kill things that die in its scene anyway)")]
		[SerializeField] protected bool m_SyncPropHealth = false;
		/*		private GameObject m_GameObject;
				private PhotonView m_PhotonView;

				private void Awake()
				{
					m_GameObject = gameObject;
					m_PhotonView = photonView;
				}*/
		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnEnable()
		{
			SceneManager.sceneLoaded += OnLevelLoad;
			EventHandler.RegisterEvent<Transform, DamageData>("TransmitDamage", TransmitDamage);
			EventHandler.RegisterEvent<Transform>("TransmitHeal", TransmitHeal);
			EventHandler.RegisterEvent<Transform, Vector3, Vector3, GameObject>("TransmitKill", TransmitKill);
			EventHandler.RegisterEvent<Transform, Vector3, Quaternion, bool>("TransmitRespawn", TransmitRespawn);
		}


		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnDisable()
		{
			SceneManager.sceneLoaded -= OnLevelLoad;
			EventHandler.UnregisterEvent<Transform, DamageData>("TransmitDamage", TransmitDamage);
			EventHandler.UnregisterEvent<Transform>("TransmitHeal", TransmitHeal);
			EventHandler.UnregisterEvent<Transform, Vector3, Vector3, GameObject>("TransmitKill", TransmitKill);
			EventHandler.UnregisterEvent<Transform, Vector3, Quaternion, bool>("TransmitRespawn", TransmitRespawn);
		}

		/// <summary>
		/// The object has been damaged.
		/// </summary>
		/// <param name="damageData">The data of the damage source.</param>
		protected virtual void TransmitDamage(Transform targetTransform, DamageData damageData)
		{       
			// NOTES:
			// 1) players (CharacterHealth) will have health synced perfectly across
			//		all machines at all times
			// 2) health of plain Health (props) is only kept in perfect sync if
			//		'SyncPropHealth' is true. however, when their health reaches zero on the
			//		master and a 'TransmitKill' message occurs the prop in question will
			//		always die immediately on all machines
			// 3) 'sourceTransform' is not used here, but needed for overrides that deal
			//		with more complex gameplay (see example in 'MPDMDamageCallbacks')
			// 4) 'damage' is assumed to have already been updated in the master scene
			//		damage handler. it is not used here, but overrides can do a lot more with
			//		it (see example in 'MPDMDamageCallbacks').
			// 5) 'damage' can be both positive and negative. a negative number will add health

			if (!PhotonNetwork.IsMasterClient)
				return;

			int viewID = MPMaster.GetViewIDOfTransform(targetTransform);
			if (viewID == 0)
				return;

			MPHealth d = GetDamageHandlerOfViewID(viewID);
			if (d == null)
				return;

			// abort if target already died (no health to update)
			if (d.HealthValue <= 0.0f)
				return;

			// abort if this is a prop and we're not supposed to sync prop health
			if (!m_SyncPropHealth && !(d is MPCharacterHealth))
				return;

			//	MPDebug.Log("TRANSMIT OBJECT HEALTH!");
			//	Debug.Log("TRANSMIT OBJECT HEALTH!");
			photonView.RPC("ReceiveObjectHealth", RpcTarget.Others, viewID, (float)d.HealthValue);    // NOTE: cast to float required for Anti-Cheat Toolkit support
		}

		protected virtual void TransmitHeal(Transform targetTransform)
		{       
			// NOTES:
			// 1) players (CharacterHealth) will have health synced perfectly across
			//		all machines at all times
			// 2) health of plain Health (props) is only kept in perfect sync if
			//		'SyncPropHealth' is true. however, when their health reaches zero on the
			//		master and a 'TransmitKill' message occurs the prop in question will
			//		always die immediately on all machines
			// 3) 'sourceTransform' is not used here, but needed for overrides that deal
			//		with more complex gameplay (see example in 'MPDMDamageCallbacks')
			// 4) 'damage' is assumed to have already been updated in the master scene
			//		damage handler. it is not used here, but overrides can do a lot more with
			//		it (see example in 'MPDMDamageCallbacks').
			// 5) 'damage' can be both positive and negative. a negative number will add health

			if (!PhotonNetwork.IsMasterClient)
				return;

			int viewID = MPMaster.GetViewIDOfTransform(targetTransform);
			if (viewID == 0)
				return;

			MPHealth d = GetDamageHandlerOfViewID(viewID);
			if (d == null)
				return;

			// abort if target already died (no health to update)
			if (d.HealthValue <= 0.0f)
				return;

			// abort if this is a prop and we're not supposed to sync prop health
			if (!m_SyncPropHealth && !(d is MPCharacterHealth))
				return;

			//	MPDebug.Log("TRANSMIT OBJECT HEALTH!");
			//	Debug.Log("TRANSMIT OBJECT HEALTH!");
			photonView.RPC("ReceiveObjectHealth", RpcTarget.Others, viewID, (float)d.HealthValue);    // NOTE: cast to float required for Anti-Cheat Toolkit support
		}

		/// <summary>
		/// updates the damagehandler of a corresponding photonview id with a
		/// a new health value. sent by the master to keep client damagehandlers
		/// in sync
		/// </summary>
		[PunRPC]
		protected virtual void ReceiveObjectHealth(int viewID, float health, PhotonMessageInfo info)
		{

			//MPDebug.Log("RECEIVE OBJECT HEALTH!");
			//Debug.Log("RECEIVE OBJECT HEALTH!");
			if ((info.Sender != PhotonNetwork.MasterClient) ||
				(info.Sender.IsLocal))
				return;

			MPHealth d = GetDamageHandlerOfViewID(viewID);
			if (d == null)
				return;

			d.HealthAttribute.Value = health;

		}

		/// <summary>
		/// this method responds to a 'TransmitKill' Global Event raised by an
		/// object in the master scene and sends out an RPC to enforce the kill
		/// on remote machines. it is typically sent out by MPCharacterHealth
		/// or MPHealth
		/// </summary>
		protected virtual void TransmitKill(Transform targetTransform, Vector3 position, Vector3 force, GameObject attacker)
		{

			if (!PhotonNetwork.IsMasterClient)
				return;
			// An attacker is not required. If one exists it must have a PhotonView component attached for identification purposes.
			var attackerPhotonViewID = -1;
			if (attacker != null)
			{
				var attackerPhotonView = attacker.GetCachedComponent<PhotonView>();
				if (attackerPhotonView == null)
				{
					Debug.LogError($"Error: The attacker {attacker.name} must have a PhotonView component.");
					return;
				}
				attackerPhotonViewID = attackerPhotonView.ViewID;
			}
			// --- killing a PLAYER ---
			MPPlayer player = MPPlayer.Get(targetTransform);
			if (player != null)
			{

				// TIP: local player could be forced to drop its current weapon as a pickup here
				// however dropping items is not yet supported
				//   Debug.Log("local player could be forced to drop its current weapon/kit as a pickup here");
				player.photonView.RPC("ReceivePlayerKill", RpcTarget.All, position, force, attackerPhotonViewID);
				return;
			}

			// --- killing an OBJECT ---
			int viewID = MPMaster.GetViewIDOfTransform(targetTransform);
			if (viewID > 0)
			{
				//  MPDebug.Log("TRANSMIT OBJECT KILL!");
				// send RPC with kill command to photonView of this script on all clients
				photonView.RPC("ReceiveObjectKill", RpcTarget.Others, viewID, position, force, attackerPhotonViewID);
			}

		}


		/// <summary>
		/// this method responds to a 'TransmitRespawn' event raised by an object in the
		/// master scene, and sends out an RPC to trigger the respawn on remote machines.
		/// it is typically initiated by MPHealth.
		/// </summary>
		protected virtual void TransmitRespawn(Transform targetTransform, Vector3 position, Quaternion rotation, bool transformChange)
		{
			// --- respawning a PLAYER ---
			if (MPPlayer.Get(targetTransform) != null)
			{
				Debug.Log("MPDamageCallbacks TransmitRespawn()");
				MPPlayer.Get(targetTransform).Respawner.Respawn(position, rotation, transformChange);
				//MPPlayerSpawner.TransmitPlayerRespawn(targetTransform, position, rotation, transformChange);
				return;
			}

			// --- respawning an OBJECT ---
			int viewID = MPMaster.GetViewIDOfTransform(targetTransform);
			if (viewID > 0)
			{
				photonView.RPC("ReceiveObjectRespawn", RpcTarget.Others, viewID, position, rotation);
			}

		}


		/// <summary>
		/// 
		/// </summary>
		[PunRPC]
		public virtual void ReceiveObjectKill(int viewId, Vector3 position, Vector3 force, int attackerViewID, PhotonMessageInfo info)
		{
			// MPDebug.Log("RECIEVE OBJECT KILL!");
			if (info.Sender != PhotonNetwork.MasterClient)
				return;

			MPHealth d = GetDamageHandlerOfViewID(viewId);
			if ((d == null) || (d is MPCharacterHealth))
				return;

			// cache respawner before we deactivate the object, or 'GetRespawnerOfViewID'
			// won't be able to find the deactivated object later
			GetRespawnerOfViewID(viewId);

			Transform attacker = MPMaster.GetTransformOfViewID(attackerViewID);

			d.Die(position, force, attacker ? attacker.gameObject : null);

		}


		/// <summary>
		/// 
		/// </summary>
		[PunRPC]
		public virtual void ReceiveObjectRespawn(int viewId, Vector3 position, Quaternion rotation, PhotonMessageInfo info)
		{

			if (info.Sender != PhotonNetwork.MasterClient)
				return;

			Respawner r = GetRespawnerOfViewID(viewId);
			if ((r == null) || (r is CharacterRespawner))
				return;

			// make object temporarily invisible so we don't see it 'pos-lerping'
			// across the map to its respawn position
			//if (r.Renderer != null)
			//	r.Renderer.enabled = false;

			r.Respawn();

			// restore visibility in half a sec
			//if (r.Renderer != null)
			//	SchedulerBase.Schedule(0.5f, () => { r.Renderer.enabled = true; });

		}


		/// <summary>
		/// caches and returns the damagehandler of the given photonview id.
		/// damagehandlers are stored in a dictionary that resets on level load
		/// </summary>
		public static MPHealth GetDamageHandlerOfViewID(int id)
		{

			MPHealth d = null;

			if (!m_DamageHandlersByViewID.TryGetValue(id, out d))
			{
				PhotonView p = PhotonView.Find(id);
				if (p != null)
				{
					d = p.transform.GetComponent<MPHealth>();
					if (d != null)
						m_DamageHandlersByViewID.Add(id, d);
					return d;
				}
				// NOTE: we do not add null results, since photonviews come and go
			}

			return d;

		}


		/// <summary>
		/// caches and returns the respawner of the given photonview id.
		/// respawners are stored in a dictionary that resets on level load
		/// </summary>
		public static Respawner GetRespawnerOfViewID(int id)
		{

			Respawner d = null;

			if (!m_RespawnersByViewID.TryGetValue(id, out d))
			{

				PhotonView p = PhotonView.Find(id);
				if (p != null)
				{
					d = p.transform.GetComponent<Respawner>();
					if (d != null)
						m_RespawnersByViewID.Add(id, d);
					return d;
				}

				// NOTE: we do not add null results, since photonviews come and go
			}

			return d;

		}


		/// <summary>
		/// 
		/// </summary>
		protected void OnLevelLoad(Scene scene, LoadSceneMode mode)
		{
			m_DamageHandlersByViewID.Clear();
			m_RespawnersByViewID.Clear();
		}
	}
}