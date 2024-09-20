/////////////////////////////////////////////////////////////////////////////////
//
//  MPAntiCheatHelper.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	description:	this is a bridge between UCC Ultimate Multiplayer and the third-party
//					'Anti-Cheat Toolkit' by focus (REQUIRED). adding it to the
//					scene will quickstart some chosen cheat detectors and hook up
//					a standard, UCC multiplayer specific response to cheating.
//
//					USAGE:
//						1) install 'Anti-Cheat Toolkit' from Asset Store:
//							https://www.assetstore.unity3d.com/en/#!/content/10395
//						2) to enable the UCC ACTk integration, go to:
//							'Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols'
//							 and add the following string to the text field:
//							;ANTICHEAT
//							as soon as Unity has recompiled, this component will be made
//							functional, and the UCC component state & preset system + remote
//							player wizard will now support Anti-Cheat Toolkit's ObscuredTypes.
//						3) add a new gameobject to the scene, name it i.e. 'AntiCheatHelper'
//							and drag this script onto it to customize cheat detection and
//							response for your game
//						4) see the manual 'Cheat Detection' chapter for info on how to harden
//							UCC and your game using Anti-Cheat Toolkit's ObscuredTypes.
//
/////////////////////////////////////////////////////////////////////////////////

using Photon.Pun;

