using System.Collections.Generic;
using System.Threading.Tasks;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians;
using UnityEngine;
using UnityEngine.AI;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class FollowingState : BaseState
    {
        private readonly float _followDistance = 2f;
        private readonly float _sperationDistance = 2f;
        private readonly float _cohesionWeight = 1.5f;
        private readonly float _seperationWeight = 2f;
        private readonly float _alignmentWeight = 1.0f;
        private readonly float _destinationTolerance = 1f;
        private readonly float _speedModifier = 1.5f;
        private readonly float _maxDistanceFromLeader = 10f;

        public InitialPathfinderState initialPathfindingState;
        public FollowerWaitingState followerWaitingState;
        private bool _isCalculatingFlocking;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            GameObject leaderObject = stateHandler.myLeader;

            if (!leaderObject)
            {
                Debug.LogError("Leader not found reverting to initial pathfinding state");
                return initialPathfindingState;
            }

            // Handle flocking in the background
            if (!_isCalculatingFlocking)
            {
                ApplyFlockingBehaviourAsync(stateHandler, leaderObject);
            }

            // Move follower and manage animation on the main thread
            MoveFollower(stateHandler, animationHandler, aiStats);

            // Check if the follower needs to wait
            if (CheckNeedToWait(stateHandler))
            {
                return followerWaitingState;
            }

            return this; // Stay in this state for now
        }

        private async void ApplyFlockingBehaviourAsync(StateHandler stateHandler, GameObject leader)
        {
            _isCalculatingFlocking = true;

            // Perform heavy flocking calculations on a background thread
            Vector3 followPosition = await Task.Run(() =>
            {
                return CalculateFlockingPosition(stateHandler, leader);
            });

            // Back on the main thread, set the NavMeshAgent's destination
            NavMeshAgent agent = stateHandler.agent;
            agent.destination = followPosition;

            // Reset flag for next frame's calculations
            _isCalculatingFlocking = false;
        }

        private Vector3 CalculateFlockingPosition(StateHandler stateHandler, GameObject leader)
        {
            // Get leader position and direction
            Vector3 leaderPosition = leader.transform.position;
            Vector3 leaderDirection = leader.GetComponent<NavMeshAgent>().velocity.normalized;

            // All pedestrian members in group
            PedestrianGroup pedestrianGroup = leader.GetComponentInParent<PedestrianGroup>();

            if (pedestrianGroup == null)
            {
                Debug.LogError("Pedestrian Group not logged or assigned");
                return leaderPosition; // Fallback to leader's position
            }

            // Calculate flocking behavior
            Vector3 cohesion = CalculateCohesion(stateHandler, pedestrianGroup.allMembers);
            Vector3 separation = CalculateSeparation(stateHandler, pedestrianGroup.allMembers);
            Vector3 alignment = leaderDirection * _alignmentWeight;

            // Calculate final destination based on flocking behavior
            Vector3 followPosition = leaderPosition - leaderDirection * _followDistance + cohesion + separation + alignment;
            return followPosition;
        }

        private Vector3 CalculateCohesion(StateHandler stateHandler, List<Pedestrian> groupMembers)
        {
            Vector3 centerOfMass = Vector3.zero;
            int count = 0;

            foreach (var member in groupMembers)
            {
                if (member != stateHandler.GetComponent<Pedestrian>()) // Skip self
                {
                    centerOfMass += member.transform.position;
                    count++;
                }
            }

            if (count == 0) return Vector3.zero;

            centerOfMass /= count; // Calculate the average position
            return (centerOfMass - stateHandler.transform.position) * _cohesionWeight;
        }

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
                        separationForce += directionAway.normalized / directionAway.magnitude; // Weight the direction away
                    }
                }
            }

            return separationForce * _seperationWeight;
        }

        // Move the follower and handle animation (must stay on main thread)
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
            }
            else
            {
                // Return to leader's speed once close enough
                agent.speed = stateHandler.myLeader.GetComponent<NavMeshAgent>().speed;
                animationHandler?.SetWalkingAnimation(aiStats.CheckWalkingStyle()); // Use normal walking style
            }

            // Check if close enough to the target, then stop the agent
            if (agent.remainingDistance <= _destinationTolerance)
            {
                agent.isStopped = true;
            }
            else
            {
                agent.isStopped = false;
            }
        }

        private bool CheckNeedToWait(StateHandler stateHandler)
        {
            NavMeshAgent agent = stateHandler.agent;
            return agent.remainingDistance <= _destinationTolerance;
        }
    }
}
