/////////////////////////////////////////////////////////////////////////////////
//
//	Gameplay.cs
//
//	description:	a place for globally accessible info on the game session, such
//					as whether we're in singleplayer or multiplayer mode. this can
//					be inherited to provide more info on the game: for example: custom
//					game modes
//
//					TIP: for global quick-access to the local player, see 'FST_LocalPlayer'
//
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine.SceneManagement;
using UnityEngine;
#if UNITY_EDITOR
using Opsive.Shared.Events;
#else
using UnityEngine;
#endif
namespace FastSkillTeam.UltimateMultiplayer.Shared.Game {
	public class Gameplay
	{

		public static string Version = "0.0.0";

		public static bool IsMultiplayer = false;
		protected static bool m_IsMaster = true;


		/// <summary>
		/// this property can be set by multiplayer scripts to assign master status
		/// to the local player. in singleplayer this is forced to true
		/// </summary>
		public static bool IsMaster
		{
			get
			{
				if (!IsMultiplayer)
					return true;
				return m_IsMaster;
			}
			set
			{
				if (!IsMultiplayer)
					return;
				m_IsMaster = value;
			}
		}


        /// <summary>
        /// pauses or unpauses the game by means of setting timescale to zero. will
        /// backup the current timescale for when the game is unpaused.
        /// NOTE: will not work in multiplayer
        /// </summary>
        public static bool IsPaused
        {
            get { return Time.timeScale == 0; }
            set { Time.timeScale = (Gameplay.IsMultiplayer ? 1 : value == true ? 0: 1); }
        }

        /// <summary>
        /// returns the build index of the currently loaded level (Unity version agnostic)
        /// </summary>
        public static int CurrentLevel
		{
			get
			{
				return SceneManager.GetActiveScene().buildIndex;
			}
		}


		/// <summary>
		/// returns the name of the currently loaded level (Unity version agnostic)
		/// </summary>
		public static string CurrentLevelName
		{
			get
			{
				return SceneManager.GetActiveScene().name;
			}
		}


		/// <summary>
		/// quits the game in the appropriate way, depending on whether we're running
		/// in the editor, in a standalone build or a webplayer (these are the only
		/// platforms supported by the 'Quit' method at present)
		/// </summary>
		public static void Quit(string webplayerQuitURL = "http://FastSkillTeam.com")
		{

#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
			EventHandler.ExecuteEvent("EditorApplicationQuit");
#elif UNITY_STANDALONE
			Application.Quit();
#elif UNITY_WEBPLAYER
			Application.OpenURL(webplayerQuitURL);
#endif
			// NOTES:
			//  1) web player is not supported by Unity 5.4+
			//	2) on iOS, an app should only be terminated by the user
			//	3) at time of writing OpenURL does not work on WebGL

		}
	}
}