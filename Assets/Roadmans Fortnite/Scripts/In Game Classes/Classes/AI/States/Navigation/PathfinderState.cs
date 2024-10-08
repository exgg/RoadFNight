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
            //Debug.Log($"[PathfinderState] Tick called. Previous: {stateHandler.previousPathPoint?.name}, Current: {stateHandler.currentPathPoint?.name}");

            // Choose a new random path
            var randomPath = ChooseRandomPath(stateHandler);

            // Check if both previous and current path points are road crossing points
            Debug.Log("Why the fuck are you not loggin??");
            
            if (randomPath != null)
            {
                // Set the selected path as the new current path point
                stateHandler.currentPathPoint = randomPath;
                _lastChosenPathPoint = stateHandler.previousPathPoint; // Track the last chosen path

                //Debug.Log($"[PathfinderState] Chosen new path point: {randomPath.name}, transitioning to WalkingState.");

                return walkingState;
            }
            else
            {
                //Debug.LogWarning($"[PathfinderState] No valid random path found.");
            }
            
            //Debug.LogError($"[PathfinderState] No valid path found, staying in PathfinderState. Previous: {stateHandler.previousPathPoint?.name}, Current: {stateHandler.currentPathPoint?.name}");
            return this;
        }

        private GameObject ChooseRandomPath(StateHandler stateHandler)
        {
            List<GameObject> waypointLoggerList = stateHandler.previousPathPoint?.GetComponent<WaypointLogger>()?.waypoints;

            if (waypointLoggerList == null || waypointLoggerList.Count == 0)
            {
                //Debug.LogError("[PathfinderState] No waypoints available for pathfinding.");
                return null; // No valid paths available
            }

            GameObject chosenPath = null;
            int safetyCounter = 0; // Safety counter to avoid infinite loop

            do
            {
                int randomChoice = Random.Range(0, waypointLoggerList.Count);
                chosenPath = waypointLoggerList[randomChoice];

                //Debug.Log($"[PathfinderState] Trying path: {chosenPath.name}, Distance to Previous: {Vector3.Distance(stateHandler.previousPathPoint.transform.position, chosenPath.transform.position)}");

                safetyCounter++;

                if (safetyCounter > 100)
                {
                    //Debug.LogWarning("Safety break: Could not find a valid path different from the previous one.");
                    break;
                }
            }
            while (IsPathTooClose(stateHandler, chosenPath)); // Check path distance validity

            if (chosenPath != null)
            {
                //Debug.Log($"[PathfinderState] Successfully chose path: {chosenPath.name} after {safetyCounter} attempts.");
            }

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

            //Debug.Log($"[PathfinderState] Distance to Previous: {distanceToPrevious}, Current: {distanceToCurrent}, LastChosen: {distanceToLastChosen}");

            return distanceToPrevious < FloatingPointThreshold || distanceToCurrent < FloatingPointThreshold || distanceToLastChosen < FloatingPointThreshold;
        }
    }
}
