using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class ManageGenerate : MonoBehaviour
{
    public CityGen generateCity;
    public NavMeshSurface aISurface;

    private void Start()
    {
        if (!generateCity)
            generateCity = FindObjectOfType<CityGen>();
        
        if (!aISurface)
            aISurface = FindObjectOfType<NavMeshSurface>();

        if (generateCity && aISurface)
        {
            GenerateCity();
            GenerateNavMesh();
        }
    }

    private void GenerateCity()
    {
        print("Generating City");

        generateCity.ClearBuildings();
        generateCity.ClearBlocks();
        generateCity.ClearPerimeter();
        generateCity.Generate();
        generateCity.GenerateBuildings();
    }

    private void GenerateNavMesh()
    {
        print("building navmesh");
        aISurface.BuildNavMesh();
    }
}
