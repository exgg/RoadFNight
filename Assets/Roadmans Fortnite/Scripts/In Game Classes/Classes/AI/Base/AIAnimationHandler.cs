using Roadmans_Fortnite.Data.Enums.NPCEnums;
using System;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base
{
    public class AIAnimationHandler : MonoBehaviour
    {
        private Animator _animator;
        
        private void Start()
        {
            _animator = GetComponent<Animator>();
        }

        public void SetWalkingAnimation(string walkingStyle)
        {
            _animator.Play(walkingStyle);
        }

        public void PlaySpecificAnimation(string animName)
        {
            _animator.Play(animName);
        }
        
        public void SetWaitingAnimation(Gender gender)
        {
            _animator.Play(gender == Gender.Female ? "Female_Idle" : "Male_Idle");
        }
        
        public bool IsAnimationPlaying(string animationName)
        {
            // Check if the specified animation is currently playing
            return _animator.GetCurrentAnimatorStateInfo(0).IsName(animationName);
        }
    }
}
