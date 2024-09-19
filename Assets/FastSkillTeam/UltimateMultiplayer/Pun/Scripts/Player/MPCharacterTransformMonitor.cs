/////////////////////////////////////////////////////////////////////////////////
//
//  MPCharacterTransformMonitor.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	This class syncs the UCC remote player movement in multiplayer.
//					it extends functionality with prediction and extrapolation, 
//					ground snapping and movemnt whilst on platforms.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
#if ULTIMATE_SEATING_CONTROLLER
	using FastSkillTeam.UltimateSeatingController;
#endif

	using FastSkillTeam.UltimateMultiplayer.Pun.Utility;
	using Photon.Pun;
	using UnityEngine;
	using Opsive.UltimateCharacterController.Character;
    using Opsive.Shared.Events;
    using Photon.Realtime;

    public class MPCharacterTransformMonitor : MonoBehaviourPun, IPunObservable
	{
		[Tooltip("Prevents the remote player from deviating more than X meters from the latest incoming horizontal network position, and caps vertical transform position to between ground and Y meters above that position")]
		[SerializeField] protected Vector2 m_MaxDeviation = new Vector2(5.0f, 1.0f);

        // ground snap
        [Tooltip("If within ground range and falling, transform will be smooth-snapped to ground")]
		[SerializeField] protected float m_GroundSnapRange = 0.5f;
        [Tooltip("Remote characters check for the ground. This is an optimisation variable for how many ground hits can be assessed at any one time.")]
        [SerializeField] protected int m_MaxGroundHits = 10;
        [Tooltip("Override the default logic and use a custom ground snap mask? \nNOTE: Default logic uses CharacterLayerManager.SolidObjectLayers \nTIP: exclude movable objects for smoother physics around rigidbodies.")]
        [SerializeField] protected bool m_OverrideGroundSnapMask = false;
        [Tooltip("When Override Ground Snap Mask is true, this is the custom mask to use.")]
        [SerializeField] protected LayerMask m_GroundSnapMaskOverride = 1 << 0;

        
		// prediction & lag simulation
		[Tooltip("If enabled, velocity will be used to mask lag by predicting next position every frame.")]
		[SerializeField] protected bool m_PredictPosition = true;
		[Tooltip("With a laggy connection it is possible for remote characters to lerp through walls when using prediction, use safe prediction to prevent that.")]
		[SerializeField] protected bool m_SafePrediction = true;
        [Tooltip("If remote character is within wall range and in a predicted state, prediction will be temporarily disabled in order to prevent the remote character passing through the wall.")]
        [SerializeField] protected float m_SafePredictionCheckRadius = 0.4f;
        [Tooltip("Safe prediction will check for walls and objects. This is an optimisation variable for how many colliders can be assessed at any one time.")]
        [SerializeField] protected int m_MaxSafePredictionHits = 10;
        [Tooltip("Override the default logic and use a custom safe prediction mask? (Only used for prediction, deactivates prediction so the remote character does not lerp through walls on a bad connnection.) \nNOTE: Default logic uses CharacterLayerManager.SolidObjectLayers")]
        [SerializeField] protected bool m_OverrideSafePredictionMask = false;
        [Tooltip("When Override Safe Prediction Mask is true, this is the custom mask to use.")]
        [SerializeField] protected LayerMask m_SafePredictionMaskOverride = 1 << 0;

#if UNITY_EDITOR
        [Tooltip("Public for debugging via the inspector only, you can not set this variable manually.")]
		public bool m_CanPredictPosition = false;
#else
		private bool m_CanPredictPosition = false;
#endif

        [Tooltip("Used to test prediction algorithms by ignoring a certain amount of incoming position updates.")]
		[SerializeField] protected int m_SimulateLostFrames = 0;
		[Tooltip("If enabled, shows a transparent capsule collider representing the position updates we are actually receiving from the cloud.")]
		[SerializeField] protected bool m_ShowNetworkPosition = false;


		// constants
		private const float CLIMB_INTERPOLATION_SPEED = 10.0f;
		private const float PLATFORM_INTERPOLATION_SPEED = 10.0f;

		//Expected components
#if ULTIMATE_SEATING_CONTROLLER
		protected Board m_BoardAbility;
#endif
#if PHOTON_UNITY_NETWORKING
		private PhotonView m_PhotonView;
#endif
#if ULTIMATE_CHARACTER_CONTROLLER_CLIMBING
		private Opsive.UltimateCharacterController.AddOns.Climbing.Climb m_ClimbAbility;
#endif
		private Opsive.UltimateCharacterController.Character.Abilities.Ragdoll m_RagdollAbility;
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER//Reminder: Ask Justin to add my define too, CanPlaceFootstep is only available in multiplayer
		private CharacterFootEffects m_CharacterFootEffects;
#endif
		private GameObject m_GameObject;
		//private MPPlayer m_Player;
		private Transform m_Transform;
		private UltimateCharacterLocomotion m_CharacterLocomotion;
		private CharacterLayerManager m_CharacterLayerManager;

        // MPPlayer Player { get { if (m_Player == null) m_Player = GetComponent<MPPlayer>(); return m_Player; } }

        // variables for interpolated position, rotation and velocity
        protected Vector3 m_SmoothRotation = Vector3.zero;
        protected Vector3 m_CurrentPosition = Vector3.zero;
        protected Vector3 m_CurrentRotation = Vector3.zero;
        protected Vector3 m_CurrentVelocity = Vector3.zero;
        //protected Vector3 m_TempPosition = (Vector3.up * 1000);			// for instantiating new player objects out-of-the-way
        private Vector3 m_TargetPosition = Vector3.zero;
        protected Vector3 m_LastPlatformPos = Vector3.zero;
        private float m_LastPositionY = 0;

        private RaycastHit[] m_GroundHitsBuffer = new RaycastHit[10];
        private Collider[] m_CrashHitsBuffer = new Collider[10];
        private int m_FramesToDrop = 0;                                 // for simulating lost frames.


        protected RaycastHit m_AltitudeHit;                         // used to detect the current ground altitude
        protected float m_GroundAltitude = 0.0f;

        protected int m_RemoteWeaponIndex = 0;

        // animation
        protected bool m_IsAnimated = true;

        // platforms
        private int m_PlatformIDLastFrame = 0;
        private int m_PlatformID = 0;

        // latest 'real' position, rotation and velocity received over network
        protected Vector3 m_NetworkPosition;
        protected Vector3 m_NetworkRotation;
        protected Vector3 m_NetworkScale;
        protected Vector3 m_NetworkVelocity = Vector3.zero;

        protected GameObject m_NetworkPositionMarker = null;
        protected GameObject NetworkPositionMarker
        {
            get
            {
                if (m_NetworkPositionMarker == null)
                    m_NetworkPositionMarker = MP3DUtility.DebugPrimitive(PrimitiveType.Capsule, Vector3.one, new Color(1, 1, 1, 0.2f), Vector3.up, m_Transform);
                return m_NetworkPositionMarker;
            }
        }

        // initialization
        protected bool StartedLerping
        {
            get
            {
                return (Time.time > m_TimeOfBirth + 0.5f);
            }
        }
        protected float m_TimeOfBirth = 0.0f;
        private bool m_InitialSync = true;
        protected bool m_UsingOpsiveSync = false;

        private void Awake()
		{
			m_GameObject = gameObject;
			m_Transform = transform;
			m_CharacterLocomotion = m_GameObject.GetComponent<UltimateCharacterLocomotion>();
			m_CharacterLayerManager = m_GameObject.GetComponent<CharacterLayerManager>();
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER//Reminder: Ask Justin to add my define too, CanPlaceFootstep is only available in multiplayer
			m_CharacterFootEffects = m_GameObject.GetComponent<CharacterFootEffects>();
#endif
			// EventHandler.RegisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
			m_RagdollAbility = m_CharacterLocomotion.GetAbility<Opsive.UltimateCharacterController.Character.Abilities.Ragdoll>();
#if ULTIMATE_CHARACTER_CONTROLLER_CLIMBING
			m_ClimbAbility = m_CharacterLocomotion.GetAbility<Opsive.UltimateCharacterController.AddOns.Climbing.Climb>();
#endif

#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
			m_UsingOpsiveSync = m_GameObject.GetComponent<Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Character.PunCharacterTransformMonitor>();
#endif

#if PHOTON_UNITY_NETWORKING
			m_PhotonView = photonView;
#endif

#if ULTIMATE_SEATING_CONTROLLER
			if (m_CharacterLocomotion != null)
			{
				m_BoardAbility = m_CharacterLocomotion.GetAbility<Board>();
			}
#endif

			EventHandler.RegisterEvent(gameObject, "OnRespawn", OnRespawn);
			EventHandler.RegisterEvent<bool>(gameObject, "OnCharacterImmediateTransformChange", OnImmediateTransformChange);
			EventHandler.RegisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
		}

		/// <summary>
		/// The character has been destroyed.
		/// </summary>
		private void OnDestroy()
		{
			EventHandler.UnregisterEvent(gameObject, "OnRespawn", OnRespawn);
			EventHandler.UnregisterEvent<bool>(gameObject, "OnCharacterImmediateTransformChange", OnImmediateTransformChange);
			EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
		}

		/// <summary>
		/// The character has respawned.
		/// </summary>
		private void OnRespawn()
		{
			m_NetworkPosition = m_Transform.position;
			m_NetworkRotation = m_Transform.rotation.eulerAngles;
		}

		/// <summary>
		/// The character's position or rotation has been teleported.
		/// </summary>
		/// <param name="snapAnimator">Should the animator be snapped?</param>
		private void OnImmediateTransformChange(bool snapAnimator)
		{
			m_NetworkPosition = m_Transform.position;
			m_NetworkRotation = m_Transform.rotation.eulerAngles;
		}

		/// <summary>
		/// A player has entered the room. Initialize the Demo manager to the local player.
		/// </summary>
		/// <param name="player">The Photon Player that entered the room.</param>
		/// <param name="character">The character that the player controls.</param>
		private void OnPlayerEnteredRoom(Player player, GameObject character)
		{
			m_InitialSync = true;
		}

		// Update is called once per frame
		void Update()
		{
			if (m_UsingOpsiveSync)
				return;

			if (m_PhotonView.IsMine)
				return;

            if (m_InitialSync)
            {
				m_Transform.SetPositionAndRotation(m_NetworkPosition, Quaternion.Euler(m_NetworkRotation));
                m_InitialSync = false;
            }

            m_LastPlatformPos = m_CurrentPosition;

			UpdateNetworkValues();

			UpdatePosition();

			UpdateVelocity();

			UpdateRotation();

			UpdateDebugPrimitive();

			//  UpdateFiring();
		}

		/// <summary>
		/// for testing prediction algorithms
		/// </summary>
		protected virtual void UpdateNetworkValues()
		{

			// if we are not simulating lag, always use the most up-to-date
			// position and velocity values
			if (m_SimulateLostFrames < 1)
			{
				m_CurrentPosition = m_NetworkPosition;
				m_CurrentVelocity = m_NetworkVelocity;
				m_CurrentRotation = m_NetworkRotation;
			}
			else
			{
				// we are simulating lag, so only periodically update position and velocity
				if (m_FramesToDrop <= 0)
				{
					m_CurrentPosition = m_NetworkPosition;
					m_CurrentVelocity = m_NetworkVelocity;
					m_CurrentRotation = m_NetworkRotation;
					m_FramesToDrop = m_SimulateLostFrames;
				}
				m_FramesToDrop--;
			}

		}

		/// <summary>
		/// handles on-join position and animation
		/// </summary>
		private void Init()
		{

			if (!StartedLerping)
			{
				//SetPositionAndRotation(LastMasterPosition, LastMasterRotation);
				//if (m_CharacterLocomotion.MovingPlatform != null)
				//	SetPosition(Vector3.zero);
			}

			if (!m_IsAnimated)
				SetAnimated(true);

		}


		/// <summary>
		/// this is used to prevent remote players from spawning with a
		/// falling animation. when they spawn at their temp (free fall)
		/// position, animators will get immediately paused and forcibly
		/// 'grounded' and unpaused as soon as they spawn properly
		/// </summary>
		public virtual void SetAnimated(bool value)
		{

			m_IsAnimated = value;


			m_CharacterLocomotion.Grounded = true;
			/*
						Animator a = m_GameObject.GetComponentInChildren<Animator>();
						if (a != null)
						{
							a.SetBool("IsGrounded", true);
							a.enabled = value;
						}
			*/
		}

		/// <summary>
		/// performs a basic prediction algorithm that is a little bit
		/// more accurate than plain lerp
		/// </summary>
		protected virtual void UpdatePosition()
		{

			// wait if player has not yet been initialized
			if (m_CharacterLocomotion == null)
				return;


			Init();
            m_LastPositionY = m_TargetPosition.y;
            m_TargetPosition = m_Transform.position;

			// don't interpolate boarded players (they should be fixed to vehicle, vehicle should have its own sync)
#if ULTIMATE_SEATING_CONTROLLER
			if (m_BoardAbility != null && m_BoardAbility.IsActive)
				return;
#endif

			// don't interpolate dead or ragdolling characters (they should be handled by local ragdoll physics) unless on a moving platform.
            if (m_CharacterLocomotion.MovingPlatform == null && (m_CharacterLocomotion.Alive == false || (m_RagdollAbility != null && m_RagdollAbility.IsActive)))
                return;

#if ULTIMATE_CHARACTER_CONTROLLER_CLIMBING
            // --- climbing ---
            if (m_ClimbAbility != null && m_ClimbAbility.IsActive)
			{
				m_TargetPosition = Vector3.Lerp(m_TargetPosition, m_CurrentPosition, (StartedLerping ? Time.deltaTime * CLIMB_INTERPOLATION_SPEED : 1));
				m_Transform.position = m_TargetPosition;
				return;
			}
#endif
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER//Reminder: Ask Justin to add my define too, CanPlaceFootstep is only available in multiplayer
			if (m_CharacterFootEffects != null && (m_Transform.position - m_CurrentPosition).sqrMagnitude > 0.01f)
			{
				m_CharacterFootEffects.CanPlaceFootstep = true;
			}
#endif
			// --- platforms ---
			if (m_PlatformIDLastFrame != PlatformID)
			{
				// jumped onto, or off of, a platform
				m_LastPlatformPos = m_CurrentPosition;
				m_PlatformIDLastFrame = PlatformID;

				return;
			}
			else if (m_CharacterLocomotion.MovingPlatform != null)
			{
				// standing on a platform
				m_CurrentPosition = Vector3.Lerp(m_LastPlatformPos, m_CurrentPosition, (StartedLerping ? Time.deltaTime * PLATFORM_INTERPOLATION_SPEED : 1));
				m_TargetPosition = m_CharacterLocomotion.MovingPlatform.TransformPoint(m_CurrentPosition);
				m_Transform.position = m_TargetPosition;
				return;
			}
			m_PlatformIDLastFrame = PlatformID;

			// --- prediction and interpolation ---

			// optionally, perform positional movement prediction for a more accurate
			// position overall, and especially during lagged conditions. we do this
			// by adding last incoming velocity to current position every frame
			if (m_CanPredictPosition)
				m_TargetPosition += m_CurrentVelocity * Time.deltaTime;

			// always interpolate current position with last incoming position. this
			// makes movement smooth and has the character slide gently to the exact
			// network position after stopping
			m_TargetPosition = Vector3.Lerp(m_TargetPosition, m_CurrentPosition,
				(StartedLerping ?
					(Time.deltaTime * (m_CanPredictPosition ? 1.0f : 5.0f))  // stronger lerp with no prediction
					: 1));

			// --- snap cases ---
            StoreGroundAltitude();

			// prevent remote players from lerp-sliding over long distances
			if ((m_MaxDeviation.x > 0.0f) && Vector3.Distance(MP3DUtility.HorizontalVector(m_CurrentPosition), MP3DUtility.HorizontalVector(m_TargetPosition)) > m_MaxDeviation.x)
			{
				m_TargetPosition.x = m_CurrentPosition.x;
				m_TargetPosition.z = m_CurrentPosition.z;
			}

            // prevent body from ever sinking below ground, and from lerp-lagging
            // too much behind the latest incoming network altitude
            m_TargetPosition = MP3DUtility.HorizontalVector(m_TargetPosition) +
								(Vector3.up * Mathf.Clamp(m_TargetPosition.y, m_GroundAltitude,
								((m_MaxDeviation.y > 0) ? (m_CurrentPosition.y + m_MaxDeviation.y) : m_CurrentPosition.y)));

            // prevent remote players from becoming stuck on a ledge/floor while the local is falling
            if ((m_MaxDeviation.y > 0.0f) && (m_TargetPosition.y - m_CurrentPosition.y) > m_MaxDeviation.y)
                m_TargetPosition.y = m_CurrentPosition.y;

            // smooth-snap position to ground if falling while close to ground.
            //float deltaVelocityY = m_TargetPosition.y - m_LastPositionY;//uncomment and use in place of m_CurrentVelocity if you face any issues.
            if ((m_CurrentVelocity.y < 0) && (m_TargetPosition.y < m_GroundAltitude + m_GroundSnapRange))
				m_TargetPosition = Vector3.Lerp(m_TargetPosition,
					 MP3DUtility.HorizontalVector(m_TargetPosition) + (Vector3.up * m_GroundAltitude),
									 Time.deltaTime * 20.0f);

			m_Transform.position = m_TargetPosition;

		}

		/// <summary>
		/// stores an interpolated velocity value for smooth animations
		/// </summary>
		protected virtual void UpdateVelocity()
		{
			//NOTE: in UCCv3 we cannot set the velocity.
			/*            if (m_CharacterLocomotion == null)
							return;

						if (m_CharacterLocomotion.MovingPlatform != null)
							m_CharacterLocomotion.Velocity = (m_CharacterLocomotion.InputVector.x * Vector3.left) + (m_CharacterLocomotion.InputVector.y * Vector3.forward);
						else
							m_CharacterLocomotion.Velocity = (Vector3.Lerp(m_CharacterLocomotion.Velocity, m_CurrentVelocity, Time.deltaTime * 10.0f));*/

		}

		/// <summary>
		/// stores smooth rotation values based on last known rotation
		/// </summary>
		protected virtual void UpdateRotation()
		{
#if ULTIMATE_SEATING_CONTROLLER
			if (m_BoardAbility != null && m_BoardAbility.IsActive)
				return;
#endif
			// NOTE:
			// prediction is not really necessary here, as it is OK for the
			// rotation to drift behind somewhat. movement direction is handled
			// by serialization and firing angle is handled via RPC's.

			// 'LerpAngle' is used for proper full-circle rotation.
			m_SmoothRotation.x = Mathf.LerpAngle(m_SmoothRotation.x, m_CurrentRotation.x, Time.deltaTime * 10.0f);
			m_SmoothRotation.y = Mathf.LerpAngle(m_SmoothRotation.y, m_CurrentRotation.y, Time.deltaTime * 10.0f);
			m_SmoothRotation.z = Mathf.LerpAngle(m_SmoothRotation.z, m_CurrentRotation.z, Time.deltaTime * 10.0f);
			m_Transform.rotation = Quaternion.Euler(m_SmoothRotation);     // rotate collider

		}

		/// <summary>
		/// for testing prediction algorithms
		/// </summary>
		protected virtual void UpdateDebugPrimitive()
		{

			// if applicable, draw a debug capsule showing the last known network position
			if (m_ShowNetworkPosition)
			{
				NetworkPositionMarker.transform.position = m_CurrentPosition;
				if (!NetworkPositionMarker.activeSelf)
					NetworkPositionMarker.SetActive(true);
			}
			else if (m_NetworkPositionMarker != null)   // NOTE: _not_ polling the _property_ is intended here - we don't want it to initialize
				NetworkPositionMarker.SetActive(false);

		}
       
		/// <summary>
		/// gets the height of the ground immediately below this player
		/// </summary>
		protected virtual void StoreGroundAltitude()
		{
            // spherecast from waist and ten meters down to store the current altitude

			// Ensure m_GroundHitsBuffer is pre-allocated with the appropriate size
            if (m_GroundHitsBuffer == null || m_GroundHitsBuffer.Length != m_MaxGroundHits)
                m_GroundHitsBuffer = new RaycastHit[m_MaxGroundHits];

            int count = Physics.SphereCastNonAlloc(new Ray(m_Transform.position + Vector3.up, Vector3.down), 0.4f, m_GroundHitsBuffer, 10.0f, m_OverrideGroundSnapMask ? m_GroundSnapMaskOverride : m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore);
			if (count > m_GroundHitsBuffer.Length)
				Debug.LogWarning($"More hits ({count}) than accounted for... Consider increasing the MaxGroundHits {m_MaxGroundHits}");

			bool hasHit = false;
			float dist = float.MaxValue;
			for (int i = 0; i < m_GroundHitsBuffer.Length; i++)
			{
                if (m_GroundHitsBuffer[i].collider == null) 
					continue;

				bool valid = true;
				for (int v = 0; v < m_CharacterLocomotion.Colliders.Length; v++)
				{
					if (m_GroundHitsBuffer[i].collider == m_CharacterLocomotion.Colliders[v])
					{
						valid = false;
						break;
					}
				}
				if (valid == false)
					continue;

				float d = (m_Transform.position - m_GroundHitsBuffer[i].point).sqrMagnitude;
                if (d < dist)
				{
					m_AltitudeHit = m_GroundHitsBuffer[i];
					dist = d;
				}
				hasHit = true;
			}

			if (hasHit)
				m_GroundAltitude = m_AltitudeHit.point.y;
			else
				m_GroundAltitude = -100000.0f;

		}

        private void FixedUpdate()
        {
            if (m_UsingOpsiveSync)
                return;

            if (m_PhotonView.IsMine)
                return;

            if (m_PredictPosition == false)
			{
				m_CanPredictPosition = false;
				return;
			}

			if (m_SafePrediction == false)
			{
				m_CanPredictPosition = true;
				return;
			}

            // Ensure m_CrashHitsBuffer is pre-allocated with the appropriate size
            if (m_CrashHitsBuffer == null || m_CrashHitsBuffer.Length != m_MaxSafePredictionHits)
                m_CrashHitsBuffer = new Collider[m_MaxSafePredictionHits];

            // spherecast from waist to surrounds to check for walls etc.
            int count = Physics.OverlapSphereNonAlloc(m_Transform.position + (Vector3.up * ((m_SafePredictionCheckRadius * 2) + 0.05f)), m_SafePredictionCheckRadius, m_CrashHitsBuffer, m_OverrideSafePredictionMask ? m_SafePredictionMaskOverride : m_CharacterLayerManager.SolidObjectLayers, QueryTriggerInteraction.Ignore);

            if (count > m_CrashHitsBuffer.Length)
                Debug.LogWarning($"More hits ({count}) than accounted for... Consider increasing the m_MaxSafePredictionHits {m_MaxSafePredictionHits}");

            bool hasHit = false;
            for (int i = 0; i < m_CrashHitsBuffer.Length; i++)
            {
                if(m_CrashHitsBuffer[i] == null) 
					continue;

                bool valid = true;
                for (int v = 0; v < m_CharacterLocomotion.Colliders.Length; v++)
                {
                    if (m_CrashHitsBuffer[i] == m_CharacterLocomotion.Colliders[v])
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid == false)
                    continue;

                hasHit = true;
				break;
            }

			m_CanPredictPosition = !hasHit;

		}

		public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
		{
			//If using Opsives PunCharacterTransformMonitor then leave it to do its job. 
			if (m_UsingOpsiveSync)
				return;
			if (m_CharacterLocomotion == null)
				return;
			if (stream.IsWriting)
			{
				stream.SendNext((int)PlatformID);
				if (PlatformID == 0)
					stream.SendNext((Vector3)m_Transform.position); // send position of player
				else
					stream.SendNext((Vector3)m_CharacterLocomotion.MovingPlatform.InverseTransformPoint(m_Transform.position));    // position of player on current platform

				stream.SendNext((Vector3)m_Transform.eulerAngles);  // send rotation
				stream.SendNext((Vector3)m_CharacterLocomotion.Velocity);  //send direction player is moving
																		   //	stream.SendNext((Vector2)new Vector2(m_CharacterLocomotion.InputVector.x, m_CharacterLocomotion.InputVector.y));   // direction player is trying to move
				m_NetworkPosition = m_Transform.position;
			}
			else
			{
				PlatformID = (int)stream.ReceiveNext();

				m_NetworkPosition = (Vector3)stream.ReceiveNext();

				m_NetworkRotation = (Vector3)stream.ReceiveNext();

				m_NetworkVelocity = (Vector3)stream.ReceiveNext();
			}
		}

		// platform id
		int PlatformID
		{
			get
			{
				m_PlatformID = MPMaster.GetViewIDOfTransform(m_CharacterLocomotion.MovingPlatform);
				return m_PlatformID;
			}
			set
			{

				if (value == m_PlatformIDLastFrame)
					return;

				Transform platform = MPMaster.GetTransformOfViewID(value);
				if ((platform != null)
					&& ((platform.GetComponent<Collider>() != null)
					//&& (Player.IsCloseTo(platform.collider))//On laggy connections this may not be desired...
					))
				{
					m_CharacterLocomotion.SetMovingPlatform(platform);
					m_PlatformID = value;
				}
				else
				{
					m_CharacterLocomotion.SetMovingPlatform(null);
					m_PlatformID = 0;
				}

			}
		}
	}
}