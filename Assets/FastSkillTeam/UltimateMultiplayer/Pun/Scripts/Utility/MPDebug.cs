/////////////////////////////////////////////////////////////////////////////////
//
//	MPDebug.cs
//
//	description:	simple debug message functionality for multiplayer. messages
//					are re-routed to the chat by default, but could also be pushed
//					to a console of any kind. this is work in progress
//
/////////////////////////////////////////////////////////////////////////////////
namespace FastSkillTeam.UltimateMultiplayer.Pun
{
	using Opsive.Shared.Events;

	public class MPDebug
	{
		/// <summary>
		/// prints a message to MPGameChat and anything else subscribed.
		/// </summary>
		public static void Log(string msg) => EventHandler.ExecuteEvent<string, bool>("ChatMessage", msg, false);
	}
}