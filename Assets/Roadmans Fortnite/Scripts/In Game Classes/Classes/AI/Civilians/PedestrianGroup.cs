using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gley.PedestrianSystem;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using RoadfnightPedestrian;
using Gley.PedestrianSystem.Internal;

/*
 * A group brain that controls specific groups
 */

public class PedestrianGroup : MonoBehaviour
{
    public string group_name;

    public Pedestrian leader;
    public List<Pedestrian> all_members = new List<Pedestrian>();
    public PedestrianSystem _system;

    private void Awake()
    {
        
    }

    public void update_members()
    {
        foreach (var member in GetComponentsInChildren<Pedestrian>(true))
        {
            //update state
            member.GetComponent<StateHandler>().HandleMovementStateMachine();
            //update visibility
            Dictionary<string, float> distance_dict = calculate_distance_to_players(member);
            foreach (var player in distance_dict.Keys)
            {
                member.visible_dict[player] = update_visible_state(distance_dict[player]);
            }
        }
    }

    //Calculate the distance between pedestrian and players
    public Dictionary<string, float> calculate_distance_to_players(Pedestrian pedestrian)
    {
        Dictionary<string, float> distance_dict = new Dictionary<string, float>();

        foreach (var player in _system.player_lst)
        {
            Vector2 player_pos = new Vector2(player.transform.position.x, player.transform.position.z);
            Vector2 _pos = new Vector2(pedestrian.transform.position.x, pedestrian.transform.position.z);
            distance_dict[player.name] = Vector2.Distance(player_pos, _pos);
        }
        foreach (var i in distance_dict)
        {
            Debug.Log(i);
        }
        return distance_dict;
    }

    public bool update_visible_state(float distance)
    {
        return distance > _system.visible_threshold ? false : true;
    }
}
