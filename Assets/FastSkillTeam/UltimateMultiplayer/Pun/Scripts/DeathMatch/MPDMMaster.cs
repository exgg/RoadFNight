/////////////////////////////////////////////////////////////////////////////////
//
//  MPDMMaster.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	An example of how to extend the base (MPMaster) class
//					with a call to show the deathmatch scoreboard when the game
//					pauses on end-of-round, and to restore it when game resumes
//
//					TIP: Study the base class to learn how the game state works.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
#if PHOTON_UNITY_NETWORKING
	using Photon.Pun;
#endif
	public class MPDMMaster : MPMaster
	{
		/// <summary>
		/// Enables use of the death match scoreboard
		/// </summary>
        public override void OnEnable()
        {
            base.OnEnable();
			UI.MPScoreBoard.UseScoreBoard = true;
		}

		/// <summary>
		/// Disables use of the death match scoreboard
		/// </summary>
		public override void OnDisable()
        {
            base.OnDisable();
			UI.MPScoreBoard.UseScoreBoard = false;
		}

#if PHOTON_UNITY_NETWORKING
		/// <summary>
		/// Show the score board after base has executed.
		/// </summary>
		[PunRPC]
		protected override void ReceiveFreeze(PhotonMessageInfo info)
		{

			if (!info.Sender.IsMasterClient)
				return;

			base.ReceiveFreeze(info);

			UI.MPScoreBoard.ShowScore = true;

		}

		/// <summary>
		/// Hide the score board after base has executed.
		/// </summary>
		[PunRPC]
		protected override void ReceiveUnFreeze(PhotonMessageInfo info)
		{

			if (!info.Sender.IsMasterClient)
				return;

			base.ReceiveUnFreeze(info);

			UI.MPScoreBoard.ShowScore = false;

		}
#endif
	}
}