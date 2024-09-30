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
    }
}
