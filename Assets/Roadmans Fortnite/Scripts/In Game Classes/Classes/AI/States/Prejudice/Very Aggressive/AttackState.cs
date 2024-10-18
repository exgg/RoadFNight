using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class AttackState : BaseState
    {
        public float attackCooldown = 1.5f;
        private float currentCooldownTime = 0f;

        [Header("States")]
        public PursueTargetState pursueTargetState;
        public AttackStanceState attackStanceState;
        public WalkingState walkingState;
        
        
        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Ensure there's a valid target
            if (stateHandler.currentTarget == null)
            {
                Debug.LogWarning("No target available for attack or target is dead");
                return attackStanceState;
            }

            // Check if target is dead
            var targetPedestrian = stateHandler.currentTarget.GetComponent<Pedestrian>();
            if (targetPedestrian != null )
            {
                if(targetPedestrian.health <= 0 || targetPedestrian.currenHealthStatus == HealthStatus.Died)
                {
                    Debug.Log("Target is dead, abandoning attack.");
                    stateHandler.currentTarget = null;
                }
            }

            // Rotate to face the target
            FaceTarget(stateHandler);

            // Handle cooldown between attacks
            if (currentCooldownTime > 0)
            {
                currentCooldownTime -= Time.deltaTime;
                return this; // Stay in attack state until cooldown is complete
            }

            // Perform the attack
            PerformAttack(stateHandler.currentTarget, animationHandler, stateHandler);

            // Reset the cooldown after the attack
            currentCooldownTime = attackCooldown;

            return attackStanceState; // Return to attack stance after the attack
        }

        private void PerformAttack(GameObject target, AIAnimationHandler animationHandler, StateHandler stateHandler)
        {
            // Play the attack animation
            Debug.Log("Playing attack animation.");
            animationHandler.PlaySpecificAnimation("Attack");

            // Apply damage to the target
            var targetPedestrian = target.GetComponent<Pedestrian>();
            if (targetPedestrian != null)
            {
                Debug.Log("Attacking target.");
                targetPedestrian.health -= Random.Range(5, 30); // Apply damage
                var targetStateHandler = target.GetComponent<StateHandler>();

                if (!targetStateHandler.currentTarget)
                    targetStateHandler.currentTarget = stateHandler.gameObject;
                
                // Mark target as dead if health falls to 0 or below
                if (targetPedestrian.health <= 0)
                {
                    targetPedestrian.currenHealthStatus = HealthStatus.Died;
                    targetStateHandler.BeginRagdoll();
                    Debug.Log($"{targetPedestrian.name} is dead.");
                    walkingState.startedWalking = false;
                    stateHandler.currentTarget = null;
                }
            }
        }

        private void FaceTarget(StateHandler stateHandler)
        {
            // Rotate towards the target
            Vector3 direction = (stateHandler.currentTarget.transform.position - stateHandler.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            stateHandler.transform.rotation = Quaternion.Slerp(stateHandler.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
