/////////////////////////////////////////////////////////////////////////////////
//
//  MiniMapHandler.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	MiniMapHandler is a system that manages the registration and
//	                unregistration of minimap components. It functions online
//	                without the need to add any PhotonView components.
//	                This component serves as the heart of the system and handles
//	                the initialization and management of minimap UI objects.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.MiniMap
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using Opsive.UltimateCharacterController.UI;

    public class MiniMapHandler : CharacterMonitor
    {
        [Space]
        [Header("Shared Section")]
        [Tooltip("The GameObject that is parent of minimap ui. (Can be null, will default to self)")]
        [SerializeField] protected GameObject m_MiniMapGameObject;
        [Tooltip("Auto initialize this component along with any registered UI Objects?")]
        [SerializeField] protected bool m_AutoInitialize = true;
        [Tooltip("The icon prefab to spawn for registered minimap objects.")]
        [SerializeField] protected GameObject m_IconPrefab;
        [Tooltip("The collider to use for clamping icons.\n TIP: You can use a polygon collider for custom shaped maps!")]
        [SerializeField] protected Collider2D[] m_ShapeColliders = new Collider2D[0];
        [Tooltip("The collider to use for clamping icons.\n TIP: You can use a polygon collider for custom shaped maps!")]
        [SerializeField] protected int m_ShapeColliderIndex = 0;
        [Tooltip("The border image of the minimap.")]
        [SerializeField] protected Sprite m_Border;
        [Tooltip("Set this true, if you want minimap border as the baked minimap.")]
        [SerializeField] protected bool m_UseBorderAsMapTexture;

        [Tooltip("The mask for the minimap display.")]
        [SerializeField] protected Sprite m_MapMask;
        [Tooltip("Opacity of border.")]
        [SerializeField, Range(0, 1)] protected float m_BorderOpacity = 1f;
        [Tooltip("Opacity of minimap.")]
        [SerializeField, Range(0, 1)] protected float m_MapOpacity = 1f;
        [Tooltip("Scale the graphics of the minimap")]
        [SerializeField] protected Vector3 m_MapScale = new Vector3(1f, 1f, 1f);
        [SerializeField] protected bool m_OnlyScaleBorder = false;
#if ULTIMATE_SEATING_CONTROLLER
        [Tooltip("If true any boarded character icons will be hidden.")]
        [SerializeField] protected bool m_HideBoardedIcons = true;
        public bool HideBoardedIcons { get => m_HideBoardedIcons; set => m_HideBoardedIcons = value; }
#endif
        [Space]
        [Header("Baked Map Section")]
        [Tooltip("Set this true if you will not use a render texture or render camera.")]
        [SerializeField] protected bool m_UseBakedMap;
        [Tooltip("Should the map move with the local players character?")]
        [SerializeField] protected bool m_MoveMap = true;
        [Tooltip("Set this image if you will not use a render texture or render camera.")]
        [SerializeField] protected Sprite m_BakedMap;
        [Tooltip("Set this point at the position of a point of your UI map that is relative to World Anchor Reference A.")]
        [SerializeField] protected RectTransform m_UIAnchorReferenceA;
        [Tooltip("Set this point at the position of a point of your UI map that is relative to World Anchor Reference B.")]
        [SerializeField] protected RectTransform m_UIAnchorReferenceB;
        [Tooltip("Set this point at the position in the world that your UI Anchor Reference A will match in the minimap.")]
        [SerializeField] protected Transform m_WorldAnchorReferenceA;
        [Tooltip("Set this point at the position in the world that your UI Anchor Reference B will match in the minimap.")]
        [SerializeField] protected Transform m_WorldAnchorReferenceB;
        [Space]
        [Header("Live Map Section")]
        [SerializeField] protected Material m_MiniMapRenderMaterial;
        [Tooltip("Set which layers to show in the minimap")]
        [SerializeField] protected LayerMask m_RenderLayers;
        [Tooltip("Camera offset from the target")]
        [SerializeField] protected Vector3 m_CameraOffset = new Vector3(0f, 20f, 0f);
        [Tooltip("Camera's orthographic size")]
        [SerializeField] protected float m_CameraRenderSize = 15f;
        [Tooltip("Camera's far clip")]
        [SerializeField] protected float m_CameraFarClipPlane = 30f;
        [Tooltip("Adjust the rotation according to your scene")]
        [SerializeField] protected Vector3 m_CameraRotation = new Vector3(90f, 0f, 0f);
        [Tooltip("If true the camera rotates according to the target")]
        [SerializeField] protected bool m_RotateMapWithLocalPlayer = false;

        public bool AutoInitialize { get => m_AutoInitialize; set => m_AutoInitialize = value; }

        public int ShapeColliderIndex
        {
            get
            {
                return m_ShapeColliderIndex;
            }
            set
            {
                m_ShapeColliderIndex = value;
                if (m_ShapeColliderIndex < 0)
                    m_ShapeColliderIndex = 0;
                if (m_ShapeColliderIndex >= m_ShapeColliders.Length)
                    m_ShapeColliderIndex = m_ShapeColliders.Length - 1;

                for (int i = 0; i < m_ShapeColliders.Length; i++)
                    m_ShapeColliders[i].enabled = m_ShapeColliderIndex == i;
            }
        }
        public LayerMask RenderLayers { get => m_RenderLayers; set => m_RenderLayers = value; }
        public float CameraFarClipPlane { get => m_CameraFarClipPlane; set => m_CameraFarClipPlane = value; }
        public float CameraRenderSize { get => m_CameraRenderSize; set => m_CameraRenderSize = value; }
        public float BorderOpacity { get => m_BorderOpacity; set => m_BorderOpacity = value; }
        public float MapOpacity { get => m_MapOpacity; set => m_MapOpacity = value; }
        public Sprite Border { get => m_Border; set => m_Border = value; }
        public Sprite BakedMap {  get { return m_BakedMap; } set { m_BakedMap = value; m_MiniMapPanelImage.sprite = m_BakedMap; } }
        public Sprite MapMask { get => m_MapMask; set => m_MapMask = value; }
        public Camera MapCamera { get => m_MapCamera; set => m_MapCamera = value; }
        public Vector3 CameraRotation { get => m_CameraRotation; set => m_CameraRotation = value; }
        public Vector3 CameraOffset { get => m_CameraOffset; set => m_CameraOffset = value; }
        public Vector3 MapScale { get => m_MapScale; set => m_MapScale = value; }
        public bool UseBorderAsMapTexture { get => m_UseBorderAsMapTexture; set => m_UseBorderAsMapTexture = value; }
        public bool UseBakedMap { get => m_UseBakedMap; set => m_UseBakedMap = value; }
        public bool MoveMap { get => m_MoveMap; set => m_MoveMap = value; }
        public bool RotateMapWithLocalPlayer { get => m_RotateMapWithLocalPlayer; set => m_RotateMapWithLocalPlayer = value; }
        public Transform LocalPlayerTransform { get => m_LocalPlayerTransform; set => m_LocalPlayerTransform = value; }
        public Collider2D ShapeCollider
        {
            get
            {
                if (m_ShapeColliders == null || (m_ShapeColliders != null && m_ShapeColliders.Length == 0))
                    return null;

                return m_ShapeColliders[m_ShapeColliderIndex];
            }
        }

        public RectTransform UIAnchorReferenceA { get => m_UIAnchorReferenceA; set => m_UIAnchorReferenceA = value; }
        public RectTransform UIAnchorReferenceB { get => m_UIAnchorReferenceB; set => m_UIAnchorReferenceB = value; }
        public Transform WorldAnchorReferenceA { get => m_WorldAnchorReferenceA; set => m_WorldAnchorReferenceA = value; }
        public Transform WorldAnchorReferenceB { get => m_WorldAnchorReferenceB; set => m_WorldAnchorReferenceB = value; }

        private static MiniMapHandler m_Instance = null;
        public static MiniMapHandler Instance { get { if (m_Instance == null) m_Instance = UnityEngine.Object.FindObjectOfType<MiniMapHandler>(); return m_Instance; } }

        private Camera m_MapCamera;

        private RenderTexture m_MiniMapRenderTexture;
        private Transform m_LocalPlayerTransform;

        private GameObject m_MiniMapPanel;
        private Image m_MapPanelMaskImage;
        private Image m_MapPanelBorderImage;
        private Color m_MapColor;
        private Color m_MapBorderColor;

        private RectTransform m_MapPanelRect;
        private RectTransform m_MapPanelMaskRect;

        private Vector2 m_Resolution;
        private Image m_MiniMapPanelImage;
        private float m_Ratio;

        private readonly Dictionary<MiniMapData, MiniMapUIObject> m_OwnerIconMap = new Dictionary<MiniMapData, MiniMapUIObject>();

        private Transform m_OriginalLocalPlayerTransform;
        private bool m_OriginalMoveMap;
        private RectTransform m_RectTransform;
        private bool m_Init = false;
        private bool m_Locked;

        protected override void Awake()
        {
            m_Instance = this;
            m_ShowUI = true;
            base.Awake();
            gameObject.SetActive(true);
            m_OwnerIconMap.Clear();
            m_OriginalMoveMap = m_MoveMap;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_Instance = null;
        }

        protected override void OnAttachCharacter(GameObject character)
        {
            base.OnAttachCharacter(character);
            if (!character)
                return;
            m_LocalPlayerTransform = character.transform;
            m_OriginalLocalPlayerTransform = m_LocalPlayerTransform;
        }

        public void LockCamPosition(bool locked)
        {
            if (locked == m_Locked)
                return;

            m_Locked = locked;

            if (locked)
            {
                m_MoveMap = false;
                m_LocalPlayerTransform = null;
                m_MapPanelRect.anchoredPosition = Vector3.zero;
                if (m_MapCamera)
                    m_MapCamera.transform.position = Vector3.zero + m_CameraOffset;
            }
            else
            {
                m_LocalPlayerTransform = m_OriginalLocalPlayerTransform;
                m_MoveMap = m_OriginalMoveMap;
            }
        }

        public void SetUIAnchors(RectTransform anchorReferenceA, RectTransform anchorReferenceB)
        {
            m_UIAnchorReferenceA = anchorReferenceA;
            m_UIAnchorReferenceB = anchorReferenceB;
        }

        public void SetWorldAnchors(Transform anchorReferenceA, Transform anchorReferenceB)
        {
            m_WorldAnchorReferenceA = anchorReferenceA;
            m_WorldAnchorReferenceB = anchorReferenceB;
        }

        public void SetAnchors(Transform anchorReferenceA, Transform anchorReferenceB, RectTransform uiAnchorReferenceA, RectTransform uiAnchorReferenceB)
        {
            m_WorldAnchorReferenceA = anchorReferenceA;
            m_WorldAnchorReferenceB = anchorReferenceB;
            m_UIAnchorReferenceA = uiAnchorReferenceA;
            m_UIAnchorReferenceB = uiAnchorReferenceB;
        }

        public void Initialize()
        {
            InitializeInternal();

            foreach (var miniMapUIObject in m_OwnerIconMap)
            {
                miniMapUIObject.Value.Initialize(this, miniMapUIObject.Key, m_MapPanelRect);
            }
        }

        public GameObject MiniMapGameObject
        {
            get
            {
                return m_MiniMapGameObject;
            }
            set
            {
                if (m_Init == true && m_MiniMapGameObject == value)
                    return;

                m_MiniMapGameObject = value;

                if (m_AutoInitialize == true)
                {
                    m_Init = false;
                    InitializeInternal();
                }
            }
        }

        private void InitializeInternal()
        {
            if (m_Init)
                return;

            m_Init = true;
            if (m_MiniMapGameObject == null)
                m_MiniMapGameObject = gameObject;

            m_RectTransform = m_MiniMapGameObject.GetComponent<RectTransform>();
            GameObject maskPanelGO = m_RectTransform.GetComponentInChildren<Mask>().gameObject;

            m_MapPanelMaskImage = maskPanelGO.GetComponent<Image>();
            m_MapPanelBorderImage = maskPanelGO.transform.parent.GetComponent<Image>();
            m_MiniMapPanel = maskPanelGO.transform.GetChild(0).gameObject;
            m_MiniMapPanelImage = m_MiniMapPanel.GetComponent<Image>();

            m_MapColor = m_MiniMapPanelImage.color;
            m_MapBorderColor = m_MapPanelBorderImage.color;

            if (m_UseBakedMap == false && m_MapCamera == null)
                m_MapCamera = transform.GetComponentInChildren<Camera>(true);
            if (m_MapCamera != null)
                m_MapCamera.cullingMask = m_RenderLayers;

            m_MapPanelMaskRect = maskPanelGO.GetComponent<RectTransform>();
            m_MapPanelRect = m_MiniMapPanel.GetComponent<RectTransform>();
          //  m_MapPanelRect.anchoredPosition = m_MapPanelMaskRect.anchoredPosition;
            m_Resolution = new Vector2(Screen.width, Screen.height);

            m_MiniMapPanelImage.material = m_MiniMapRenderMaterial;
            m_MiniMapPanelImage.sprite = m_BakedMap;
            m_MiniMapPanelImage.enabled = !m_UseBorderAsMapTexture;
            if (!m_UseBakedMap)
                SetupRenderTexture();

            for (int i = 0; i < m_ShapeColliders.Length; i++)
                m_ShapeColliders[i].enabled = m_ShapeColliderIndex == i;
        }

        private void OnDisable()
        {
            if (m_MiniMapRenderTexture != null)
            {
                if (!m_MiniMapRenderTexture.IsCreated())
                    m_MiniMapRenderTexture.Release();
            }
        }

        private void CalculateRatio()
        {
            Vector3 dist = m_WorldAnchorReferenceA.position - m_WorldAnchorReferenceB.position;
            dist.y = 0;
            float wrldDist = dist.magnitude;
            float mmDist = Mathf.Sqrt(Mathf.Pow((m_UIAnchorReferenceA.anchoredPosition.x - m_UIAnchorReferenceB.anchoredPosition.x), 2)) + Mathf.Sqrt(Mathf.Pow((m_UIAnchorReferenceA.anchoredPosition.y - m_UIAnchorReferenceB.anchoredPosition.y), 2));
            m_Ratio = mmDist / wrldDist;
        }

        public Vector3 GetWorldMapAnchor()
        {
            if (m_WorldAnchorReferenceA == null)
                return Vector3.zero;
            return m_WorldAnchorReferenceA.position * m_Ratio;
        }
        public Vector2 GetUIMapAnchor()
        {
            if (m_UIAnchorReferenceA == null)
                return Vector2.zero;
            return m_UIAnchorReferenceA.anchoredPosition;
        }

        private void LateUpdate()
        {
            if (!m_Init)
                return;

            //Set minimap values
            m_MapPanelMaskImage.sprite = m_MapMask;
            m_MapPanelBorderImage.sprite = m_Border;
            if (m_OnlyScaleBorder == true)
                m_MapPanelBorderImage.rectTransform.localScale = m_MapScale;
            else m_RectTransform.localScale = m_MapScale;
            m_MapBorderColor.a = m_Visible ? m_BorderOpacity : 0;
            m_MapColor.a = m_Visible ? m_MapOpacity : 0;
            m_MapPanelBorderImage.color = m_MapBorderColor;
            m_MiniMapPanelImage.color = m_MapColor;

            if (m_UseBakedMap)
            {
                CalculateRatio();
                return;
            }

            //Set minimappanel size and position, so it updates with size and resolution changes
            m_MapPanelMaskRect.sizeDelta = new Vector2(Mathf.RoundToInt(m_MapPanelMaskRect.sizeDelta.x), Mathf.RoundToInt(m_MapPanelMaskRect.sizeDelta.y));
            m_MapPanelRect.position = m_MapPanelMaskRect.position;
           // m_MapPanelRect.sizeDelta = m_MapPanelMaskRect.sizeDelta;//updating size here can mess up icon offset!
            m_MiniMapPanelImage.enabled = !m_UseBorderAsMapTexture;

            if (Screen.width != m_Resolution.x || Screen.height != m_Resolution.y)
            {
                //Set the render texture
                SetupRenderTexture();
                m_Resolution.x = Screen.width;
                m_Resolution.y = Screen.height;
            }
            //Set the camera
            UpdateRenderCamera();
        }

        private void SetupRenderTexture()
        {
            if (m_MapCamera == null)
                return;

            //Release the old texture, preventing memory leaks
            if (m_MiniMapRenderTexture != null && m_MiniMapRenderTexture.IsCreated())
                m_MiniMapRenderTexture.Release();
            //Setup render texture and resize it.
            //Create a new render texture, as only a runtime created render texture's size can be changed.
            m_MiniMapRenderTexture = new RenderTexture((int)m_MapPanelRect.sizeDelta.x, (int)m_MapPanelRect.sizeDelta.y, 24);
            //Create only creates new render texture in memory, if it is not already created.
            m_MiniMapRenderTexture.Create();

            m_MiniMapRenderMaterial.mainTexture = m_MiniMapRenderTexture;

            m_MapCamera.targetTexture = m_MiniMapRenderTexture;

            //Hack to refresh the minimap panel texture.
            m_MapPanelMaskRect.gameObject.SetActive(false);
            m_MapPanelMaskRect.gameObject.SetActive(true);
        }

        private void UpdateRenderCamera()
        {
            if (m_MapCamera == null)
                return;
            m_MapCamera.orthographicSize = m_CameraRenderSize;
            m_MapCamera.farClipPlane = m_CameraFarClipPlane;

            if (m_LocalPlayerTransform != null)
            {
                m_MapCamera.transform.eulerAngles = m_CameraRotation;

                if (m_RotateMapWithLocalPlayer)
                    m_MapCamera.transform.eulerAngles = m_LocalPlayerTransform.eulerAngles + m_CameraRotation;

                m_MapCamera.transform.position = m_LocalPlayerTransform.position + m_CameraOffset;
            }
            else
            {
                m_MapCamera.transform.eulerAngles = m_CameraRotation;
                m_MapCamera.transform.position = m_CameraOffset;
            }
        }

        public MiniMapUIObject RegisterMapObject(MiniMapData data)
        {
            if (m_AutoInitialize == true)
                InitializeInternal();

            if (!m_OwnerIconMap.TryGetValue(data, out MiniMapUIObject miniMapObject))
            {
                miniMapObject = Instantiate(m_IconPrefab).AddComponent<MiniMapUIObject>();
                m_OwnerIconMap.Add(data, miniMapObject);
                if (m_AutoInitialize == true)
                {
                    miniMapObject.Initialize(this, data, m_MapPanelRect);
                }
            }

            return miniMapObject;
        }

        public void UnregisterMapObject(GameObject owner, MiniMapUIObject miniMapObject)
        {
            foreach (var data in m_OwnerIconMap)
            {
                if (data.Key.Owner == owner)
                {
                    m_OwnerIconMap.Remove(data.Key);
                    break;
                }
            }

            if (miniMapObject)
                Destroy(miniMapObject.gameObject);
        }
    }
}