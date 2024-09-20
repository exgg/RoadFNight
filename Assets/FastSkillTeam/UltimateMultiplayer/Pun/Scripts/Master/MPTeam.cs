/////////////////////////////////////////////////////////////////////////////////
//
//  MPTeam.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	Base class for a multiplayer team. Defines basic properties
//					such as team name, grouping (used for spawnpoints), color (nametags)
//					and the character index (used for character model selection).
//					The character index can be also forced to a selected character.
//					For more on character index, see MPMenu.cs and MPPlayerSpawner.cs
//				
//					NOTE: this class works in conjunction with 'MPTeamManager'.
//					you can inherit both classes to declare teams with
//					further functionality. for an example of this, see the
//					deathmatch scripts 'MPDMTeam' and 'MPDMTeamManager'
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
	using UnityEngine;

	[System.Serializable]
	public class MPTeam
	{
		public string Name = "new team";
		public int Grouping = -1;       // used for spawnpoints
		public int ObjectiveGrouping = -1;       // used for objective spawnpoints
		public Color Color = Color.blue;    // used for nametags
		public GameObject CharacterPrefab = null;
		public int ModelIndex = -1; // determines which local and remote body prefab to use for team members
		/// <summary>
		/// returns the team number (0 means 'NoTeam')
		/// </summary>
		public int Number
		{
			get
			{
				return MPTeamManager.Instance.Teams.IndexOf(this);
			}
		}

		/// <summary>
		/// constructor
		/// </summary>
		public MPTeam(string name, int grouping, int objectiveGrouping, Color color, GameObject characterPrefab, int modelIndex = -1)
		{
			Name = name;
			Grouping = grouping;
			ObjectiveGrouping = objectiveGrouping;
			Color = color;
			CharacterPrefab = characterPrefab;
			ModelIndex = modelIndex;
		}
	}
}