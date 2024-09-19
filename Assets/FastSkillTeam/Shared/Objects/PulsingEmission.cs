/////////////////////////////////////////////////////////////////////////////////
//
//  PulsingEmission.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	A simple script to pulse the emission color and intensity between
//	                0 and a defined Maximum Intensity.
//	                
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.Shared.Objects
{
    using UnityEngine;

    public class PulsingEmission : MonoBehaviour
    {
        [SerializeField] protected bool m_SyncEnabled = true;
        [SerializeField] protected Color m_EmmissiveColor = Color.white;
        [SerializeField, Range(0f, 1f)] protected float m_MaxIntensity = 1f;
        [SerializeField] protected float m_Speed = 3f;

        private Color m_MinColor = Color.black;
        private Renderer m_Renderer;
        private bool m_Init = false;

        public bool SyncEnabled { get { return m_SyncEnabled; } set { m_SyncEnabled = value; } }
        public float MaxIntensity { get { return m_MaxIntensity; } set { m_MaxIntensity = value; } }
        public float Speed { get { return m_Speed; } set { m_Speed = value; } }

        private bool m_Show = true;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (m_Init)
                return;
            m_Renderer = GetComponent<Renderer>();
        }

        private void OnEnable()
        {
            if (m_SyncEnabled)
                m_Renderer.enabled = true;
        }

        private void OnDisable()
        {
            if (m_SyncEnabled)
                m_Renderer.enabled = false;
        }

        private void Update()
        {
            if (!m_Show)
                return;
            float intensity = Mathf.Lerp(0, m_MaxIntensity, Mathf.PingPong(Time.time * m_Speed, 1.0f));
            Color color = Color.Lerp(m_MinColor, m_EmmissiveColor, intensity);
            m_Renderer.material.SetColor("_EmissionColor", color * intensity);
        }

        public void Pulse(bool enable)
        {
            Initialize();
            if (enable && !m_Show)
            {
                m_Show = true;
            }
            else if (!enable && m_Show)
            {
                m_Show = false;
                m_Renderer.material.SetColor("_EmissionColor", m_MinColor);
            }
        }
    }
}