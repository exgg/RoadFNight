using System.Collections.Generic;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians
{
    /// <summary>
    /// A group brain that controls specific groups
    /// </summary>
    public class PedestrianGroup : MonoBehaviour
    {
        public string groupName;
        public Pedestrian leader;
        public List<Pedestrian> allMembers = new List<Pedestrian>();
        public PedestrianSystem system;

        // Cached dictionary for storing distance and visibility states
        private readonly Dictionary<Pedestrian, float> _pedestrianDistances = new Dictionary<Pedestrian, float>();

        // Cache for StateHandlers to avoid repeated GetComponent calls
        private Dictionary<Pedestrian, StateHandler> _cachedStateHandlers = new Dictionary<Pedestrian, StateHandler>();

        private void Awake()
        {
            // Initialize pedestrians and cache their StateHandler components
            foreach (var member in GetComponentsInChildren<Pedestrian>(true))
            {
                allMembers.Add(member);

                // Cache StateHandler component
                StateHandler stateHandler = member.GetComponent<StateHandler>();
                if (stateHandler != null)
                {
                    _cachedStateHandlers[member] = stateHandler;
                }

                // Initialize distance dictionary with infinite distance
                _pedestrianDistances[member] = float.MaxValue;
            }

            system = FindObjectOfType<PedestrianSystem>();
        }

        public void AddMember(Pedestrian newMember)
        {
            Debug.Log("Tell me something man");
            // Check if the pedestrian is already in the members list
            if (allMembers.Contains(newMember)) 
                return;

            // Add to members list
            allMembers.Add(newMember);

            // Get and cache the StateHandler component for this pedestrian
            StateHandler stateHandler = newMember.GetComponent<StateHandler>();

            if (stateHandler != null)
            {
                // Cache both the pedestrian and its state handler
                if (!_cachedStateHandlers.ContainsKey(newMember))
                {
                    _cachedStateHandlers.Add(newMember, stateHandler);
                }
            }
           
            // Initialize the distance dictionary with a default value if not already present
            if (!_pedestrianDistances.ContainsKey(newMember))
            {
                _pedestrianDistances.Add(newMember, float.MaxValue);
            }

            Debug.Log($"New pedestrian {newMember.name} added to group {groupName}.");
        }

        
        // Updates state machines and behaviors (e.g., movement, visibility state) without checking distances
        public void UpdateMemberStates()
        {
            //Debug.Log($"I am running in group {transform.name}");
            
            foreach (var member in allMembers)
            {
                // Use cached StateHandler reference to update state
                if (_cachedStateHandlers.TryGetValue(member, out StateHandler stateHandler))
                {
                    stateHandler.HandleMovementStateMachine();
                }

                // Update visibility based on cached distance (if distance was previously calculated)
                if (_pedestrianDistances.TryGetValue(member, out float distance))
                {
                    member.visible_dict["VisibilityState"] = distance <= system.visibleThreshold;
                }
            }
        }

        // Updates distances between members and players
        public void UpdateMemberDistances(List<GameObject> players, float visibleThreshold)
        {
            foreach (var member in allMembers)
            {
                // Find the closest player and update distance
                
                float closestDistance = CalculateDistanceToClosestPlayer(member, players);
                _pedestrianDistances[member] = closestDistance;
            }
        }

        // Calculate the distance between a pedestrian and the closest player
        private float CalculateDistanceToClosestPlayer(Pedestrian pedestrian, List<GameObject> players)
        {
            float closestDistance = float.MaxValue;
            Vector2 pedestrianPos = new Vector2(pedestrian.transform.position.x, pedestrian.transform.position.z);

            foreach (var player in players)
            {
                Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.z);
                float distance = Vector2.Distance(pedestrianPos, playerPos);

                if (distance < closestDistance)
                    closestDistance = distance;
            }

            return closestDistance;
        }
    }
}
