namespace FastSkillTeam.UltimateMultiplayer.Pun.UI
{
    using Opsive.Shared.Events;
    using Opsive.UltimateCharacterController.Game;
    using Opsive.UltimateCharacterController.Traits;
    using Opsive.UltimateCharacterController.UI;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    public class MPSpawnPointSelector : CharacterMonitor
    {
        [System.Serializable]
        public class SpawnData
        {
            public Button button;
            public int grouping;
            public SpawnPoint referenceSpawnPoint;
        }
        public Button home;
        [SerializeField] protected bool m_HideIfNoObjectives = true;
        [SerializeField] protected float m_CameraRenderSize = 250f;
        [SerializeField] protected Vector3 m_MapScale = new Vector3(2, 2, 1);
        [SerializeField] protected SpawnData[] m_AvailableSpawnZones;
        [SerializeField] protected bool m_SyncRespawnerGrouping = true;
        private int m_SyncedGrouping = -1;
        private Respawner m_Respawner;
        private bool m_FirstSpawn = true;

        MiniMap.MiniMapHandler m_MiniMapController;

        private Vector3 m_OriginalMapScale;
        private float m_OriginalCamRenderSize;
        protected override void Awake()
        {
            if(m_HideIfNoObjectives == true && (MPMaster.Instance == null || (MPMaster.Instance != null && MPMaster.Instance.CurrentGameType == Shared.GameType.Standard)))
            {
                m_AttachToCamera = false;
                m_ShowUI = false;
                gameObject.SetActive(false);
                return;
            }
            m_ShowUI = true;
            base.Awake();
            for (int i = 0; i < m_AvailableSpawnZones.Length; i++)
            {
                Button b = m_AvailableSpawnZones[i].button;
                int grouping = m_AvailableSpawnZones[i].grouping;
                b.onClick.AddListener(() => OnClick(grouping));
                b.interactable = false;
            }
            home.onClick.AddListener(() => OnClick(-1));
            gameObject.SetActive(MPPlayerSpawner.Instance.SpawnOnJoin == false);
            m_MiniMapController = FindObjectOfType<MiniMap.MiniMapHandler>();
            m_OriginalMapScale = m_MiniMapController.MapScale;
            m_OriginalCamRenderSize = m_MiniMapController.CameraRenderSize;
            m_MiniMapController.CameraRenderSize = m_CameraRenderSize;
            m_MiniMapController.MapScale = m_MapScale;
        }
        protected override bool CanShowUI()
        {
            return m_ShowUI && m_Visible;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < m_AvailableSpawnZones.Length; i++)
            {
                m_AvailableSpawnZones[i].button.onClick.RemoveAllListeners();
            }
            home.onClick.RemoveAllListeners();
        }
        protected override void OnAttachCharacter(GameObject character)
        {
            if (m_Character != null)
            {
                EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(m_Character, "OnDeath", OnDeath);
                EventHandler.UnregisterEvent(m_Character, "OnRespawn", OnRespawn);
                home.onClick.RemoveAllListeners();
                m_Respawner = null;
            }

            base.OnAttachCharacter(character);
            if (m_MiniMapController)
            {
                m_MiniMapController.CameraRenderSize = m_OriginalCamRenderSize;
                m_MiniMapController.MapScale = m_OriginalMapScale;
            }
            if (m_Character == null)
                return;
            m_Respawner = m_Character.GetComponent<Respawner>();
            int group = m_Respawner.Grouping;
            home.onClick.RemoveAllListeners();
            home.onClick.AddListener(() => OnClick(group));
            m_ShowUI = false;
            gameObject.SetActive(CanShowUI());
            EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(m_Character, "OnDeath", OnDeath);
            EventHandler.RegisterEvent(m_Character, "OnRespawn", OnRespawn);
        }
        protected override void Start()
        {
            base.Start();
            enabled = true;
        }
        private void OnRespawn()
        {
            //enabled = false;
            m_MiniMapController.CameraRenderSize = m_OriginalCamRenderSize;
            m_MiniMapController.MapScale = m_OriginalMapScale;
            m_MiniMapController.LockCamPosition(false);
            gameObject.SetActive(false);
        }

        private void OnDeath(Vector3 positon, Vector3 force, GameObject attacker)
        {
            enabled = true;
            gameObject.SetActive(true);
            Utility.Utility.LockCursor = false;
            m_MiniMapController.MapScale = m_MapScale;
            m_MiniMapController.LockCamPosition(true);
            m_MiniMapController.CameraRenderSize = m_CameraRenderSize;
        }


        private void OnClick(int group)
        {
            if (m_FirstSpawn && MPPlayerSpawner.Instance.SpawnOnJoin == false)
            {
                //    MPMaster.Instance.TransmitInitialSpawnInfo(PhotonNetwork.LocalPlayer, MPConnection.Instance.SelectedCharacterIndex);//will work for first player only..
               MPMaster.Instance.photonView.RPC("RequestInitialSpawnInfo", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, MPConnection.Instance.SelectedModelIndex, group);//to avoid using pun props (hackable), we send the SelectedModelIndex 
                m_FirstSpawn = false;
                gameObject.SetActive(false);
                return;
            }

            Debug.Log("Clicked : " + group);

            Vector3 pos = m_Character.transform.position;
            var rot = m_Character.transform.rotation;

            if (SpawnPointManager.GetPlacement(m_Character, group, ref pos, ref rot))
            {
                m_Respawner.CancelRespawn();
                if (m_SyncRespawnerGrouping)
                    m_Respawner.Grouping = group;
                m_Respawner.Respawn(pos, rot, true);
            }

            m_SyncedGrouping = group;

            gameObject.SetActive(false);
        }

        private void Update()
        {
            //TODO: Show all available spawnpoints to relevant team number (update this while displayed in case of objective team changes)
            for (int i = 0; i < m_AvailableSpawnZones.Length; i++)
            {
                Vector3 pos = Vector3.zero;
                var rot = Quaternion.identity;
                int grouping = m_AvailableSpawnZones[i].grouping;
                bool active = m_AvailableSpawnZones[i].referenceSpawnPoint != null && m_AvailableSpawnZones[i].referenceSpawnPoint.gameObject.activeInHierarchy && SpawnPointManager.GetPlacement(m_Character, grouping, ref pos, ref rot);
                //display available...
                m_AvailableSpawnZones[i].button.interactable = active;
                //ensure respawner is up to date, if the respawner grouping is the same as a spawnpoint grouping that was lost (objective takeover) then it needs to be reset.
                if (active == false && m_SyncRespawnerGrouping && m_SyncedGrouping == grouping && m_Respawner.Grouping == m_SyncedGrouping)
                {
                    m_SyncedGrouping = MPTeamManager.GetTeamGrouping(MPLocalPlayer.Instance.TeamNumber);
                    m_Respawner.Grouping = m_SyncedGrouping;
                }
            }
        }
    }
}