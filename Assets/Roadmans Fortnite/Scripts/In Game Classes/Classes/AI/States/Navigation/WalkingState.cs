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
        public bool isCrossingRoad;
        
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
            if (NeedsToWaitForTraffic(stateHandler) && !isCrossingRoad)
            {
                return waitingState;
            }

            // Calculate distance to the target
            float distanceToTarget = Vector3.Distance(stateHandler.transform.position, targetPosition);

            // If destination is reached, transition to pathfinder state
            if (distanceToTarget <= _destinationTolerance)
            {
                TransitionToNextPath(stateHandler);
                
                isCrossingRoad = false;
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
        private bool NeedsToWaitForTraffic(StateHandler stateHandler)
        {
            if (_previousWaypointLogger == null || _currentWaypointLogger == null)
            {
                return false;
            }

            if (_previousWaypointLogger.IsRoadCrossPoint && _currentWaypointLogger.IsRoadCrossPoint && !CanCrossRoad(stateHandler))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the AI is performing a valid road crossing that requires waiting.
        /// </summary>
        private bool CanCrossRoad(StateHandler stateHandler)
        {
            // Determine if the AI is primarily moving along the X or Z axis
            Vector3 directionToNextPoint = (_currentWaypointLogger.transform.position - _previousWaypointLogger.transform.position).normalized;
            bool isMovingAlongX = Mathf.Abs(directionToNextPoint.x) > Mathf.Abs(directionToNextPoint.z);

            // Ensure both points are road crossing points
            if (!_currentWaypointLogger.IsRoadCrossPoint || !_previousWaypointLogger.IsRoadCrossPoint)
            {
                return true; // No need to wait if not a road crossing
            }

            // Check if AI just crossed a road
            if (stateHandler.justCrossedRoad)
            {
                return true; // Skip further checks if AI just crossed a road
            }

            // Ensure that this is a valid road crossing scenario (crossing from one side to another, not along the road)
            bool needsToCrossRoad = IsCrossingActive(_previousWaypointLogger, _currentWaypointLogger);

            // Get the traffic light system for the appropriate axis
            _trafficLightSystem = _currentWaypointLogger.NearestTrafficLight;

            if (_trafficLightSystem == null || !needsToCrossRoad)
            {
                return true; // Proceed if no traffic light or not actually crossing
            }

            // Check if the traffic light allows crossing based on the axis movement
            return isMovingAlongX ? _trafficLightSystem.canCrossX : _trafficLightSystem.canCrossY;
        }


        /// <summary>
        /// Determines if the AI is actually crossing a road (not just moving parallel to it).
        /// </summary>
        private bool IsCrossingActive(WaypointLogger previous, WaypointLogger current)
        {
            // Check the movement direction to see if the AI is moving primarily along one axis (indicating a crossing)
            Vector3 directionToNextPoint = (current.transform.position - previous.transform.position).normalized;

            // Determine if the AI is crossing the road by primarily moving along one axis (X or Z)
            bool isCrossingX = Mathf.Abs(directionToNextPoint.x) > Mathf.Abs(directionToNextPoint.z);
            bool isCrossingZ = Mathf.Abs(directionToNextPoint.z) > Mathf.Abs(directionToNextPoint.x);

            // Return true if the AI is crossing in either direction (but not both at once)
            return isCrossingX || isCrossingZ;
        }

        /// <summary>
        /// Draws a gizmo to point out where the pedestrian is moving to / reached
        /// </summary>
        private void DrawRoadCrossingGizmo(WaypointLogger previous, WaypointLogger current, float roadWidthThreshold)
        {
            // Only draw if Gizmos are enabled
            if (previous == null || current == null) return;

            Vector3 previousPos = previous.transform.position;
            Vector3 currentPos = current.transform.position;

            // Draw a line between previous and current path points
            Gizmos.color = Color.red;
            Gizmos.DrawLine(previousPos, currentPos);

            // Draw a square around the previous point to visualize the road width threshold
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(previousPos, new Vector3(roadWidthThreshold, 0.1f, roadWidthThreshold));

            // Draw a square around the current point as well
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(currentPos, new Vector3(roadWidthThreshold, 0.1f, roadWidthThreshold));
        }

        private void OnDrawGizmos()
        {
            // To ensure the Gizmos are drawn
            if (_currentWaypointLogger != null && _previousWaypointLogger != null)
            {
                DrawRoadCrossingGizmo(_previousWaypointLogger, _currentWaypointLogger, 3f);
            }
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
