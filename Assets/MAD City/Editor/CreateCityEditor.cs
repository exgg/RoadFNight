using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class CreateCityEditor : Editor
{

    public GameObject CityGenerator;
[MenuItem("Tools/MAD City/New City",false,0)]
public static void NewCity()
    {
        GameObject newCity = Instantiate(Resources.Load("MADCity", typeof(GameObject))) as GameObject;
        newCity.name = "MADCity";
    }
}
