/////////////////////////////////////////////////////////////////////////////////
//
//  ScoreMonitor.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	The ScoreMonitor will update the UI for any external object
//	                messages sending score updates. See > MPDMPlayerStats for example.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.UI
{
    using Opsive.UltimateCharacterController.UI;
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using UnityEngine;
    using UnityEngine.UI;
    using Text = Opsive.Shared.UI.Text;

    /// <summary>
    /// The ScoreMonitor will update the UI for any external object messages sending score updates. See > MPDMPlayerStats for example. 
    /// </summary>
    public class ScoreMonitor : CharacterMonitor
    {
        [Tooltip("A reference to the object that will show the message icon.")]
        [SerializeField] protected Image m_Icon;
        [Tooltip("A reference to the object that will show the message text.")]
        [SerializeField] protected Text m_Text;
        [Tooltip("The length of time that the message should be visible for after scoring points.")]
        [SerializeField] protected float m_ObjectVisiblityDuration = 1.5f;
        [Tooltip("The amount to fade the message after it should no longer be displayed.")]
        [SerializeField] protected float m_ObjectFadeSpeed = 0.05f;

        [System.NonSerialized] private GameObject m_GameObject;
        private bool m_ShouldFade;
        private float m_ObjectAlphaColor;
        private ScheduledEventBase m_ScheduledFade;
        private int m_CurrentScoreValue = 0;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_GameObject = gameObject;
            m_GameObject.SetActive(false);
        }

        /// <summary>
        /// Attaches the monitor to the specified character.
        /// </summary>
        /// <param name="character">The character to attach the monitor to.</param>
        protected override void OnAttachCharacter(GameObject character)
        {
            if (m_Character != null)
                EventHandler.UnregisterEvent<int>(m_Character, "OnScorePoints", OnScorePoints);

            base.OnAttachCharacter(character);

            if (m_Character == null)
                return;

            gameObject.SetActive(CanShowUI());
            EventHandler.RegisterEvent<int>(m_Character, "OnScorePoints", OnScorePoints);
        }

        /// <summary>
        /// An object has been picked up by the character.
        /// </summary>
        /// <param name="objectPickup">The object that was picked up.</param>
        private void OnScorePoints(int score)
        {
            m_ShouldFade = true;
            m_ObjectAlphaColor = 1;
            if (m_ShouldFade)
                Scheduler.Cancel(m_ScheduledFade);

            m_CurrentScoreValue += score;//score should accumulate until fade is done.
            UpdateMessage(m_CurrentScoreValue);
        }
 
        /// <summary>
        /// Updates the text and icon UI.
        /// </summary>
        private void UpdateMessage(int score)
        {
            if (m_Text.gameObject != null)
            {
                m_Text.text = score.ToString();
                m_Text.enabled = !string.IsNullOrEmpty(m_Text.text);
            }
            if (m_Icon != null)
            {
               // m_Icon.sprite = //headshot icon etc;
               m_Icon.enabled = m_Icon.sprite != null;
            }

            // The message will fade if an object is picked up.
            if (m_Text.enabled)
            {
                if (m_Text.gameObject != null)
                {
                    var color = m_Text.color;
                    color.a = 1;
                    m_Text.color = color;
                }
                if (m_Icon != null)
                {
                    var color = m_Icon.color;
                    color.a = 1;
                    m_Icon.color = color;
                }

                if (m_ShouldFade)
                {
                    m_ScheduledFade = Scheduler.Schedule(m_ObjectVisiblityDuration, FadeMessage);
                }
            }

            m_GameObject.SetActive(m_ShowUI && (m_Text.enabled || m_ShouldFade));
        }

        /// <summary>
        /// Fades the message according to the fade speed.
        /// </summary>
        private void FadeMessage()
        {
            m_ObjectAlphaColor = Mathf.Max(m_ObjectAlphaColor - m_ObjectFadeSpeed, 0);
            if (m_ObjectAlphaColor == 0)
            {
                m_CurrentScoreValue = 0;
                m_GameObject.SetActive(false);
                m_ShouldFade = false;
                m_ScheduledFade = null;
                return;
            }

            // Fade the text and icon.
            if (m_Text.gameObject != null)
            {
                var color = m_Text.color;
                color.a = m_ObjectAlphaColor;
                m_Text.color = color;
            }
            if (m_Icon)
            {
                var color = m_Icon.color;
                color.a = m_ObjectAlphaColor;
                m_Icon.color = color;
            }

            // Keep fading until there is nothing left to fade.
            m_ScheduledFade = Scheduler.Schedule(0.01f, FadeMessage);
        }

        /// <summary>
        /// Can the UI be shown?
        /// </summary>
        /// <returns>True if the UI can be shown.</returns>
        protected override bool CanShowUI()
        {
            return base.CanShowUI() && m_ShouldFade;
        }
    }
}