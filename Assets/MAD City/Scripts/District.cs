using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class District : MonoBehaviour
{
    public int level;
    public DistrictData district_data;

    public Race predominant_race;//>50%
    public Dictionary<Race, float> race_lst = new Dictionary<Race, float>();//race, ratio
    
}
