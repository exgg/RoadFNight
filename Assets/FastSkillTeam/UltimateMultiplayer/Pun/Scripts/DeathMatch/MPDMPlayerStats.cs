/////////////////////////////////////////////////////////////////////////////////
//
//	MPDMPlayerStats.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	an example of how to extend the base (MPPlayerStats) class
//					with getter and setter actions for additional player stats:
//					Deaths, Frags and Score. these are later manipulated in the
//					example class 'MPDamageCallbacksDeathMatch'
//
//					IMPORTANT: by default, a 'MPPlayerStats' component is auto-added
//					to every player by the MPPlayerSpawner on startup. if you want to
//					use a derived component instead (such as this one) then you must
//					update the name of the data component in the Inspector. go to your
//					MPPlayerSpawner component - > Add Components -> Local & Remote
//					and update the string to match the new stats component classname.
//
//					TIP: study the base class to learn more about player stats
//
/////////////////////////////////////////////////////////////////////////////////

using Opsive.Shared.Events;

namespace FastSkillTeam.UltimateMultiplayer.Pun {
	public class MPDMPlayerStats : MPPlayerStats
	{

		protected int Frags = 0;
		protected int Deaths = 0;
		protected int Score = 0;
		protected int BonusScore = 0;

		public override void InitStats()
		{
			base.InitStats();

			Getters.Add("Deaths", delegate () { return Deaths; });
			Getters.Add("Frags", delegate () { return Frags; });
			Getters.Add("Score", delegate () { return Score; });
			Getters.Add("BonusScore", delegate { return BonusScore; });

			Setters.Add("Deaths", delegate (object val) { Deaths = (int)val; });
			Setters.Add("Frags", delegate (object val) { Frags = (int)val; });
			Setters.Add("Score", delegate (object val) { int curScore = Score; Score = (int)val; int dif = Score - curScore; if (dif != 0) EventHandler.ExecuteEvent(gameObject, "OnScorePoints", dif); });
			Setters.Add("BonusScore", delegate (object val) { BonusScore = (int)val; if (BonusScore < 0) BonusScore = 0; });

		}


		/// <summary>
		/// resets health, shots and inventory to default + resurrects
		/// this player (if dead)
		/// </summary>
		public override void FullReset()
		{

			base.FullReset();       // always remember to call base in subsequent overrides

			Frags = 0;
			Deaths = 0;
			Score = 0;
			BonusScore = 0;

		}
	}
}