/////////////////////////////////////////////////////////////////////////////////
//
//  MPRushAbility.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	This ability allows the character to arm and disarm rush
//                  objectives and can display UI to show progress of arming or
//                  disarming by utilising the Attribute Manager.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using Opsive.Shared.Game;
    using Opsive.Shared.Input;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateCharacterController.Character.Abilities;
    using UnityEngine;

    [DefaultState("ArmDisarm")]
    [DefaultStartType(AbilityStartType.ButtonDown)]
    [DefaultStopType(AbilityStopType.Manual)]
    [DefaultAllowPositionalInput(true)]
    [DefaultAllowRotationalInput(true)]
    [Opsive.Shared.Utility.Group("Ultimate Multiplayer")]
    public class MPRushAbility : DetectObjectAbilityBase
    {
        [Tooltip("If arming or disarming and the character moves further than this value away from the objective, the action will be cancelled.")]
        [SerializeField] protected float m_CancelDistance = 2f;

        public float CancelDistance { get => m_CancelDistance; set => m_CancelDistance = value; }
        public override bool IgnorePriority => true;
        public override bool IsConcurrent => true;

        private UltimateCharacterLocomotionHandler m_Handler;
        private PlayerInputProxy m_PlayerInput;
        private MPDMObjectiveRush m_RushObjective;

        private bool m_IsHeld = false;
        private float m_Timer = -1;
        private float m_ArmDuration = 0;
        private float m_DisarmDuration = 0;
        private bool m_HasAttribute = false;

        public override string AbilityMessageText
        {
            get
            {
                var message = m_AbilityMessageText;
                if (m_RushObjective != null)
                {
                    message = string.Format(message, m_RushObjective.AbilityMessage());
                }
                return message;
            }
            set { base.AbilityMessageText = value; }
        }

        public override void Awake()
        {
            base.Awake();
            m_Handler = m_GameObject.GetCachedComponent<UltimateCharacterLocomotionHandler>();
            if (!m_Handler)
                Debug.LogError("UltimateCharacterLocomotionHandler is required");

            m_PlayerInput = m_GameObject.GetComponentInChildren<PlayerInputProxy>();
            if (m_AttributeModifier.Attribute != null)
            {
                m_AttributeModifier.EnableModifier(false);
                m_HasAttribute = true;
            }
        }

        public override bool ShouldBlockAbilityStart(Ability startingAbility)
        {
            return false;
        }
        public override bool ShouldStopActiveAbility(Ability activeAbility)
        {
            return false;
        }
        public override bool CanStartAbility()
        {
            return base.CanStartAbility() && m_RushObjective != null && m_RushObjective.CanInteract();
        }
        public override bool CanStopAbility(bool force)
        {
            return m_RushObjective == null || (m_RushObjective != null && m_IsHeld == false);
        }
        protected override bool ValidateObject(GameObject obj, RaycastHit? raycastHit)
        {
            if (!base.ValidateObject(obj, raycastHit))
                return false;

            m_RushObjective = obj.GetCachedParentComponent<MPDMObjectiveRush>();
            if (m_RushObjective == null)
                return false;

            m_ArmDuration = m_RushObjective.ArmDuration;
            m_DisarmDuration = m_RushObjective.DisarmDuration;
            return true;
        }

        protected override void AbilityStarted()
        {
            if (m_Handler == null)
                return;

            base.AbilityStarted();

            m_IsHeld = false;
        }
       
        public override void Update()
        {
            if (m_RushObjective == null)
                return;

            base.Update();

            if (!m_IsHeld)
            {
                for (int i = 0; i < m_InputNames.Length; i++)
                {
                    if (m_PlayerInput.GetButton(m_InputNames[i]))
                    {
                        m_IsHeld = true;
                        int defendingTeam = m_RushObjective.DefendingTeamNumber;
                        m_Timer = Time.time + (MPLocalPlayer.Instance.TeamNumber == defendingTeam ? m_DisarmDuration : m_ArmDuration);
                        if (m_HasAttribute)
                        {
                            m_AttributeModifier.EnableModifier(true);
                            m_AttributeModifier.Attribute.MaxValue = MPLocalPlayer.Instance.TeamNumber == defendingTeam ? m_DisarmDuration : m_ArmDuration;
                        }
                        break;
                    }
                }
            }
            else
            {
                bool pressed = false;

                for (int i = 0; i < m_InputNames.Length; i++)
                {
                    if (m_PlayerInput.GetButton(m_InputNames[i]))
                    {
                        pressed = true;
                        break;
                    }
                }

                if (pressed == false || (Vector3.Distance(m_RushObjective.transform.position, m_Transform.position) > m_CancelDistance))
                {
                    m_IsHeld = false;
                    m_Timer = -1;
                    if (m_HasAttribute)
                    {
                        m_AttributeModifier.Attribute.Value = 0;
                        m_AttributeModifier.EnableModifier(false);
                    }
                    m_RushObjective = null;
                    m_CharacterLocomotion.TryStopAbility(this);
                }
            }

            if (m_Timer > -1)
            {
                if (m_HasAttribute)
                    m_AttributeModifier.Attribute.Value = m_ArmDuration - (m_Timer - Time.time);
                if (Time.time > m_Timer)
                {
                    m_RushObjective.Interact(m_GameObject);
                    m_Timer = -1;
                    m_RushObjective = null;
                    m_CharacterLocomotion.TryStopAbility(this);
                }
            }
        }

        protected override void AbilityStopped(bool force)
        {
            base.AbilityStopped(force);
            if (m_HasAttribute)
                m_AttributeModifier.Attribute.Value = 0;
            m_Timer = -1;
            m_IsHeld = false;
            if (force)
                m_RushObjective = null;
        }
    }
}