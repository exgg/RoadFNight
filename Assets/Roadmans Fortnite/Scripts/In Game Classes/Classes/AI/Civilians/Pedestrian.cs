using System;
using Redcode.Pools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects;
using Random = UnityEngine.Random;

public class Pedestrian : MonoBehaviour, IPoolObject
{
    public int health;
    
    [Header("Health Status")]
    public HealthStatus currenHealthStatus;
    private PedestrianSystem _system;

    [Space(2)]
    [Header("Pedestrian Class Statistics")]
    public Gender myGender;
    public Nationality myNationality;
    public RaceCategory myRace;
    public Religion myReligion;
    public WealthClass myWealthClass;
    public Sexuality mySexuality;

    [Header("Prejudice Settings")]
    [Tooltip("Setup on spawn from a dictionary within SpawnPedestrians in the master brain")]
    public PrejudiceSettings prejudice;
    
    [Header("Behaviour Types")]
    public BehaviourType myBehaviourType;
    public GroupControlType myGroupControlType;

    [Header("Levels for Behaviours")]
    [Tooltip("This is randomly generated on spawn to give different individuals")]
    public float confidenceLevel;
    public float aggressionLevel;
    
    [Header("Combat/Prejudice Fields")]
    public float attackRange = 2f; // The range at which the AI can start attacking
    
    //For server to allocate visible state to players
    //e.g. {player1 : true, player2 : false, player3 : true}
    public Dictionary<string, bool> visible_dict = new Dictionary<string, bool>();

    private void Awake()
    {
        _system = GetComponentInParent<PedestrianSystem>();

        confidenceLevel = Random.Range(0, 100);
        aggressionLevel = Random.Range(0, 100);
    }

    private void Start()
    {
        
    }

    // Called when getting this object from pool.
    public void OnGettingFromPool()
    {
        currenHealthStatus = HealthStatus.Alive;
        health = 100;
    }

    public void OnCreatedInPool()
    {
        throw new System.NotImplementedException();
    }

    public string CheckWalkingStyle()
    {
        return (myGender, mySexuality, myWealthClass, myBehaviourType) switch
        {
            //Sassy Walk
            (Gender.Male, Sexuality.Homosexual, _, BehaviourType.Standard) or
            (Gender.Female, _, WealthClass.UpperClass, BehaviourType.Standard) or
            (Gender.TransFemale, _, _, BehaviourType.Standard) or
            (Gender.Female, _, WealthClass.GangsterClass, BehaviourType.Standard) => "Sassy_Walk",
            
            // Gangster Walk
            (Gender.Male, _, WealthClass.GangsterClass, BehaviourType.Standard) => "Gangster_Walk",
            
            // Drunken Walk
            (_, _, _, BehaviourType.Drunk) or 
            (_, _, _, BehaviourType.Druggy) => "Drunk_Walk",
            
            // Base walk
            _ => "Standard_Walk"
        };
    }
}

