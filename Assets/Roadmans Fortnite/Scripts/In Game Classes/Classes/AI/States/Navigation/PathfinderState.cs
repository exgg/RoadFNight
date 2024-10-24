using System.Collections.Generic;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class PathfinderState : BaseState
    {
        public WalkingState walkingState;
        private const float FloatingPointThreshold = 3f; // Minimum distance to consider two points different
        private GameObject _lastChosenPathPoint = null; // Temporary reference for the last chosen path point

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Choose a new random path
            GameObject randomPath = ChooseRandomPath(stateHandler);

            if (randomPath != null)
            {
                // Set the selected path as the new current path point
                stateHandler.currentPathPoint = randomPath;
                _lastChosenPathPoint = stateHandler.previousPathPoint; // Track the last chosen path

                // Transition to walking state
                return walkingState;
            }

            // Stay in pathfinding state if no valid path is found
            return this;
        }

        private GameObject ChooseRandomPath(StateHandler stateHandler)
        {
            // Cache the list of waypoints from the previous path point's WaypointLogger
            List<GameObject> waypointLoggerList = stateHandler.previousPathPoint?.GetComponent<WaypointLogger>()?.waypoints;

            if (waypointLoggerList == null || waypointLoggerList.Count == 0)
            {
                // No valid waypoints available
                return null;
            }

            // Generate a random path choice
            int randomChoice = Random.Range(0, waypointLoggerList.Count);
            GameObject chosenPath = waypointLoggerList[randomChoice];

            // If the chosen path is the same as the previous path point, choose the next one
            if (chosenPath == _lastChosenPathPoint)
            {
                randomChoice = (randomChoice + 1) % waypointLoggerList.Count; // Increment index and wrap around if necessary
                chosenPath = waypointLoggerList[randomChoice];
            }

            return chosenPath;
        }
        
        /// <summary>
        /// Checks if the chosen path is too close to the previous, current, or last chosen path.
        /// </summary>
        private bool IsPathTooClose(StateHandler stateHandler, GameObject chosenPath)
        {
            // Ensure the chosen path is valid
            if (chosenPath == null)
            {
                Debug.LogWarning("[PathfinderState] Chosen path is null.");
                return true; // Force path finding to continue if invalid
            }

            // Cache the current and previous path point positions for efficiency
            if (stateHandler.previousPathPoint == null)
            {
                Debug.LogWarning("[PathfinderState] Previous path point is null.");
                return true; // If there's no previous path, it could be too close
            }
            
            // Cache the current and previous path point positions for efficiency
            Vector3 previousPos = stateHandler.previousPathPoint.transform.position;
            Vector3 chosenPos = chosenPath.transform.position;
            Vector3? currentPos = stateHandler.currentPathPoint != null ? (Vector3?)stateHandler.currentPathPoint.transform.position : null;
            Vector3? lastChosenPos = _lastChosenPathPoint != null ? (Vector3?)_lastChosenPathPoint.transform.position : null;

            // Calculate distances once and compare
            float distanceToPrevious = Vector3.SqrMagnitude(previousPos - chosenPos);
            float distanceToCurrent = currentPos.HasValue ? Vector3.SqrMagnitude(currentPos.Value - chosenPos) : float.MaxValue;
            float distanceToLastChosen = lastChosenPos.HasValue ? Vector3.SqrMagnitude(lastChosenPos.Value - chosenPos) : float.MaxValue;

            // Return true if the chosen path is too close to any of the relevant points
            return distanceToPrevious < FloatingPointThreshold * FloatingPointThreshold ||
                   distanceToCurrent < FloatingPointThreshold * FloatingPointThreshold ||
                   distanceToLastChosen < FloatingPointThreshold * FloatingPointThreshold;
        }
    }
}
