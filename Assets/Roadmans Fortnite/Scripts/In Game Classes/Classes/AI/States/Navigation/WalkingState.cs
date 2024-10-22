using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.RoadCrossing;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class WalkingState : BaseState
    {
        public PathfinderState pathfinderState;
        public InitialPathfinderState initialPathfinderState;
        public WaitingState waitingState;

        private TrafficLightSystem _trafficLightSystem;
        private WaypointLogger _currentWaypointLogger;
        private WaypointLogger _previousWaypointLogger;

        // Tolerance level for floating point precision and multiple agents attempting to reach point
        private readonly float _destinationTolerance = 2f;

        public bool startedWalking;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Ensure the current path point is set, or return to pathfinding
            if (!stateHandler.currentPathPoint)
            {
                startedWalking = false;
                return initialPathfinderState;
            }

            // Cache the agent destination to reduce repetitive calls
            Vector3 targetPosition = stateHandler.currentPathPoint.transform.position;
            stateHandler.agent.destination = targetPosition;

            // Check if walking just started and set animation
            if (!startedWalking)
            {
                StartWalking(aiStats, animationHandler);
            }

            // Cache the logger components only once for performance
            CacheWaypointLoggers(stateHandler);

            // Check if the AI needs to wait at a road crossing
            if (NeedsToWaitForTraffic())
            {
                return waitingState;
            }

            // Calculate distance to the target
            float distanceToTarget = Vector3.Distance(stateHandler.transform.position, targetPosition);

            // If destination is reached, transition to pathfinder state
            if (distanceToTarget <= _destinationTolerance)
            {
                TransitionToNextPath(stateHandler);
                return pathfinderState;
            }

            // Continue walking to the destination
            return this;
        }

        /// <summary>
        /// Sets the walking animation and marks walking as started.
        /// </summary>
        private void StartWalking(Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            string walkingStyle = aiStats.CheckWalkingStyle();
            animationHandler.SetWalkingAnimation(walkingStyle);
            startedWalking = true;
        }

        /// <summary>
        /// Caches the WaypointLogger components to avoid repetitive GetComponent calls.
        /// </summary>
        private void CacheWaypointLoggers(StateHandler stateHandler)
        {
            if (stateHandler.currentPathPoint != null && _currentWaypointLogger == null)
            {
                _currentWaypointLogger = stateHandler.currentPathPoint.GetComponent<WaypointLogger>();
            }

            if (stateHandler.previousPathPoint != null && _previousWaypointLogger == null)
            {
                _previousWaypointLogger = stateHandler.previousPathPoint.GetComponent<WaypointLogger>();
            }
        }

        /// <summary>
        /// Checks if the AI needs to wait for a traffic light based on the road crossing points.
        /// </summary>
        private bool NeedsToWaitForTraffic()
        {
            if (_previousWaypointLogger == null || _currentWaypointLogger == null)
            {
                return false;
            }

            if (_previousWaypointLogger.IsRoadCrossPoint && _currentWaypointLogger.IsRoadCrossPoint && !CanCrossRoad())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the AI can cross the road based on the traffic light system.
        /// </summary>
        private bool CanCrossRoad()
        {
            // Determine if the AI is primarily moving along the X or Z axis
            Vector3 directionToNextPoint = (_currentWaypointLogger.transform.position - _previousWaypointLogger.transform.position).normalized;
            bool isMovingAlongX = Mathf.Abs(directionToNextPoint.x) > Mathf.Abs(directionToNextPoint.z);

            // Get the traffic light system for the appropriate axis
            _trafficLightSystem = _currentWaypointLogger.NearestTrafficLight;

            if (_trafficLightSystem == null)
            {
                return true; // Allow to proceed if no traffic light system is found
            }

            return isMovingAlongX ? _trafficLightSystem.canCrossX : _trafficLightSystem.canCrossY;
        }

        /// <summary>
        /// Transitions to the next path point and resets walking.
        /// </summary>
        private void TransitionToNextPath(StateHandler stateHandler)
        {
            stateHandler.previousPathPoint = stateHandler.currentPathPoint;
            startedWalking = false;
            _currentWaypointLogger = null;
            _previousWaypointLogger = null; // Reset cached loggers for the next path
        }

        public void ResetWalking()
        {
            startedWalking = false;
        }
    }
}
