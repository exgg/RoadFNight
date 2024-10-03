using System.Collections.Generic;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians;
using UnityEngine;
using UnityEngine.AI;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class FollowingState : BaseState
    {
        // flocking properties
        private readonly float _followDistance = 2f;
        private readonly float _sperationDistance = 2f;
        private readonly float _cohesionWeight = 1.5f;
        private readonly float _seperationWeight = 2f;
        private readonly float _alignmentWeight = 1.0f;
        private readonly float _destinationTolerance = 1f;
        
        // Dont get too far away
        private readonly float _speedModifier = 1.5f;
        private readonly float _maxDistanceFromLeader = 10f;

        public InitialPathfinderState initialPathfindingState;
        
        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            GameObject leaderObject = stateHandler.myLeader;

            if (!leaderObject)
            {
                Debug.LogError("Leader not found reverting to initial pathfinding state");
                return initialPathfindingState;
            }
            
            // Apply flocking behaviour based on leader position and nearby members
            ApplyFlockingBehaviour(stateHandler, leaderObject);
            
            // handle movement and animation within this state 
            MoveFollower(stateHandler, animationHandler, aiStats);
            
            return this; // stay in this state for now until we add idle and other states 
        }
        
        // apply flocking behaviour from given leader
        private void ApplyFlockingBehaviour(StateHandler stateHandler, GameObject leader)
        {
            NavMeshAgent agent = stateHandler.agent;
            
            // Get leader position and direction
            Vector3 leaderPosition = leader.transform.position;
            Vector3 leaderDirection = leader.GetComponent<NavMeshAgent>().velocity.normalized;
            
            // All pedestrian members in group
            PedestrianGroup pedestrianGroup = leader.GetComponentInParent<PedestrianGroup>();

            if (!pedestrianGroup)
            {
                Debug.LogError("Pedestrian Group not logged or assigned");
                return;
            }
            
            // calculate the desired position for the follower based on flocking principles
            Vector3 cohesion = CalculateCohesion(stateHandler, pedestrianGroup.allMembers);
            Vector3 seperation = CalculateSeparation(stateHandler, pedestrianGroup.allMembers);
            Vector3 alignment = leaderDirection * _alignmentWeight;
            
            // calculate final destination based on flocking behaviour
            Vector3 followPosition = leaderPosition - leaderDirection * _followDistance + cohesion + seperation + alignment;

            // setup destination
            agent.destination = followPosition;
            
            // debug for visual feedback on issues or outcome
            Debug.DrawLine(stateHandler.transform.position, followPosition, Color.green);
        }

        private Vector3 CalculateCohesion(StateHandler stateHandler, List<Pedestrian> groupMembers)
        {
            Vector3 centerOfMass = Vector3.zero;
            int count = 0;

            foreach (var member in groupMembers)
            {
                if (member != stateHandler.GetComponent<Pedestrian>()) // logic step to skip self
                {
                    centerOfMass += member.transform.position;
                    count++;
                }
            }

            if (count == 0) return Vector3.zero;

            centerOfMass /= count; // calculate the average position

            return (centerOfMass - stateHandler.transform.position) * _cohesionWeight;
        }
        
        // calculate separation force to avoid collisions with other group members

        private Vector3 CalculateSeparation(StateHandler stateHandler, List<Pedestrian> groupMembers)
        {
            Vector3 separationForce = Vector3.zero;

            foreach (var member in groupMembers)
            {
                if (member != stateHandler.GetComponent<Pedestrian>())
                {
                    Vector3 directionAway = stateHandler.transform.position - member.transform.position;

                    if (directionAway.magnitude < _sperationDistance)
                    {
                        separationForce += directionAway.normalized / directionAway.magnitude; // weight the direction away
                    }
                }
            }

            return separationForce * _seperationWeight;
        }
        
        // move the follower and handle animation 
        private void MoveFollower(StateHandler stateHandler, AIAnimationHandler animationHandler, Pedestrian aiStats)
        {
            NavMeshAgent agent = stateHandler.agent;

            // Calculate distance to the leader
            float distanceToLeader = Vector3.Distance(stateHandler.transform.position, stateHandler.myLeader.transform.position);

            // Speed up the follower if they are too far away from the leader
            if (distanceToLeader > _maxDistanceFromLeader)
            {
                agent.speed *= _speedModifier; // Temporarily increase speed
                animationHandler?.SetWalkingAnimation("Running"); // Use running animation if available
                Debug.Log("Follower is far from leader, speeding up.");
            }
            else
            {
                // Return to leader's speed once close enough
                agent.speed = stateHandler.myLeader.GetComponent<NavMeshAgent>().speed;
                animationHandler?.SetWalkingAnimation(aiStats.CheckWalkingStyle()); // Use normal walking style
                Debug.Log("Follower is within range of leader, maintaining pace.");
            }

            // Check if close enough to the target, then stop the agent and trigger idle
            if (agent.remainingDistance <= _destinationTolerance)
            {
                agent.isStopped = true;
                //animationHandler?.PlayAnimation("Idle"); // Switch to idle animation
                Debug.Log("Follower arrived at position, switching to idle.");
            }
            else
            {
                agent.isStopped = false;
            }
        }
    }
}
