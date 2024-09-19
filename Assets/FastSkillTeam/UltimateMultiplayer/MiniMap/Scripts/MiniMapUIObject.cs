////////////////////////////////////////////////////////////////////////////////
//
//  MiniMapUIObject.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//  
//  Description:    This script is part of the MiniMap system.
//                  This is the UI object that will be created for the system
//                  and handle the position and rotation along with clamping to the 
//                  UI map border via specified 2d shape collider.
//            
//  NOTE:           Scene objects should have a MiniMapSceneObject component
//                  attached to them.
//
////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.MiniMap
{
    using UnityEngine;
    using UnityEngine.UI;

    public class MiniMapUIObject : MonoBehaviour
    {
        private MiniMapData m_MiniMapData;
        private MiniMapHandler m_Handler;
        private GameObject m_Owner;
        private Camera m_MapCamera;
        private Image m_Sprite;

        private RectTransform m_ContainerRectTransform;
        private Vector3[] m_Corners;

        private RectTransform m_SpriteRect;

        private Vector2 m_ScreenPosition;
        private Transform m_MiniMapTarget;
        private Vector3Int m_Up;
        private bool m_RotatingMap;
        private bool m_RotateWithObject;

        private Transform m_Root;

        private void FixedUpdate()
        {
            SetPositionAndRotation();
        }

        public void Initialize(MiniMapHandler handler, MiniMapData data, RectTransform containerRectTransform)
        {
            m_Up = data.Up;
            m_Handler = handler;
            m_RotatingMap = m_Handler.RotateMapWithLocalPlayer;
            m_MiniMapData = data;
            m_RotateWithObject = m_MiniMapData.RotateWithObject;
            m_Owner = data.Owner;
            m_MiniMapTarget = m_Owner.transform;


            m_Root = m_Owner.transform.root;


            m_Sprite = gameObject.GetComponent<Image>();
            m_Sprite.sprite = data.Icon;
            m_SpriteRect = m_Sprite.gameObject.GetComponent<RectTransform>();
            m_SpriteRect.sizeDelta = data.Size;
            m_ContainerRectTransform = containerRectTransform;
            transform.SetParent(m_ContainerRectTransform, false);
            SetPositionAndRotation();
        }

        public void SetColor(Color color)
        {
            if(m_Sprite == null)
                m_Sprite = gameObject.GetComponent<Image>();
            m_Sprite.color = color;
        }

        public void SetPositionAndRotation()
        {
            if (m_MiniMapTarget == null)
            {
                m_Handler.UnregisterMapObject(m_Owner, this);
                return;
            }

            SetPosition();
            SetRotation();
        }

        private void SetPosition()
        {
            m_MapCamera = m_Handler.MapCamera;

            if (m_MapCamera == null)
            {
                Vector3 worldAnchor = m_Handler.GetWorldMapAnchor();
                Vector2 uiAnchor = m_Handler.GetUIMapAnchor();
                Vector2 deltaPos = uiAnchor + new Vector2(m_MiniMapTarget.position.x - worldAnchor.x, m_MiniMapTarget.position.z - worldAnchor.z);
                m_SpriteRect.anchoredPosition = deltaPos;

                if (m_Handler.MoveMap && m_Handler.LocalPlayerTransform == m_Root)
                    m_ContainerRectTransform.anchoredPosition = -deltaPos;

                if (m_Handler.LocalPlayerTransform && m_MiniMapData.ClampIcon && Mathf.Abs(Vector3.Distance(m_MiniMapTarget.position, m_Handler.LocalPlayerTransform.position)) < m_MiniMapData.HideDistance)
                    ClampIcon();

                return;
            }
            m_Corners = new Vector3[4];
            m_ContainerRectTransform.GetWorldCorners(m_Corners);
            m_ScreenPosition = RectTransformUtility.WorldToScreenPoint(m_MapCamera, m_MiniMapTarget.position);
            m_SpriteRect.anchoredPosition = m_ScreenPosition - m_ContainerRectTransform.sizeDelta / 2f;
            if (m_Handler.LocalPlayerTransform && m_MiniMapData.ClampIcon && Mathf.Abs(Vector3.Distance(m_MiniMapTarget.position, m_Handler.LocalPlayerTransform.position)) < m_MiniMapData.HideDistance)
                ClampIcon();
        }

        private void SetRotation()
        {
            if (Mathf.Abs(m_Up.y) == 1)
            {
                if (m_RotateWithObject == true)
                {
                    if (m_RotatingMap == true)
                        m_SpriteRect.localEulerAngles = new Vector3(0, 0, m_MiniMapData.Up.y * (m_MiniMapTarget.eulerAngles.y - m_Handler.CameraRotation.z - m_MiniMapTarget.eulerAngles.y) + m_MiniMapData.Rotation);
                    else m_SpriteRect.localEulerAngles = new Vector3(0, 0, -m_MiniMapData.Up.y * (m_MiniMapTarget.eulerAngles.y) + m_MiniMapData.Rotation);
                }
                else m_SpriteRect.localEulerAngles = new Vector3(0, 0, m_SpriteRect.localEulerAngles.y + m_MiniMapData.Rotation);
            }
            else if (Mathf.Abs(m_Up.z) == 1)
            {
                if (m_RotateWithObject == true)
                {
                    if (m_RotatingMap == true)
                        m_SpriteRect.localEulerAngles = new Vector3(0, 0, m_MiniMapData.Up.z * (m_MiniMapTarget.eulerAngles.z - m_Handler.CameraRotation.z - m_MiniMapTarget.eulerAngles.z) + m_MiniMapData.Rotation);
                    else m_SpriteRect.localEulerAngles = new Vector3(0, 0, -m_MiniMapData.Up.z * (m_MiniMapTarget.eulerAngles.z) + m_MiniMapData.Rotation);
                }
                else m_SpriteRect.localEulerAngles = new Vector3(0, 0, m_SpriteRect.localEulerAngles.z + m_MiniMapData.Rotation);
            }
            else
            {
                if (m_RotateWithObject == true)
                {
                    if (m_RotatingMap == true)
                        m_SpriteRect.localEulerAngles = new Vector3(0, 0, m_MiniMapData.Up.x * (m_MiniMapTarget.eulerAngles.x - m_Handler.CameraRotation.z - m_MiniMapTarget.eulerAngles.x) + m_MiniMapData.Rotation);
                    else m_SpriteRect.localEulerAngles = new Vector3(0, 0, -m_MiniMapData.Up.x * (m_MiniMapTarget.eulerAngles.x) + m_MiniMapData.Rotation);
                }
                else m_SpriteRect.localEulerAngles = new Vector3(0, 0, m_SpriteRect.localEulerAngles.x + m_MiniMapData.Rotation);
            }
        }

        private void ClampIcon()
        {
            if (m_ContainerRectTransform == null)
                return;
            //Debug.DrawLine(m_SpriteRect.position, m_ContainerRectTransform.position, Color.green);
            Vector2 direction = m_ContainerRectTransform.position - m_SpriteRect.position;
            RaycastHit2D[] hits = Physics2D.RaycastAll(m_SpriteRect.position, direction);
            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == m_Handler.ShapeCollider)
                    {
                        m_SpriteRect.position = hits[i].point;
                        break;
                    }
                }
            }
        }
    }
}