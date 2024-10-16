using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class AttackState : BaseState
    {
        public float attackCooldown = 1.5f; // Cooldown between attacks
        private float currentCooldownTime = 0f;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Ensure there's a valid target
            if (stateHandler.currentTarget == null)
            {
                Debug.LogWarning("No target available for attack.");
                return null; // Return to a default state, like idle or pursue
            }

            // Get the target's Pedestrian component and check if they are dead
            var targetPedestrian = stateHandler.currentTarget.GetComponent<Pedestrian>();
            if (targetPedestrian != null && targetPedestrian.currenHealthStatus == HealthStatus.Died)
            {
                Debug.Log("Target is dead, abandoning attack.");
                stateHandler.currentTarget = null; // Clear the target
                return null; // Return to a neutral state (e.g., idle or search state)
            }

            // Rotate to face the target before attacking
            FaceTarget(stateHandler);

            // Handle cooldown timer between attacks
            if (currentCooldownTime > 0)
            {
                Debug.Log("Waiting for cooldown: " + currentCooldownTime);
                currentCooldownTime -= Time.deltaTime;
                return this; // Stay in attack state, waiting for cooldown
            }

            // Attack the target when the cooldown is complete
            PerformAttack(stateHandler.currentTarget, animationHandler);

            // Reset the cooldown after the attack
            currentCooldownTime = attackCooldown;

            return this; // Stay in attack state to attack again after the cooldown
        }

        private void PerformAttack(GameObject target, AIAnimationHandler animationHandler)
        {
            // Play attack animation
            Debug.Log("Playing attack animation.");
            animationHandler.PlaySpecificAnimation("Attack");

            // Apply damage to the target
            var targetPedestrian = target.GetComponent<Pedestrian>();
            if (targetPedestrian != null)
            {
                Debug.Log("Performing attack on target.");
                targetPedestrian.health -= 10; // Apply damage to the target

                // If the target dies, mark them as dead
                if (targetPedestrian.health <= 0)
                {
                    targetPedestrian.currenHealthStatus = HealthStatus.Died;
                    Debug.Log($"{targetPedestrian.name} is dead.");
                }
            }
        }

        private void FaceTarget(StateHandler stateHandler)
        {
            // Smoothly rotate towards the target
            Vector3 direction = (stateHandler.currentTarget.transform.position - stateHandler.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            stateHandler.transform.rotation = Quaternion.Slerp(stateHandler.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
