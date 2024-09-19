/////////////////////////////////////////////////////////////////////////////////
//
//  MPRigidbody.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	Put this script on a rigidbody gameobject to make it sync
//					authoritatively over the network in multiplayer.
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
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{

    using UnityEngine;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using Photon.Pun;
    using Photon.Realtime;
    using Shared.Game;
    using Opsive.Shared.Game;
    using FastSkillTeam.UltimateMultiplayer.Pun.Utility;
    using Opsive.Shared.Events;

    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(Rigidbody))]

    public class MPRigidbody : MonoBehaviourPun, IPunObservable
    {
        [Tooltip("If enabled, shows a transparent capsule collider representing the position updates we are actually receiving from the cloud.")]
        [SerializeField] protected bool m_ShowNetworkPosition = false;
        [SerializeField] protected Vector3 m_NetworkPositionMarkerOffset = Vector3.zero;

        // latest 'real' position, rotation received over network
        protected Vector3 m_NetworkPosition;
        protected Vector3 m_NetworkRotation;

        protected GameObject m_NetworkPositionMarker = null;
        protected GameObject NetworkPositionMarker
        {
            get
            {
                if (m_NetworkPositionMarker == null)
                    m_NetworkPositionMarker = MP3DUtility.DebugPrimitive(PrimitiveType.Sphere, Vector3.one, new Color(1, 1, 1, 0.2f), m_NetworkPositionMarkerOffset, m_Transform);
                return m_NetworkPositionMarker;
            }
        }

        protected Rigidbody m_Rigidbody = null;
        protected Rigidbody Rigidbody
        {
            get
            {
                m_Rigidbody = GetComponent<Rigidbody>();
                return m_Rigidbody;
            }
        }

        // NOTE: for use by derived classes
        protected Collider m_Collider = null;
        protected Collider Collider
        {
            get
            {
                if (m_Collider == null)
                    m_Collider = GetComponent<Collider>();
                return m_Collider;
            }
        }

        protected Collider[] m_Colliders = null;
        protected Collider[] Colliders
        {
            get
            {
                if (m_Colliders == null)
                    m_Colliders = gameObject.GetCachedComponents<Collider>();
                return m_Colliders;
            }
        }

        protected Transform m_Transform = null;
        protected Transform Transform
        {
            get
            {
                if (m_Transform == null)
                    m_Transform = transform;
                return m_Transform;
            }
        }

        // list of every MPRigidbody in the scene
        public static List<MPRigidbody> Instances = new List<MPRigidbody>();



        /// <summary>
        /// 
        /// </summary>
        private void Awake()
        {
            // add every MPRigidbody to this list so we can refresh master
            // control whether it's enabled / active or not
            Instances.Add(this);
            //     m_NetworkPosition = Transform.position;//fix for spawning at zero and not updating
            //    m_NetworkRotation = Transform.rotation.eulerAngles;//^^
        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnEnable()
        {
            m_NetworkPosition = Transform.position;
            m_NetworkRotation = Transform.rotation.eulerAngles;
            SceneManager.sceneLoaded += OnLevelLoad;
            EventHandler.RegisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
            EventHandler.RegisterEvent<Player, GameObject>("OnPlayerLeftRoom", OnPlayerLeftRoom);
        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnDisable()
        {
            SceneManager.sceneLoaded -= OnLevelLoad;
            EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
            EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerLeftRoom", OnPlayerLeftRoom);
        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void Start()
        {

            // set up the photonview to observe this monobehaviour
            photonView.ObservedComponents.Add(this);
            photonView.Synchronization = ViewSynchronization.UnreliableOnChange;

        }

        protected virtual void Update()
        {
            UpdateDebugPrimitive();
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void FixedUpdate()
        {
            if (Gameplay.IsMaster)  // NOTE: instead of 'photonView.isMine', which in this case would result in erratic object movement at start of game
                return;

            // NOTE: When used for platforms the below must all happen in FixedUpdate or non-master clients will
            // be knocked off platforms

            // smooth out movement by performing a plain lerp of the last incoming position and rotation
            Transform.position = Vector3.Lerp(Transform.position, m_NetworkPosition, Time.deltaTime * 15.0f);
            Transform.rotation = Quaternion.Lerp(Transform.rotation, Quaternion.Euler(m_NetworkRotation), Time.deltaTime * 15.0f);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
                WriteToStream(stream, info);
            else ReadFromStream(stream, info);
        }

        protected virtual void WriteToStream(PhotonStream stream, PhotonMessageInfo info)
        {
            stream.SendNext((Vector3)Transform.position);
            stream.SendNext((Vector3)Transform.eulerAngles);
        }

        protected virtual void ReadFromStream(PhotonStream stream, PhotonMessageInfo info)
        {
            m_NetworkPosition = (Vector3)stream.ReceiveNext();
            m_NetworkRotation = (Vector3)stream.ReceiveNext();
        }


        /// <summary>
        /// refreshes master control whenever a master client handover occurs
        /// </summary>
        protected virtual void OnPlayerLeftRoom(Player player, GameObject character)
        {

            RefreshMasterControl();

            Nudge();

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnPlayerEnteredRoom(Player player, GameObject character)
        {

            Nudge();

        }


        /// <summary>
        /// TEMP: nudges all platforms with players on them to force player
        /// positions in sync for when someone joins or leaves
        /// </summary>
        void Nudge()
        {

            if (!Gameplay.IsMaster)
                return;

            foreach (MPPlayer p in MPPlayer.Players.Values)
            {
                if (p == null)
                    continue;
                if (p.GetUltimateCharacterLocomotion.MovingPlatform == Transform)
                {
                    Transform.position += (Vector3.down * 0.1f);
                    SchedulerBase.Schedule(1.0f, () => { Transform.localEulerAngles -= (Vector3.down * 0.1f); });
                }
            }

        }


        /// <summary>
        /// refreshes master control every time you join a room
        /// </summary>
        protected virtual void OnJoinedRoom()
        {
            RefreshMasterControl();
        }


        /// <summary>
        /// enables rigidbody physics on the master client and disables
        /// it on all other machines
        /// </summary>
        protected virtual void RefreshMasterControl()
        {

            Rigidbody.isKinematic = !PhotonNetwork.IsMasterClient;

        }


        /// <summary>
        /// call this to make the master take over all MPRigidbodies in the
        /// scene, regardless of whether they are enabled / active or not
        /// </summary>
        public static void RefreshMasterControlAll()
        {

            foreach (MPRigidbody r in Instances)
            {
                r.RefreshMasterControl();
            }

        }


        /// <summary>
        /// clears list of scene vp_MPRigidbodies in the event of a level load
        /// </summary>
        protected void OnLevelLoad(Scene scene, LoadSceneMode mode)
        {

            RefreshMasterControl();

            Instances.Clear();

        }

        /// <summary>
        /// for testing prediction algorithms
        /// </summary>
        protected virtual void UpdateDebugPrimitive()
        {

            // if applicable, draw a debug capsule showing the last known network position
            if (m_ShowNetworkPosition)
            {
                NetworkPositionMarker.transform.position = m_NetworkPosition;
                if (!NetworkPositionMarker.activeSelf)
                    NetworkPositionMarker.SetActive(true);
            }
            else if (m_NetworkPositionMarker != null)   // NOTE: _not_ polling the _property_ is intended here - we don't want it to initialize
                NetworkPositionMarker.SetActive(false);

        }

    }
}