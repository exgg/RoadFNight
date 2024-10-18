using System;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class PursueTargetState : BaseState
    {
        public float attackRange = 2f; // The range at which the AI can start attacking
        
        public InitialPathfinderState initialPathfinderState;
        public FollowingState followingState;
        public AttackStanceState attackStanceState; // Reference to the next state

        
        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Check if the AI has a current target
            if (stateHandler.currentTarget == null)
            {
                Debug.LogWarning("No target assigned for pursuing.");

                return ReturnCorrectState(aiStats);
            }

            // Get the target's position
            GameObject target = stateHandler.currentTarget;

            // Check if the target is alive
            if (aiStats.currenHealthStatus == HealthStatus.Died)
            {
                Debug.Log("Target is dead, abandoning pursuit.");
                return ReturnCorrectState(aiStats);
            }

            // Use the NavMeshAgent to move towards the target
            NavMeshAgent agent = stateHandler.agent;
            agent.SetDestination(target.transform.position);

            // Calculate the distance to the target
            float distanceToTarget = Vector3.Distance(stateHandler.transform.position, target.transform.position);

            // If within attack range, switch to the AttackStance state
            if (distanceToTarget <= attackRange)
            {
                Debug.Log("Within attack range, transitioning to AttackStanceState.");
                return attackStanceState;
            }

            // Play pursuit animation
            //animationHandler.SetWalkingAnimation("Running");

            // Stay in the pursue state if not within range
            return this;
        }

        private BaseState ReturnCorrectState(Pedestrian aiStats)
        {
            if (aiStats.myGroupControlType == GroupControlType.Leader)
                return initialPathfinderState;
            else
                return followingState;
        }
    }
}
