/////////////////////////////////////////////////////////////////////////////////
//
//  MPRigidbodyAdvanced.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	Put this script on a rigidbody gameobject to make it sync
//					authoritatively over the network in multiplayer with prediction.
//
//					unity physics is non-deterministic, meaning if you run the
//					exact same case on different machines you will end up with
//					slightly different object positions. this is undesireable
//					in multiplayer since positions will start to deviate over time.
//
//					this script will restrict physics calculations and moving platform
//					logic to occur on the master client only. on all other clients
//					the object will be remote-controlled by the master. rigidbodies
//					will come to rest in the exact same place on all machines.
//
//					NOTES:
//					1) This rigidbody can only be moved by explosions, projectiles
//						and custom master-side scripting. if you want the player to
//						be able to push it around (or stand on it to make it tilt etc.)
//						then instead use a 'MPPushableRigidbody'
//					2) If you want the player to be able to ride the platform,
//						don't forget to put it in the 'Moving Platform' layer (27),
//						otherwise the player will typically slide off (which might
//						of course also be a desired behavior sometimes)
//					3) Though the rigidbody always comes to rest in the exact same
//						place on all machines, due to network latency its state
//						will differ very slightly on the machines while in motion.
//						it is always possible for a bullet to hit a rigidbody on
//						one machine while missing on another. in these rare cases,
//						what happens on the master client is always what counts!
//                  4) Prediction is likely not desireable if used with platforms,
//                      this is untested.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{

    using UnityEngine;
    using Photon.Pun;
    using Shared.Game;
    using FastSkillTeam.UltimateMultiplayer.Pun.Utility;

    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(Rigidbody))]

    public class MPRigidbodyAdvanced : MPRigidbody, IPunObservable
    {
        // ground snap
        [Tooltip("If within ground range and falling, transform will be smooth-snapped to ground")]
        [SerializeField] protected float m_GroundSnapRange = 0.5f;
        [Tooltip("Remote objects can check for the ground. This is an optimisation variable for how many ground hits can be assessed at any one time.")]
        [SerializeField] protected int m_MaxGroundHits = 10;
        [Tooltip("When Override Ground Snap Mask is true, this is the custom mask to use.")]
        [SerializeField] protected LayerMask m_GroundSnapMask = 1 << 0;

        // prediction & lag simulation
        [Tooltip("Prevents the remote object from deviating more than X meters from the latest incoming horizontal network position, and caps vertical transform position to between ground and Y meters above that position")]
        [SerializeField] protected Vector2 m_MaxDeviation = new Vector2(5.0f, 1.0f);
        [Tooltip("If enabled, velocity will be used to mask lag by predicting next position every frame.")]
        [SerializeField] protected bool m_PredictPosition = true;
        [Tooltip("With a laggy connection it is possible for remote objects to lerp through walls when using prediction, use safe prediction to prevent that.")]
        [SerializeField] protected bool m_SafePrediction = false;
        [Tooltip("If remote object is within wall range and in a predicted state, prediction will be temporarily disabled in order to prevent the remote object passing through the wall.")]
        [SerializeField] protected float m_SafePredictionCheckRadius = 0.4f;
        [Tooltip("Safe prediction will check for walls and objects. This is an optimisation variable for how many colliders can be assessed at any one time.")]
        [SerializeField] protected int m_MaxSafePredictionHits = 10;
        [Tooltip("When Override Safe Prediction Mask is true, this is the custom mask to use.")]
        [SerializeField] protected LayerMask m_SafePredictionMask = 1 << 0;
        [Tooltip("When Override Safe Prediction Mask is true, this is the custom mask to use.")]
        [SerializeField] protected QueryTriggerInteraction m_QueryTriggerInteraction = QueryTriggerInteraction.Ignore;

#if UNITY_EDITOR
        [Tooltip("Public for debugging via the inspector only, you can not set this variable manually.")]
        public bool m_CanPredictPosition = false;
#else
		private bool m_CanPredictPosition = false;
#endif

        [Tooltip("Used to test prediction algorithms by ignoring a certain amount of incoming position updates.")]
        [SerializeField] protected int m_SimulateLostFrames = 0;

        protected Vector3 m_SmoothRotation = Vector3.zero;
        protected Vector3 m_CurrentPosition = Vector3.zero;
        protected Vector3 m_CurrentRotation = Vector3.zero;
        protected Vector3 m_CurrentVelocity = Vector3.zero;
        //protected Vector3 m_TempPosition = (Vector3.up * 1000);			// for instantiating new objects out-of-the-way
        private Vector3 m_TargetPosition = Vector3.zero;
        protected Vector3 m_LastPlatformPos = Vector3.zero;
        private RaycastHit[] m_GroundHitsBuffer = new RaycastHit[10];
        private Collider[] m_CrashHitsBuffer = new Collider[10];
        private int m_FramesToDrop = 0;                                 // for simulating lost frames.
        private float m_LastPositionY = 0;


        // latest 'real' velocity received over network
        protected Vector3 m_NetworkVelocity = Vector3.zero;

        protected RaycastHit m_AltitudeHit;                         // used to detect the current ground altitude
        protected float m_GroundAltitude = 0.0f;

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

        protected override void OnEnable()
        {
            base.OnEnable();
            m_TimeOfBirth = Time.time;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Update()
        {
            //Do not update the base!
            if (Gameplay.IsMaster)  // NOTE: instead of 'photonView.isMine', which in this case would result in erratic object movement at start of game
                return;

            if (m_InitialSync)
            {
                Transform.SetPositionAndRotation(m_NetworkPosition, Quaternion.Euler(m_NetworkRotation));
                m_InitialSync = false;
            }

            m_LastPlatformPos = m_CurrentPosition;

            UpdateNetworkValues();

            UpdatePosition();

            UpdateVelocity();

            UpdateRotation();

            UpdateDebugPrimitive();

        }

        protected override void FixedUpdate()
        {
            if (Gameplay.IsMaster)
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

            // spherecast to surrounds to check for walls etc.
            int count = Physics.OverlapSphereNonAlloc(m_Transform.position, m_SafePredictionCheckRadius, m_CrashHitsBuffer, m_SafePredictionMask, m_QueryTriggerInteraction);

            if (count > m_CrashHitsBuffer.Length)
                Debug.LogWarning($"More hits ({count}) than accounted for... Consider increasing the m_MaxSafePredictionHits {m_MaxSafePredictionHits}");

            bool hasHit = false;
            for (int i = 0; i < m_CrashHitsBuffer.Length; i++)
            {
                if (m_CrashHitsBuffer[i] == null)
                    continue;

                bool valid = true;
                for (int v = 0; v < Colliders.Length; v++)
                {
                    if (m_CrashHitsBuffer[i] == Colliders[v])
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
                Transform.SetPositionAndRotation(m_NetworkPosition, Quaternion.Euler(m_NetworkRotation));
            }
        }


        /// <summary>
		/// performs a basic prediction algorithm that is a little bit
		/// more accurate than plain lerp
		/// </summary>
		protected virtual void UpdatePosition()
        {

            // wait if object has not yet been initialized
            if (Transform == null)
                return;


            Init();
            m_LastPositionY = m_TargetPosition.y;
            m_TargetPosition = m_Transform.position;



            // --- prediction and interpolation ---

            // optionally, perform positional movement prediction for a more accurate
            // position overall, and especially during lagged conditions. we do this
            // by adding last incoming velocity to current position every frame
            if (m_CanPredictPosition)
                m_TargetPosition += m_CurrentVelocity * Time.deltaTime;

            // always interpolate current position with last incoming position. this
            // makes movement smooth and has the object slide gently to the exact
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

            // prevent object from ever sinking below ground, and from lerp-lagging
            // too much behind the latest incoming network altitude
            m_TargetPosition = MP3DUtility.HorizontalVector(m_TargetPosition) +
                                (Vector3.up * Mathf.Clamp(m_TargetPosition.y, m_GroundAltitude,
                                ((m_MaxDeviation.y > 0) ? (m_CurrentPosition.y + m_MaxDeviation.y) : m_CurrentPosition.y)));

            // prevent remote objects from becoming stuck on a ledge/floor while the local is falling
            if ((m_MaxDeviation.y > 0.0f) && (m_TargetPosition.y - m_CurrentPosition.y) > m_MaxDeviation.y)
                m_TargetPosition.y = m_CurrentPosition.y;

            // smooth-snap position to ground if falling while close to ground.
            float deltaVelocityY = m_TargetPosition.y - m_LastPositionY;//m_CurrentVelocity.y uncomment and use in place of m_CurrentVelocity if you face any issues.
            if (m_GroundSnapRange > 0 && (deltaVelocityY < 0) && (m_TargetPosition.y < m_GroundAltitude + m_GroundSnapRange))
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

            if (Rigidbody == null)
                return;

            Rigidbody.velocity = (Vector3.Lerp(Rigidbody.velocity, m_CurrentVelocity, Time.deltaTime * 10.0f));

        }

        /// <summary>
        /// stores smooth rotation values based on last known rotation
        /// </summary>
        protected virtual void UpdateRotation()
        {
            // NOTE:
            // prediction is not really necessary here, as it is OK for the
            // rotation to drift behind somewhat. movement direction is handled
            // by serialization.

            // 'LerpAngle' is used for proper full-circle rotation.
            m_SmoothRotation.x = Mathf.LerpAngle(m_SmoothRotation.x, m_CurrentRotation.x, Time.deltaTime * 10.0f);
            m_SmoothRotation.y = Mathf.LerpAngle(m_SmoothRotation.y, m_CurrentRotation.y, Time.deltaTime * 10.0f);
            m_SmoothRotation.z = Mathf.LerpAngle(m_SmoothRotation.z, m_CurrentRotation.z, Time.deltaTime * 10.0f);
            m_Transform.rotation = Quaternion.Euler(m_SmoothRotation);     // rotate collider

        }


        /// <summary>
        /// for testing prediction algorithms
        /// </summary>
        protected override void UpdateDebugPrimitive()
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
            if (m_MaxGroundHits <= 0)
            {
                m_GroundAltitude = -100000.0f;
                return;
            }
            // spherecast from waist and ten meters down to store the current altitude
            m_GroundHitsBuffer = new RaycastHit[m_MaxGroundHits];
            int count = Physics.SphereCastNonAlloc(new Ray(m_Transform.position + Vector3.up, Vector3.down), 0.4f, m_GroundHitsBuffer, 10.0f, m_GroundSnapMask, QueryTriggerInteraction.Ignore);
            if (count > m_GroundHitsBuffer.Length)
            {
                Debug.LogWarning("More hits than accounted for... Consider increasing the MaxGroundHits");
            }
            bool hasHit = false;
            float dist = float.MaxValue;
            for (int i = 0; i < m_GroundHitsBuffer.Length; i++)
            {
                if (m_GroundHitsBuffer[i].collider == null)
                    continue;

                bool valid = true;
                for (int v = 0; v < Colliders.Length; v++)
                {
                    if (m_GroundHitsBuffer[i].collider == Colliders[v])
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid == false)
                    continue;

                float d = Vector3.Distance(m_Transform.position, m_GroundHitsBuffer[i].point);
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

        /// <summary>
        /// 
        /// </summary>
        protected override void WriteToStream(PhotonStream stream, PhotonMessageInfo info)
        {
            base.WriteToStream(stream, info);
            if (Rigidbody != null)
                stream.SendNext((Vector3)Rigidbody.velocity);
        }
        protected override void ReadFromStream(PhotonStream stream, PhotonMessageInfo info)
        {
            base.ReadFromStream(stream, info);
            if (Rigidbody != null)
                m_NetworkVelocity = (Vector3)stream.ReceiveNext();
        }
    }
}