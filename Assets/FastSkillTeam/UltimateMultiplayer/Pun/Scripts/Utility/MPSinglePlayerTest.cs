/////////////////////////////////////////////////////////////////////////////////
//
//  MPSinglePlayerTest.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	A development utility for quick tests where you don't want
//					to spend time connecting to the Photon Cloud. just put this
//					script on a gameobject in the scene and activate it.
//					if 'SpawnMode' is set to 'Prefab' (and a valid player prefab
//					is provided) it will be spawned in the scene. if 'SpawnMode'
//					is set to 'Scene', the first player object found in the scene
//					will be used.
//
//					NOTE: only intended for testing, not for use in an actual game
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun.Utility.Prototyping
{
    using FastSkillTeam.MiniMap;
    using FastSkillTeam.UltimateMultiplayer.Shared.Game;
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.Shared.Input;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateCharacterController.Game;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.Traits;
    using Photon.Pun;
    using UnityEngine;

    public class MPSinglePlayerTest : MonoBehaviour
    {
        [SerializeField] protected GameObject m_Character = null;
        [SerializeField] protected int m_TeamNumber = -1;
        [SerializeField] protected int m_ModelIndex = 0;
        [SerializeField] protected bool m_AddAllItemsToCharacter;
        [SerializeField] protected GameObject[] m_ObjectsToDisable = new GameObject[0];

        //     [SerializeField] protected GameObject m_SpawnPanel = null;
        [SerializeField] protected SpawnMode m_SpawnMode = SpawnMode.Scene;

        public enum SpawnMode
        {
            Scene,
            Prefab
        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void Start()
        {
            if (m_Character == null)
            {
                m_SpawnMode = SpawnMode.Scene;
                m_Character = FindObjectOfType<UltimateCharacterLocomotion>().GameObject;
            }

            if (PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("Warning (" + this + ") This script should not be used when connected. Destroying self. (Did you forget to disable a MPSinglePlayerTest object in a multiplayer map?)");
                Destroy(gameObject);
                return;
            }

            Debug.Log("spawning test player and setting offline mode");

            for (int i = 0; i < m_ObjectsToDisable.Length; i++)
                m_ObjectsToDisable[i].SetActive(false);

            PhotonNetwork.OfflineMode = true;
            Gameplay.IsMultiplayer = false;
            Gameplay.IsMaster = true;

            UltimateCharacterLocomotion[] players = FindObjectsOfType<UltimateCharacterLocomotion>();
            foreach (UltimateCharacterLocomotion player in players)
            {
                if (player != m_Character)
                    player.gameObject.SetActive(false);
            }

            MPMaster[] masters = Component.FindObjectsOfType<MPMaster>() as MPMaster[];
            foreach (MPMaster g in masters)
            {
                if (g.gameObject != gameObject)
                    g.gameObject.SetActive(false);
            }

            // disable demo gui via globalevent since we don't want hard references
            // to code in the demo folder
            EventHandler.ExecuteEvent("DisableMultiplayerGUI");

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;

            GameObject l = null;

            switch (m_SpawnMode)
            {
                case SpawnMode.Prefab:

                    if (!SpawnPointManager.GetPlacement(null, MPTeamManager.GetTeamGrouping(m_TeamNumber), ref pos, ref rot))
                    {
                        Debug.LogErrorFormat("No spawnpoint with grouping {0} could be found!", MPTeamManager.GetTeamGrouping(m_TeamNumber));
                        return;
                    }

                    l = (GameObject)GameObject.Instantiate(m_Character, pos, rot);
                    l.GetComponent<UltimateCharacterLocomotion>().SetPositionAndRotation(pos, rot);
                    break;


                case SpawnMode.Scene:

                    l = m_Character;
                    l.SetActive(true);

                    pos = transform.position;
                    rot = transform.rotation;
                    l.GetComponent<UltimateCharacterLocomotion>().SetPositionAndRotation(pos, rot);

                    break;
            }
            if (l)
            {
                ModelManager m = GetComponent<ModelManager>();
                if (m != null)
                {
                    if (m_ModelIndex > m.AvailableModels.Length - 1)
                        m_ModelIndex = m.AvailableModels.Length - 1;
                    if (m_ModelIndex < 0)
                        m_ModelIndex = 0;

                    if (m.ActiveModelIndex != m_ModelIndex)
                        m.ChangeModels(m.AvailableModels[m_ModelIndex]);
                }
                InitializeCharacter(l);
            }

            // EventHandler.ExecuteEvent("OnPlayerEnteredRoom", PhotonNetwork.LocalPlayer, l);
            /*  if (TestKitPanelPrefab)
              {
                  GameObject testKits = Instantiate(TestKitPanelPrefab, GameObject.Find("CanvasMiniMap").transform);
                  UnityEngine.UI.Button[] buttons = testKits.GetComponentsInChildren<UnityEngine.UI.Button>();
                  for (int i = 0; i < buttons.Length; i++)
                  {
                      buttons[i].onClick.AddListener(delegate { FST_LocalPlayer.Inventory.GetComponent<FastSkillTeam.FST_Kit_Manager>().SetKit(i - 1); }); 
                  }
              }*/
        }

        UltimateCharacterLocomotion m_CharacterLocomotion;
        private Health m_CharacterHealth;
        private Respawner m_CharacterRespawner;
        private PlayerInput m_PlayerInput;


        /// <summary>
        /// Initializes the Demo Manager with the specified character.
        /// </summary>
        /// <param name="character">The character that should be initialized/</param>

        protected void InitializeCharacter(GameObject character)
        {
            m_Character = character;

            if (m_Character == null)
            {
                return;
            }

            m_CharacterLocomotion = m_Character.GetComponent<UltimateCharacterLocomotion>();
            m_CharacterHealth = m_Character.GetComponent<Health>();
            m_CharacterRespawner = m_Character.GetComponent<Respawner>();
            m_PlayerInput = m_Character.GetComponent<PlayerInputProxy>().PlayerInput;

            m_CharacterRespawner.Grouping = m_TeamNumber - 1;

            // Some ViewTypes need a reference to the character bones.
            var cameraController = Opsive.Shared.Camera.CameraUtility.FindCamera(character).GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            Animator characterAnimator;
            var modelManager = character.GetComponent<ModelManager>();
            if (modelManager != null)
            {
                characterAnimator = modelManager.ActiveModel.GetComponent<Animator>();
            }
            else
            {
                characterAnimator = character.GetComponentInChildren<AnimatorMonitor>(true).GetComponent<Animator>();
            }
#if FIRST_PERSON_CONTROLLER
            var transformLookViewType = cameraController.GetViewType<Opsive.UltimateCharacterController.FirstPersonController.Camera.ViewTypes.TransformLook>();
            if (transformLookViewType != null)
            {
                transformLookViewType.MoveTarget = characterAnimator.GetBoneTransform(HumanBodyBones.Head);
                transformLookViewType.RotationTarget = characterAnimator.GetBoneTransform(HumanBodyBones.Hips);
            }
#endif
#if THIRD_PERSON_CONTROLLER
            var lookAtViewType = cameraController.GetViewType<Opsive.UltimateCharacterController.ThirdPersonController.Camera.ViewTypes.LookAt>();
            if (lookAtViewType != null)
            {
                lookAtViewType.Target = characterAnimator.GetBoneTransform(HumanBodyBones.Head);
            }

            // The path is located within the scene. Set it to the spawned character.
            var pseudo3DPath = GameObject.FindObjectOfType<Opsive.UltimateCharacterController.Motion.Path>(true);
            if (pseudo3DPath != null)
            {
                for (int i = 0; i < m_CharacterLocomotion.MovementTypes.Length; ++i)
                {
                    if (m_CharacterLocomotion.MovementTypes[i] is Opsive.UltimateCharacterController.ThirdPersonController.Character.MovementTypes.Pseudo3D)
                    {
                        var pseudo3DMovementType = m_CharacterLocomotion.MovementTypes[i] as Opsive.UltimateCharacterController.ThirdPersonController.Character.MovementTypes.Pseudo3D;
                        pseudo3DMovementType.Path = pseudo3DPath;
                        break;
                    }
                }
            }
#endif
            // Optionally add all the items to the character for debugging.
            if (m_AddAllItemsToCharacter)
            {
                var inventory = m_Character.GetCachedComponent<Inventory>();
                var itemSetManager = m_Character.GetCachedComponent<ItemSetManager>();
                var itemCollection = itemSetManager.ItemCollection;

                for (int i = 0; i < itemCollection.ItemTypes.Length; i++)
                {
                    var itemType = itemCollection.ItemTypes[i];
                    // Add 15 units of all items.
                    inventory.AddItemIdentifierAmount(itemType, 15);
                }
            }



            // The character needs to be assigned to the camera.
            cameraController.SetPerspective(m_CharacterLocomotion.FirstPersonPerspective, true);
            cameraController.Character = m_Character;
            if (m_Character.activeInHierarchy)
            {
                EventHandler.ExecuteEvent(m_Character, "OnCharacterSnapAnimator", true);
            }

            m_CharacterLocomotion.Start();

            MPLocalPlayer p = m_Character.GetComponent<MPLocalPlayer>();
            PhotonView pv = p.photonView;
            if (pv != null)
                pv.OwnerActorNr = 1;
            if (p == null)
                p = m_Character.AddComponent<MPLocalPlayer>();
            p.ID = 1;
            p.TeamNumber = m_TeamNumber;
            MiniMapSceneObject mmc = m_Character.GetComponentInChildren<MiniMapSceneObject>(true);
            if (mmc)
                mmc.Initialize(m_Character, 1, m_TeamNumber);
        }
    }
}
