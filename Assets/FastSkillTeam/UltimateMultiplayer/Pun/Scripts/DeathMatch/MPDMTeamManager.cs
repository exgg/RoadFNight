/////////////////////////////////////////////////////////////////////////////////
//
//  MPDMTeamManager.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	An example of how to extend the base (MPTeamManager) class
//					with refresh logic for an additional stat: team 'Score'
//
//					NOTE: this class works in conjunction with 'MPDMTeam'.
//					for more information about how multiplayer teams work, see the
//					base (MPTeam and MPTeamManager) classes	
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using FastSkillTeam.UltimateMultiplayer.Pun.UI;
    using System.Collections.Generic;
	//using Hashtable = ExitGames.Client.Photon.Hashtable;

	public class MPDMTeamManager : MPTeamManager
	{
		/// <summary>
		/// convert all the teams to deathmatch teams
		/// </summary>
		protected override void Start()
		{
			base.Start();

			// convert the MPTeams from the inspector list into MPDMTeams
			List<MPDMTeam> dmTeams = new List<MPDMTeam>();
			for (int v = Teams.Count - 1; v > -1; v--)
			{
				MPDMTeam dmt = new MPDMTeam(Teams[v].Name, Teams[v].Grouping, Teams[v].ObjectiveGrouping, Teams[v].Color, Teams[v].CharacterPrefab, Teams[v].ModelIndex);
				dmTeams.Add(dmt);
			}

			// clear team list
			Teams.Clear();

			// add the new DM teams to the team list
			for (int v = dmTeams.Count - 1; v > -1; v--)
			{
				Teams.Add(dmTeams[v]);
			}
		}

		/// <summary>
		/// Adds score to a team.
		/// </summary>
		/// <param name="team">The team to add the score to.</param>
		/// <param name="score">The amount of score to apply.</param>
		public void AddExtraScore(MPDMTeam team, int score)
        {
			team.ExtraScore += score;
		}

		/// <summary>
		/// Keep scores up to date. Refreshes the score board for instant stat updates.
		/// </summary>
		public override void RefreshTeams()
		{

			base.RefreshTeams();    // always remember to call base in subsequent overrides

			// begin by zeroing out team score
			foreach (MPTeam t in Teams)
			{
				(t as MPDMTeam).Score = (t as MPDMTeam).ExtraScore;
			}

			// then add every team member's score to the team score, resulting
			// in a positive or negative number
			foreach (MPPlayer p in MPPlayer.Players.Values)
			{
				if (p == null)
					continue;
				(p.Team as MPDMTeam).Score += (int)p.Stats.Get("Score");
			}

			if (MPScoreBoard.ShowScore)
				MPScoreBoard.Refresh();

		}
	}
}