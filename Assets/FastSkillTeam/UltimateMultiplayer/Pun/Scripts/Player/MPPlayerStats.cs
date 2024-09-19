/////////////////////////////////////////////////////////////////////////////////
//
//  MPPlayerStats.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	This component is a hub for all the common gameplay stats of
//					a player in multiplayer. it does not HOLD any data, but COLLECTS
//					it from various external components and EXPOSES it via the public
//						'Get', 'Set' and 'Erase' methods.
//					this is relied heavily upon by 'MPMaster'. the basic stats are:
//						CharacterIndex, Team, Health, Shots, Position and Rotation.
//
//					NOTE: By default, this component is automatically added to every player
//					upon spawn. if you inherit the component you must update the class name
//					to be auto-added. this can be altered in the Inspector. go to your
//					MPPlayerSpawner component - > Add Components -> Local & Remote.
//
//					TIP: New stats can be added by inheriting this class. for an example
//					of this, see 'MPDMPlayerStats' and 'MPDMDamageCallbacks'
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{

	using UnityEngine;
	using System.Collections.Generic;
	using System;
	using Opsive.UltimateCharacterController.Inventory;
	using Opsive.Shared.Game;
	using Opsive.UltimateCharacterController.Character.Abilities;
	using Hashtable = ExitGames.Client.Photon.Hashtable;

	public class MPPlayerStats : MonoBehaviour
	{
		public Dictionary<string, Func<object>> Getters = new Dictionary<string, Func<object>>();
		public Dictionary<string, Action<object>> Setters = new Dictionary<string, Action<object>>();

		public float Health
		{
			get
			{
				if (MPPlayer == null)
					return 0.0f;
				if (MPPlayer.PlayerHealth == null)
					return 0.0f;
				return MPPlayer.PlayerHealth.HealthValue;
			}
			set
			{
				if (MPPlayer == null)
					return;
				if (MPPlayer.PlayerHealth == null)
					return;
				MPPlayer.PlayerHealth.HealthAttribute.Value = value;
			}
		}

		public int TeamNumber
		{
			get
			{
				if (MPPlayer == null)
					return 0;

				return MPPlayer.TeamNumber;
			}
			set
			{
				if (MPPlayer == null)
					return;

				MPPlayer.TeamNumber = value;
			}
		}

		public int Grouping
		{
			get
			{
				if (MPPlayer == null)
					return -1;

				return MPPlayer.Grouping;
			}
			set
			{
				if (MPPlayer == null)
					return;

				MPPlayer.Grouping = value;
			}
		}

		public int ModelIndex
		{
			get
			{
				if (MPPlayer == null)
					return 0;
				return MPPlayer.ModelIndex;
			}
			set
			{
				if (MPPlayer == null)
					return;
				MPPlayer.ModelIndex = value;
			}
		}


		// --- expected components ---

		Inventory m_Inventory = null;
		public Inventory Inventory
		{
			get
			{
				if (m_Inventory == null)
					m_Inventory = (Inventory)gameObject.GetComponentInChildren<Inventory>();
				return m_Inventory;
			}
		}


		protected MPPlayer m_MPPlayer = null;
		public MPPlayer MPPlayer
		{
			get
			{
				if (m_MPPlayer == null)
					m_MPPlayer = transform.GetComponent<MPPlayer>();//stats generally are added to the player root.
				if (m_MPPlayer == null)
					m_MPPlayer = transform.GetComponentInParent<MPPlayer>();//if not we need to search upwards, as if we search from "root" down,
																		   //then we may end up with the wrong MPPlayer.
																		   //For example, a player may be on board a vehicle with other players when this is fetched
				return m_MPPlayer;
			}
		}


		/// <summary>
		/// hashtable of all the important player stats to be part of the
		/// game state. the master client will sync these stats with all
		/// other players in multiplayer. NOTE: the actual stat names are
		/// defined in the overridable method 'AddStats'
		/// </summary>
		public virtual Hashtable All
		{
			// this should be used seldomly, by game state-updating methods
			// TODO: move to master client ?
			get
			{
				if (m_Stats == null)
					m_Stats = new Hashtable();
				else
					m_Stats.Clear();
				foreach (string s in Getters.Keys)
				{
					m_Stats.Add(s, Get(s));
				}
				return m_Stats;
			}
			set
			{
				if (value == null)
				{
					m_Stats = null;
					return;
				}
				foreach (string s in Setters.Keys)
				{
					object o = GetFromHashtable(value, s);
					if (o != null)  // may be null if only a partial gamestate is received
					{
						Set(s, o);
					}
				}
			}
		}
		protected Hashtable m_Stats;


		/// <summary>
		/// returns a list of the names of all player multiplayer stats
		/// </summary>
		public List<string> Names   //	TODO: make static, based on first player, if any (?)
		{
			get
			{
				if (m_StatNames == null)
				{
					m_StatNames = new List<string>();
					foreach (string s in Getters.Keys)
					{
						m_StatNames.Add(s);
					}
				}
				return m_StatNames;
			}
		}
		protected List<string> m_StatNames;


		/// <summary>
		/// Init stats as soon as this object is spawned.
		/// </summary>
		void Awake()
		{
			InitStats();
		}

		/// <summary>
		/// this class can be overridden to add additional stats, but
		/// NOTE: remember to include base.AddStats in the override.
		/// also don't call 'AddStats' from the derived class but
		/// leave this to 'Awake' in this class
		/// </summary>
		public virtual void InitStats()
		{
		//	Debug.Log("Init Stats");
			// -------- getters --------

			if (MPPlayer == null)
			{
				Debug.LogError("Error (" + this + ") Found no MPPlayer! Aborting ...");
				return;
			}

			Getters.Add("ModelIndex", delegate () { return ModelIndex; });
			Getters.Add("Team", delegate () { return MPPlayer.TeamNumber; });
			Getters.Add("Health", delegate () { return Health; });
			Getters.Add("Shots", delegate () { return MPPlayer.Shots; });
			Getters.Add("Position", delegate () { return MPPlayer.Transform.position; });
			Getters.Add("Rotation", delegate () { return MPPlayer.Transform.rotation; });
			Getters.Add("Grouping", delegate () { return Grouping; });

			// -------- setters --------

			Setters.Add("ModelIndex", delegate (object val) { ModelIndex = (int)val; });
			Setters.Add("Team", delegate (object val) { MPPlayer.TeamNumber = (int)val; });
			Setters.Add("Health", delegate (object val) { Health = (float)val; });
			// NOTE: 'Shots' must never be updated with a lower (lagged) value or
			// simulation will go out of sync. however, we need to be able
			// to set it to zero for game reset purposes.
			Setters.Add("Shots", delegate (object val) { MPPlayer.Shots = (((int)val > 0) ? Mathf.Max(MPPlayer.Shots, (int)val) : 0); });
			Setters.Add("Position", delegate (object val) { MPPlayer.LastMasterPosition = (Vector3)val; MPPlayer.SetPosition(MPPlayer.LastMasterPosition); });
			Setters.Add("Rotation", delegate (object val) { MPPlayer.LastMasterRotation = (Quaternion)val; MPPlayer.SetRotation(MPPlayer.LastMasterRotation); });
			Setters.Add("Grouping", delegate (object val) { Grouping = (int)val; });
		}


		/// <summary>
		/// erases all the stats of this particular player
		/// </summary>
		public static void EraseStats()
		{

			foreach (MPPlayer player in MPPlayer.Players.Values)
			{
				if (player == null)
					continue;
				player.Stats.All = null;
			}

		}


		/// <summary>
		/// resets health, shots and inventory to default + resurrects
		/// this player (if dead)
		/// </summary>
		public virtual void FullReset()
		{

			Health = MPPlayer.PlayerHealth.HealthAttribute.MaxValue;

			//Grouping = -1;

			MPPlayer.Shots = 0;

			Inventory.LoadDefaultLoadout();

			Die die = MPPlayer.GetUltimateCharacterLocomotion.GetAbility<Die>();
			if (die != null)
				MPPlayer.GetUltimateCharacterLocomotion.TryStopAbility(die, true);

		}


		/// <summary>
		/// restores health, shots, inventory and life on all players
		/// </summary>
		public static void FullResetAll()
		{

			// reset all network players
			foreach (MPPlayer p in MPPlayer.Players.Values)
			{
				if (p == null)
					continue;
				p.Stats.FullReset();
			}

			Scheduler.Schedule(1, delegate ()
			{
				MPPlayer.RefreshPlayers();
			});

		}


		/// <summary>
		/// extracts a stat of a player given its string name
		/// </summary>
		public object Get(string stat)
		{
			Func<object> o = null;
			Getters.TryGetValue(stat, out o);
			if (o != null)
				return o.Invoke();
			Debug.LogError("Error (" + this + ") The stat '" + stat + "' has not been declared by the player stats script.");
			return null;
		}


		/// <summary>
		/// sets a stat on a player given its string name
		/// </summary>
		public void Set(string stat, object val)
		{
			Action<object> o = null;
			Setters.TryGetValue(stat, out o);
			if (o != null)
				o.Invoke(val);
		}


		/// <summary>
		/// 
		/// </summary>
		public static void Set(MPPlayerStats playerStats, string stat, object val)
		{
			if (playerStats == null)
			{
				Debug.LogError("Error (MPPlayerStats) 'playerStats' was null.");
				return;
			}

			playerStats.Set(stat, val);
		}


		/// <summary>
		/// 
		/// </summary>
		public void SetFromHashtable(Hashtable stats)
		{
			if (stats == null)
				return;

			foreach (object o in stats.Keys)
				Set((string)o, GetFromHashtable(stats, (string)o));
		}


		/// <summary>
		/// extracts a value from a provided player state hashtable given
		/// its string name
		/// </summary>
		public static object GetFromHashtable(Hashtable hashTable, string stat)
		{
			m_Stat = null;
			hashTable.TryGetValue(stat, out m_Stat);
			return m_Stat;
		}
		protected static object m_Stat;
	}
}