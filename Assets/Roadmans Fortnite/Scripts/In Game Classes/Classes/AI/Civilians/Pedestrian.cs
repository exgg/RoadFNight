using Redcode.Pools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

using Roadmans_Fortnite.Data.Enums.NPCEnums;

public class Pedestrian : MonoBehaviour, IPoolObject
{
    
    public int Health { get; private set; }
    public HealthStatus currenHealthStatus;
    private PedestrianSystem _system;

    public Gender myGender;
    public Nationality myNationality;
    public WealthClass myWealthClass;
    public Sexuality mySexuality;
    public BehaviourType myBehaviourType;
    
    //For server to allocate visible state to players
    //e.g. {player1 : true, player2 : false, player3 : true}
    public Dictionary<string, bool> visible_dict = new Dictionary<string, bool>();

    private void Awake()
    {
        _system = GetComponentInParent<PedestrianSystem>();
    }

    // Called when getting this object from pool.
    public void OnGettingFromPool()
    {
        currenHealthStatus = HealthStatus.Alive;
        Health = 100;
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

