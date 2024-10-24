using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class AttackStanceState : BaseState
    {
        // References to other states
        public AttackState attackState;
        public PursueTargetState pursueTargetState;
        public WalkingState walkingState;

        // Time variables to simulate a wind-up before attack
        private readonly float _windUpTime = 1f; // Time AI will wait before attacking
        private float _currentWindUpTime = 0f;    // Timer for the current wind-up

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Early exit if no valid target or target is dead
            if (!IsValidTarget(stateHandler))
            {
                ClearTarget(stateHandler);
                walkingState.startedWalking = false;
                return pursueTargetState;
            }

            // Check the distance to the target
            if (!IsWithinAttackRange(stateHandler, 1f))
            {
                return pursueTargetState; // Return to pursue state if the target is out of range
            }

            // Process attack wind-up timer
            _currentWindUpTime += Time.deltaTime;
            if (_currentWindUpTime >= _windUpTime)
            {
                _currentWindUpTime = 0f; // Reset wind-up timer
                return attackState;      // Transition to attack state after wind-up
            }

            return this; // Remain in attack stance state until wind-up completes
        }

        /// <summary>
        /// Checks if the current target is valid and alive.
        /// </summary>
        private bool IsValidTarget(StateHandler stateHandler)
        {
            GameObject target = stateHandler.currentTarget;
            if (target == null) return false;

            Pedestrian targetPedestrian = target.GetComponent<Pedestrian>();
            return targetPedestrian != null && targetPedestrian.currenHealthStatus == HealthStatus.Alive && targetPedestrian.health > 0;
        }

        /// <summary>
        /// Clears the current target in the StateHandler when it is no longer valid.
        /// </summary>
        private void ClearTarget(StateHandler stateHandler)
        {
            stateHandler.currentTarget = null;
        }

        /// <summary>
        /// Checks if the target is within the specified attack range.
        /// </summary>
        private bool IsWithinAttackRange(StateHandler stateHandler, float attackRange)
        {
            GameObject target = stateHandler.currentTarget;
            if (target == null) return false;

            float distanceToTarget = Vector3.Distance(stateHandler.transform.position, target.transform.position);
            return distanceToTarget <= attackRange;
        }
    }
}
