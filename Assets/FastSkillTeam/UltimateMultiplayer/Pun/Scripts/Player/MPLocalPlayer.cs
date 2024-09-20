/////////////////////////////////////////////////////////////////////////////////
//
//  MPLocalPlayer.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	This class represents the UCC local player in multiplayer.
//					it extends MPPlayer with all functionality specific
//					to the one-and-only local player on this machine. this includes
//					listening to all sorts of events triggered by the EventHandler
//					(such as round resets and chat functions).
//					It also prevents input during certain multiplayer game phases.
//
/////////////////////////////////////////////////////////////////////////////////


namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using FastSkillTeam.UltimateMultiplayer.Shared.Game;
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.Shared.Input;
    using Opsive.UltimateCharacterController.Traits;
    using Photon.Pun;
    using UnityEngine;

    public class MPLocalPlayer : MPPlayer
    {
		// local player
		private static MPLocalPlayer m_Instance = null;
		public static MPLocalPlayer Instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = Component.FindObjectOfType(typeof(MPLocalPlayer)) as MPLocalPlayer;
				}
				return m_Instance;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Awake()
		{
			m_Instance = this;
			base.Awake();
			EventHandler.RegisterEvent("OnResetGame", OnResetGame);

        }

		private void OnResetGame()
		{
			Respawner.Respawn();
		}

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnEnable()
		{
			EventHandler.RegisterEvent<bool>("OnShowChatWindow", OnShowChat);
		}


		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnDisable()
		{
			EventHandler.UnregisterEvent<bool>("OnShowChatWindow", OnShowChat);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			EventHandler.UnregisterEvent("OnResetGame", OnResetGame);
            m_Instance = null;
		}

		/// <summary>
		/// 
		/// </summary>
		protected virtual void Start()
		{

			// allow player to move by default
			UnFreeze();

		}
		private bool m_OriginalEnableCursorWithEscape;
		private void OnShowChat(bool show)
		{
			EventHandler.ExecuteEvent<bool>(Instance.GetUltimateCharacterLocomotion.GameObject, "OnEnableGameplayInput", !show);
			var p = Instance.Transform.GetComponentInChildren(typeof(PlayerInputProxy), true) as PlayerInputProxy;

			Debug.Log("Mouse lock hack, REMIND JUSTIN => Need to add EnableCursorWithEscape to the Interface or Proxy");
#if ENABLE_LEGACY_INPUT_MANAGER
			UnityInput u = p.PlayerInput as UnityInput;
			if (show)
			{
				m_OriginalEnableCursorWithEscape = u.EnableCursorWithEscape;
				u.EnableCursorWithEscape = false;
			}
			else Scheduler.Schedule(0.1f, () => u.EnableCursorWithEscape = m_OriginalEnableCursorWithEscape);
#else
			Opsive.Shared.Integrations.InputSystem.UnityInputSystem u = p.PlayerInput as Opsive.Shared.Integrations.InputSystem.UnityInputSystem;
			if (show)
			{
				m_OriginalEnableCursorWithEscape = u.EnableCursorWithEscape;
				u.EnableCursorWithEscape = false;
			}
			else Scheduler.Schedule(0.1f, () => u.EnableCursorWithEscape = m_OriginalEnableCursorWithEscape);
#endif

		}

		/// <summary>
		/// disarms, stops and locks player so that it cannot move
		/// </summary>
		public static void Freeze()
		{

			if (Instance == null)
				return;

			if (Instance.GetUltimateCharacterLocomotion == null)
				return;

			Scheduler.Schedule(0.1f, delegate () { EventHandler.ExecuteEvent<bool>(Instance.GetUltimateCharacterLocomotion.GameObject, "OnEnableGameplayInput", false); });

		}


		/// <summary>
		/// allows player to move again and tries to wield the start weapon
		/// </summary>
		public static void UnFreeze()
		{

			if (Instance == null)
				return;

			if (Instance.GetUltimateCharacterLocomotion == null)
				return;

			//  TIP: Uncomment this for any issues with ongoing abilities.
			//  NOTE: It is more fluent to handle this within the ability itself. Take Board for example,
			//  being ejected the moment the games stops is abrupt, therefore the ability is allowed to continue running, but without user input.
			//	var abilities = MPLocalPlayer.Instance.GetUltimateCharacterLocomotion.ActiveAbilities;
			//	for (int i = 0; i < abilities.Length; i++)
			//	MPLocalPlayer.Instance.GetUltimateCharacterLocomotion.TryStopAbility(abilities[i], true);

			EventHandler.ExecuteEvent<bool>(Instance.GetUltimateCharacterLocomotion.GameObject, "OnEnableGameplayInput", true);
		}

        /// <summary>
        /// For special use cases where the player is damaged locally only. (ie. Self Harm Locally... MP Damage Zone Damage, Fall Damage).
		/// Notifies the master client.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <param name="forceMagnitude"></param>
        /// <param name="attacker"></param>
        public static void Damage(float amount, Vector3 position, Vector3 direction, float forceMagnitude, GameObject attacker)
		{
            if (Instance == null)
                return;

            Instance.PlayerHealth.Damage(amount, position, direction, forceMagnitude, attacker);

            if (Gameplay.IsMaster)
				return;

			int attackerID = MPMaster.GetViewIDOfTransform(attacker.transform);

            Instance.photonView.RPC("ReceiveLocalDamageRPC", RpcTarget.MasterClient, amount, attackerID);
        }

        /*
                private void OnCollisionEnter(Collision collision)
                {
                    if (collision.rigidbody)
                    {
                        Vector3 direction = Transform.forward;
                        Vector3 point = collision.contacts[0].point;
                        EventHandler.ExecuteEvent<Vector3, Vector3>(collision.rigidbody, "Push", direction, point);
                    }
                }
        */
    }
}