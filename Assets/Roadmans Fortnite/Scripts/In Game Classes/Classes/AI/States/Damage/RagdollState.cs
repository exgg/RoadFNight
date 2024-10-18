using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Damage
{
    public class RagdollState : BaseState
    {
        [SerializeField] private GameObject ragdollTarget;
        
        private Rigidbody[] _ragdollBodies;
        private Animator _animator;

        public void OnEnter(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Enable ragdoll mode (disable animator and enable physics)
            
            Debug.Log("Entered Ragdoll");
            _animator = animationHandler.animator;
            if (_animator != null)
            {
                _animator.enabled = false;
            }

            stateHandler.currentTarget = null;
            aiStats.currenHealthStatus = HealthStatus.Died;
            
            EnableRagdoll(stateHandler, animationHandler);
        }

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // AI remains in the ragdoll state, no further logic
            return this;
        }

        private void EnableRagdoll(StateHandler stateHandler, AIAnimationHandler animationHandler)
        {
            // Recursively get all the Rigid bodies from all children (even nested)
            Rigidbody[] ragdollBodies = ragdollTarget.GetComponentsInChildren<Rigidbody>(true);

            // Disable the NavMeshAgent and Animator to prevent movement/animation
            if (stateHandler.agent != null) stateHandler.agent.enabled = false;
            if (animationHandler.animator != null) animationHandler.animator.enabled = false;

            // Loop through all Rigid body components and enable physics (set isKinematic to false)
            foreach (Rigidbody rb in ragdollBodies)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
            }
        }
        public void OnExit(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // logic for exiting ragdoll, if needed
        }
    }
}