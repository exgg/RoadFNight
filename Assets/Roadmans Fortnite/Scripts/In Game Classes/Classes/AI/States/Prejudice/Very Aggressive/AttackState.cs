using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class AttackState : BaseState
    {
        public float attackCooldown = 1.5f; // Cooldown between attacks
        private float currentCooldownTime = 0f;

        private bool isAttacking = false; // Flag to indicate if an attack animation is playing

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Check if there is a target
            if (stateHandler.currentTarget == null)
            {
                Debug.LogWarning("No target available for attack.");
                return null; // Return to a default state, like idle or pursue
            }

            // Handle cooldown timer between attacks
            if (currentCooldownTime > 0)
            {
                currentCooldownTime -= Time.deltaTime;
                return this; // Stay in attack state, waiting for cooldown
            }

            // If an attack animation is playing, wait until it finishes
            if (isAttacking)
            {
                // Check if the attack animation has finished (assuming you have an animation system that can provide this info)
                if (!animationHandler.IsAnimationPlaying("Attack")) // Adjust this based on your animation system
                {
                    isAttacking = false; // Animation is finished, reset the attack flag
                    currentCooldownTime = attackCooldown; // Reset cooldown
                }

                return this; // Stay in attack state until animation finishes
            }

            // Play attack animation and flag that attack is in progress
            animationHandler.PlaySpecificAnimation("Attack");
            isAttacking = true;

            // Perform the attack logic here (damage, effects, etc.)
            PerformAttack(stateHandler.currentTarget);

            return this; // Remain in attack state until the animation completes and cooldown passes
        }

        private void PerformAttack(GameObject target)
        {
            // Example logic for damaging the target
            var targetPedestrian = target.GetComponent<Pedestrian>();
            if (targetPedestrian != null)
            {
                Debug.Log("Performing attack on target.");
                // Assuming the target has a Health component or a way to reduce health
                targetPedestrian.health -= 10; // Adjust the damage value accordingly
                if (targetPedestrian.health <= 0)
                {
                    targetPedestrian.currenHealthStatus = HealthStatus.Died;
                    Debug.Log($"{targetPedestrian.name} is dead.");
                }
            }
        }
    }
}
