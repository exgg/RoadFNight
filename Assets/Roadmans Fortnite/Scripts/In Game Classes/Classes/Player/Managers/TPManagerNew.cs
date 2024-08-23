using System.Collections;
using System.Collections.Generic;
using Mirror;
using Roadmans_Fortnite.Scripts.Classes.Player.Input;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class TPManagerNew : NetworkBehaviour
{
    // weapon assembly stuff
    [Header("Weapon Data")]
    public WeaponManager currentWeaponManager;
    public GameObject weapons;
    public List<Transform> weaponsFound = new List<Transform>();
    public Transform shellEjectionPoint;
    private bool _hasActiveWeapon;
    
    // player rigging stuff
    [Header("Player Animation")] 
    public Rig playerRig;
    public Transform secondHandRigTarget;
    public Animator playerAnimator;
    
    private InputHandler _input;

    // data for the camera. Aiming, idle, no weapon idle
    [Header("Camera")]
    public GameObject idleCamera;
    public GameObject weaponIdleCamera;
    public GameObject weaponAimCamera;
    public GameObject firstPersonControllerCamera;
    public Transform target;

    [Header("Camera Modes")] 
    public bool isFirstPerson = false;

    [Header("HeadMesh")] 
    public GameObject[] headMeshes;

    [Header("LoadingScreen")] 
    public GameObject loadingScreenPrefab; // this may need removing

    [Header("Car Theft Cams")] 
    public GameObject carTheftCamera;

    [SyncVar] 
    public int aimValue;

    public void Initialize()
    {
        // toggle loading screen
        
        // Find input handle
        // get character controller 
        // enable cc
        // find third person controller
        // Find all cameras
        // find all weapons
            // foreach weapon 
            // blah blah
            
        // find if has an active weapon
            // if no active weapon set the animator weight to 1
    }
    public void TickUpdate()
    {
        
    }
    
    
    private void 
}
