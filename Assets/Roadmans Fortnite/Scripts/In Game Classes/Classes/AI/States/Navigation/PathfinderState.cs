using System.Collections.Generic;
using System.Threading.Tasks;
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
        private bool _isCalculating; // Prevent multiple calculations at the same time

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            if (_isCalculating)
                return this; // Avoid running multiple calculations concurrently

            // Run pathfinding in a separate thread
            HandlePathfindingAsync(stateHandler).ConfigureAwait(false);

            return this; // Stay in this state until a path is found
        }

        private async Task HandlePathfindingAsync(StateHandler stateHandler)
        {
            _isCalculating = true;

            // Perform random path selection and distance checks in a background thread
            GameObject chosenPath = await Task.Run(() => ChooseRandomPath(stateHandler));

            // Back on the main thread: set the path and transition state
            if (chosenPath != null)
            {
                stateHandler.currentPathPoint = chosenPath;
                _lastChosenPathPoint = stateHandler.previousPathPoint;
                _isCalculating = false;
                stateHandler.SwitchToNextState(walkingState); // Transition to WalkingState
            }
            else
            {
                _isCalculating = false;
            }
        }

        private GameObject ChooseRandomPath(StateHandler stateHandler)
        {
            List<GameObject> waypointLoggerList = stateHandler.previousPathPoint?.GetComponent<WaypointLogger>()?.waypoints;

            if (waypointLoggerList == null || waypointLoggerList.Count == 0)
            {
                Debug.LogError("[PathfinderState] No waypoints available for pathfinding.");
                return null; // No valid paths available
            }

            GameObject chosenPath = null;
            int safetyCounter = 0; // Safety counter to avoid infinite loop

            do
            {
                int randomChoice = Random.Range(0, waypointLoggerList.Count);
                chosenPath = waypointLoggerList[randomChoice];
                safetyCounter++;

                if (safetyCounter > 100)
                {
                    Debug.LogWarning("Safety break: Could not find a valid path different from the previous one.");
                    break;
                }
            }
            while (IsPathTooClose(stateHandler, chosenPath)); // Check path distance validity

            return chosenPath;
        }

        /// <summary>
        /// Checks if the chosen path is too close to the previous, current, or last chosen path.
        /// </summary>
        private bool IsPathTooClose(StateHandler stateHandler, GameObject chosenPath)
        {
            float distanceToPrevious = Vector3.Distance(stateHandler.previousPathPoint.transform.position, chosenPath.transform.position);
            float distanceToCurrent = stateHandler.currentPathPoint != null ? Vector3.Distance(stateHandler.currentPathPoint.transform.position, chosenPath.transform.position) : float.MaxValue;
            float distanceToLastChosen = _lastChosenPathPoint != null ? Vector3.Distance(_lastChosenPathPoint.transform.position, chosenPath.transform.position) : float.MaxValue;

            return distanceToPrevious < FloatingPointThreshold || distanceToCurrent < FloatingPointThreshold || distanceToLastChosen < FloatingPointThreshold;
        }
    }
}
