using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Opsive.Shared.Utility;

namespace Roadmans_Fortnite.Editor.EditorAddons
{
    [CustomEditor(typeof(WaypointLogger))]
    public class InstantPathLinker : UnityEditor.Editor
    {
        // Field to set the number of links
        private int numberOfLinks = 1;

        public override void OnInspectorGUI()
        {
            // Draw the default inspector for the WaypointLogger
            DrawDefaultInspector();

            // Add a field for the number of links to create
            numberOfLinks = EditorGUILayout.IntField("Number of Links", numberOfLinks);

            // Add a button to trigger the linking process
            if (GUILayout.Button("Link Nearest Waypoints"))
            {
                Debug.Log("Looking for waypoints");
                LinkNearestWaypoints();
            }

            // Add a second button for duplicating and linking
            if (GUILayout.Button("Duplicate Singular Path"))
            {
                Debug.Log("Duplicating the waypoint and linking");
                DuplicateAndLinkWaypoint();
            }
        }

        // Method to find and link the nearest waypoints
        private void LinkNearestWaypoints()
        {
            Debug.Log("Initialized looking for waypoints");
            WaypointLogger currentWaypoint = (WaypointLogger)target;

            // Find all WaypointLogger objects in the scene
            WaypointLogger[] allLoggers = FindObjectsOfType<WaypointLogger>();

            Debug.Log($"There are {allLoggers.Length} loggers in the scene ");

            // Get the nearest N waypoints
            List<WaypointLogger> nearestWaypoints = FindNearestWaypoints(currentWaypoint, allLoggers, numberOfLinks);

            // Link them, making sure there's no duplication
            foreach (var waypoint in nearestWaypoints)
            {
                Debug.Log("There are waypoints around ");
                if (!currentWaypoint.waypoints.Contains(waypoint.gameObject))
                {
                    Debug.Log("Waypoint is not added, adding now");
                    // Add to the current waypoint's list
                    currentWaypoint.waypoints.Add(waypoint.gameObject);

                    // Also add the current waypoint to the other waypoint's list if not already present
                    if (!waypoint.waypoints.Contains(currentWaypoint.gameObject))
                    {
                        waypoint.waypoints.Add(currentWaypoint.gameObject);
                    }

                    Debug.Log($"Linked {currentWaypoint.name} with {waypoint.name}");
                }
            }

            Debug.Log("Logging waypoints");
            // Force the editor to refresh and reflect changes
            EditorUtility.SetDirty(currentWaypoint);
            Debug.Log("Editor Refreshed");
        }

        // Finds the nearest N waypoints to the given waypoint
        private List<WaypointLogger> FindNearestWaypoints(WaypointLogger current, WaypointLogger[] allPoints, int n)
        {
            return allPoints
                .Where(p => p != current)  // Exclude the current waypoint
                .OrderBy(p => Vector3.Distance(current.transform.position, p.transform.position))  // Sort by distance
                .Take(n)  // Take the closest n points
                .ToList();
        }

        // Duplicates the selected waypoint, clears links, and links the original with the duplicate
        private void DuplicateAndLinkWaypoint()
        {
            WaypointLogger currentWaypoint = (WaypointLogger)target;

            WaypointLogger[] totalWaypoints = FindObjectsOfType<WaypointLogger>();
            
            // Create a duplicate of the current GameObject and move it slightly to the right
            GameObject duplicatedObject = Instantiate(currentWaypoint.gameObject, currentWaypoint.transform.position + new Vector3(2, 0, 0), Quaternion.identity);

            WaypointLogger duplicatedWaypoint = duplicatedObject.GetComponent<WaypointLogger>();
            duplicatedWaypoint.name = "PathPoint (" + totalWaypoints.Length + ")";
            duplicatedObject.transform.parent = currentWaypoint.gameObject.transform.parent;
            
            // Clear the waypoints on the duplicate
            duplicatedWaypoint.waypoints.Clear();

            // Link the original with the duplicate
            currentWaypoint.waypoints.Add(duplicatedObject);
            duplicatedWaypoint.waypoints.Add(currentWaypoint.gameObject);

            // Mark the objects as dirty to save changes
            EditorUtility.SetDirty(currentWaypoint);
            EditorUtility.SetDirty(duplicatedWaypoint);

            Debug.Log($"Duplicated and linked {currentWaypoint.name} with {duplicatedWaypoint.name}");
        }
    }
}
