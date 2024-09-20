/////////////////////////////////////////////////////////////////////////////////
//
//  PulsingLight.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	A simple script to pulse a light intensity between 0 and a
//	                defined Maximum Intensity.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.Shared.Objects
{
    using UnityEngine;

    /// <summary>
    /// A simple script to pulse a light intensity between 0 and a defined Maximum Intensity.
    /// </summary>
    public class PulsingLight : MonoBehaviour
    {
        [SerializeField] protected bool m_SyncEnabled = true;
        [SerializeField] protected float m_MaxIntensity = 1f;
        [SerializeField] protected float m_Speed = 3f;

        public bool SyncEnabled { get { return m_SyncEnabled; } set { m_SyncEnabled = value; } }
        public float MaxIntensity { get { return m_MaxIntensity; } set { m_MaxIntensity = value; } }
        public float Speed { get { return m_Speed; } set { m_Speed = value; } }

        private Light m_Light;
        private bool m_Init = false;
        private bool m_Show = true;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (m_Init)
                return;
            m_Light = GetComponent<Light>();
        }

        private void OnEnable()
        {
            if (m_SyncEnabled)
                m_Light.enabled = true;
        }

        private void OnDisable()
        {
            if (m_SyncEnabled)
                m_Light.enabled = false;

            m_Init = false;
        }

        private void Update()
        {
            if (!m_Show)
                return;
            m_Light.intensity = Mathf.Lerp(0, m_MaxIntensity, Mathf.PingPong(Time.time * m_Speed, 1.0f));
        }

        public void Pulse(bool enable)
        {
            Initialize();
            if (enable && !m_Show)
            {
                m_Show = true;
                m_Light.enabled = true;
            }
            else if(!enable && m_Show)
            {
                m_Show = false;
                m_Light.enabled = false;
            }
        }
    }
}