using System;
using System.Collections.Generic;
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

            // draw detection lines
            Gizmos.color = Color.red;
            foreach (var target in FindVisibleTargets())
            {
                Gizmos.DrawLine(transform.position, target.position);
            }
        }

        private void Update()
        {
            FindVisibleTargets();
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
}
