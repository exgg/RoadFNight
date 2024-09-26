using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Opsive.Shared.Utility;
using Unity.VisualScripting;

namespace Roadmans_Fortnite.Editor.EditorAddons
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaypointLogger))]
    public class InstantPathLinker : UnityEditor.Editor
    {
        // Field to set the number of links
        private int numberOfLinks = 2;
        private int undoLimit = 15; // Limit for undo actions
        public float axisThreshold = 0; // Threshold for axis alignment

        public override void OnInspectorGUI()
        {
            // Draw the default inspector for the WaypointLogger
            DrawDefaultInspector();

            EditorGUILayout.LabelField("Waypoint Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Add a field for the number of links to create
            numberOfLinks = EditorGUILayout.IntField("Number of Links", numberOfLinks);

            // Add a threshold field for the alignment tolerance using a slider
            axisThreshold = EditorGUILayout.Slider("Axis Threshold", axisThreshold, 0f, 10f);

            EditorGUILayout.Space(2);
            
            
            EditorGUILayout.LabelField("Duplication", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            // Add a second button for duplicating and linking
            if (GUILayout.Button("Duplicate Singular Path"))
            {
                Debug.Log("Duplicating the waypoint and linking");
                DuplicateAndLinkWaypoint();
            }
            
            if (GUILayout.Button("Duplicate Multiple Path Points"))
            {
                Debug.Log("Duplicating and linking multiple waypoints");
                DuplicateAndLinkMultipleWaypoints();
            }
            
            EditorGUILayout.Space(2);
            
            EditorGUILayout.LabelField("Alignment", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Align Paths to Last Selected"))
            {
                Debug.Log("Aligning selected paths to the last selected path");
                AlignSelectedPathsToLast();
            }
            
            EditorGUILayout.Space(2);
            
            EditorGUILayout.LabelField("Linking", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Link Path Pair"))
            {
                Debug.Log("Linking selected path pair");
                LinkPathPair();
            }
            
            // Add a button to trigger the linking process
            if (GUILayout.Button("Link Nearest Waypoints"))
            {
                Debug.Log("Looking for waypoints");
                LinkNearestWaypointsForSelection();
            }
            
            EditorGUILayout.Space(2);
            
            EditorGUILayout.LabelField("Unlink/Clear", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Unlink Selected Paths"))
            {
                Debug.Log("Unlinking selected path pair");
                UnlinkPathPair();
            }

            EditorGUILayout.Space(2);
            
            if (GUILayout.Button("Clear Selected Points"))
            {
                Debug.Log("Clearing all selected paths");
                ClearSelectedPathPoints();
            }
        }

        #region Alignment
        
        private void AlignSelectedPathsToLast()
        {
            var selectedObjects = Selection.objects;

            // Ensure there are at least two selected objects (one to align and at least one other to align to)
            if (selectedObjects.Length < 2)
            {
                Debug.LogWarning("Please select at least two waypoints to align.");
                return;
            }

            // Get the last selected waypoint (the one to align others to)
            GameObject lastSelectedObject = selectedObjects[selectedObjects.Length - 1] as GameObject;
            WaypointLogger lastSelectedWaypoint = lastSelectedObject?.GetComponent<WaypointLogger>();

            if (lastSelectedWaypoint == null)
            {
                Debug.LogWarning("The last selected object does not have a WaypointLogger component.");
                return;
            }

            Vector3 lastPosition = lastSelectedWaypoint.transform.position;

            // Iterate over all other selected objects and align them
            for (int i = 0; i < selectedObjects.Length - 1; i++)
            {
                GameObject selectedObject = selectedObjects[i] as GameObject;
                WaypointLogger selectedWaypoint = selectedObject?.GetComponent<WaypointLogger>();

                if (selectedWaypoint == null)
                {
                    Debug.LogWarning($"Selected object at index {i} does not have a WaypointLogger component.");
                    continue;
                }

                Vector3 selectedPosition = selectedWaypoint.transform.position;

                // Determine whether to align on the X or Z axis based on the smaller positional difference
                float deltaX = Mathf.Abs(selectedPosition.x - lastPosition.x);
                float deltaZ = Mathf.Abs(selectedPosition.z - lastPosition.z);

                if (deltaX < deltaZ)
                {
                    // Align on the X axis (adjust X, keep Z the same)
                    selectedPosition.x = lastPosition.x;
                }
                else
                {
                    // Align on the Z axis (adjust Z, keep X the same)
                    selectedPosition.z = lastPosition.z;
                }

                // Record the position change for undo
                Undo.RecordObject(selectedWaypoint.transform, "Align Path Position");

                // Apply the new aligned position
                selectedWaypoint.transform.position = selectedPosition;

                // Mark the object as dirty to ensure the changes are saved
                EditorUtility.SetDirty(selectedWaypoint);

                Debug.Log($"Aligned {selectedWaypoint.name} to {lastSelectedWaypoint.name} on the {(deltaX < deltaZ ? "X" : "Z")} axis");
            }

            // After alignment, optionally keep all objects selected
            Selection.objects = selectedObjects;
        }

        #endregion

        #region Linking
        
        // New method to link nearest waypoints for multiple selected WaypointLoggers
        private void LinkNearestWaypointsForSelection()
        {
            // Get all selected objects
            var selectedObjects = Selection.objects;

            foreach (var selectedObject in selectedObjects)
            {
                WaypointLogger waypointLogger = (selectedObject as GameObject)?.GetComponent<WaypointLogger>();

                if (waypointLogger != null)
                {
                    // Call the modified linking method for each selected waypoint logger
                    LinkNearestWaypoints(waypointLogger);
                }
            }
        }

        
        // Modified method to link nearest waypoints and account for existing links
        private void LinkNearestWaypoints(WaypointLogger currentWaypoint)
        {
            Debug.Log("Initialized looking for waypoints");

            // Find all WaypointLogger objects in the scene
            WaypointLogger[] allLoggers = FindObjectsOfType<WaypointLogger>();

            Debug.Log($"There are {allLoggers.Length} loggers in the scene ");

            // Check how many links already exist
            int currentLinkCount = currentWaypoint.waypoints.Count;

            // Calculate how many more links are needed
            int neededLinks = numberOfLinks - currentLinkCount;

            if (neededLinks <= 0)
            {
                Debug.Log($"{currentWaypoint.name} already has {currentLinkCount} links, no more links needed.");
                return;
            }

            Debug.Log($"{currentWaypoint.name} needs {neededLinks} more links.");

            // Get the nearest needed waypoints without diagonal connections
            List<WaypointLogger> nearestWaypoints = FindNearestWaypoints(currentWaypoint, allLoggers, neededLinks);

            // Start the Undo group
            Undo.SetCurrentGroupName("Link Nearest Waypoints");
            int undoGroupIndex = Undo.GetCurrentGroup();

            // Link them, making sure there's no duplication
            foreach (var waypoint in nearestWaypoints)
            {
                Debug.Log("Linking additional waypoints ");
                if (!currentWaypoint.waypoints.Contains(waypoint.gameObject))
                {
                    Debug.Log("Waypoint is not added, adding now");

                    // Register undo for the current and nearest waypoint before modification
                    Undo.RecordObject(currentWaypoint, "Link Waypoints");
                    Undo.RecordObject(waypoint, "Link Waypoints");

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

            // End the undo group and collapse it into a single operation
            Undo.CollapseUndoOperations(undoGroupIndex);

            Debug.Log("Logging waypoints");
            // Force the editor to refresh and reflect changes
            EditorUtility.SetDirty(currentWaypoint);
            Debug.Log("Editor Refreshed");
        }

        // Method to link two selected waypoints manually
        private void LinkPathPair()
        {
            // Get all selected objects
            var selectedObjects = Selection.objects;

            if (selectedObjects.Length != 2)
            {
                Debug.LogWarning("Please select exactly two waypoints to link.");
                return;
            }

            // Ensure both selected objects are WaypointLogger components
            WaypointLogger waypointLoggerA = (selectedObjects[0] as GameObject)?.GetComponent<WaypointLogger>();
            WaypointLogger waypointLoggerB = (selectedObjects[1] as GameObject)?.GetComponent<WaypointLogger>();

            if (waypointLoggerA == null || waypointLoggerB == null)
            {
                Debug.LogWarning("Both selected objects must have WaypointLogger components.");
                return;
            }

            // Check if the two waypoints are already linked and link them if not
            if (!waypointLoggerA.waypoints.Contains(waypointLoggerB.gameObject))
            {
                Undo.RecordObject(waypointLoggerA, "Link Waypoints");
                waypointLoggerA.waypoints.Add(waypointLoggerB.gameObject);
            }

            if (!waypointLoggerB.waypoints.Contains(waypointLoggerA.gameObject))
            {
                Undo.RecordObject(waypointLoggerB, "Link Waypoints");
                waypointLoggerB.waypoints.Add(waypointLoggerA.gameObject);
            }

            // Mark the objects as dirty to ensure changes are saved
            EditorUtility.SetDirty(waypointLoggerA);
            EditorUtility.SetDirty(waypointLoggerB);

            Debug.Log($"Linked {waypointLoggerA.name} with {waypointLoggerB.name}");
        }

        #endregion

        #region Unlinking

        
        // Method to unlink two selected waypoints manually
        private void UnlinkPathPair()
        {
            // Get all selected objects
            var selectedObjects = Selection.objects;

            if (selectedObjects.Length != 2)
            {
                Debug.LogWarning("Please select exactly two waypoints to unlink.");
                return;
            }

            // Ensure both selected objects are WaypointLogger components
            WaypointLogger waypointLoggerA = (selectedObjects[0] as GameObject)?.GetComponent<WaypointLogger>();
            WaypointLogger waypointLoggerB = (selectedObjects[1] as GameObject)?.GetComponent<WaypointLogger>();

            if (waypointLoggerA == null || waypointLoggerB == null)
            {
                Debug.LogWarning("Both selected objects must have WaypointLogger components.");
                return;
            }

            // Check if the two waypoints are linked and unlink them if they are
            if (waypointLoggerA.waypoints.Contains(waypointLoggerB.gameObject))
            {
                Undo.RecordObject(waypointLoggerA, "Unlink Waypoints");
                waypointLoggerA.waypoints.Remove(waypointLoggerB.gameObject);
            }

            if (waypointLoggerB.waypoints.Contains(waypointLoggerA.gameObject))
            {
                Undo.RecordObject(waypointLoggerB, "Unlink Waypoints");
                waypointLoggerB.waypoints.Remove(waypointLoggerA.gameObject);
            }

            // Mark the objects as dirty to ensure changes are saved
            EditorUtility.SetDirty(waypointLoggerA);
            EditorUtility.SetDirty(waypointLoggerB);

            Debug.Log($"Unlinked {waypointLoggerA.name} from {waypointLoggerB.name}");
        }

        #endregion

        #region Clearing

        private void ClearSelectedPathPoints()
        {
            var selectedObjects = Selection.objects;
            var allWaypointLoggers = FindObjectsOfType<WaypointLogger>();

            foreach (var selectedObject in selectedObjects)
            {
                // Ensure the selected object is a GameObject and has a WaypointLogger component
                var wayPoint = (selectedObject as GameObject)?.GetComponent<WaypointLogger>();

                if (wayPoint == null)
                {
                    Debug.LogWarning("Selected object does not have a WaypointLogger component.");
                    continue;
                }

                // Iterate through all WaypointLogger instances in the scene
                foreach (var waypointLogger in allWaypointLoggers)
                {
                    // Check if the waypointLogger's list contains the selected waypoint
                    if (waypointLogger.waypoints.Contains(wayPoint.gameObject))
                    {
                        // Record the undo operation
                        Undo.RecordObject(waypointLogger, "Clear Waypoint Links");

                        // Remove the selected waypoint from the waypointLogger's list
                        waypointLogger.waypoints.Remove(wayPoint.gameObject);

                        // Mark the waypointLogger as dirty to ensure changes are saved
                        EditorUtility.SetDirty(waypointLogger);
                    }
                }

                // Clear the waypoints list of the selected waypoint itself (optional)
                if (wayPoint.waypoints.Count > 0)
                {
                    Undo.RecordObject(wayPoint, "Clear Own Waypoints");
                    wayPoint.waypoints.Clear();
                    EditorUtility.SetDirty(wayPoint);
                }

                Debug.Log($"Cleared all paths associated with {wayPoint.name}");
            }
        }

        #endregion

        #region Logic Checks

        // Finds the nearest N waypoints to the given waypoint without diagonal connections (along X or Z axis only)
        private List<WaypointLogger> FindNearestWaypoints(WaypointLogger current, WaypointLogger[] allPoints, int neededLinks)
        {
            return allPoints
                .Where(p => p != current && !current.waypoints.Contains(p.gameObject))  // Exclude the current waypoint and already linked ones
                .Where(p => IsOnSameAxis(current.transform.position, p.transform.position)) // Only allow waypoints on the same axis (X or Z)
                .OrderBy(p => CalculateDistanceOnXZPlane(current.transform.position, p.transform.position))  // Sort by distance on the X and Z axes
                .Take(neededLinks)  // Take the number of needed links
                .ToList();
        }

        // Helper method to check if two waypoints are aligned on either the X or Z axis with a threshold
        private bool IsOnSameAxis(Vector3 pointA, Vector3 pointB)
        {
            // Return true if the two points are aligned on either the X axis or the Z axis within the threshold
            return (Mathf.Abs(pointA.x - pointB.x) <= axisThreshold || Mathf.Abs(pointA.z - pointB.z) <= axisThreshold);
        }

        // Helper method to calculate the distance between two points on the XZ plane
        private float CalculateDistanceOnXZPlane(Vector3 pointA, Vector3 pointB)
        {
            // Only calculate distance in 2D, ignoring the Y axis
            return Vector2.Distance(new Vector2(pointA.x, pointA.z), new Vector2(pointB.x, pointB.z));
        }


        #endregion

        #region Duplication

           // Duplicates the selected waypoint, clears links, and links the original with the duplicate
        private void DuplicateAndLinkWaypoint()
        {
            WaypointLogger currentWaypoint = (WaypointLogger)target;

            WaypointLogger[] totalWaypoints = FindObjectsOfType<WaypointLogger>();

            // Create a duplicate of the current GameObject and move it slightly to the right
            GameObject duplicatedObject = Instantiate(currentWaypoint.gameObject, currentWaypoint.transform.position + new Vector3(2, 0, 0), Quaternion.identity);

            WaypointLogger duplicatedWaypoint = duplicatedObject.GetComponent<WaypointLogger>();
            duplicatedWaypoint.name = "PathPoint (" + (totalWaypoints.Length + 1) + ")";
            duplicatedObject.transform.parent = currentWaypoint.gameObject.transform.parent;
            duplicatedObject.transform.position = currentWaypoint.transform.position;

            // Register the undo operation for the duplicate creation
            Undo.RegisterCreatedObjectUndo(duplicatedObject, "Duplicate and Link Waypoint");

            // Clear the waypoints on the duplicate
            duplicatedWaypoint.waypoints.Clear();

            // Link the original with the duplicate
            Undo.RecordObject(currentWaypoint, "Link Waypoints");
            Undo.RecordObject(duplicatedWaypoint, "Link Waypoints");

            currentWaypoint.waypoints.Add(duplicatedObject);
            duplicatedWaypoint.waypoints.Add(currentWaypoint.gameObject);

            // Mark the objects as dirty to save changes
            EditorUtility.SetDirty(currentWaypoint);
            EditorUtility.SetDirty(duplicatedWaypoint);
            
            // set duplicate as the new active selection
            Selection.activeGameObject = duplicatedObject;

            Debug.Log($"Duplicated and linked {currentWaypoint.name} with {duplicatedWaypoint.name}");
        }
        
        private void DuplicateAndLinkMultipleWaypoints()
        {
            // Get all selected objects
            var selectedObjects = Selection.objects;

            // Ensure there are selected objects to duplicate
            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("No waypoints selected for duplication.");
                return;
            }

            // Create a list to hold only the newly duplicated objects for setting them as the new selection after duplication
            List<GameObject> newSelection = new List<GameObject>();

            foreach (var selectedObject in selectedObjects)
            {
                // Ensure each selected object is a GameObject and has a WaypointLogger component
                WaypointLogger originalWaypoint = (selectedObject as GameObject)?.GetComponent<WaypointLogger>();

                if (originalWaypoint == null)
                {
                    Debug.LogWarning("One of the selected objects does not have a WaypointLogger component.");
                    continue;
                }

                // Create a duplicate of the current GameObject at the same position
                GameObject duplicatedObject = Instantiate(originalWaypoint.gameObject, originalWaypoint.transform.position, Quaternion.identity);

                WaypointLogger duplicatedWaypoint = duplicatedObject.GetComponent<WaypointLogger>();
                duplicatedWaypoint.name = "PathPoint (" + (FindObjectsOfType<WaypointLogger>().Length + 1) + ")";
                duplicatedObject.transform.parent = originalWaypoint.gameObject.transform.parent;

                // Register the undo operation for the duplicate creation
                Undo.RegisterCreatedObjectUndo(duplicatedObject, "Duplicate and Link Multiple Waypoints");

                // Clear the waypoints on the duplicate
                duplicatedWaypoint.waypoints.Clear();

                // Link the original with the duplicate
                Undo.RecordObject(originalWaypoint, "Link Waypoints");
                Undo.RecordObject(duplicatedWaypoint, "Link Waypoints");

                originalWaypoint.waypoints.Add(duplicatedObject);
                duplicatedWaypoint.waypoints.Add(originalWaypoint.gameObject);

                // Mark the objects as dirty to save changes
                EditorUtility.SetDirty(originalWaypoint);
                EditorUtility.SetDirty(duplicatedWaypoint);

                // Add the duplicate to the new selection list
                newSelection.Add(duplicatedObject);

                Debug.Log($"Duplicated and linked {originalWaypoint.name} with {duplicatedWaypoint.name}");
            }

            // Set only the newly duplicated objects as the selected objects in the editor
            Selection.objects = newSelection.ToArray();
        }

        #endregion
     
    }
}
