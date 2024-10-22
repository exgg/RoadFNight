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

        private NavMeshAgent _agent;
        private GameObject _target;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Cache NavMeshAgent and current target references
            _agent = stateHandler.agent;
            _target = stateHandler.currentTarget;

            // Early exit if no target assigned or target is dead
            if (_target == null || IsTargetDead(stateHandler))
            {
                stateHandler.currentTarget = null;
                return ReturnCorrectState(aiStats); // Return to pathfinding or following
            }

            // Calculate the distance to the target
            float distanceToTarget = Vector3.Distance(stateHandler.transform.position, _target.transform.position);

            // If within attack range, transition to AttackStanceState
            if (distanceToTarget <= attackRange)
            {
                return attackStanceState;
            }

            // Set destination and pursue the target
            _agent.SetDestination(_target.transform.position);

            // Play walking animation while pursuing
            if (animationHandler != null)
            {
                string walkingStyle = aiStats.CheckWalkingStyle();
                animationHandler.SetWalkingAnimation(walkingStyle); // Ensure AI is walking while pursuing
            }

            return this; // Remain in the pursue state until in range
        }

        /// <summary>
        /// Determines if the current target is dead.
        /// </summary>
        private bool IsTargetDead(StateHandler stateHandler)
        {
            var targetPedestrian = _target.GetComponent<Pedestrian>();
            return targetPedestrian == null || targetPedestrian.currenHealthStatus == HealthStatus.Died || targetPedestrian.health <= 0;
        }

        /// <summary>
        /// Determines the correct state to return to based on the AI's group control type.
        /// </summary>
        private BaseState ReturnCorrectState(Pedestrian aiStats)
        {
            return aiStats.myGroupControlType == GroupControlType.Leader
                ? initialPathfinderState
                : followingState;
        }
    }
}
