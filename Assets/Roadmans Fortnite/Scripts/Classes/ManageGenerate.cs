using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ManageGenerate : MonoBehaviour
{
    public CityGen generateCity;

    public void GenerateCity()
    {
        print("Generating City");
        
        generateCity.ClearBuildings();
        generateCity.ClearBlocks();
        generateCity.ClearPerimeter();
        generateCity.Generate();
        generateCity.GenerateBuildings();
    }
}
