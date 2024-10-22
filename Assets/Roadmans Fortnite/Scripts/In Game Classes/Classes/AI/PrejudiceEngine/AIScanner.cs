using System;
using System.Collections.Generic;
using System.Linq;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.PrejudiceEngine
{
    public class AIScanner : MonoBehaviour
    {
        [Header("View Cone Settings")]
        public float viewRadius = 10f;
        [Range(0, 360)] public float viewAngle = 90f;

        [Header("Detection Settings")]
        public LayerMask targetMask;
        public LayerMask obstructionMask;

        [Header("Visual Settings")]
        public bool showGizmos = true;
        public Color viewConeColor = new Color(0, 1, 0, 0.2f);

        [Header("Prejudice Settings")]
        public PrejudiceSettings prejudiceSettings;

        private StateHandler _stateHandler;

        // Cache of visible targets for performance optimization
        private List<Transform> _cachedVisibleTargets = new List<Transform>();
        private float _cacheTime = 0.2f; // Cache for 200ms
        private float _cacheTimer = 0f;

        private void Start()
        {
            prejudiceSettings = GetComponent<Pedestrian>().prejudice;
            _stateHandler = GetComponent<StateHandler>();
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = viewConeColor;

            Gizmos.DrawWireSphere(transform.position, viewRadius);
            Vector3 leftBoundary = DirectionFromAngle(-viewAngle / 2);
            Vector3 rightBoundary = DirectionFromAngle(viewAngle / 2);
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);

            Gizmos.color = Color.red;
            foreach (var target in _cachedVisibleTargets)
            {
                if (target.transform.parent != transform.parent)
                {
                    Gizmos.DrawLine(transform.position, target.position);
                }
            }
        }

        private void Update()
        {
            // Use a cache timer to reduce how often the expensive `FindVisibleTargets` is called
            if (_cacheTimer <= 0)
            {
                _cachedVisibleTargets = FindVisibleTargets().ToList();
                _cacheTimer = _cacheTime; // Reset cache timer
            }
            else
            {
                _cacheTimer -= Time.deltaTime;
            }

            if (!_stateHandler.currentTarget)
            {
                foreach (var target in _cachedVisibleTargets)
                {
                    var prejudiceResult = EvaluatePrejudice(target);
                    if (prejudiceResult == PrejudiceResult.Dislike)
                    {
                        _stateHandler.currentTarget = target.gameObject;

                        var targetPedestrianHandler = target.GetComponent<StateHandler>();
                        if (targetPedestrianHandler != null && !_stateHandler.currentTarget)
                        {
                            targetPedestrianHandler.currentTarget = gameObject;
                        }
                        break; // Stop after finding the first disliked target
                    }
                }
            }
        }

        /// <summary>
        /// Finds all visible targets within the view cone based on the view radius and angle.
        /// </summary>
        public IEnumerable<Transform> FindVisibleTargets()
        {
            Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
            foreach (var collider in targetsInViewRadius)
            {
                if (collider.transform.parent == transform.parent) continue; // Skip same parent objects

                Transform target = collider.transform;
                Vector3 directionToTarget = (target.position - transform.position).normalized;

                if (Vector3.Angle(transform.forward, directionToTarget) < viewAngle / 2)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, target.position);

                    if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                    {
                        yield return target;
                    }
                }
            }
        }

        private PrejudiceResult EvaluatePrejudice(Transform target)
        {
            if (prejudiceSettings == null)
            {
                Debug.LogError("PrejudiceSettings is not assigned.");
                return PrejudiceResult.Neutral;
            }

            var targetPedestrian = target.GetComponent<Pedestrian>();
            if (targetPedestrian == null || targetPedestrian.currenHealthStatus == HealthStatus.Died)
            {
                return PrejudiceResult.Neutral;
            }

            // Check against cached HashSets for faster lookup
            if (IsDisliked(targetPedestrian))
            {
                return PrejudiceResult.Dislike;
            }

            if (IsLiked(targetPedestrian))
            {
                return PrejudiceResult.Like;
            }

            return PrejudiceResult.Neutral;
        }

        // Helper method to check disliked prejudice traits using HashSet for faster lookups
        private bool IsDisliked(Pedestrian targetPedestrian)
        {
            return prejudiceSettings.dislikedNationalities.Contains(targetPedestrian.myNationality) ||
                   prejudiceSettings.dislikedGenders.Contains(targetPedestrian.myGender) ||
                   prejudiceSettings.dislikedRaces.Contains(targetPedestrian.myRace) ||
                   prejudiceSettings.dislikedSexualities.Contains(targetPedestrian.mySexuality);
        }

        // Helper method to check liked prejudice traits using HashSet for faster lookups
        private bool IsLiked(Pedestrian targetPedestrian)
        {
            return prejudiceSettings.likedNationalities.Contains(targetPedestrian.myNationality) ||
                   prejudiceSettings.likedGenders.Contains(targetPedestrian.myGender) ||
                   prejudiceSettings.likedRaces.Contains(targetPedestrian.myRace) ||
                   prejudiceSettings.likedSexualities.Contains(targetPedestrian.mySexuality);
        }

        private Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal = false)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
    }

    public enum PrejudiceResult
    {
        Like,
        Dislike,
        Neutral
    }
}