#if ANTICHEAT
using FastSkillTeam.UltimateMultiplayer.Pun;
using FastSkillTeam.UltimateMultiplayer.Shared.Game;
using CodeStage.AntiCheat.Detectors;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class MPAntiCheatHelper : MonoBehaviourPun
{

	////////////// 'Detectors' section ////////////////

	[System.Serializable]
	public class DetectorSection
	{
		public ObscuredBool SpeedHack = true;
		public ObscuredBool ObscuredCheating = true;
		public ObscuredBool Injection = false;
		public ObscuredBool WallHack = false;
		public ObscuredVector3 WallHackSpawnPos = (Vector3.down * 200);
		/*
		#if UNITY_EDITOR
				[ HelpBox("See the manual 'Cheat Detection' chapter for info on how to work with these detectors.")]From UFPSV1 Update me!
		#endif
		*/
	}
	public DetectorSection Detectors = new DetectorSection();

	////////////// 'Standard Cheat Response' section ////////////////

	[System.Serializable]
	public class StandardCheatResponseSection
	{

		public ObscuredFloat RandomDelay = 0.0f;
		public ObscuredString ChatMessage = "I am a cheater and have attempted a {0}.";
		public ObscuredBool HideMessageLocally = true;
		public ObscuredString ErrorLogMessage = "Detected a {0}.";
		public ObscuredBool HideErrorDialog = false;
		public ObscuredBool Disconnect = true;
		public ObscuredBool PreventReconnect = true;
		public ObscuredBool QuitGame = false;

	}
	public StandardCheatResponseSection StandardCheatResponse = new StandardCheatResponseSection();

	public new ObscuredBool DontDestroyOnLoad = true;

	private const string PLACEHOLDER_HACK_NAME = "sneaky hack";
	protected string m_DetectedHackName;


	/// <summary>
	/// starts the user-specified detectors
	/// </summary>
	void Start()
	{

		m_DetectedHackName = PLACEHOLDER_HACK_NAME;

		if (Detectors.SpeedHack && (SpeedHackDetector.Instance == null))
			SpeedHackDetector.StartDetection(OnSpeedHackDetected);

		if (Detectors.ObscuredCheating && (ObscuredCheatingDetector.Instance == null))
			ObscuredCheatingDetector.StartDetection(OnObscuredCheatingDetected);

		if (Detectors.WallHack && (WallHackDetector.Instance == null))
			WallHackDetector.StartDetection(OnWallHackDetected, Detectors.WallHackSpawnPos);

		if (Detectors.Injection && (InjectionDetector.Instance == null))
			InjectionDetector.Instance.CheatDetected += OnInjectionDetected;

		if (DontDestroyOnLoad)
			Object.DontDestroyOnLoad(transform.root.gameObject);

	}

    private void OnDestroy()
    {
		if (Detectors.Injection && (InjectionDetector.Instance == null))
			InjectionDetector.Instance.CheatDetected -= OnInjectionDetected;
	}

    /// <summary>
    /// default callback for speed hack response
    /// </summary>
    public virtual void OnSpeedHackDetected()
	{
		m_DetectedHackName = "speed hack";
		OnHackDetected();
	}


	/// <summary>
	/// default callback for value hack response
	/// </summary>
	public virtual void OnObscuredCheatingDetected()
	{
		m_DetectedHackName = "value hack";
		OnHackDetected();
	}


	/// <summary>
	/// default callback for wall hack response
	/// </summary>
	public virtual void OnWallHackDetected()
	{
		m_DetectedHackName = "wall hack";
		OnHackDetected();
	}

	/// <summary>
	/// default callback for dll injection response
	/// </summary>
	public virtual void OnInjectionDetected(string reason)
	{
		m_DetectedHackName = string.Format( "dll injection ({0})", reason);
		OnHackDetected();
	}


	/// <summary>
	/// common callback for all hack types. triggers the cheat response after
	/// a random delay
	/// </summary>
	public virtual void OnHackDetected()
	{

		if (StandardCheatResponse.RandomDelay == 0.0f)
			TriggerCheatResponse();
		else
            Opsive.Shared.Game.SchedulerBase.Schedule(Random.Range(0, Mathf.Max(0, StandardCheatResponse.RandomDelay)), TriggerCheatResponse);

	}
	

	/// <summary>
	/// after a cheat has been detected, this method implements one or more
	/// of the following responses: sends a chat message, logs an error
	/// message, clears the local chat, hides the local error dialog,
	/// disconnects from the photon cloud, prevents reconnection until the
	/// game is restarted, quits the game.
	/// </summary>
	protected virtual void TriggerCheatResponse()
	{

		string chatMessage = (string.IsNullOrEmpty(StandardCheatResponse.ChatMessage) ? "" : string.Format(StandardCheatResponse.ChatMessage, m_DetectedHackName));
		string errorMessage = (string.IsNullOrEmpty(StandardCheatResponse.ErrorLogMessage) ? "" : string.Format(StandardCheatResponse.ErrorLogMessage, m_DetectedHackName));
		m_DetectedHackName = PLACEHOLDER_HACK_NAME;

		// if we have a chat message, send it to any script listening to the
		// GlobalEvent 'ChatMessage' (by default: MPGameChat)
		if (!string.IsNullOrEmpty(StandardCheatResponse.ChatMessage))
		{
			Opsive.Shared.Events.EventHandler.ExecuteEvent<string, bool>("ChatMessage", chatMessage, true);
			if(StandardCheatResponse.HideMessageLocally)
				Opsive.Shared.Events.EventHandler.ExecuteEvent("ClearChat");
		}

		// if we should hide the error dialog, send it to any script listening
		// to the Global Event 'EnableErrorDialog' (default: CrashPopup)
		if (StandardCheatResponse.HideErrorDialog)
			Opsive.Shared.Events.EventHandler.ExecuteEvent<bool>("EnableErrorDialog", false);

		// if we have an error message, save it to the unity log file ('output_log.txt')
		// unless 'HideErrorDialog' is true, by default this will also display a 'CrashPopup'
		if (!string.IsNullOrEmpty(errorMessage))
			Debug.LogError(errorMessage);   

		// if we should disconnect, wait one frame to allow time for sending any chat message
		if (StandardCheatResponse.Disconnect)
		{
			if (!string.IsNullOrEmpty(chatMessage))
				Opsive.Shared.Game.Scheduler.Schedule(0, () => MPConnection.Instance.Disconnect());
			else
				MPConnection.Instance.Disconnect();
		}

		// impose a soft kick for the remainder of the session by having MPConnection
		// refuse to connect to Photon Cloud. NOTE: ofcourse this will not stay in effect
		// when the executable is restarted. however, if the cheater was the master he
		// will no longer be master if he logs back into an ongoing game
		if (StandardCheatResponse.PreventReconnect)
		{
			MPConnection.Instance.LogOnTimeOut = 0.0f;
			MPConnection.Instance.MaxConnectionAttempts = -1;
		}

		if (StandardCheatResponse.QuitGame)
			Gameplay.Quit();

	}
}
#elif UNITY_EDITOR

/// <summary>
/// To enable this component:
/// 1) Install 'Anti-Cheat Toolkit' from the Unity Asset Store.
/// 2) From the Unity main menu, go to 'Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols' and add the string ANTICHEAT to the list.
/// For more info on how to use the component, see the manual 'Cheat Detection' chapter.
/// </summary>
public class MPAntiCheatHelper : MonoBehaviourPun
{
}
#endif

