using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class AttackStanceState : BaseState
    {
        // References to other states
        public AttackState attackState;
        public PursueTargetState pursueTargetState;

        // Time variables to simulate a wind-up before attack
        private readonly float _windUpTime = 1f;  // How long the AI will stay in the attack stance before attacking
        private float _currentWindUpTime = 0f; // Initialize this at zero

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            Debug.Log("I am in the attack stance");
            
            // Reset wind-up time when entering the state to avoid carry-over
            if (_currentWindUpTime == 0f)
            {
                Debug.Log("Entering AttackStanceState: Resetting wind-up time.");
            }

            // If no target is available, return to the pursue state or idle
            if (!stateHandler.currentTarget || stateHandler.currentTarget.GetComponent<Pedestrian>().currenHealthStatus == HealthStatus.Died)
            {
                Debug.Log("No target available, switching back to pursuit.");
                return pursueTargetState;
            }

            // Calculate the distance to the target
            GameObject target = stateHandler.currentTarget;
            float distanceToTarget = Vector3.Distance(stateHandler.transform.position, target.transform.position);

            // If the target is outside the attack range, go back to pursuing the target
            if (distanceToTarget > 3f) 
            {
                Debug.Log("Target moved out of range, returning to PursueTarget state.");
                return pursueTargetState;
            }

            // Wind-up to the attack (wait for the wind-up time to complete before attacking)
            _currentWindUpTime += Time.deltaTime;

            Debug.Log($"CurrentWindupTime {_currentWindUpTime}");            
            
            if (_currentWindUpTime >= _windUpTime)
            {
                Debug.Log("Wind-up complete, transitioning to attack state.");
                _currentWindUpTime = 0f; // Reset wind-up timer for the next time
                return attackState;
            }

            
            return this; // Remain in this state until wind-up completes
        }
    }
}
