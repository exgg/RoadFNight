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

            GameObject chosenPath = null;
            int safetyCounter = 0; // Safety counter to avoid infinite loops

            // Try selecting a valid random path, ensuring it's not too close to the previous/current path
            do
            {
                int randomChoice = Random.Range(0, waypointLoggerList.Count);
                chosenPath = waypointLoggerList[randomChoice];

                safetyCounter++;

                // Break out of loop if too many failed attempts to find a valid path
                if (safetyCounter > 100)
                {
                    chosenPath = null;
                    break;
                }
            }
            while (IsPathTooClose(stateHandler, chosenPath)); // Check if the chosen path is too close

            return chosenPath;
        }

        /// <summary>
        /// Checks if the chosen path is too close to the previous, current, or last chosen path.
        /// </summary>
        private bool IsPathTooClose(StateHandler stateHandler, GameObject chosenPath)
        {
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
