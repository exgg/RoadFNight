using System;
using System.Collections.Generic;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.PrejudiceEngine
{
    public class AIScanner : MonoBehaviour
    {
        [Header("View Cone Settings")]
        public float viewRadius = 10f;          // Radius of the view cone
        [Range(0, 360)] public float viewAngle = 90f;  // Angle of the view cone (in degrees)

        [Header("Detection Settings")]
        public LayerMask targetMask;            // Layer mask to define which objects are "targets"
        public LayerMask obstructionMask;       // Layer mask to define what counts as an obstruction

        [Header("Visual Settings")]
        public bool showGizmos = true;          // Toggle Gizmos visualization on or off
        public Color viewConeColor = new Color(0, 1, 0, 0.2f); // Color for the view cone visualization

        [Header("Prejudice Settings")]
        public PrejudiceSettings prejudiceSettings; // Reference to the AI's prejudice settings

        private StateHandler _stateHandler;
        
        private void Start()
        {
            prejudiceSettings = GetComponent<Pedestrian>().prejudice;
            _stateHandler = GetComponent<StateHandler>();
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = viewConeColor;

            // Draw the view cone using Gizmos
            Gizmos.DrawWireSphere(transform.position, viewRadius);  // Draw the outer circle of the view cone
            Vector3 leftBoundary = DirectionFromAngle(-viewAngle / 2);
            Vector3 rightBoundary = DirectionFromAngle(viewAngle / 2);
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);

            // Draw detection lines
            Gizmos.color = Color.red;
            foreach (var target in FindVisibleTargets())
            {
                if (target.transform.parent != transform.parent)
                {
                    Gizmos.DrawLine(transform.position, target.position);

                }
            }
        }

        private void Update()
        {
            var visibleTargets = FindVisibleTargets();
            if (!_stateHandler.currentTarget)
            {
                foreach (var target in visibleTargets)
                {
                    // Evaluate prejudice to find a disliked target
                    var prejudiceResult = EvaluatePrejudice(target);
                    if (prejudiceResult == PrejudiceResult.Dislike)
                    {
                        // Set the disliked target as the AI's current target
                        _stateHandler.currentTarget = target.gameObject;
                        Debug.Log($"Target acquired: {target.name} - transitioning to aggressive behavior.");

                        // Make the AI the target of the disliked pedestrian (so they attack back)
                        var targetPedestrianHandler = target.GetComponent<StateHandler>();
                        if (targetPedestrianHandler != null && !_stateHandler.currentTarget)
                        {
                            targetPedestrianHandler.currentTarget = gameObject;
                            Debug.Log($"{target.name} is now targeting {gameObject.name} in return.");
                        }

                        // Break once a target is found, no need to search further
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Finds all visible targets within the view cone based on the view radius and angle.
        /// </summary>
        public Transform[] FindVisibleTargets()
        {
            // Get all potential targets within the view radius
            Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
            var visibleTargets = new List<Transform>();

            // Iterate through each potential target
            foreach (var collider in targetsInViewRadius)
            {
                if (collider.transform.parent != transform.parent)
                {
                    Transform target = collider.transform;
                    Vector3 directionToTarget = (target.position - transform.position).normalized;

                    // Check if the target is within the view angle
                    if (Vector3.Angle(transform.forward, directionToTarget) < viewAngle / 2)
                    {
                        float distanceToTarget = Vector3.Distance(transform.position, target.position);

                        // Check if the target is not obstructed by any obstacles
                        if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                        {
                            visibleTargets.Add(target);
                        }
                    }
                }
            }
            return visibleTargets.ToArray();
        }

      private PrejudiceResult EvaluatePrejudice(Transform target)
        {
            // Ensure that PrejudiceSettings is assigned
            if (prejudiceSettings == null)
            {
                Debug.LogError("PrejudiceSettings is not assigned in the inspector.");
                return PrejudiceResult.Neutral;
            }

            // Example: Assume the target has a "Pedestrian" component with nationality, gender, race, etc.
            var targetPedestrian = target.GetComponent<Pedestrian>();

            // Ensure the targetPedestrian component exists
            if (targetPedestrian == null || targetPedestrian.currenHealthStatus == HealthStatus.Died)
            {
                //Debug.LogError($"Pedestrian component missing on target: {target.name}");
                return PrejudiceResult.Neutral;
            }

            // Check if arrays in PrejudiceSettings are properly initialized
            if (prejudiceSettings.dislikedNationalities == null || prejudiceSettings.dislikedGenders == null || 
                prejudiceSettings.dislikedRaces == null || prejudiceSettings.dislikedSexualities == null)
            {
                Debug.LogError("One or more prejudice arrays are not initialized in PrejudiceSettings.");
                return PrejudiceResult.Neutral;
            }

            // Check if the target's nationality, gender, race, or other traits are disliked
            if (Array.Exists(prejudiceSettings.dislikedNationalities, n => n == targetPedestrian.myNationality) ||
                Array.Exists(prejudiceSettings.dislikedGenders, g => g == targetPedestrian.myGender) ||
                Array.Exists(prejudiceSettings.dislikedRaces, r => r == targetPedestrian.myRace) ||
                Array.Exists(prejudiceSettings.dislikedSexualities, s => s == targetPedestrian.mySexuality))
            {
                return PrejudiceResult.Dislike;
            }

            // Check if the target's traits are liked
            if (Array.Exists(prejudiceSettings.likedNationalities, n => n == targetPedestrian.myNationality) ||
                Array.Exists(prejudiceSettings.likedGenders, g => g == targetPedestrian.myGender) ||
                Array.Exists(prejudiceSettings.likedRaces, r => r == targetPedestrian.myRace) ||
                Array.Exists(prejudiceSettings.likedSexualities, s => s == targetPedestrian.mySexuality))
            {
                return PrejudiceResult.Like;
            }

            return PrejudiceResult.Neutral; // If no preference, remain neutral
      }


        /// <summary>
        /// Gets the direction vector from a given angle in degrees.
        /// </summary>
        private Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal = false)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
    }

    /// <summary>
    /// Prejudice evaluation result to dictate state transitions.
    /// </summary>
    public enum PrejudiceResult
    {
        Like,
        Dislike,
        Neutral
    }
}
