using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PedestrianGroupData", menuName = "Pedestrian/PedestrianGroup", order = 1)]
public class PedestrianGroupData : ScriptableObject
{
    public float detecting_radius = 20;//within this range
    public int max_num = 5;//max number of the group

    public float insult_threshold = 50;
    public float fight_threshold = 80;
}
