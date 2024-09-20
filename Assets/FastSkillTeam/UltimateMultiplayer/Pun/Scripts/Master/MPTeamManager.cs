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
//	Description:	This script allows you to define multiplayer teams in the editor,
//					and contains a number of utility methods to work with them in code
//
//					NOTE: this class works in conjunction with 'MPTeam'.
//					you can inherit both classes to declare teams with
//					further functionality. for an example of this, see the
//					deathmatch demo scripts 'MPDMTeam' and 'MPDMTeamManager'	
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
	public class MPTeamManager : MonoBehaviour
	{



		[SerializeField]
		public List<MPTeam> Teams = new List<MPTeam>();


		/// <summary>
		/// returns the amount of teams. NOTE: team 0 is always 'NoTeam'
		/// </summary>
		public static int TeamCount
		{
			get
			{
				return (Instance == null ? 0 : Instance.Teams.Count);
			}
		}


		/// <summary>
		/// returns true if the scene has a MPTeamManager-derived component.
		/// if this returns false, the teams concept is largely ignored by
		/// other multiplayer scripts
		/// </summary>
		public static bool Exists
		{
			get
			{
				return Instance != null;
			}
		}


		// --- properties ---

		private static MPTeamManager m_Instance = null;
		public static MPTeamManager Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = Component.FindObjectOfType(typeof(MPTeamManager)) as MPTeamManager;
				return m_Instance;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		protected virtual void Start()
		{

			// insert team zero as 'NoTeam'. players on this team are to be
			// considered 'teamless' and their team color will be white
			Teams.Insert(0, new MPTeam("NoTeam", 0, 0, Color.white, MPPlayerSpawner.Instance.DefaultCharacter));
		}

		protected virtual void OnEnable()
        {
			m_Instance = this;
		}

		protected virtual void OnDisable()
		{
			m_Instance = null;
		}

		/// <summary>
		/// returns the player type for team of 'teamNumber'. this determines
		/// which CharacterIndex to use for team members
		/// </summary>
		public static int GetTeamModelIndex(int teamNumber)
		{

			if (teamNumber < 1)
				return 0;

			return Instance.Teams[teamNumber].ModelIndex;

		}

		/// <summary>
		/// this method can be overridden to refresh team logic such as team
		/// score, if implemented
		/// </summary>
		public virtual void RefreshTeams()
		{
		}

		/// <summary>
		/// returns the team with the lowest player count. used to even out
		/// the odds by assigning joining players to the smallest team
		/// </summary>
		public virtual int GetSmallestTeam()
		{

			MPTeam smallestTeam = null;
			if (MPPlayer.Players.Count > 0)
			{
				foreach (MPTeam team in Teams)
				{
					if (Teams.IndexOf(team) > 0)
					{
						if ((smallestTeam == null) || (GetTeamSize(team) <= GetTeamSize(smallestTeam)))
						{
							smallestTeam = team;
						}
					}
				}
			}
			if (smallestTeam == null)
				smallestTeam = Teams[Random.Range(1, Teams.Count)];

			return (smallestTeam != null ? Teams.IndexOf(smallestTeam) : 0);

		}

		/// <summary>
		/// returns the player count of 'team'
		/// </summary>
		protected virtual int GetTeamSize(MPTeam team)
		{

			int amount = 0;
			foreach (MPPlayer p in MPPlayer.Players.Values)
			{
				if (p == null)
					continue;
				if (p.TeamNumber == Teams.IndexOf(team))
					amount++;
			}

			return amount;
		}

		/// <summary>
		/// returns the name of team with 'teamNumber'
		/// </summary>
		public static string GetTeamName(int teamNumber)
		{

			if (!IsValidTeamNumber(teamNumber))
				return null;

			return Instance.Teams[teamNumber].Name;

		}

		/// <summary>
		/// returns the spawnpoint grouping of team with 'teamNumber'
		/// </summary>
		public static int GetTeamGrouping(int teamNumber)
		{
			if (!IsValidTeamNumber(teamNumber))
				return -1;

			return Instance.Teams[teamNumber].Grouping;

		}

		/// <summary>
		/// returns the objective spawnpoint grouping of team with 'teamNumber'
		/// </summary>
		public static int GetObjectiveGrouping(int teamNumber)
		{
			if (!IsValidTeamNumber(teamNumber))
				return -1;

			return Instance.Teams[teamNumber].ObjectiveGrouping;

		}

		/// <summary>
		/// returns the color of team with 'teamNumber'
		/// </summary>
		public static Color GetTeamColor(int teamNumber)
		{

			if (!IsValidTeamNumber(teamNumber))
				return Color.white;

			return Instance.Teams[teamNumber].Color;

		}

		/// <summary>
		/// used to verify if a team number will be out of bounds visavi
		/// the list of teams
		/// </summary>
		public static bool IsValidTeamNumber(int teamNumber)
		{
			if (Instance == null)
				return false;

			if (Instance.Teams == null)
				return false;

			if (teamNumber < 0)
			{
				Debug.LogError("Team number must be 0 or greater and less than the count of teams.");
				return false;
			}

			// the number of teams is always one more than the number
			// of valid team numbers because of team 0 ('NoTeam')
			return (teamNumber < Instance.Teams.Count);

		}

		public static GameObject GetTeamCharacter(int teamNumber)
        {
			if (!IsValidTeamNumber(teamNumber))
				return MPPlayerSpawner.Instance.DefaultCharacter;
			GameObject character = Instance.Teams[teamNumber].CharacterPrefab;
			if (character == null)
				character = MPPlayerSpawner.Instance.DefaultCharacter;
			return character;
		}
    }
}

