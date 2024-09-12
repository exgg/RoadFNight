using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "DistrictData", menuName = "CityGen/District", order = 4)]
public class DistrictData : ScriptableObject
{
    PersonData.PersonRace dist_race; //predominant_race
    PersonData.PersonReligion dist_religion;
    int people_volume;

    void Start()
    {

    }

    void Update()
    {

    }
}
