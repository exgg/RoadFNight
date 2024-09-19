/////////////////////////////////////////////////////////////////////////////////
//
//  MPDMDamageCallbacks.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	an example of how to extend the base (MPDamageCallbacks)
//					class with additional callback logic for 'Damage' events. here,
//					we refresh the 'Deaths', 'Frags' and 'Score' stats declared in
//					MPDMPlayerStats every time a player dies, and broadcast a new
//					gamestate (with these stats only) to reflect it on all machines.
//
//					TIP: study the base class to see how the 'TransmitKill' callback works
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
	using UnityEngine;
	using Photon.Pun;
	using Opsive.UltimateCharacterController.Traits.Damage;
	using Opsive.UltimateCharacterController.Items.Actions;
	using Opsive.UltimateCharacterController.Character;
	using Opsive.Shared.Events;

	public class MPDMDamageCallbacks : MPDamageCallbacks
	{
		[Tooltip("The score awarded per frag (kill) in the deathmatch game mode.")]
		[SerializeField] protected int m_ScorePerFrag = 10;
		[Tooltip("The score deducted per death in the deathmatch game mode.")]
		[SerializeField] protected int m_ScorePerDeath = -10;
		[Tooltip("The score deducted per team kill in the deathmatch game mode.")]
		[SerializeField] protected int m_ScorePerTeamKill = -10;
		/// <summary>
		/// If a player dies during deathmatch, this is where the stats are tracked.
		/// </summary>
		protected override void TransmitDamage(Transform targetTransform, DamageData damageData)
		{
			if (!PhotonNetwork.IsMasterClient)
				return;

			MPPlayer target = MPPlayer.Get(targetTransform);
			if ((target == null)                                    // if target is an object (not a player) ...
				|| (target.PlayerHealth.HealthValue > 0.0f))       // ... or it's a player that was only damaged (not killed) ...
			{
				//TODO: Damage Stats?

				// ... transmit a simple health update (update remote clients with
				// its health from the master scene) and bail out
				base.TransmitDamage(targetTransform, damageData);
				return;
			}

			// if we get here then target was a player that got killed, so
			// see if we know about the damage source

			Transform sourceTransform = null;
			GameObject attackingObject = null;
			IDamageSource source = damageData.DamageSource;
			var ctx = damageData.ImpactContext;
			float damage = damageData.Amount;

			if (source != null)
			{
				attackingObject = source.SourceGameObject;

				// If the originator is an item then more data needs to be sent.
				if (source is CharacterItemAction)
				{
					var itemAction = source as CharacterItemAction;
					sourceTransform = itemAction.CharacterTransform;
					if (attackingObject == null)
						attackingObject = itemAction.GameObject;
					//TODO: Add stats
				}
				if (sourceTransform == null && damageData.DamageSource.SourceOwner && source.TryGetCharacterLocomotion(out CharacterLocomotion characterLocomotion))
					sourceTransform = characterLocomotion.transform;

				//Debug.Log("GOT FROM SOURCE: " + (sourceTransform ? sourceTransform : "NULL"));
				//MPDebug.Log("GOT FROM SOURCE: " + (sourceTransform ? sourceTransform : "NULL"));
			}
			if ((sourceTransform == null || attackingObject == null) && ctx != null)
			{
				if (attackingObject == null && ctx.CharacterItemAction != null)
				{
					attackingObject = ctx.CharacterItemAction.GameObject;
					//TODO: Add stats
				}

				if (sourceTransform == null && ctx.ImpactCollisionData != null && ctx.ImpactCollisionData.SourceCharacterLocomotion != null)
				{
					sourceTransform = ctx.ImpactCollisionData.SourceCharacterLocomotion.transform;
				}
				if ((sourceTransform == null || attackingObject == null) && ctx.ImpactCollisionData.SourceGameObject != null)
				{
					if (sourceTransform == null)
						sourceTransform = ctx.ImpactCollisionData.SourceGameObject.transform;
					if (attackingObject == null)
						attackingObject = ctx.ImpactCollisionData.SourceGameObject;
				}

				//	Debug.Log("GOT ALTERNATIVELY: " + (sourceTransform? sourceTransform:"NULL"));
				//	MPDebug.Log("GOT ALTERNATIVELY: " + (sourceTransform ? sourceTransform : "NULL"));
			}

			int viewID = MPMaster.GetViewIDOfTransform(targetTransform);
			if (viewID == 0)//Unable to fetch the target, they may have disconnected.
				return;

			MPPlayer attacker = null;
			if (sourceTransform != null)
				attacker = MPPlayer.Get(sourceTransform);

			if (attacker == null)
			{
				//Debug.Log("MPDamageCallbacks: NULL ATTACKER");
				//MPDebug.Log("MPDamageCallbacks: NULL ATTACKER");

				//could make player kill themselves, as it was a likely suicide or random death
				return;
			}

			//	MPDebug.Log("healthValue + damage = " + target.PlayerHealth.HealthValue + damage);
			//	Debug.Log("healthValue + damage = " + target.PlayerHealth.HealthValue + damage);

			// we know who did it! if this injury killed the target, update the
			// local (master scene) stats for both players TIP: hit statistics
			// can be implemented here
			if ((target.PlayerHealth.HealthValue + damage) > 0.0f)
			{
				// you get one 'Score' and one 'Kill' for every takedown of a player
				// on a different team
				if (target != attacker)
				{
					if ((target.TeamNumber != attacker.TeamNumber)                  // inter-team kill
						|| ((target.TeamNumber == 0) && (attacker.TeamNumber == 0)) // or there are no teams!
						)
					{
						MPDebug.Log(MPPlayer.GetName(attacker.ID) + " KILLED " + MPPlayer.GetName(target.ID));
						Debug.Log(MPPlayer.GetName(attacker.ID) + " KILLED " + MPPlayer.GetName(target.ID));
						attacker.Stats.Set("Frags", (int)attacker.Stats.Get("Frags") + 1);
						attacker.Stats.Set("Score", (int)attacker.Stats.Get("Score") + (int)attacker.Stats.Get("BonusScore") + m_ScorePerFrag);
						target.Stats.Set("Deaths", (int)target.Stats.Get("Deaths") + 1);
						target.Stats.Set("Score", (int)target.Stats.Get("Score") + m_ScorePerDeath);
					}
					else    // intra-team kill
					{
						MPDebug.Log(MPPlayer.GetName(attacker.ID) + " TEAM-KILLED " + MPPlayer.GetName(target.ID));
						Debug.Log(MPPlayer.GetName(attacker.ID) + " TEAM-KILLED " + MPPlayer.GetName(target.ID));
						// you loose one 'Score' for every friendly kill
						// the teammate's stats are not affected
						attacker.Stats.Set("Score", (int)attacker.Stats.Get("Score") + m_ScorePerTeamKill);
					}

					//TODO: Optimise if possible (and if still can prevent hacks) by using the recieved Kill from TransmitKill. This will trigger one extra RPC for kill feed. For now it has to happen on master, as only master knows all!
					EventHandler.ExecuteEvent<string, string, string>("ReportMPKill", MPPlayer.GetName(attacker.ID), attackingObject ? attackingObject.name : "FUBAR", MPPlayer.GetName(target.ID));
				}
				else
				{
					// killing yourself shall always award one 'Death' and minus one 'Score'
					MPDebug.Log("FUBAR " + MPPlayer.GetName(target.ID));
					Debug.Log("FUBAR " + MPPlayer.GetName(target.ID));
					target.Stats.Set("Deaths", (int)target.Stats.Get("Deaths") + 1);
					target.Stats.Set("Score", (int)target.Stats.Get("Score") + m_ScorePerDeath);

					//TODO: Optimise if possible (and if still can prevent hacks) by using the recieved Kill from TransmitKill. This will trigger one extra RPC for kill feed. For now it has to happen on master, as only master knows all!
					EventHandler.ExecuteEvent<string, string, string>("ReportMPKill", MPPlayer.GetName(target.ID), attackingObject ? attackingObject.name : "FUBAR", MPPlayer.GetName(target.ID));
				}

			}

			// send RPC with updated stats to the photonView of the gamelogic
			// object on all clients. NOTES:
			//	1) we only broadcast the stats that have actually changed
			//	2) we can't send a target and sender with the same ID, since
			//     adding the same key twice to a hashtable is impossible
			if (target != attacker) // kill
			{
				MPMaster.Instance.TransmitPlayerState(new int[] { target.ID, attacker.ID },
					new string[] { "Deaths", "Score" },
					new string[] { "Frags", "Score" });
			}
			else    // suicide
			{
				MPMaster.Instance.TransmitPlayerState(new int[] { target.ID },
					new string[] { "Deaths", "Score" });
			}

			//ensure the scoreboard is kept up to date on all machines.
			if (MPTeamManager.Exists)
				MPTeamManager.Instance.RefreshTeams();
		}
	}
}