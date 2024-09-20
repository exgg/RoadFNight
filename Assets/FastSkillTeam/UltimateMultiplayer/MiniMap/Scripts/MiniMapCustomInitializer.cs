namespace FastSkillTeam.MiniMap
{
    using UnityEngine;

    public class MiniMapCustomInitializer : MonoBehaviour
    {
        [SerializeField] protected GameObject m_MiniMapGameObject;
        [SerializeField] protected RectTransform m_MinimapUIAnchorA;
        [SerializeField] protected RectTransform m_MinimapUIAnchorB;
        [SerializeField] protected RectTransform m_MinimapWorldAnchorA;
        [SerializeField] protected RectTransform m_MinimapWorldAnchorB;
        private MiniMapHandler m_MiniMapHandler;
        public void InitializeMinimap()
        {
            if (m_MiniMapHandler == null)
                m_MiniMapHandler = MiniMapHandler.Instance;
            if (m_MiniMapHandler == null)
                return;

            m_MiniMapHandler.SetUIAnchors(m_MinimapUIAnchorA, m_MinimapUIAnchorB);

            if (m_MinimapWorldAnchorA != null)
                m_MiniMapHandler.WorldAnchorReferenceA = m_MinimapWorldAnchorA;

            if (m_MinimapWorldAnchorB != null)
                m_MiniMapHandler.WorldAnchorReferenceB = m_MinimapWorldAnchorB;

            if (m_MiniMapGameObject != null)
                m_MiniMapHandler.MiniMapGameObject = m_MiniMapGameObject;
        }
    }
}