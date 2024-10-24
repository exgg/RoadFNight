using Roadmans_Fortnite.Data.Enums.NPCEnums;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base
{
    public class AIAnimationHandler : MonoBehaviour
    {
        public Animator animator;
        
        private void Start()
        {
            animator = GetComponent<Animator>();
        }

        public void SetWalkingAnimation(string walkingStyle)
        {
            animator.Play(walkingStyle);
        }

        public void PlaySpecificAnimation(string animName)
        {
            animator.Play(animName);
        }
        
        public void SetWaitingAnimation(Gender gender)
        {
            animator.Play(gender == Gender.Female ? "Female_Idle" : "Male_Idle");
        }
        
        public bool IsAnimationPlaying(string animationName)
        {
            // Check if the specified animation is currently playing
            return animator.GetCurrentAnimatorStateInfo(0).IsName(animationName);
        }
    }
}
