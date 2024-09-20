/////////////////////////////////////////////////////////////////////////////////
//
//  MPDMTeam.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	Base class for a deathmatch team. this is an example of how to
//					extend the base (MPTeam) class with an additional stat, "Score"
//				
//					NOTE: this class works in conjunction with 'MPDMTeamManager'.
//					for more information about how multiplayer teams work, see the
//					base (MPTeam and MPTeamManager) classes	
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
namespace FastSkillTeam.UltimateMultiplayer.Pun {

	[System.Serializable]
	public class MPDMTeam : MPTeam
	{
        public MPDMTeam(string name, int grouping, int objectiveGrouping, Color color, GameObject character, int modelIndex = -1) : base(name, grouping, objectiveGrouping, color, character, modelIndex) { }

		public int Score = 0;

		public int ExtraScore = 0;
	}
}
