/////////////////////////////////////////////////////////////////////////////////
//
//  MPGameChat.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	A simple chat system for basic multiplayer testing. Including
//					channels and filters.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
	using UnityEngine;
	using System.Collections.Generic;
	using Photon.Pun;
	using Photon.Realtime;
	using Opsive.Shared.Input;
	using Opsive.Shared.Events;
	using Opsive.Shared.Game;
	using FastSkillTeam.UltimateMultiplayer.Shared.Game;

	[RequireComponent(typeof(PhotonView))]
	public class MPGameChat : MonoBehaviourPun
	{

		// adjustable settings
		public enum AllowedChannels { Team, Squad, All }
		protected static AllowedChannels s_AllowedChannel = AllowedChannels.Team;


		[SerializeField] protected AllowedChannels m_AllowedChannel = AllowedChannels.Team;
		public AllowedChannels AllowedChannel { get => m_AllowedChannel; set { m_AllowedChannel = value; s_AllowedChannel = value; } }
		/*[Tooltip("Sound played when mssg successful.")]
		[SerializeField] protected AudioClip m_ChatSound = null;
		[Tooltip("Sound played when mssg errors.")]
		[SerializeField] protected AudioClip m_ChatErrorSound = null;*/
		public MessageFilterBehavior FilterMode = MessageFilterBehavior.Alert;

		// layout
		protected float m_LineHeight = 16.0f;
		protected int m_Padding = 2;

		// work variables
		protected int m_LastTextLength = 0;
		protected int m_FocusControlAttempts = 0;
		protected float m_LastScreenWidth = 0.0f;
		protected float m_LastScreenHeight = 0.0f;
		protected Vector2 m_ScrollPosition = Vector2.zero;
		protected bool m_HaveSkin = false;
		protected bool m_TextInputVisible = false;
		protected bool m_MouseCursorZonesInitialized = false;
		protected List<int> m_MutedPlayers = new List<int>();

		// input rects
		protected Rect[] m_OriginalMouseCursorZones;
		protected Rect[] m_TextInputMouseCursorZones;

		// gui rects
		protected Rect m_ViewRect = new Rect();
		protected Rect m_TextFieldRect = new Rect();
		protected Rect m_SendButtonRect = new Rect();
		protected Rect m_ScrollbarRect = new Rect();

		// messages
		protected string m_InputLine = "";

		public enum MessageFilterBehavior
		{
			Alert,  // play a sound to alert the sender that this message was blocked
			Silent  // have it appear as if the message was sent, but only actually show it on the sender's machine
		}

		private bool IsMessageClean(int senderID, string s)
		{
			// you can implement muting of players by filling the 'mutedplayers'
			// list with id:s from a gui control
			if (m_Chat != null && m_Chat.m_MutedPlayers.Contains(senderID))
				return false;

			// you can prevent undesired words from displaying by returning false here, for example ...
			if (s.Contains("jar jar binks") || s.Contains("f00k"))
				return false;
			// ... although this should be done much more efficiently (use a dictionary)


			// This will make the message silently fail on clients with another team. 
			// TODO: sending client needs different bindings for global and team messages(squad, team, all teams during scoreboard maybe)
			if (Gameplay.IsMultiplayer && MPPlayer.Get(senderID) != null)
			{
				if (s_AllowedChannel != AllowedChannels.All)
				{
					//check team
					if (MPPlayer.Get(senderID).TeamNumber != MPLocalPlayer.Instance.TeamNumber)
					{
						//as we need to use this for debug messages during development only return false if not a dev build
						Debug.Log("Message from enemy team has been hidden!");
						MPDebug.Log("Message from enemy team has been hidden!");
						return false;
					}

					//check squad
					if (s_AllowedChannel == AllowedChannels.Squad)
					{
						if (MPPlayer.Get(senderID).SquadNumber != MPLocalPlayer.Instance.SquadNumber)
						{
							//as we need to use this for debug messages during development only return false if not a dev build
							Debug.Log("Message from another squad has been hidden!");
							MPDebug.Log("Message from another squad has been hidden!");
							return false;
						}
					}
				}
			}

			return true;
		}

		/// <summary>
		/// this delegate verifies that a message sender is currently allowed
		/// to post chat messages, and looks for forbidden strings in a message.
		/// can be used for player muting and swear word check, respectively
		/// </summary>
		protected System.Func<int, string, bool> MessageOK = delegate (int senderID, string s)
		{
			// you can implement muting of players by filling the 'mutedplayers'
			// list with id:s from a gui control
			if (m_Chat != null && m_Chat.m_MutedPlayers.Contains(senderID))
				return false;

			// you can prevent undesired words from displaying by returning false here, for example ...
			if (s.Contains("jar jar binks") || s.Contains("f00k"))
				return false;
			// ... although this should be done much more efficiently (use a dictionary)


			// This will make the message silently fail on clients with another team. 
			// TODO: sending client needs different bindings for global and team messages(squad, team, all teams during scoreboard maybe)
			if (Gameplay.IsMultiplayer && MPPlayer.Get(senderID) != null)
			{
				if (s_AllowedChannel != AllowedChannels.All)
				{
					//check team
					if (MPPlayer.Get(senderID).TeamNumber != MPLocalPlayer.Instance.TeamNumber)
					{
						//as we need to use this for debug messages during development only return false if not a dev build
						Debug.Log("Message from enemy team has been hidden!");
						MPDebug.Log("Message from enemy team has been hidden!");
						return false;
					}

					//check squad
					if (s_AllowedChannel == AllowedChannels.Squad)
					{
						if (MPPlayer.Get(senderID).SquadNumber != MPLocalPlayer.Instance.SquadNumber)
						{
							//as we need to use this for debug messages during development only return false if not a dev build
							Debug.Log("Message from another squad has been hidden!");
							MPDebug.Log("Message from another squad has been hidden!");
							return false;
						}
					}
				}
			}

			return true;
		};


		// --- expected components ---

		protected PlayerInputProxy m_FPInput = null;
		protected PlayerInputProxy FPInput
		{
			get
			{
				if (m_FPInput == null)
					m_FPInput = (PlayerInputProxy)Component.FindObjectOfType(typeof(PlayerInputProxy));
				return m_FPInput;
			}
		}

		protected AudioSource m_AudioSource = null;
		protected AudioSource AudioSource
		{
			get
			{
				if (Camera.main != null)
					m_AudioSource = Camera.main.transform.root.GetComponent<AudioSource>(); // NOTE: assuming the main camera is on a typical UCC local player setup here
				else if (GetComponent<AudioSource>() != null)
					m_AudioSource = GetComponent<AudioSource>();
				else
					m_AudioSource = gameObject.AddComponent<AudioSource>();

				return m_AudioSource;
			}
		}

		protected static MPGameChat m_Chat = null;

		/// <summary>
		/// 
		/// </summary>
		protected virtual void Start()
		{
			s_AllowedChannel = m_AllowedChannel;
		}


		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnEnable()
		{
			m_Chat = this;
			EventHandler.RegisterEvent<string, bool>("ChatMessage", AddMessage);
		}


		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnDisable()
		{
			m_Chat = null;
			EventHandler.UnregisterEvent<string, bool>("ChatMessage", AddMessage);
		}

		/// <summary>
		/// 
		/// </summary>
		protected virtual void PlaySound(AudioClip sound)
		{

			if ((sound == null))
				return;

			if ((m_Chat == null))
				return;

			if ((m_Chat.AudioSource == null))
				return;

			if (m_Chat.AudioSource.isPlaying)
				return;

			m_Chat.AudioSource.clip = sound;
			m_Chat.AudioSource.Play();

		}

		/// <summary>
		/// 
		/// </summary>
		protected virtual string GetFormattedPlayerName(int ID)
		{
			return "[" + MPPlayer.GetName(ID) + "] ";
		}

		/// <summary>
		/// 
		/// </summary>
		public static void AddMessage(string message, bool broadcast = true)
		{
			if (m_Chat == null)
				m_Chat = (MPGameChat)Component.FindObjectOfType(typeof(MPGameChat));
			if (m_Chat != null)
			{
				if (broadcast && (PhotonNetwork.NetworkClientState == ClientState.Joined))
					m_Chat.photonView.RPC("AddChatMessage", RpcTarget.AllBuffered, message);
				else if (!broadcast)
					EventHandler.ExecuteEvent<string, MPChatInput.MessageType, bool>("OnGetMessage", message, MPChatInput.MessageType.Debug, false);
				else
					EventHandler.ExecuteEvent<string, MPChatInput.MessageType, bool>("OnGetMessage", message, MPChatInput.MessageType.Player, false);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[PunRPC]
		void AddChatMessage(string message, PhotonMessageInfo info)
		{
			if (!IsMessageClean(info.Sender.ActorNumber, message))
				return;

			EventHandler.ExecuteEvent<string, MPChatInput.MessageType, bool>("OnGetMessage", message, info.Sender.UserId == PhotonNetwork.LocalPlayer.UserId ? MPChatInput.MessageType.Player : MPChatInput.MessageType.Remote, false);
		}


		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnJoinedRoom()
		{
			// enabled = false;//edit as we have voice and use this for connection status for now
			Scheduler.Schedule(1, delegate ()
			{
				MPDebug.Log("Press ENTER to CHAT");
			});

		}

		// -------- GUI styles --------

		protected GUIStyle m_TextStyle = null;  // NOTE: don't use this directly. instead, use its property below
		public GUIStyle TextStyle               // nametag runtime generated GUI style
		{
			get
			{
				if (m_TextStyle == null)
					m_TextStyle = new GUIStyle("Label");
				m_TextStyle.normal.textColor = Color.white;
				return m_TextStyle;
			}
		}
	}
}