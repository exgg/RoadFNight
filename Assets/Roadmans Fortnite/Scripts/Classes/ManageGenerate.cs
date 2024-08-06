using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.Serialization;

public class ManageGenerate : MonoBehaviour
{
    public CityGen generateCity;
    public NavMeshSurface aISurface;

    public bool navMeshCreated;
    
    private void Start()
    {
        if (!generateCity)
            generateCity = FindObjectOfType<CityGen>();
        
        if (!aISurface)
            aISurface = FindObjectOfType<NavMeshSurface>();

        GenerateCity();
        GenerateNavMesh();
    }

    private void GenerateCity()
    {
        print($"Generating City City Class:");

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

        navMeshCreated = true;
    }
}
