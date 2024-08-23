using System.Collections;
using System.Collections.Generic;
using Mirror;
using Roadmans_Fortnite.Scripts.Classes.Player.Controllers;
using Roadmans_Fortnite.Scripts.Classes.Player.Input;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UIElements;

public class TPManagerNew : NetworkBehaviour
{
    // weapon assembly stuff
    [Header("Weapon Data")]
    public WeaponManager currentWeaponManager;
    public GameObject weapons;
    public List<Transform> weaponsFound = new List<Transform>();
    public Transform shellEjectionPoint;
    private bool _hasActiveWeapon;
    private bool _isShooting;
    
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
    public GameObject firstPersonIdleCamera;
    public Transform target;

    [Header("Controllers")] 
    public ThirdPersonController thirdPersonController;
    
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
        
        // Find input handler and use this
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


    private void AimingLogic()
    {
        if (!currentWeaponManager || !playerRig || !thirdPersonController || !weaponAimCamera || !secondHandRigTarget || !playerAnimator)
        {
            Debug.LogError($"There is an issue with CurrentWeaponManager{currentWeaponManager == null} " +
                  $"n/ or the PlayerRig: {playerRig == null} n/ or Third Person Controller {thirdPersonController == null} n/ or" +
                  $"Weapon aim Camera {weaponAimCamera == null} n/ or SecondHandRigTarget {secondHandRigTarget == null} n/ or playerAnimator {playerAnimator == null}");
            return;
        }
        if (isLocalPlayer)
        {
              Vector3 mouseWorldPosition = Vector3.zero;
              Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
              Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);

              currentWeaponManager.debugTransform.position = ray.GetPoint(20f);
              mouseWorldPosition = ray.GetPoint(20f);

              Vector3 worldAimTarget = mouseWorldPosition;
              worldAimTarget.y = currentWeaponManager.Player.position.y;
              Vector3 aimDirection = (worldAimTarget - currentWeaponManager.Player.position).normalized;

              if (_input.aimInput) // why do we have 2 here?
              {

                  // use the inventory manager, this however will be using the player manager class: 

                  currentWeaponManager.Player.forward = Vector3.Lerp(currentWeaponManager.Player.forward, aimDirection, Time.deltaTime * 20f);
                  
                  playerRig.weight = 1;
            
                
                  secondHandRigTarget.localPosition = currentWeaponManager.LeftHandPosition;
                  secondHandRigTarget.localRotation = currentWeaponManager.LeftHandRotation;
             

                  currentWeaponManager.isAiming = true;
                  currentWeaponManager.Crosshair.SetActive(true);

                  // use the animation set from the current weapon
                  if (!_input.isShooting)
                      playerAnimator.Play(currentWeaponManager.WeaponAimTriggerName);


                  thirdPersonController.SetSensitivity(currentWeaponManager.aimSensitivity);
                  thirdPersonController.SetRotateOnMove(false);
  
                  
                  weaponAimCamera.SetActive(true);
              }
              
              else
              {
                  if (playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("KickOut"))
                  {
                      if(!carTheftCamera.activeInHierarchy)
                          carTheftCamera.SetActive(true);
                  }
                  else
                  {
                      if(carTheftCamera.activeInHierarchy)
                          carTheftCamera.SetActive(false);
                  }
              }
        }
        else
        {
            if (_input.aimInput)
            {
                
                playerRig.weight = 1;
                
                // set target rig location / rotation 
                secondHandRigTarget.localPosition = currentWeaponManager.LeftHandPosition;
                secondHandRigTarget.localRotation = currentWeaponManager.LeftHandRotation;

                // toggle is aiming
                currentWeaponManager.isAiming = true;
                
                // play the animation of aiming
                playerAnimator.Play(currentWeaponManager.WeaponAimTriggerName);

                thirdPersonController.SetSensitivity(currentWeaponManager.aimSensitivity);
            }
            else
            {
                playerRig.weight = 0;
                currentWeaponManager.isAiming = false;
                
                thirdPersonController.SetSensitivity(currentWeaponManager.normalSensitivity);
                thirdPersonController.SetRotateOnMove(true);
                
                playerAnimator.ResetTrigger(currentWeaponManager.WeaponAimTriggerName);
                playerAnimator.SetTrigger(currentWeaponManager.WeaponIdleTriggerName);
            }
        }
       
        // this then sets to player ai weapon is false... we are getting rid of this
    }

    public void StickCameraToPlayer()
    {
        
    }

    public void StickCameraToVehicle()
    {
        
    }
    
    
    
}
