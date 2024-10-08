using System.Collections.Generic;
using System.Linq;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class InitialPathfinderState : BaseState
    {
        public WalkingState walkingState;
        public FollowingState followingState;
        private readonly float _axisThreshold = 3f;
        
        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            if (aiStats.myGroupControlType != GroupControlType.Leader)
            {
                //Debug.Log("I am a follower moving to follow state");
                return followingState;
            }
            
            
            // Find all available WaypointLogger instances in the scene
            var loggerList = FindObjectsOfType<WaypointLogger>();

            // Find the nearest path point(s) for the AI
            var nearestPathPoints = FindNearestPathPoint(stateHandler, loggerList);

            //Debug.Log("Looking for path point");
            
            // Logic after finding the nearest path point (could be transitioning to another state)
            if (nearestPathPoints.Count > 0)
            {
                // Move to walking state or whatever the next state would be
                stateHandler.currentPathPoint = nearestPathPoints[0]; // Set the closest waypoint as target
                stateHandler.previousPathPoint = stateHandler.currentPathPoint;
                return walkingState;  // Transition to walking state
            }

            // Stay in the current state if no valid paths are found
            return this;
        }

        // Method to find the nearest path points (using WaypointLogger instances)
        private List<GameObject> FindNearestPathPoint(StateHandler stateHandler, WaypointLogger[] pathPoints)
        {
            return pathPoints
                .Where(p => IsOnSameAxis(stateHandler.transform.position, p.transform.position)) // Only allow waypoints on the same axis (X or Z)
                .OrderBy(p => CalculateDistanceOnXZPlane(stateHandler.transform.position, p.transform.position))  // Sort by distance on the X and Z axes
                .Select(p => p.gameObject) // Return the GameObject from the WaypointLogger
                .ToList();
        }

        // Helper method to check if two points are aligned on the X or Z axis within the threshold
        private bool IsOnSameAxis(Vector3 pointA, Vector3 pointB)
        {
            // Return true if the two points are aligned on either the X axis or the Z axis within the threshold
            return (Mathf.Abs(pointA.x - pointB.x) <= _axisThreshold || Mathf.Abs(pointA.z - pointB.z) <= _axisThreshold);
        }

        // Helper method to calculate the distance between two points on the XZ plane (ignoring the Y axis)
        private float CalculateDistanceOnXZPlane(Vector3 pointA, Vector3 pointB)
        {
            // Calculate distance in 2D (XZ plane), ignoring the Y axis
            return Vector2.Distance(new Vector2(pointA.x, pointA.z), new Vector2(pointB.x, pointB.z));
        }
    }
}

