using System.Collections;
using System.Collections.Generic;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects;
using UnityEngine;

[CreateAssetMenu(fileName = "RegionsData", menuName = "CityGen/Regions", order = 2)]
public class RegionsData : ScriptableObject
{
    public string RegionName;
    public int RegionNo;
    //public Color RegionColor;
    public GameObject[] Blocks;
    public BuildingData[] SmallBuildings;
    public BuildingData[] LargeBuildings;
    public BuildingData[] PointsOfInterest;
    public ColorPallete buildingColors;
}
