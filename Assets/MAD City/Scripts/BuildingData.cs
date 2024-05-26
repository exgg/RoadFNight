using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "CityGen/Buildings", order = 2)]
public class BuildingData : ScriptableObject
{
    public int minPieces = 5;
    public int maxPieces = 20;
    public GameObject[] baseParts;
    public GameObject[] middleParts;
    public GameObject[] topParts;
}
