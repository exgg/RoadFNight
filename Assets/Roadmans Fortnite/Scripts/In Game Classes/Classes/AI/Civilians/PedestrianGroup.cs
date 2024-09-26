using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * A group brain that controls specific groups
 */

public class PedestrianGroup : MonoBehaviour
{
    public string group_name;

    public PrejudiceInfo prejudiceInfo;

    public Pedestrian group_lead;
    public List<Pedestrian> senior_member = new List<Pedestrian>();
    public List<Pedestrian> junior_member = new List<Pedestrian>();

    //hate points to other groups, if over the threshold might lead to group fight
    public Dictionary<string, float> hate_dict = new Dictionary<string, float>();
    
}
