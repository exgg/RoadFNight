using System;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VehicleEnterExit;

public class BotSpawner : NetworkBehaviour
{
    [SerializeField] GameObject _playerPrefab;
    PlayerAI _spawnedBot;

    [SerializeField] public VehicleSync _targetedVehicle;
    [SerializeField] public Transform _targetedWaypoint;
   // [HideInInspector]
    public Instance instance;

    private GameObject _instanceManagement;
    
    private void Start()
    {
        _instanceManagement = GameObject.FindGameObjectWithTag("InstanceTag");
        instance = _instanceManagement.GetComponent<Instance>();
        
    }

    private void Update ()
    {
        if (!isServer)
            return;

        if (_spawnedBot != null)
            return;

        _spawnedBot = Instantiate(_playerPrefab, transform.position, transform.rotation).GetComponent<PlayerAI>();
        
        NetworkServer.Spawn(_spawnedBot.gameObject);

        instance.instancedServerWorldObjects.Add(_spawnedBot.gameObject);

        if (_targetedVehicle)
        {
            _spawnedBot.SetAsBot();
            _spawnedBot.GetInTheVehicle(_targetedVehicle);
        }
        else
        {
            _spawnedBot.SetAsBot();
            _spawnedBot.SetWaypoint(_targetedWaypoint, "Walking");
        }
        Debug.Log(gameObject.name + " has instantiated " + "'" + _spawnedBot.gameObject.name + "'");
    }
}
